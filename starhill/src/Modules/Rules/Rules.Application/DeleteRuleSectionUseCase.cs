using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Rules.Contracts;
using Rules.Domain;

namespace Rules.Application;

/// <summary>
/// Xóa một section Draft (kèm mọi bản dịch — FK <c>rule_section_translation</c>→<c>rule_section</c> ON DELETE CASCADE,
/// cùng schema <c>rules</c>). Không tồn tại → <see cref="RulesErrors.RuleSectionNotFound"/>. Xóa CỨNG (Draft không
/// soft-delete: nội dung đã Publish nằm ở snapshot bất biến riêng, xóa Draft không đụng cái khách đang đọc — CP4).
/// Void command → <see cref="ICommandUseCase{TInput}"/> khai <see cref="PersistenceKey"/>. Dùng input record
/// <see cref="DeleteRuleSectionInput"/> (KHÔNG Guid trần) để service type DUY NHẤT toàn Host (tránh đụng
/// <c>ICommandUseCase&lt;Guid&gt;</c> của module khác như DeleteRoom).
/// </summary>
public sealed class DeleteRuleSectionUseCase : ICommandUseCase<DeleteRuleSectionInput>
{
    public string PersistenceKey => RulesModule.PersistenceKey;

    private readonly IRepository<RuleSection> _sections;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRuleSectionUseCase(IRepository<RuleSection> sections, IUnitOfWork unitOfWork)
    {
        _sections = sections;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(DeleteRuleSectionInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var section = await _sections.FindByIdAsync(input.SectionId, ct).ConfigureAwait(false);
        if (section is null)
        {
            return Result.Failure(RulesErrors.RuleSectionNotFound);
        }

        ConcurrencyGuard.EnsureExpectedRowVersion(section.RowVersion, input.ExpectedRowVersion);

        _sections.Remove(section);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}
