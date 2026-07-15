using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using ResortConfig.Contracts.Localization;
using ResortConfig.Contracts.Queries;
using Rules.Contracts;

namespace Faq.Application;

/// <summary>
/// Guest đọc CÂY FAQ active theo ngôn ngữ (Req 4.2/4.3/4.4). Read-only → <see cref="IUseCase{TInput,TOutput}"/> không
/// transaction. Thứ tự (fail-closed + gate TRƯỚC nội dung — bản chất CP3):
/// <list type="number">
/// <item>Cấu hình nền qua <see cref="IResortGuestConfigQuery"/> — thiếu → <c>configuration_unavailable</c>.</item>
/// <item><c>FaqEnabled=false</c> → <c>faq_disabled</c> (backend enforce feature-flag — Req 14, không dựa frontend).</item>
/// <item><b>RULE-GATE</b> <see cref="IRuleGate.EnsureAcknowledgedAsync"/> với <see cref="GuestFeature.Faq"/> (CP3 —
/// Faq là consumer ĐẦU TIÊN của rule-gate). Đặt gate TRONG use case = defense-in-depth: mọi caller (endpoint hiện
/// tại + tương lai) đều bị gate, không thể quên. Chưa ack (khi cấu hình yêu cầu) → <c>rule_ack_required</c> (403).</item>
/// <item>Chọn ngôn ngữ hiển thị (<see cref="ITranslationResolver.MatchSupported"/>) + render mỗi node (fallback CP5).</item>
/// <item>Dựng cây: category (SortOrder) → item gốc (ParentId==null, SortOrder) → con đệ quy (in-memory).</item>
/// </list>
/// </summary>
public sealed class GetGuestFaqTreeUseCase : IUseCase<GetGuestFaqTreeInput, GetGuestFaqTreeResult>
{
    private readonly IFaqReader _reader;
    private readonly IResortGuestConfigQuery _configQuery;
    private readonly ITranslationResolver _translationResolver;
    private readonly IRuleGate _ruleGate;

    public GetGuestFaqTreeUseCase(
        IFaqReader reader,
        IResortGuestConfigQuery configQuery,
        ITranslationResolver translationResolver,
        IRuleGate ruleGate)
    {
        _reader = reader;
        _configQuery = configQuery;
        _translationResolver = translationResolver;
        _ruleGate = ruleGate;
    }

    public async Task<Result<GetGuestFaqTreeResult>> ExecuteAsync(
        GetGuestFaqTreeInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var config = await _configQuery.GetAsync(input.ResortId, ct).ConfigureAwait(false);
        if (config is null)
        {
            return Result.Failure<GetGuestFaqTreeResult>(FaqErrors.ConfigurationUnavailable);
        }

        if (!config.FaqEnabled)
        {
            return Result.Failure<GetGuestFaqTreeResult>(FaqErrors.Disabled);
        }

        // CP3 rule-gate (Faq là consumer đầu tiên): chặn TRƯỚC khi lộ nội dung nếu cấu hình yêu cầu ack mà chưa ack.
        var gate = await _ruleGate
            .EnsureAcknowledgedAsync(input.ResortId, input.GuestVisitId, GuestFeature.Faq, ct)
            .ConfigureAwait(false);
        if (gate.IsFailure)
        {
            return Result.Failure<GetGuestFaqTreeResult>(gate.Error);
        }

        var language = _translationResolver.MatchSupported(
            input.RequestedLanguage, config.EnabledLanguageCodes, config.DefaultLanguageCode);

        var snapshot = await _reader.LoadActiveTreeAsync(input.ResortId, ct).ConfigureAwait(false);

        // Nhóm item theo ParentId để dựng flow cha-con (in-memory). ToLookup cho phép key null (ParentId? — item gốc);
        // order nguồn theo SortOrder rồi Id TRƯỚC → mỗi nhóm lookup đã theo thứ tự xác định (tiebreak).
        var itemsByParent = snapshot.Items
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .ToLookup(i => i.ParentId);

        var categories = new List<RenderedFaqCategory>(snapshot.Categories.Count);
        foreach (var category in snapshot.Categories.OrderBy(c => c.SortOrder).ThenBy(c => c.Id))
        {
            var resolved = _translationResolver.Resolve(category.Translations, language, config.DefaultLanguageCode);
            var rootItems = BuildItems(category.Id, parentId: null, itemsByParent, language, config.DefaultLanguageCode);

            categories.Add(new RenderedFaqCategory(
                category.Id,
                category.Key,
                category.SortOrder,
                resolved.Value?.Name,
                resolved.IsMissing ? language : resolved.ResolvedLanguage,
                resolved.IsFallback,
                resolved.IsMissing,
                rootItems));
        }

        return Result.Success(new GetGuestFaqTreeResult(language, categories));
    }

    private List<RenderedFaqItem> BuildItems(
        Guid categoryId,
        Guid? parentId,
        ILookup<Guid?, FaqItemSnapshot> itemsByParent,
        string language,
        string defaultLanguage)
    {
        var rendered = new List<RenderedFaqItem>();
        foreach (var item in itemsByParent[parentId]) // rỗng nếu không có; đã theo thứ tự SortOrder/Id.
        {
            // Chỉ item CÙNG category (parentId==null gom mọi root của mọi category → lọc theo category).
            if (item.CategoryId != categoryId)
            {
                continue;
            }

            var resolved = _translationResolver.Resolve(item.Translations, language, defaultLanguage);
            var subChildren = BuildItems(categoryId, item.Id, itemsByParent, language, defaultLanguage);

            rendered.Add(new RenderedFaqItem(
                item.Id,
                item.SortOrder,
                resolved.Value?.Question,
                resolved.Value?.AnswerHtmlSanitized,
                resolved.IsMissing ? language : resolved.ResolvedLanguage,
                resolved.IsFallback,
                resolved.IsMissing,
                subChildren));
        }

        return rendered;
    }
}
