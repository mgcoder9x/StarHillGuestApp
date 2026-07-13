namespace ResortConfig.Contracts.Queries;

/// <summary>
/// Kiểm tra một <c>ResortId</c> có tồn tại hay không — hợp đồng cross-module (F30) để module khác THẨM ĐỊNH
/// tham chiếu resort TRƯỚC khi ghi, thay cho FK (KHÔNG có FK chéo-schema — QR-DV-003). Consumer đầu tiên:
/// <c>Rooms.CreateRoom</c> (chặn tạo phòng trỏ tới resort không tồn tại → tránh phòng "mồ côi").
/// <para>
/// Đây là toàn vẹn tham chiếu MỨC ỨNG DỤNG (best-effort): giữa lúc kiểm và lúc ghi resort có thể bị xoá về lý
/// thuyết — nhưng Resort là seed-once instance-per-resort, KHÔNG có thao tác xoá/xoá-mềm → cửa sổ TOCTOU không
/// hiện thực trong nghiệp vụ hiện tại. Vẫn tốt hơn tuyệt đối so với không kiểm gì.
/// </para>
/// </summary>
public interface IResortExistenceQuery
{
    /// <summary>Trả <c>true</c> nếu tồn tại <c>Resort</c> với <paramref name="resortId"/>.</summary>
    Task<bool> ExistsAsync(Guid resortId, CancellationToken ct = default);
}
