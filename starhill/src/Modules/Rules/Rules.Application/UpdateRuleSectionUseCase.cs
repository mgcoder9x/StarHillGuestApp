using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using FluentValidation;
using Rules.Contracts;
using Rules.Domain;

namespace Rules.Application;

/// <summary>
/// Sửa cấu hình đọc của một section Draft (SortOrder/IsRequired/RequireScrollEnd/MinReadSeconds). KHÔNG đổi Key
/// (khóa ổn định — design §3) hay RuleSetId. Không tồn tại → <see cref="RulesErrors.RuleSectionNotFound"/>.
/// Concurrency: xmin (<c>IHasConcurrencyToken</c>) — hai người sửa đè → base <c>ConcurrencyConflictException</c>
/// → middleware 409 (CP15). Void command → <see cref="ICommandUseCase{TInput}"/> khai <see cref="PersistenceKey"/>
/// (TransactionCommandUseCaseDecorator resolve đúng Unit of Work Rules). Mirror <c>UpdateRoomUseCase</c>.
/// </summary>
public sealed class UpdateRuleSectionUseCase : ICommandUseCase<UpdateRuleSectionInput>
{
    public string PersistenceKey => RulesModule.PersistenceKey;

    private readonly IRepository<RuleSection> _sections;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRuleSectionUseCase(IRepository<RuleSection> sections, IUnitOfWork unitOfWork)
    {
        _sections = sections;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(UpdateRuleSectionInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var section = await _sections.FindByIdAsync(input.SectionId, ct).ConfigureAwait(false);
        if (section is null)
        {
            return Result.Failure(RulesErrors.RuleSectionNotFound);
        }

        ConcurrencyGuard.EnsureExpectedRowVersion(section.RowVersion, input.ExpectedRowVersion);

        section.SortOrder = input.SortOrder;
        section.IsRequired = input.IsRequired;
        section.RequireScrollEnd = input.RequireScrollEnd;
        section.MinReadSeconds = input.MinReadSeconds;

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>Validate sửa section.</summary>
public sealed class UpdateRuleSectionValidator : AbstractValidator<UpdateRuleSectionInput>
{
    public UpdateRuleSectionValidator()
    {
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinReadSeconds).InclusiveBetween(0, 3600);
    }
}
