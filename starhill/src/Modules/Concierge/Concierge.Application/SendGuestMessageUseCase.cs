using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Concierge.Domain;
using ResortConfig.Contracts.Queries;
using Rules.Contracts;

namespace Concierge.Application;

/// <summary>
/// Khách gửi tin (Req 5.1/5.2/5.10). Read-write value-returning <see cref="IUseCase{TInput,TOutput}"/> tự quản
/// SaveChanges (mirror RequestHousekeeping). Thứ tự (fail-closed + gate TRƯỚC ghi — CP3):
/// <list type="number">
/// <item>Config qua <see cref="IResortGuestConfigQuery"/> — thiếu → <c>configuration_unavailable</c>.</item>
/// <item><c>ChatEnabled=false</c> → <c>chat_disabled</c> (backend enforce feature-flag — Req 14).</item>
/// <item>RULE-GATE <see cref="IRuleGate.EnsureAcknowledgedAsync"/> với <see cref="GuestFeature.Chat"/> (CP3, defense-in-depth).</item>
/// <item>Validate body: trim; rỗng → <c>concierge_message_empty</c>; dài quá <c>MaxMessageLength</c> (đọc
/// <see cref="IResortSettingsQuery"/>) → <c>concierge_message_too_long</c>. Body lưu PLAIN TEXT — KHÔNG sanitize (khác Rules/FAQ).</item>
/// <item>FIND-OR-CREATE/REOPEN hội thoại của visit (unique <c>ux_conversation_visit</c> — 1 hội thoại/visit, Req 5.2):
/// chưa có → tạo Open; đã có (kể cả Closed mà visit còn hiệu lực) → append + mở lại (reopen). Append tin(Guest) +
/// <c>UnreadForStaff++</c> + cập nhật mốc thời gian.</item>
/// </list>
/// Race tạo hội thoại ĐẦU cho visit → bắt <see cref="UniqueConstraintViolationException"/>: gỡ track bản vừa tạo,
/// nạp hội thoại đã tồn tại rồi append (KHÔNG mất tin của khách). Sau persist → <see cref="IConciergeRealtimeNotifier"/>
/// báo nhóm lễ tân (realtime chỉ tăng tốc; polling luôn là fallback).
/// </summary>
public sealed class SendGuestMessageUseCase : IUseCase<SendGuestMessageInput, SendGuestMessageResult>
{
    private readonly IResortGuestConfigQuery _configQuery;
    private readonly IRuleGate _ruleGate;
    private readonly IResortSettingsQuery _settingsQuery;
    private readonly IRepository<Conversation> _conversations;
    private readonly IRepository<Message> _messages;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IConciergeRealtimeNotifier _notifier;

    public SendGuestMessageUseCase(
        IResortGuestConfigQuery configQuery,
        IRuleGate ruleGate,
        IResortSettingsQuery settingsQuery,
        IRepository<Conversation> conversations,
        IRepository<Message> messages,
        IUnitOfWork unitOfWork,
        IClock clock,
        IConciergeRealtimeNotifier notifier)
    {
        _configQuery = configQuery;
        _ruleGate = ruleGate;
        _settingsQuery = settingsQuery;
        _conversations = conversations;
        _messages = messages;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notifier = notifier;
    }

