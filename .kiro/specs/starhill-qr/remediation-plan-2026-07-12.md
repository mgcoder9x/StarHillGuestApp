# starhill/ — Kế hoạch khắc phục P0 (design-first, verify-then-implement) — 2026-07-12

> Trạng thái: **PLAN — chưa sửa code.** Chờ user chốt Quyết định D1 (cơ chế đồng bộ base) trước khi triển khai.
> Mọi finding dưới đây ĐÃ verify bằng đọc file thật (không tin báo cáo cũ mù quáng).

## 1. Bối cảnh
- `platform/` = base chuẩn (đã hardening: keyed persistence, transaction resolver, messaging production-oriented; 403 test xanh với Docker).
- `starhill/` = bản VENDOR (copy) base cũ + module QR (Identity/ResortConfig/Rooms). Base trong starhill LỆCH khỏi platform (thiếu mọi fix Gate 0/Wave 1 + keyed persistence).

## 2. P0 đã VERIFY (đọc file thật 2026-07-12)

### P0-1 — Persistence UNKEYED + multi-module last-registration-wins (CATASTROPHIC)
- `starhill/src/Bedrock.Infrastructure/DependencyInjection/BedrockPersistenceExtensions.cs`: chỉ có overload UNKEYED — `AddScoped<IUnitOfWork,EfUnitOfWork>()`, `AddScoped<PlatformDbContext>(sp=>sp.GetRequiredService<TContext>())`, `AddScoped(typeof(IRepository<>))`, `TryAddScoped<IOutboxWriter>`, `TryAddScoped<IRefreshTokenStore>`. KHÔNG có overload moduleKey, KHÔNG có `PersistenceRegistrationRegistry` chặn 2 unkeyed context.
- `starhill/src/Host/StarHill.Api/Program.cs`: gọi `AddIdentityInfrastructure` → `AddResortConfigInfrastructure` → `AddRoomsInfrastructure` TUẦN TỰ, mỗi cái `AddBedrockPersistence<TContext>` unkeyed.
- **Hệ quả (Microsoft DI last-wins):** `PlatformDbContext` alias + `IUnitOfWork` + `IOutboxWriter` + `IRefreshTokenStore` + generic `IRepository<>` → resolve về **RoomsDbContext** (đăng ký cuối) cho MỌI module. ⇒ refresh-token/UoW của Identity chạy trên RoomsDbContext (sai schema/DB, bảng không tồn tại → hỏng runtime). Đây là P0 số 1.

### P0-2 — 3 database vật lý (DRIFT so với design §1.3 "một DB vật lý, nhiều schema")
- `appsettings.json`: `Identity→starhill_identity`, `ResortConfig→starhill_resort_config`, `Rooms→starhill_rooms` (3 DB khác nhau).
- Design §1.3 (verified): mỗi module connection string trỏ **CÙNG một PostgreSQL**, khác **schema** (schema đặt ở DbContext.HasDefaultSchema — Identity `identity`, ResortConfig `resort_config`, Rooms `rooms`). ⇒ phải hợp nhất 3 connection về cùng DB (vd `starhill`), giữ schema per-module.

### P0-3 — docker-compose KHÔNG chạy nổi 3 module
- compose `postgres` chỉ tạo `POSTGRES_DB=starhill_identity`; host env chỉ override `ConnectionStrings__Identity` (→ service `postgres`). ResortConfig/Rooms KHÔNG override → dùng appsettings `Host=localhost` (localhost TRONG container = chính nó, không có Postgres) + DB không tồn tại.
- `Program.cs` khi `ApplyMigrationsOnStartup=true` (compose bật) migrate CẢ 3 (Identity+ResortConfig+Rooms) → ResortConfig/Rooms **fail-fast lúc migrate** → compose boot HỎNG.

## 3. QUYẾT ĐỊNH THEN CHỐT cần user chốt

### D1 — Cơ chế starhill tiêu thụ base (repo-structure; ẢNH HƯỞNG cách fix P0-1)
| Phương án | Ưu | Nhược |
|---|---|---|
| **(a) Project-reference platform/src/Bedrock.* (+Adapters) — KHUYẾN NGHỊ** | Một nguồn duy nhất → **drift KHÔNG thể tái diễn**; 0 công re-vendor; starhill lập tức có keyed base + mọi fix | starhill build phụ thuộc platform/ hiện diện (chấp nhận vì cùng repo) |
| (b) Internal NuGet versioned | Cô lập version release | Cần dựng feed + CI publish — nặng, sớm (base còn tiến hóa) |
| (c) Giữ vendor + drift-guard (upstream commit manifest + CI diff) | starhill tự chứa | Vẫn phải re-vendor 92 file NGAY + duy trì guard; drift dễ tái diễn |

- **Lý do khuyến nghị (a):** review đã chỉ 92 file lệch — bằng chứng vendor thủ công KHÔNG bền. (a) triệt tiêu drift TẬN GỐC (không còn 2 bản base), đúng "một nguồn sự thật", và bỏ luôn công đồng bộ 92 file. (b) đúng cho lúc RELEASE (API base ổn định) nhưng sớm bây giờ. (c) chỉ hoãn vấn đề.

### D2 — Chiến lược DB: ĐÃ được design quyết (§1.3 "một DB vật lý, nhiều schema"). Không phải quyết định mở — chỉ cần sửa code cho khớp design.

