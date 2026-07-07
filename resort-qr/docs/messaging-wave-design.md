# Wave Design — Messaging / Chat (khách ↔ lễ tân)

> **Nguồn authoritative đã đọc & validate:** `design/backend/21-realtime-signalr.md` (SignalR hub, auth kép, authorize-on-join, EndVisit evict, event contract), `docs/resort-qr-portal/requirements.md` Req 5 + Req 10.8 (cascade), `docs/resort-qr-portal/design.md` §Data Models (Conversation/Message) + §"Nhắn tin gom theo phòng" + reopen (Req 5.10), ai-notes DEC-026/031, TK-050 (Notes FK nợ), TK-024.
>
> **Mục tiêu:** khách gửi/nhận tin nhắn với lễ tân; lễ tân xem inbox gom theo phòng, trả lời, đọc, đóng; realtime qua SignalR + fallback polling. **Bề mặt rò rỉ dữ liệu giữa lượt khách** ⇒ isolation là bất biến số 1.

## 1. Bất biến (Correctness Properties) — enforce ở DB + domain

- **P6/B9 — cô lập lượt lưu trú:** khách CHỈ thấy hội thoại của **chính GuestVisit mình**; khách lượt mới cùng phòng KHÔNG thấy tin lượt trước. Enforce: conversation gắn `GuestVisitId`; guest đọc/gửi qua `PortalWindowGuard` (visit thuộc session + còn hạn); SignalR authorize-on-join theo server-derived visit (21 §4).
- **1 hội thoại / GuestVisit (reopen model, Req 5.10):** partial→**unique tổng** `Conversation(GuestVisitId)`. Staff đóng + guest gửi lại trong cùng visit Active → **reopen** chính conversation đó (Status Open), KHÔNG tạo mới. Visit kết thúc → conversation Closed (cascade) → visit mới = conversation mới.
- **P11 — unread đơn điệu:** `UnreadForStaff` tăng khi guest gửi, về 0 khi staff đọc; không âm.
- **P7 — note nội bộ không lộ:** (đã có) — nay wire FK `InternalNote.ConversationId → Conversation` (TK-050) nhưng note vẫn chỉ qua endpoint RequireStaff.
- **Cascade (Req 10.8):** EndVisit (idle/lễ tân đóng) → đóng conversation Open của visit (Closed) — qua `IVisitEndHandler` (STAGE-only, commit chung 1 SaveChanges, đúng pattern Housekeeping).

## 2. Domain model

```
Conversation : AuditableEntity            // AuditableEntity → CreatedAt/UpdatedAt/actor + RowVersion(xmin)
  Id, ResortId, RoomId, GuestSessionId, GuestVisitId,
  Status (enum ConversationStatus: Open/Closed),
  LastMessageAt, LastGuestMessageAt?, LastStaffMessageAt?,
  UnreadForStaff (int, >=0),
  ClosedAt?, ClosedByUserId?
  -- unique tổng: GuestVisitId (1 conversation / visit — reopen)
  -- index: (ResortId, RoomId, LastMessageAt desc) cho inbox gom theo phòng

Message : Entity                          // append-only; không cần audit interceptor/actor
  Id, ConversationId (FK, cascade), SenderType (enum: Guest/Staff/System),
  SenderUserId? (staff), Body (<= MaxMessageLength), CreatedAt,
  ReadByStaffAt?, ReadByGuestAt?
  -- index: (ConversationId, Id) — Id UUIDv7 time-ordered (SQLite không ORDER BY DateTimeOffset — TK-047)
```

**Quyết định cần chốt khi code (Slice A):**
- `Conversation : AuditableEntity` để có `RowVersion` (xmin). **Rủi ro:** hai lần tăng `UnreadForStaff` đồng thời → 409 concurrency. MVP: 1 khách/visit gửi tuần tự, staff đọc → xung đột hiếm. Nếu thành vấn đề: chuyển unread sang **recompute** (COUNT message chưa đọc) thay vì tăng thủ công. Ghi TK. → **MVP: tăng thủ công + xử lý 409 bằng retry ở caller nếu cần; đo trước, tối ưu sau.**
- `Message : Entity` (không AuditableEntity): tin nhắn bất biến, `SenderUserId` gán tường minh (staff) hoặc null (guest) — KHÔNG lấy từ audit interceptor (khác note). CreatedAt gán bằng `IDateTimeProvider`.

