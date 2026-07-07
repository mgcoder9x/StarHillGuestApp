# 21 — Realtime SignalR Hub (auth kép, authorize group, contract sự kiện)

> **File authoritative cho:** SignalR Hub `/hubs/chat` — xác thực kép (guest cookie / admin JWT), nhóm (group) & quy tắc **authorize khi join** (server là trọng tài), hợp đồng sự kiện, `IRealtimeNotifier` adapter (notify post-commit), reconnect/fallback, và hướng scale. **Giải quyết GAP-1** (`11` §C).
>
> Realtime là **hạ tầng nền** dùng chung cho Messaging/Housekeeping (wave sau); nhưng **auth + hợp đồng group/sự kiện phải đúng từ base** vì đây là bề mặt rò rỉ dữ liệu giữa các phiên khách.

## 1. Vấn đề bản chất cần giải (GAP-1)

Guest xác thực bằng **cookie** (không JWT); admin bằng **JWT**. Một Hub phục vụ **cả hai**. Rủi ro: nếu tin `conversationId` client gửi khi join, một khách có thể **nghe lén hội thoại của lượt/khách khác**. ⇒ **server phải tự xác định phạm vi được phép**, không tin id client.

## 2. Xác thực kết nối Hub (WebSocket)

- **Admin/Staff:** JWT. **Lưu ý kỹ thuật (chính xác):** trình duyệt **không set được header `Authorization` trên handshake WebSocket** → SignalR gửi access token qua **query string `access_token`**. Backend cấu hình `JwtBearerEvents.OnMessageReceived`: nếu path bắt đầu `/hubs` thì đọc token từ `context.Request.Query["access_token"]`.
  - ⚠️ **Mask `access_token` trong log** (nó nằm ở URL query) — `19` §4.
- **Guest:** cookie `GuestSession` **tự động gửi kèm** trên WS handshake (same-origin) → không cần query token. Middleware nạp `IGuestContext` từ cookie như request thường.
- Hub cho phép cả hai loại principal; phân biệt bằng: có JWT hợp lệ → staff/admin; có guest cookie → guest; không cả hai → từ chối kết nối (hoặc chỉ cho các group công khai — ở đây không có group công khai).

## 3. Nhóm (group) & quy tắc join (server là trọng tài)

| Group | Ai được join | Cách join |
|---|---|---|
| `resort-{resortId}-staff` | **chỉ** staff/admin đã xác thực | server **tự add on connect** nếu principal là staff/admin của resort đó (KHÔNG để client tự join group này) |
| `conversation-{conversationId}` | staff/admin (bất kỳ conv trong resort) **hoặc** guest **của đúng visit sở hữu conv** | qua `JoinConversation` có **authorize** (§4) |

```csharp
public override async Task OnConnectedAsync()
{
    if (_currentUser.IsAuthenticated && _currentUser.Role is "Admin" or "Staff")
        await Groups.AddToGroupAsync(Context.ConnectionId, $"resort-{_resortId}-staff");
    // guest KHÔNG tự vào group nào lúc connect; chỉ join conversation của mình qua JoinConversation
    await base.OnConnectedAsync();
}
```

## 4. `JoinConversation` — authorize bằng dữ liệu server, không tin client

```pascal
ALGORITHM JoinConversation(conversationId)   // client → server
BEGIN
  conv ← db.Conversation.find(conversationId)
  IF conv = null THEN RETURN            // im lặng bỏ qua (không lộ tồn tại)
  IF currentUser.IsStaffOrAdmin AND conv.ResortId == currentUser.ResortId THEN
     Groups.Add(connectionId, "conversation-"+conversationId)         // staff xem được conv trong resort
  ELSE IF guestContext.GuestSessionId != null THEN
     // guest CHỈ join conv thuộc GuestVisit Active của CHÍNH session mình
     visit ← activeVisitOf(guestContext.GuestSessionId, conv.RoomId)
     // (P0) ENFORCE cùng điều kiện với REST — không để connection bypass expiry:
     IF visit = null OR visit.Status != 'Active' OR now > visit.ExpiresAt
                       OR (now - visit.LastSeenAt) > portalWindow
        THEN RETURN                     // từ chối (visit hết hạn/quá portal window) — như session_expired
     IF conv.GuestVisitId == visit.Id THEN
        Groups.Add(connectionId, "conversation-"+conversationId)
        Groups.Add(connectionId, "visit-"+visit.Id+"-guest")   // để EndVisit evict được (§4.1)
     ELSE RETURN                        // từ chối: conv không thuộc visit của guest này
  ELSE RETURN                           // không xác thực → từ chối
END
```

### 4.1 EndVisit → EVICT connection của guest (P0 — không nghe lén sau khi hết hạn)

**Bản chất vấn đề:** authorize chỉ lúc join là **chưa đủ** — connection cũ vẫn nằm trong group `conversation-{id}` và tiếp tục nhận realtime kể cả khi visit đã `Expired`/`Closed` (REST đã trả `session_expired` nhưng SignalR vẫn "nghe lén"). Fix gốc: **khi `EndVisit` chạy** (lazy/sweeper/lễ tân đóng — `13` §5), phải **đẩy guest ra khỏi realtime của visit đó**.

