# Module design — Rooms (Wave B/C, module thứ 2)

> **Design-first** cho module `Rooms` trên Bedrock (`starhill/`). Đọc kèm `../design.md` §4.3. WHAT: `docs/resort-qr-portal/`
> Req 1 (QR→phòng), Req 7 (quản lý phòng + sinh QR), CP1/CP2. Nguồn PORT: `resort-qr/src/.../Rooms/*` + `Infrastructure/Rooms/QrCoderQrService` + `Configurations/RoomConfigurations`. Mọi mệnh đề dựa trên ĐỌC MÃ thật (không suy đoán).

## 0. Vì sao Rooms là module kế tiếp (SỬA drift)
Dependency graph (`docs/resort-qr-portal/tasks.md` wave 3/4 + `../design.md` §10): **GuestAccess.resolve token → phòng phụ thuộc Rooms**. README trước ghi "next = GuestAccess" là SAI thứ tự → sửa: **Rooms trước, GuestAccess sau**. Rooms tự chứa (Room CRUD + RoomQrToken issue/rotate + QR PNG) và lộ `IRoomTokenResolver` cho GuestAccess/Housekeeping.

## 1. GAP NĂNG LỰC NỀN (quan trọng — QR-AD-010): base thiếu dịch unique-violation
- **Bằng chứng (verify)**: `EfUnitOfWork.SaveChangesAsync` CHỈ bắt `DbUpdateConcurrencyException` → `ConcurrencyConflictException`. Grep base: `DbUpdateException`/`23505`/`PostgresException`/`UniqueConstraint` = **0 match**. `Bedrock.Domain/Results` KHÔNG có `UniqueConstraintViolationException` (resort-qr có, trong SharedKernel).
- **Vì sao cần**: use case Rooms bắt unique-violation RACE-SAFE (ux_room_number khi 2 admin tạo trùng số phòng; ux_qr_active khi 2 admin rotate) → trả `RoomNumberTaken`/`QrGenerationFailed` deterministic. Application ⊥ EF → KHÔNG được bắt `DbUpdateException` (EF). ⇒ cần exception TRUNG LẬP ở `Bedrock.Domain` mà Application bắt được, do `EfUnitOfWork` (Infrastructure) dịch. Đây là **năng lực nền tái dùng** (song song `ConcurrencyConflictException`), KHÔNG phải nghiệp vụ QR.
- **Quyết định (QR-AD-010)**: bổ sung ở BASE `platform/` (đúng QR-N-002 — base thiếu năng lực nền thì thêm ở base rồi copy), KHÔNG nhồi vào module:
  1. `Bedrock.Domain/Results/UniqueConstraintViolationException.cs` (neutral, mirror ConcurrencyConflictException; mang `ConstraintName?` để module phân biệt ràng buộc nào).
  2. `EfUnitOfWork.SaveChangesAsync`: thêm catch `DbUpdateException` mà inner là Postgres unique-violation (SqlState `23505`) → ném `UniqueConstraintViolationException(constraintName)`. Thứ tự catch: `DbUpdateConcurrencyException` trước (nó là con của `DbUpdateException`), rồi `DbUpdateException`.
     - Npgsql: `PlatformDbContext` đã dùng `Database.IsNpgsql()` ⇒ Bedrock.Infrastructure đã tham chiếu Npgsql (VERIFY csproj lúc impl). Detect qua `ex.InnerException is Npgsql.PostgresException { SqlState: "23505" } pg` → `pg.ConstraintName`.
  3. **Guard test base** (keystone): Testcontainers — insert vi phạm unique index → `UniqueConstraintViolationException` (không phải DbUpdateException trần). Đặt ở `platform/tests/Bedrock.Infrastructure.Tests`.
  4. Verify `platform\scripts\vp.cmd` (build 0-warning + test) → rồi **copy** các file Bedrock.* đã đổi sang `starhill/` (giữ 2 bản đồng bộ; ghi rõ ở journal). Cập nhật base journal (`platform-base`) AD mới cho năng lực này.
- **Reversibility**: Medium. Đây là bước đụng base — làm cẩn thận, verify base trước/sau.

## 2. Cấu trúc 5-project (mirror Identity/ResortConfig)
```
starhill/src/Modules/Rooms/
  Rooms.Domain          → Bedrock.Domain
  Rooms.Contracts       → Bedrock.Messaging.Contracts   (+ lộ IRoomTokenResolver + DTO read)
  Rooms.Application     → Rooms.Domain + Rooms.Contracts + ResortConfig.Contracts + Bedrock.Application (+FluentValidation)
  Rooms.Infrastructure  → Rooms.Application + Bedrock.Infrastructure (+ EFCore, EFCore.Design, QRCoder)
  Rooms.Api             → Rooms.Application + Bedrock.Api   [SLICE SAU — cần Identity auth + Role→policy QR-AD-005]
```
> **Cross-module MỚI**: `Rooms.Application` ref **`ResortConfig.Contracts`** (đọc `GuestWebBaseUrl` cho RenderQrPng) — đây là cross-module Id/Contracts hợp lệ (QR-AD-002). Kích hoạt query port ResortConfig đã hoãn (I10 — nay có consumer thật).