## 3. Persistence & migration (per-wave)

- EF config `ConversationConfiguration`, `MessageConfiguration` (snake_case, enum→string HasMaxLength(16)).
- **Unique** `ux_conversation_visit` trên `conversation(guest_visit_id)` (tổng — reopen). FK `Conversation.GuestVisitId → GuestVisit` (Restrict), `Conversation.RoomId → Room` (Restrict), `Message.ConversationId → Conversation` (**Cascade** — con của aggregate).
- **Wire FK nợ (TK-050):** `InternalNote.ConversationId → Conversation` (Restrict, nullable). Trong `InternalNoteConfiguration` thêm `HasOne<Conversation>().WithMany().HasForeignKey(x=>x.ConversationId).OnDelete(Restrict)`.
- **Migration riêng wave này** (`AddMessaging`): thêm bảng `conversation`, `message` + index + FK note. `dotnet ef migrations has-pending-model-changes` = No changes (TK-045).

## 4. Cascade — MessagingVisitEndHandler

```csharp
public sealed class MessagingVisitEndHandler : IVisitEndHandler, IScopedService
{
    // STAGE-only (không SaveChanges): tìm conversation Open của visit → Status=Closed, ClosedAt=now, ClosedByUserId=endedByUserId.
    // Idempotent: không có conversation Open → no-op. Chỉ đụng entity Conversation (không đọc chéo GuestVisit).
}
```
Auto-wire qua Scrutor (`IEnumerable<IVisitEndHandler>`), giống `HousekeepingVisitEndHandler`. Không sửa GuestAccess.

## 5. Use cases

### Guest (PortalWindowGuard + settings ChatEnabled + RuleGate nếu ruleAckRequiredForChat)
- **SendGuestMessage** `(visitId, rawSessionKey, body)` → guard; nếu ChatEnabled=false → `forbidden`; rule gate; tìm-hoặc-tạo conversation của visit (reopen nếu Closed & visit Active); thêm Message(Guest); `UnreadForStaff++`; cập nhật LastMessageAt/LastGuestMessageAt; refresh sliding-window SAU thành công. Rate-limit `guest-write`. Idempotency chống double-tap.
- **GetGuestConversation** `(visitId, rawSessionKey)` → guard; trả conversation + danh sách message (order by Id); đánh dấu `ReadByGuestAt` cho message của staff. Dùng cho polling fallback.

### Admin (RequireStaff)
- **ListConversationsByRoom** `(status?, roomId?, paging)` → inbox gom theo phòng, badge `UnreadForStaff`, sort LastMessageAt desc.
- **GetConversationDetail** `(conversationId)` → conversation + messages.
- **ReplyToConversation** `(conversationId, body)` → thêm Message(Staff, SenderUserId=currentUser); LastStaffMessageAt; nếu Closed → reopen (Open) tùy chính sách (thống nhất với reopen guest). KHÔNG cho reply nếu visit đã kết thúc? Staff vẫn xem lịch sử; reply tạo tin — cho phép (staff chủ động), nhưng guest chỉ nhận nếu còn visit. Ghi rõ.
- **MarkConversationRead** `(conversationId)` → `UnreadForStaff=0`, `ReadByStaffAt` cho message guest chưa đọc.
- **CloseConversation** `(conversationId)` → Status=Closed, ClosedAt/ClosedByUserId.

## 6. REST endpoints (+ status để enrich OpenAPI ở Slice B)

Guest (`/api/guest`):
- `POST /messages` (rate-limit guest-write) → 200 `SendMessageResult`; 400 (validator/message_too_long); 403 (session_expired/rule_ack_required/chat_disabled); 429; 500.
- `GET /conversation` → 200 `GuestConversationResponse`; 400; 403; 500.

Admin (`/api/admin/conversations`, RequireStaff):
- `GET /` (filter room/status, paging) → 200 `PagedResult<ConversationListItem>`; 400; 401/403; 500.
- `GET /{id:guid}` → 200 `ConversationDetailDto`; 401/403; 404; 500.
- `POST /{id:guid}/reply` → 200/201 `MessageDto`; 400; 401/403; 404; 409 (đã Closed nếu chính sách chặn); 500.
- `POST /{id:guid}/read` → 204; 401/403; 404; 500.
- `POST /{id:guid}/close` → 204; 401/403; 404; 409 (đã Closed); 500.

