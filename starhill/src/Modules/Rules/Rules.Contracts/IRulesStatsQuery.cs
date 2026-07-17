namespace Rules.Contracts;

/// <summary>
/// Query-port stats cross-module (QR-AD-002) cho Host Dashboard đọc số lượt XÁC NHẬN nội quy kể từ mốc
/// <paramref name="since"/> (Host truyền = đầu ngày) của một resort (Req 9.1 "ack trong ngày"). Tách quyết định
/// "đầu ngày/tz" khỏi module (module chỉ đếm AcceptedAt ≥ since). Impl EF scoped ở Infrastructure. Chỉ ĐỌC-ĐẾM.
/// </summary>
public interface IRulesStatsQuery
{
    Task<int> CountAcknowledgementsSinceAsync(Guid resortId, DateTimeOffset since, CancellationToken ct = default);
}
