using ResortQr.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ResortQr.Infrastructure.Persistence;

/// <summary>
/// Factory design-time cho <c>dotnet ef</c> (startup project Api ≠ project chứa DbContext).
/// Đọc <c>ConnectionStrings:Postgres</c> (fallback placeholder để add migration offline không cần DB thật).
/// Clock/CurrentUser = stub — không được gọi khi build model.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Design-time: chỉ cần connString hợp lệ để build model (không mở kết nối khi add migration).
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Database=resortqr_design;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options, DesignTimeClock.Instance, DesignTimeCurrentUser.Instance);
    }

    private sealed class DesignTimeClock : IDateTimeProvider
    {
        public static readonly DesignTimeClock Instance = new();

        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch;
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public static readonly DesignTimeCurrentUser Instance = new();

        public Guid? UserId => null;

        public bool IsAuthenticated => false;

        public IReadOnlyCollection<string> Roles => [];

        public bool IsInRole(string role) => false;
    }
}
