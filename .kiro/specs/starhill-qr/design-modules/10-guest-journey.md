# Module design — Guest Journey (Wave FE.5): resolve → force-read → FAQ → chat → housekeeping

> **Design-first, CHƯA code.** Kiến trúc CHỌN = Candidate B (`architecture_selection-guest-journey.md`): JourneyCore +
> ApiGateway + thin capability views. WHAT: `docs/resort-qr-portal/requirements.md` Req 2/3/4/5/6/10/13/14 (mặt guest).
> Backend guest ĐÃ XONG (8/8 module). Mọi hợp đồng dưới đây ĐỌC TỪ CODE endpoint thật (§0) — KHÔNG suy đoán.

## 0. Đối soát nguồn — hợp đồng guest endpoint đã verify (đọc code, KHÔNG bịa)

Tất cả AllowAnonymous, cookie `__Host-` định danh **THIẾT BỊ** (không phòng) → **mọi call PHẢI gửi `roomId`** (từ `resolvedContext.room.id`). Resolve ngữ cảnh backend qua `ICurrentGuestContextResolver` (cookie + roomId); `Cache-Control: no-store`.

| Method + Path | Request | Response (200) | Touch cửa sổ? | Rule-gate? |
|---|---|---|---|---|
| `POST /v1/guest/resolve` | `{token}` | `GuestResolveResponse{room,resort,languages,defaultLanguage,visit{id,portalWindowExpiresAt},features{faqEnabled,chatEnabled,housekeepingEnabled,ruleAckRequiredForFaq/Chat/Housekeeping}}` (set cookie) | tạo/nối visit | — |
| `GET /v1/guest/rules?roomId=&lang=` | query | `GuestRulesResponse{publicationId,version,language,sections[{key,sortOrder,isRequired,requireScrollEnd,minReadSeconds,title,bodyHtmlSanitized,resolvedLanguage,isFallback,isMissing}]}` | **KHÔNG** (đọc phụ trợ) | không (đọc rules luôn được) |
| `POST /v1/guest/rules/acknowledge` | `{roomId,lang?}` | `AcknowledgeRulesResponse{rulePublicationId,version,alreadyAcknowledged}` | **CÓ** (sau ack) | — |
| `GET /v1/guest/faq?roomId=&lang=` | query | FAQ tree active theo lang (fallback+isFallback) | **CÓ** (duyệt = tương tác) | **CÓ** (403 `rule_ack_required` nếu bật) |
| `POST /v1/guest/messages` | `{roomId,body}` | `{conversationId,messageId,status,reopened}` | **CÓ** | **CÓ** + `ChatEnabled` |
| `GET /v1/guest/conversation?roomId=` | query | hội thoại visit hiện tại (messages) | **KHÔNG** (polling fallback) | — |
| `POST /v1/guest/housekeeping` | `{roomId}` | `{ticketId,status,alreadyOpen}` | **CÓ** | **CÓ** + flag |
| `GET /v1/guest/housekeeping?roomId=` | query | trạng thái ticket phòng | **KHÔNG** | — |
| SignalR `/hubs/chat` | negotiate (guest AllowAnonymous, join hội thoại mình) | push message realtime | — | — |

