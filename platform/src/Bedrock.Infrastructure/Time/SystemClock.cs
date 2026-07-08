using Bedrock.Application.DependencyInjection;
using Bedrock.Application.Ports.Time;

namespace Bedrock.Infrastructure.Time;

/// <summary>
/// Implementation mặc định của <see cref="IClock"/> dùng đồng hồ hệ thống (UTC).
/// Đăng ký Singleton (không state, thread-safe) — khai <see cref="ISingletonService"/> để auto-scan
/// nhặt đúng lifetime; <c>AddBedrockPersistence</c> cũng <c>TryAddSingleton</c> tường minh (idempotent).
/// Đây là điểm DUY NHẤT chạm <see cref="DateTimeOffset.UtcNow"/> — use case luôn qua <see cref="IClock"/>
/// để tất định + test được (design §5.7 / port <c>IClock</c>).
/// </summary>
public sealed class SystemClock : IClock, ISingletonService
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
