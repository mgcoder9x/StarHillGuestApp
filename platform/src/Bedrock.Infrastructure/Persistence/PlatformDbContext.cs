using System.Linq.Expressions;
using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Domain.Entities;
using Bedrock.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Bedrock.Infrastructure.Persistence;

/// <summary>
/// DbContext nền dùng chung (app dẫn xuất + khai <c>DbSet&lt;&gt;</c> của mình). Cung cấp CƠ CHẾ generic:
/// snake_case (wire ở <c>AddBedrockPersistence</c>), soft-delete query filter, audit set tự động,
/// concurrency-token map CÓ ĐIỀU KIỆN theo provider (chỉ Npgsql → xmin), và dispatch domain-event
/// trong <c>SaveChanges</c> CÙNG transaction (R33/CP14). Lõi KHÔNG biết entity nghiệp vụ (F3/F4).
/// </summary>
public abstract class PlatformDbContext : DbContext
{
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    protected PlatformDbContext(
        DbContextOptions options,
        IClock clock,
        ICurrentUser currentUser,
        IDomainEventDispatcher domainEventDispatcher)
        : base(options)
    {
        _clock = clock;
        _currentUser = currentUser;
        _domainEventDispatcher = domainEventDispatcher;
    }

    /// <summary>
    /// Trần số vòng dispatch domain-event trong một <c>SaveChanges</c> (design §7.5, R33.4). Chặn vòng
    /// vô hạn khi handler liên tục raise event mới. Override được để test biên (đặt trần nhỏ).
    /// </summary>
    protected virtual int MaxDomainEventDispatchDepth => 25;

    /// <summary>
    /// Tên filter soft-delete (EF Core 10 NAMED query filter — A-25). Module thêm filter riêng (vd tenant) bằng
    /// tên KHÁC sẽ COMPOSE (AND) với filter này thay vì GHI ĐÈ (unnamed filter thứ hai sẽ thay thế). Public để
    /// module/test tham chiếu nhất quán.
    /// </summary>
    public const string SoftDeleteFilterName = "SoftDelete";

