# Module design — Identity Login (Wave F) — mở khóa luồng admin end-to-end

> **Design-first, CHƯA code.** Đóng gap phát hiện qua runtime probe (QR-N-051): Identity CHỈ có `/token/refresh`, KHÔNG
> có LOGIN → không lấy được token khởi đầu → toàn bộ admin endpoint (401) không dùng được → FE không thao tác được.
> WHAT: `docs/resort-qr-portal/requirements.md` Req 11 (auth Admin/Staff). Mọi kết luận dưới đây dựa trên CODE ĐÃ ĐỌC
> (không suy đoán) — xem §0.

## 0. Đối soát nguồn (đã verify trên đĩa, phiên 2026-07-16)

- **Crypto/JWT SẴN SÀNG ở base** (KHÔNG phải dựng): `Bedrock.Infrastructure/Cryptography/Argon2idPasswordHasher`
  (`IPasswordHasher.Hash`/`Verify(password, hash)` — Argon2id PHC, verify hằng-thời-gian) + `IJwtTokenService.Issue(ClaimsIdentity)`
  + `ITokenGenerator` — TẤT CẢ đăng ký qua `AddBedrockSecurity` (Host gọi) + `RequirePort` (boot chặn nếu thiếu; Host
  boot OK ⇒ đã có). ⇒ Login chỉ **DÙNG** các port này.
- **Hợp đồng ROLE (verify `StarHillPolicies` + `JwtTestTokens`)**: claim JWT-native type `role`, giá trị `admin`/`staff`
  (`StarHillPolicies.RoleAdmin="admin"`, `RoleStaff="staff"`). Base cấu hình `RoleClaimType="role"`, `MapInboundClaims=false`.
  Policy `RequireStaff` chấp nhận CẢ admin lẫn staff (superset — QR-AD-005/020); `RequireAdmin` chỉ admin. ⇒ JWT login
  PHẢI mang claim `role` đúng giá trị này, nếu không login xong vẫn 403.
- **Refresh family (verify `RefreshAccessTokenUseCase` + `IRefreshTokenStore`)**: `IRefreshTokenStore.AddAsync(RefreshTokenSnapshot(Id, UserId, FamilyId, Sha256HexHash, ExpiresAt, RevokedAt:null))`;
  refresh token thô = `ITokenGenerator.NewToken()`, hash lưu = SHA-256 hex. TTL refresh = 14 ngày (hằng nội bộ refresh UC).
  Login TẠO family MỚI (FamilyId mới).
- **Identity hiện KHÔNG có User** (verify: `Identity.Domain` chỉ `AuthErrors.cs`; `IdentityDbContext` chỉ map Outbox/Inbox
  + refresh_token, không DbSet nghiệp vụ). ⇒ phải DỰNG MỚI: `IdentityUser` entity + bảng + store.
- **`IRepository<T>`**: có `FirstOrDefaultAsync(predicate)` → đủ để lookup user theo username (không cần custom store).
- **Access-token TTL**: do `IJwtTokenService`/config quyết (không set ở use case) — login dùng `Issue` như refresh.

## 1. Mục tiêu & bất biến

1. **Login server-authoritative**: verify username+password ở server; sai → MỘT mã lỗi chung `invalid_credentials`
   (KHÔNG phân biệt "user không tồn tại" vs "sai mật khẩu" vs "user khoá" — chống user-enumeration/oracle; mirror
   triết lý `InvalidRefreshToken`).
2. **Chống timing user-enumeration**: user không tồn tại vẫn CHẠY một `Verify` giả (hash cố định) để cân bằng thời gian
   (Argon2 verify là phần chậm) → attacker không suy ra username tồn tại qua latency.
3. **Role trong token**: access-token mang `sub`=UserId + `role`=admin|staff → policy RequireAdmin/RequireStaff hoạt động.
4. **Refresh family**: login phát refresh-token gia đình MỚI (rotation về sau do `RefreshAccessTokenUseCase` lo).
5. **Không log/lưu mật khẩu thô** (F15/Req 11.6). Password chỉ lưu HASH (Argon2). Endpoint login body POST (không query).
6. **User inactive không đăng nhập được** (`IsActive=false` → invalid_credentials).
7. **Boundary**: Identity.Application dùng `IRepository<IdentityUser>` + port bảo mật base; không rò EF/Api (I7).

