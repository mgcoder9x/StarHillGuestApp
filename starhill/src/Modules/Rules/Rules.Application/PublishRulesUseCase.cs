using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using FluentValidation;
using Rules.Domain;

namespace Rules.Application;

/// <summary>
/// Phát hành Draft → snapshot bất biến <see cref="RulePublication"/> mới (CP4 — QR-AD-030). Khách CHỈ đọc từ
/// publication <c>IsCurrent</c>; sửa Draft sau đó KHÔNG đụng snapshot đã publish (đã copy đông cứng). Đúng MỘT
/// publication IsCurrent/resort (partial unique <c>ux_rule_publication_current</c>).
/// <para>
/// <b>FLIP-BEFORE-INSERT (bản chất — QR-AD-036, cùng lớp QR-N-018/QR-AD-026):</b> partial unique kiểm tại mỗi
/// row-write (không deferrable) → nếu INSERT bản mới <c>IsCurrent=true</c> TRƯỚC khi hạ bản cũ thì hai hàng cùng
/// thỏa filter tại thời điểm insert → 23505. Vì vậy: hạ <c>IsCurrent=false</c> bản hiện hành + <b>SaveChanges TRƯỚC</b>
/// (flush UPDATE xuống DB), RỒI insert publication mới + copy section/translation, SaveChanges. Cả hai bước nằm trong
/// <see cref="IUnitOfWork.ExecuteInTransactionAsync"/> → nguyên tử (crash giữa chừng = rollback, không mất publication
/// hiện hành). <see cref="RulePublication.Version"/> = <c>(current?.Version ?? 0) + 1</c>: bản hiện hành LUÔN là
/// version cao nhất (mỗi publish tăng dần + demote bản trước) nên không cần truy vấn MAX riêng.
/// </para>
/// Là <see cref="IUseCase{TInput,TOutput}"/> (value-returning) tự quản transaction hẹp (mirror <c>ResolveTokenUseCase</c>) —
/// KHÔNG <c>ICommandUseCase</c> vì cần hai SaveChanges có kiểm soát thứ tự trong một transaction.
/// </summary>
public sealed class PublishRulesUseCase : IUseCase<PublishRulesInput, PublishRulesResult>
{
    private readonly IRuleDraftReader _draftReader;
    private readonly IRepository<RulePublication> _publications;
    private readonly IRepository<RulePublicationSection> _publicationSections;
    private readonly IRepository<RulePublicationSectionTranslation> _publicationTranslations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PublishRulesUseCase(
        IRuleDraftReader draftReader,
        IRepository<RulePublication> publications,
        IRepository<RulePublicationSection> publicationSections,
        IRepository<RulePublicationSectionTranslation> publicationTranslations,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _draftReader = draftReader;
        _publications = publications;
        _publicationSections = publicationSections;
        _publicationTranslations = publicationTranslations;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<PublishRulesResult>> ExecuteAsync(PublishRulesInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Đọc Draft (no-tracking, NGOÀI transaction — chỉ đọc, không giữ khóa). Rỗng → từ chối (không publish rỗng).
        var draft = await _draftReader.LoadDraftAsync(input.ResortId, ct).ConfigureAwait(false);
        if (draft is null || draft.Sections.Count == 0)
        {
            return Result.Failure<PublishRulesResult>(RulesErrors.NoPublishableContent);
        }

        var now = _clock.UtcNow;

        var result = await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                // (1) Hạ bản hiện hành (nếu có) + FLUSH TRƯỚC để giải phóng slot partial-unique trước khi insert.
                var current = await _publications
                    .FirstOrDefaultAsync(p => p.ResortId == input.ResortId && p.IsCurrent, token)
                    .ConfigureAwait(false);

                var nextVersion = (current?.Version ?? 0) + 1;

                if (current is not null)
                {
                    current.IsCurrent = false;
                    await _unitOfWork.SaveChangesAsync(token).ConfigureAwait(false);
                }

                // (2) Insert publication mới + copy đông cứng section/translation (đã sanitize ở Draft — CP12).
                var publication = new RulePublication
                {
                    ResortId = input.ResortId,
                    Version = nextVersion,
                    PublishedAt = now,
                    PublishedByUserId = input.PublishedByUserId,
                    ChangeNote = input.ChangeNote,
                    IsCurrent = true,
                };
                _publications.Add(publication);

                foreach (var section in draft.Sections)
                {
                    var pubSection = new RulePublicationSection
                    {
                        RulePublicationId = publication.Id,
                        Key = section.Key,
                        SortOrder = section.SortOrder,
                        IsRequired = section.IsRequired,
                        RequireScrollEnd = section.RequireScrollEnd,
                        MinReadSeconds = section.MinReadSeconds,
                    };
                    _publicationSections.Add(pubSection);

                    foreach (var translation in section.Translations)
                    {
                        _publicationTranslations.Add(new RulePublicationSectionTranslation
                        {
                            RulePublicationSectionId = pubSection.Id,
                            LanguageCode = translation.LanguageCode,
                            Title = translation.Title,
                            BodyHtmlSanitized = translation.BodyHtmlSanitized,
                        });
                    }
                }

                await _unitOfWork.SaveChangesAsync(token).ConfigureAwait(false);
                return new PublishRulesResult(publication.Id, nextVersion);
            },
            ct).ConfigureAwait(false);

        return Result.Success(result);
    }
}

/// <summary>Validate publish (ChangeNote ≤ 1000 — khớp cột; ResortId bắt buộc).</summary>
public sealed class PublishRulesValidator : AbstractValidator<PublishRulesInput>
{
    public PublishRulesValidator()
    {
        RuleFor(x => x.ResortId).NotEmpty();
        RuleFor(x => x.ChangeNote).MaximumLength(1000).When(x => x.ChangeNote is not null);
    }
}
