# Design — B-Rooms.3: Rooms.Api slice (admin CRUD phòng + QR PNG + rotate token)

> Design-first cho slice **B-Rooms.3**. Đọc lại + valid TRƯỚC khi code. Mọi khẳng định dưới đây đã kiểm bằng
> code thật (đường dẫn ghi kèm). Phần cuối là **Open Questions** cần user chốt trước khi triển khai.

---

## 0. Phạm vi & nguồn sự thật (đã verify)

**Mục tiêu:** lộ nửa-Api của module Rooms — các endpoint HTTP admin cho: tạo/sửa/đổi-trạng-thái/xoá-mềm phòng,
rotate (thu hồi + cấp lại) token QR, và render QR PNG. Đây là seam HTTP cho use case đã tồn tại (KHÔNG viết
nghiệp vụ mới ở tầng Api — Api chỉ là cơ chế HTTP, F14).

**Use case đã có sẵn** (`starhill/src/Modules/Rooms/Rooms.Application/`, đã đọc):
| Use case | Signature | Ghi chú |
|---|---|---|
| `CreateRoomUseCase` | `IUseCase<CreateRoomInput, CreateRoomResult>` | input mang `ResortId` (caller phân giải — QR-DV-004) |
| `UpdateRoomUseCase` | `ICommandUseCase<UpdateRoomInput>` | `PersistenceKey=RoomsModule.PersistenceKey` |
| `ChangeRoomStatusUseCase` | `ICommandUseCase<ChangeRoomStatusInput>` | |
| `DeleteRoomUseCase` | `ICommandUseCase<Guid>` | xoá mềm |
| `RotateRoomTokenUseCase` | `IUseCase<RotateRoomTokenInput, RotateRoomTokenResult>` | revoke+cấp mới nguyên tử |
| `RenderRoomQrPngUseCase` | `IUseCase<RenderRoomQrPngInput, RenderRoomQrPngResult>` | trả `byte[] Png` |

**Contracts input/output** (`Rooms.Application/RoomContracts.cs`, đã đọc):
`CreateRoomInput(Guid ResortId, string RoomNumber, string? Building, int? Floor)` → `CreateRoomResult(Guid RoomId, string Token, string TokenPreview)`;
`RotateRoomTokenInput(Guid RoomId, string? Reason)` → `RotateRoomTokenResult(string Token, string TokenPreview)`;
`UpdateRoomInput(Guid RoomId, string RoomNumber, string? Building, int? Floor)`; `ChangeRoomStatusInput(Guid RoomId, RoomStatus Status)`;
`RenderRoomQrPngInput(Guid RoomId)` → `RenderRoomQrPngResult(byte[] Png)`.

**Mã lỗi** (`RoomsErrors.cs`, đã đọc): `RoomNumberTaken`(validation_error), `QrGenerationFailed`(qr_generation_failed/Conflict),
`RoomNotFound`(not_found/NotFound), `ResortNotFound`(resort_not_found/NotFound), `InvalidConfiguration`(invalid_configuration/Validation).

**NGOÀI phạm vi slice này** (lý do: chưa có use case/read-model):
- GET danh sách/chi tiết phòng (`RoomListItem` read-model đã defer — ghi rõ trong `RoomContracts.cs`: "→ slice query sau (I10)").
- Bulk QR (in hàng loạt) — cần query list trước.

---

## 1. Pattern base đã xác minh (để mirror, không bịa)

1. **Discovery endpoint** (`platform/src/Bedrock.Api/Endpoints/IEndpointModule.cs` + `BedrockApiExtensions.cs`):
   mỗi module Api khai một class `IEndpointModule`, đăng ký singleton trong `AddXApi`; Host `UseBedrockApi` →
   `endpoints.ServiceProvider.GetServices<IEndpointModule>()` → gọi `MapEndpoints`. KHÔNG reflection-scan.
2. **Versioning** (`Identity.Api/IdentityEndpointModule.cs`): `endpoints.MapVersionedGroup("/identity", BedrockApiVersioning.V1)`
   → route thật `/v1/identity/...`. Rooms sẽ dùng `MapVersionedGroup("/rooms", V1)` → `/v1/rooms` (khớp QR-DV-001).