    /// <summary>
    /// Điểm override chuẩn của EF (mọi <c>SaveChangesAsync</c> công khai đều đi qua đây). Trình tự (design §7.5):
    /// (1) vòng dispatch domain-event → (2) áp soft-delete + audit → (3) MỘT <c>base.SaveChangesAsync</c>
    /// → toàn bộ (state gốc + hiệu ứng handler) commit nguyên tử (CP14).
    /// </summary>
    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        // P1-01 (AD-094): FAILURE BOUNDARY bao TRỌN dispatch → soft-delete/audit → base save. Nếu BẤT KỲ bước nào
        // SAU khi đã dequeue event ném (dispatch, convention, hoặc base.SaveChanges), restore MỌI event đã dequeue
        // về đúng entity (thứ tự ngược) → KHÔNG mất event (attempt/retry sau vẫn dispatch được). Trước đây chỉ
        // dispatch-throw mới restore (AD-075) → SaveChanges/convention throw sau dispatch làm mất event âm thầm.
        var dequeued = new List<(Entity Entity, IDomainEvent[] Events)>();
        try
        {
            await DispatchDomainEventsAsync(dequeued, cancellationToken).ConfigureAwait(false);
            ApplySoftDelete();
            ApplyAudit();
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            for (var i = dequeued.Count - 1; i >= 0; i--)
            {
                dequeued[i].Entity.RestoreDomainEvents(dequeued[i].Events);
            }

            throw;
        }
    }

    /// <summary>
    /// Dispatch đồng bộ KHÔNG hỗ trợ: dispatch domain-event là bất đồng bộ (handler trả <see cref="Task"/>).
    /// Chặn tường minh để không có đường ghi nào bỏ qua dispatch (fail-loud thay vì mất event âm thầm).
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new NotSupportedException(
            "Dùng SaveChangesAsync: dispatch domain-event trong SaveChanges là bất đồng bộ (R33). "
            + "SaveChanges đồng bộ sẽ bỏ qua dispatch nên bị chặn tường minh.");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        var isNpgsql = Database.IsNpgsql();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            // Soft-delete: NAMED query filter (A-25) loại bản ghi đã xóa mềm. Named → compose với filter module
            // (vd tenant) thay vì bị ghi đè. Agnostic (SQLite test được).
            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType).HasQueryFilter(SoftDeleteFilterName, BuildIsNotDeletedFilter(clrType));
            }

            // Concurrency token: CHỈ Npgsql — map thủ công RowVersion → system column "xmin" (kiểu xid,
            // DB sinh khi add/update). Npgsql EF Core 10 đã BỎ UseXminAsConcurrencyToken() nên phải map tay.
            // KHÔNG map trên provider khác (SQLite không có xmin) → giữ test provider-agnostic chạy được.
            if (isNpgsql && typeof(IHasConcurrencyToken).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType)
                    .Property(nameof(IHasConcurrencyToken.RowVersion))
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
            }
        }
    }

    private static LambdaExpression BuildIsNotDeletedFilter(Type entityType)
    {
        // e => !e.IsDeleted  (entityType implement ISoftDeletable với property công khai)
        var parameter = Expression.Parameter(entityType, "e");
        var isDeleted = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
        return Expression.Lambda(Expression.Not(isDeleted), parameter);
    }

    private async Task DispatchDomainEventsAsync(List<(Entity Entity, IDomainEvent[] Events)> dequeuedSink, CancellationToken ct)
    {
        var depth = 0;
        while (true)
        {
            // Gom + clear domain event từ mọi entity đang track (handler có thể raise thêm ở vòng sau).
            var entitiesWithEvents = ChangeTracker.Entries<Entity>()
                .Select(e => e.Entity)
                .Where(e => e.DomainEvents.Count > 0)
                .ToArray();

            if (entitiesWithEvents.Length == 0)
            {
                return; // Hết event → kết thúc bình thường.
            }

            if (depth >= MaxDomainEventDispatchDepth)
            {
                // R33.4 / task 6.4: vượt trần → ném lỗi rõ, KHÔNG silent-drop event (caller SaveChangesAsync restore
                // toàn bộ event đã dequeue qua failure boundary — P1-01/AD-094).
                throw new InvalidOperationException(
                    $"Dispatch domain-event vượt trần {MaxDomainEventDispatchDepth} vòng — nghi vòng lặp vô hạn "
                    + "(handler liên tục raise event mới). Xem lại handler hoặc tăng MaxDomainEventDispatchDepth.");
            }

            var events = new List<IDomainEvent>();
            foreach (var entity in entitiesWithEvents)
            {
                var dequeued = entity.DomainEvents.ToArray();
                dequeuedSink.Add((entity, dequeued)); // ghi vào sink chung → failure boundary ở SaveChangesAsync restore.
                events.AddRange(dequeued);
                entity.ClearDomainEvents();
            }

            // KHÔNG try/catch cục bộ: restore do failure boundary bao ngoài (SaveChangesAsync) đảm nhận — phủ CẢ
            // dispatch-throw LẪN convention/base-save throw sau dispatch (AD-094 mở rộng AD-075).
            await _domainEventDispatcher.DispatchAsync(events, ct).ConfigureAwait(false);

            depth++;
        }
    }

    private void ApplySoftDelete()
    {
        var now = _clock.UtcNow;
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                // Xóa mềm: chuyển Deleted → Modified + đánh dấu (audit phía sau set UpdatedAt vì đã Modified).
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = now;
            }
        }
    }

    private void ApplyAudit()
    {
        var now = _clock.UtcNow;
        var actor = _currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedByUserId = actor;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedByUserId = actor;
                    // A-25: metadata TẠO do infrastructure sở hữu — caller sửa CreatedAt/CreatedByUserId KHÔNG được
                    // persist (đánh dấu IsModified=false → EF loại khỏi UPDATE). Bảo toàn bất biến "created bất biến".
                    entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                    entry.Property(nameof(IAuditable.CreatedByUserId)).IsModified = false;
                    break;
                default:
                    break;
            }
        }
    }
}
