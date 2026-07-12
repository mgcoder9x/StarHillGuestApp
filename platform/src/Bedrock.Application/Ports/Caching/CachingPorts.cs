namespace Bedrock.Application.Ports.Caching;

/// <summary>Tuỳ chọn hết hạn cache: TTL tuyệt đối và/hoặc trượt (null = không hết hạn theo chiều đó).</summary>
public sealed record CacheEntryOptions(TimeSpan? AbsoluteTtl = null, TimeSpan? SlidingTtl = null);

/// <summary>
/// Cache ứng dụng (contract-first, F28). Default khi chưa có adapter = <c>NullAppCache</c> miss-through
/// (degrade AN TOÀN — cache miss vẫn đúng, chỉ chậm hơn; §5.5). Adapter Redis impl cả 4 port caching dưới đây.
/// </summary>
public interface IAppCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);
}

/// <summary>Handle của một distributed lock đang giữ; <see cref="IAsyncDisposable.DisposeAsync"/> = nhả lock.</summary>
#pragma warning disable CA1040 // Marker rỗng CÓ CHỦ ĐÍCH: handle chỉ để dispose (nhả lock), không có thành viên riêng.
public interface ILockHandle : IAsyncDisposable;
#pragma warning restore CA1040

/// <summary>
/// Khoá phân tán (chống race đa-node). Trả null nếu KHÔNG lấy được khoá trong thời hạn — caller PHẢI kiểm null.
/// Default khi chưa có adapter = fail-loud (mất lock = tái tạo race, nguy hiểm — §5.5).
/// </summary>
public interface IDistributedLock
{
    Task<ILockHandle?> AcquireAsync(string key, TimeSpan ttl, CancellationToken ct = default);
}

/// <summary>Idempotency store (distributed): <c>true</c> nếu LẦN ĐẦU với key (được phép chạy), false nếu đã bắt đầu.</summary>
public interface IIdempotencyStore
{
    Task<bool> TryBeginAsync(string idempotencyKey, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>Chuyển claim sang Completed; duplicate tiếp theo phải bị chặn/replay bởi adapter.</summary>
    Task CompleteAsync(string idempotencyKey, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>Nhả claim sau failure/exception để retry hợp lệ không bị khóa đến hết TTL.</summary>
    Task AbortAsync(string idempotencyKey, CancellationToken ct = default);
}

/// <summary>
/// Rate-limit store PHÂN TÁN (nhiều node) — khác ASP.NET RateLimiter middleware ở <c>Bedrock.Api</c> (in-memory
/// per-node). Hai tầng, một khái niệm, không trùng trách nhiệm (§5.5). <c>true</c> = còn quota trong cửa sổ.
/// </summary>
public interface IRateLimitStore
{
    Task<bool> TryAcquireAsync(string partition, int limit, TimeSpan window, CancellationToken ct = default);
}