## 4. Kế hoạch triển khai (SAU khi chốt D1=a) — theo thứ tự, mỗi bước verify
1. **Đồng bộ base (D1a):** starhill xóa cây `Bedrock.*` + `Adapters.*` vendor; đổi ProjectReference của module/Host/test starhill sang `platform/src/Bedrock.*` + `platform/src/Adapters/*`. Build 0 warning.
2. **Keyed persistence 3 module (fix P0-1 tận gốc):** mỗi `AddXxxInfrastructure` dùng `AddBedrockPersistence<TContext>(ModuleKey, cs)` + `AddBedrockOutbox/Inbox/RefreshTokens<TContext>(ModuleKey)` khi cần; mỗi module có `PersistenceKey` (hằng trong `<M>.Contracts`); use case ghi khai `PersistenceKey`; consumer/dispatcher/handler keyed. Compile-enforce (AD-098) sẽ bắt use case void thiếu key.
3. **1-DB-nhiều-schema (fix P0-2):** appsettings 3 connection → cùng DB `starhill` (giữ schema per-module ở DbContext). 
4. **Compose (fix P0-3):** postgres tạo DB `starhill`; host override CẢ 3 connection (`ConnectionStrings__Identity/ResortConfig/Rooms` → service `postgres`, DB `starhill`); migrate 3 schema chạy được; verify `docker compose up` → /health/ready=200.
5. **Fail-closed CI + Host test 3 module:** test Host thật boot đủ 3 module + smoke; Testcontainers fail-closed trên CI (không skip câm).
6. **P1 (sau P0):** ResortId invariant qua query port (Rooms), default-language nguyên tử (ResortConfig aggregate), Dashboard shape (Application+Api thay vì nhét Host), bundle migration Rooms.

## 5. Anti-drift cho starhill
- Nếu D1=(a): drift base bị triệt tiêu (không còn bản copy). Journal 4-file + guard riêng cho phần NGHIỆP VỤ QR (module boundary, ResortId invariant, default-language) sẽ lập khi bắt đầu bước 2+.
- Nếu D1=(c): BẮT BUỘC thêm upstream-commit-manifest + CI diff-guard (nếu không, 92-file-drift tái diễn).

---
**HỎI USER:** chốt D1 = (a) project-reference [khuyến nghị] / (b) internal NuGet / (c) vendor+guard? Sau khi chốt, tôi triển khai bước 1→4 (P0) rồi verify với Docker.

---

## 6. ĐÍNH CHÍNH D1 sau khi đọc cấu trúc thật (2026-07-12) — quan trọng

**Phát hiện:** `starhill/` là SOLUTION SONG SONG ĐẦY ĐỦ, KHÔNG phải "vendor vài file":
- `starhill/src/` có full base: `Bedrock.Domain/Application/Api/Infrastructure/Messaging.Contracts` + `Adapters/Messaging.RabbitMq` (bản COPY, cũ, unkeyed).
- `starhill/tests/` có cả `Bedrock.UnitTests/ArchitectureTests/Api.Tests/Infrastructure.Tests/ContractTests` (bản copy của test base) + `StarHill.ArchitectureTests` + test module (Identity/ResortConfig/Rooms).
- Module tham chiếu base qua `ProjectReference ..\..\..\Bedrock.Infrastructure\Bedrock.Infrastructure.csproj` (nội trong cây starhill).
- `starhill/Platform.slnx` + `Directory.Build.props` + `Directory.Packages.props` RIÊNG (trùng tên `Platform.slnx` với platform/).
⇒ Đây chính là 2 BẢN base song song = gốc drift 92-file. "Đồng bộ" thực chất là **quyết định repo-strategy**, không phải copy file.

**3 end-state khả thi (chọn 1):**
| | Mô tả | Việc phải làm | Rủi ro |
|---|---|---|---|
| **D1-a project-ref** | Xóa `Bedrock.*`+`Adapters`+`Bedrock.*Tests` trong starhill; repoint mọi ProjectReference module/Host/adapter-consumer/test sang `platform/src/*`; viết lại `starhill/Platform.slnx`; reconcile `Directory.Packages.props` | Xóa ~11 project + sửa ~15 csproj + slnx; fix cascade compile (base mới đổi API: keyed persistence, `OutgoingIntegrationMessage`, `ICommandUseCase<TInput>:ITransactionalUseCase`…) | CAO (blast rộng, có thể MAX_PATH output nếu path sâu, compile-loop) |
| **D1-a' MERGE một solution** | Đưa Modules QR + Host QR vào CHÍNH `platform/Platform.slnx`; xoá hẳn cây `starhill/` | Di chuyển Modules/Host QR sang platform; 1 solution duy nhất | CAO nhưng SẠCH nhất lâu dài |
| **D1-c vendor-sync có kiểm soát** | Giữ 2 cây; đồng bộ base bằng script + **CI diff-guard** (so `starhill/src/Bedrock.*` == `platform/src/Bedrock.*`, khác → fail) | Copy base mới đè starhill (11 project) + fix cascade + thêm diff-guard | TRUNG BÌNH (giữ độc lập; drift bị guard chặn) |

**Đánh giá lại (chính xác):** cả 3 đều là refactor LỚN + destructive + có cascade compile do base mới đổi API. Đây là quyết định cấu trúc sản phẩm dài hạn — KHÔNG nên tự quyết đơn phương vì (1) không biết ý định repo của user (base tái dùng cho NHIỀU sản phẩm hay chỉ starhill?), (2) blast radius rất rộng, (3) sai hướng = phí refactor khổng lồ.

**Khuyến nghị chính xác:**
- Nếu base `platform/` dùng cho **NHIỀU sản phẩm tương lai** → **D1-c** (2 cây độc lập + diff-guard + sau này NuGet khi ổn định).
- Nếu **CHỈ có starhill** dùng base → **D1-a' MERGE** (sạch nhất, 1 nguồn, hết drift vĩnh viễn).
- **D1-a** (cross-tree project-ref) là trung gian: hết drift nhưng starhill lệ thuộc platform/ hiện diện + slnx lẫn lộn 2 cây — ít khuyến nghị lâu dài.
