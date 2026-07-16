# Module design — Concierge (Wave K)

> **Design-first, CHƯA triển khai code.** Chi tiết hóa `../design.md` §2 (module #7 Concierge = Messaging+Notes gộp,
> QR-AD-004). WHAT: `docs/resort-qr-portal/requirements.md` Req 5 (nhắn tin gom theo phòng, 1 hội thoại/visit, reopen,
> realtime), Req 9.4 (ghi chú nội bộ), Req 10.8 (cascade đóng hội thoại khi visit kết thúc — CP9), Req 3.11 (rule-gate
> `/messages`), Req 14 (ChatEnabled + ruleAckRequiredForChat + MaxMessageLength + MessageRateLimitPerMinute). Data model:
> `docs/resort-qr-portal/design.md` §Data Models (Conversation/Message/InternalNote) + §Flows (nhắn tin) + §SignalR Hub.
> Mọi kết luận dựa trên file/code đã ĐỌC. Concierge là module **DỰNG MỚI**. **Module nghiệp vụ CUỐI (8/8)** → hoàn tất mở đường C-GA.5 trọn vẹn.

## 0. Đối soát nguồn (đã verify trên đĩa)

- **Tên "Concierge" (QR-AD-004)**: tránh đụng `Bedrock.Messaging.Contracts` (bus sự kiện hạ tầng base). "Concierge" =
  nhắn tin khách↔lễ tân (nghiệp vụ). Schema `concierge`, keyed `ConciergeModule.PersistenceKey="concierge"`.
- **Contracts đã có + đã đọc chữ ký thật (consumer):**
  1. `Rules.Contracts.IRuleGate.EnsureAcknowledgedAsync(resortId, guestVisitId, GuestFeature.Chat, ct)` (enum có `Chat`) — rule-gate CP3 cho gửi tin.
  2. `GuestAccess.Contracts.ICurrentGuestContextResolver.ResolveAsync(sessionKey, roomId)` → `Result<CurrentGuestContext(GuestVisitId,GuestSessionId,RoomId,ResortId)>` + `TouchAsync`.
  3. `ResortConfig.Contracts.Queries.IResortGuestConfigQuery.GetAsync(resortId)` → `ResortGuestConfig(ChatEnabled, RequireRuleAckForChat, ...)`. `IResortSettingsQuery.GetAsync()` → `ResortSettingsSnapshot(ResortId, MaxMessageLength, MessageRateLimitPerMinute)`.
  4. `ICurrentUser` (Bedrock) — actor staff (reply/read/close/note).
- **Precedent**: guest endpoint (RulesGuestEndpointModule/HousekeepingGuestEndpointModule: resolve→gate-in-usecase→touch/no-store),
  admin (RequireStaff + resortId server), paging (IRoomQueries/PagedResult), partial/unique (ux_qr_active), xmin (IHasConcurrencyToken),
  Housekeeping `CancelOpenTicketsForVisitUseCase` (cascade capability — Concierge có tương tự `CloseConversationForVisit`).
- **SignalR CHƯA có ở codebase** (grep 0) — realtime là năng lực MỚI. `Bedrock.Api` chưa wire SignalR → K-Con.4 thêm ở Host (adapter Api).

## 1. Mục tiêu và bất biến

1. **Một hội thoại/lượt lưu trú (Req 5.2/5.10 — CP-mới)**: unique `Conversation(GuestVisitId)`. Guest gửi tin đầu → tạo
   Conversation cho visit; guest gửi tiếp khi bị đóng (Status=Closed) mà **visit còn Active** → **REOPEN đúng hội thoại đó**
   (Status=Open, append, UnreadForStaff++), KHÔNG tạo hội thoại thứ hai. Race tạo đầu → bắt `UniqueConstraintViolationException`→load existing.