## 7. SignalR (theo 21 — Slice C)

- Reuse `ChatHub /hubs/chat` + auth kép + `JoinConversation` authorize (21 §4) + EndVisit evict (21 §4.1).
- `IRealtimeNotifier` events post-commit: `MessageReceived` (conversation + staff group), `MessageRead`, `ConversationUpdated` (badge unread, inbox), `VisitEnded` (đã có port `NotifyVisitEndedAsync`).
- Guest fallback polling dùng `GET /api/guest/conversation` (hợp đồng giống realtime).

## 8. Chia sub-slice (mỗi slice 1 increment build+test)

- **Slice A (turn tới):** domain (Conversation/Message + enum) + EF config + unique/FK + **migration `AddMessaging`** + `MessagingVisitEndHandler` (cascade) + wire Notes FK. **KHÔNG API/hub.** Test: unit cascade handler + integration migration/unique (1 conversation/visit) + `has-pending-model-changes`=No changes.
- **Slice B:** use cases guest (send/get) + admin (list/detail/reply/read/close) + REST endpoints + enrich OpenAPI + tests (unread monotonic, reopen, isolation, rule gate/chat disabled, close).
- **Slice C:** `ChatHub` + `IRealtimeNotifier` adapter + events + wire notify post-commit trong send/reply/read/close + EndVisit `NotifyVisitEnded` + tests SignalR (join authorize, no-leak B9, evict on EndVisit).

## 9. Kịch bản test (Given/When/Then — chuẩn nghiệm thu)

- **TC-MSG-A1 (unique/visit):** tạo 2 conversation cùng GuestVisit → lần 2 bị chặn (ux_conversation_visit). (Integration, PostgreSQL-thật-nếu-có; ở đây SQLite bật FK — unique enforce được.)
- **TC-MSG-A2 (cascade):** visit có conversation Open → EndVisit → conversation Closed (qua MessagingVisitEndHandler), idempotent gọi lại no-op.
- **TC-MSG-A3 (Notes FK):** tạo note với ConversationId hợp lệ OK; ConversationId không tồn tại → vi phạm FK.
- **TC-MSG-B1 (send + unread):** guest gửi n tin → UnreadForStaff=n; staff read → 0; không âm.
- **TC-MSG-B2 (reopen, Req 5.10):** staff close; guest cùng visit Active gửi lại → reopen conversation cũ (Open), không tạo conversation 2.
- **TC-MSG-B3 (isolation, P6):** visit A có tin; visit B cùng phòng GET conversation → KHÔNG thấy tin của A.
- **TC-MSG-B4 (rule gate/chat disabled):** ruleAckRequiredForChat=true & chưa ack → 403; ChatEnabled=false → 403.
- **TC-MSG-B5 (session_expired):** quá portal window → POST/GET trả session_expired (403), không cập nhật LastSeenAt (check-before-update).
- **TC-MSG-C1 (join authorize, B9):** guest join conversation của visit khác → bị từ chối (không nghe lén).
- **TC-MSG-C2 (evict on EndVisit):** guest đang join → EndVisit → nhận `VisitEnded`; rejoin bị từ chối.

## 10. An toàn & ranh giới
- `foundation/` không đụng. Module Messaging nằm trong `Modules/` (Application + Infrastructure) đúng ranh giới; giao tiếp liên module qua port (IVisitEndHandler, IRealtimeNotifier).
- Query service coupled DbContext (ConversationQueries) → namespace `Infrastructure.Persistence`, wire tường minh `TryAddScoped` (DEC-049).
- Enum→string; OrderBy Id (UUIDv7); build 0 warning; test PostgreSQL-thật khi có Docker (TK-036), hiện SQLite bật FK.


---

## 11. Slice B-admin — THIẾT KẾ CHI TIẾT ĐÃ CHỐT (design-first, validate trước khi code)