    public async Task<Result<SendGuestMessageResult>> ExecuteAsync(
        SendGuestMessageInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var config = await _configQuery.GetAsync(input.ResortId, ct).ConfigureAwait(false);
        if (config is null)
        {
            return Result.Failure<SendGuestMessageResult>(ConciergeErrors.ConfigurationUnavailable);
        }

        if (!config.ChatEnabled)
        {
            return Result.Failure<SendGuestMessageResult>(ConciergeErrors.ChatDisabled);
        }

        var gate = await _ruleGate
            .EnsureAcknowledgedAsync(input.ResortId, input.GuestVisitId, GuestFeature.Chat, ct)
            .ConfigureAwait(false);
        if (gate.IsFailure)
        {
            return Result.Failure<SendGuestMessageResult>(gate.Error);
        }

        var settings = await _settingsQuery.GetAsync(ct).ConfigureAwait(false);
        if (settings is null)
        {
            return Result.Failure<SendGuestMessageResult>(ConciergeErrors.ConfigurationUnavailable);
        }

        // Body PLAIN TEXT: trim (chuẩn hoá khoảng trắng đầu/cuối) — KHÔNG HTML-sanitize (chat là text; client render textContent).
        var body = (input.Body ?? string.Empty).Trim();
        if (body.Length == 0)
        {
            return Result.Failure<SendGuestMessageResult>(ConciergeErrors.MessageEmpty);
        }

        if (body.Length > settings.MaxMessageLength)
        {
            return Result.Failure<SendGuestMessageResult>(ConciergeErrors.MessageTooLong);
        }

        var now = _clock.UtcNow;

        var existing = await _conversations
            .FirstOrDefaultAsync(c => c.GuestVisitId == input.GuestVisitId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            var msg = AppendGuestMessage(existing, body, now, out var reopened);
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            await NotifyAsync(input.ResortId, existing.Id, msg.Id, ct).ConfigureAwait(false);
            return Result.Success(new SendGuestMessageResult(existing.Id, msg.Id, existing.Status, reopened));
        }

        // Chưa có hội thoại cho visit → tạo mới (Open) + tin đầu tiên (nguyên tử một SaveChanges).
        var conversation = new Conversation
        {
            ResortId = input.ResortId,
            RoomId = input.RoomId,
            GuestSessionId = input.GuestSessionId,
            GuestVisitId = input.GuestVisitId,
            Status = ConversationStatus.Open,
            LastMessageAt = now,
            LastGuestMessageAt = now,
            UnreadForStaff = 1,
            CreatedAt = now,
        };
        _conversations.Add(conversation);
        var firstMessage = new Message
        {
            ConversationId = conversation.Id,
            SenderType = MessageSenderType.Guest,
            Body = body,
            CreatedAt = now,
        };
        _messages.Add(firstMessage);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            // Race: một request khác vừa tạo hội thoại cho visit này (unique ux_conversation_visit) → gỡ track bản
            // vừa tạo, nạp hội thoại đã tồn tại rồi append (KHÔNG mất tin của khách).
            _conversations.Remove(conversation);
            _messages.Remove(firstMessage);

            var raced = await _conversations
                .FirstOrDefaultAsync(c => c.GuestVisitId == input.GuestVisitId, ct)
                .ConfigureAwait(false);
            if (raced is null)
            {
                return Result.Failure<SendGuestMessageResult>(CommonErrors.Conflict());
            }

            var racedMsg = AppendGuestMessage(raced, body, now, out var racedReopened);
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            await NotifyAsync(input.ResortId, raced.Id, racedMsg.Id, ct).ConfigureAwait(false);
            return Result.Success(new SendGuestMessageResult(raced.Id, racedMsg.Id, raced.Status, racedReopened));
        }

        await NotifyAsync(input.ResortId, conversation.Id, firstMessage.Id, ct).ConfigureAwait(false);
        return Result.Success(new SendGuestMessageResult(conversation.Id, firstMessage.Id, conversation.Status, Reopened: false));
    }

    /// <summary>Append tin(Guest) vào hội thoại đang tồn tại: mở lại nếu đang Closed (visit còn hiệu lực — Req 5.10),
    /// tăng UnreadForStaff, cập nhật mốc thời gian. Trả <paramref name="reopened"/> = hội thoại vừa được mở lại.</summary>
    private Message AppendGuestMessage(Conversation conversation, string body, DateTimeOffset now, out bool reopened)
    {
        reopened = conversation.Status == ConversationStatus.Closed;
        if (reopened)
        {
            conversation.Status = ConversationStatus.Open;
            conversation.ClosedAt = null;
            conversation.ClosedByUserId = null;
        }

        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderType = MessageSenderType.Guest,
            Body = body,
            CreatedAt = now,
        };
        _messages.Add(message);

        conversation.UnreadForStaff += 1;
        conversation.LastMessageAt = now;
        conversation.LastGuestMessageAt = now;
        return message;
    }

    private Task NotifyAsync(Guid resortId, Guid conversationId, Guid messageId, CancellationToken ct) =>
        _notifier.NotifyMessageReceivedAsync(resortId, conversationId, messageId, ct);
}
