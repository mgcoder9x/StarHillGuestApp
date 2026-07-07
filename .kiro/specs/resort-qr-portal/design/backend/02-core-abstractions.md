# 02 — Core Abstractions (Low-Level Design)

> **File authoritative cho:** các "viên gạch" nền (Entity, Result/Error, Repository/UnitOfWork, UseCase, cross-cutting ports). Code C# thật (.NET 10), có thể copy làm điểm khởi đầu.

## 1. Base entity & conventions

Reference có `Entity<TKey>`, `AuditEntity<TKey>`, `SoftDeleteEntity`, `FullyEntity<TKey>` khá tốt. Base mới giữ ý tưởng nhưng gọn hơn, thêm **concurrency token** ngay từ nền.

```csharp
namespace ResortQr.Domain.Common;

// Khóa chính: uuid, sinh client-side bằng UUIDv7 (time-ordered) để có Id ngay lúc tạo.
public abstract class Entity
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
}

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    Guid? CreatedByUserId { get; set; }
    Guid? UpdatedByUserId { get; set; }
}

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
}

// Concurrency token — PostgreSQL xmin ánh xạ qua uint (không tốn cột thật).
public interface IConcurrencyAware
{
    uint RowVersion { get; set; }
}

public abstract class AuditableEntity : Entity, IAuditable, IConcurrencyAware
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public uint RowVersion { get; set; }   // map tới xmin
}
```

**Cải tiến:** reference dùng `TKey` generic cho mọi entity. Thực tế dự án này **luôn dùng `Guid`**. Cố định `Guid` bỏ generic rườm rà, khóa ngoại đồng nhất. Audit set tự động trong `SaveChanges` (xem `03-cross-cutting-concerns.md`).

**Quyết định khóa chính (firm, có lý do chính xác — 2026-07-03):** kiểu cột luôn là **`uuid`**; sinh **client-side bằng `Guid.CreateVersion7()`**.

- **Vì sao client-side (không để DB sinh):** Id có sẵn **ngay lúc tạo entity**, trước khi lưu → set FK/đồ thị đối tượng trong cùng transaction, làm idempotency key, ghi log, trả `201 Created` kèm Location mà không cần round-trip đọc lại DB. Đây là lợi ích kiến trúc/bảo trì, quan trọng hơn chênh lệch kích thước index ở quy mô resort.
- **Vì sao v7 (không v4):** v7 time-ordered → prefix timestamp nằm ở byte cao. PostgreSQL `uuid` sort theo byte canonical; Npgsql serialize `Guid` theo thứ tự canonical (vì `id::text` trong PG phải khớp `guid.ToString()`), nên v7 **được kỳ vọng** cho index B-tree tuần tự (theo benchmark PG: index nhỏ ~26–27%, scan có thứ tự nhanh ~3x).
- **Điểm chưa tự kiểm chứng (trung thực):** có một báo cáo cộng đồng cho rằng byte-layout cụ thể của `Guid.CreateVersion7()` không đạt locality trên PG. Vì chưa tự benchmark → coi đây là **giả định cần xác nhận một lần**, KHÔNG khẳng định chắc.
- **Rủi ro được kiềm chế (đảo rẻ):** cột luôn là `uuid`. Nếu benchmark một lần (≈100k insert, đo index bloat) cho thấy mất locality → **chỉ đổi nguồn sinh** sang `DEFAULT uuidv7()` của **PostgreSQL 18** (`HasDefaultValueSql("uuidv7()")`) — **cùng kiểu cột, KHÔNG đổi schema/dữ liệu**, chỉ khác "ai sinh giá trị". Xem `03-tradeoffs.md` TRD-003, `../../ai-notes/04-things-to-know.md` TK-002.

## 2. Result & Error (thay throw exception cho lỗi nghiệp vụ)

Reference trả `IHttpResponse`/`HttpResponse<T>` trộn khái niệm HTTP vào tầng domain. Base mới tách: **Application trả `Result<T>` trung lập**, Api dịch sang HTTP/ProblemDetails.