> Bổ sung sau khi đã đọc & đối chiếu code thật: `IHousekeepingQueries`/`EfHousekeepingQueries` (read-model pattern), `ChangeHousekeepingStatusUseCase` (command pattern + `ICurrentUser` + máy trạng thái), `HousekeepingStaffTests` (test pattern), `ResortQrPersistenceExtensions` (wire `TryAddScoped` — scan loại namespace Persistence, DEC-049), `Paging.cs`, `GuestVisitStatus{Active,Closed,Expired}`, `ICurrentUser.UserId`. Nguồn nghiệp vụ: `design.md` dòng 160-161 (`/conversations` gom theo phòng/filter/unread; `/{id}`; `/{id}/reply|read|close` — Staff|Admin), dòng 348-352 (luồng inbox + reopen là hành động GUEST khi visit Active).

### 11.1 Ba điểm mở — CHỐT với lý do bản chất (kiểm chứng được)

**(1) Chính sách staff `reply` khi hội thoại đang `Closed` — CHỐT: reopen NẾU visit còn Active; 409 nếu visit đã kết thúc.**
- Bản chất: `design.md` §6 (dòng 352) định nghĩa reopen là **hành động của GUEST** khi "visit còn Active"; spec **im lặng** về staff-reply-reopen. Mô hình chat hỗ trợ (Zendesk/Intercom) thường "reply mở lại ticket đã đóng" — nhưng điều kiện tiên quyết ở hệ này là **guest phải nhận được**: guest chỉ nhận khi visit còn Active (portal window). Reopen một hội thoại thuộc visit ĐÃ kết thúc = tạo hội thoại "Open zombie" mà guest KHÔNG bao giờ đọc (session_expired) → ô nhiễm inbox + sai ngữ nghĩa.
- Quyết định: `reply` khi `Closed` →
  - visit `Active` → **reopen** (Status=Open, xóa ClosedAt/ClosedByUserId) + append tin staff. Nhất quán tuyệt đối với reopen của guest (cả hai chỉ reopen khi visit Active).
  - visit `Closed`/`Expired` → **409 `ConversationClosed`** (không reopen; guest đã rời, tin không giao được). Staff muốn ghi chú nội bộ → dùng `InternalNote` (P7), đúng công cụ.
- Vì sao KHÔNG chọn "chặn mọi reply khi Closed (409)" (gợi ý ban đầu ở end.md/§5 là "reopen tùy chính sách" — chưa chốt): sẽ kẹt luồng hợp lệ khi staff lỡ đóng sớm một hội thoại của visit CÒN Active và muốn nhắn tiếp; guest vẫn ở phòng, vẫn nhận được → reopen là đúng.
- Vì sao KHÔNG chọn "reopen vô điều kiện": tạo Open-zombie trên visit chết (như trên). Guard của guest vốn đã chặn guest reopen visit chết (session_expired) — staff phải có kiểm tương đương thủ công vì staff KHÔNG đi qua `PortalWindowGuard`.

**(2) "Inbox gom theo phòng" — CHỐT: list phẳng các Conversation kèm `RoomNumber` + badge `UnreadForStaff`, filter `roomId?`/`status?`, phân trang; FE gom hiển thị theo phòng.**
- Bản chất: `design.md` dòng 267/302/348 nói "dashboard **gom hiển thị** theo phòng" — "gom" là việc TRÌNH BÀY ở FE. API trả từng conversation kèm định danh phòng (`RoomNumber`) để FE nhóm mà không N+1. Đúng mô hình read-model đã có (`HousekeepingTicketListItem` cũng kèm `RoomNumber` qua subquery).

