# Wave Housekeeping — thiết kế (ticket dọn phòng + nhật ký) — design-first

> Bám master `design.md` (§data model: HousekeepingTicket/HousekeepingEvent), `docs/.../test-plan.md` P10 + TC-HK-01..06, Req 6/13. CMS thao tác — trạng thái + nhật ký.

## 0. Mô hình (bản chất)
- `HousekeepingTicket`: yêu cầu dọn phòng. Khách tạo (gắn visit/session) HOẶC staff tạo cho phòng trống (visit/session null). Trạng thái: **Requested → InProgress → Done**, hoặc **Cancelled** (khi kết thúc visit). "Mở" = Requested|InProgress.
- `HousekeepingEvent`: nhật ký append-only mỗi lần chuyển trạng thái (Status mới, ByUserId null=khách, OccurredAt).
- **Bất biến sống còn (P10):** đúng **1 ticket "mở"/phòng** → partial unique `ux_hk_open` (room_id WHERE status IN ('Requested','InProgress')) → chống double-tap + trùng. complete-by-room/token chỉ đóng ticket đúng phòng. Mỗi chuyển trạng thái ghi đúng 1 event.

## 1. Index / ràng buộc (`04` §5/§7)
- `ux_hk_open` partial unique (room_id) WHERE status IN ('Requested','InProgress') — enum-string, provider-agnostic (như ux_qr_active), đặt thẳng config.
- `ix_hk_event_ticket` (housekeeping_ticket_id). Cascade event ← ticket. FK ticket → Resort/Room/GuestVisit?/GuestSession? = **Restrict** (visit/session nullable cho staff-ticket).
- Ticket `AuditableEntity` (audit ai tạo + xmin chặn 2 staff hoàn tất đè). Event `Entity` (append-only).

## 2. Lộ trình sub-slice
- **A (✅ DONE — DEC-070):** domain (2 entity + 2 enum) + EF config + **migration `Housekeeping_Init`** (per-wave) + DbSet + test ràng buộc SQLite (ux_hk_open, mở-lại-sau-đóng, cascade event, FK, staff-ticket null). Nền schema.
- **B — guest create + read (✅ DONE — DEC-071):** `POST /api/guest/housekeeping` (idempotent theo phòng — đã có ticket mở → trả lại, không trùng; race ux_hk_open → bắt UniqueConstraintViolationException; ghi event Requested) + `GET /api/guest/housekeeping` (chỉ ticket của lượt). Bảo vệ: `IPortalWindowGuard` + `IRuleGate(Housekeeping)`. Tắt: create→403 forbidden, read→rỗng. Rate-limit per-session/per-hour HOÃN cho task #18 (TK-046) — chống spam tức thời bằng bất biến 1 ticket mở/phòng. 7 test SQLite.
- **C — staff (✅ DONE — DEC-072):** `/api/admin/housekeeping` (`RequireStaff`): list (phân trang + lọc status, OrderBy Id vì SQLite không ORDER BY DateTimeOffset — TK-047); đổi trạng thái (máy trạng thái + event); complete-by-room (App) + complete-by-token (StaffScan, dùng chung helper); staff tạo (visit null, idempotent). 10 test SQLite. **→ WAVE HOUSEKEEPING HOÀN TẤT A+B+C.**
- **Tích hợp cross-module (SAU, cân nhắc):** EndVisit/VisitIdleSweeper hủy ticket mở của visit + ghi event (P9/TC-VIS-04). Hiện `ResolveTokenUseCase`/`PortalWindowGuard` HOÃN cascade này — nối khi có cổng/port phù hợp (tránh coupling GuestAccess↔Housekeeping; qua port như IRuleAckStatusProvider).

## 3. Truy vết
- master design §data model; `docs/.../test-plan.md` P10, TC-HK-01..06, TC-VIS-04; Req 6/13; DEC-006 (enum string), DEC-064 (rule-gate), DEV-026 (portal-window), DEC-008 (sweeper).


---

## 4. Wave D — Realtime (`HousekeepingUpdated` → staff board) — THIẾT KẾ (design-first)

> Nguồn authoritative: `design/backend/21-realtime-signalr.md` §5 (event `HousekeepingUpdated` → group `resort-{id}-staff`, payload `{ ticketId, roomId, status }`) + §6 (notify POST-COMMIT qua `IRealtimeNotifier`). Hạ tầng realtime đã dựng ở Messaging Slice C (DEC-088/089) — port `IRealtimeNotifier`, adapter SignalR, `RealtimeEventNames.HousekeepingUpdated` đã có sẵn. Wave D **chỉ gắn notify** vào use case housekeeping — KHÔNG đụng hub/adapter/schema.

