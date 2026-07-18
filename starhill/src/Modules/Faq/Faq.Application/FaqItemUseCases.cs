using Bedrock.Application.Ports.Html;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Faq.Contracts;
using Faq.Domain;
using FluentValidation;

namespace Faq.Application;

/// <summary>
/// Tạo mục FAQ dưới một danh mục (Req 4.1/8.5). ResortId DERIVE từ category (nguồn sự thật — tránh mismatch). Validate
/// bất biến cây <c>ParentId</c> (cùng category, tồn tại; cycle không thể xảy ra khi tạo — <see cref="FaqItemParentValidator"/>).
/// Danh mục không tồn tại → <see cref="FaqErrors.CategoryNotFound"/>; parent xấu → <see cref="FaqErrors.InvalidParent"/>.
/// Value-returning <see cref="IUseCase{TInput,TOutput}"/> tự quản một SaveChanges.
/// </summary>
public sealed class CreateFaqItemUseCase : IUseCase<CreateFaqItemInput, CreateFaqItemResult>
{
    private readonly IRepository<FaqCategory> _categories;
    private readonly IRepository<FaqItem> _items;
    private readonly IUnitOfWork _unitOfWork;

    public CreateFaqItemUseCase(
        IRepository<FaqCategory> categories, IRepository<FaqItem> items, IUnitOfWork unitOfWork)
    {
        _categories = categories;
        _items = items;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateFaqItemResult>> ExecuteAsync(
        CreateFaqItemInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var category = await _categories.FindByIdAsync(input.CategoryId, ct).ConfigureAwait(false);
        if (category is null)
        {
            return Result.Failure<CreateFaqItemResult>(FaqErrors.CategoryNotFound);
        }

        var parentOk = await FaqItemParentValidator
            .IsValidParentAsync(_items, input.CategoryId, input.ParentId, selfId: null, ct)
            .ConfigureAwait(false);
        if (!parentOk)
        {
            return Result.Failure<CreateFaqItemResult>(FaqErrors.InvalidParent);
        }

        var item = new FaqItem
        {
            ResortId = category.ResortId,
            CategoryId = input.CategoryId,
            ParentId = input.ParentId,
            SortOrder = input.SortOrder,
            IsActive = input.IsActive,
        };
        _items.Add(item);

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success(new CreateFaqItemResult(item.Id));
    }
}

/// <summary>Validate tạo mục FAQ.</summary>
public sealed class CreateFaqItemValidator : AbstractValidator<CreateFaqItemInput>
{
    public CreateFaqItemValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

/// <summary>
/// Sửa mục FAQ (ParentId/SortOrder/IsActive; KHÔNG đổi CategoryId). Re-validate cây (self/cùng-category/cycle —
/// <see cref="FaqItemParentValidator"/>). Không tồn tại → <see cref="FaqErrors.ItemNotFound"/>; parent xấu →
/// <see cref="FaqErrors.InvalidParent"/>. Concurrency xmin (CP15). Void command khai <see cref="PersistenceKey"/>.
/// </summary>
public sealed class UpdateFaqItemUseCase : ICommandUseCase<UpdateFaqItemInput>
{
    public string PersistenceKey => FaqModule.PersistenceKey;

