# Module design — Housekeeping (Wave G)

> **Design-first, CHƯA triển khai code.** Chi tiết hóa `../design.md` §2 (module #8 Housekeeping). WHAT:
> `docs/resort-qr-portal/requirements.md` Req 6 (yêu cầu dọn phòng: guest tạo + staff hoàn tất app/QR + log), Req 3.11
> (rule-gate backend cho `/housekeeping`), Req 9/10.8 (dashboard + cascade huỷ khi visit kết thúc), Req 14
> (HousekeepingEnabled + ruleAckRequiredForHousekeeping + HousekeepingRateLimitPerHour). Data model nguồn:
> `docs/resort-qr-portal/design.md` §Data Models (HousekeepingTicket/HousekeepingEvent) + §Constraints ("1 ticket mở/phòng"
> unique) + §API. Mọi kết luận dựa trên file/code đã ĐỌC (KHÔNG suy đoán). Housekeeping là module **DỰNG MỚI** (legacy chưa có).

## 0. Đối soát nguồn (đã verify trên đĩa)

- **Legacy KHÔNG có Housekeeping** (`resort-qr/` mới tới Identity/Rooms/GuestAccess) ⇒ thiết kế từ requirements.
- **Contracts đã có + đã đọc chữ ký thật (Housekeeping là consumer):**
  1. `Rooms.Contracts.IRoomTokenResolver.ResolveActiveTokenAsync(string token)` → `RoomResolution(RoomId, ResortId,
     RoomNumber, Building?, Floor?, IsRoomActive)` (đọc `IRoomTokenResolver.cs`) — cho **complete-by-token** (staff quét QR
     phòng → RoomId → hoàn tất ticket mở của phòng đó). Token Revoked/phòng-xóa → null (CP1).
  2. `Rules.Contracts.IRuleGate.EnsureAcknowledgedAsync(resortId, guestVisitId, GuestFeature.Housekeeping, ct)`
     (enum có `Housekeeping` — đã verify) — rule-gate CP3 cho guest tạo ticket.
  3. `GuestAccess.Contracts.ICurrentGuestContextResolver.ResolveAsync(sessionKey, roomId)` →
     `Result<CurrentGuestContext(GuestVisitId, GuestSessionId, RoomId, ResortId)>` + `TouchAsync` — guest tạo/xem ticket.
  4. `ResortConfig.Contracts.Queries.IResortGuestConfigQuery.GetAsync(resortId)` → `ResortGuestConfig` (mang
     `HousekeepingEnabled`, `RequireRuleAckForHousekeeping`). `IResortSettingsQuery.GetAsync()` → `ResortSettingsSnapshot`
     (mang `ResortId` single-resort + `HousekeepingRateLimitPerHour`) — admin phân giải resortId server-side.
  5. `ICurrentUser` (Bedrock) — actor staff (CompletedByUserId) cho hoàn tất/tạo-chủ-động.
- **Precedent partial-unique + IUseCase tự-quản-transaction:** `ux_qr_active` (Rooms — 1 token Active/phòng) +
  `ux_rule_publication_current` (Rules — 1 IsCurrent/resort) + `RotateRoomTokenUseCase`/`PublishRulesUseCase`
  (flip/insert nguyên tử, bắt `UniqueConstraintViolationException` base QR-AD-010). Housekeeping tái dùng cùng lớp cơ chế.
- **xmin:** `PlatformDbContext` map `RowVersion`→`xmin` cho entity `IHasConcurrencyToken` trên Npgsql (CP15).

## 1. Mục tiêu và bất biến

1. **Đúng MỘT ticket MỞ/phòng (Req 6.2 — CP-mới, cùng lớp CP2)**: partial unique `HousekeepingTicket(RoomId)
   WHERE Status IN ('Requested','InProgress')`. Guest bấm "yêu cầu dọn" khi đã có ticket mở → KHÔNG tạo trùng, trả
   ticket hiện có (idempotent — "nhắc"). Race-safe ở DB (không TOCTOU) — bắt `UniqueConstraintViolationException` → load-return existing.
2. **Rule-gate BACKEND (CP3, Req 3.11)**: guest `POST /housekeeping` gọi `IRuleGate(Housekeeping)` → `403 rule_ack_required`
   khi cấu hình yêu cầu mà chưa ack. Enforce trong use case (defense-in-depth, mirror Faq).
3. **Feature-flag `HousekeepingEnabled` (Req 14)**: tắt → guest tạo/xem trả `housekeeping_disabled` (backend enforce).
4. **Log MỌI chuyển trạng thái (Req 6.8)**: mỗi transition ghi `HousekeepingEvent` (ActorType Guest/Staff/System,
   ActorUserId?, Method App/StaffScan?, thời điểm) — đối soát "ai làm gì, khi nào". Ghi trong CÙNG transaction với đổi Status.
5. **Máy trạng thái ticket**: `Requested → InProgress → Done`; ticket MỞ (Requested/InProgress) → `Cancelled` (cascade
   visit-end/staff). Transition không hợp lệ (vd Done→InProgress) → `housekeeping_invalid_transition`. Done/Cancelled = terminal.
6. **Hoàn tất do STAFF (Req 6.4/6.5)**: complete-by-room (chọn phòng) / complete-by-token (quét QR — dùng
   `IRoomTokenResolver`, QR chỉ định phòng KHÔNG thay đăng nhập) / set-status. Ghi `CompletedByUserId` + `CompletionMethod`
   (App/StaffScan). RequireStaff.
7. **Guest xem trạng thái của mình (Req 6.7)**: `GET /housekeeping` trả ticket hiện hành của phòng (Requested/InProgress/Done gần nhất).
8. **Cascade huỷ khi visit kết thúc (Req 10.8 — CP9)**: visit end → ticket MỞ do visit đó tạo → `Cancelled`. Đây là
   **C-GA.5** (event-driven outbox/inbox) — Housekeeping cấp use case `CancelOpenTicketsForVisitUseCase`; WIRING cascade
   (consumer GuestVisitEnded) làm ở C-GA.5 (defer — cần outbox GuestAccess). Thiết kế capability nay, không premature-couple.
9. **Optimistic concurrency (CP15)**: hai staff cùng hoàn tất một ticket → người sau 409 (xmin), không ghi đè âm thầm.
10. **Boundary (QR-AD-002)**: cross-module chỉ qua `<M>.Contracts` + Id trần; không FK chéo-schema; keyed persistence `housekeeping`.

## 2. Cấu trúc module và dependency

```text
starhill/src/Modules/Housekeeping/
  Housekeeping.Domain         -> Bedrock.Domain
  Housekeeping.Contracts      -> Bedrock.Messaging.Contracts (chỉ HousekeepingModule.PersistenceKey; port cascade để C-GA.5)
  Housekeeping.Application    -> Domain + Contracts + Rooms.Contracts + Rules.Contracts + ResortConfig.Contracts + Bedrock.Application (+FluentValidation)
  Housekeeping.Infrastructure -> Application + Bedrock.Infrastructure + EF Core/Npgsql
  Housekeeping.Api            -> Application + ResortConfig.Contracts + GuestAccess.Contracts + StarHill.Authorization + Bedrock.Api
```

- `HousekeepingModule.PersistenceKey = "housekeeping"` (hằng Contracts, nguồn duy nhất).
- **Vì sao Application ref Rooms.Contracts + Rules.Contracts** (quyết định §11): complete-by-token cần `IRoomTokenResolver`
  (Rooms) — token→RoomId là logic use case; rule-gate cần `IRuleGate` (Rules) enforce trong use case (defense-in-depth CP3).
  Cả hai cross-module qua Contracts (QR-AD-002, Id trần) — hợp lệ.
- **Vì sao Api ref GuestAccess.Contracts** (không Application): resolve `CurrentGuestContext` từ cookie là concern HTTP (mirror Faq/Rules).
- KHÔNG map Outbox/Inbox ở H-Hk.1..3 (cascade là consumer PHÍA GuestAccess phát event — C-GA.5 quyết nơi outbox).

## 3. Domain model

> Field/enum là **nguồn product** `docs/resort-qr-portal/design.md` §Data Models — TÁI DÙNG nguyên. Chốt nơi đặt
> (schema `housekeeping`) + ràng buộc DB + concurrency.

- **`HousekeepingTicket(Id, ResortId, RoomId, RequestedByGuestSessionId?, GuestVisitId?, Status, CreatedAt, StartedAt?,
  CompletedAt?, CompletedByUserId?, CompletionMethod?, RowVersion)`** — Status enum `HousekeepingStatus{Requested,
  InProgress,Done,Cancelled}`; CompletionMethod enum `HousekeepingCompletionMethod{App,StaffScan}` (nullable — chỉ set khi Done).
  ResortId/RoomId/GuestSessionId/GuestVisitId/CompletedByUserId là **Guid trần** (không FK chéo-schema rooms/guest_access/identity).
  `IHasConcurrencyToken` → xmin (CP15).
- **`HousekeepingEvent(Id, HousekeepingTicketId, NewStatus, ActorType, ActorUserId?, Method?, CreatedAt)`** — ActorType
  enum `HousekeepingActorType{Guest,Staff,System}`; Method enum nullable. Bản ghi nhật ký bất biến (append-only, KHÔNG concurrency token).
- **Lưu enum dạng STRING** (`HasConversion<string>()`) — quyết định §11: để partial-unique filter `status IN
  ('Requested','InProgress')` đọc được + ổn định khi thêm enum value (int-ordinal dễ vỡ filter). Verify precedent Rooms
  lúc code (RoomStatus lưu kiểu gì → theo cùng để nhất quán; nếu Rooms lưu int thì cân nhắc — nhưng filter status cần string).
- FK NỘI-module (schema `housekeeping`): HousekeepingEvent→HousekeepingTicket (Cascade). FK cross-schema: KHÔNG.

### Ràng buộc DB (migration `housekeeping`)

| Ràng buộc | Cột | Mục đích |
|---|---|---|
| partial unique `ux_hk_open_ticket_room` | `HousekeepingTicket(RoomId) WHERE Status IN ('Requested','InProgress')` | 1 ticket MỞ/phòng (Req 6.2, CP-mới) — race-safe |
| index `ix_hk_ticket_resort_status` | `HousekeepingTicket(ResortId, Status)` | dashboard board theo trạng thái (Req 6.3/9) |
| index `ix_hk_event_ticket` | `HousekeepingEvent(HousekeepingTicketId, CreatedAt)` | nhật ký theo ticket (đối soát) |
| FK Cascade | HousekeepingEvent→HousekeepingTicket | event thuộc ticket |
| xmin | HousekeepingTicket (IHasConcurrencyToken) | CP15 |

## 4. Use case (Application)

**Guest (idempotent create + status):**
- `RequestHousekeepingUseCase` (`IUseCase`, guest): config→HousekeepingEnabled(false→housekeeping_disabled)→**rule-gate
  (Housekeeping)**→ pre-check ticket MỞ của room (có→trả existing, idempotent "nhắc") → tạo `Requested` + event(Guest,
  System-ActorType=Guest) trong một transaction; race tạo trùng → bắt `UniqueConstraintViolationException` → load-return
  ticket mở hiện có (idempotent, không lỗi). Nhận `resortId, roomId, guestSessionId?, guestVisitId?` từ context.
- `GetRoomHousekeepingStatusUseCase` (read-only): trả ticket hiện hành của room (ưu tiên MỞ; nếu không có → Done/Cancelled
  gần nhất, hoặc null). Guest xem trạng thái (Req 6.7).

**Staff (hoàn tất/tiến trình/tạo chủ động):**
- `SetHousekeepingStatusUseCase` (`IUseCase`/ICommandUseCase, staff): ticketId + new status (InProgress/Done) + method +
  actorUserId. Validate transition (§1.5); set timestamps (StartedAt khi→InProgress, CompletedAt+CompletedByUserId+
  CompletionMethod khi→Done) + ghi event(Staff, method). xmin (CP15).
- `CompleteHousekeepingByRoomUseCase` (staff): roomId → tìm ticket MỞ của room → set Done (method=App). Không có ticket mở
  → `housekeeping_no_open_ticket`.
- `CompleteHousekeepingByTokenUseCase` (staff): token → `IRoomTokenResolver` → RoomId → như complete-by-room (method=StaffScan).
  Token không phân giải → `qr_invalid` (tái dùng mã GuestAccess? — hoặc housekeeping_room_not_found; chốt §7).
- `CreateHousekeepingByStaffUseCase` (staff, Req 6.9): staff chủ động tạo ticket cho room (ActorType=Staff) — cùng ràng buộc 1-mở/phòng.
- `CancelOpenTicketsForVisitUseCase` (System, cascade C-GA.5): huỷ mọi ticket MỞ do một visit tạo → Cancelled + event(System).
  WIRING event-driven ở C-GA.5 (defer). Cần đọc-nhiều ticket theo GuestVisitId → read-model `IHousekeepingReader` (F9, không IQueryable).

**Read-model** `IHousekeepingReader`/`EfHousekeepingReader` (F9): board theo resort+status (dashboard admin) + ticket theo
room (guest status) + ticket mở theo visit (cascade). Trả DTO snapshot no-tracking.

## 5. HTTP contract (§7 chi tiết mã lỗi)

- **Guest** (AllowAnonymous — cookie thiết bị; resolve context; check-before-touch: POST touch-sau-thành-công như Faq;
  GET status KHÔNG touch [đọc phụ trợ] — cân nhắc §11):
  - `POST /v1/guest/housekeeping {roomId}` — tạo/nhắc (idempotent 1-mở/phòng); rule-gate; `403 rule_ack_required`/`housekeeping_disabled`.
  - `GET /v1/guest/housekeeping?roomId` — trạng thái ticket hiện hành của phòng.
- **Staff/Admin** (RequireStaff — Req 6/9):
  - `GET /v1/housekeeping?status=` — board theo trạng thái (phân trang chuẩn PagedRequest/PagedResult như Rooms query).
  - `POST /v1/housekeeping/{id}/status {status}` — InProgress/Done.
  - `POST /v1/housekeeping/complete-by-room {roomId}` — hoàn tất ticket mở của phòng (method App).
  - `POST /v1/housekeeping/complete-by-token {token}` — lối tắt quét QR (method StaffScan).
  - (Tạo chủ động: `POST /v1/housekeeping {roomId}` staff — hoặc gộp complete-by-room; chốt lúc code.)
- Mọi lỗi qua `ProblemDetailsBuilder`; mã mới `HousekeepingErrors` (+`ErrorCodeSnapshotTests` QR-AD-018): `housekeeping_disabled`
  (403), `housekeeping_no_open_ticket` (404), `housekeeping_ticket_not_found` (404), `housekeeping_invalid_transition`
  (validation), `configuration_unavailable` (dùng chung). Rule-gate trả `rule_ack_required` (đã có).

## 6. Persistence & Host wiring

- `HousekeepingDbContext : PlatformDbContext`, schema `housekeeping`, keyed `HousekeepingModule.PersistenceKey`; migration
  + history table schema `housekeeping` (QR-AD-028). Repos/UoW keyed; use case factory thủ công. KHÔNG Outbox/Inbox (H-Hk.1..3).
- Host: conn `Housekeeping` + `AddHousekeepingInfrastructure`/`AddHousekeepingApi` + migrate gated. csproj Host + Platform.slnx
  + `starhill-ci.yml` bundle `housekeeping` + appsettings/compose `ConnectionStrings__Housekeeping`.

## 7. Correctness properties & guard test (mỗi CP một guard — keystone)

| CP / bất biến | Guard test (khi code) | Docker? |
|---|---|---|
| 1-ticket-mở/phòng (partial unique) | `HousekeepingPostgresConstraintTests` (Postgres): 2 ticket mở cùng room→vi phạm; Done rồi tạo mới→cho phép | Postgres |
| idempotent create (guest bấm 2 lần → 1 ticket) | `RequestHousekeepingUseCaseTests` (SQLite): bấm 2 lần→cùng ticketId, 1 bản ghi | Không |
| CP3 rule-gate | `RequestHousekeepingUseCaseTests` (fake IRuleGate: tắt→tạo; bật+chưa ack→rule_ack_required) | Không |
| feature-flag housekeeping_disabled | `RequestHousekeepingUseCaseTests` (config HousekeepingEnabled=false) | Không |
| máy trạng thái + event-log mỗi transition | `HousekeepingStatusUseCaseTests` (SQLite): Requested→InProgress→Done ghi 3 event; Done→InProgress→invalid; complete-by-room/token set Done+method+actor | Không |
| complete-by-token (IRoomTokenResolver) | `CompleteByTokenUseCaseTests` (fake resolver: token→room→Done method=StaffScan; token lạ→lỗi) | Không |
| CP15 concurrency | `HousekeepingConcurrencyTests` (Postgres xmin): hai complete cùng ticket→người sau 409 | Postgres |
| cascade huỷ theo visit (C-GA.5) | `CancelOpenTicketsForVisitUseCaseTests` (SQLite): huỷ ticket mở của visit→Cancelled+event; wiring event defer | Không |
| role guard `/v1/housekeeping/*`=RequireStaff; guest AllowAnonymous | `HousekeepingEndpointAuthTests` (TestServer + fake) | Không |
| boundary | `HousekeepingBoundaryTests` (Contracts thuần; Application⊥Infra/EF/ASP.NET; cross-module chỉ Contracts) | Không |
| Host wiring | `HostEndpointWiringSmokeTests` +InlineData `/v1/housekeeping/*`→401 + guest→problem+json | Không |

Postgres test là gate cuối cho partial-unique/concurrency; không "skip mềm" làm bằng chứng cuối (chạy CI/máy Docker).

## 8. Build slices và cổng dừng

1. **H-Hk.0 — design (file này):** journal + diagnostics 0; chưa code.
2. **H-Hk.1 — Domain/Contracts/Persistence:** ✅ XONG (QR-N-058/QR-AD-041): 2 entity (`HousekeepingTicket : Entity,
   IHasConcurrencyToken` + `HousekeepingEvent : Entity`) + 3 enum (string) + `HousekeepingDbContext` schema `housekeeping`
   keyed + Factory + `HousekeepingConfigurations` (partial unique `ux_hk_open_ticket_room` filter `status IN
   ('Requested','InProgress')` + FK Cascade event→ticket + xmin + 2 index) + `AddHousekeepingInfrastructure` + migration
   `InitialCreate` (verify). Test `HousekeepingBoundaryTests` (3) + `HousekeepingPostgresConstraintTests` (2 Postgres/CI).
   Host wiring + CI bundle DEFER H-Hk.3. `vp all` 0-warning/0-fail; `vp journal` INV-1..6 xanh.
3. **H-Hk.2 — Application:** guest create (idempotent + rule-gate + flag) + status machine (complete-by-room/token/set-status
   + event-log) + guest status read + cancel-for-visit (capability) + read-model + validator + tests (SQLite + Postgres concurrency).
   `ErrorCodeSnapshotTests` +mã Housekeeping.
4. **H-Hk.3 — Api + Host wiring:** guest endpoints (create/status, rule-gate, touch) + admin endpoints (board/complete/status,
   RequireStaff) + `AddHousekeepingApi` + Host wire + CI bundle + `HousekeepingEndpointAuthTests` + smoke +InlineData. Flip QR-AD-0xx Implemented.
5. **C-GA.5 (sau, cross-module):** GuestVisitEnded outbox (GuestAccess) → consumer gọi `CancelOpenTicketsForVisitUseCase`
   + đóng hội thoại Concierge (khi có). Cascade CP9 hoàn tất.

Mỗi slice dừng nếu: build warning/error; JournalConsistency INV-1..6 fail; migration model drift; Docker partial-unique/
concurrency fail; **AD chuyển Implemented mà thiếu Guard-Tests (INV-6)**.

## 9. Quyết định/trade-off (ghi journal khi chốt code)

- **QR-AD-0xx (Housekeeping module):** keyed schema `housekeeping`; 1-ticket-mở/phòng bằng **partial unique** (race-safe,
  cùng lớp CP2/ux_qr_active) + create idempotent (bắt unique-violation → trả existing). KHÔNG bảng rate-limit riêng.
- **QR-AD-0xx (rule-gate trong use case):** Application ref Rules.Contracts; gate trong `RequestHousekeepingUseCase` (defense-in-depth CP3).
- **QR-AD-0xx (enum lưu string):** Status/Method/ActorType `HasConversion<string>()` để partial-filter đọc được + ổn định (verify precedent Rooms).
- **QR-AD-0xx (event-log same-transaction):** ghi `HousekeepingEvent` CÙNG transaction với đổi Status (không mất vết, không lệch).
- **QR-AD-0xx (complete-by-token dùng IRoomTokenResolver):** QR chỉ định phòng, KHÔNG thay đăng nhập (Req 6.5) — staff vẫn RequireStaff.
- **QR-TO-0xx:** rate-limit ticket = dựa 1-mở/phòng + Bedrock global IP limiter (KHÔNG bảng/counter riêng) vs limiter chuyên biệt HousekeepingRateLimitPerHour.
- **QR-TO-0xx:** GET guest status touch hay không (đọc phụ trợ như Rules-GET → KHÔNG touch; POST create touch). Chốt: GET không-touch, POST touch.
- **QR-DV-0xx:** cascade huỷ (CP9) TÁCH sang C-GA.5 (event-driven) — module Housekeeping chỉ cấp use case capability + test, wiring sau.

## 10. Self-validation trước code

- [x] Data model + enum lấy từ product design (không bịa); nơi đặt schema/constraint/xmin chốt rõ (§3).
- [x] Contracts tiêu thụ (`IRoomTokenResolver`/`IRuleGate`/`ICurrentGuestContextResolver`/`IResortGuestConfigQuery`/
      `IResortSettingsQuery`/`ICurrentUser`) đã ĐỌC CHỮ KÝ THẬT (§0).
- [x] 1-ticket-mở/phòng bằng partial-unique lý giải bằng cơ chế Postgres (mirror ux_qr_active/QR-AD-036), guard Postgres.
- [x] Create idempotent (bắt unique-violation→existing); máy trạng thái + event-log mỗi transition (Req 6.8).
- [x] Rule-gate backend (CP3) + feature-flag + concurrency xmin (CP15) + complete app/QR (Req 6.4/6.5) đều có guard + nơi chạy.
- [x] Cascade CP9 tách C-GA.5 (event-driven) — capability thiết kế nay, wiring sau; không premature-couple.
- [ ] User review design trước H-Hk.1 implementation.
