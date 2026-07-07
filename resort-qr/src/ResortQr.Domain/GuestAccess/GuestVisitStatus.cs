namespace ResortQr.Domain.GuestAccess;

/// <summary>
/// Trạng thái lượt lưu trú (lưu string — DEC-006). 'Active' là hợp đồng với partial index
/// <c>ux_visit_active WHERE status='Active'</c> và <c>ix_visit_sweep</c>.
/// </summary>
public enum GuestVisitStatus
{
    Active,
    Closed,
    Expired,
}