**(3) Thứ tự sắp xếp inbox — CHỐT cho MVP: `ORDER BY Id DESC` (UUIDv7, ≈ thời điểm TẠO hội thoại), KHÔNG `LastMessageAt DESC`.**
- Bản chất (mâu thuẫn có thật, kiểm chứng được): thứ tự "hoạt động mới nhất trước" đúng ra là `LastMessageAt DESC`. NHƯNG **TK-047 (đã kiểm chứng, từng làm hỏng `EfHousekeepingQueries`):** provider SQLite (test Docker-free) **ném `NotSupportedException` khi `ORDER BY` một `DateTimeOffset`**. Nếu query order theo `LastMessageAt` (DateTimeOffset) → MỌI lời gọi list ném lỗi trên SQLite → mất sạch khả năng test filter/paging/projection (những thứ provider-agnostic, giá trị nhất). Postgres thì order được.
- Quyết định MVP: order `Id DESC` — provider-agnostic (Guid orderable trên cả SQLite lẫn Postgres), tất định, đã được chứng minh trong `EfHousekeepingQueries`. Đánh đổi: reopen một hội thoại CŨ (Id thấp) có tin mới sẽ KHÔNG nhảy lên đầu (order theo Id tạo, không theo hoạt động). **Chấp nhận cho MVP** vì (a) FE gom theo phòng + badge unread là tín hiệu ưu tiên chính (staff nhìn badge, không nhìn thứ tự toàn cục); (b) fix "đúng recency" = `LastMessageAt DESC` + index `(resort_id, room_id, last_message_at desc)` CHỈ chạy/tối ưu trên Postgres → phải chờ Testcontainers (TK-036) mới verify được, không làm mù quáng. Ghi TK để nâng cấp có kiểm chứng. → **KHÔNG dùng provider-branch (order khác nhau SQLite vs Postgres) vì sẽ khiến test verify hành vi KHÁC production (false green).**
- Chi tiết hội thoại (`GetConversationDetail`) order message theo `Id` (UUIDv7) — message append-only nên Id = thứ tự thời gian THẬT → đúng tuyệt đối, không đánh đổi (TK-047 an toàn).

### 11.2 Hợp đồng (Application/Messaging)

```
// Read-model (query, coupled DbContext → Infrastructure.Persistence, wire TryAddScoped — DEC-049)
IConversationQueries : IScopedService
  Task<PagedResult<ConversationListItem>> ListAsync(Guid? roomId, ConversationStatus? status, PagedRequest, CT)
  Task<ConversationDetailDto?> GetDetailAsync(Guid conversationId, CT)   // null → 404

ConversationListItem(Guid ConversationId, Guid RoomId, string RoomNumber, ConversationStatus Status,
                     int UnreadForStaff, DateTimeOffset LastMessageAt,
                     DateTimeOffset? LastGuestMessageAt, DateTimeOffset? LastStaffMessageAt)
ConversationDetailDto(Guid ConversationId, Guid RoomId, string RoomNumber, ConversationStatus Status,
                      int UnreadForStaff, DateTimeOffset LastMessageAt, DateTimeOffset? ClosedAt,
                      IReadOnlyList<AdminMessageDto> Messages)
AdminMessageDto(Guid Id, MessageSenderType SenderType, Guid? SenderUserId, string Body,
                DateTimeOffset CreatedAt, DateTimeOffset? ReadByStaffAt, DateTimeOffset? ReadByGuestAt)

// Commands
ReplyToConversationInput(Guid ConversationId, string? Body)  → IUseCase<_, ReplyToConversationResult>
ReplyToConversationResult(Guid MessageId, DateTimeOffset CreatedAt, ConversationStatus Status)  // Status=Open khi thành công
MarkConversationReadInput(Guid ConversationId)              → ICommandUseCase   // 204
CloseConversationInput(Guid ConversationId)                 → ICommandUseCase   // 204
```

### 11.3 Use cases (RequireStaff ở tầng endpoint; use case KHÔNG tự check role)

- **ReplyToConversationUseCase**(`IUnitOfWork, IDateTimeProvider, ICurrentUser`): load Conversation (FindById) — null → `ConversationNotFound`(404). Body trim; rỗng → Validation; > `settings.MaxMessageLength`(fallback 2000, lấy theo `conversation.ResortId`) → `MessageTooLong`(400). Nếu `Closed`: load `GuestVisit` (theo `GuestVisitId`); visit `Active` → reopen (Open, clear Closed*); ngược lại → `ConversationClosed`(409). Add `Message(Staff, SenderUserId=_currentUser.UserId, Body, CreatedAt=now)`; `LastStaffMessageAt=now`, `LastMessageAt=now`. **KHÔNG** đổi `UnreadForStaff` (đó là guest→staff) và **KHÔNG** auto-mark-read (orthogonal — luồng `design.md` dòng 348 tách "read" và "reply"). 1 SaveChanges. **KHÔNG** gate `ChatEnabled` (toggle này quản truy cập GUEST; staff quản dữ liệu sẵn có — ghi DEC).
- **MarkConversationReadUseCase**(`IUnitOfWork, IDateTimeProvider`): load Conversation — null → 404. `UnreadForStaff=0`; set `ReadByStaffAt=now` cho các Message của GUEST còn null. Chạy được bất kể Open/Closed (staff đọc tin cuối của hội thoại đã đóng). Idempotent. 204.
- **CloseConversationUseCase**(`IUnitOfWork, IDateTimeProvider, ICurrentUser`): load — null → 404; đã `Closed` → `ConversationClosed`(409); ngược lại Status=Closed, ClosedAt=now, ClosedByUserId=_currentUser.UserId. 204.
- Validators: `ReplyToConversationValidator` (ConversationId NotEmpty, Body NotEmpty), `MarkConversationReadValidator`/`CloseConversationValidator` (ConversationId NotEmpty).