    private readonly IRepository<FaqItem> _items;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateFaqItemUseCase(IRepository<FaqItem> items, IUnitOfWork unitOfWork)
    {
        _items = items;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(UpdateFaqItemInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var item = await _items.FindByIdAsync(input.ItemId, ct).ConfigureAwait(false);
        if (item is null)
        {
            return Result.Failure(FaqErrors.ItemNotFound);
        }

        ConcurrencyGuard.EnsureExpectedRowVersion(item.RowVersion, input.ExpectedRowVersion);

        var parentOk = await FaqItemParentValidator
            .IsValidParentAsync(_items, item.CategoryId, input.ParentId, selfId: item.Id, ct)
            .ConfigureAwait(false);
        if (!parentOk)
        {
            return Result.Failure(FaqErrors.InvalidParent);
        }

        item.ParentId = input.ParentId;
        item.SortOrder = input.SortOrder;
        item.IsActive = input.IsActive;

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>Validate sửa mục FAQ.</summary>
public sealed class UpdateFaqItemValidator : AbstractValidator<UpdateFaqItemInput>
{
    public UpdateFaqItemValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

/// <summary>
/// Xóa mục FAQ — CHẶN nếu còn mục con tham chiếu (<see cref="FaqErrors.ItemHasChildren"/>): giữ toàn vẹn cây thay vì
/// cascade âm thầm (FK self ParentId Restrict — backstop DB). Bản dịch cascade theo (FK Cascade). Không tồn tại →
/// <see cref="FaqErrors.ItemNotFound"/>. Void command khai <see cref="PersistenceKey"/>.
/// </summary>
public sealed class DeleteFaqItemUseCase : ICommandUseCase<DeleteFaqItemInput>
{
    public string PersistenceKey => FaqModule.PersistenceKey;

    private readonly IRepository<FaqItem> _items;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFaqItemUseCase(IRepository<FaqItem> items, IUnitOfWork unitOfWork)
    {
        _items = items;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(DeleteFaqItemInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var item = await _items.FindByIdAsync(input.ItemId, ct).ConfigureAwait(false);
        if (item is null)
        {
            return Result.Failure(FaqErrors.ItemNotFound);
        }

        ConcurrencyGuard.EnsureExpectedRowVersion(item.RowVersion, input.ExpectedRowVersion);

        var hasChildren = await _items.AnyAsync(i => i.ParentId == input.ItemId, ct).ConfigureAwait(false);
        if (hasChildren)
        {
            return Result.Failure(FaqErrors.ItemHasChildren);
        }

        _items.Remove(item);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Tạo/cập nhật bản dịch mục FAQ — SANITIZE-ON-SAVE (Req 8.6/CP12): <c>Question</c> + <c>AnswerHtml</c> qua
/// <see cref="IHtmlSanitizer"/> TRƯỚC lưu (cột <c>AnswerHtmlSanitized</c>). Upsert theo unique <c>(item, lang)</c>.
/// Mục không tồn tại → <see cref="FaqErrors.ItemNotFound"/>. Đua tạo cùng ngôn ngữ → <see cref="FaqErrors.Conflict"/>.
/// </summary>
public sealed class UpsertFaqItemTranslationUseCase
    : IUseCase<UpsertFaqItemTranslationInput, UpsertFaqItemTranslationResult>
{
    private readonly IRepository<FaqItem> _items;
    private readonly IRepository<FaqItemTranslation> _translations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHtmlSanitizer _sanitizer;

    public UpsertFaqItemTranslationUseCase(
        IRepository<FaqItem> items,
        IRepository<FaqItemTranslation> translations,
        IUnitOfWork unitOfWork,
        IHtmlSanitizer sanitizer)
    {
        _items = items;
        _translations = translations;
        _unitOfWork = unitOfWork;
        _sanitizer = sanitizer;
    }

    public async Task<Result<UpsertFaqItemTranslationResult>> ExecuteAsync(
        UpsertFaqItemTranslationInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var itemExists = await _items.AnyAsync(i => i.Id == input.ItemId, ct).ConfigureAwait(false);
        if (!itemExists)
        {
            return Result.Failure<UpsertFaqItemTranslationResult>(FaqErrors.ItemNotFound);
        }

        var question = Sanitize(input.Question);
        var answer = Sanitize(input.AnswerHtml);

        var existing = await _translations
            .FirstOrDefaultAsync(t => t.FaqItemId == input.ItemId && t.LanguageCode == input.LanguageCode, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            ConcurrencyGuard.EnsureExpectedRowVersion(existing.RowVersion, input.ExpectedRowVersion);
            existing.Question = question;
            existing.AnswerHtmlSanitized = answer;
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            return Result.Success(new UpsertFaqItemTranslationResult(existing.Id));
        }

        ConcurrencyGuard.EnsureExpectedRowVersion(null, input.ExpectedRowVersion);

        var translation = new FaqItemTranslation
        {
            FaqItemId = input.ItemId,
            LanguageCode = input.LanguageCode,
            Question = question,
            AnswerHtmlSanitized = answer,
        };
        _translations.Add(translation);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Failure<UpsertFaqItemTranslationResult>(FaqErrors.Conflict);
        }

        return Result.Success(new UpsertFaqItemTranslationResult(translation.Id));
    }

    private string? Sanitize(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? null : _sanitizer.Sanitize(raw);
}

/// <summary>Validate upsert bản dịch mục FAQ.</summary>
public sealed class UpsertFaqItemTranslationValidator : AbstractValidator<UpsertFaqItemTranslationInput>
{
    public UpsertFaqItemTranslationValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.LanguageCode).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Question).MaximumLength(500).When(x => x.Question is not null);
    }
}
