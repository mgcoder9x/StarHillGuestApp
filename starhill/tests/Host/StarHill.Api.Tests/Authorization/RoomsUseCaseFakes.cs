using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using ResortConfig.Application;
using ResortConfig.Contracts.Queries;
using Rooms.Application;
using Rooms.Domain;

namespace StarHill.Api.Tests.Authorization;

// Fake use case + query cho guard auth Rooms: trả success KHÔNG chạm DB → cô lập test ở tầng routing/authorization
// (đường Admin-OK vẫn cần handler chạy tới cùng; fake giúp verify KHÔNG cần Postgres/Docker).

internal sealed class FakeCreateRoom : IUseCase<CreateRoomInput, CreateRoomResult>
{
    public static readonly Guid RoomId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Task<Result<CreateRoomResult>> ExecuteAsync(CreateRoomInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new CreateRoomResult(RoomId, "raw-secret-token", "raw-***")));
}

internal sealed class FakeRotateToken : IUseCase<RotateRoomTokenInput, RotateRoomTokenResult>
{
    public Task<Result<RotateRoomTokenResult>> ExecuteAsync(RotateRoomTokenInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new RotateRoomTokenResult("raw-secret-token", "raw-***")));
}

internal sealed class FakeRenderQrPng : IUseCase<RenderRoomQrPngInput, RenderRoomQrPngResult>
{
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47]; // "‰PNG" magic (đủ để assert content).

    public Task<Result<RenderRoomQrPngResult>> ExecuteAsync(RenderRoomQrPngInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new RenderRoomQrPngResult(Png)));
}