2. **Scope theo visit (Req 5.9)**: guest CHỈ thấy hội thoại của visit HIỆN TẠI của mình; visit mới (quét lại) → hội thoại mới, KHÔNG thấy tin visit cũ.
3. **Rule-gate BACKEND (CP3, Req 3.11)**: `POST /messages` gọi `IRuleGate(Chat)` → `403 rule_ack_required` khi cấu hình yêu cầu mà chưa ack. Enforce trong use case.
4. **Feature-flag `ChatEnabled` (Req 14)**: tắt → guest gửi/đọc trả `chat_disabled`.
5. **Body = PLAIN TEXT (quyết định §9)**: tin nhắn lưu THÔ (trim + giới hạn `MaxMessageLength`), KHÔNG HTML-sanitize (khác
   Rules/FAQ) — vì chat là text (sanitize sẽ mangle ký tự hợp lệ như `<`,`>`); client render bằng textContent (auto-escape), KHÔNG innerHTML.
6. **UnreadForStaff (Req 5.3)**: đếm denormalize trên Conversation — guest gửi → ++; staff read → reset 0. Dashboard badge.
7. **Realtime SignalR (Req 5.5/5.6) + fallback polling**: use case persist rồi đẩy qua **port `IConciergeRealtimeNotifier`**
   (Application) — impl `SignalRConciergeNotifier` (Api, IHubContext). Application THUẦN (fake notifier test được). Guest luôn có
   polling `GET /conversation` (realtime chỉ là tăng tốc, không phải nguồn sự thật). Hub auth-on-join (guest chỉ join hội thoại của session mình).
8. **Ghi chú nội bộ (Req 9.4)**: `InternalNote` gắn phòng HOẶC hội thoại; **KHÔNG BAO GIỜ** lộ cho guest (chỉ endpoint admin RequireStaff).
9. **Cascade đóng hội thoại khi visit-end (Req 10.8/CP9)**: visit end → hội thoại Open của visit → Closed (System). Capability
   `CloseConversationForVisitUseCase`; WIRING event-driven = **C-GA.5** (defer — cùng lúc huỷ ticket Housekeeping).
10. **Concurrency (CP15)**: staff read/close vs guest gửi đồng thời cùng Conversation → xmin (người sau 409, không đè âm thầm).
11. **Boundary (QR-AD-002)**: cross-module chỉ qua Contracts + Id trần; không FK chéo-schema; keyed `concierge`.

## 2. Cấu trúc module và dependency

```text
starhill/src/Modules/Concierge/
  Concierge.Domain         -> Bedrock.Domain
  Concierge.Contracts      -> Bedrock.Messaging.Contracts (ConciergeModule.PersistenceKey; port cascade để C-GA.5)
  Concierge.Application    -> Domain + Contracts + Rules.Contracts + ResortConfig.Contracts + Bedrock.Application (+FluentValidation) + IConciergeRealtimeNotifier (port nội bộ)
  Concierge.Infrastructure -> Application + Bedrock.Infrastructure + EF Core/Npgsql
  Concierge.Api            -> Application + ResortConfig.Contracts + GuestAccess.Contracts + StarHill.Authorization + Bedrock.Api (+SignalR hub + notifier impl)
```

- `IConciergeRealtimeNotifier` (Application port): `NotifyMessageReceivedAsync`/`NotifyConversationUpdatedAsync`/`NotifyMessageReadAsync`.
  Default no-op đăng ký ở Infrastructure (Application dùng được khi chưa có SignalR — K-Con.2); Host OVERRIDE bằng `SignalRConciergeNotifier` (K-Con.4).
- Application ref Rules.Contracts (rule-gate) + ResortConfig.Contracts (config/settings). KHÔNG ref GuestAccess.Contracts (Api resolve context).
- Api ref GuestAccess.Contracts (resolve) + chứa SignalR hub + notifier impl (adapter realtime — Host cắm).

## 3. Domain model (nguồn product design §Data Models — tái dùng nguyên)

