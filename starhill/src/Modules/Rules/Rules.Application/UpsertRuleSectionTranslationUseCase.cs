using Bedrock.Application.Ports.Html;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using FluentValidation;
using Rules.Domain;

namespace Rules.Application;

/// <summary>
/// Tạo/cập nhật bản dịch của một section Draft — điểm hiện thực SANITIZE-ON-SAVE (Req 8.6/11.4, CP12). Nội dung thô
/// (<c>Title</c>/<c>BodyHtml</c>) đi qua <see cref="IHtmlSanitizer.Sanitize"/> TRƯỚC khi lưu (cột
/// <c>BodyHtmlSanitized</c>) → khách không bao giờ nhận HTML chứa script; Publish chỉ COPY nội dung đã sạch (không
/// double-sanitize on-read — QR-TO-011). Sanitize CẢ Title (design §7 — chống XSS mọi bề mặt).
/// <para>
/// Upsert theo unique <c>(section, lang)</c>: có → cập nhật (xmin concurrency CP15); chưa → thêm mới, đua tạo cùng
/// ngôn ngữ hiếm → <see cref="UniqueConstraintViolationException"/> → <see cref="RulesErrors.TranslationLanguageTaken"/>.
/// Section không tồn tại → <see cref="RulesErrors.RuleSectionNotFound"/> (chặn trước, tránh FK lỗi thô).
/// </para>
/// </summary>
public sealed class UpsertRuleSectionTranslationUseCase
    : IUseCase<UpsertRuleSectionTranslationInput, UpsertRuleSectionTranslationResult>
{
    private readonly IRepository<RuleSection> _sections;
    private readonly IRepository<RuleSectionTranslation> _translations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHtmlSanitizer _sanitizer;

    public UpsertRuleSectionTranslationUseCase(
        IRepository<RuleSection> sections,
        IRepository<RuleSectionTranslation> translations,
        IUnitOfWork unitOfWork,
        IHtmlSanitizer sanitizer)
    {
        _sections = sections;
        _translations = translations;
        _unitOfWork = unitOfWork;
        _sanitizer = sanitizer;
    }

    public async Task<Result<UpsertRuleSectionTranslationResult>> ExecuteAsync(
        UpsertRuleSectionTranslationInput input,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var sectionExists = await _sections
            .AnyAsync(s => s.Id == input.SectionId, ct)
            .ConfigureAwait(false);
        if (!sectionExists)
        {
            return Result.Failure<UpsertRuleSectionTranslationResult>(RulesErrors.RuleSectionNotFound);
        }

        // SANITIZE-ON-SAVE (CP12): rỗng/null → null chuẩn hóa; ngược lại làm sạch qua adapter allowlist (Ganss).
        var title = Sanitize(input.Title);
        var body = Sanitize(input.BodyHtml);

        var existing = await _translations
            .FirstOrDefaultAsync(t => t.RuleSectionId == input.SectionId && t.LanguageCode == input.LanguageCode, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            ConcurrencyGuard.EnsureExpectedRowVersion(existing.RowVersion, input.ExpectedRowVersion);
            existing.Title = title;
            existing.BodyHtmlSanitized = body;
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            return Result.Success(new UpsertRuleSectionTranslationResult(existing.Id));
        }

        ConcurrencyGuard.EnsureExpectedRowVersion(null, input.ExpectedRowVersion);

        var translation = new RuleSectionTranslation
        {
            RuleSectionId = input.SectionId,
            LanguageCode = input.LanguageCode,
            Title = title,
            BodyHtmlSanitized = body,
        };
        _translations.Add(translation);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Failure<UpsertRuleSectionTranslationResult>(RulesErrors.TranslationLanguageTaken);
        }

        return Result.Success(new UpsertRuleSectionTranslationResult(translation.Id));
    }

    private string? Sanitize(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? null : _sanitizer.Sanitize(raw);
}

/// <summary>Validate upsert bản dịch (Title ≤ 300; LanguageCode bắt buộc/ngắn; Body không giới hạn — rich text).</summary>
public sealed class UpsertRuleSectionTranslationValidator : AbstractValidator<UpsertRuleSectionTranslationInput>
{
    public UpsertRuleSectionTranslationValidator()
    {
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.LanguageCode).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Title).MaximumLength(300).When(x => x.Title is not null);
    }
}
