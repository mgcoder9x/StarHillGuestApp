namespace Rooms.Contracts;

/// <summary>
/// Hằng định danh module Rooms cho KEYED persistence (design §4.6 — schema <c>rooms</c>). Dùng khi
/// Host/Infrastructure gọi <c>AddBedrockPersistence&lt;RoomsDbContext&gt;(PersistenceKey, ...)</c>, khi đăng ký
/// keyed repository <c>Room</c>/<c>RoomQrToken</c>, và khi use case ghi khai <c>PersistenceKey</c> → resolver chọn
/// ĐÚNG Unit of Work của Rooms (chống last-registration-wins P0-1).
/// </summary>
public static class RoomsModule
{
    public const string PersistenceKey = "rooms";
}