## 3. Entities (`Rooms.Domain`) — port SharedKernel→Bedrock

| Entity | Base | Ghi chú |
|---|---|---|
| `Room` | `AuditableEntity`, `ISoftDeletable`, **`IHasConcurrencyToken`** | ResortId, RoomNumber, Building?, Floor?, Status(enum Active/Inactive/Maintenance), IsDeleted/DeletedAt. (resort-qr comment nói "concurrency xmin" nhưng chỉ `: AuditableEntity, ISoftDeletable` — Bedrock cần implement `IHasConcurrencyToken` tường minh nếu muốn xmin; Room CRUD có sửa đồng thời → GIỮ concurrency: thêm `IHasConcurrencyToken`.) |
| `RoomQrToken` | `Entity` | RoomId, Token (plaintext, unique global), TokenPreview, Status(Active/Revoked), Version, CreatedAt, CreatedByUserId?, RevokedAt?/RevokedByUserId?/RevocationReason?. KHÔNG auto-expire. |
| `RoomStatus`, `RoomQrTokenStatus` | enum (lưu string) | 'Active' là hợp đồng với partial index. |

## 4. Use cases (`Rooms.Application`) — port
- `CreateRoomUseCase` (`IUseCase<CreateRoomInput,CreateRoomResult>`): tạo Room + RoomQrToken Active nguyên tử (1 SaveChanges). Bắt `UniqueConstraintViolationException` → phân biệt: constraint `ux_room_number`→`RoomNumberTaken`, khác→`QrGenerationFailed`. Dùng **`ITokenGenerator`** (base) qua `RoomTokenFactory` (port nội bộ: retry sinh token unique + Mask).
- `UpdateRoomUseCase`, `ChangeRoomStatusUseCase`, `DeleteRoomUseCase`(soft) (`ICommandUseCase<>`).
- `RotateRoomTokenUseCase`: revoke Active + insert Active mới nguyên tử; race ux_qr_active → `QrGenerationFailed`.
- `RenderRoomQrPngUseCase`: đọc `GuestWebBaseUrl` qua **`ResortConfig.Contracts.IResortSettingsQuery`** (KHÔNG qua repository ResortSettings — khác resort-qr, vì khác module/schema); validate https tuyệt đối; URL `{base}/r/{token}`; render qua `IQrService`.
- Ports Bedrock: **`ITokenGenerator`**, **`IClock`**(via IDateTimeProvider? — Bedrock dùng `IClock`; port map: dùng `IClock`), **`ICurrentUser`**, `IUnitOfWork`/`IRepository`. Validators FluentValidation (whitelist).
- **Đổi so với resort-qr**: `IDateTimeProvider`→`IClock`; `ITokenGenerator.NewToken()` — VERIFY tên method của Bedrock `ITokenGenerator` lúc impl (có thể khác); `ISingletonService`/`IScopedService` marker của Bedrock (auto-scan) — dùng marker Bedrock cho use case (verify Bedrock auto-scan use case thế nào; Identity đăng ký use case THỦ CÔNG trong AddIdentityInfrastructure — nên Rooms cũng đăng ký thủ công trong AddRoomsInfrastructure thay vì dựa marker). 

## 5. i18n/QR service + package
- `IQrService` (port Application, mirror resort-qr — `RenderPng(url)`) + impl `QrCoderQrService` (Infrastructure, QRCoder PngByteQRCode, ECC Q, 20px). Đăng ký singleton thủ công.
- **QRCoder package**: thêm CPM `Directory.Packages.props` — reuse version từ `resort-qr/Directory.Packages.props` (KHÔNG bịa; verify lúc impl bằng đọc file đó / `dotnet add`).

## 6. Persistence (`Rooms.Infrastructure`)
- `RoomsDbContext : PlatformDbContext` schema `rooms`; DbSet Room/RoomQrToken; KHÔNG outbox (chưa phát event). Factory design-time mirror.
- EF config (port `RoomConfigurations`): Room status enum→string, FK ResortId Restrict; **partial unique `ux_room_number`** trên `(ResortId, RoomNumber) WHERE NOT is_deleted` (đặt ở DbContext Npgsql-aware — resort-qr đặt ở AppDbContext); RoomQrToken: unique `ux_qrtoken_token`(Token), **partial unique `ux_qr_active`** trên `(RoomId) WHERE status='Active'`.
  - LƯU Ý cross-schema FK: Room.ResortId → ResortConfig.resort (schema khác). Bedrock cấm FK chéo schema (QR-AD-002) → **BỎ FK `Room→Resort`** (resort-qr có FK vì cùng DbContext). Chỉ giữ ResortId là Guid trần; single-resort nên tính hợp lệ đảm bảo ở app (lấy resortId từ ResortConfig qua Contracts khi tạo room).
