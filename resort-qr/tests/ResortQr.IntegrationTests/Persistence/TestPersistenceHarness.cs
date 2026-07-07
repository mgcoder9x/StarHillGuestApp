using System;
using System.Collections.Generic;
using ResortQr.Application.Abstractions;
using ResortQr.Infrastructure.Persistence;
using ResortQr.SharedKernel.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ResortQr.IntegrationTests.Persistence;

/// <summary>
/// Entity mẫu (CHỈ dùng test) để kiểm các convention nền: audit + xóa mềm + concurrency-aware.
/// </summary>
public sealed class SampleEntity : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>DbContext test dẫn xuất <see cref="ResortQrDbContext"/> — thêm DbSet nghiệp vụ giả lập.</summary>
public sealed class SampleDbContext : ResortQrDbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options, IDateTimeProvider clock, ICurrentUser currentUser)
        : base(options, clock, currentUser)
    {
    }

    public DbSet<SampleEntity> Samples => Set<SampleEntity>();
}

/// <summary>Clock test có thể chỉnh (đẩy thời gian để kiểm CreatedAt≠UpdatedAt).</summary>
public sealed class MutableClock : IDateTimeProvider
{
    public MutableClock(DateTimeOffset start) => UtcNow = start;

    public DateTimeOffset UtcNow { get; set; }
}

/// <summary>Current-user test cố định (actor cho audit).</summary>
public sealed class FixedCurrentUser : ICurrentUser
{
    public FixedCurrentUser(Guid? userId) => UserId = userId;

    public Guid? UserId { get; }

    public bool IsAuthenticated => UserId is not null;

    public IReadOnlyCollection<string> Roles => Array.Empty<string>();

    public bool IsInRole(string role) => false;
}

/// <summary>
/// Harness SQLite in-memory (Docker-free): DB quan hệ THẬT (enforce transaction/UPDATE...WHERE/unique) —
/// hợp lệ để kiểm hành vi PROVIDER-AGNOSTIC (DEC-021 chỉ cấm EF InMemory provider — thứ KHÔNG enforce ràng buộc).
/// Bật snake_case để verify EFCore.NamingConventions 10.0.1 chạy trên EF10. Giữ connection MỞ để DB in-memory sống.
/// KHÔNG kiểm được: xmin, partial index Postgres, race đa-connection thật → cần Testcontainers (TK-036).
/// </summary>
public sealed class SqlitePersistenceHarness : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqlitePersistenceHarness(DateTimeOffset now, Guid? actor)
    {
        Clock = new MutableClock(now);
        CurrentUser = new FixedCurrentUser(actor);

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<SampleDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    private readonly DbContextOptions<SampleDbContext> _options;

    public MutableClock Clock { get; }

    public FixedCurrentUser CurrentUser { get; }

    /// <summary>Mỗi lần gọi tạo context MỚI (mô phỏng scope/req riêng) nhưng CÙNG DB (chung connection).</summary>
    public SampleDbContext CreateContext() => new(_options, Clock, CurrentUser);

    public void Dispose() => _connection.Dispose();
}
