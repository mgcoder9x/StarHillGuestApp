namespace Bedrock.Application.Ports.Persistence;

/// <summary>
/// Điểm ghi DB DUY NHẤT + giao dịch tường minh. Gom nhiều thay đổi vào một lần ghi nguyên tử.
/// KHÔNG có <c>Repository&lt;T&gt;()</c> accessor (DV-002): use case inject <see cref="IRepository{T}"/> trực tiếp
/// (dependency tường minh + testable). Rotation token/side-effect + ghi Outbox PHẢI nằm trong
/// <see cref="ExecuteInTransactionAsync"/> (I3/F5/F25).
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Ghi tất cả thay đổi đang chờ trong MỘT lần. Trả số bản ghi bị ảnh hưởng.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Chạy <paramref name="action"/> trong một transaction: commit nếu không lỗi, rollback nếu lỗi (all-or-nothing).
    /// <b>REENTRANCY (R7.4):</b> nếu đã có transaction do chính UoW này mở đang hoạt động, lời gọi lồng
    /// THAM GIA transaction hiện hành (không BEGIN lồng, không commit sớm) — cần thiết vì Transaction behavior
    /// (§8) bọc command trong khi use case cũng có thể gọi tường minh.
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken ct = default);
}
