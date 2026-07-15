using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Faq.Contracts;
using Faq.Domain;
using FluentValidation;

namespace Faq.Application;

/// <summary>
/// Sắp lại thứ tự DANH MỤC FAQ của một resort (Req 4.5 drag-drop). Cập nhật <c>SortOrder</c> hàng loạt trong MỘT
/// transaction (nguyên tử — void command <see cref="ICommandUseCase{TInput}"/> khai <see cref="PersistenceKey"/> →
/// TransactionCommandUseCaseDecorator bọc một transaction quanh SaveChanges). Mỗi entry phải là danh mục THUỘC
/// <see cref="ReorderFaqCategoriesInput.ResortId"/> (chống sửa chéo resort) — sai → <see cref="FaqErrors.CategoryNotFound"/>.
/// Chỉ cập nhật Id được gửi (partial cho phép). Concurrency xmin (CP15) áp cho từng bản ghi.
/// </summary>
public sealed class ReorderFaqCategoriesUseCase : ICommandUseCase<ReorderFaqCategoriesInput>
{
    public string PersistenceKey => FaqModule.PersistenceKey;

    private readonly IRepository<FaqCategory> _categories;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderFaqCategoriesUseCase(IRepository<FaqCategory> categories, IUnitOfWork unitOfWork)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(ReorderFaqCategoriesInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        foreach (var entry in input.Entries)
        {
            var category = await _categories.FindByIdAsync(entry.Id, ct).ConfigureAwait(false);
            if (category is null || category.ResortId != input.ResortId)
            {
                return Result.Failure(FaqErrors.CategoryNotFound);
            }

            category.SortOrder = entry.SortOrder;
        }

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>Validate reorder danh mục: có entry; Id phân biệt; SortOrder ≥ 0.</summary>
public sealed class ReorderFaqCategoriesValidator : AbstractValidator<ReorderFaqCategoriesInput>
{
    public ReorderFaqCategoriesValidator()
    {
        RuleFor(x => x.ResortId).NotEmpty();
        RuleFor(x => x.Entries).NotEmpty();
        RuleForEach(x => x.Entries).ChildRules(e => e.RuleFor(v => v.SortOrder).GreaterThanOrEqualTo(0));
        RuleFor(x => x.Entries)
            .Must(entries => entries.Select(e => e.Id).Distinct().Count() == entries.Count)
            .WithMessage("Danh sách thứ tự có Id trùng lặp.")
            .When(x => x.Entries is not null);
    }
}

/// <summary>
/// Sắp lại thứ tự MỤC FAQ trong một danh mục (Req 4.5 drag-drop). Cập nhật <c>SortOrder</c> hàng loạt nguyên tử.
/// Mỗi entry phải là item THUỘC <see cref="ReorderFaqItemsInput.CategoryId"/> — sai → <see cref="FaqErrors.ItemNotFound"/>.
/// KHÔNG đổi quan hệ cha-con (chỉ thứ tự) — reorder không phải move-node (giữ đơn giản; move dùng UpdateFaqItem).
/// </summary>
public sealed class ReorderFaqItemsUseCase : ICommandUseCase<ReorderFaqItemsInput>
{
    public string PersistenceKey => FaqModule.PersistenceKey;

    private readonly IRepository<FaqItem> _items;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderFaqItemsUseCase(IRepository<FaqItem> items, IUnitOfWork unitOfWork)
    {
        _items = items;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(ReorderFaqItemsInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        foreach (var entry in input.Entries)
        {
            var item = await _items.FindByIdAsync(entry.Id, ct).ConfigureAwait(false);
            if (item is null || item.CategoryId != input.CategoryId)
            {
                return Result.Failure(FaqErrors.ItemNotFound);
            }

            item.SortOrder = entry.SortOrder;
        }

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>Validate reorder item: có entry; Id phân biệt; SortOrder ≥ 0.</summary>
public sealed class ReorderFaqItemsValidator : AbstractValidator<ReorderFaqItemsInput>
{
    public ReorderFaqItemsValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Entries).NotEmpty();
        RuleForEach(x => x.Entries).ChildRules(e => e.RuleFor(v => v.SortOrder).GreaterThanOrEqualTo(0));
        RuleFor(x => x.Entries)
            .Must(entries => entries.Select(e => e.Id).Distinct().Count() == entries.Count)
            .WithMessage("Danh sách thứ tự có Id trùng lặp.")
            .When(x => x.Entries is not null);
    }
}
