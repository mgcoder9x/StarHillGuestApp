using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Concierge.Domain;

namespace Concierge.Application;

/// <summary>
/// Khách đọc hội thoại của LƯỢT LƯU TRÚ hiện tại của mình (Req 5.9 — polling fallback). KHÔNG rule-gate (đọc dữ liệu
/// của chính mình — chỉ giới hạn bởi scope visit do Api phân giải). Trả tin sắp theo thời gian (Id-v7). Đồng thời
/// đánh dấu các tin của NHÂN VIÊN mà khách chưa đọc là <c>ReadByGuestAt=now</c> (để lễ tân thấy "đã xem"): nạp id
/// qua read-model rồi load tracked + set + một SaveChanges (mirror CancelOpenTicketsForVisit). KHÔNG có tin chưa đọc
/// → KHÔNG ghi (đọc thuần, tránh write mỗi lần poll). Realtime read-receipt (notify) để K-Con.4.
/// </summary>
public sealed class GetGuestConversationUseCase : IUseCase<GetGuestConversationInput, GetGuestConversationResult>
{
    private readonly IConciergeReader _reader;
    private readonly IRepository<Message> _messages;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public GetGuestConversationUseCase(
        IConciergeReader reader, IRepository<Message> messages, IUnitOfWork unitOfWork, IClock clock)
    {
        _reader = reader;
        _messages = messages;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<GetGuestConversationResult>> ExecuteAsync(
        GetGuestConversationInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var view = await _reader.GetGuestConversationByVisitAsync(input.GuestVisitId, ct).ConfigureAwait(false);
        if (view is null)
        {
            return Result.Success(new GetGuestConversationResult(null));
        }

        var unreadIds = await _reader
            .ListMessageIdsUnreadByGuestAsync(view.ConversationId, ct)
            .ConfigureAwait(false);
        if (unreadIds.Count > 0)
        {
            var now = _clock.UtcNow;
            var marked = false;
            foreach (var id in unreadIds)
            {
                var message = await _messages.FindByIdAsync(id, ct).ConfigureAwait(false);
                if (message is not null && message.ReadByGuestAt is null)
                {
                    message.ReadByGuestAt = now;
                    marked = true;
                }
            }

            if (marked)
            {
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            }
        }

        return Result.Success(new GetGuestConversationResult(view));
    }
}
