namespace GuestAccess.Domain;

/// <summary>
/// Trạng thái lượt lưu trú (lưu string trong DB). Chuyển trạng thái MỘT CHIỀU: Active → Closed (lễ tân đóng)
/// hoặc Active → Expired (idle/sweeper). Không reopen visit đã kết thúc — quét lại tạo visit MỚI (Req 10.5).
/// </summary>
public enum GuestVisitStatus
{
    Active,
    Closed,
    Expired,
}