```csharp
namespace ResortQr.SharedKernel;

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error NotFound(string code, string msg)   => new(code, msg, ErrorType.NotFound);
    public static Error Conflict(string code, string msg)   => new(code, msg, ErrorType.Conflict);
    public static Error Validation(string code, string msg) => new(code, msg, ErrorType.Validation);
    public static Error Forbidden(string code, string msg)  => new(code, msg, ErrorType.Forbidden);
    public static Error Rate(string code, string msg)       => new(code, msg, ErrorType.RateLimited);
}

public enum ErrorType { Validation, NotFound, Conflict, Forbidden, RateLimited, Unexpected }

public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }
    private Result(bool ok, T? value, Error? error) { IsSuccess = ok; Value = value; Error = error; }

    public static Result<T> Ok(T value) => new(true, value, null);
    public static Result<T> Fail(Error error) => new(false, default, error);
    public static implicit operator Result<T>(Error error) => Fail(error);
}
```

Bộ **mã lỗi ổn định** (hợp đồng guest, đồng bộ với `docs/.../design.md`) khai báo tập trung:

```csharp
public static class AppErrors
{
    public static readonly Error QrInvalid       = Error.NotFound("qr_invalid", "QR không hợp lệ.");
    public static readonly Error QrRevoked       = Error.NotFound("qr_revoked", "QR đã bị thu hồi.");
    public static readonly Error RoomInactive    = Error.Conflict("room_inactive", "Phòng không khả dụng.");
    public static readonly Error RuleAckRequired = Error.Forbidden("rule_ack_required", "Cần xác nhận nội quy.");
    public static readonly Error SessionExpired  = Error.Forbidden("session_expired", "Phiên đã hết hạn, vui lòng quét QR lại.");
    public static readonly Error RateLimited     = Error.Rate("rate_limited", "Thao tác quá nhanh, thử lại sau.");
    public static readonly Error MessageTooLong  = Error.Validation("message_too_long", "Tin nhắn quá dài.");
    public static readonly Error Concurrency     = Error.Conflict("concurrency_conflict", "Nội dung đã đổi, tải lại.");
    // ... (danh mục ĐẦY ĐỦ + ánh xạ HTTP + khi nào phát + FE xử lý: xem 14-error-catalog.md)
}
```

> **Catalog đầy đủ (authoritative):** `14-error-catalog.md` — liệt kê **toàn bộ** code (guest/admin/chung), ánh xạ HTTP, thời điểm phát và cách FE xử lý. Đây là hợp đồng BE↔FE; đừng để `// ...` mơ hồ.

## 3. Repository & Unit of Work (SỬA lỗi lớn của reference)

**Bản chất vấn đề (đã kiểm chứng):** `BaseRepository.AddAsync/UpdateAsync/DeleteAsync/...` gọi `SaveChangesAsync()` **ngay trong mỗi thao tác**; `UnitOfWork` **không có** phương thức `SaveChanges` (chỉ `CreateTransaction`/`GetRepository`); không có `CancellationToken`. Hệ quả: không gom được nhiều thay đổi vào một lần lưu → mất tính nguyên tử; `IUnitOfWork` gần vô nghĩa; `Program.cs` phải đăng ký UoW kèm `// TODO: Remove`.

**Cải tiến:** Repository **chỉ thao tác ChangeTracker** (Add/Update/Remove/Query), **không** SaveChanges. Chỉ `IUnitOfWork.SaveChangesAsync()` mới ghi DB. Transaction tường minh khi cần nhiều lần save.

```csharp
namespace ResortQr.Application.Abstractions;

public interface IRepository<TEntity> where TEntity : Entity
{
    IQueryable<TEntity> Query();                                  // read-only; caller có thể .AsNoTracking()
    ValueTask<TEntity?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    void Add(TEntity entity);                                     // KHÔNG save
    void Update(TEntity entity);                                  // KHÔNG save
    void Remove(TEntity entity);                                  // soft-delete nếu ISoftDeletable
}

public interface IUnitOfWork
{
    IRepository<TEntity> Repository<TEntity>() where TEntity : Entity;
    Task<int> SaveChangesAsync(CancellationToken ct = default);   // điểm ghi DB DUY NHẤT
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default);
}
```

Use case điển hình (publish nội quy — nhiều bảng, một transaction):

```csharp
public async Task<Result<int>> PublishAsync(Guid resortId, string? changeNote, CancellationToken ct)
{
    return await _uow.ExecuteInTransactionAsync(async token =>
    {
        var draft = await _uow.Repository<RuleSet>().FirstOrDefaultAsync(x => x.ResortId == resortId, token);
        Guard.NotNull(draft, AppErrors.QrInvalid);              // ví dụ minh họa

        var current = await _uow.Repository<RulePublication>()
            .FirstOrDefaultAsync(x => x.ResortId == resortId && x.IsCurrent, token);
        if (current is not null) current.IsCurrent = false;      // hạ cờ bản cũ

        var next = SnapshotFromDraft(draft, version: (current?.Version ?? 0) + 1);
        _uow.Repository<RulePublication>().Add(next);

        await _uow.SaveChangesAsync(token);                      // ghi 1 lần, nguyên tử
        return next.Version;
    }, ct);
}
```