```pascal
// Gọi POST-COMMIT sau khi EndVisit lưu (13 §5): notifier → hub
ALGORITHM OnVisitEnded(visitId)
BEGIN
  hub.Clients.Group("visit-"+visitId+"-guest").Send("VisitEnded", { visitId })   // client hiện overlay "quét lại QR"
  // Evict: bỏ mọi connection của visit khỏi các group + (tùy chọn) abort connection
  FOR conn IN connectionsOfGroup("visit-"+visitId+"-guest")
     Groups.Remove(conn, "conversation-*")        // rời group hội thoại → ngừng nhận realtime
     Groups.Remove(conn, "visit-"+visitId+"-guest")
END
```

- Cần map **visit → connections**: dùng chính group `visit-{visitId}-guest` (guest join lúc §4). SignalR không liệt kê member group trực tiếp → thực thi bằng cách gửi `VisitEnded` để **client tự rời** (`connection.stop()` hoặc leave), **và** server đánh dấu visit hết hạn nên mọi `JoinConversation`/rejoin sau đó bị từ chối (§4). Kết hợp hai lớp: client tự ngắt + server chặn rejoin.
- **Reconnect sau khi hết hạn:** SignalR client reconnect → `OnConnectedAsync`/`JoinConversation` chạy lại §4 → visit không còn Active → **từ chối** → không nghe lại được.
- Thêm port: `IRealtimeNotifier.NotifyVisitEndedAsync(Guid visitId, ct)` (§6), gọi **post-commit** trong `EndVisit`.

- **Bản chất fix (chống rò rỉ):** server **suy ra** conversation hợp lệ từ `IGuestContext`→visit→conversation; **không** tin `conversationId` client gửi để cấp quyền. Guest lượt mới của cùng phòng **không** join được conv của lượt trước (khớp Req 5.9). Property B9.
- `LeaveConversation` chỉ remove group của chính connection (không ảnh hưởng ai khác).

## 5. Hợp đồng sự kiện (server → client)

| Event | Gửi tới group | Payload (DTO) | Khi nào |
|---|---|---|---|
| `MessageReceived` | `conversation-{id}` (+ `resort-{id}-staff`) | `{ conversationId, messageId, senderType, body, createdAt }` | có tin nhắn mới |
| `MessageRead` | `conversation-{id}` | `{ conversationId, readBy, at }` | đánh dấu đã đọc |
| `ConversationUpdated` | `resort-{id}-staff` | `{ conversationId, roomId, unreadForStaff, lastMessageAt, status }` | cập nhật inbox (badge unread) |
| `HousekeepingUpdated` | `resort-{id}-staff` (+ conv nếu cần) | `{ ticketId, roomId, status }` | ticket đổi trạng thái |
| `VisitEnded` | `visit-{visitId}-guest` | `{ visitId }` | visit bị EndVisit (idle/lễ tân đóng) → guest hiện overlay "quét lại QR" + client tự ngắt (§4.1) |

- Client → server: `JoinConversation(conversationId)`, `LeaveConversation(conversationId)` (chỉ hai method, đều authorize/scope như §4).
- Payload là **DTO tối giản** (không trả entity domain); body tin nhắn đã sanitize nếu là rich text (ở đây tin nhắn là text thường + giới hạn độ dài).

## 6. `IRealtimeNotifier` — adapter + notify POST-COMMIT

- Use case **không** tham chiếu Hub; gọi port `IRealtimeNotifier` (`02` §5). Adapter ở Infrastructure dùng `IHubContext<ChatHub>` để push tới group.
- **Notify chỉ SAU khi commit** (DEV-008 / `02` §6.2): tránh bắn `MessageReceived` rồi transaction rollback → staff thấy tin "ma".
- Adapter map: `NotifyConversationAsync(convId, evt)` → `hub.Clients.Group($"conversation-{convId}").SendAsync(evt.Name, evt.Payload)`; `NotifyStaffAsync(resortId, evt)` → group staff.
- **`NotifyVisitEndedAsync(visitId)`** (P0, §4.1) → gửi `VisitEnded` tới group `visit-{visitId}-guest` để guest ngừng nhận realtime khi visit kết thúc. Gọi **post-commit** trong `EndVisit` (`13` §5).

## 7. Reconnect, fallback, scale

- **Reconnect:** client (`packages/realtime`) tự reconnect backoff (Req 17.6); **on reconnect phải join lại group** (staff group tự add lại ở `OnConnectedAsync`; conversation group → client gọi lại `JoinConversation`).
- **Fallback polling:** WS lỗi/không có → guest poll `GET /api/guest/conversation` (Req 17.7 / docs ~15s). Hợp đồng dữ liệu giống nhau để UI không phân biệt.
- **Scale-out (đường mở, MVP single-instance):** nhiều instance → cần **backplane** (Redis backplane cho SignalR) để broadcast xuyên instance, hoặc sticky sessions. MVP một instance không cần. Ghi TK-031 (`20` §7).

## 8. Bảo mật tóm tắt
- Guest không join được `resort-*-staff` (server tự quản group đó).
- Guest chỉ nghe conv của **chính visit mình** (§4) — không rò rỉ giữa lượt/khách.
- `access_token` trên query WS được **mask trong log** (`19`).
- Origin check: WS chỉ chấp nhận từ origin cấu hình (same-origin) — align CORS/CSP `connect-src wss:` (`20` §5).

## 9. Truy vết
- **Validates: Requirements 5.5, 5.6, 5.9, 6 (realtime housekeeping), 17.6, 17.7**
- Property B9 (tách guest/admin, không rò rỉ). Giải quyết GAP-1 (`11` §C, TK-024).
