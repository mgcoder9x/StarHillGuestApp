using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ResortConfig.Infrastructure.Persistence;

/// <summary>
/// Design-time factory cho <c>dotnet ef migrations</c> (F31/§4.6 — migration per-module). BẮT BUỘC khớp CHÍNH XÁC
/// options runtime (<c>UseNpgsql</c> + <c>UseSnakeCaseNamingConvention</c> như <c>AddBedrockPersistence</c>) —
/// nếu lệch, migration sinh ra sẽ lệch model runtime (drift schema). Connection string DUMMY: <c>migrations add</c>
/// không kết nối; <c>database update</c> nhận connection thật qua CLI/env lúc deploy (out-of-band).
/// </summary>
public sealed class ResortConfigDbContextFactory : IDesignTimeDbContextFactory<ResortConfigDbContext>
{
    public ResortConfigDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ResortConfigDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=resort_config_design_time;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "resort_config"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ResortConfigDbContext(options, DesignTimeStub.Instance, DesignTimeStub.Instance, DesignTimeStub.Instance);
    }

    /// <summary>Stub design-time cho <see cref="IClock"/>/<see cref="ICurrentUser"/>/<see cref="IDomainEventDispatcher"/>
    /// — chỉ để dựng context lúc sinh migration; KHÔNG dùng runtime.</summary>
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
