using System;
using Foundation.Application.Abstractions;
using Foundation.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Foundation.IntegrationTests.Persistence;

/// <summary>
/// Verify OFFLINE (không cần DB/Docker) rằng convention concurrency token phụ thuộc PROVIDER hoạt động đúng:
/// - Npgsql: RowVersion map cột hệ thống <c>xmin</c> (type xid, DB-generated, concurrency token) —
///   đây là bản thay thế THỦ CÔNG cho helper UseXminAsConcurrencyToken() đã bị bỏ ở Npgsql EF Core 10.
/// - SQLite: KHÔNG gắn concurrency token (nhánh điều kiện Database.IsNpgsql() = false) → test SQLite không vỡ.
/// Model building chạy OnModelCreating nhưng KHÔNG mở kết nối → kiểm được không cần Postgres.
/// </summary>
public sealed class ProviderConditionalModelTests
{
    private static readonly IDateTimeProvider Clock = new MutableClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    private static readonly ICurrentUser User = new FixedCurrentUser(null);

    [Fact]
    public void Npgsql_maps_rowversion_to_xmin_concurrency_token()
    {
        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UseNpgsql("Host=localhost;Database=offline_model_only") // KHÔNG mở kết nối — chỉ build model
            .UseSnakeCaseNamingConvention()
            .Options;

        using var ctx = new SampleDbContext(options, Clock, User);

        var property = ctx.Model
            .FindEntityType(typeof(SampleEntity))!
            .FindProperty(nameof(AuditableEntity.RowVersion))!;

        Assert.True(property.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
        Assert.Equal("xmin", property.GetColumnName());
        Assert.Equal("xid", property.GetColumnType());
    }

    [Fact]
    public void Sqlite_does_not_map_rowversion_as_concurrency_token()
    {
        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UseSqlite("DataSource=:memory:")
            .UseSnakeCaseNamingConvention()
            .Options;

        using var ctx = new SampleDbContext(options, Clock, User);

        var property = ctx.Model
            .FindEntityType(typeof(SampleEntity))!
            .FindProperty(nameof(AuditableEntity.RowVersion))!;

        // Nhánh Npgsql-only bị bỏ qua → RowVersion là cột thường, KHÔNG phải concurrency token.
        Assert.False(property.IsConcurrencyToken);
        Assert.NotEqual("xmin", property.GetColumnName());
    }
}
