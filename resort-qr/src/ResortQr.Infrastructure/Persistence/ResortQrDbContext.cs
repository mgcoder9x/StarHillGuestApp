using System.Linq.Expressions;
using ResortQr.Application.Abstractions;
using ResortQr.Application.Identity;
using ResortQr.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ResortQr.Infrastructure.Persistence;

/// <summary>
/// DbContext NỀN (app dẫn xuất): áp các convention GENERIC dùng lại cho mọi app —
/// audit tự động (CreatedAt/UpdatedAt + actor), xóa mềm (query filter + chuyển Delete→Modified),
/// optimistic concurrency <c>xmin</c> (CHỈ Npgsql — điều kiện theo provider để test SQLite không vỡ).
/// Base tự map entity generic của mình (<see cref="RefreshTokenRecord"/>); app khai thêm DbSet nghiệp vụ.
/// </summary>
public abstract class ResortQrDbContext : DbContext
{
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser _currentUser;

    protected ResortQrDbContext(DbContextOptions options, IDateTimeProvider clock, ICurrentUser currentUser)
        : base(options)
    {
        _clock = clock;
        _currentUser = currentUser;
    }

    /// <summary>Refresh token là khái niệm app-agnostic → base sở hữu bảng + store atomic.</summary>
    public DbSet<RefreshTokenRecord> RefreshTokens => Set<RefreshTokenRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        // Config của base (RefreshToken...) — nằm trong assembly Infrastructure.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResortQrDbContext).Assembly);

        ApplyConventions(modelBuilder);
    }

    /// <summary>
    /// Ghi thay đổi (điểm ghi thật). Áp audit + xóa mềm TRƯỚC khi persist. UoW mới là nơi bọc transaction/409.
    /// </summary>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>Áp convention theo model: query filter xóa mềm + concurrency token (Npgsql-only).</summary>
    private void ApplyConventions(ModelBuilder modelBuilder)
    {
        var isNpgsql = Database.IsNpgsql();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType).HasQueryFilter(BuildIsNotDeletedFilter(clrType));
            }

            // xmin là cột hệ thống của PostgreSQL → chỉ áp khi provider là Npgsql.
            // Lý do gốc: cho phép test provider-agnostic bằng SQLite (không Docker) mà không vỡ model.
            // LƯU Ý (verify từ assembly thật): Npgsql EF Core 10 ĐÃ BỎ helper UseXminAsConcurrencyToken()
            // → map THỦ CÔNG RowVersion(uint) → cột hệ thống xmin (type xid, DB-generated, concurrency token).
            // Đây đúng những gì helper cũ làm bên dưới; xid vẫn được Npgsql 10 hỗ trợ.
            if (isNpgsql && typeof(IConcurrencyAware).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType)
                    .Property(nameof(IConcurrencyAware.RowVersion))
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
            }
        }
    }

    /// <summary>Dựng biểu thức <c>e => !e.IsDeleted</c> động theo kiểu entity (cho HasQueryFilter).</summary>
    private static LambdaExpression BuildIsNotDeletedFilter(Type clrType)
    {
        var parameter = Expression.Parameter(clrType, "e");
        var isDeleted = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
        var notDeleted = Expression.Not(isDeleted);
        return Expression.Lambda(notDeleted, parameter);
    }

    /// <summary>
    /// Audit: Added→set CreatedAt/CreatedBy; Modified→set UpdatedAt/UpdatedBy (KHÔNG đụng Created*).
    /// Xóa mềm: entity <see cref="ISoftDeletable"/> bị Delete → chuyển Modified + IsDeleted=true + DeletedAt.
    /// </summary>
    private void ApplyAuditAndSoftDelete()
    {
        var now = _clock.UtcNow;
        var actor = _currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is ISoftDeletable softDeletable && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = now;
            }

            if (entry.Entity is not IAuditable auditable)
            {
                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    auditable.CreatedAt = now;
                    auditable.CreatedByUserId = actor;
                    break;

                case EntityState.Modified:
                    auditable.UpdatedAt = now;
                    auditable.UpdatedByUserId = actor;
                    // Chống ghi đè vết tạo khi update.
                    entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                    entry.Property(nameof(IAuditable.CreatedByUserId)).IsModified = false;
                    break;

                default:
                    break;
            }
        }
    }
}