**Mã lỗi ProblemDetails `code` (từ GuestAccess/Rules — QR-AD-025/032, verify snapshot):** `session_expired` (quá cửa sổ 30' → **rescan**), `guest_context_missing` (cookie lạ/thiếu → rescan), `configuration_unavailable` (settings chưa seed), `rule_ack_required` (403 — chưa ack mà bật gate). ProblemDetailsBuilder emit `code` **nguyên văn** (verify QR-N-076).

**Trạng thái guest-web hiện tại (đã đọc):** `GuestResolveView` (thật, POST resolve + scrub token khỏi URL — QR-N-079), `HomeView` showcase, `guestSession` store (context ở sessionStorage), **`rules.ts` = boolean client-only** (DRIFT: KHÔNG phản ánh ack server — sẽ thay bằng JourneyCore.ruleAck từ server), router `/rules//faq//chat//housekeeping` đều trỏ `HomeView` (placeholder). i18n en/vi/ko/zh.

## 1. Mục tiêu & bất biến (kế thừa architecture_selection INV1-8 + DI-1..5)

1. **INV1** capability mở khoá = `featureEnabled ∧ (¬ackRequired ∨ ackedCurrentVersion)`.
2. **INV2** server-authoritative: client-gate CHỈ advisory UX; server 403 `rule_ack_required` là chốt thật. (Khắc phục drift `rules.ts`.)
3. **INV3** hết cửa sổ 30' → mọi hành vi nghiệp vụ nhận `session_expired`/`guest_context_missing` → **rescan** (không silent-success).
4. **INV4** chỉ dữ liệu visit hiện tại; đổi `visit.id` → clear persistence + reset Core.
5. **INV5** ngôn ngữ áp cả UI (i18n JSON) + nội dung server (isFallback không để trống).
6. **INV6** force-read "Tiếp tục" bật khi MỌI section bắt buộc thoả: scroll-end (nếu RequireScrollEnd) + hết MinReadSeconds + checkbox xác nhận.
7. **INV7** không token/secret ở URL/log (đã có QR-N-079).
8. **INV8** rules/faq `bodyHtmlSanitized` render có kiểm soát (server đã sanitize — Req 8.6); chat plain-text (`textContent`).
9. **DI-1** CHỈ `ApiGateway` gọi `fetch`. **DI-2** JourneyCore chỉ giữ session/ack/flags/window (KHÔNG data capability). **DI-3** capability-availability là derived getter (không bang trùng). **DI-4** ruleAck từ server. **DI-5** visit đổi → reset.

## 2. Kiến trúc (Candidate B ánh xạ sang Vue 3 + Pinia)

```text
guest-web/src/
  core/
    journeyCore.ts        (Pinia store) — resolvedContext, features, ruleAck{ackedVersion,currentVersion,isAcked},
                          portalWindow{expiresAt}, selectedLanguage; derived: canFaq/canChat/canHousekeeping/
                          mustReadRules/mustRescan/roomId. KHÔNG giữ faqTree/messages/ticket (DI-2).
    apiGateway.ts         điểm fetch DUY NHẤT (DI-1): tiêm roomId+lang, credentials:'include', map ProblemDetails→
                          GuestApiError(code); intercept: code∈{session_expired,guest_context_missing}→core.markRescanNeeded()
                          + điều hướng /r rescan; 403 rule_ack_required→core.markAckRequired()+điều hướng /rules.
    sessionPersistence.ts boundary sessionStorage (context/lang/ackCache theo visit.id); clear khi visit đổi (DI-5).
    realtimeChannel.ts    SignalR /hubs/chat + polling fallback (GET /conversation); đẩy message cho ChatView; KHÔNG biết gate.
  views/
    GuestResolveView.vue  (đã có) resolve→set core+persistence→điều hướng theo mustReadRules.
    RulesView.vue         force-read state machine (§3) + acknowledge.
    FaqView.vue           cây FAQ (§ lấy /guest/faq), CTA "gửi tin về câu hỏi này"→ChatView prefill.
    ChatView.vue          hội thoại (realtime+polling), gửi tin plain-text.
    HousekeepingView.vue  tạo yêu cầu + xem trạng thái (poll GET).
    HomeView.vue          trang phòng: thẻ vào từng capability (mờ/khoá theo core derived).
  router/index.ts         guard beforeEach đọc core: mustRescan→/r; mustReadRules & vào faq/chat/housekeeping→/rules.
```

**Ánh xạ component ↔ file:** JourneyCore→`core/journeyCore.ts`; ApiGateway→`core/apiGateway.ts` (mở rộng từ `api/guestApi.ts` hiện tại); SessionPersistence→`core/sessionPersistence.ts` (tách từ `stores/guestSession.ts`); RealtimeChannel→`core/realtimeChannel.ts`; Views→`views/*`; Router guards→`router/index.ts`. **Xoá `stores/rules.ts`** (boolean drift) — thay bằng `core.ruleAck` server-authoritative (DI-4).

## 3. Force-read state machine (Req 3 — TRỌNG TÂM, INV6)

Trạng thái mỗi section (từ `GuestRulesResponse.sections`, sắp theo `sortOrder`):
- `viewed`: true khi (¬RequireScrollEnd) HOẶC IntersectionObserver báo sentinel cuối section vào viewport.
- `readTimerDone`: true khi (MinReadSeconds==0) HOẶC đếm ngược từ lúc section hiển thị đạt MinReadSeconds.
- Section "satisfied" = `¬IsRequired ∨ (viewed ∧ readTimerDone)`.

Máy trạng thái flow:
```
Loading → Reading(sectionIndex) → AllSatisfied → (checkbox tick) → Acknowledging → Done
                     ↑ Next chỉ bật khi section hiện tại satisfied (INV6)
```
- Thanh tiến độ `i/N` (Req 3.5). Nút "Tiếp tục" của section i disabled tới khi section i satisfied (scroll-end + countdown).
- `AllSatisfied` → hiện checkbox "Tôi đã đọc và đồng ý" (Req 3.6); tick → enable "Xác nhận".
- "Xác nhận" → `POST /guest/rules/acknowledge{roomId,lang}` → 200 → `core.setAcked(version)` + persistence cache theo visit.id → điều hướng về Home (hoặc capability đích đã lưu). `alreadyAcknowledged=true` cũng coi như done (idempotent).
- **Xem lại bất cứ lúc nào** (Req 3.10): RulesView truy cập được cả khi đã ack (không bắt lại), chỉ ẩn checkbox/nút nếu `isAcked ∧ version khớp`.
- **INV2**: nếu client nghĩ đã ack nhưng server publish version mới → lần gọi faq/chat/housekeeping trả 403 `rule_ack_required` → ApiGateway lái về /rules (đồng bộ lại `currentVersion` từ response). Client-gate chỉ là UX.

## 4. Capability-availability (derived getters trên JourneyCore — DI-3, INV1)

```
roomId            = resolvedContext?.room.id
mustRescan        = !resolvedContext || rescanNeeded (set bởi ApiGateway khi session_expired/guest_context_missing)
ackRequiredFor(x) = features.ruleAckRequiredForX
isAckedCurrent    = ruleAck.isAcked && ruleAck.ackedVersion === ruleAck.currentVersion
mustReadRules     = (ackRequiredForFaq||Chat||Housekeeping) && !isAckedCurrent   // gate tối thiểu để vào các mục cần ack
canFaq            = features.faqEnabled && (!ackRequiredForFaq || isAckedCurrent)
canChat           = features.chatEnabled && (!ackRequiredForChat || isAckedCurrent)
canHousekeeping   = features.housekeepingEnabled && (!ackRequiredForHousekeeping || isAckedCurrent)
```
- Nguồn `features` + `visit.portalWindowExpiresAt` = từ resolve. `ruleAck.currentVersion` = từ `GET /guest/rules` (version) hoặc từ 403 refresh. `ruleAck.isAcked/ackedVersion` = từ `POST acknowledge` (hoặc cache persistence theo visit.id; nhưng server vẫn là chốt — INV2).

## 5. ApiGateway — hợp đồng + error→event (DI-1, INV2/INV3)

- Một wrapper `guestFetch<T>(method, path, {roomId?, lang?, body?})`: `credentials:'include'`, `Accept: application/json`; tự thêm `roomId`/`lang` (query cho GET, body cho POST) từ tham số (view truyền, lấy từ `core.roomId`/`core.selectedLanguage`).
- Map lỗi ProblemDetails → `GuestApiError{status,code,detail}` (đã có ở `guestApi.ts`).
- **Intercept tập trung (một chỗ — thay vì rải khắp view):**
  | `code` | Hành động Gateway | Component |
  |---|---|---|
  | `session_expired` / `guest_context_missing` | `core.markRescanNeeded()` → router.push(`/r/{lastToken?}` hoặc màn "Quét lại QR") | INV3 |
  | `rule_ack_required` (403) | `core.markAckRequired()` (+cập nhật currentVersion nếu response mang) → router.push(`/rules`) | INV2 |
  | `configuration_unavailable` | toast lỗi cấu hình (hiếm — settings chưa seed) | — |
  | khác | ném cho view xử lý cục bộ | — |
- **API methods** (thin, khớp §0): `resolve(token)`, `getRules(roomId,lang)`, `acknowledgeRules(roomId,lang)`, `getFaq(roomId,lang)`, `sendMessage(roomId,body)`, `getConversation(roomId)`, `requestHousekeeping(roomId)`, `getHousekeepingStatus(roomId)`. MOCK DEV-only cho mỗi cái (mirror `guestApi.ts` hiện có — máy không backend xem được).

## 6. Realtime + polling fallback (Req 5.5/5.6, INV — chat)
- `realtimeChannel`: `@microsoft/signalr` HubConnection `/hubs/chat`; guest join hội thoại của mình (server per-method auth, K-Con.4). Nhận `message`→ đẩy vào ChatView state.
- **Polling LUÔN là nguồn sự thật/fallback** (khớp comment backend): ChatView poll `GET /guest/conversation` mỗi ~5s khi mở + khi realtime disconnect. Realtime chỉ giảm độ trễ. Lazy-load `@microsoft/signalr` chỉ khi vào ChatView (Req 13.2 bundle).
- GET conversation KHÔNG touch cửa sổ → polling không tự giữ session "sống" (đúng: chỉ hành vi gửi tin mới touch). Nếu gửi tin lúc hết hạn → `session_expired` → rescan (INV3).

## 7. Responsive + i18n (kế thừa 08-frontend §3)
- Guest mobile-first tuyệt đối: `100dvh` app-shell, safe-area, `interactive-widget=resizes-content` cho ChatView (bàn phím), touch ≥44px, auto-fit, no-horizontal-overflow. RulesView: vùng cuộn + `IntersectionObserver` sentinel cuối mỗi section (RequireScrollEnd).
- i18n: `selectedLanguage` (ưu tiên `?lang`→localStorage→navigator→resort default `en` — Req 2.1); đổi ngôn ngữ → refetch nội dung server (rules/faq) theo `lang` + lưu localStorage; render `isFallback` có nhãn (INV5). UI text ở i18n JSON en/vi/ko/zh.

## 8. Build slices (từng bước chắc chắn, mỗi slice: design-đã-có → code → Playwright gate → journal)
1. **FE.5a — Core + Rules force-read (THAY MOCKUP)**: dựng `core/{journeyCore,apiGateway,sessionPersistence}`, **xoá `stores/rules.ts`** (drift), refactor GuestResolveView→set core, RulesView state-machine (§3) nối `GET /guest/rules`+`POST acknowledge` THẬT, router guards đọc core. Playwright: force-read (Next disabled tới scroll-end+countdown), checkbox→ack→home; mock session_expired→rescan; mock 403→/rules.
   - **QUAN TRỌNG (QR-N-082/QR-DV-008)**: `HomeView.vue` hiện tại là **MOCKUP tĩnh ~900 dòng** (nội quy/FAQ/chat/housekeeping hardcode i18n, chat/timeline giả, 40+ ngôn ngữ, theme switcher) — KHÔNG nối 8 guest endpoint. FE.5a phải **thay HomeView mockup** bằng shell thật + RulesView thật; giữ lại phần thị giác dùng được (bố cục thẻ, ngôn ngữ 4-locale thật) nhưng bỏ dữ liệu giả + `ruleConfirmed` boolean. Đây là rewrite lớn + xoá demo → CẦN user xác nhận phạm vi trước.
2. **FE.5b — FAQ**: FaqView cây (GET /guest/faq), điều hướng cha-con, CTA→prefill ChatView. Playwright: render tree, fallback nhãn, gated khi ackRequiredForFaq.
   - **Hợp đồng FAQ đã verify (FaqContracts.cs)**: `GET /v1/guest/faq?roomId=&lang=` → `GetGuestFaqTreeResult{language, categories: RenderedFaqCategory[]}`. `RenderedFaqCategory{id,key,sortOrder,name?,resolvedLanguage,isFallback,isMissing, items: RenderedFaqItem[]}`. `RenderedFaqItem{id,sortOrder,question?,answerHtmlSanitized?,resolvedLanguage,isFallback,isMissing, children: RenderedFaqItem[] (đệ quy)}`. Rule-gate + `FaqEnabled` enforce TRONG `GetGuestFaqTreeUseCase` → 403 `rule_ack_required` (ApiGateway→/rules) / `configuration_unavailable` / `faq_disabled`. `answerHtmlSanitized` đã sanitize server (INV8 → render v-html có kiểm soát). Touch cửa sổ (duyệt = tương tác). CTA "gửi tin về vấn đề này" → router.push chat với prefill (hoàn tất ở FE.5c).
3. **FE.5c — Chat POLLING (nguồn sự thật)**: ChatView `POST /guest/messages` + `GET /guest/conversation` poll (~4s + refresh-sau-gửi), plain-text render (INV8 — interpolation `{{}}`, KHÔNG v-html), tiêu thụ `query.prefill` (FAQ CTA Req 4.6), gating capability='chat'. Playwright: poll hiện tin + gửi append + prefill + gating + no-overflow.
   - **Hợp đồng verify (ConciergeContracts.cs)**: `POST /v1/guest/messages{roomId,body}` → `{conversationId,messageId,status,reopened}`; `GET /v1/guest/conversation?roomId=` → `GetGuestConversationResult{conversation: GuestConversationView|null}`; `GuestConversationView{conversationId,status('Open'|'Closed'),lastMessageAt,messages: GuestMessageView[]}`; `GuestMessageView{messageId,senderType('Guest'|'Staff'|'System'),body,createdAt,readByStaffAt?}`. Body PLAIN TEXT (server trim+MaxMessageLength, KHÔNG sanitize → FE render textContent). GET không touch; POST touch. Rule-gate+ChatEnabled enforce use case (403→/rules).
   - **QR-TO (SignalR tách FE.5c-ii)**: realtime `/hubs/chat` (methods server→client `MessageReceived`/`ConversationUpdated`/`MessageRead`, **payload chỉ Id → client poll ngay**) là ACCELERATOR; polling đã là nguồn sự thật. Tách vì: (a) thêm dep `@microsoft/signalr`; (b) e2e không có hub → WebSocket-fail dễ phá gate no-console-error; (c) chỉ verify đúng khi hub chạy (Docker/CI). Bản chất: giao nguồn-sự-thật trước, realtime bồi sau — KHÔNG phải fix ngọn.
4. **FE.5c-ii — SignalR realtime (accelerator, deferred verify Docker/CI)**: lazy `@microsoft/signalr` connect `/hubs/chat?roomId=` (cookie same-origin) → `JoinConversation` → on `MessageReceived/ConversationUpdated/MessageRead` → **poll ngay** (không tin payload). LogLevel.None + catch → fail im lặng, polling fallback. Verify negotiate 200 + push→poll trên hub thật.
5. **FE.5d — Housekeeping**: HousekeepingView tạo yêu cầu + trạng thái (poll), alreadyOpen. Playwright: request→status, gated.
Mỗi slice dừng nếu: build FE lỗi/warning; Playwright fail (overflow/console/gate sai); drift design↔UI.

## 9. Quyết định/trade-off (ghi journal khi code)
- **QR-AD (kiến trúc B)**: JourneyCore+ApiGateway (xem `architecture_selection-guest-journey.md`). Lý do metric: cross-cutting invariants ~25% vs ~62% (route-scattered); 0 sync-cycle. Trade-off: Core fan-in hub → DI-2 chặn god-object.
- **QR-AD (sửa drift `rules.ts`)**: thay boolean client-only bằng `core.ruleAck` server-authoritative; client-gate advisory, server 403 chốt thật (INV2). Đây là "fix tận gốc" cho drift đang tồn tại.
- **QR-TO**: realtime SignalR vs chỉ-polling — chọn cả hai (polling nguồn thật + SignalR tăng tốc, lazy) vì Req 5.5/5.6 + bundle mobile.
- **QR-TO**: roomId tiêm ở ApiGateway (mọi call cần) vs từng view tự gắn — chọn Gateway (một chỗ, tránh sót roomId → guest_context_missing).

## 10. Self-validation trước code
- [x] Hợp đồng 8 guest endpoint + mã lỗi đọc TỪ CODE thật (§0) — không bịa; mọi call cần roomId đã xác nhận.
- [x] Kiến trúc B đã chọn qua metric (architecture_selection); component ánh xạ file cụ thể.
- [x] Force-read state machine phủ Req 3.3/3.4/3.5/3.6/3.10 + INV6; ack server-authoritative (INV2) sửa drift rules.ts.
- [x] session_expired/guest_context_missing→rescan + 403→/rules tập trung ở ApiGateway (INV3/INV2, DI-1).
- [x] Realtime+polling fallback (polling nguồn thật); lazy signalr (Req 13.2).
- [x] Responsive/i18n kế thừa 08-frontend §3; render sanitized HTML (INV8) + chat plain-text.
- [ ] User review design này (đặc biệt §3 state-machine + §5 intercept + quyết định xoá rules.ts) trước FE.5a.
