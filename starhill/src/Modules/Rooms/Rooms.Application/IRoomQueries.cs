using Bedrock.Application.UseCases;
using Rooms.Domain;

namespace Rooms.Application;

/// <summary>
/// Item read-model danh sách/chi tiết phòng cho admin (B-Rooms.4). DTO thuần (KHÔNG lộ entity Domain). Mang preview
/// + version của token QR Active hiện hành để admin đối soát (Req 11.6) — KHÔNG lộ token thô (CP1). Nếu phòng chưa
/// có token Active (vd đang Inactive sau rotate lỗi) thì <see cref="ActiveTokenPreview"/>=null, <see cref="ActiveTokenVersion"/>=0.
/// </summary>
public sealed record RoomListItem(
    Guid RoomId,
    string RoomNumber,
    string? Building,
    int? Floor,
    RoomStatus Status,
    string? ActiveTokenPreview,
    int ActiveTokenVersion,
    DateTimeOffset CreatedAt);

/// <summary>
/// Read port admin NỘI-MODULE của Rooms (đặt ở Application, KHÔNG Contracts — không cross-module; Api cùng module
/// tiêu thụ). Read-only → KHÔNG qua use case pipeline (mirror <c>IRoomTokenResolver</c>/<c>IResortSettingsQuery</c>).
/// Impl EF ở Infrastructure inject <c>RoomsDbContext</c>, đăng ký scoped. Phòng xóa mềm bị global query filter loại.
/// </summary>
public interface IRoomQueries
{
    /// <summary>Danh sách phòng (phân trang, lọc theo <paramref name="status"/> nếu có), sắp theo Building→RoomNumber.</summary>
    Task<PagedResult<RoomListItem>> ListAsync(RoomStatus? status, PagedRequest paging, CancellationToken ct = default);

    /// <summary>Chi tiết một phòng; <c>null</c> nếu không tồn tại (hoặc đã xóa mềm).</summary>
    Task<RoomListItem?> GetByIdAsync(Guid roomId, CancellationToken ct = default);
}