### 11.4 EfConversationQueries (Infrastructure.Persistence, inject base `ResortQrDbContext`, `AsNoTracking`)
- `ListAsync`: `Set<Conversation>()` + filter `roomId`/`status` optional; `LongCountAsync` total; `OrderByDescending(c => c.Id)` (11.1-(3)); Skip/Take theo `PagedRequest`; project `ConversationListItem` (RoomNumber qua subquery `Set<Room>().Where(r=>r.Id==c.RoomId).Select(RoomNumber).FirstOrDefault() ?? ""` — như housekeeping).
- `GetDetailAsync`: lấy conversation (FirstOrDefault theo Id, AsNoTracking); null → return null; messages `Set<Message>().Where(ConversationId==id).OrderBy(m=>m.Id)` project `AdminMessageDto`; RoomNumber subquery.
- Wire: `services.TryAddScoped<Application.Messaging.IConversationQueries, EfConversationQueries>();` trong `ResortQrPersistenceExtensions`.

### 11.5 REST (`/api/admin/conversations`, `RequireStaffPolicy`) + OpenAPI enrich (DEC-080/083)
- `GET /` (query: roomId?, status?, page, pageSize) → 200 `PagedResult<ConversationListItem>`; 400 (status filter sai); `ProducesAdminAuthProblems()`.
- `GET /{id:guid}` → 200 `ConversationDetailDto`; 404; admin-auth.
- `POST /{id:guid}/reply` {Body} → 200 `ReplyToConversationResult`; 400 (validator/message_too_long); 404; 409; admin-auth.
- `POST /{id:guid}/read` → 204; 404; admin-auth.
- `POST /{id:guid}/close` → 204; 404; 409; admin-auth.
- Đăng ký `app.MapResortQrAdminMessagingEndpoints();` trong `Program.cs` (cạnh các Map admin khác).

### 11.6 Test (integration, SQLite bật FK; mirror HousekeepingStaffTests + GuestMessagingTests)
- **Reply**: (a) Open → append, LastStaffMessageAt set, SenderUserId=staff, UnreadForStaff KHÔNG đổi; (b) Closed + visit Active → reopen (Open, ClosedAt null) + append; (c) Closed + visit Expired → 409 conflict; (d) không tồn tại → 404; (e) body > MaxMessageLength → 400 message_too_long.
- **MarkRead**: unread=n + tin guest → read → unread=0, ReadByStaffAt set cho tin guest, tin staff không đụng; idempotent (gọi 2 lần vẫn 0); not found → 404.
- **Close**: Open → Closed + ClosedByUserId=staff; đã Closed → 409; not found → 404.
- **List (query)**: 2 conversation 2 phòng + filter status/roomId + RoomNumber đúng + total đúng; paging.
- **Detail (query)**: message order theo Id (chèn nhiều tin) + RoomNumber; null khi id lạ.
- **Isolation (đối chiếu B3):** list/detail là read-model admin (không cô lập theo visit — staff xem tất) → KHÔNG cần test isolation ở admin; isolation là bất biến phía GUEST (đã phủ B3).


---

## 12. Slice C — SignalR realtime — THIẾT KẾ (design-first) + chia C1/C2

> Nguồn authoritative: `design/backend/21-realtime-signalr.md` (auth kép, authorize-on-join, EndVisit evict, event contract §5), DEC-026/031. **Đã verify code THẬT (không bịa):** KHÔNG tồn tại `IRealtimeNotifier`/`ChatHub`/`IGuestContext` (grep 0 kết quả) — DEV-019 P0-1 ghi rõ IGuestContext "chưa hiện thực". JWT claims chỉ `sub`/`role`/`jti` (JwtTokenService) — CHƯA có resortId. Auth cấu hình ở `ResortQrAuthExtensions` (resort-qr, KHÔNG phải foundation) → được phép thêm `OnMessageReceived`. `PortalWindowGuard.ValidateAsync(visitId, rawSessionKey)` là nguồn luật cổng guest dùng chung.

