# 05 — Anti-Drift Mechanism (pha QR)

> Chống drift cho pha `starhill-qr`, kế thừa cơ chế đã chứng minh ở `platform-base`.

## Nguyên tắc kế thừa từ base (giữ nguyên hiệu lực)

- **KEYSTONE:** không có quyết định code-enforceable nào mà thiếu guard test. Mỗi QR-AD/CP code-được-kiểm PHẢI có guard test trong solution `starhill/`.
- **Vòng lặp mỗi increment:** `starhill\scripts\vp.cmd` (build 0-warning + full test) + `vp journal` (khi có) → xanh; append journal QR (QR-AD/DV/TO/N) với Provenance thật.
- **CP1 no-business-in-core** vẫn áp cho `Bedrock.*` trong `starhill/` (nghiệp vụ QR chỉ ở Modules). Guard `NoBusinessInCore*Tests` copy theo — LƯU Ý: token cấm hiện gồm `guest|room|resort|admin|staff`; module QR (Rooms/GuestAccess...) nằm NGOÀI `Bedrock.*` nên không bị quét, nhưng phải chắc scan chỉ nhắm assembly `Bedrock.*` (không nhắm module QR).

## Cần thiết lập cho pha QR (khi dựng)

1. **JournalConsistencyTests cho journal QR:** ✅ ĐÃ CÓ (QR-AD-006) — `StarHill.ArchitectureTests/StarHillJournalConsistencyTests` parse `.kiro/specs/starhill-qr/journal/` (INV-1..5, tiền tố `QR-`, CP range 1..15). Chạy qua `vp journal` cùng test base. Lệch journal = FAIL BUILD.
2. **Correctness Properties QR:** `docs/resort-qr-portal/design.md` có 15 CP của sản phẩm — map mỗi CP → guard test trong `starhill/` (bản đồ trong `design.md` pha này).
3. **Guard ranh giới base sạch:** cân nhắc test/kiểm rằng module QR không rò vào `Bedrock.*` (đã có CP1 + ModuleBoundaryTests copy theo).

## Trạng thái hiện tại

- Journal QR: QR-AD-001..006, QR-DV-001, QR-TO-001..003, QR-N-001..004. **Cổng tự động ĐÃ SỐNG** (QR-AD-006): `vp journal` chạy `StarHillJournalConsistencyTests` 5/5 (INV-1..5) — journal QR không còn dựa review thủ công. getDiagnostics vẫn dùng cho `design.md` (Kiro Spec Format).

## Guard map — quyết định design (2026-07-11) → cơ chế chống drift

> Keystone: quyết định code-enforceable phải có guard. Các quyết định design (QR-AD-002..005, QR-DV-001) map guard như sau — hiện thực khi build module tương ứng (chưa có test project sản phẩm → tạm review + getDiagnostics).

