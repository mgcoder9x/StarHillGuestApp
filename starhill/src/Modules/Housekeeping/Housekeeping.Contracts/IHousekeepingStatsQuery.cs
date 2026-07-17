namespace Housekeeping.Contracts;

/// <summary>
/// Query-port stats cross-module (QR-AD-002) cho Host Dashboard đọc số ticket dọn phòng ĐANG MỞ
/// (Requested/InProgress) của một resort (Req 9.1). Impl EF đăng ký thủ công scoped ở Infrastructure. Chỉ ĐỌC-ĐẾM.
/// </summary>
public interface IHousekeepingStatsQuery
{
    Task<int> CountOpenTicketsAsync(Guid resortId, CancellationToken ct = default);
}