3. **Map Result→HTTP**: `Results.Problem(ProblemDetailsBuilder.Build(result.Error, CorrelationContext.Resolve(http)))`
   — nguồn DUY NHẤT shape lỗi (N-041). `Error.Type` → status code do `ProblemDetailsBuilder` quyết.
4. **DI nửa-Api** (`Identity.Api/DependencyInjection/IdentityApiExtensions.cs`): `services.AddSingleton<IEndpointModule, XEndpointModule>()`
   (APPEND, không TryAdd — mỗi module đóng góp một instance vào `IEnumerable`).
5. **Auth cơ chế base** (`Bedrock.Api/Authentication/BedrockAuthExtensions.cs`, đã đọc):
   - `services.AddAuthorization()` ĐÃ được gọi trong `AddBedrockAuthCore` → thêm named policy về sau là **additive**, an toàn.
   - `RoleClaimType="role"`, `MapInboundClaims=false` → policy kiểm claim `"role"` JWT-native.
   - Base **cố ý KHÔNG** khai Admin/Staff (comment tại file): "Role/permission cụ thể do module Identity/Host khai".
6. **Test auth KHÔNG Docker** (`Bedrock.Api.Tests/Authentication/AuthMechanismTests.cs`, đã đọc):
   `HostBuilder().ConfigureWebHost(UseTestServer)` + `AddBedrockAuthCore` + `AddAuthorizationBuilder().AddPolicy(...)`
   + phát JWT test bằng `JsonWebTokenHandler` với claim → assert 401/403/200. Không cần DB/Docker cho tầng policy.

---

## 2. Quyết định thiết kế (mỗi cái + LÝ DO chính xác)

### D-A — Ánh xạ role từng endpoint (nền QR-AD-005 + Req 7.6)

Superset Admin⊇Staff hiện thực bằng 2 named policy:
- `RequireAdmin` = `RequireRole("admin")`
- `RequireStaff` = `RequireRole("staff", "admin")` → **admin nằm trong danh sách** ⇒ Admin qua được endpoint Staff (superset).

**Lý do dùng "staff","admin" thay vì role phân cấp:** base dùng `RequireRole` thuần trên claim `"role"` (verify ở
`BedrockAuthExtensions`). Không có cơ chế role-hierarchy sẵn. Cách chuẩn/đơn giản/kiểm-được để "Admin là superset"
là liệt kê cả `admin` trong policy Staff. Deterministic, không phụ thuộc middleware ngoài.

Ánh xạ endpoint (Req 7.6 "tạo/sửa/xoá phòng + sinh/thu hồi QR chỉ Admin; Staff chỉ xem"):

| # | Method | Route (thật) | Use case | Policy | Req |
|---|---|---|---|---|---|
| 1 | POST | `/v1/rooms` | CreateRoomUseCase | **RequireAdmin** | 7.6 |
| 2 | PUT | `/v1/rooms/{roomId}` | UpdateRoomUseCase | **RequireAdmin** | 7.6 |
| 3 | PATCH | `/v1/rooms/{roomId}/status` | ChangeRoomStatusUseCase | **RequireAdmin** | 7.6 |
| 4 | DELETE | `/v1/rooms/{roomId}` | DeleteRoomUseCase | **RequireAdmin** | 7.6 |
| 5 | POST | `/v1/rooms/{roomId}/rotate-token` | RotateRoomTokenUseCase | **RequireAdmin** | 7.4/7.6 |
| 6 | GET | `/v1/rooms/{roomId}/qr.png` | RenderRoomQrPngUseCase | **RequireStaff** (khuyến nghị — xem Open Q1) | 7.1 |