internal sealed class FakeUpdateRoom : ICommandUseCase<UpdateRoomInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(UpdateRoomInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeChangeStatus : ICommandUseCase<ChangeRoomStatusInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(ChangeRoomStatusInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeDeleteRoom : ICommandUseCase<Guid>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(Guid input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeRoomQueries : IRoomQueries
{
    public static readonly Guid KnownRoomId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static RoomListItem Item(Guid id) =>
        new(id, "A-101", "A", 1, RoomStatus.Active, "abc12…", 1, DateTimeOffset.UnixEpoch);

    public Task<PagedResult<RoomListItem>> ListAsync(RoomStatus? status, PagedRequest paging, CancellationToken ct = default) =>
        Task.FromResult(new PagedResult<RoomListItem>(new List<RoomListItem> { Item(KnownRoomId) }, paging.SafePage, paging.SafePageSize, 1));

    public Task<RoomListItem?> GetByIdAsync(Guid roomId, CancellationToken ct = default) =>
        Task.FromResult<RoomListItem?>(roomId == KnownRoomId ? Item(roomId) : null);
}

internal sealed class FakeUpdateResortSettings : ICommandUseCase<UpdateResortSettingsInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(UpdateResortSettingsInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

// ---- Rules admin use case fakes (D-Rules.4c-1 endpoint auth guard, KHÔNG DB) ----

internal sealed class FakeCreateRuleSection : IUseCase<Rules.Application.CreateRuleSectionInput, Rules.Application.CreateRuleSectionResult>
{
    public static readonly Guid SectionId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    public Task<Result<Rules.Application.CreateRuleSectionResult>> ExecuteAsync(
        Rules.Application.CreateRuleSectionInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Rules.Application.CreateRuleSectionResult(SectionId)));
}

internal sealed class FakeUpdateRuleSection : ICommandUseCase<Rules.Application.UpdateRuleSectionInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(Rules.Application.UpdateRuleSectionInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeDeleteRuleSection : ICommandUseCase<Rules.Application.DeleteRuleSectionInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(Rules.Application.DeleteRuleSectionInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeUpsertRuleTranslation
    : IUseCase<Rules.Application.UpsertRuleSectionTranslationInput, Rules.Application.UpsertRuleSectionTranslationResult>
{
    public static readonly Guid TranslationId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    public Task<Result<Rules.Application.UpsertRuleSectionTranslationResult>> ExecuteAsync(
        Rules.Application.UpsertRuleSectionTranslationInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Rules.Application.UpsertRuleSectionTranslationResult(TranslationId)));
}

internal sealed class FakePublishRules : IUseCase<Rules.Application.PublishRulesInput, Rules.Application.PublishRulesResult>
{
    public static readonly Guid PublicationId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    public Task<Result<Rules.Application.PublishRulesResult>> ExecuteAsync(
        Rules.Application.PublishRulesInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Rules.Application.PublishRulesResult(PublicationId, 1)));
}

internal sealed class FakeGetDraftPreview
    : IUseCase<Rules.Application.GetDraftPreviewInput, Rules.Application.GetDraftPreviewResult>
{
    public Task<Result<Rules.Application.GetDraftPreviewResult>> ExecuteAsync(
        Rules.Application.GetDraftPreviewInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Rules.Application.GetDraftPreviewResult(
            "en",
            new List<Rules.Application.RenderedRuleSection>
            {
                new("welcome", 1, true, false, 0, "Welcome", "<p>hi</p>", "en", false, false),
            })));
}

internal sealed class FakeGetPublicationHistory
    : IUseCase<Rules.Application.GetPublicationHistoryInput, Rules.Application.GetPublicationHistoryResult>
{
    public Task<Result<Rules.Application.GetPublicationHistoryResult>> ExecuteAsync(
        Rules.Application.GetPublicationHistoryInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Rules.Application.GetPublicationHistoryResult(
            new List<Rules.Application.RulePublicationHistoryItem>
            {
                new(FakePublishRules.PublicationId, 1, DateTimeOffset.UnixEpoch, null, "first", true),
            })));
}

// ---- Rules guest fakes (D-Rules.4c-2 endpoint test, KHÔNG DB) ----

internal sealed class FakeCurrentGuestContextResolver : GuestAccess.Contracts.ICurrentGuestContextResolver
{
    public required Result<GuestAccess.Contracts.CurrentGuestContext> NextResult { get; set; }
    public string? LastSessionKey { get; private set; }
    public int TouchCount { get; private set; }

    public Task<Result<GuestAccess.Contracts.CurrentGuestContext>> ResolveAsync(
        string? sessionKey, Guid roomId, CancellationToken ct = default)
    {
        LastSessionKey = sessionKey;
        return Task.FromResult(NextResult);
    }

    public Task TouchAsync(Guid guestVisitId, CancellationToken ct = default)
    {
        TouchCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeGetCurrentRules : IUseCase<Rules.Application.GetCurrentRulesInput, Rules.Application.GetCurrentRulesResult>
{
    public static readonly Guid PublicationId = Guid.Parse("88888888-8888-8888-8888-888888888888");

    public Task<Result<Rules.Application.GetCurrentRulesResult>> ExecuteAsync(
        Rules.Application.GetCurrentRulesInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Rules.Application.GetCurrentRulesResult(
            PublicationId, 1, "en",
            new List<Rules.Application.RenderedRuleSection>
            {
                new("welcome", 1, true, false, 0, "Welcome", "<p>hi</p>", "en", false, false),
            })));
}

internal sealed class FakeAcknowledgeRules : IUseCase<Rules.Application.AcknowledgeRulesInput, Rules.Application.AcknowledgeRulesResult>
{
    public Task<Result<Rules.Application.AcknowledgeRulesResult>> ExecuteAsync(
        Rules.Application.AcknowledgeRulesInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Rules.Application.AcknowledgeRulesResult(FakeGetCurrentRules.PublicationId, 1, false)));
}

internal sealed class FakeResortSettingsQuery : IResortSettingsQuery
{
    public static readonly Guid ResortId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public Task<ResortSettingsSnapshot?> GetAsync(CancellationToken ct = default) =>
        Task.FromResult<ResortSettingsSnapshot?>(new ResortSettingsSnapshot(
            ResortId: ResortId,
            FaqEnabled: true,
            ChatEnabled: true,
            HousekeepingEnabled: true,
            RequireRuleAckForFaq: false,
            RequireRuleAckForChat: false,
            RequireRuleAckForHousekeeping: false,
            PortalWindowMinutes: 30,
            VisitIdleExpiryHours: 24,
            GuestWebBaseUrl: "https://guest.example.com",
            MaxMessageLength: 1000,
            MessageRateLimitPerMinute: 10,
            HousekeepingRateLimitPerHour: 5));
}

// ---- Faq admin + guest use case fakes (E-Faq.4 endpoint auth guard, KHÔNG DB) ----

internal sealed class FakeCreateFaqCategory : IUseCase<Faq.Application.CreateFaqCategoryInput, Faq.Application.CreateFaqCategoryResult>
{
    public static readonly Guid CategoryId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public Task<Result<Faq.Application.CreateFaqCategoryResult>> ExecuteAsync(
        Faq.Application.CreateFaqCategoryInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Faq.Application.CreateFaqCategoryResult(CategoryId)));
}

