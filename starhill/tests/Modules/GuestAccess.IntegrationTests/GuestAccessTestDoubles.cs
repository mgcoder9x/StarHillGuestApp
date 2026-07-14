using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using ResortConfig.Contracts.Queries;
using Rooms.Contracts;

namespace GuestAccess.IntegrationTests;

/// <summary>Clock có thể chỉnh (test lazy-expiry/sliding window tất định).</summary>
internal sealed class MutableClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
}

/// <summary>Sinh token duy nhất (raw session key mới mỗi lần) — mirror generator test của Rooms.</summary>
internal sealed class SequentialTokenGenerator : ITokenGenerator
{
    private int _counter;
    public string NewToken(int byteLength = 32) =>
        $"raw-{System.Threading.Interlocked.Increment(ref _counter):D6}-{Guid.NewGuid():N}";
}

internal sealed class StubCurrentUser : ICurrentUser
{
    public Guid? UserId => null;
    public bool IsAuthenticated => false;
    public IReadOnlyCollection<string> Roles => [];
    public IReadOnlyCollection<string> Permissions => [];
    public Guid? TenantId => null;
    public Guid? SessionId => null;
    public bool IsInRole(string role) => false;
    public bool HasPermission(string permission) => false;
}

/// <summary>
/// Stub <see cref="IRoomTokenResolver"/> — cô lập GuestAccess khỏi RoomsDbContext (impl EF thật đã test ở Rooms).
/// Trả <see cref="Result"/> đã cấu hình theo token: null (qr_invalid) / IsRoomActive=false (room_inactive) / active.
/// </summary>
internal sealed class StubRoomTokenResolver : IRoomTokenResolver
{
    public RoomResolution? Result { get; set; }

    /// <summary>Số lần resolver được gọi — chứng minh malformed token bị chặn TRƯỚC resolver/DB (QR-AD-025).</summary>
    public int CallCount { get; private set; }

    /// <summary>Token thô cuối cùng resolver nhận — để test khẳng định không có lookup khi input malformed.</summary>
    public string? LastToken { get; private set; }

    public Task<RoomResolution?> ResolveActiveTokenAsync(string token, CancellationToken ct = default)
    {
        CallCount++;
        LastToken = token;
        return Task.FromResult(Result);
    }
}

/// <summary>Stub <see cref="IResortGuestConfigQuery"/> — trả config cố định (hoặc null = configuration_unavailable).</summary>
internal sealed class StubResortGuestConfigQuery : IResortGuestConfigQuery
{
    public ResortGuestConfig? Config { get; set; }

    public Task<ResortGuestConfig?> GetAsync(Guid resortId, CancellationToken ct = default) =>
        Task.FromResult(Config);
}
