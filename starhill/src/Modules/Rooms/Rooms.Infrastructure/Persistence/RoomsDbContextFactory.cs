using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Rooms.Infrastructure.Persistence;

/// <summary>
/// Design-time factory cho <c>dotnet ef migrations</c> (F31/§4.6 per-module). Khớp CHÍNH XÁC options runtime
/// (<c>UseNpgsql</c> + <c>UseSnakeCaseNamingConvention</c>) để migration không drift model. Connection string DUMMY
/// (migrations add không kết nối); schema <c>rooms</c> đặt trong DbContext.
/// </summary>
public sealed class RoomsDbContextFactory : IDesignTimeDbContextFactory<RoomsDbContext>
{
    public RoomsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RoomsDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=rooms_design_time;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "rooms"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new RoomsDbContext(options, DesignTimeStub.Instance, DesignTimeStub.Instance, DesignTimeStub.Instance);
    }

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
