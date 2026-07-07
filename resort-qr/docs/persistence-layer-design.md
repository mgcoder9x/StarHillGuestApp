# ResortQr — Thiết kế tầng EF Persistence (ĐÃ TRIỂN KHAI — 2026-07-04)

> **Trạng thái:** ✅ **IMPLEMENTED** (DB-free bằng SQLite; Postgres-specific hoãn tới khi có Docker). Xem `ai-notes` DEC-048/049/050/051, DEV-020/021, TK-036/037/038. Build 0W/0E, **110 test xanh**.
> **Cập nhật quan trọng so với bản thiết kế gốc bên dưới:**
> - ⚠️ **Npgsql EF Core 10 ĐÃ BỎ `UseXminAsConcurrencyToken()`** (verify từ assembly thật) → map THỦ CÔNG `RowVersion → xmin` (type `xid`, DB-generated, IsConcurrencyToken) trong `ResortQrDbContext`. Xem DEV-020.
> - Map `RefreshTokenRecord` TRỰC TIẾP làm EF entity (bỏ lớp `RefreshToken:Entity` riêng — DRY). Xem DEV-021/DEC-050.
> - Persistence nền wire TƯỜNG MINH qua `AddResortQrPersistence<TContext>()` + loại namespace Persistence khỏi Scrutor auto-scan. Xem DEC-049.
> - Pin transitive `SQLitePCLRaw.lib.e_sqlite3` 3.50.3 vá CVE-2025-6965. Xem DEC-051.
>
> _Bản thiết kế gốc (giữ nguyên để truy vết) như dưới._

---

# ResortQr — Thiết kế tầng EF Persistence (sẵn sàng code khi có DB)

> **Trạng thái:** THIẾT KẾ (chưa code) — chờ validate rồi triển khai. Bám `design/backend/03`§3, `04`, `05`§2, `23`§5/§7 của spec resort + bản chất generic của ResortQr.
> **Nguyên tắc:** base cung cấp **hạ tầng persistence GENERIC** (tái dùng mọi app); app tự khai entity + DbContext-derivation + migration của mình. KHÔNG bịa API — mọi điểm chưa chắc chắn đánh dấu ⚠️ verify-khi-implement.

## 0. Vì sao tách rõ generic vs app-specific

- **Generic (ResortQr cung cấp):** `ResortQrDbContext` base (conventions), `EfRepository<T>`, `EfUnitOfWork`, entity `RefreshToken` + config + `EfRefreshTokenStore` (refresh token là khái niệm app-agnostic), audit/soft-delete interceptor, design-time factory helper.
- **App cung cấp:** entity nghiệp vụ (User, Resort...), `DbSet<>` khai trong DbContext dẫn xuất, `IUserAuthStore` impl (map User của app → `AuthenticatedUser`), migration.
- Lý do: base không được biết entity nghiệp vụ; nhưng RefreshToken thì generic → base hiện thực luôn store atomic để mọi app hưởng cơ chế rotation chống race đúng.

## 1. Package cần thêm (Infrastructure) — pin khi implement

| Package | Vai trò | Ghi chú |
|---|---|---|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | provider production | dòng 10.x khớp EF Core 10 (TK-018) |
| `Microsoft.EntityFrameworkCore` | EF core | |
| `EFCore.NamingConventions` | snake_case tự động | ⚠️ verify hỗ trợ .NET 10/EF10 khi cài |
| `Microsoft.EntityFrameworkCore.Design` | migration/design-time | ở Api hoặc Infrastructure |
| (test) `Microsoft.EntityFrameworkCore.Sqlite` | test provider-agnostic KHÔNG cần Docker | |
| (test) `Testcontainers.PostgreSql` | test Postgres-specific (hoãn — cần Docker) | TK-036 |

## 2. `ResortQrDbContext` (base, app dẫn xuất)

```
public abstract class ResortQrDbContext : DbContext
{
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser _currentUser;
    protected ResortQrDbContext(DbContextOptions options, IDateTimeProvider clock, ICurrentUser currentUser) : base(options) {...}

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyConfigurationsFromAssembly(typeof(ResortQrDbContext).Assembly); // config của base (RefreshToken)
        ApplyConventions(b);
        base.OnModelCreating(b);
    }
}
```

### Conventions (áp trong OnModelCreating + SaveChanges)

