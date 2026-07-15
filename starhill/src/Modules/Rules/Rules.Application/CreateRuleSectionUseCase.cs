using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using FluentValidation;
using Rules.Domain;

namespace Rules.Application;

/// <summary>
/// Tạo một section nội quy trong bản Draft của resort (thiết kế §3/§4). LAZY ensure <see cref="RuleSet"/>: nếu resort
/// chưa có bản nháp thì tạo cùng transaction với section (một SaveChanges = nguyên tử). Bất biến "một RuleSet/resort"
/// được ràng buộc ở DB bằng unique <c>ux_rule_set_resort</c> (QR-AD-035) — nếu hai request đua tạo bản nháp đầu tiên,
/// người sau nhận <see cref="UniqueConstraintViolationException"/> (base QR-AD-010) → trả <see cref="RulesErrors.DraftConflict"/>
/// (an toàn, không phá dữ liệu; client thử lại sẽ thấy RuleSet đã có). Inject <see cref="IRepository{T}"/> trực tiếp (DV-002).
/// <para>Là <see cref="IUseCase{TInput,TOutput}"/> (value-returning) tự quản SaveChanges — mirror <c>CreateRoomUseCase</c>.</para>
/// </summary>
public sealed class CreateRuleSectionUseCase : IUseCase<CreateRuleSectionInput, CreateRuleSectionResult>
{
    private readonly IRepository<RuleSet> _ruleSets;
    private readonly IRepository<RuleSection> _sections;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateRuleSectionUseCase(
        IRepository<RuleSet> ruleSets,
        IRepository<RuleSection> sections,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _ruleSets = ruleSets;
        _sections = sections;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CreateRuleSectionResult>> ExecuteAsync(
        CreateRuleSectionInput input,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var now = _clock.UtcNow;

        var ruleSet = await _ruleSets.FirstOrDefaultAsync(x => x.ResortId == input.ResortId, ct).ConfigureAwait(false);
        if (ruleSet is null)
        {
            ruleSet = new RuleSet { ResortId = input.ResortId, UpdatedAt = now };
            _ruleSets.Add(ruleSet);
        }
        else
        {
            ruleSet.UpdatedAt = now;
        }

        var section = new RuleSection
        {
            RuleSetId = ruleSet.Id,
            Key = input.Key,
            SortOrder = input.SortOrder,
            IsRequired = input.IsRequired,
            RequireScrollEnd = input.RequireScrollEnd,
            MinReadSeconds = input.MinReadSeconds,
        };
        _sections.Add(section);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            // ux_rule_set_resort: một request khác vừa tạo RuleSet đầu tiên cho resort này (đua hiếm).
            return Result.Failure<CreateRuleSectionResult>(RulesErrors.DraftConflict);
        }

        return Result.Success(new CreateRuleSectionResult(section.Id));
    }
}

/// <summary>Validate tạo section (ValidationUseCaseDecorator chặn trước khi vào use case).</summary>
public sealed class CreateRuleSectionValidator : AbstractValidator<CreateRuleSectionInput>
{
    public CreateRuleSectionValidator()
    {
        RuleFor(x => x.ResortId).NotEmpty();
        RuleFor(x => x.Key).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinReadSeconds).InclusiveBetween(0, 3600);
    }
}
