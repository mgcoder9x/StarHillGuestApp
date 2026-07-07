namespace ResortQr.Domain.Rooms;

/// <summary>
/// Trạng thái token QR (lưu string — DEC-006). CHỈ Active/Revoked — KHÔNG auto-expire.
/// Giá trị 'Active' là hợp đồng với partial index <c>ux_qr_active WHERE status='Active'</c>.
/// </summary>
public enum RoomQrTokenStatus
{
    Active,
    Revoked,
}