## 2. Domain model — `IdentityUser`

- `IdentityUser : Entity` (Id UUIDv7): `Username` (required, unique — chuẩn hóa lower khi lưu/lookup), `PasswordHash`
  (required, Argon2 PHC), `Role` (`UserRole` enum: Admin/Staff), `IsActive` (bool, default true), `DisplayName` (string?),
  `CreatedAt`. **KHÔNG** `IHasConcurrencyToken` ở v1 (login/seed không đua sửa; thêm sau nếu có màn quản trị user).
- `UserRole` enum { Admin, Staff } (Identity.Domain). Map ↔ claim: Admin→"admin", Staff→"staff" (dùng StarHillPolicies).
- **QUYẾT ĐỊNH (QR-AD-038)**: v1 KHÔNG mang `ResortId` trên user (sản phẩm single-resort; admin phân giải resortId qua
  `IResortSettingsQuery` khi tạo phòng — đã có). Thêm ResortId khi multi-resort thật (I10 — không gold-plate).

## 3. Persistence

- `users` table trong schema `identity` (cùng `IdentityDbContext`): unique index `ux_identity_user_username` (lower).
  EF config + migration mới (KHÔNG sửa InitialCreate — delta sạch, mirror QR-AD-035).
- `AddBedrockRepository<IdentityDbContext, IdentityUser>(PersistenceKey)` (keyed) trong `AddIdentityInfrastructure`.

## 4. LoginUseCase (`IUseCase<LoginCommand, LoginResult>`)

`LoginCommand(string Username, string Password)` → `LoginResult(string AccessToken, string RefreshToken, DateTimeOffset RefreshExpiresAt)`.

Luồng (value-returning → tự mở transaction hẹp qua `ExecuteInTransactionAsync`, mirror Refresh UC):
1. Chuẩn hóa username (Trim + lower invariant).
2. `user = repo.FirstOrDefaultAsync(u => u.Username == normalized)`.
3. **Verify (timing-safe)**: nếu `user is null` → chạy `hasher.Verify(password, DummyHash)` (bỏ kết quả) → trả
   `invalid_credentials`. Nếu có user: `ok = hasher.Verify(password, user.PasswordHash)`; `!ok || !user.IsActive` →
   `invalid_credentials` (vẫn verify TRƯỚC khi kiểm IsActive để không rò qua timing).
4. Trong transaction: tạo `FamilyId=Guid.CreateVersion7()`, `rawRefresh=tokenGenerator.NewToken()`, `expiresAt=now+14d`;
   `store.AddAsync(new RefreshTokenSnapshot(newId, user.Id, familyId, Sha256Hex(rawRefresh), expiresAt, null))`;
   `SaveChangesAsync`.
5. `accessToken = jwt.Issue(BuildIdentity(user.Id, user.Role))` với `sub`+`role`.
6. Trả `LoginResult(accessToken, rawRefresh, expiresAt)`.

`Sha256Hex` + `BuildIdentity` mirror Refresh UC (cùng cơ chế hash refresh; role thêm claim `role`).

## 5. HTTP endpoint