| Quyết định | Cơ chế enforce (guard) | Trạng thái |
|---|---|---|
| QR-AD-002 cross-module chỉ qua `<M>.Contracts` (không ref Infra module khác) | ModuleBoundaryTests (copy từ base) + bổ sung assertion cho module QR | ⏳ khi build module |
| QR-AD-002 per-module schema (không chia bảng) | mỗi module DbContext `HasDefaultSchema` + migration riêng (khuôn IdentityDbContext) | ⏳ |
| QR-AD-002 cascade đồng bộ (CP9: visit kết thúc → đóng hội thoại + huỷ ticket) | guard test CP9 (GuestAccess integration) | ⏳ |
| QR-AD-005 Role→policy (Admin superset Staff) | guard test CP8 (Staff→Admin endpoint = 403; Admin→Staff endpoint = OK) | ⏳ |
| QR-DV-001 route `/v1/<group>/...` | HTTP integration test theo path thật | ⏳ |
| CP1 no-business-in-core (Bedrock.* trong starhill/) | NoBusinessInCore*Tests (copy từ base) — chỉ quét assembly Bedrock.* | ✅ copy theo base |
| Journal QR nhất quán (INV-1..5) | JournalConsistencyTests bản QR (trỏ `.kiro/specs/starhill-qr/journal/`) | ✅ QR-AD-006 (StarHill.ArchitectureTests 5/5; `vp journal`) |
| CI sản phẩm nhắm starhill/ (không trỏ nhầm base) | `validate_ci.py` bản copy: `CI_PATH=starhill-ci.yml` + `REQUIRED_TARGET_TOKENS` (raw check) | ✅ QR-AD-007 (`vp ci` OK; `vp all` xanh) |
| CI chạy cổng anti-drift (journal base + QR) | `starhill-ci.yml` job build-test = full suite (gồm cả 2 JournalConsistencyTests) | ✅ QR-AD-007 (test 252/0-fail) |
| QR-AD-008 ResortConfig persistence tối giản + seeder idempotent | (design) không AddOutboxInbox; seeder runtime; guard CP14 seed `en` default | ✅ B.2: `ResortConfigSeeder` + integration test seeder-idempotent + CP14 partial-unique (Testcontainers, skip w/o Docker; chạy CI) |
| CP14 (một default) + CP15 (concurrency settings) | integration test Testcontainers `ResortConfigPersistenceTests` | ✅ B.2 (skip local w/o Docker; chạy trên CI `starhill-ci.yml`) |
| ResortConfig module boundary (CP4) | `StarHill.ArchitectureTests/ResortConfigBoundaryTests` (NetArchTest + negative control) | ✅ B.2 (4 test pass) |
| QR-AD-010 base unique-violation → neutral exception | (base) `UniqueConstraintViolationException` + EfUnitOfWork dịch; guard test Testcontainers ở `platform/tests/Bedrock.Infrastructure.Tests` | ✅ B-Rooms.0: base AD-069 (platform `vp all` 247/0-fail + JournalConsistency) → copy 4 file sang starhill (`vp all` 269/0-fail). Guard = `RefreshTokenRotationRaceTests` (Testcontainers/CI) + Api map 409 |
| QR-AD-011 Rooms module (CP1 resolver + CP2 1-active-token) | Rooms integration (Testcontainers) + `IRoomTokenResolver`; ModuleBoundary Rooms (StarHill.ArchitectureTests) | ✅ B-Rooms.2a: CP1 resolver + CP2 (ux_qr_active) `RoomsPersistenceTests` (SQLite local) + `RoomsBoundaryTests` (3 test). Use case → B-Rooms.2b |
| QR-AD-011 Rooms use case CRUD/rotate (CP2 rotate nguyên tử + Version++; not-found; retry token) | `RoomsUseCaseTests` (SQLite local) + mapping trùng-số Testcontainers | ✅ B-Rooms.2b-i: `RoomsUseCaseTests` 10 test (Create 1-active/Version=1; Rotate revoke+Active mới+Version++/giữ lịch sử; not-found; phòng-Inactive→qr_generation_failed; Update/ChangeStatus/Delete-soft) + `RoomsPostgresConstraintTests.CreateRoom_duplicate...` (Testcontainers, dịch 23505→RoomNumberTaken). `vp all` 292/0-fail |
| QR-AD-011/I7 Rooms.Application ⊥ EF/ASP.NET (bắt exception TRUNG LẬP, không DbUpdateException) | `RoomsBoundaryTests.Application_should_not_depend_on_infrastructure_or_api` (NetArchTest) | ✅ B-Rooms.2b-i: Application ⊥ Bedrock.Infrastructure/Rooms.Infrastructure/EFCore/AspNetCore; B-Rooms.2b-ii +ResortConfig.{Domain,Application,Infrastructure} (chỉ .Contracts) |
| QR-AD-011 Rooms RenderQrPng (CP1: URL {base}/r/{token} không số phòng trần; https-only) | `RenderRoomQrPngUseCaseTests` (SQLite local + QRCoder thật) | ✅ B-Rooms.2b-ii: 7 test (PNG hợp lệ; invalid baseurl null/http/not-url→invalid_configuration; Inactive/lạ/không-token→lỗi đúng). QrCoderQrService PNG thuần managed |
| QR-DV-004 `CreateRoomInput` mang `ResortId` (caller phân giải, use case KHÔNG đọc Resort repo) | `CreateRoomValidator` (ResortId NotEmpty) + `RoomsUseCaseTests` dùng input.ResortId; Rooms.Application KHÔNG ref ResortConfig ở 2b-i (I10) | ✅ B-Rooms.2b-i: validator + use case test pass; boundary test giữ Application không rò Infra |
| QR-DV-003 Rooms bỏ FK chéo-schema + đọc settings qua Contracts | ModuleBoundary (Rooms chỉ ref ResortConfig.Contracts) + review migration (không FK resort_config) | ✅ B-Rooms.2a: migration Rooms không FK chéo-schema (verify). B-Rooms.2b-ii: RenderQrPng đọc GuestWebBaseUrl qua `IResortSettingsQuery` (Contracts); `RoomsBoundaryTests` chặn Application chạm ResortConfig.{Domain,Application,Infrastructure} |
| QR-DV-002 i18n ở Contracts + manual-register resolver | (design) `ModuleBoundaryTests` giữ Contracts chỉ ref Bedrock.Messaging.Contracts; CP5 unit resolver | ✅ B.1: CP5 `TranslationResolverTests` (ResortConfig.UnitTests, `vp all` 262/0-fail) |
| QR-AD-009 migration = generated_code (miễn analyzer) | `.editorconfig [**/Persistence/Migrations/*.cs] generated_code=true` | ✅ (`vp build` 0-warning sau khi có migration composite index) |

- design.md pha QR: getDiagnostics **0** (Kiro Spec Format hợp lệ) — kiểm mỗi lần sửa.
- QR-AD-003 (deploy) + QR-AD-004 (tên Concierge): không code-enforceable trực tiếp — enforce bằng review + naming khi tạo project.