- **`Conversation(Id, ResortId, RoomId, GuestSessionId, GuestVisitId, Status, LastMessageAt, LastGuestMessageAt?,
  LastStaffMessageAt?, UnreadForStaff, CreatedAt, ClosedAt?, ClosedByUserId?, RowVersion)`** — Status enum `ConversationStatus{Open,Closed}`.
  Id trần cross-schema. `IHasConcurrencyToken`→xmin (CP15). unique `(GuestVisitId)` (1 hội thoại/visit).
- **`Message(Id, ConversationId, SenderType, SenderUserId?, Body, CreatedAt, ReadByStaffAt?, ReadByGuestAt?)`** — SenderType
  enum `MessageSenderType{Guest,Staff,System}`; Body plain text (giới hạn MaxMessageLength). Append-only (không concurrency token). FK→Conversation Cascade.
- **`InternalNote(Id, ResortId, RoomId?, ConversationId?, AuthorUserId, Body, CreatedAt, UpdatedAt?, RowVersion)`** — ghi chú
  admin; RoomId?/ConversationId? (gắn phòng hoặc hội thoại); `IHasConcurrencyToken` (sửa được → xmin). FK ConversationId?→Conversation (nội-schema, Restrict/SetNull).
- Enum lưu **string** (HasConversion — nhất quán Rooms/Housekeeping; readable filter/board).

### Ràng buộc DB (migration `concierge`)

| Ràng buộc | Cột | Mục đích |
|---|---|---|
| unique `ux_conversation_visit` | `Conversation(GuestVisitId)` | 1 hội thoại/visit (Req 5.2, reopen không tạo mới) |
| index `ix_conversation_resort_room_last` | `Conversation(ResortId, RoomId, LastMessageAt)` | dashboard gom theo phòng + sắp mới nhất (Req 5.3) — order Id/last (xem §7 provider-agnostic) |
| index `ix_message_conversation` | `Message(ConversationId, CreatedAt)` | lịch sử hội thoại |
| index `ix_note_resort_room` | `InternalNote(ResortId, RoomId)` | ghi chú theo phòng |
| FK Cascade | Message→Conversation | tin thuộc hội thoại |
| FK Restrict | InternalNote→Conversation (ConversationId?) | không xoá hội thoại còn note trỏ tới (giữ vết) |
| xmin | Conversation, InternalNote | CP15 |

## 4. Use case (Application)

**Guest:**
- `SendGuestMessageUseCase` (guest): config→ChatEnabled(false→chat_disabled)→**rule-gate(Chat)**→find-or-create/reopen Conversation
  của visit (unique GuestVisitId; Closed+visit-active→reopen Open) → thêm Message(Guest) + UnreadForStaff++ + LastMessageAt/LastGuestMessageAt=now
  → SaveChanges (race tạo→bắt unique→reopen existing) → notifier.NotifyMessageReceived (staff group). Validate body không rỗng + ≤ MaxMessageLength.
- `GetGuestConversationUseCase` (read-only, polling fallback): trả hội thoại của visit hiện tại + messages (đánh dấu ReadByGuest). KHÔNG rule-gate (đọc của mình).

**Staff:**
- `ListConversationsUseCase`/read-model (board gom theo phòng, filter status/unread, phân trang) + `GetConversationUseCase` (chi tiết + messages).
- `ReplyConversationUseCase` (staff): thêm Message(Staff, actorUserId) + LastStaffMessageAt=now + Status=Open (reply mở lại nếu đang Closed?) → notifier (conversation group).
- `MarkConversationReadUseCase` (staff): UnreadForStaff=0 + ReadByStaffAt các message guest → notifier MessageRead.
- `CloseConversationUseCase` (staff): Status=Closed + ClosedAt/ClosedByUserId → notifier ConversationUpdated.
- `CloseConversationForVisitUseCase` (System, cascade C-GA.5): đóng hội thoại Open của visit.

**Notes:** `CreateInternalNoteUseCase`/`UpdateInternalNoteUseCase`/`DeleteInternalNoteUseCase` + list read-model (theo phòng/hội thoại). RequireStaff.