- `POST /v1/identity/token/login` (AllowAnonymous — chính nó cấp token). Body `{username, password}`. Rate-limited
  (middleware base slot #9 — chống brute-force ở tầng hạ tầng). Trả 200 `{accessToken, refreshToken, refreshExpiresAt}`
  hoặc 401 `invalid_credentials` (ProblemDetails). `Cache-Control: no-store`. KHÔNG log body.
- Đặt trong `IdentityEndpointModule` (cạnh `/token/refresh`).

## 6. Errors

- `AuthErrors.InvalidCredentials = Error.Unauthorized("identity.invalid_credentials", ...)` — mã MỚI client-facing →
  cập nhật `ErrorCodeSnapshotTests` (QR-AD-018).

## 7. Seed admin (dev) — để test được end-to-end

- `IdentityUserSeeder` (idempotent, GATED cờ dev — mirror `ResortConfigSeeder`): nếu chưa có user nào (hoặc chưa có
  username cấu hình) → tạo 1 admin. Username + password lấy TỪ CONFIG (`Identity:SeedAdmin:Username`/`:Password`) —
  **KHÔNG hardcode password prod** (F35). Dev/compose đặt password placeholder; prod KHÔNG bật seeder (hoặc cấp qua secret).
- **QUYẾT ĐỊNH cần user duyệt (QR-AD-039)**: cơ chế seed admin + nguồn credential (config-gated dev-only). Prod tạo admin
  qua migration/ops out-of-band hay seeder-với-secret? → đề xuất: seeder chỉ dev; prod out-of-band. Cần chốt.

## 8. Refresh-role follow-up (GAP liên quan — flag)

- Hiện `RefreshAccessTokenUseCase` phát access-token CHỈ `sub` (không role — comment thừa nhận "chờ user store"). ⇒ sau
  khi refresh, admin MẤT role → 403. Khi có `IdentityUser` store: **enhance Refresh** load user theo `UserId` → thêm claim
  `role` (+ kiểm user còn IsActive → nếu bị vô hiệu hoá thì refresh fail). Là **slice F.2** (sau login F.1) — auth mới đúng
  end-to-end. Ghi QR-N để không quên.

## 9. Guard tests (mỗi bất biến một guard)

| Bất biến | Guard test | Docker? |
|---|---|---|
| Login đúng credential → phát access(role)+refresh | `LoginUseCaseTests` (SQLite): seed user Argon2 thật → login OK, token có claim role | Không |
| Sai password / user lạ / inactive → invalid_credentials (không oracle) | `LoginUseCaseTests`: 3 case cùng một mã lỗi | Không |
| Refresh family tạo đúng (login rồi refresh chạy được) | `LoginUseCaseTests` + reuse refresh store (SQLite/hoặc Postgres) | tùy |
| Endpoint 200/401 + role claim → admin endpoint authorize | `IdentityLoginEndpointTests` (WebApplicationFactory, no DB? cần user store → Postgres) | Postgres |
| Seed admin idempotent (chạy 2 lần → 1 admin) | `IdentityUserSeederTests` (Postgres) | Postgres |
| username unique | `IdentityPostgresConstraintTests` | Postgres |

## 10. Build slices

1. **F.1a** — Domain `IdentityUser`/`UserRole` + persistence (bảng users + unique + migration) + repo keyed + boundary/unique test.
2. **F.1b** — `LoginUseCase` + `InvalidCredentials` + endpoint + snapshot + `LoginUseCaseTests` (Argon2 thật) + endpoint test.
3. **F.1c** — `IdentityUserSeeder` (dev-gated) + Host wiring + compose/appsettings + seeder test + verify runtime probe (login→token→gọi admin endpoint 2xx).
4. **F.2** — enhance Refresh: load user role + kiểm IsActive (auth đúng sau refresh).

## 11. Self-validation trước code
- [x] Argon2/JWT/ITokenGenerator SẴN ở base + RequirePort (đọc thật) — login chỉ dùng.
- [x] Hợp đồng claim `role`=admin|staff (đọc StarHillPolicies + JwtTestTokens) — token login khớp policy.
- [x] Refresh family API (`IRefreshTokenStore.AddAsync` + snapshot) đọc thật.
- [x] `IRepository.FirstOrDefaultAsync` đủ lookup username (F9 — không cần custom store).
- [x] Generic error + timing-defense (chống enumeration) — quyết định bảo mật tường minh.
- [x] Refresh-role gap nhận diện → slice F.2.
- [ ] **User duyệt**: (a) cơ chế seed admin dev-only + nguồn credential (QR-AD-039); (b) phạm vi F.1 (login) trước, F.2 (refresh-role) sau.
- [ ] Lockout/brute-force: v1 dựa rate-limiter base (slot #9); account-lockout đếm-lần-sai để dành (I10) — cần user xác nhận đủ chưa.
