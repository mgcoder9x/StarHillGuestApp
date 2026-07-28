using Concierge.Infrastructure.Persistence;
using Faq.Infrastructure.Persistence;
using GuestAccess.Infrastructure.Persistence;
using Housekeeping.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Infrastructure.Persistence;
using Rooms.Infrastructure.Persistence;
using Rules.Infrastructure.Persistence;
using Xunit;

namespace StarHill.Api.Tests;

/// <summary>
/// Guard chống drift migration (khép lỗ hổng cổng đã để lọt bug ApplyMigrationsOnStartup crash): MỌI module
/// DbContext của Host phải KHÔNG còn "pending model changes" — tức snapshot migration khớp model hiện tại.
/// Nếu ai đó đổi entity/config (kể cả feature base như outbox-replay-audit) mà QUÊN sinh migration, EF
/// <c>Database.HasPendingModelChanges()</c> trả true → test FAIL ngay ở CI_PR, TRƯỚC khi <c>MigrateAsync()</c>
/// lúc boot ném <c>PendingModelChangesWarning</c> và giết Host trong compose/production.
/// <para>
/// Docker-FREE: boot Host qua <see cref="SecretInjectingHostFactory"/> (migration-on-startup TẮT ở smoke config),
/// chỉ dựng model + so snapshot trong bộ nhớ — KHÔNG chạm PostgreSQL. Phủ ĐỦ 8 context (guard cũ
/// <see cref="ModuleMigrationHistorySchemaTests"/> chỉ kiểm history-schema cho 4 context, KHÔNG bắt drift model).
/// </para>
/// </summary>
public sealed class PendingModelChangesGuardTests : IClassFixture<SecretInjectingHostFactory>
{
    private readonly SecretInjectingHostFactory _factory;

    public PendingModelChangesGuardTests(SecretInjectingHostFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    public static TheoryData<Type> ModuleDbContextTypes() => new()
    {
        typeof(IdentityDbContext),
        typeof(ResortConfigDbContext),
        typeof(RoomsDbContext),
        typeof(GuestAccessDbContext),
        typeof(RulesDbContext),
        typeof(FaqDbContext),
        typeof(HousekeepingDbContext),
        typeof(ConciergeDbContext),
    };

    [Theory]
    [MemberData(nameof(ModuleDbContextTypes))]
    public void Module_dbcontext_has_no_pending_model_changes(Type contextType)
    {
        using var scope = _factory.Services.CreateScope();
        var context = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);

        Assert.False(
            context.Database.HasPendingModelChanges(),
            $"{contextType.Name} có thay đổi model chưa sinh migration. Chạy: dotnet ef migrations add <Tên> "
            + $"--project src/Modules/<Module>/<Module>.Infrastructure --startup-project <cùng đường dẫn>. "
            + "Bỏ qua sẽ khiến ApplyMigrationsOnStartup ném PendingModelChangesWarning và crash Host lúc boot.");
    }
}