internal sealed class FakeUpdateFaqCategory : ICommandUseCase<Faq.Application.UpdateFaqCategoryInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(Faq.Application.UpdateFaqCategoryInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeDeleteFaqCategory : ICommandUseCase<Faq.Application.DeleteFaqCategoryInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(Faq.Application.DeleteFaqCategoryInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeUpsertFaqCategoryTranslation
    : IUseCase<Faq.Application.UpsertFaqCategoryTranslationInput, Faq.Application.UpsertFaqCategoryTranslationResult>
{
    public static readonly Guid TranslationId = Guid.Parse("aaaaaaa1-9999-9999-9999-999999999999");

    public Task<Result<Faq.Application.UpsertFaqCategoryTranslationResult>> ExecuteAsync(
        Faq.Application.UpsertFaqCategoryTranslationInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Faq.Application.UpsertFaqCategoryTranslationResult(TranslationId)));
}

internal sealed class FakeCreateFaqItem : IUseCase<Faq.Application.CreateFaqItemInput, Faq.Application.CreateFaqItemResult>
{
    public static readonly Guid ItemId = Guid.Parse("bbbbbbb1-9999-9999-9999-999999999999");

    public Task<Result<Faq.Application.CreateFaqItemResult>> ExecuteAsync(
        Faq.Application.CreateFaqItemInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Faq.Application.CreateFaqItemResult(ItemId)));
}

internal sealed class FakeUpdateFaqItem : ICommandUseCase<Faq.Application.UpdateFaqItemInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(Faq.Application.UpdateFaqItemInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeDeleteFaqItem : ICommandUseCase<Faq.Application.DeleteFaqItemInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(Faq.Application.DeleteFaqItemInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeUpsertFaqItemTranslation
    : IUseCase<Faq.Application.UpsertFaqItemTranslationInput, Faq.Application.UpsertFaqItemTranslationResult>
{
    public static readonly Guid TranslationId = Guid.Parse("ccccccc1-9999-9999-9999-999999999999");

    public Task<Result<Faq.Application.UpsertFaqItemTranslationResult>> ExecuteAsync(
        Faq.Application.UpsertFaqItemTranslationInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Faq.Application.UpsertFaqItemTranslationResult(TranslationId)));
}

internal sealed class FakeReorderFaqCategories : ICommandUseCase<Faq.Application.ReorderFaqCategoriesInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(Faq.Application.ReorderFaqCategoriesInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeReorderFaqItems : ICommandUseCase<Faq.Application.ReorderFaqItemsInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(Faq.Application.ReorderFaqItemsInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeGetGuestFaqTree : IUseCase<Faq.Application.GetGuestFaqTreeInput, Faq.Application.GetGuestFaqTreeResult>
{
    public Task<Result<Faq.Application.GetGuestFaqTreeResult>> ExecuteAsync(
        Faq.Application.GetGuestFaqTreeInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Faq.Application.GetGuestFaqTreeResult(
            "en",
            new List<Faq.Application.RenderedFaqCategory>
            {
                new(FakeCreateFaqCategory.CategoryId, "arrival", 1, "Arrival", "en", false, false,
                    new List<Faq.Application.RenderedFaqItem>
                    {
                        new(FakeCreateFaqItem.ItemId, 1, "Q", "<p>A</p>", "en", false, false, []),
                    }),
            })));
}

// ---- Housekeeping use case + reader fakes (H-Hk.3 endpoint auth guard, KHÔNG DB) ----

internal sealed class FakeRequestHousekeeping : IUseCase<Housekeeping.Application.RequestHousekeepingInput, Housekeeping.Application.RequestHousekeepingResult>
{
    public static readonly Guid TicketId = Guid.Parse("dddddddd-9999-9999-9999-999999999999");

    public Task<Result<Housekeeping.Application.RequestHousekeepingResult>> ExecuteAsync(
        Housekeeping.Application.RequestHousekeepingInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Housekeeping.Application.RequestHousekeepingResult(
            TicketId, Housekeeping.Domain.HousekeepingStatus.Requested, AlreadyOpen: false)));
}

internal sealed class FakeGetRoomHousekeepingStatus
    : IUseCase<Housekeeping.Application.GetRoomHousekeepingStatusInput, Housekeeping.Application.GetRoomHousekeepingStatusResult>
{
    public Task<Result<Housekeeping.Application.GetRoomHousekeepingStatusResult>> ExecuteAsync(
        Housekeeping.Application.GetRoomHousekeepingStatusInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Housekeeping.Application.GetRoomHousekeepingStatusResult(null)));
}

