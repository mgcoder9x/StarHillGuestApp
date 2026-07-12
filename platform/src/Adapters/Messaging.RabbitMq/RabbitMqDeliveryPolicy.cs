using System.Text.Json;
using Bedrock.Application.Messaging.Dispatch;

namespace Adapters.Messaging.RabbitMq;

/// <summary>Hành động với một delivery sau khi dispatch (P0-02).</summary>
public enum DeliveryAction
{
    /// <summary>Xử lý xong (Handled/Duplicate) → ACK.</summary>
    Acknowledge,

    /// <summary>Lỗi TẠM THỜI còn lượt → publish sang retry (delay) rồi ACK bản gốc.</summary>
    Retry,

    /// <summary>Lỗi VĨNH VIỄN hoặc hết lượt retry → publish sang final DLQ (quarantine) rồi ACK.</summary>
    DeadLetter,
}

/// <summary>
/// P0-02 — CHÍNH SÁCH giao message consume, TÁCH thành hàm THUẦN (không I/O) để unit-test đầy đủ (phần rủi ro nhất
/// của retry tier). Phân loại:
/// <list type="bullet">
///   <item><b>Permanent</b> (dispatch trả <see cref="InboxDispatchOutcome.DeadLettered"/> = unknown type/content-type/
///   schema sai; hoặc exception <see cref="JsonException"/> = deserialize/id-mismatch) → DLQ NGAY (retry vô ích).</item>
///   <item><b>Transient</b> (exception khác: DB/network/timeout/handler tạm) → <see cref="DeliveryAction.Retry"/> nếu
///   còn lượt (<paramref name="priorAttempts"/> + 1 &lt; maxAttempts), hết lượt → DLQ.</item>
/// </list>
/// Nhờ tách hàm thuần, quyết định "transient có bị coi là poison vĩnh viễn không" được kiểm chứng KHÔNG cần broker.
/// </summary>
public static class RabbitMqDeliveryPolicy
{
    /// <summary>Quyết định cho trường hợp dispatch TRẢ VỀ outcome (không ném).</summary>
    public static DeliveryAction ForOutcome(InboxDispatchOutcome outcome) => outcome switch
    {
        InboxDispatchOutcome.Handled or InboxDispatchOutcome.Duplicate => DeliveryAction.Acknowledge,
        _ => DeliveryAction.DeadLetter, // DeadLettered (permanent envelope/type/schema) → quarantine, KHÔNG retry.
    };

    /// <summary>
    /// Quyết định khi dispatch NÉM exception. <paramref name="priorAttempts"/> = số lần đã giao TRƯỚC lần này
    /// (header <c>x-bedrock-attempt</c>, 0 nếu lần đầu). <paramref name="maxAttempts"/> = tổng số lần giao tối đa.
    /// </summary>
    public static DeliveryAction ForException(Exception exception, int priorAttempts, int maxAttempts)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (IsPermanent(exception))
        {
            return DeliveryAction.DeadLetter; // permanent → không retry (retry cũng lỗi y hệt).
        }

        // Tổng số lần giao tính CẢ lần hiện tại = priorAttempts + 1. Còn lượt (chưa đạt max) → retry.
        return priorAttempts + 1 < maxAttempts ? DeliveryAction.Retry : DeliveryAction.DeadLetter;
    }

    /// <summary>Lỗi VĨNH VIỄN (retry không cứu được): payload/envelope hỏng. Hiện: <see cref="JsonException"/>
    /// (deserialize/id-mismatch từ dispatcher). Mở rộng khi có thêm loại permanent xác định.</summary>
    public static bool IsPermanent(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception is JsonException;
    }
}
