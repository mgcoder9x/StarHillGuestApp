using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using ResortConfig.Contracts.Localization;
using ResortConfig.Contracts.Queries;

namespace Rules.Application;

/// <summary>
/// Guest đọc bản nội quy HIỆN HÀNH (publication IsCurrent) theo ngôn ngữ mong muốn với fallback (CP5/CP4 — QR-AD-030).
/// KHÔNG đọc Draft (chỉ snapshot đã publish — bất biến). Read-only → <see cref="IUseCase{TInput,TOutput}"/> không mở
/// transaction (không ghi).
/// <para>
/// Luồng: (1) cấu hình nền (enabled languages + default) qua <see cref="IResortGuestConfigQuery"/> — thiếu →
/// <c>configuration_unavailable</c> (fail-closed, nhất quán resolve QR-AD-024); (2) publication IsCurrent qua
/// <see cref="IRulePublicationReader"/> — chưa publish → <c>rules_unavailable</c>; (3) chọn ngôn ngữ hiển thị bằng
/// <see cref="ITranslationResolver.MatchSupported"/>; (4) mỗi section resolve bản dịch (requested → default fallback →
/// missing) bằng <see cref="ITranslationResolver.Resolve{T}"/>.
/// </para>
/// </summary>
public sealed class GetCurrentRulesUseCase : IUseCase<GetCurrentRulesInput, GetCurrentRulesResult>
{
    private readonly IRulePublicationReader _reader;
    private readonly IResortGuestConfigQuery _configQuery;
    private readonly ITranslationResolver _translationResolver;

    public GetCurrentRulesUseCase(
        IRulePublicationReader reader,
        IResortGuestConfigQuery configQuery,
        ITranslationResolver translationResolver)
    {
        _reader = reader;
        _configQuery = configQuery;
        _translationResolver = translationResolver;
    }

    public async Task<Result<GetCurrentRulesResult>> ExecuteAsync(
        GetCurrentRulesInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var config = await _configQuery.GetAsync(input.ResortId, ct).ConfigureAwait(false);
        if (config is null)
        {
            return Result.Failure<GetCurrentRulesResult>(RulesErrors.ConfigurationUnavailable);
        }

        var snapshot = await _reader.LoadCurrentAsync(input.ResortId, ct).ConfigureAwait(false);
        if (snapshot is null)
        {
            return Result.Failure<GetCurrentRulesResult>(RulesErrors.RulesUnavailable);
        }

        var language = _translationResolver.MatchSupported(
            input.RequestedLanguage, config.EnabledLanguageCodes, config.DefaultLanguageCode);

        var sections = new List<RenderedRuleSection>(snapshot.Sections.Count);
        foreach (var section in snapshot.Sections)
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

        return Result.Success(new GetCurrentRulesResult(snapshot.PublicationId, snapshot.Version, language, sections));
    }
}
