# starhill-qr — Spec pha dựng lại QR trên nền Bedrock

> **Pha MỚI** (sau khi `platform-base` hoàn tất): dựng lại sản phẩm **Resort QR Portal / Star Hill Guest App**
> trên nền base Bedrock, theo mô hình module hoá.

## Nguồn sự thật (authority)

| Loại | Vị trí | Vai trò |
|---|---|---|
| **WHAT (yêu cầu)** | `docs/resort-qr-portal/requirements.md` (14 nhóm EARS) + `deep-solution-design.md` (Decision Log) | Nghiệp vụ QR phải làm gì — TÁI DÙNG, không viết lại. |
| **HOW-product (thiết kế QR gốc)** | `docs/resort-qr-portal/design.md` + `tasks.md` + `test-plan.md` (15 Correctness Properties) | Thiết kế sản phẩm QR (data model/API/SignalR/luồng). Tham chiếu. |
| **HOW-on-Bedrock (thiết kế pha này)** | `design.md` (SẼ tạo trong thư mục này) | Bản đồ QR → module/adapter Bedrock; mỗi domain = module 5-project; chỉ dùng port Bedrock. |
| **Nền base** | `platform/` (Bedrock — ĐÓNG BĂNG, sạch) | Base tái dùng. KHÔNG sửa để làm QR. |
| **Base đã copy (nơi làm QR)** | `starhill/` (bản vendored của `platform/`) | Sản phẩm StarHill = copy Bedrock + module QR. |
| **Bản CŨ (tham chiếu logic)** | `resort-qr/` (standalone trên SharedKernel riêng) | Logic domain/application cũ để PORT sang module Bedrock. |
| **Journal pha QR** | `.kiro/specs/starhill-qr/journal/` | 4 việc (quyết định/đổi/tradeoff/cần-biết) + anti-drift RIÊNG cho pha QR. |

## Chiến lược (đã chốt với user)

1. **Copy-out**: base `platform/` giữ SẠCH/tái dùng; sản phẩm làm trên bản copy `starhill/` (vendored). Verify base tốt TRƯỚC khi copy (đã làm).
2. **Module hoá**: mỗi domain QR (Rooms, GuestAccess, Rules, FAQ, Messaging, Housekeeping, + Identity auth) → **module 5-project** trên Bedrock (khuôn như module Identity mẫu).
3. **Port, không viết lại**: mang logic domain/application từ `resort-qr/` sang; thay SharedKernel→Bedrock.Domain, persistence riêng→Bedrock.Infrastructure (PlatformDbContext/UoW/outbox/refresh-store), security riêng→Bedrock.Api. Frontend Vue (`resort-qr/frontend`) tái dùng.
4. **Design-first**: tạo `design.md` (bản đồ) + duyệt TRƯỚC khi build module.

## Quyết định còn mở (chốt trước khi build module đầu)

- [x] Deploy cùng-origin `portal.starhill.local` hay tách subdomain → **CHỐT: cùng-origin** (QR-AD-003; mặc định theo khuyến nghị design gốc).
- [x] Nguồn cert HTTPS nội bộ → **CHỐT: internal CA + DNS nội bộ** (QR-AD-003).
- [x] Realtime SignalR: **ChatHub đặt ở module Concierge**, map trong Host `StarHill.Api`; dùng cho guest↔staff realtime + HousekeepingUpdated. Outbox Bedrock để dành cho event nội bộ/cascade khi tách tải (QR-AD-002/QR-TO-002).

## Trạng thái

- ✅ Base `platform-base` hoàn tất + verify (build 0-warning, 247 test, JournalConsistency 5/5) — cổng "base tốt" ĐẠT.
- ✅ Copy `platform/` → `starhill/` (loại bin/obj) + verify build 0-warning độc lập.
- ✅ `design.md` (bản đồ QR→Bedrock: 8 module + Dashboard-ở-Host, cross-module Id-trần, waves) — getDiagnostics 0.
- ✅ **Wave A HOÀN TẤT (hạ tầng governance sản phẩm)**:
  - QR-AD-006: cổng anti-drift journal QR (`StarHill.ArchitectureTests/StarHillJournalConsistencyTests`, INV-1..5).
  - QR-AD-007: CI sản phẩm `.github/workflows/starhill-ci.yml` (nhắm starhill/) + `validate_ci.py` retarget + bất biến "nhắm starhill".
  - Gate tổng `vp all`: build 0-warning + validate-ci OK + test **252 (235 pass / 17 skip Docker / 0 fail)**.
- ✅ **Wave B XONG** — module **ResortConfig** đầy đủ nền:
  - B.1: Domain/Contracts/Application/Infrastructure + migration + resolver + CP5 unit.
  - B.2: `ResortConfigSeeder` (en default, Req 12.4) + Host wiring (conn `ResortConfig` + migrate/seed gated) + integration **CP14** (partial-unique default) / **CP15** (xmin concurrency) / seeder-idempotent (Testcontainers, skip w/o Docker) + **ModuleBoundary** arch test (StarHill.ArchitectureTests).
  - Gate: `vp all` = build 0-warning + validate-ci + **269 test (249 pass / 20 skip / 0 fail)**. Journal QR-N-008/009; guard map cập nhật ✅ CP14/CP15/boundary.
  - Hoãn (I10): query ports (→ khi GuestAccess tiêu thụ) + ResortConfig.Api (→ B.3, cần Identity auth).
- ⏳ TIẾP — **Wave C: module Rooms** (SỬA thứ tự: Rooms TRƯỚC GuestAccess — resolve phụ thuộc Rooms). ✅ design xong (`design-modules/02-rooms.md`, getDiagnostics 0). Phát hiện **gap nền** (QR-AD-010: base thiếu dịch unique-violation → bổ sung ở platform/ rồi copy) + deviations QR-DV-003 (bỏ FK chéo-schema, đọc settings qua Contracts). Journal: QR-AD-010/011, QR-DV-003, QR-N-010.
  - Slice: **B-Rooms.0** (base unique-violation capability, đụng platform/) → B-Rooms.1 (ResortConfig `IResortSettingsQuery`) → B-Rooms.2 (Rooms module + migration + CP1/CP2 + boundary) → B-Rooms.3 (Rooms.Api, cần Identity auth).
  - ✅ **B-Rooms.0 XONG** (QR-N-011): base `platform/` +AD-069 (`UniqueConstraintViolationException` + EfUnitOfWork dịch 23505 + Api 409) → `vp all` 247/0-fail → copy 4 file sang `starhill/` → `vp all` 269/0-fail. Increment đụng base đầu tiên, verify trước/sau, 2 bản đồng bộ. Journal base AD-069; QR QR-N-011.
  - ✅ **B-Rooms.1 XONG** (QR-N-012): `IResortSettingsQuery` + `EfResortSettingsQuery` + test SQLite chạy-cục-bộ (2 test pass, `vp all` 271/0-fail).
  - ✅ **B-Rooms.2a XONG** (QR-N-013): Rooms.Domain/Contracts/Infrastructure + migration schema `rooms` (ux_room_number/ux_qr_active/ux_qrtoken_token) + `IRoomTokenResolver` + Host wiring. Test SQLite local CP1(resolver)/CP2(1-active-token) + ModuleBoundary. `vp all` **282 (261 pass/21 skip/0 fail)**.
  - ⏳ TIẾP: **B-Rooms.2b** — Rooms.Application use cases (Create/Rotate/RenderQrPng...) — verify ITokenGenerator/Result API/UniqueConstraintViolationException/QRCoder. Rồi B-Rooms.3 (Rooms.Api, cần Identity auth).