### 12.1 Quyết định nền (bản chất, kiểm chứng được)

- **KHÔNG dựng `IGuestContext` mới cho hub** — tái dùng `PortalWindowGuard`. Bản chất: hub cần biết "guest này có sở hữu conversation không + còn hạn không" = ĐÚNG hợp đồng `PortalWindowGuard.ValidateAsync(conv.GuestVisitId, rawCookie)`. Hub đọc cookie guest RAW từ `Context.GetHttpContext().Request.Cookies[cookieName]` rồi gọi guard → nếu Ok = authorized (cùng enforce với REST, DEC-031 "một nguồn luật"). Tránh nhân bản luật + tránh xây IGuestContext (YAGNI, chưa cần cho MVP). Ghi: nếu sau này nhiều nơi cần guest identity trong hub → cân nhắc IGuestContext middleware (DEV-019 P0-1).
- **Staff group `resort-{resortId}-staff`:** SEND side có resortId (từ `conversation.ResortId`) — luôn biết. RECEIVE side (staff join group lúc connect) cần resortId của staff: MVP single-resort (DEC-028) → **lookup `AppUser.ResortId`** theo `sub` claim lúc `OnConnectedAsync` (1 PK-lookup/connection, KHÔNG phải/message). KHÔNG thêm claim resortId vào JWT ở C này (tránh đụng login/token + auth test); nếu cần tối ưu sau → thêm claim (đường mở).
- **Notify POST-COMMIT trong use case** (design §6, DEV-008): use case gọi port `IRealtimeNotifier` SAU `SaveChanges` — tránh bắn event rồi rollback (staff thấy tin "ma"). Adapter (C2) PHẢI nuốt lỗi transport (không ném vào use case — thao tác đã commit không được vỡ vì realtime lỗi).

### 12.2 Chia increment (mỗi cái build+test xanh độc lập)

- **C1 — Port + notify wiring (KHÔNG hub, verify Docker-free):**
  - Port `ResortQr.Application.Realtime.IRealtimeNotifier`: `NotifyConversationAsync(convId, RealtimeEvent)`, `NotifyStaffAsync(resortId, RealtimeEvent)`, `NotifyVisitEndedAsync(visitId)`. `RealtimeEvent(string Name, object Payload)` (generic, transport-agnostic — khớp adapter map §6). Tên event: `RealtimeEventNames` (MessageReceived/MessageRead/ConversationUpdated/HousekeepingUpdated/VisitEnded — §5).
  - Payload records (Application.Messaging): `MessageReceivedPayload`, `ConversationUpdatedPayload`, `MessageReadPayload`.
  - `NullRealtimeNotifier` (Application.Realtime, no-op, KHÔNG marker) — đăng ký `TryAddSingleton` (C2 sẽ `Replace` bằng adapter SignalR). Lý do Null-default: chưa có hub thì realtime im lặng, polling fallback vẫn chạy (Req 17.7); DI luôn resolve được.
  - Gắn notify post-commit: **SendGuestMessage** (→ Conversation:MessageReceived + Staff:ConversationUpdated), **ReplyToConversation** (→ Conversation:MessageReceived + Staff:ConversationUpdated), **MarkConversationRead** (→ Staff:ConversationUpdated + Conversation:MessageRead nếu có tin được đánh dấu), **CloseConversation** (→ Staff:ConversationUpdated status=Closed), **VisitEnder** (→ NotifyVisitEnded khi thực sự kết thúc).
  - Test (spy `RecordingRealtimeNotifier`): mỗi use case THÀNH CÔNG → ghi đúng event/payload; THẤT BẠI (session_expired / 409 closed+ended / message_too_long) → KHÔNG ghi gì (chứng minh notify chỉ sau commit, không bắn khi lỗi). VisitEnder no-op → không ghi.
  - **DEFER sang C2:** notify guest-read từ `GetGuestConversation` (read-receipt, ít quan trọng); notify khi lazy-expiry qua `PortalWindowGuard`/`ResolveToken` (guest kích hoạt vốn đã bị reject; eviction connection lingering là edge — ghi TK).
