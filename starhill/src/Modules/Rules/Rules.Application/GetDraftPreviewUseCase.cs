using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using ResortConfig.Contracts.Localization;
using ResortConfig.Contracts.Queries;

namespace Rules.Application;

/// <summary>
/// Admin/Staff xem trước bản Draft "NHƯ KHÁCH" (Req 8.4) — render theo ngôn ngữ chọn + fallback (CP5) TỪ Draft
/// (KHÔNG tạo publication, KHÔNG ảnh hưởng khách). Mirror <see cref="GetCurrentRulesUseCase"/> nhưng đọc
/// <see cref="IRuleDraftReader"/> thay vì publication reader. Read-only → <see cref="IUseCase{TInput,TOutput}"/> không transaction.
/// <para>
/// Fail-closed cấu hình nền thiếu → <c>configuration_unavailable</c> (nhất quán guest read). Draft chưa có (resort
/// chưa tạo section nào) → trả preview RỖNG (Success, sections=[]) — admin đang soạn, không phải lỗi (khác guest read
/// <c>rules_unavailable</c>: guest KHÔNG có gì để đọc, còn admin preview-của-draft-rỗng là hợp lệ).
/// </para>
/// </summary>
public sealed class GetDraftPreviewUseCase : IUseCase<GetDraftPreviewInput, GetDraftPreviewResult>
{
    private readonly IRuleDraftReader _draftReader;
    private readonly IResortGuestConfigQuery _configQuery;
    private readonly ITranslationResolver _translationResolver;

    public GetDraftPreviewUseCase(
        IRuleDraftReader draftReader,
        IResortGuestConfigQuery configQuery,
        ITranslationResolver translationResolver)
    {
        _draftReader = draftReader;
        _configQuery = configQuery;
        _translationResolver = translationResolver;
    }

    public async Task<Result<GetDraftPreviewResult>> ExecuteAsync(
        GetDraftPreviewInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var config = await _configQuery.GetAsync(input.ResortId, ct).ConfigureAwait(false);
        if (config is null)
        {
            return Result.Failure<GetDraftPreviewResult>(RulesErrors.ConfigurationUnavailable);
        }

        var language = _translationResolver.MatchSupported(
            input.RequestedLanguage, config.EnabledLanguageCodes, config.DefaultLanguageCode);

        var draft = await _draftReader.LoadDraftAsync(input.ResortId, ct).ConfigureAwait(false);
        if (draft is null)
        {
            // Draft rỗng → preview rỗng (admin đang soạn). KHÔNG phải lỗi.
            return Result.Success(new GetDraftPreviewResult(language, []));
        }

        var sections = new List<RenderedRuleSection>(draft.Sections.Count);
        foreach (var section in draft.Sections)
        {
            var resolved = _translationResolver.Resolve(section.Translations, language, config.DefaultLanguageCode);
            sections.Add(new RenderedRuleSection(
                section.Key,
                section.SortOrder,
                section.IsRequired,
                section.RequireScrollEnd,
                section.MinReadSeconds,
                resolved.Value?.Title,
                resolved.Value?.BodyHtmlSanitized,
                resolved.IsMissing ? language : resolved.ResolvedLanguage,
                resolved.IsFallback,
                resolved.IsMissing));
        }

        return Result.Success(new GetDraftPreviewResult(language, sections));
    }
}

/// <summary>
/// Admin/Staff xem lịch sử publication (Req 8.4) — metadata mọi bản đã phát hành (Version giảm dần). Read-only.
/// </summary>
public sealed class GetPublicationHistoryUseCase : IUseCase<GetPublicationHistoryInput, GetPublicationHistoryResult>
{
    private readonly IRulePublicationReader _reader;

    public GetPublicationHistoryUseCase(IRulePublicationReader reader) => _reader = reader;

    public async Task<Result<GetPublicationHistoryResult>> ExecuteAsync(
        GetPublicationHistoryInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var history = await _reader.ListHistoryAsync(input.ResortId, ct).ConfigureAwait(false);
        return Result.Success(new GetPublicationHistoryResult(history));
    }
}
