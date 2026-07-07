# ResortQr.Infrastructure — Thiết kế EF mapping (AppDbContext + config + store) — design-first

> **Trạng thái:** THIẾT KẾ để triển khai; test Docker-free bằng SQLite (DB quan hệ THẬT). Bám `04` §5/§7/§9, `06`, `23` §7, `12`, `16`, `24`.
> **Phụ thuộc nền (đã có):** `ResortQrDbContext` (audit/soft-delete/xmin-Npgsql/snake_case convention), `EfRepository<T>`/`EfUnitOfWork`, `RefreshTokenConfiguration`, `AddResortQrPersistence<TContext>()`.

## 0. Nguyên tắc
- Ràng buộc kỹ thuật (MaxLength/enum→string/index/partial-unique/CHECK/FK) đặt ở **EF config** (Infrastructure), KHÔNG ở Domain (POCO sạch — DEC-053).
- **snake_case BẮT BUỘC** ở AppDbContext options (`UseSnakeCaseNamingConvention`) — filter partial index tham chiếu cột snake_case. App + test đều bật (README đã ghi).
- **Reference-by-id (không nav prop):** FK cấu hình tường minh `HasOne<T>().WithMany().HasForeignKey(x=>x.FkId).OnDelete(Restrict)` (`04` §7.3) — enforce toàn vẹn + xóa RESTRICT, không cần navigation.

## 1. `AppDbContext : ResortQrDbContext`
- DbSet: `Resorts, ResortSettingsSet, ResortLanguages, AppUsers, Rooms, RoomQrTokens, GuestSessions, GuestVisits`.
- Ctor `(DbContextOptions<AppDbContext>, IDateTimeProvider, ICurrentUser)` → base.
- `OnModelCreating`: gọi `base.OnModelCreating(mb)` — base tự (a) `ApplyConfigurationsFromAssembly(Infrastructure)` → **tự nạp cả config app** (cùng assembly) + `RefreshTokenConfiguration`; (b) `ApplyConventions` (soft-delete filter cho Room, xmin cho Room/AppUser/ResortSettings vì DbSet đã đưa entity vào model TRƯỚC OnModelCreating). Sau đó **thêm 2 partial index phụ thuộc BOOL provider** (xem §3).

## 2. Config mỗi entity (parameterless → base auto-nạp)
Mỗi `IEntityTypeConfiguration<T>`: `ToTable(snake)`, `Id.ValueGeneratedNever()` (UUIDv7 client-side), enum `HasConversion<string>().HasMaxLength(16)`, MaxLength chuỗi, FK Restrict, index.

| Entity | Cột chính (MaxLength) | Index (§5/§7) | FK (Restrict) |
|---|---|---|---|
| Resort | name(200), timezone(64), logo_url(512) | — | — |
| ResortSettings | guest_web_base_url(512) | **unique** `ux_resort_settings_resort`(resort_id) → 1-1 (D5) + CHECK ck_portal_window/ck_idle_expiry/ck_msg_len (`04` §7.4) | resort_id→resort |
| ResortLanguage | code(16), display_name(64) | unique `ux_lang_code`(resort_id,code); **partial** `ux_lang_default`(resort_id) WHERE is_default (§3) | resort_id→resort |
| AppUser | email(256), display_name(128), password_hash(256), role(str16) | **unique** `ux_appuser_email`(email) (`04` §7.1) | resort_id→resort |
| Room | room_number(20), building(50), status(str16) | **partial unique** `ux_room_number`(resort_id,room_number) WHERE is_deleted=false (§3) | resort_id→resort |
| RoomQrToken | token(64), token_preview(32), status(str16), revocation_reason(200) | **unique** `ux_qrtoken_token`(token) (`04`§7.1); **partial unique** `ux_qr_active`(room_id) WHERE status='Active' | room_id→room |
| GuestSession | session_key_hash(64), preferred_language(16) | **unique** `ux_guestsession_key`(session_key_hash) | — |
| GuestVisit | status(str16) | **partial unique** `ux_visit_active`(guest_session_id,room_id) WHERE status='Active'; `ix_visit_sweep`(expires_at) WHERE status='Active' | resort_id→resort, room_id→room, guest_session_id→guest_session |

