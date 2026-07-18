using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using ResortConfig.Contracts.Localization;
using ResortConfig.Contracts.Queries;

namespace Faq.Application;

public sealed record GetFaqAdminTreeInput(Guid ResortId);

public sealed record GetFaqAdminTreeResult(
    IReadOnlyList<string> EnabledLanguageCodes,
    string DefaultLanguageCode,
    IReadOnlyList<FaqAdminCategoryItem> Categories);

public sealed record FaqAdminCategoryItem(
    Guid CategoryId,
    uint RowVersion,
    string Key,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<string> MissingLanguages,
    IReadOnlyList<FaqAdminCategoryTranslationItem> Translations,
    IReadOnlyList<FaqAdminItemNode> Items);

public sealed record FaqAdminCategoryTranslationItem(
    Guid TranslationId,
    uint RowVersion,
    string LanguageCode,
    string? Name);

public sealed record FaqAdminItemNode(
    Guid ItemId,
    uint RowVersion,
    Guid CategoryId,
    Guid? ParentId,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<string> MissingLanguages,
    IReadOnlyList<FaqAdminItemTranslationItem> Translations,
    IReadOnlyList<FaqAdminItemNode> Children);

public sealed record FaqAdminItemTranslationItem(
    Guid TranslationId,
    uint RowVersion,
    string LanguageCode,
    string? Question,
    string? AnswerHtmlSanitized);

/// <summary>Full admin tree, không active-filter và không resolve về một ngôn ngữ.</summary>
public sealed class GetFaqAdminTreeUseCase(
    IFaqAdminReader reader,
    IResortGuestConfigQuery configQuery,
    ITranslationResolver translationResolver)
    : IUseCase<GetFaqAdminTreeInput, GetFaqAdminTreeResult>
{
    public async Task<Result<GetFaqAdminTreeResult>> ExecuteAsync(
        GetFaqAdminTreeInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var config = await configQuery.GetAsync(input.ResortId, ct).ConfigureAwait(false);
        if (config is null)
        {
            return Result.Failure<GetFaqAdminTreeResult>(FaqErrors.ConfigurationUnavailable);
        }

        var snapshot = await reader.LoadAsync(input.ResortId, ct).ConfigureAwait(false);
        var itemsByParent = snapshot.Items.ToLookup(item => item.ParentId);

        FaqAdminItemNode BuildItem(FaqAdminItemSnapshot item, HashSet<Guid> path)
        {
            if (!path.Add(item.ItemId))
            {
                throw new InvalidOperationException("FAQ admin tree contains a cycle.");
            }

            var children = itemsByParent[item.ItemId]
                .Where(child => child.CategoryId == item.CategoryId)
                .Select(child => BuildItem(child, path))
                .ToList();
            path.Remove(item.ItemId);

            return new FaqAdminItemNode(
                item.ItemId,
                item.RowVersion,
                item.CategoryId,
                item.ParentId,
                item.SortOrder,
                item.IsActive,
                translationResolver.MissingLanguages(item.Translations, config.EnabledLanguageCodes),
                item.Translations.Select(translation => new FaqAdminItemTranslationItem(
                    translation.TranslationId,
                    translation.RowVersion,
                    translation.LanguageCode,
                    translation.Question,
                    translation.AnswerHtmlSanitized)).ToList(),
                children);
        }

        var categories = snapshot.Categories.Select(category => new FaqAdminCategoryItem(
            category.CategoryId,
            category.RowVersion,
            category.Key,
            category.SortOrder,
            category.IsActive,
            translationResolver.MissingLanguages(category.Translations, config.EnabledLanguageCodes),
            category.Translations.Select(translation => new FaqAdminCategoryTranslationItem(
                translation.TranslationId,
                translation.RowVersion,
                translation.LanguageCode,
                translation.Name)).ToList(),
            itemsByParent[null]
                .Where(item => item.CategoryId == category.CategoryId)
                .Select(item => BuildItem(item, []))
                .ToList())).ToList();

        return Result.Success(new GetFaqAdminTreeResult(
            config.EnabledLanguageCodes,
            config.DefaultLanguageCode,
            categories));
    }
}
