# 13 — GuestAccess: đặc tả hành vi đầy đủ (resolve, visit lifecycle, portal window)

> **File authoritative cho:** hành vi chi tiết của module GuestAccess — thuật toán resolve, vòng đời `GuestVisit`, cửa sổ thao tác, lazy idle-expiry, xử lý race đồng thời, cascade khi kết thúc visit, và ma trận edge-case. `05-algorithms-and-specs.md` §3 là bản tóm tắt; **file này là bản đầy đủ**.
>
> Đây là **luồng xương sống** của base (Req 1, 10). Sai một edge-case ở đây → khách thấy dữ liệu của khách khác, hoặc phiên không bao giờ hết hạn. Vì vậy đặc tả kỹ, kiểm chứng được, kèm pre/post/invariant.

## 1. Ba mốc thời gian — phân biệt rõ (nguồn nhầm lẫn thường gặp)

| Khái niệm | Giá trị | Tính từ | Ai kiểm tra | Hậu quả khi quá hạn |
|---|---|---|---|---|
| **Portal window** | mặc định 30' (`PortalWindowMinutes`) | `GuestVisit.LastSeenAt` | **endpoint tương tác** (rules/faq/messages/housekeeping/conversation) | `session_expired` → khách **quét QR lại** để mở khóa (KHÔNG tạo visit mới) |
| **Idle expiry** | mặc định 24h (`VisitIdleExpiryHours`) | `GuestVisit.LastSeenAt` (lưu sẵn ở `ExpiresAt = LastSeenAt + idle`) | **resolve (lazy)** + **sweeper (nền)** | visit → `Expired`; quét lại tạo **visit mới** |
| **Cookie GuestSession** | 30–90 ngày | phát/gia hạn ở resolve | middleware auth | hết hạn → thiết bị coi như mới |

**Bất biến nền:** cả Portal window lẫn Idle expiry đều suy từ **`GuestVisit.LastSeenAt`** (không dùng `GuestSession.LastSeenAt`). `ExpiresAt` được vật hoá (materialized) = `LastSeenAt + idle` để index/sweeper truy vấn nhanh.

**Sliding window (chốt semantics — Req 10.5/10.6):** mỗi **hoạt động thành công trong hạn** (resolve HOẶC endpoint tương tác) **đẩy `LastSeenAt=now`** → cửa sổ trượt theo hoạt động (phiên còn dùng thì còn sống). Khi **đã quá hạn** mới phải **quét QR lại**; endpoint tương tác quá hạn KHÔNG tự gia hạn (kiểm-tra-trước-cập-nhật-sau, §4) ⇒ vẫn hết hạn được (B8). Idle-expiry 24h là trần cứng cũng trượt theo `LastSeenAt`.

## 2. Vì sao cần LAZY idle-expiry ở resolve (không chỉ dựa sweeper)

