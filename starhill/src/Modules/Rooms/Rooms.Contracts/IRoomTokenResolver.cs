namespace Rooms.Contracts;

/// <summary>
/// Kết quả phân giải token QR → phòng (DTO thuần cho module hạ nguồn). KHÔNG lộ enum Domain: trạng thái phòng
/// biểu diễn bằng <see cref="IsRoomActive"/> (bool) để consumer (GuestAccess) map lỗi <c>room_inactive</c> nếu cần.
/// </summary>
public sealed record RoomResolution(
    Guid RoomId,
    Guid ResortId,
    string RoomNumber,
    string? Building,
    int? Floor,
    bool IsRoomActive);

/// <summary>
/// Phân giải capability token QR → phòng (Req 1.3, CP1). Consumer: GuestAccess.resolve + Housekeeping.complete-by-token.
/// Trả <c>null</c> khi token KHÔNG khớp token Active nào, hoặc phòng đã xóa mềm (KHÔNG lộ thông tin phòng khác — CP1).
/// Token đã Revoked → không khớp (chỉ tra token Status=Active) → null. Phòng Inactive/Maintenance vẫn trả resolution
/// với <see cref="RoomResolution.IsRoomActive"/>=false để consumer tự quyết (phân biệt qr_invalid vs room_inactive).
/// Impl EF ở Infrastructure (đăng ký thủ công scoped — Contracts không mang marker DI).
/// </summary>
public interface IRoomTokenResolver
{
    Task<RoomResolution?> ResolveActiveTokenAsync(string token, CancellationToken ct = default);
}