internal sealed class FakeSetHousekeepingStatus
    : IUseCase<Housekeeping.Application.SetHousekeepingStatusInput, Housekeeping.Application.HousekeepingTicketResult>
{
    public Task<Result<Housekeeping.Application.HousekeepingTicketResult>> ExecuteAsync(
        Housekeeping.Application.SetHousekeepingStatusInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Housekeeping.Application.HousekeepingTicketResult(
            FakeRequestHousekeeping.TicketId, input.NewStatus)));
}

internal sealed class FakeCompleteHousekeepingByRoom
    : IUseCase<Housekeeping.Application.CompleteHousekeepingByRoomInput, Housekeeping.Application.HousekeepingTicketResult>
{
    public Task<Result<Housekeeping.Application.HousekeepingTicketResult>> ExecuteAsync(
        Housekeeping.Application.CompleteHousekeepingByRoomInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Housekeeping.Application.HousekeepingTicketResult(
            FakeRequestHousekeeping.TicketId, Housekeeping.Domain.HousekeepingStatus.Done)));
}

internal sealed class FakeCompleteHousekeepingByToken
    : IUseCase<Housekeeping.Application.CompleteHousekeepingByTokenInput, Housekeeping.Application.HousekeepingTicketResult>
{
    public Task<Result<Housekeeping.Application.HousekeepingTicketResult>> ExecuteAsync(
        Housekeeping.Application.CompleteHousekeepingByTokenInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Housekeeping.Application.HousekeepingTicketResult(
            FakeRequestHousekeeping.TicketId, Housekeeping.Domain.HousekeepingStatus.Done)));
}

internal sealed class FakeCreateHousekeepingByStaff
    : IUseCase<Housekeeping.Application.CreateHousekeepingByStaffInput, Housekeeping.Application.RequestHousekeepingResult>
{
    public Task<Result<Housekeeping.Application.RequestHousekeepingResult>> ExecuteAsync(
        Housekeeping.Application.CreateHousekeepingByStaffInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Housekeeping.Application.RequestHousekeepingResult(
            FakeRequestHousekeeping.TicketId, Housekeeping.Domain.HousekeepingStatus.Requested, AlreadyOpen: false)));
}

internal sealed class FakeHousekeepingReader : Housekeeping.Application.IHousekeepingReader
{
    public Task<Housekeeping.Application.HousekeepingTicketView?> GetCurrentTicketByRoomAsync(Guid roomId, CancellationToken ct = default) =>
        Task.FromResult<Housekeeping.Application.HousekeepingTicketView?>(null);