**Read-model** `IConciergeReader` (F9): board hội thoại (paged, order Id-v7), conversation+messages, notes theo phòng, hội thoại Open theo visit (cascade).

## 5. SignalR (K-Con.4) — realtime, isolated

- Hub `ChatHub` tại `/hubs/chat` (Concierge.Api). Groups: `resort-{resortId}-staff` (lễ tân online), `conversation-{conversationId}`.
- Server→Client: `MessageReceived`, `MessageRead`, `ConversationUpdated`. Client→Server: `JoinConversation`/`LeaveConversation`
  (kiểm quyền: guest chỉ join hội thoại của session mình — resolve context, KHÔNG tin id client gửi; staff join resort group theo claim).
- `SignalRConciergeNotifier : IConciergeRealtimeNotifier` (wrap `IHubContext<ChatHub>`) — Host cắm OVERRIDE no-op. Use case gọi notifier SAU persist.
- **Fallback polling luôn hoạt động**: `GET /v1/guest/conversation` (guest) + `GET /v1/conversations` (staff). Realtime chỉ tăng tốc; không phải nguồn sự thật (đơn giản, an toàn nội-mạng).

## 6. Persistence & Host wiring

- `ConciergeDbContext : PlatformDbContext` schema `concierge` keyed; migration + history schema `concierge` (QR-AD-028). Repos/UoW keyed; use case factory thủ công.
- Host: conn `Concierge` + `AddConciergeInfrastructure`/`AddConciergeApi` + migrate + SignalR (K-Con.4: `AddSignalR` + `MapHub<ChatHub>` + override notifier) + CI bundle + appsettings/compose.

## 7. Correctness properties & guard test

| CP / bất biến | Guard test | Docker? |
|---|---|---|
| 1 hội thoại/visit + reopen (unique GuestVisitId) | `ConciergePostgresConstraintTests` (2 conversation cùng visit→vi phạm); `SendGuestMessageUseCaseTests` (gửi lại sau close→reopen cùng id, không tạo mới) | Postgres + SQLite |
| CP3 rule-gate chat | `SendGuestMessageUseCaseTests` (fake gate: tắt→gửi; bật+chưa ack→rule_ack_required) | Không |
| chat_disabled | `SendGuestMessageUseCaseTests` (ChatEnabled=false) | Không |
| body plain-text + length limit | `SendGuestMessageUseCaseTests` (body > MaxMessageLength→validation; body có `<script>` lưu NGUYÊN [không sanitize] — client escape) | Không |
| UnreadForStaff ++/reset | `ConciergeUseCaseTests` (guest gửi→unread++; staff read→0) | Không |
| scope theo visit (guest chỉ thấy visit mình) | `GetGuestConversationUseCaseTests` (visit khác→không thấy) | Không |
| CP15 concurrency | `ConciergeConcurrencyTests` (2 update Conversation→người sau 409) | Postgres |
| cascade đóng khi visit-end (CP9) | `CloseConversationForVisitUseCaseTests`; wiring C-GA.5 | Không |
| realtime notifier gọi sau persist | `SendGuestMessageUseCaseTests` (fake notifier: NotifyMessageReceived gọi 1 lần sau khi lưu) | Không |
| role guard `/v1/conversations`+`/v1/notes`=RequireStaff; guest AllowAnonymous | `ConciergeEndpointAuthTests` | Không |
| SignalR hub auth-on-join (guest chỉ hội thoại mình) | `ChatHubAuthTests` (K-Con.4) | Không |
| boundary | `ConciergeBoundaryTests` (Contracts thuần; Application⊥Infra/EF/ASP.NET/SignalR) | Không |
| Host wiring | `HostEndpointWiringSmokeTests` +InlineData conversations/notes→401 + guest/conversation→problem+json | Không |

## 8. Build slices và cổng dừng