| Convention | Cách làm | Provider |
|---|---|---|
| **snake_case** | `optionsBuilder.UseSnakeCaseNamingConvention()` (EFCore.NamingConventions) | agnostic |
| **soft-delete filter** | với mỗi entity `ISoftDeletable`: `b.Entity(t).HasQueryFilter(e => !((ISoftDeletable)e).IsDeleted)` (dựng expression) | agnostic ✅ SQLite test được |
| **audit** | override `SaveChangesAsync`: Added→set CreatedAt/CreatedByUserId; Modified→UpdatedAt/UpdatedByUserId (không đụng Created*) | agnostic ✅ SQLite test được |
| **soft-delete on delete** | trong SaveChanges: entry `ISoftDeletable` State=Deleted → chuyển Modified + IsDeleted=true, DeletedAt=now | agnostic ✅ |
| **concurrency xmin** | ⚠️ **CHỈ Npgsql**: `if (Database.IsNpgsql()) foreach IConcurrencyAware → b.Entity(t).UseXminAsConcurrencyToken()` | Npgsql-only → test bằng Testcontainers |
| **enum→string** | `HasConversion<string>()` + MaxLength | agnostic |
| **timestamptz/UTC** | DateTimeOffset UTC; Npgsql map timestamptz (offset=0) — TK-025 | Npgsql verify |

**Quyết định quan trọng (provider-conditional concurrency):** `UseXminAsConcurrencyToken()` là Npgsql-only → gọi CÓ ĐIỀU KIỆN theo `Database.IsNpgsql()`. Lý do gốc: cho phép **test provider-agnostic bằng SQLite** (không Docker) mà không vỡ; xmin thật kiểm bằng Testcontainers/Postgres. KHÔNG hardcode xmin vô điều kiện (sẽ chặn SQLite test).

## 3. `EfRepository<T>` + `EfUnitOfWork` (generic)

```
public sealed class EfRepository<T> : IRepository<T> where T : Entity {
    IQueryable<T> Query() => _ctx.Set<T>();
    ValueTask<T?> FindByIdAsync(id, ct) => _ctx.Set<T>().FindAsync([id], ct);
    Task<T?> FirstOrDefaultAsync(pred, ct); Task<bool> AnyAsync(pred, ct);
    void Add/Update/Remove(entity);   // Remove: nếu ISoftDeletable → interceptor xử lý ở SaveChanges
    // KHÔNG SaveChanges (DEV-001)
}
public sealed class EfUnitOfWork : IUnitOfWork {
    IRepository<T> Repository<T>() => cache-per-type;
    Task<int> SaveChangesAsync(ct) => _ctx.SaveChangesAsync(ct);
    Task<TR> ExecuteInTransactionAsync<TR>(action, ct) => strategy.ExecuteAsync(BeginTx → action → Commit / Rollback);
}
```
- `ExecuteInTransactionAsync` dùng `Database.CreateExecutionStrategy()` để tương thích retry (Npgsql). ✅ SQLite test rollback được.
- `DbUpdateConcurrencyException` từ SaveChanges → **propagate**; **cần thêm** map ở Api middleware: `DbUpdateConcurrencyException → 409 concurrency_conflict` (hiện middleware chỉ map unhandled→500; khi Infrastructure có EF, thêm mapping — nhưng để tránh Api phụ thuộc EF, dùng **`IExceptionMapper` extension point** hoặc bắt ở UoW và ném `ConcurrencyConflictException` của SharedKernel rồi Api map). **Quyết định:** UoW bắt `DbUpdateConcurrencyException` → ném `ConcurrencyConflictException` (định nghĩa ở SharedKernel) → Api middleware map → 409. Giữ Api KHÔNG phụ thuộc EF.

## 4. `RefreshToken` entity + `EfRefreshTokenStore` (atomic — fix expert #1)

```
public sealed class RefreshToken : Entity {   // KHÔNG AuditableEntity (04 §8: tạo/thu hồi, không xmin)
    public required Guid UserId; FamilyId; string TokenHash;
    public required DateTimeOffset ExpiresAt; CreatedAt;
    public DateTimeOffset? RevokedAt; Guid? ReplacedByTokenId; string? RevokedReason;
}
// Config: ToTable("refresh_token"); HasIndex(TokenHash) [ix_refresh_hash]; HasIndex(UserId); HasIndex(FamilyId); MaxLength cho hash/reason.
```

