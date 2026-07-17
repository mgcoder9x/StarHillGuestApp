# Module design — Dashboard stats (FE.0b / task 11.1): tổng hợp Host qua query-port mỗi module

> **Design-first, CHƯA code.** WHAT: `docs/resort-qr-portal/requirements.md` Req 9.1 (tổng quan vận hành: hội thoại chưa đọc /
> hội thoại mở / ticket dọn phòng mở / phòng active / lượt ack nội quy trong ngày) + `tasks.md` task 11.1
> (`IDashboardService` + `GET /dashboard/stats`). Dashboard KHÔNG là module — ghép ở **Host**, đọc query-port mỗi
> module qua Contracts (QR-AD-002). Mọi kết luận dựa file đã ĐỌC (§0).

## 0. Đối soát nguồn (đã verify)

- **Dashboard-ở-Host** (QR-AD-002): Host `StarHill.Api` aggregate; cross-module CHỈ qua `<M>.Contracts` (Id trần).
- **resortId** (single-resort): admin endpoint phân giải qua `IResortSettingsQuery.GetAsync()` → `settings.ResortId` (đã đọc `HousekeepingAdminEndpointModule.ResolveResortIdAsync`). Thiếu → `configuration_unavailable`.
- **RequireStaff** (QR-AD-005/020): dashboard vận hành cho Staff+Admin (Admin superset). Pattern `RequireAuthorization(StarHillPolicies.RequireStaff)` + `MapVersionedGroup(V1)` + `ProblemDetailsBuilder`.
- **Nguồn đếm đã verify**:
  - Concierge `Conversation`: `Status ∈ {Open,Closed}` + `UnreadForStaff` (int, denormalize). → open = Status=Open; unread = Conversation có UnreadForStaff>0.
  - Housekeeping `HousekeepingTicket`: `Status ∈ {Requested,InProgress,Done,Cancelled}` + `ResortId`. → open = Status ∈ {Requested,InProgress} (mirror partial-unique).
  - Rooms `Room`: `Status ∈ {Active,Inactive}` + `ISoftDeletable` + `ResortId` trần. → active = Status=Active, chưa soft-delete.
  - Rules `RuleAcknowledgement`: `ResortId` + `AcceptedAt` (DateTimeOffset). → acks-today = ResortId=X ∧ AcceptedAt ≥ đầu-ngày.
- **Chưa có count query cross-module** ở 4 module (reader hiện guest/board-facing) → thêm query-port stats (Contracts) mỗi module.

## 1. Query-port stats mỗi module (Contracts) + Ef impl (Infrastructure)

| Module | Contracts interface (mới) | Trả | Ef impl đọc |
|---|---|---|---|
| Concierge | `IConciergeStatsQuery.GetStatsAsync(resortId, ct)` | `ConciergeStats(OpenConversations, UnreadConversations)` | `db.Conversations` no-tracking: count Status=Open; count UnreadForStaff>0 (scope ResortId) |
| Housekeeping | `IHousekeepingStatsQuery.CountOpenTicketsAsync(resortId, ct)` | `int` | `db.HousekeepingTickets` count Status ∈ {Requested,InProgress} (ResortId) |
| Rooms | `IRoomStatsQuery.CountActiveRoomsAsync(resortId, ct)` | `int` | `db.Rooms` count Status=Active (soft-delete tự lọc qua query filter; ResortId) |
| Rules | `IRulesStatsQuery.CountAcknowledgementsSinceAsync(resortId, since, ct)` | `int` | `db.RuleAcknowledgements` count ResortId ∧ AcceptedAt≥since |

- DTO record thuần (Contracts, KHÔNG marker DI — mirror ResortSettingsSnapshot). Ef impl `AsNoTracking` + `CountAsync`, inject DbContext cụ thể (scoped), đăng ký `AddScoped<Iface, EfImpl>()` trong `AddXInfrastructure` (mirror EfResortSettingsQuery). KHÔNG cần repository/keyed (chỉ đọc-đếm).
- **"since" do HOST truyền** (Rules): tách quyết định "đầu ngày" khỏi module Rules (module chỉ đếm ≥ mốc). Host tính đầu-ngày (§3).