1. **K-Con.0 — design (file này):** journal + diagnostics 0; chưa code.
2. **K-Con.1 — Domain/Contracts/Persistence:** 3 entity + enum + DbContext schema `concierge` + migration (unique visit + FK + xmin) + boundary + Postgres constraint.
3. **K-Con.2 — Application core:** send-message (create/reopen + gate + flag + unread) + reply/read/close + notes CRUD + read-model + `IConciergeRealtimeNotifier` (no-op default) + tests (SQLite + Postgres concurrency) + ErrorCodeSnapshot +mã Concierge.
4. **K-Con.3 — Api REST + Host wiring:** guest (GET /conversation + POST /messages) + admin (conversations/reply/read/close + notes) + AddConciergeApi + Host wire + CI bundle + endpoint auth + smoke.
5. **K-Con.4 — SignalR realtime:** ChatHub + SignalRConciergeNotifier + auth-on-join + Host MapHub + override notifier + hub auth test.
6. **C-GA.5 — cascade (cross-module):** GuestVisitEnded outbox (GuestAccess) → consumer gọi `CloseConversationForVisitUseCase` (Concierge) + `CancelOpenTicketsForVisitUseCase` (Housekeeping). Đóng CP9 trọn vẹn.

Mỗi slice dừng nếu: build warning/error; JournalConsistency INV-1..6 fail; migration drift; Docker unique/concurrency fail; **AD Implemented thiếu Guard-Tests (INV-6)**.

## 9. Quyết định/trade-off (ghi journal khi chốt code)

- **QR-AD-0xx (Concierge module)**: schema `concierge`; 1-hội-thoại/visit bằng unique `ux_conversation_visit` + reopen (không tạo mới); enum string; xmin.
- **QR-AD-0xx (body plain-text KHÔNG sanitize)**: khác Rules/FAQ — chat là text, client render textContent (auto-escape); sanitize sẽ mangle `<`/`>` hợp lệ. Trade-off: dựa client render đúng vs sanitize backend. Chọn plain-text + length-limit; tài liệu hoá hợp đồng render.
- **QR-AD-0xx (realtime qua port IConciergeRealtimeNotifier)**: Application THUẦN (no-op default, SignalR override ở Host) → testable + SignalR isolated. Polling luôn là fallback (realtime không phải nguồn sự thật).
- **QR-AD-0xx (cascade CloseConversationForVisit)**: capability nay, wiring event C-GA.5 (cùng Housekeeping CancelOpenTicketsForVisit).
- **QR-TO-0xx**: reply có tự-reopen hội thoại Closed không (staff reply vào hội thoại đã đóng) — chốt: reply đặt Status=Open (staff chủ động tiếp tục).
- **QR-TO-0xx**: rate-limit tin nhắn = Bedrock global IP limiter + MaxMessageLength vs limiter chuyên biệt MessageRateLimitPerMinute (chốt lúc code).
- **QR-TO-0xx**: order dashboard theo LastMessageAt (Postgres) vs Id-v7 (provider-agnostic, SQLite) — dùng Id-v7 nếu cần test SQLite (bài học QR-N-059).

## 10. Self-validation trước code

- [x] Data model + enum lấy từ product design; nơi đặt schema/constraint/xmin chốt rõ (§3).
- [x] Contracts tiêu thụ (`IRuleGate`/`ICurrentGuestContextResolver`/`IResortGuestConfigQuery`/`IResortSettingsQuery`/`ICurrentUser`) đã ĐỌC chữ ký thật (§0).
- [x] 1-hội-thoại/visit + reopen lý giải bằng unique + active-visit; scope-theo-visit (Req 5.9); rule-gate + flag.
- [x] Body plain-text (không sanitize) — quyết định có lý do (§9), khác Rules/FAQ.
- [x] Realtime tách port (Application thuần, SignalR isolated ở Api/Host); polling fallback luôn có.
- [x] Cascade CP9 (CloseConversationForVisit) capability + wiring C-GA.5; ghép Housekeeping.
- [x] Concurrency xmin; enum string provider-agnostic; order Id-v7 (bài học SQLite QR-N-059).
- [ ] User review design trước K-Con.1 implementation.