**Lý do qr.png = RequireStaff (khuyến nghị):** endpoint này **render token Active ĐÃ TỒN TẠI** ra PNG — KHÔNG
"sinh/thu hồi" token (không tạo/rotate). Đây là hành vi **xem** (Staff được phép theo Req 7.6 "Staff chỉ được xem
thông tin phòng phục vụ vận hành"). Ngoài ra nó cho phép **guard CP8 kiểm được CẢ HAI nửa** của superset trong
chính slice Rooms: Staff→endpoint Admin = 403, **Admin→endpoint Staff = 200** (chứng minh Admin⊇Staff). Nếu mọi
endpoint đều Admin-only thì nửa "Admin qua endpoint Staff = OK" không có chỗ kiểm ở tầng Rooms. → Xem Open Q1.

### D-B — Response KHÔNG BAO GIỜ trả token thô; chỉ `TokenPreview` + `RoomId`

`CreateRoomResult`/`RotateRoomTokenResult` mang cả `Token` (thô) lẫn `TokenPreview` (đã mask). Endpoint chỉ trả
`RoomId` + `TokenPreview`. Token thô chỉ đến client qua **ảnh QR** (`GET qr.png`) — không xuất hiện trong JSON/log.

**Lý do:** token là bí mật không-đoán-được nhúng URL QR (Req 7.1, CP1 "không lộ"). Trả token thô trong JSON tăng
bề mặt rò rỉ (log, history trình duyệt, proxy) không cần thiết — admin lấy QR để in qua `qr.png`. Giảm rủi ro, đúng
định hướng thương mại. (Đây là quyết định AI tự ra — spec không nói response shape → sẽ ghi QR-AD.)

### D-C — Project dùng chung `StarHill.Authorization` cho policy Admin/Staff

Tạo class-lib mới `starhill/src/StarHill.Authorization` gồm:
- `StarHillPolicies` (static): `const string RequireAdmin`, `RequireStaff`; `const string RoleAdmin="admin"`, `RoleStaff="staff"`.
- `AddStarHillAuthorization(this IServiceCollection)`: `services.AddAuthorizationBuilder().AddPolicy(RequireAdmin, p => p.RequireRole(RoleAdmin)).AddPolicy(RequireStaff, p => p.RequireRole(RoleStaff, RoleAdmin))`.

Host `StarHill.Api/Program.cs` gọi `services.AddStarHillAuthorization()`. Mỗi module Api (Rooms.Api giờ; GuestAccess/
Rules/Faq/Housekeeping/Concierge sau) tham chiếu **hằng tên policy** từ project này.

**Lý do (DRY + chống drift, KHÔNG gold-plate):** Admin/Staff là mô hình 2-vai của **sản phẩm** (không thuộc base
domain-agnostic — base cố ý không khai). Có **nhiều consumer thật** (mọi module admin của QR) cần đúng 2 policy
này với **cùng ngữ nghĩa superset**. Nếu mỗi module tự viết `RequireRole("staff","admin")` inline sẽ dễ drift (ai
đó quên `admin` → phá superset). Một nguồn hằng + một hàm đăng ký = ngữ nghĩa superset định nghĩa MỘT chỗ. Đây là
seam cross-cutting hợp lệ, không phải trừu tượng thừa (I10) vì đã có consumer đa dạng. Guard CP8 khoá ngữ nghĩa.

**Vì sao không đặt trong base Identity:** base `platform/src/Modules/Identity` domain-agnostic; nhồi role sản phẩm
(Admin/Staff) vào base làm bẩn base tái-dùng-nhiều-dự-án (đi ngược QR-AD-012). Policy sản phẩm ⇒ ở cây `starhill/`.

### D-D — Phân giải `resortId` (single-resort) ở tầng Api qua `IResortSettingsQuery`

Endpoint Create không nhận `resortId` từ client. Api resolve: `var s = await settingsQuery.GetAsync(ct);` →
`s.ResortId`. Nếu `s is null` (chưa seed) → trả `RoomsErrors.InvalidConfiguration` (chưa cấu hình resort).

**Lý do:** QR-DV-004 đã chốt "caller (Rooms.Api) phân giải resortId (single-resort qua `IResortSettingsQuery`) rồi
truyền vào" — giữ `Rooms.Application` KHÔNG phụ thuộc ResortConfig (boundary `RoomsBoundaryTests`). `ResortSettingsSnapshot`
đã mang `Guid ResortId` (verify `IResortSettingsQuery.cs`). Cross-module qua **Contracts** là cơ chế hợp lệ (QR-AD-002);
Rooms.**Api** (không phải Rooms.Application) được phép ref `ResortConfig.Contracts`.

### D-E — DTO Api tách khỏi contract Application (mirror Identity `RefreshRequest`)

Api khai record request/response riêng (vd `CreateRoomRequest(string RoomNumber, string? Building, int? Floor)`),
KHÔNG bind trực tiếp `CreateRoomInput` từ body (vì `CreateRoomInput.ResortId` do server điền, client không được set).
**Lý do:** tách DTO HTTP khỏi command Application (đúng mẫu `IdentityEndpointModule.RefreshRequest`); chặn client
tự truyền `ResortId` (mass-assignment). Response records: `CreateRoomResponse(Guid RoomId, string TokenPreview)`, v.v.

---

## 3. Hợp đồng HTTP chi tiết

**Request/Response bodies:**
- `POST /v1/rooms` — body `{ roomNumber, building?, floor? }` → **201 Created** `{ roomId, tokenPreview }`, header `Location: /v1/rooms/{roomId}`.
- `PUT /v1/rooms/{roomId}` — body `{ roomNumber, building?, floor? }` → **204 No Content**.
- `PATCH /v1/rooms/{roomId}/status` — body `{ status }` (enum `RoomStatus`: Active/Inactive/Maintenance) → **204**.
- `DELETE /v1/rooms/{roomId}` → **204** (xoá mềm).
- `POST /v1/rooms/{roomId}/rotate-token` — body `{ reason? }` → **200** `{ tokenPreview }`.
- `GET /v1/rooms/{roomId}/qr.png` → **200** `image/png` (thân là byte[] PNG); dùng `Results.File(png, "image/png")`.

**Ánh xạ lỗi → status (qua `ProblemDetailsBuilder`, `application/problem+json`):**
| Error | Type | HTTP |
|---|---|---|
| validation (FluentValidation) / `RoomNumberTaken` / `InvalidConfiguration` | Validation | 400 |
| `RoomNotFound` / `ResortNotFound` | NotFound | 404 |
| `QrGenerationFailed` | Conflict | 409 |
| thiếu/không hợp lệ token JWT | — | 401 (base) |
| sai role | — | 403 (base) |

**Versioning:** tất cả trong `MapVersionedGroup("/rooms", BedrockApiVersioning.V1)` + `.MapToApiVersion(V1)` mỗi endpoint
(mirror Identity). `.WithName(...)` đặt tên duy nhất mỗi endpoint.

---

## 4. Cấu trúc code sẽ tạo

```
starhill/src/StarHill.Authorization/                 (MỚI — class lib)
  StarHill.Authorization.csproj                      (ref: Microsoft.AspNetCore.App framework)
  StarHillPolicies.cs                                (hằng tên policy + role)
  StarHillAuthorizationExtensions.cs                 (AddStarHillAuthorization)

starhill/src/Modules/Rooms/Rooms.Api/                (MỚI — class lib)
  Rooms.Api.csproj                                   (ref: Rooms.Application, ResortConfig.Contracts,
                                                       StarHill.Authorization, $(PlatformSrc)/Bedrock.Api)
  RoomsEndpointModule.cs                             (IEndpointModule — 6 endpoint + DTO records)
  DependencyInjection/RoomsApiExtensions.cs          (AddRoomsApi → AddSingleton<IEndpointModule,...>)
```

**Wire vào Host** (`starhill/src/Host/StarHill.Api/Program.cs`):
- thêm `services.AddStarHillAuthorization();` (sau `AddBedrockApi`).
- thêm `services.AddRoomsApi();` (cạnh `AddRoomsInfrastructure`).

**Thêm vào `starhill/Platform.slnx`:** 2 project mới (`StarHill.Authorization`, `Rooms.Api`).

---

## 5. Verify plan (tối đa hoá phần KHÔNG cần Docker)

### 5.1 Guard CP8 — ngữ nghĩa superset policy (KHÔNG Docker) — cốt lõi
`StarHill.ArchitectureTests/StarHillAuthorizationPolicyTests` (mẫu `AuthMechanismTests`):
- TestServer minimal: `AddBedrockAuthCore` + `AddStarHillAuthorization`; map 2 stub endpoint
  `.RequireAuthorization(RequireAdmin)` và `.RequireAuthorization(RequireStaff)`.
- Phát JWT test claim `role`: `admin`, `staff`, hoặc không role.
- Assert: staff→admin=403; admin→staff=200; staff→staff=200; admin→admin=200; no-role→cả hai=403; no-token=401.

### 5.2 Guard — RoomsEndpointModule route + policy binding (KHÔNG Docker)
`RoomsEndpointAuthTests`: dựng TestServer map `RoomsEndpointModule` thật + `AddStarHillAuthorization` + **fake use case**
(đăng ký `IUseCase<...>`/`ICommandUseCase<...>`/`IResortSettingsQuery` giả trả success) → không chạm DB:
- staff→(POST/PUT/PATCH/DELETE/rotate) = **403** (route Admin thật + policy thật).
- admin→POST `/v1/rooms` = **201** (fake use case), admin→các route = expected 2xx/204.
- admin & staff→GET `/v1/rooms/{id}/qr.png` = **200** + `Content-Type: image/png` (fake trả PNG bytes) → kiểm CP1 route shape + qr.png content-type + superset.
- kiểm route thật đúng `/v1/rooms...` (QR-DV-001).

### 5.3 Boot Host (KHÔNG Docker)
`StarHill.Api.Tests` (WebApplicationFactory<Program>, `ValidateOnBuild=true`): thêm module Api không được phá boot —
xác nhận DI resolve `RoomsEndpointModule` + policy đăng ký OK. (Hiện đã 3/3 — không được rớt.)

### 5.4 Full gate
`starhill\scripts\vp.cmd all` (build 0-warning + full test) + `vp journal` (INV-1..5 sau khi append QR-AD).

---

## 6. Anti-drift (journal QR — sẽ append sau khi user duyệt + code xong)

- **QR-AD-019** — Rooms.Api endpoints + ánh xạ role (Req 7.6) + response không trả token thô (D-A/D-B) + resolve
  resortId qua `IResortSettingsQuery` (D-D). Provenance: file use case + Req 7.6 + `AuthMechanismTests`.
- **QR-AD-020** — `StarHill.Authorization` policy dùng chung Admin/Staff superset (D-C). (Có thể gộp vào QR-AD-019
  nếu user muốn ít AD — sẽ hỏi.)
- **Guard map** thêm/cập nhật dòng: chuyển QR-AD-005 (Role→policy CP8) từ ⏳ → ✅ trỏ `StarHillAuthorizationPolicyTests`
  + `RoomsEndpointAuthTests`; QR-DV-001 (route `/v1/<group>/...`) ⏳ → ✅ trỏ `RoomsEndpointAuthTests`.
- INV-3: KHÔNG ghi forward-ref số AD chưa tồn tại (bài học cũ) — append đúng thứ tự.

---

## 7. Open Questions (cần user chốt TRƯỚC khi code)

**Q1 — Role cho `GET /v1/rooms/{roomId}/qr.png`:**
- (a) **RequireStaff** (khuyến nghị) — render token đã tồn tại là hành vi "xem"; Staff xem được (Req 7.6); cho phép
  guard CP8 kiểm cả nửa "Admin qua endpoint Staff = OK".
- (b) **RequireAdmin** — coi việc lấy/in QR thuần là việc Admin (đọc Req 7 "in mã QR" nghiêng về Admin). Nếu chọn
  (b), guard CP8 nửa "Admin→Staff=OK" sẽ chỉ kiểm ở `StarHillAuthorizationPolicyTests` (stub), không có endpoint
  Staff thật ở Rooms slice này.

**Q2 — Số lượng AD:** gộp Rooms.Api + StarHill.Authorization vào **một** QR-AD-019, hay tách **hai** (QR-AD-019 +
QR-AD-020)? (Khuyến nghị: tách hai — hai quyết định độc lập, dễ truy vết + tái dùng.)

**Q3 — Response Create/Rotate:** xác nhận chỉ trả `{ roomId, tokenPreview }` (D-B, không trả token thô). Nếu FE cần
token thô để render QR phía client thay vì gọi `qr.png`, phải nói rõ (khuyến nghị GIỮ không trả thô — dùng `qr.png`).

**Q4 — Range slice:** xác nhận GET list/detail phòng để **slice query sau** (đúng defer trong `RoomContracts.cs`),
không nhồi vào slice này.