## 2. Ranh giới (QR-AD-002)
- Interface + DTO ở `<M>.Contracts` (public surface). Ef impl ở `<M>.Infrastructure`. Host chỉ chạm interface Contracts (Id trần) → boundary test hiện có vẫn giữ (Contracts thuần; không ref chéo Infra module).
- KHÔNG thêm bảng/entity. Chỉ đọc-đếm trên model sẵn có → migration KHÔNG đổi.

## 3. Host: endpoint tổng hợp
- `DashboardEndpointModule` (Host `StarHill.Api`): `GET /v1/dashboard/stats` `RequireAuthorization(RequireStaff)` `MapVersionedGroup("/dashboard", V1)`.
- Luồng: resolve resortId (`IResortSettingsQuery.GetAsync()` → null → `configuration_unavailable` ProblemDetails) → tính `todayStart` = `IClock.UtcNow.Date` (UTC đầu ngày; tz resort-local là refinement sau — §5) → gọi TUẦN TỰ 4 query (context khác nhau, tránh dùng song song 1 context) → `DashboardStatsResponse(UnreadConversations, OpenConversations, OpenHousekeepingTickets, ActiveRooms, RulesAcksToday)`.
- DTO response ở Host (không lộ entity). `no-store` (số liệu realtime nhẹ). Inject 4 query Contracts + `IResortSettingsQuery` + `IClock`.

## 4. Verification (Docker-free tối đa)
- **Ef stats query mỗi module**: test SQLite (seed vài bản ghi các trạng thái → assert count đúng; scope theo ResortId; acks lọc theo since). Thêm vào integration test project mỗi module (đã có SQLite harness).
- **Host aggregate + auth**: `DashboardEndpointTests` (StarHill.Api.Tests, TestServer + fake 4 query): RequireStaff (no-token 401 / Staff 200 / Admin 200) + aggregate map đúng 5 số + resortId-null→configuration_unavailable. KHÔNG Docker.
- `vp all` build 0-warning + full 0-fail; `vp journal` INV-1..6.

## 5. Quyết định/trade-off (journal khi code)
- **QR-AD-0xx**: query-port stats per-module (Contracts) thay vì Host đọc thẳng DbContext module (giữ QR-AD-002 — Host KHÔNG chạm Infra/DbContext module khác). Trade-off: +1 interface nhỏ/module vs coupling Host↔Infra (chọn interface — sạch ranh giới).
- **QR-AD-0xx (unread = count hội thoại UnreadForStaff>0)**: khớp Req 9.1 "số hội thoại chưa đọc" (đếm HỘI THOẠI, không phải tổng tin). Nếu muốn tổng tin chưa đọc → đổi sang Sum(UnreadForStaff) (chốt lúc code theo đúng chữ Req).
- **QR-TO-0xx (today boundary)**: UTC đầu ngày (đơn giản, nội bộ 1 resort) vs resort-local tz. Chọn UTC nay; tz-local refinement khi ResortSettings có tz (chưa có field tz → không bịa). Host truyền `since` nên đổi sau không đụng module Rules.
- **QR-TO-0xx (tuần tự vs song song 4 query)**: tuần tự (an toàn — mỗi context 1 instance/scope; dashboard poll không hot-path). Song song cần scope riêng → phức tạp, chưa cần.

## 6. Self-validation trước code
- [x] Nguồn đếm mỗi module verify field thật (Conversation.Status/UnreadForStaff; HousekeepingTicket.Status/ResortId; Room.Status/ISoftDeletable/ResortId; RuleAcknowledgement.ResortId/AcceptedAt).
- [x] resortId + RequireStaff + ProblemDetails pattern lấy từ HousekeepingAdminEndpointModule (đã đọc).
- [x] Ranh giới QR-AD-002: query-port Contracts + Ef Infra; Host chỉ chạm Contracts.
- [x] Không đổi schema/migration (chỉ đọc-đếm).
- [x] Verify Docker-free (SQLite count + TestServer auth/aggregate).
- [ ] Triển khai + `vp all`/`vp journal` xanh + journal QR-AD/QR-N.
