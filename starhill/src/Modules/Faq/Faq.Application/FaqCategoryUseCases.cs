using Bedrock.Application.Ports.Html;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Faq.Contracts;
using Faq.Domain;
using FluentValidation;

namespace Faq.Application;

/// <summary>
/// Tạo danh mục FAQ (Req 4.1/8.5). Unique <c>(ResortId, Key)</c> cấp DB (<c>ux_faq_category_key</c>) — đua tạo trùng
/// Key → <see cref="UniqueConstraintViolationException"/> (base QR-AD-010) → <see cref="FaqErrors.Conflict"/>. Value-returning
/// <see cref="IUseCase{TInput,TOutput}"/> tự quản một SaveChanges (mirror CreateRoom/CreateRuleSection). Inject repo trực tiếp (DV-002).
/// </summary>
public sealed class CreateFaqCategoryUseCase : IUseCase<CreateFaqCategoryInput, CreateFaqCategoryResult>
{
    private readonly IRepository<FaqCategory> _categories;
    private readonly IUnitOfWork _unitOfWork;

    public CreateFaqCategoryUseCase(IRepository<FaqCategory> categories, IUnitOfWork unitOfWork)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateFaqCategoryResult>> ExecuteAsync(
        CreateFaqCategoryInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var category = new FaqCategory
        {
            ResortId = input.ResortId,
            Key = input.Key,
            SortOrder = input.SortOrder,
            IsActive = input.IsActive,
        };
        _categories.Add(category);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Failure<CreateFaqCategoryResult>(FaqErrors.Conflict);
        }

        return Result.Success(new CreateFaqCategoryResult(category.Id));
    }
}

