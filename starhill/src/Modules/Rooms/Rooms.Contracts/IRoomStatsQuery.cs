namespace Rooms.Contracts;

/// <summary>
/// Query-port stats cross-module (QR-AD-002) cho Host Dashboard đọc số phòng ĐANG HOẠT ĐỘNG (Status=Active, loại
/// phòng xóa-mềm) của một resort (Req 9.1). Impl EF đăng ký thủ công scoped ở Infrastructure. Chỉ ĐỌC-ĐẾM.
/// </summary>
public interface IRoomStatsQuery
{
    Task<int> CountActiveRoomsAsync(Guid resortId, CancellationToken ct = default);
}