**Bản chất vấn đề (đúng đắn, fix gốc):** Req 10.7 có background `VisitIdleSweeper` chuyển visit quá `ExpiresAt` → `Expired`. Nhưng sweeper chạy theo chu kỳ (mặc định 5'). Nếu một resolve tới **sau `ExpiresAt` nhưng trước lần quét kế của sweeper**, mà logic chỉ xét `Status == Active`, thì sẽ **nối lại nhầm** một visit đã idle > 24h → **khách mới của phòng thấy hội thoại/ack của lượt trước** (vi phạm Req 10.5/10.8, rò rỉ dữ liệu giữa các lượt khách).

**Nguyên tắc:** *tính đúng đắn KHÔNG được phụ thuộc thời điểm chạy của sweeper.* Sweeper chỉ là **lưới an toàn/dọn dẹp** (đóng cascade cho visit không ai đụng tới). Resolve phải **tự kiểm tra `now > ExpiresAt`** (lazy expiry) và coi visit đó như đã hết hạn.

## 3. Thuật toán ResolveToken (đầy đủ, có race + lazy expiry)

```pascal
ALGORITHM ResolveToken(rawToken, httpCtx, now)   // now = clock.UtcNow
PRE:  rawToken là chuỗi từ URL /r/{token}
BEGIN
  // (1) Phân giải token → phòng (không lộ phòng khác nếu lỗi)
  token ← lookup RoomQrToken WHERE Token = rawToken     // dùng ux_qrtoken_token (UNIQUE)
  IF token = null                THEN RETURN error(qr_invalid)      // 404
  IF token.Status = 'Revoked'    THEN RETURN error(qr_revoked)      // 404
  room ← token.Room
  IF room = null OR room.IsDeleted OR room.Status != 'Active'
                                 THEN RETURN error(room_inactive)   // 409
  resort ← room.Resort

  // (2) Định danh thiết bị qua cookie GuestSession (chỉ /resolve mới TẠO mới — 03 §4b)
  //     Middleware GuestCookieRead (03 §4a) đã nạp IGuestContext nếu có cookie hợp lệ.
  session ← (guestContext.GuestSessionId != null) ? load(guestContext.GuestSessionId) : null
  IF session = null THEN
     rawKey ← newSessionKey()                          // CSPRNG secret (raw)
     session ← createGuestSession(SessionKeyHash = hash(rawKey))   // DB lưu HASH, không lưu raw
     setGuestCookie(httpCtx, rawKey)                   // cookie chứa secret RAW, HttpOnly/Secure/SameSite=Lax
  ELSE
     session.LastSeenAt ← now

  // (3) Xác định GuestVisit — LAZY EXPIRY + nối lại đúng 1 visit
  visit ← query GuestVisit
            WHERE GuestSessionId = session.Id AND RoomId = room.Id AND Status = 'Active'
            ORDER BY StartedAt DESC LIMIT 1              // dùng ux_visit_active
  IF visit != null AND now > visit.ExpiresAt THEN
     // visit "Active" nhưng đã idle quá hạn (sweeper chưa kịp) → kết thúc ngay
     EndVisit(visit, reason = IdleExpired, now)          // §5, cascade, idempotent
     visit ← null
  IF visit = null THEN
     TRY
        visit ← createGuestVisit(session, room, now)     // Status=Active, LastSeenAt=now,
                                                         // ExpiresAt=now+idle, StartedAt=now
        saveChanges()                                    // có thể ném unique(ux_visit_active)
     CATCH UniqueViolation(ux_visit_active)
        // race: request song song vừa tạo visit Active cho (session, room)
        visit ← re-query GuestVisit Active (session, room)   // dùng lại, không lỗi ra khách
  ELSE
     // nối lại visit đang Active còn trong hạn idle
     visit.LastSeenAt ← now
     visit.ExpiresAt  ← now + idle                       // refresh cửa sổ (sliding window). Endpoint tương tác cũng refresh khi thành công TRONG hạn (§4); điểm riêng của /resolve là được phép nối lại/mở khóa khi ĐÃ quá portal window.

  saveChanges()

  // (4) Trả ResolveResponse (features từ ResortSettings; ack theo publication IsCurrent)
  RETURN ok(BuildResolveResponse(room, resort, resort.EnabledLanguages,
                                 visit, ruleAckState(visit), features(settings)))
POST:
  - Trả đúng phòng của token hợp lệ; KHÔNG lộ phòng khác khi lỗi (Req 1.4).
  - Sau resolve tồn tại ĐÚNG MỘT GuestVisit Active cho (session, room) (ux_visit_active giữ bất biến).
  - Visit được nối lại chỉ khi Active AND now ≤ ExpiresAt; ngược lại là visit MỚI (Req 10.5).
  - **Semantics cửa sổ = SLIDING WINDOW (đã chốt):** cả `/resolve` LẪN endpoint tương tác **thành công TRONG hạn** đều refresh `LastSeenAt=now` (và `ExpiresAt=now+idle`) — hoạt động kéo dài phiên (Req 10.6). Điểm khác biệt: **khi ĐÃ quá portal window** (`now-LastSeenAt>PortalWindow`), endpoint tương tác trả `session_expired` và **KHÔNG** refresh (Req 10.5); lúc đó **chỉ `/resolve` (quét lại QR)** mới mở khóa. Thứ tự "kiểm-tra-TRƯỚC, cập-nhật-SAU" (§4) đảm bảo cửa sổ vẫn luôn có thể hết hạn (B8) dù là sliding.
END
```

**Ghi chú xử lý race (chính xác):** hai điểm race được xử lý bằng **DB là trọng tài**, không bằng lock ứng dụng:
- Tạo `GuestVisit` trùng → chặn bởi `ux_visit_active` → bắt unique-violation, re-query, dùng lại.
- Tạo `GuestSession` trùng (2 tab, chưa có cookie): chấp nhận thiết bị có thể sinh 2 session trong khe rất hẹp trước khi cookie set (tác hại tối thiểu: 2 định danh thiết bị). Không đáng đánh đổi phức tạp để chặn tuyệt đối.

## 4. EnforcePortalWindow (cho endpoint tương tác — thứ tự SỐNG CÒN)

```pascal
ALGORITHM EnforcePortalWindow(visit, now, portalMinutes, idleHours)
PRE: visit thuộc đúng GuestSession hiện tại (từ cookie); visit != null
BEGIN
  IF visit = null OR visit.Status != 'Active'
       THEN RETURN error(session_expired)                 // 403
  IF now > visit.ExpiresAt                                 // lazy idle-expiry cả ở đây
       THEN { EndVisit(visit, IdleExpired, now); RETURN error(session_expired) }
  IF (now - visit.LastSeenAt) > portalMinutes
       THEN RETURN error(session_expired)                  // KHÔNG cập nhật LastSeenAt

  result ← proceed(handler)                                // xử lý nghiệp vụ

  // CHỈ cập nhật SAU khi xử lý thành công (nếu cập nhật trước kiểm tra → gia hạn vô hạn)
  visit.LastSeenAt ← now
  visit.ExpiresAt  ← now + idleHours
  saveChanges()
  RETURN result
POST:
  - Endpoint tương tác KHÔNG tự refresh khi đã quá portal window ⇒ cửa sổ LUÔN có thể hết hạn (Property B8).
  - Chỉ /resolve (quét lại) mới mở khóa lại (§3 bước 3, nhánh nối lại).
END
```

> **Lý do thứ tự (kiểm tra TRƯỚC, cập nhật SAU):** nếu cập nhật `LastSeenAt=now` trước khi kiểm tra ở mọi request, mỗi lần gọi sẽ tự đẩy hạn → cửa sổ **không bao giờ** hết hạn (bug đã cảnh báo trong docs cũ). Đây là bất biến B8.

## 5. EndVisit — cascade idempotent (đóng conversation, hủy ticket)

```pascal
ALGORITHM EndVisit(visit, reason, now)     // reason ∈ {ClosedByStaff, IdleExpired}
PRE: visit != null
BEGIN
  IF visit.Status != 'Active' THEN RETURN            // idempotent: đã kết thúc thì bỏ qua
  visit.Status   ← (reason = ClosedByStaff) ? 'Closed' : 'Expired'
  visit.ClosedAt ← now
  IF reason = ClosedByStaff THEN visit.ClosedByUserId ← currentUser.UserId

  // Cascade (Req 10.8) — ở tầng ứng dụng, KHÔNG xóa dữ liệu (giữ lịch sử)
  FOR conv IN conversations WHERE GuestVisitId = visit.Id AND Status = 'Open'
       conv.Status ← 'Closed'; conv.ClosedAt ← now
  FOR ticket IN housekeeping_tickets
       WHERE GuestVisitId = visit.Id AND Status IN ('Requested','InProgress')
       ticket.Status ← 'Cancelled'
       append HousekeepingEvent(ticket, 'Cancelled', ActorType=System, now)   // nhật ký (Req 6.8)
  saveChanges()
  // POST-COMMIT (sau khi lưu): đẩy guest ra khỏi realtime của visit này (P0 — 21 §4.1)
  realtimeNotifier.NotifyVisitEndedAsync(visit.Id)     // gửi VisitEnded + chặn rejoin
POST:
  - Sau EndVisit: guest của visit này KHÔNG post được nữa (visit không Active) — Req 10.8.
  - Hàm idempotent: gọi lại (sweeper + lazy-expiry cùng lúc) không gây tác dụng kép.
  - Dữ liệu lịch sử (message/ack/ticket) VẪN còn để lễ tân tra cứu; chỉ đổi trạng thái.
END
```

**Ai gọi EndVisit:** (a) lễ tân đóng thủ công `POST /guest-visits/{id}/close` (reason=ClosedByStaff); (b) resolve/EnforcePortalWindow khi phát hiện `now > ExpiresAt` (lazy, reason=IdleExpired); (c) `VisitIdleSweeper` nền (reason=IdleExpired). Cả ba dùng chung một hàm idempotent → không lệch hành vi.

## 6. VisitIdleSweeper (lưới an toàn, không phải nguồn đúng đắn)

```pascal
ALGORITHM SweepIdleVisits(now, batchSize)     // chạy mỗi ~5' (Req 14.4)
BEGIN
  candidates ← query GuestVisit WHERE Status='Active' AND ExpiresAt < now
                 LIMIT batchSize               // dùng ix_visit_sweep
  FOR visit IN candidates
     TRY   EndVisit(visit, IdleExpired, now)   // idempotent
     CATCH → log lỗi, bỏ qua visit đó, tiếp tục (Req 14.8)   // không để 1 lỗi chặn cả lượt
  // KHÔNG bao giờ đụng RoomQrToken (Req 14.5)
END
```

> Sweeper tồn tại để **dọn cascade** cho visit mà khách bỏ đi (không có resolve/tương tác nào kích hoạt lazy-expiry). Tính đúng đắn "không nối lại visit quá hạn" đã được lazy-expiry ở §3/§4 đảm bảo độc lập với sweeper.

## 7. Ma trận edge-case (kiểm chứng khi test)

| # | Tình huống | Kỳ vọng |
|---|---|---|
| E1 | Token không tồn tại | `qr_invalid`, không tạo session/visit |
| E2 | Token Revoked | `qr_revoked`, không lộ phòng |
| E3 | Phòng Inactive/Maintenance/Deleted | `room_inactive` |
| E4 | Thiết bị mới quét lần đầu | tạo GuestSession + GuestVisit Active |
| E5 | Cùng thiết bị+phòng, visit Active còn hạn, quét lại | **nối lại** đúng visit, giữ hội thoại/ack, refresh cửa sổ |
| E6 | Visit Active nhưng `now > ExpiresAt` (idle 24h), sweeper chưa chạy, quét lại | **lazy-expire** visit cũ (cascade) + tạo **visit mới**; không thấy dữ liệu lượt cũ |
| E7a | 2 resolve đồng thời, **đã có cùng GuestSession** (cookie sẵn), chưa có visit | chỉ **1** visit Active/(session,room) (ux_visit_active + re-query) |
| E7b | 2 tab quét lần đầu **chưa có cookie** (chưa có session) | **có thể 2 GuestSession** (mỗi cái 1 visit) — chấp nhận (khe hẹp trước khi cookie set, §3 ghi chú race); bất biến "1 visit" chỉ áp **trong cùng 1 session** |
| E8 | Endpoint tương tác sau > 30' không thao tác | `session_expired`, KHÔNG refresh; quét lại mới mở khóa |
| E9 | Endpoint tương tác khi visit đã Expired/Closed | `session_expired` |
| E10 | Lễ tân đóng visit đang mở | visit Closed + conversation Closed + ticket mở → Cancelled; guest không post được |
| E11 | Cùng thiết bị quét phòng A rồi phòng B | 2 visit Active (mỗi phòng 1) — hợp lệ (unique theo (session,room)) |
| E12 | Sweeper và lazy-expiry cùng xử lý một visit | idempotent, không tác dụng kép |

## 8. Truy vết
- Bất biến "đúng 1 visit Active/(session,room)": `04` §7.1 `ux_visit_active`, Property B3.
- Cửa sổ luôn hết hạn được: Property B8.
- Cascade giữ lịch sử: Req 10.8.
- Validates: Requirements 1.3, 1.4, 1.5, 10.1–10.8, 14.4–14.5, 14.8.