/// <summary>Validate tạo danh mục.</summary>
public sealed class CreateFaqCategoryValidator : AbstractValidator<CreateFaqCategoryInput>
{
    public CreateFaqCategoryValidator()
    {
        RuleFor(x => x.ResortId).NotEmpty();
        RuleFor(x => x.Key).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

/// <summary>
/// Sửa danh mục (SortOrder/IsActive; KHÔNG đổi Key/ResortId). Không tồn tại → <see cref="FaqErrors.CategoryNotFound"/>.
/// Concurrency xmin (CP15). Void command → <see cref="ICommandUseCase{TInput}"/> khai <see cref="PersistenceKey"/>.
/// </summary>
public sealed class UpdateFaqCategoryUseCase : ICommandUseCase<UpdateFaqCategoryInput>
{
    public string PersistenceKey => FaqModule.PersistenceKey;

    private readonly IRepository<FaqCategory> _categories;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateFaqCategoryUseCase(IRepository<FaqCategory> categories, IUnitOfWork unitOfWork)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(UpdateFaqCategoryInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var category = await _categories.FindByIdAsync(input.CategoryId, ct).ConfigureAwait(false);
        if (category is null)
        {
            return Result.Failure(FaqErrors.CategoryNotFound);
        }

        ConcurrencyGuard.EnsureExpectedRowVersion(category.RowVersion, input.ExpectedRowVersion);

        category.SortOrder = input.SortOrder;
        category.IsActive = input.IsActive;

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>Validate sửa danh mục.</summary>
public sealed class UpdateFaqCategoryValidator : AbstractValidator<UpdateFaqCategoryInput>
{
    public UpdateFaqCategoryValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

/// <summary>
/// Xóa danh mục — CHẶN nếu còn item tham chiếu (<see cref="FaqErrors.CategoryNotEmpty"/>): giữ toàn vẹn cây thay vì
/// cascade âm thầm (FK item→category là Restrict — backstop DB). Bản dịch danh mục cascade theo (FK Cascade). Không
/// tồn tại → <see cref="FaqErrors.CategoryNotFound"/>. Void command khai <see cref="PersistenceKey"/>.
/// </summary>
public sealed class DeleteFaqCategoryUseCase : ICommandUseCase<DeleteFaqCategoryInput>
{
    public string PersistenceKey => FaqModule.PersistenceKey;

    private readonly IRepository<FaqCategory> _categories;
    private readonly IRepository<FaqItem> _items;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFaqCategoryUseCase(
        IRepository<FaqCategory> categories, IRepository<FaqItem> items, IUnitOfWork unitOfWork)
    {
        _categories = categories;
        _items = items;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(DeleteFaqCategoryInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var category = await _categories.FindByIdAsync(input.CategoryId, ct).ConfigureAwait(false);
        if (category is null)
        {
            return Result.Failure(FaqErrors.CategoryNotFound);
        }

        ConcurrencyGuard.EnsureExpectedRowVersion(category.RowVersion, input.ExpectedRowVersion);

        var hasItems = await _items.AnyAsync(i => i.CategoryId == input.CategoryId, ct).ConfigureAwait(false);
        if (hasItems)
        {
            return Result.Failure(FaqErrors.CategoryNotEmpty);
        }

        _categories.Remove(category);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Tạo/cập nhật tên danh mục theo ngôn ngữ — SANITIZE-ON-SAVE (CP12): <c>Name</c> qua <see cref="IHtmlSanitizer"/>
/// TRƯỚC lưu (phòng stored-XSS nếu render Name không escape). Upsert theo unique <c>(category, lang)</c>. Danh mục
/// không tồn tại → <see cref="FaqErrors.CategoryNotFound"/>. Đua tạo cùng ngôn ngữ → <see cref="FaqErrors.Conflict"/>.
/// </summary>
public sealed class UpsertFaqCategoryTranslationUseCase
    : IUseCase<UpsertFaqCategoryTranslationInput, UpsertFaqCategoryTranslationResult>
{
    private readonly IRepository<FaqCategory> _categories;
    private readonly IRepository<FaqCategoryTranslation> _translations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHtmlSanitizer _sanitizer;

    public UpsertFaqCategoryTranslationUseCase(
        IRepository<FaqCategory> categories,
        IRepository<FaqCategoryTranslation> translations,
        IUnitOfWork unitOfWork,
        IHtmlSanitizer sanitizer)
    {
        _categories = categories;
        _translations = translations;
        _unitOfWork = unitOfWork;
        _sanitizer = sanitizer;
    }

    public async Task<Result<UpsertFaqCategoryTranslationResult>> ExecuteAsync(
        UpsertFaqCategoryTranslationInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var categoryExists = await _categories.AnyAsync(c => c.Id == input.CategoryId, ct).ConfigureAwait(false);
        if (!categoryExists)
        {
            return Result.Failure<UpsertFaqCategoryTranslationResult>(FaqErrors.CategoryNotFound);
        }

        var name = string.IsNullOrWhiteSpace(input.Name) ? null : _sanitizer.Sanitize(input.Name);

        var existing = await _translations
            .FirstOrDefaultAsync(t => t.FaqCategoryId == input.CategoryId && t.LanguageCode == input.LanguageCode, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            ConcurrencyGuard.EnsureExpectedRowVersion(existing.RowVersion, input.ExpectedRowVersion);
            existing.Name = name;
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            return Result.Success(new UpsertFaqCategoryTranslationResult(existing.Id));
        }

        ConcurrencyGuard.EnsureExpectedRowVersion(null, input.ExpectedRowVersion);

        var translation = new FaqCategoryTranslation
        {
            FaqCategoryId = input.CategoryId,
            LanguageCode = input.LanguageCode,
            Name = name,
        };
        _translations.Add(translation);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Failure<UpsertFaqCategoryTranslationResult>(FaqErrors.Conflict);
        }

        return Result.Success(new UpsertFaqCategoryTranslationResult(translation.Id));
    }
}

/// <summary>Validate upsert bản dịch danh mục.</summary>
public sealed class UpsertFaqCategoryTranslationValidator : AbstractValidator<UpsertFaqCategoryTranslationInput>
{
    public UpsertFaqCategoryTranslationValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.LanguageCode).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Name).MaximumLength(300).When(x => x.Name is not null);
    }
}