- Migration `InitialCreate` (schema rooms) + guard: CP2 integration (1 active/phòng, rotate giữ lịch sử).
- Query `EfRoomQueries` (read-model, DTO RoomListItem + ActiveTokenPreview) — Contracts `IRoomQuery`.

## 7. Contracts lộ ra (`Rooms.Contracts`)
- **`IRoomTokenResolver`**: `ResolveActiveTokenAsync(string token) → RoomResolution?` (RoomId, RoomNumber, Building, Floor, ResortId, RoomStatus) — cho **GuestAccess.resolve** + **Housekeeping.complete-by-token**. Token lỗi/revoked/room không Active → null/So (không lộ phòng khác — CP1).
- `IRoomQuery` (admin read list/by-id) — hoặc để Application; đặt ở Contracts nếu module khác cần (Dashboard đếm active rooms → cần). Lộ `IRoomStats.CountActiveAsync()` cho Dashboard.
- DTO thuần (record).

## 8. Correctness Properties → guard

### Property 1: Phân giải token an toàn (phần Rooms)
`IRoomTokenResolver` trả null cho token revoked/không tồn tại/phòng vô hiệu; URL QR không chứa số phòng trần ({base}/r/{token}). **Guard**: Rooms integration (resolver) + unit (URL build). (Phần visit/resolve HTTP thuộc GuestAccess.)
**Validates: Requirements 1.1, 1.3, 1.4, 1.6, 7.4**

### Property 2: QR token — một active/phòng & không auto-expire
Partial unique `ux_qr_active`; rotate revoke cũ + insert mới nguyên tử, giữ lịch sử; không cơ chế auto-expire. **Guard**: Rooms integration (Testcontainers): create→1 active; rotate→cũ Revoked + mới Active + Version++; 2 rotate đua → 1 thắng (UniqueConstraintViolationException→QrGenerationFailed).
**Validates: Requirements 1.5, 7.4, 7.5**

> Guard base (QR-AD-010): `UniqueConstraintViolationException` dịch đúng — `platform/tests/Bedrock.Infrastructure.Tests` (Testcontainers).

## 9. Build slices
- **B-Rooms.0 (base capability)**: QR-AD-010 ở `platform/` (UniqueConstraintViolationException + EfUnitOfWork + guard test) → verify `vp` base → copy Bedrock.* sang `starhill/` → verify starhill `vp all`.
- **B-Rooms.1**: ResortConfig query port `IResortSettingsQuery` (Contracts) + `EfResortSettingsQuery` (Infra) + đăng ký + unit/integration nhỏ (đọc settings).
- **B-Rooms.2**: Rooms Domain/Contracts/Application/Infrastructure + QRCoder + migration + đăng ký use case/service thủ công (AddRoomsInfrastructure) + Host wiring (conn `Rooms`) + CP2 integration + ModuleBoundary (StarHill.ArchitectureTests).
- **B-Rooms.3**: `Rooms.Api` (admin CRUD Admin-only/read Staff + qr.png + rotate-token) — cần Identity auth + Role→policy (QR-AD-005). Gộp với slice Identity-auth.

## 10. Điều kiện "design tốt" (tự-valid)
- [x] Gap unique-violation xác minh bằng grep (0 match) — không suy đoán.
- [x] Cross-module read GuestWebBaseUrl → kích hoạt IResortSettingsQuery (consumer thật, hết "speculative").
- [x] Cross-schema FK Room→Resort BỎ (Bedrock cấm) — ResortId Guid trần.
- [x] (impl) VERIFY: Bedrock.Infrastructure ref Npgsql (B-Rooms.0) ✅; tên method `ITokenGenerator.NewToken(int=32)` (B-Rooms.2b-i) ✅; cách đăng ký use case (thủ công như Identity — B-Rooms.2b-i/ii) ✅.
- [x] (impl) QRCoder version từ resort-qr = **1.8.0** (B-Rooms.2b-ii — verify đọc resort-qr/Directory.Packages.props, không bịa) ✅.

> **Trạng thái build (2026-07-11):** B-Rooms.0/1/2a/2b-i/2b-ii XONG. Rooms use case hoàn tất (CRUD/rotate/render). `vp all` 299 test (277 pass / 22 skip Docker / 0 fail) + `vp journal` 5/5 QR + 5/5 base. NEXT: B-Rooms.3 (`Rooms.Api`, cần Identity auth + Role→policy QR-AD-005) — xem QR-N-015.
