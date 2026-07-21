using Housekeeping.Domain;

namespace Housekeeping.Application;

// ---- Guest: tạo yêu cầu (idempotent 1-mở/phòng) + xem trạng thái (H-Hk.2, Req 6.1/6.2/6.7) ----
// ResortId/RoomId/GuestSessionId/GuestVisitId do server phân giải từ ICurrentGuestContextResolver (Api). KHÔNG tin client.

/// <summary>Khách yêu cầu dọn phòng. Idempotent: đã có ticket MỞ của phòng → trả ticket đó (nhắc), không tạo trùng.
/// GuestSessionId/GuestVisitId luôn có (server phân giải từ context) — dùng cho rule-gate + gắn ticket.</summary>
/// <summary>
/// Chi tiết yêu cầu dọn phòng do khách nhập (Req 6.1) — gom vào một bundle để không phình chữ ký + dễ validate.
/// <see cref="ServiceType"/>/<see cref="PreferredTime"/> BẮT BUỘC (guest). <see cref="PreferredTimeText"/> = <c>HH:mm</c>
/// CHỈ khi <see cref="PreferredTime"/> = <see cref="HousekeepingPreferredTime.SpecificTime"/>. Amenity ∈ [0,5];
/// <see cref="Note"/> plain-text (trim, ≤500). Use case validate + chuẩn hoá trước khi lưu (INV-HK1/HK2).
/// </summary>
public sealed record HousekeepingRequestDetails(
    HousekeepingServiceType ServiceType,
    HousekeepingPreferredTime PreferredTime,
    string? PreferredTimeText,
    int AmenityToothbrush,
    int AmenityTowel,
    int AmenityWater,
    int AmenitySoap,
    string? Note);

/// <summary>Khách yêu cầu dọn phòng. Idempotent: đã có ticket MỞ của phòng → trả ticket đó (nhắc), không tạo trùng.
/// GuestSessionId/GuestVisitId luôn có (server phân giải từ context) — dùng cho rule-gate + gắn ticket.
/// <see cref="Details"/> = chi tiết form (loại dịch vụ/thời gian/vật dụng/ghi chú) lưu TẠI request-time (FE.6a).</summary>
public sealed record RequestHousekeepingInput(
    Guid ResortId, Guid RoomId, Guid GuestSessionId, Guid GuestVisitId, HousekeepingRequestDetails Details);

/// <summary>Kết quả tạo/nhắc: ticket + <see cref="AlreadyOpen"/> (true nếu đã có ticket mở từ trước — idempotent).</summary>
public sealed record RequestHousekeepingResult(Guid TicketId, HousekeepingStatus Status, bool AlreadyOpen);

/// <summary>Khách xem trạng thái ticket hiện hành của phòng (Req 6.7).</summary>
public sealed record GetRoomHousekeepingStatusInput(Guid RoomId);

/// <summary>Trạng thái ticket hiện hành (null nếu phòng chưa từng có ticket).</summary>
public sealed record GetRoomHousekeepingStatusResult(HousekeepingTicketView? Ticket);

/// <summary>View ticket guest-facing (không lộ actor/method nội bộ ngoài trạng thái + mốc thời gian). Kèm chi tiết yêu cầu
/// (FE.6a) để khách xem lại tóm tắt đã gửi (loại dịch vụ/thời gian/vật dụng/ghi chú) — null/0 nếu ticket do staff tạo.</summary>
public sealed record HousekeepingTicketView(
    Guid TicketId,
    Guid RoomId,
    HousekeepingStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    HousekeepingServiceType? ServiceType,
    HousekeepingPreferredTime? PreferredTime,
    string? PreferredTimeText,
    int AmenityToothbrush,
    int AmenityTowel,
    int AmenityWater,
    int AmenitySoap,
    string? Note);

// ---- Staff: máy trạng thái + hoàn tất (H-Hk.2, Req 6.3/6.4/6.5/6.8/6.9) ----

/// <summary>Nhân viên đổi trạng thái ticket theo id (InProgress/Done). <paramref name="ActorUserId"/> từ ICurrentUser.</summary>
public sealed record SetHousekeepingStatusInput(
    Guid TicketId,
    HousekeepingStatus NewStatus,
    Guid? ActorUserId,
    HousekeepingCompletionMethod? Method);

public sealed record HousekeepingTicketResult(Guid TicketId, HousekeepingStatus Status);

/// <summary>Hoàn tất ticket MỞ của một phòng (chọn phòng trong app — Req 6.4, method App).</summary>
public sealed record CompleteHousekeepingByRoomInput(Guid RoomId, Guid? ActorUserId);

/// <summary>Hoàn tất ticket MỞ qua quét QR phòng (Req 6.5, method StaffScan). Token phân giải qua IRoomTokenResolver.</summary>
public sealed record CompleteHousekeepingByTokenInput(string Token, Guid? ActorUserId);

/// <summary>Nhân viên chủ động tạo ticket cho phòng (Req 6.9). Idempotent 1-mở/phòng (như guest, KHÔNG gate/flag — vận hành).</summary>
public sealed record CreateHousekeepingByStaffInput(Guid ResortId, Guid RoomId, Guid? ActorUserId);

/// <summary>Huỷ mọi ticket MỞ do một lượt lưu trú tạo (cascade khi visit kết thúc — CP9/Req 10.8; wiring event ở C-GA.5).</summary>
public sealed record CancelOpenTicketsForVisitInput(Guid GuestVisitId);

public sealed record CancelOpenTicketsForVisitResult(int CancelledCount);
