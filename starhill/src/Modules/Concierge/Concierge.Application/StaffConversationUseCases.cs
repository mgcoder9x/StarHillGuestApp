using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Concierge.Contracts;
using Concierge.Domain;
using FluentValidation;
using ResortConfig.Contracts.Queries;

namespace Concierge.Application;

/// <summary>
/// Nhân viên trả lời hội thoại (Req 5.3/5.10). Value-returning <see cref="IUseCase{TInput,TOutput}"/> tự quản
/// SaveChanges (mirror <see cref="SendGuestMessageUseCase"/> — write value-returning không dùng decorator transaction).
/// Actor <see cref="ReplyConversationInput.StaffUserId"/> do Api phân giải từ <c>ICurrentUser</c> (endpoint RequireStaff)
/// rồi truyền vào — KHÔNG tin client (mirror QR-DV-004). KHÔNG gate <c>ChatEnabled</c> (staff là hành động admin đã
/// xác thực — có thể tiếp tục đóng gói hội thoại kể cả khi chat tắt). Thứ tự: load hội thoại (null → <c>concierge_conversation_not_found</c>);
/// validate body (settings <see cref="IResortSettingsQuery"/> → <c>MaxMessageLength</c>; rỗng → <c>concierge_message_empty</c>;
/// dài → <c>concierge_message_too_long</c>; PLAIN TEXT không sanitize như guest); append <c>Message(Staff, SenderUserId=StaffUserId)</c>;
/// MỞ LẠI nếu đang Closed (staff chủ động tiếp tục — QR-TO); <c>LastStaffMessageAt/LastMessageAt=now</c>; KHÔNG tăng
/// <c>UnreadForStaff</c> (tin của chính nhân viên). Sau persist → <see cref="IConciergeRealtimeNotifier.NotifyConversationUpdatedAsync"/>.
/// </summary>
public sealed class ReplyConversationUseCase : IUseCase<ReplyConversationInput, ReplyConversationResult>
{
    private readonly IResortSettingsQuery _settingsQuery;
    private readonly IRepository<Conversation> _conversations;
    private readonly IRepository<Message> _messages;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IConciergeRealtimeNotifier _notifier;

    public ReplyConversationUseCase(
        IResortSettingsQuery settingsQuery,
        IRepository<Conversation> conversations,
        IRepository<Message> messages,
        IUnitOfWork unitOfWork,
        IClock clock,
        IConciergeRealtimeNotifier notifier)
    {
        _settingsQuery = settingsQuery;
        _conversations = conversations;
        _messages = messages;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notifier = notifier;
    }

    public async Task<Result<ReplyConversationResult>> ExecuteAsync(
        ReplyConversationInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var conversation = await _conversations.FindByIdAsync(input.ConversationId, ct).ConfigureAwait(false);
        if (conversation is null)
        {
            return Result.Failure<ReplyConversationResult>(ConciergeErrors.ConversationNotFound);
        }

        var settings = await _settingsQuery.GetAsync(ct).ConfigureAwait(false);
        if (settings is null)
        {
            return Result.Failure<ReplyConversationResult>(ConciergeErrors.ConfigurationUnavailable);
        }

        var body = (input.Body ?? string.Empty).Trim();
        if (body.Length == 0)
        {
            return Result.Failure<ReplyConversationResult>(ConciergeErrors.MessageEmpty);
        }

        if (body.Length > settings.MaxMessageLength)
        {
            return Result.Failure<ReplyConversationResult>(ConciergeErrors.MessageTooLong);
        }

        var now = _clock.UtcNow;

        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderType = MessageSenderType.Staff,
            SenderUserId = input.StaffUserId,
            Body = body,
            CreatedAt = now,
        };
        _messages.Add(message);

        // Reply MỞ LẠI hội thoại đang Closed (staff chủ động tiếp tục — QR-TO). KHÔNG tăng UnreadForStaff (tin của nhân viên).
        if (conversation.Status == ConversationStatus.Closed)
        {
            conversation.Status = ConversationStatus.Open;
            conversation.ClosedAt = null;
            conversation.ClosedByUserId = null;
        }

        conversation.LastMessageAt = now;
        conversation.LastStaffMessageAt = now;

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        await _notifier.NotifyConversationUpdatedAsync(conversation.ResortId, conversation.Id, ct).ConfigureAwait(false);
        return Result.Success(new ReplyConversationResult(message.Id, conversation.Status));
    }
}

/// <summary>Validate nhân viên trả lời: có hội thoại + có actor + body không rỗng (độ dài max chốt ở use case theo cấu hình).</summary>
public sealed class ReplyConversationValidator : AbstractValidator<ReplyConversationInput>
{
    public ReplyConversationValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.StaffUserId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty();
    }
}

/// <summary>
/// Nhân viên đánh dấu ĐÃ ĐỌC hội thoại (Req 5.3): <c>UnreadForStaff=0</c> + <c>ReadByStaffAt=now</c> cho các tin của
/// KHÁCH mà nhân viên chưa đọc. Void → <see cref="ICommandUseCase{TInput}"/> khai <see cref="PersistenceKey"/>
/// (decorator mở đúng Unit of Work Concierge). Nạp id tin-chưa-đọc qua read-model rồi load tracked + set (mirror
/// <see cref="GetGuestConversationUseCase"/>). Sau persist → <see cref="IConciergeRealtimeNotifier.NotifyMessageReadAsync"/>
/// (khách thấy "đã xem"). Hội thoại không tồn tại → <c>concierge_conversation_not_found</c>.
/// </summary>
public sealed class MarkConversationReadUseCase : ICommandUseCase<MarkConversationReadInput>
{
    public string PersistenceKey => ConciergeModule.PersistenceKey;