### 4.1 Quyết định (bản chất, kiểm chứng được)
- **Event đi tới GROUP STAFF** (`NotifyStaffAsync(resortId, HousekeepingUpdated)`) — board dọn phòng của lễ tân đồng bộ realtime. Guest KHÔNG cần (khách chỉ quan tâm ticket của mình qua polling `GET /api/guest/housekeeping`). Payload `HousekeepingUpdatedPayload(TicketId, RoomId, Status)` (đặt Application.Housekeeping — module tự khai payload, port giữ trung lập; giống MessageReceivedPayload).
- **Notify POST-COMMIT + CHỈ khi có thay đổi THẬT** (nguyên tắc "no noise", nhất quán MarkRead/Send ở Messaging):
  - `CreateHousekeepingTicket` (guest) / `StaffCreateHousekeepingTicket`: notify **CHỈ khi tạo mới** (`AlreadyOpen=false`). Idempotent trả lại ticket đang mở (`AlreadyOpen=true`) → KHÔNG notify (không có gì đổi).
  - `ChangeHousekeepingStatus`: notify sau commit (transition đã validate → luôn là thay đổi thật).
  - `Complete` (by-room/by-token): notify trong helper `HousekeepingCompletion` post-commit (một chỗ, tránh lặp 2 use case) — `open` ticket = ticketId/roomId/resortId + Status=Done.
  - Thất bại (feature disabled / rule gate / not_found / no_open_ticket) → KHÔNG notify (chỉ notify sau commit thành công).
- **DEFER (ghi TK):** cascade-cancel ticket khi EndVisit (`HousekeepingVisitEndHandler` STAGE-only trong transaction visit-end) — để notify `HousekeepingUpdated(Cancelled)` cần handler báo ngược danh sách ticket đã hủy về `VisitEnder` (nơi post-commit). Phức tạp hơn giá trị (visit-end không phải hot-path board; VisitEnder đã notify `VisitEnded`). Board hơi cũ tới lần refresh/poll kế — chấp nhận MVP.

### 4.2 Churn + wiring
- Thêm `IRealtimeNotifier` vào ctor: `CreateHousekeepingTicketUseCase`, `StaffCreateHousekeepingTicketUseCase`, `ChangeHousekeepingStatusUseCase`, `CompleteHousekeepingByRoomUseCase`, `CompleteHousekeepingByTokenUseCase` (2 cái sau truyền xuống `HousekeepingCompletion.CompleteOpenTicketForRoomAsync(..., notifier)`). `GetGuestHousekeepingUseCase` (đọc) KHÔNG đổi.
- Cập nhật test helper (GuestHousekeepingTests: CreateUc; HousekeepingStaffTests: ChangeUc/ByRoomUc/ByTokenUc/StaffCreateUc) nhận `IRealtimeNotifier?` mặc định `NullRealtimeNotifier` (như Messaging C1).

### 4.3 Test (spy `RecordingRealtimeNotifier`, SQLite)
- Guest create MỚI → 1 `HousekeepingUpdated` (staff), payload TicketId/RoomId/Status=Requested. Create idempotent (AlreadyOpen) → KHÔNG notify.
- ChangeStatus (Requested→InProgress) → notify Status=InProgress. Change trên ticket đã đóng (409) → KHÔNG notify.
- CompleteByRoom → notify Status=Done. CompleteByRoom không có ticket mở (not_found) → KHÔNG notify.
- (Staff create tương tự guest — 1 test đủ đại diện.)

### 4.4 Truy vết
- `21` §5/§6; DEC-088/089 (hạ tầng realtime); DEC-090 (wave này); RealtimeEventNames.HousekeepingUpdated.


### 4.5 TRẠNG THÁI — ✅ Wave D DONE (DEC-090)
- Notify `HousekeepingUpdated` post-commit gắn vào Create (guest/staff, chỉ khi tạo mới) + ChangeStatus + Complete (helper). Board staff đồng bộ realtime; guest dùng polling.
- Test: 4 (guest create-notify + idempotent no-notify; staff change-notify; change-failure no-notify; complete-notify + no-open no-notify). Tổng suite **328 pass + 1 skip**, build 0W/0E.
- Defer: cascade-cancel notify khi EndVisit (TK-058).
⇒ **Wave Housekeeping HOÀN TẤT A+B+C+D** (schema + guest + staff + realtime).