## 4. UseCase base & pagination

Giữ khái niệm **use-case-per-operation** của reference (interface nhỏ, dễ test, dễ phân quyền), nhưng bỏ `BaseService` phụ thuộc EF; thay bằng inject port.

```csharp
namespace ResortQr.Application.Common;

public interface IUseCase { }   // marker để test/log; không bắt buộc kế thừa

public sealed record PagedRequest(int Page = 1, int PageSize = 20)
{
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafeSize => PageSize is < 1 or > 100 ? 20 : PageSize;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);
```

Ví dụ interface use case (module GuestAccess):

```csharp
public interface IResolveTokenUseCase : IUseCase
{
    Task<Result<ResolveResponse>> ExecuteAsync(string token, GuestContextInput ctx, CancellationToken ct);
}
```

## 5. Cross-cutting ports (loại bỏ tính bất định)

```csharp
namespace ResortQr.Application.Abstractions;

public interface IDateTimeProvider : ISingletonService     // thay DateTimeOffset.UtcNow rải rác → test được
{ DateTimeOffset UtcNow { get; } }

public interface ITokenGenerator : ISingletonService       // sinh PublicToken CSPRNG
{ string NewPublicToken(int bytes = 32); }                 // base64url, không đoán được

public interface IHtmlSanitizer : ISingletonService        // sanitize nội quy/FAQ (allowlist)
{ string Sanitize(string rawHtml); }

public interface ICurrentUser : IScopedService             // ngữ cảnh admin/staff
{ Guid? UserId { get; } string? Role { get; } bool IsAuthenticated { get; } }

public interface IGuestContext : IScopedService            // ngữ cảnh guest (từ cookie)
{ Guid? GuestSessionId { get; } string? SessionKey { get; } }

public interface IRealtimeNotifier : IScopedService        // trừu tượng SignalR để use case không phụ thuộc Hub
{
    Task NotifyStaffAsync(Guid resortId, RealtimeEvent evt, CancellationToken ct);
    Task NotifyConversationAsync(Guid conversationId, RealtimeEvent evt, CancellationToken ct);
    Task NotifyVisitEndedAsync(Guid visitId, CancellationToken ct);   // evict guest realtime khi EndVisit (21 §4.1)
}

public interface IQrService : ISingletonService
{ byte[] RenderPng(string url); }

public interface IPdfService : ITransientService
{ byte[] RenderRoomLabels(IReadOnlyList<RoomLabel> labels); }
```

> Nhờ các port này, **toàn bộ use case test được bằng unit test thuần** (giả lập clock/token/sanitizer), đúng mục tiêu chất lượng ở `00-overview-and-goals.md`.

## 6. Read model (CQRS-lite) & quy tắc realtime post-commit

Bổ sung từ audit kiến trúc (`11-architecture-review-and-gaps.md`):

**6.1 Đọc phức tạp qua query service, không xuyên `IQueryable`:** `IRepository<T>.Query()` chỉ dùng cho ghi + đọc đơn giản. Truy vấn phức tạp (dashboard stats, inbox gom theo phòng + unread) đi qua **read query service** riêng: interface khai ở `Application` (ví dụ `IInboxQueries`, `IDashboardQueries`) trả **DTO read-model**; implement ở `Infrastructure` bằng EF projection (`Select` → DTO, `AsNoTracking`). Tránh trả `IQueryable` xuyên tầng cho read phức tạp (giảm leaky abstraction + tối ưu SQL).

```csharp
// Application/Abstractions
public interface IInboxQueries : IScopedService
{
    Task<PagedResult<ConversationListItem>> GetByRoomAsync(InboxFilter filter, CancellationToken ct);
}
// Infrastructure implement bằng EF: _db.Conversations.AsNoTracking().Select(x => new ConversationListItem{...})
```

**6.2 Realtime chỉ bắn SAU commit:** `IRealtimeNotifier` **luôn được gọi sau khi `SaveChangesAsync`/transaction commit thành công** — không gọi trong transaction (tránh thông báo rồi DB rollback → client thấy dữ liệu "ma"). Nếu cần đảm bảo at-least-once mạnh hơn về sau → Outbox pattern (không thuộc base).