    private readonly IConciergeReader _reader;
    private readonly IRepository<Conversation> _conversations;
    private readonly IRepository<Message> _messages;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IConciergeRealtimeNotifier _notifier;

    public MarkConversationReadUseCase(
        IConciergeReader reader,
        IRepository<Conversation> conversations,
        IRepository<Message> messages,
        IUnitOfWork unitOfWork,
        IClock clock,
        IConciergeRealtimeNotifier notifier)
    {
        _reader = reader;
        _conversations = conversations;
        _messages = messages;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notifier = notifier;
    }

    public async Task<Result> ExecuteAsync(MarkConversationReadInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var conversation = await _conversations.FindByIdAsync(input.ConversationId, ct).ConfigureAwait(false);
        if (conversation is null)
        {
            return Result.Failure(ConciergeErrors.ConversationNotFound);
        }

        var now = _clock.UtcNow;
        conversation.UnreadForStaff = 0;

        var unreadIds = await _reader
            .ListMessageIdsUnreadByStaffAsync(conversation.Id, ct)
            .ConfigureAwait(false);
        foreach (var id in unreadIds)
        {
            var message = await _messages.FindByIdAsync(id, ct).ConfigureAwait(false);
            if (message is not null && message.ReadByStaffAt is null)
            {
                message.ReadByStaffAt = now;
            }
        }

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        await _notifier.NotifyMessageReadAsync(conversation.ResortId, conversation.Id, ct).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Nhân viên đóng hội thoại (Req 5.3): <c>Status=Closed</c> + <c>ClosedAt=now</c> + <c>ClosedByUserId=StaffUserId</c>.
/// Void → <see cref="ICommandUseCase{TInput}"/>. Idempotent: đã Closed → no-op Success (không đè ClosedAt). Sau persist
/// → <see cref="IConciergeRealtimeNotifier.NotifyConversationUpdatedAsync"/>. Không tồn tại → <c>concierge_conversation_not_found</c>.
/// </summary>
public sealed class CloseConversationUseCase : ICommandUseCase<CloseConversationInput>
{
    public string PersistenceKey => ConciergeModule.PersistenceKey;

    private readonly IRepository<Conversation> _conversations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IConciergeRealtimeNotifier _notifier;

    public CloseConversationUseCase(
        IRepository<Conversation> conversations,
        IUnitOfWork unitOfWork,
        IClock clock,
        IConciergeRealtimeNotifier notifier)
    {
        _conversations = conversations;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notifier = notifier;
    }

    public async Task<Result> ExecuteAsync(CloseConversationInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var conversation = await _conversations.FindByIdAsync(input.ConversationId, ct).ConfigureAwait(false);
        if (conversation is null)
        {
            return Result.Failure(ConciergeErrors.ConversationNotFound);
        }

        if (conversation.Status == ConversationStatus.Closed)
        {
            return Result.Success(); // idempotent — không đè ClosedAt/ClosedByUserId đã ghi.
        }

        var now = _clock.UtcNow;
        conversation.Status = ConversationStatus.Closed;
        conversation.ClosedAt = now;
        conversation.ClosedByUserId = input.StaffUserId;

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        await _notifier.NotifyConversationUpdatedAsync(conversation.ResortId, conversation.Id, ct).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Đóng hội thoại Open của một lượt lưu trú khi visit kết thúc (Req 10.8/CP9 — cascade SYSTEM). Capability cho C-GA.5
/// (wiring event-driven sau, cùng lúc huỷ ticket Housekeeping). Void → <see cref="ICommandUseCase{TInput}"/>. Idempotent:
/// visit chưa có hội thoại HOẶC hội thoại đã Closed → no-op Success. Đóng bởi SYSTEM → <c>ClosedByUserId=null</c>. Sau
/// persist → <see cref="IConciergeRealtimeNotifier.NotifyConversationUpdatedAsync"/>.
/// </summary>
public sealed class CloseConversationForVisitUseCase : ICommandUseCase<CloseConversationForVisitInput>
{
    public string PersistenceKey => ConciergeModule.PersistenceKey;

    private readonly IRepository<Conversation> _conversations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IConciergeRealtimeNotifier _notifier;

    public CloseConversationForVisitUseCase(
        IRepository<Conversation> conversations,
        IUnitOfWork unitOfWork,
        IClock clock,
        IConciergeRealtimeNotifier notifier)
    {
        _conversations = conversations;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notifier = notifier;
    }

    public async Task<Result> ExecuteAsync(CloseConversationForVisitInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var conversation = await _conversations
            .FirstOrDefaultAsync(c => c.GuestVisitId == input.GuestVisitId, ct)
            .ConfigureAwait(false);
        if (conversation is null || conversation.Status == ConversationStatus.Closed)
        {
            return Result.Success(); // idempotent — không có hội thoại hoặc đã đóng.
        }

        var now = _clock.UtcNow;
        conversation.Status = ConversationStatus.Closed;
        conversation.ClosedAt = now;
        conversation.ClosedByUserId = null; // System đóng theo cascade.

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        await _notifier.NotifyConversationUpdatedAsync(conversation.ResortId, conversation.Id, ct).ConfigureAwait(false);
        return Result.Success();
    }
}