    public Task<IReadOnlyList<Guid>> ListOpenTicketIdsByVisitAsync(Guid guestVisitId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Guid>>([]);

    public Task<Bedrock.Application.UseCases.PagedResult<Housekeeping.Application.HousekeepingBoardItem>> ListBoardAsync(
        Guid resortId, Housekeeping.Domain.HousekeepingStatus? status, Bedrock.Application.UseCases.PagedRequest paging, CancellationToken ct = default) =>
        Task.FromResult(new Bedrock.Application.UseCases.PagedResult<Housekeeping.Application.HousekeepingBoardItem>(
            [], paging.SafePage, paging.SafePageSize, 0));
}

// ---- Concierge use case + reader fakes (K-Con.3 endpoint auth guard, KHÔNG DB) ----

internal sealed class FakeSendGuestMessage
    : IUseCase<Concierge.Application.SendGuestMessageInput, Concierge.Application.SendGuestMessageResult>
{
    public static readonly Guid ConversationId = Guid.Parse("eeeeeeee-9999-9999-9999-999999999999");
    public static readonly Guid MessageId = Guid.Parse("ffffffff-9999-9999-9999-999999999999");

    public Task<Result<Concierge.Application.SendGuestMessageResult>> ExecuteAsync(
        Concierge.Application.SendGuestMessageInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Concierge.Application.SendGuestMessageResult(
            ConversationId, MessageId, Concierge.Domain.ConversationStatus.Open, Reopened: false)));
}

internal sealed class FakeGetGuestConversation
    : IUseCase<Concierge.Application.GetGuestConversationInput, Concierge.Application.GetGuestConversationResult>
{
    public Task<Result<Concierge.Application.GetGuestConversationResult>> ExecuteAsync(
        Concierge.Application.GetGuestConversationInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Concierge.Application.GetGuestConversationResult(null)));
}

internal sealed class FakeReplyConversation
    : IUseCase<Concierge.Application.ReplyConversationInput, Concierge.Application.ReplyConversationResult>
{
    public Task<Result<Concierge.Application.ReplyConversationResult>> ExecuteAsync(
        Concierge.Application.ReplyConversationInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Concierge.Application.ReplyConversationResult(
            FakeSendGuestMessage.MessageId, Concierge.Domain.ConversationStatus.Open)));
}

internal sealed class FakeMarkConversationRead : ICommandUseCase<Concierge.Application.MarkConversationReadInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(Concierge.Application.MarkConversationReadInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeCloseConversation : ICommandUseCase<Concierge.Application.CloseConversationInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(Concierge.Application.CloseConversationInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeCreateInternalNote
    : IUseCase<Concierge.Application.CreateInternalNoteInput, Concierge.Application.CreateInternalNoteResult>
{
    public static readonly Guid NoteId = Guid.Parse("abababab-9999-9999-9999-999999999999");

    public Task<Result<Concierge.Application.CreateInternalNoteResult>> ExecuteAsync(
        Concierge.Application.CreateInternalNoteInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new Concierge.Application.CreateInternalNoteResult(NoteId)));
}

internal sealed class FakeUpdateInternalNote : ICommandUseCase<Concierge.Application.UpdateInternalNoteInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(Concierge.Application.UpdateInternalNoteInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeDeleteInternalNote : ICommandUseCase<Concierge.Application.DeleteInternalNoteInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(Concierge.Application.DeleteInternalNoteInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeConciergeReader : Concierge.Application.IConciergeReader
{
    public Task<Concierge.Application.GuestConversationView?> GetGuestConversationByVisitAsync(
        Guid guestVisitId, CancellationToken ct = default) =>
        Task.FromResult<Concierge.Application.GuestConversationView?>(null);

    public Task<IReadOnlyList<Guid>> ListMessageIdsUnreadByGuestAsync(Guid conversationId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Guid>>([]);

    public Task<IReadOnlyList<Guid>> ListMessageIdsUnreadByStaffAsync(Guid conversationId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Guid>>([]);

    public Task<Concierge.Application.PagedConversations> ListConversationsAsync(
        Guid resortId, Concierge.Domain.ConversationStatus? status, int page, int pageSize, CancellationToken ct = default) =>
        Task.FromResult(new Concierge.Application.PagedConversations([], page, pageSize, 0));

    public Task<Concierge.Application.ConversationDetailView?> GetConversationAsync(
        Guid conversationId, CancellationToken ct = default) =>
        Task.FromResult<Concierge.Application.ConversationDetailView?>(
            new Concierge.Application.ConversationDetailView(
                conversationId, Guid.CreateVersion7(), Guid.CreateVersion7(),
                Concierge.Domain.ConversationStatus.Open, DateTimeOffset.UnixEpoch, 0, []));
}