- Index có filter **enum-string** (`status='Active'`) → **provider-agnostic** (string literal 'Active' hợp lệ cả SQLite lẫn Npgsql) → đặt trong config.
- Index có filter **BOOL** (`is_deleted=false`, `is_default`) → **khác cú pháp** SQLite(0/1) vs Npgsql(false/true) → đặt ở `OnModelCreating` provider-aware (§3).

## 3. Partial index BOOL provider-aware (fix gốc như xmin — DEV-020)
Trong `AppDbContext.OnModelCreating` sau base:
```
var isNpgsql = Database.IsNpgsql();
mb.Entity<Room>().HasIndex(r => new { r.ResortId, r.RoomNumber }).IsUnique()
   .HasDatabaseName("ux_room_number").HasFilter(BoolFilter(isNpgsql, "is_deleted", false));
mb.Entity<ResortLanguage>().HasIndex(l => l.ResortId).IsUnique()
   .HasDatabaseName("ux_lang_default").HasFilter(BoolEquals(isNpgsql, "is_default", true));
```
`BoolFilter(isNpgsql,col,val)` → Npgsql `"{col} = {true|false}"`, SQLite `"{col} = {1|0}"`. Cột snake_case (convention). Lý do gốc: cú pháp boolean literal khác provider → phải sinh đúng theo provider (không hardcode 1 kiểu) để SQLite test được + Npgsql production đúng.

## 4. `AppUserAuthStore : IUserAuthStore` (auto-đăng ký qua IScopedService)
- Ở `ResortQr.Infrastructure/Identity/` (NGOÀI namespace Persistence → Scrutor tự đăng ký, đúng ý: app cung cấp store user).
- `FindByEmailAsync/FindByIdAsync` → project `AppUser`→`AuthenticatedUser(Id, Email, PasswordHash, [Role.ToString()], IsActive)` (AsNoTracking, read).
- `UpdatePasswordHashAsync` → load tracked `AppUser`, set `PasswordHash` (chỉ stage; commit qua UoW — rehash-on-login `12`).
- Email lookup: khớp chính xác (chuẩn hoá lowercase là việc của use case đăng ký/login — chưa build; ghi TODO).

## 5. `AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>` (`23` §7)
- Cho `dotnet ef migrations` (startup project Api ≠ project chứa DbContext). Đọc `ConnectionStrings:Postgres` từ config/env; `UseNpgsql(...).UseSnakeCaseNamingConvention()`. Clock/CurrentUser = stub design-time (không gọi lúc build model).

## 6. Test (SQLite Docker-free — DB quan hệ THẬT)
- enum→string round-trip (đọc lại đúng enum).
- **unique constraint enforce**: 2 AppUser trùng email → DbUpdateException; 2 RoomQrToken trùng token → chặn.
- **partial unique**: 2 RoomQrToken Active/cùng room → chặn; revoke cái cũ rồi thêm Active mới → OK (ux_qr_active). 2 GuestVisit Active/(session,room) → chặn.
- **ux_room_number** partial: 2 room cùng (resort,number) chưa xóa → chặn; soft-delete 1 rồi tạo lại cùng số → OK.
- **FK Restrict**: thêm Room với resort_id không tồn tại → lỗi FK (SQLite bật PRAGMA foreign_keys).
- **soft-delete + audit** trên Room (đã có cơ chế base — verify áp cho entity app).
- **AppUserAuthStore**: seed user → FindByEmail/FindById trả đúng; UpdatePasswordHash + SaveChanges → đổi hash.
- **xmin Npgsql**: verify OFFLINE model (như base) rằng Room/AppUser/ResortSettings có concurrency token; SQLite không.

## 7. Truy vết
- `04` §3/§5/§7/§9; `06` (naming/nav); `23` §7 (factory); `12` (auth store); `16` (room/qr index); `24` (tenancy). Base: DEC-047/048/053, DEV-020.
