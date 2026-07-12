namespace Rooms.Domain;

/// <summary>
/// Trạng thái token QR (lưu string). CHỈ Active/Revoked — KHÔNG auto-expire (Req 7.5/CP2).
/// Giá trị 'Active' là hợp đồng với partial index <c>ux_qr_active WHERE status='Active'</c>.
/// </summary>
public enum RoomQrTokenStatus
{
    Active,
    Revoked,
}