**`TryConsumeAsync` NGUYÊN TỬ (ExecuteUpdate — không read-then-write):**
```
public async Task<bool> TryConsumeAsync(Guid tokenId, DateTimeOffset now, string reason, Guid replacedBy, CancellationToken ct) {
    var affected = await _ctx.Set<RefreshToken>()
        .Where(r => r.Id == tokenId && r.RevokedAt == null)      // điều kiện consume-if-not-revoked
        .ExecuteUpdateAsync(s => s
            .SetProperty(r => r.RevokedAt, now)
            .SetProperty(r => r.RevokedReason, reason)
            .SetProperty(r => r.ReplacedByTokenId, replacedBy), ct);
    return affected == 1;   // 1 = chính lời gọi này thu hồi; 0 = request khác đã thu hồi (race/reuse)
}
```
- `ExecuteUpdateAsync` sinh **một câu `UPDATE ... WHERE ... AND revoked_at IS NULL`** chạy nguyên tử ở DB → 2 request đồng thời chỉ 1 câu update ăn 1 row (row-level lock) → đúng 1 thắng. Đây là fix GỐC race (không lock ứng dụng).
- `RevokeFamilyAsync`: `ExecuteUpdateAsync` WHERE FamilyId AND RevokedAt==null.
- ⚠️ verify: `ExecuteUpdateAsync` hỗ trợ ở Npgsql (EF7+) **và** SQLite (EF7+) → test atomic-consume-rowcount bằng SQLite được (không Docker); race đa-connection thật kiểm bằng Testcontainers.
- `AddAsync`: `_ctx.Add(record)` (stage). `UpdateAsync` (logout): `ExecuteUpdate` hoặc stage — dùng ExecuteUpdate conditional cho nhất quán.

## 5. Design-time factory + migration (23 §7)

- `AppDbContextFactory : IDesignTimeDbContextFactory<TDbContext>` — app cung cấp (vì DbContext cụ thể ở app). Base có thể cung cấp helper base.
- Migration nền của base: bảng `refresh_token` + index. App thêm bảng của app trong migration riêng (per-wave — 04 §4). `dotnet ef migrations add` chạy KHÔNG cần DB đang chạy.

## 6. Chiến lược TEST (đóng phần lớn expert #6 KHÔNG cần Docker)

| Hành vi | SQLite in-memory (Docker-free) | Testcontainers/Postgres (cần Docker — TK-036) |
|---|---|---|
| Audit set CreatedAt/UpdatedAt/actor | ✅ | (lặp lại để chắc) |
| Soft-delete filter loại IsDeleted | ✅ | |
| Repository/UoW transaction rollback all-or-nothing | ✅ | |
| `TryConsumeAsync` trả rowcount đúng (consume-if-not-revoked) | ✅ (logic + 1 win) | ✅ race đa-connection THẬT |
| RevokeFamily | ✅ | |
| `xmin` concurrency → 409 | ❌ (SQLite không có xmin) | ✅ BẮT BUỘC |
| partial unique index chặn ghi (1 token active/phòng...) | ❌ (cú pháp/scope khác) | ✅ BẮT BUỘC (Property B3) |
| snake_case DDL, timestamptz | ⚠️ khác | ✅ |

- **Hòa giải DEC-021 ("không InMemory"):** DEC-021 cấm **EF InMemory provider** (không enforce ràng buộc → false-green). **SQLite là DB quan hệ THẬT** (enforce unique/transaction/UPDATE...WHERE) → dùng cho hành vi **provider-agnostic** là hợp lệ. Nhưng **mọi thứ Postgres-specific (xmin, partial index) VẪN phải Testcontainers** — không được kết luận đúng từ SQLite. Ghi nhãn rõ mỗi test thuộc nhóm nào.

## 7. Ảnh hưởng tầng khác khi implement

- SharedKernel: thêm `ConcurrencyConflictException` (để UoW ném, Api map 409) — hoặc dùng `Result` ở use case. Chốt: exception + middleware map (nhất quán với 03 §1 `MapException`).
- Api `ExceptionHandlingMiddleware`/`MapException`: thêm case `ConcurrencyConflictException → 409 concurrency_conflict` (KHÔNG phụ thuộc EF trực tiếp — phụ thuộc SharedKernel exception).
- `/health/ready`: thêm Npgsql health check (`AddNpgSql` hoặc `AddDbContextCheck`) tag "ready", timeout 5s (Req 14.6-7).
- Options: thêm `ConnectionStrings:Postgres` + validate-on-start ở Production (DEC-020).

## 8. Readiness checklist (khi có DB)
1. Pin version Npgsql/EFCore.NamingConventions/EFCore.Sqlite/Testcontainers (verify tương thích .NET 10).
2. Verify `timestamptz`+`DateTimeOffset` offset=0 (TK-025), `ExecuteUpdateAsync` trên Npgsql+SQLite.
3. Viết `ResortQrDbContext` + interceptor + conventions (xmin conditional Npgsql).
4. `EfRepository`/`EfUnitOfWork` + `ConcurrencyConflictException` + Api map 409.
5. `RefreshToken` + config + `EfRefreshTokenStore` (ExecuteUpdate atomic).
6. Migration nền (refresh_token). Design-time factory (ở app mẫu).
7. Test SQLite (agnostic) + Testcontainers (xmin/partial-index/race thật).
8. `/health/ready` + connection string options.

## 9. Truy vết
- 03 §1/§3, 04 §3/§7/§8/§9, 05 §2, 23 §5/§7; Req 3, 5, 8, 14.6-7; DEC-005/006/020/021/043(#1); TK-025/034/036.
