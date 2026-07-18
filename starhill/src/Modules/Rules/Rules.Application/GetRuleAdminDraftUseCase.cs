using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using ResortConfig.Contracts.Localization;
using ResortConfig.Contracts.Queries;

namespace Rules.Application;

public sealed record GetRuleAdminDraftInput(Guid ResortId);

public sealed record GetRuleAdminDraftResult(
    Guid? RuleSetId,
    uint? RowVersion,
    IReadOnlyList<string> EnabledLanguageCodes,
    string DefaultLanguageCode,
    IReadOnlyList<RuleAdminSectionItem> Sections);

public sealed record RuleAdminSectionItem(
    Guid SectionId,
    uint RowVersion,
    string Key,
    int SortOrder,
    bool IsRequired,
    bool RequireScrollEnd,
    int MinReadSeconds,
    IReadOnlyList<string> MissingLanguages,
    IReadOnlyList<RuleAdminTranslationItem> Translations);

public sealed record RuleAdminTranslationItem(
    Guid TranslationId,
    uint RowVersion,
    string LanguageCode,
    string? Title,
    string? BodyHtmlSanitized);

/// <summary>
/// Trả toàn bộ Draft cho editor: ID + xmin + mọi translation, không resolve về một ngôn ngữ như preview.
/// Cấu hình ngôn ngữ vẫn fail-closed để <c>MissingLanguages</c> có nghĩa và client không tự đoán language set.
/// </summary>
public sealed class GetRuleAdminDraftUseCase(
    IRuleAdminReader reader,
    IResortGuestConfigQuery configQuery,
    ITranslationResolver translationResolver)
    : IUseCase<GetRuleAdminDraftInput, GetRuleAdminDraftResult>
{
    public async Task<Result<GetRuleAdminDraftResult>> ExecuteAsync(
        GetRuleAdminDraftInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var config = await configQuery.GetAsync(input.ResortId, ct).ConfigureAwait(false);
        if (config is null)
        {
            return Result.Failure<GetRuleAdminDraftResult>(RulesErrors.ConfigurationUnavailable);
        }

        var draft = await reader.LoadAsync(input.ResortId, ct).ConfigureAwait(false);
        if (draft is null)
        {
            return Result.Success(new GetRuleAdminDraftResult(
                null, null, config.EnabledLanguageCodes, config.DefaultLanguageCode, []));
        }

        var sections = draft.Sections.Select(section => new RuleAdminSectionItem(
            section.SectionId,
            section.RowVersion,
            section.Key,
            section.SortOrder,
            section.IsRequired,
            section.RequireScrollEnd,
            section.MinReadSeconds,
            translationResolver.MissingLanguages(section.Translations, config.EnabledLanguageCodes),
            section.Translations.Select(translation => new RuleAdminTranslationItem(
                translation.TranslationId,
                translation.RowVersion,
                translation.LanguageCode,
                translation.Title,
                translation.BodyHtmlSanitized)).ToList())).ToList();

        return Result.Success(new GetRuleAdminDraftResult(
            draft.RuleSetId,
            draft.RowVersion,
            config.EnabledLanguageCodes,
            config.DefaultLanguageCode,
            sections));
    }
}
