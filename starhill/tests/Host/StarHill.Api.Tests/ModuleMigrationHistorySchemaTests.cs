using GuestAccess.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Infrastructure.Persistence;
using Rooms.Infrastructure.Persistence;
using Xunit;

namespace StarHill.Api.Tests;

/// <summary>
/// Guard QR-AD-028: mỗi module DbContext của Host phải cấu hình <c>__EFMigrationsHistory</c> trong ĐÚNG schema
/// module (không dùng chung <c>public</c>). Boot Host thật qua <see cref="SecretInjectingHostFactory"/> (Docker-free
/// — migration-on-startup TẮT ở smoke config nên chỉ đọc options, không chạm DB), resolve từng DbContext và đọc
/// <see cref="RelationalOptionsExtension.MigrationsHistoryTableSchema"/>. Đây là guard tự động thay cho bằng chứng
/// Compose thủ công (QR-N-028) → khoá wiring history-per-schema ở RUNTIME Host, chống drift ledger ngầm.
/// </summary>
public sealed class ModuleMigrationHistorySchemaTests : IClassFixture<SecretInjectingHostFactory>
{
    private readonly SecretInjectingHostFactory _factory;

    public ModuleMigrationHistorySchemaTests(SecretInjectingHostFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    private static string? HistorySchema(DbContext context) =>
        RelationalOptionsExtension.Extract(context.GetService<IDbContextOptions>()).MigrationsHistoryTableSchema;

    [Fact]
    public void Each_module_dbcontext_keeps_migration_history_in_its_own_schema()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        Assert.Equal("identity", HistorySchema(sp.GetRequiredService<IdentityDbContext>()));
        Assert.Equal("resort_config", HistorySchema(sp.GetRequiredService<ResortConfigDbContext>()));
        Assert.Equal("rooms", HistorySchema(sp.GetRequiredService<RoomsDbContext>()));
        Assert.Equal("guest_access", HistorySchema(sp.GetRequiredService<GuestAccessDbContext>()));
    }
}