- **C2 — Hub + adapter + auth + evict (verify harness SignalR-over-TestServer TRƯỚC khi xây full):**
  - `AddSignalR` + `ChatHub : Hub` map `/hubs/chat`. `OnConnectedAsync`: staff/admin (principal `Context.User` role) → lookup AppUser.ResortId → join `resort-{resortId}-staff`; guest → không auto-join.
  - `JoinConversation(conversationId)`: staff (resort khớp) → join `conversation-{id}`; guest → `PortalWindowGuard.ValidateAsync(conv.GuestVisitId, cookie)` Ok → join `conversation-{id}` + `visit-{visitId}-guest`; else im lặng return (B9 — không lộ tồn tại, không nghe lén). `LeaveConversation` chỉ remove group của chính connection.
  - JWT qua query `access_token` cho path `/hubs`: thêm `OnMessageReceived` trong `ResortQrAuthExtensions` (mask access_token trong log — §8/`19`).
  - Adapter `SignalRRealtimeNotifier : IRealtimeNotifier` (Infrastructure/Api, `IHubContext<ChatHub>`) — map Notify* → `Clients.Group(...).SendAsync(evt.Name, evt.Payload)`; **try/catch nuốt lỗi**. `Replace` Null trong DI.
  - EndVisit evict: `NotifyVisitEndedAsync` → gửi `VisitEnded` tới `visit-{id}-guest`; client tự ngắt + server chặn rejoin (visit không Active → guard fail). Gắn vào VisitEnder (đã có NotifyVisitEnded ở C1).
  - Test: **C2-0** verify SignalR client kết nối được TestServer (hub trivial) TRƯỚC; **C2-1** guest join conversation của visit KHÁC → bị từ chối (B9 no-leak); **C2-2** guest join hợp lệ → nhận MessageReceived khi có reply; **C2-3** EndVisit → nhận VisitEnded + rejoin bị từ chối.

### 12.3 Rủi ro đã nhận diện (không bịa là đã giải quyết)
- Test SignalR qua `WebApplicationFactory`/TestServer cần `HttpMessageHandler` của TestServer cho `HubConnection` — kỹ thuật chuẩn nhưng CHƯA verify trong repo này → C2 bước đầu tiên là dựng 1 hub trivial + 1 test kết nối để chứng minh harness, rồi mới xây auth/authorize. KHÔNG viết full rồi mới chạy.
- Guest cookie trên WS handshake trong test: client phải gửi cookie — verify ở C2-0.


---

## 13. TRẠNG THÁI TRIỂN KHAI (cập nhật 2026-07-06)

- **Slice A** (domain + EF + migration `AddMessaging` + cascade + Notes FK) — ✅ DONE (DEC-085).
- **Slice B-guest** (SendGuestMessage/GetGuestConversation + REST + test) — ✅ DONE (DEC-086).
- **Slice B-admin** (List/Detail/Reply/MarkRead/Close + `IConversationQueries` + REST + OpenAPI + test) — ✅ DONE (DEC-087).
- **Slice C1** (port `IRealtimeNotifier` + notify POST-COMMIT + NullRealtimeNotifier + spy test) — ✅ DONE (DEC-088).
- **Slice C2** (`ChatHub` + adapter + auth kép + JoinConversation authorize + EndVisit evict + staff group + JWT query) — ✅ DONE (DEC-089).
- **Tổng test:** 329 pass + 1 skip (unit 76 + arch 5 + integration 248 + 1 skip); build 0W/0E; migration `has-pending-model-changes` = No changes. (Gồm Housekeeping realtime DEC-090 + guest-read receipt DEC-091.)
- **Defer nhỏ (TK-058):** ~~notify guest-read~~ (✅ DEC-091); notify lazy-expiry qua guard; cascade-cancel/close notify khi EndVisit (DEC-091 (b) — phân tích defer); backplane đa-instance (TK-031).
- **Kỹ thuật test hub tái dùng:** TK-059. **Transport test:** TRD-012. **access_token log:** TK-060 (verify path-only).

⇒ **Module Messaging/Chat (khách ↔ lễ tân) hoàn tất theo `21` + Req 5 + Req 10.8**, realtime phát thật + polling fallback, cô lập lượt khách (P6/B9) enforce ở cả REST lẫn SignalR.
