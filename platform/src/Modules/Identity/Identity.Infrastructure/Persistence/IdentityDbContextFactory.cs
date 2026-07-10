using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Design-time factory cho <c>dotnet ef migrations</c> (F31/§4.6 — migration per-module). CLI cần dựng
/// <see cref="IdentityDbContext"/> mà KHÔNG chạy Host → factory này cấp options + stub cho các dependency
/// (không bao giờ bị gọi lúc build model). <b>BẮT BUỘC khớp CHÍNH XÁC options runtime</b>
/// (<c>UseNpgsql</c> + <c>UseSnakeCaseNamingConvention</c> như <c>AddBedrockPersistence</c>) — nếu lệch,
/// migration sinh ra sẽ lệch model runtime (drift schema). Schema <c>identity</c> đặt trong
/// <see cref="IdentityDbContext.OnModelCreating"/> (HasDefaultSchema) → __EFMigrationsHistory cũng vào schema đó
/// (migrate độc lập per-module). Connection string là DUMMY: <c>migrations add</c> không kết nối; <c>database
/// update</c> nhận connection thật qua tham số CLI/env lúc deploy (out-of-band, không auto-migrate trong app).
/// </summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql("Host=localhost;Database=identity_design_time;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new IdentityDbContext(options, DesignTimeStub.Instance, DesignTimeStub.Instance, DesignTimeStub.Instance);
    }

    /// <summary>Stub design-time cho <see cref="IClock"/>/<see cref="ICurrentUser"/>/<see cref="IDomainEventDispatcher"/>
    /// — chỉ để dựng context lúc sinh migration; KHÔNG dùng runtime (giá trị vô hại, không side-effect).</summary>
    private sealed class DesignTimeStub : IClock, ICurrentUser, IDomainEventDispatcher
    {
        public static readonly DesignTimeStub Instance = new();

        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch;

        public Guid? UserId => null;

        public bool IsAuthenticated => false;

        public IReadOnlyCollection<string> Roles => [];

        public IReadOnlyCollection<string> Permissions => [];

        public Guid? TenantId => null;

        public Guid? SessionId => null;

        public bool IsInRole(string role) => false;

        public bool HasPermission(string permission) => false;

        public Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
