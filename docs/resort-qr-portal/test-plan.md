Cực sâu để tiếp tục cực chính xác nhé# Test Plan — Resort QR Portal

Tài liệu định nghĩa **chiến lược test và kịch bản chuẩn** để nghiệm thu hệ thống. Mục tiêu: mỗi Correctness Property và mỗi acceptance criterion quan trọng đều có ít nhất một test kiểm chứng. Viết trước khi code; dùng làm "definition of done" cho từng task.

> Nguyên tắc: **không test cái chưa build**; test được viết đúng ở subtask có đánh dấu test trong `tasks.md` (3.3, 4.4, 5.3, 7.5, 9.5, 10.4, 12.4, 13.3, 19.2) + bổ sung theo ma trận dưới đây. Test phải kiểm chứng **hành vi thật** (chạy qua DB/API thật ở tầng integration), không mock quá mức tới mức mất ý nghĩa.

## 1. Tầng test & công cụ

| Tầng | Phạm vi | Công cụ đề xuất | Ghi chú |
|---|---|---|---|
| Unit | Logic thuần (service, helper) không cần DB | xUnit + FluentAssertions | Nhanh, nhiều |
| Integration | API + EF Core + PostgreSQL thật | xUnit + `WebApplicationFactory` + **Testcontainers PostgreSQL** | DB thật trong container, không SQLite (vì dùng partial index/xmin) |
| SignalR | Hub realtime | `WebApplicationFactory` + HubConnection thật | Kiểm group/broadcast/quyền join |
| Frontend unit | Logic guest/admin (i18n, scroll-end, countdown) | Vitest | Không cần backend |
| E2E (tuỳ chọn) | Luồng người dùng đầu-cuối | Playwright (mobile viewport) | Chạy trên môi trường staging HTTPS |

**Lưu ý quan trọng:** integration test **phải dùng PostgreSQL thật** (Testcontainers) chứ không phải in-memory/SQLite, vì thiết kế dựa vào **partial unique index** (1 token Active/phòng, 1 conversation Open/visit, 1 ticket mở/phòng, 1 default language, 1 publication IsCurrent) và **concurrency token `xmin`** — các cơ chế này SQLite/InMemory không mô phỏng đúng. Test trên provider khác sẽ cho kết quả sai lệch.

## 2. Ma trận Correctness Property → Test

| Property | Test then chốt (tối thiểu) | Tầng |
|---|---|---|
| P1 Phân giải token an toàn | resolve token hợp lệ → đúng phòng; token không tồn tại/revoked/phòng Inactive → lỗi chuẩn, **không lộ phòng khác**; URL QR không chứa số phòng | Integration |
| P2 QR 1 active/phòng & không auto-expire | tạo token thứ 2 khi đã có Active → vi phạm bị chặn (partial unique); background job chạy KHÔNG đổi trạng thái token; revoke → token cũ Revoked, mới Active | Integration |
| P3 Rule gate ở backend | chưa ack + settings bật → `GET /faq`, `POST /messages`, `POST /housekeeping` trả 403 `rule_ack_required`; settings tắt → cho qua | Integration |
| P4 Draft không ảnh hưởng khách (snapshot) | publish v1 → sửa Draft → `GET /rules` khách **vẫn trả nội dung v1**; publish v2 → khách thấy v2 | Integration |
| P5 Fallback bản dịch | nội dung thiếu `ko` → trả bản `en` + `isFallback=true`, không rỗng | Integration |
| P6 Cô lập lượt lưu trú | visit A gửi tin; đóng visit A; visit B (cùng phòng) **không thấy** tin của A; guest không GET được conversation của visit khác | Integration |
| P7 Ghi chú nội bộ riêng tư | tạo InternalNote; mọi guest endpoint **không bao giờ** trả note (contract test quét response) | Integration |
| P8 Phân quyền | Staff gọi endpoint Admin (rooms CUD/users/settings PUT) → 403; guest gọi `/api/admin/*` → 401/403 | Integration |
| P9 Nối lại visit + cửa sổ + cascade | quét lại trong hạn → cùng visitId, giữ ack; idle > window → `session_expired`; đóng visit → conversation Open đóng, ticket mở Cancelled, guest cũ post → chặn | Integration |
| P10 Housekeeping không trùng & đúng phòng | tạo ticket khi đã có ticket mở → không tạo trùng; complete-by-room/token chỉ đóng ticket đúng phòng; mỗi chuyển trạng thái ghi 1 HousekeepingEvent | Integration |
| P11 Unread đơn điệu | khách gửi n tin → UnreadForStaff=n; lễ tân đọc → 0; không âm | Integration |
| P12 Sanitize nội dung | nhập `<script>`/`onclick` vào rule/FAQ → lưu & trả về đã loại bỏ; render không thực thi | Unit + Integration |
| P13 Acknowledge server-validate | ack gửi version sai/cũ → server vẫn gắn đúng publication IsCurrent; ack version không phải IsCurrent → từ chối/không tính | Integration |
| P14 Một ngôn ngữ mặc định | cố set 2 `IsDefault=true` → bị chặn (partial unique) | Integration |
| P15 Chống ghi đè đồng thời | 2 request PUT cùng section với RowVersion cũ → request sau nhận 409 | Integration |

## 3. Kịch bản test theo chức năng (Given/When/Then)

### 3.1 QR & Resolve (Req 1, 10)
- **TC-QR-01**: Given phòng Active có token T. When `GET /resolve/T`. Then 200 + contract đủ trường (room, resort, languages, defaultLanguage, visit.id, visit.portalWindowExpiresAt, rules.currentVersion, rules.acknowledged, features), tạo cookie GuestSession + GuestVisit Active.
- **TC-QR-02**: Given token đã Revoked. When resolve. Then lỗi `qr_revoked`, không có trường phòng.
- **TC-QR-03**: Given token không tồn tại. When resolve. Then `qr_invalid`.
- **TC-QR-04**: Given phòng Inactive. When resolve token phòng đó. Then `room_inactive`.
- **TC-QR-05**: Given phòng chưa có token. When admin sinh token. Then đúng 1 token Active; URL = `{GuestWebBaseUrl}/r/{token}`, không chứa số phòng.
- **TC-QR-06**: Given phòng đã có token Active. When admin revoke. Then token cũ Revoked (giữ lịch sử), token mới Active; resolve token cũ → `qr_revoked`.

### 3.2 Lượt lưu trú & cửa sổ thao tác (Req 10)
- **TC-VIS-01 (resume)**: Given thiết bị D có GuestVisit Active cho phòng R. When D quét lại R trong 24h. Then **cùng visitId**, ack giữ nguyên.
- **TC-VIS-02 (portal window)**: Given LastSeenAt cách đây > 30'. When guest gọi API tương tác. Then `session_expired` **và LastSeenAt KHÔNG bị cập nhật** (check trước update); sau khi resolve lại → nối lại visit (nếu còn Active) và refresh window.
- **TC-VIS-03 (idle expiry)**: Given visit idle > 24h. When background job chạy. Then visit → Expired; quét lại → visit MỚI.
- **TC-VIS-04 (staff close + cascade)**: Given visit có conversation Open + ticket Requested. When lễ tân đóng visit. Then conversation → Closed, ticket → Cancelled; guest cũ gửi tin → 409/403; quét lại → visit mới, conversation/ack mới.
- **TC-VIS-05 (privacy)**: Given visit A (đã đóng) có tin nhắn. When visit B cùng phòng resolve + GET conversation. Then không thấy tin của A.

### 3.3 Nội quy Draft/Publish/Ack (Req 3, 8)
- **TC-RULE-01 (snapshot)**: publish v1 (2 section) → sửa tiêu đề Draft → `GET /rules` khách trả v1 nguyên vẹn.
- **TC-RULE-02 (publish bumps + re-ack)**: guest đã ack v1 → admin publish v2 → `resolve` trả `acknowledged=false` → gate bật lại.
- **TC-RULE-03 (ack trong cùng visit)**: guest ack v (IsCurrent) → quét lại cùng visit → không bắt đọc lại.
- **TC-RULE-04 (ack server-validate)**: guest POST acknowledge với `version=999` → server gắn đúng publication IsCurrent, không tin client.
- **TC-RULE-05 (gate configurable)**: settings `ruleAckRequiredForFaq=false` → chưa ack vẫn GET faq 200; =true → 403.
- **TC-RULE-06 (fallback)**: section chỉ có `en`; guest `lang=ko` → trả `en` + `isFallback=true`.
- **TC-RULE-07 (ack unique/visit)**: cùng visit ack cùng publication 2 lần → không tạo 2 bản ghi (unique `(GuestVisitId, RulePublicationId)`); `GuestVisitId` luôn non-null.

### 3.4 FAQ (Req 4, 8)
- **TC-FAQ-01**: cây category→item→sub-item trả đúng cấu trúc theo `SortOrder`, chỉ item `IsActive=true`.
- **TC-FAQ-02**: item inactive không xuất hiện ở guest.
- **TC-FAQ-03**: sanitize answer HTML.
- **TC-FAQ-04 (concurrency)**: 2 lễ tân sửa cùng item, người sau nhận 409.

### 3.5 Nhắn tin (Req 5, 9)
- **TC-MSG-01**: guest gửi tin → conversation tạo cho visit, UnreadForStaff=1, broadcast tới nhóm staff.
- **TC-MSG-02**: lễ tân đọc → unread=0; trả lời → broadcast tới conversation, guest nhận.
- **TC-MSG-03 (idempotency)**: gửi 2 lần cùng Idempotency-Key → chỉ 1 message.
- **TC-MSG-04 (rate limit)**: vượt ngưỡng → 429, không tạo message.
- **TC-MSG-05 (độ dài)**: body > MaxMessageLength → `message_too_long`.
- **TC-MSG-06 (fallback polling)**: khi hub ngắt, `GET /conversation` trả đủ tin.
- **TC-MSG-07 (reopen)**: staff đóng hội thoại; guest **cùng visit Active** gửi tin → hội thoại cũ reopen (Status=Open), append, unread++, KHÔNG có hội thoại thứ 2 (unique `Conversation(GuestVisitId)`).

### 3.6 Housekeeping (Req 6)
- **TC-HK-01**: guest tạo ticket → Requested + HousekeepingEvent(Requested).
- **TC-HK-02 (no dup)**: phòng có ticket mở → tạo tiếp không sinh ticket thứ 2.
- **TC-HK-03 (complete-by-room)**: staff xác nhận phòng → Done, ghi CompletedByUserId/At, Method=App, event.
- **TC-HK-04 (complete-by-token)**: staff quét token → đóng đúng ticket của phòng tương ứng token, Method=StaffScan.
- **TC-HK-05 (staff tạo)**: staff tạo ticket cho phòng không có visit → hợp lệ (GuestVisitId null).
- **TC-HK-06 (guest thấy trạng thái)**: guest `GET /housekeeping` thấy Requested→InProgress→Done.

### 3.7 Auth & phân quyền (Req 11)
- **TC-AUTH-01**: login đúng → access+refresh; sai → 401.
- **TC-AUTH-02**: refresh rotation; refresh đã revoke → 401.
- **TC-AUTH-03 (RBAC)**: Staff PUT `/settings` → 403; POST `/rooms` → 403; GET `/rooms` → 200. Admin → 200.
- **TC-AUTH-04**: guest gọi `/api/admin/*` → 401/403.

### 3.8 Settings & i18n (Req 2, 14)
- **TC-SET-01**: PUT settings (Admin) đổi PortalWindowMinutes → resolve phản ánh; Staff PUT → 403.
- **TC-SET-02**: sinh QR dùng `GuestWebBaseUrl` từ settings.
- **TC-I18N-01 (frontend)**: auto-detect thứ tự url→localStorage→navigator→default; unsupported → `en`.
- **TC-I18N-02**: một `IsDefault=true` — cố thêm cái thứ 2 bị chặn.

### 3.9 Bảo mật (Req 11)
- **TC-SEC-01**: log không chứa full token/mật khẩu/refresh.
- **TC-SEC-02**: cookie SameSite + HttpOnly đúng thuộc tính.
- **TC-SEC-03**: CORS chỉ cho phép origin allowlist.
- **TC-SEC-04 (XSS)**: payload script trong rule/FAQ bị sanitize.

### 3.10 Frontend (Req 3, 13)
- **TC-FE-01 (scroll-end)**: nút "Tiếp tục" chỉ bật khi sentinel cuối section vào viewport.
- **TC-FE-02 (countdown)**: `MinReadSeconds` → nút disabled tới khi hết đếm ngược.
- **TC-FE-03 (progress)**: hiển thị đúng "k/n".
- **TC-FE-04 (session_expired)**: nhận `session_expired` → hiện màn "quét lại".

## 4. E2E luồng chính (Playwright, tuỳ chọn, chạy trên HTTPS staging)
- **E2E-GUEST**: resolve → RuleGate (scroll+countdown+tick) → Home → FAQ → gửi tin → tạo ticket dọn phòng → thấy trạng thái.
- **E2E-ADMIN**: login → tạo phòng → tải QR → soạn+publish nội quy → nhận tin ở inbox → trả lời → complete housekeeping bằng chọn phòng và bằng quét QR.
- **E2E-PRIVACY**: khách A tương tác → lễ tân đóng visit → khách B (cùng phòng) không thấy dữ liệu A.

## 5. Định nghĩa "Done" cho test
- Mọi Correctness Property (P1–P15) có ≥1 integration test xanh.
- Các endpoint guest/admin trọng yếu có happy-path + ít nhất 1 negative/permission test.
- Integration test chạy trên PostgreSQL (Testcontainers); CI xanh trước khi merge.
- Không có test phụ thuộc thứ tự chạy; mỗi test tự seed và dọn dữ liệu.

## 6. Ánh xạ test ↔ task (bổ sung so với các subtask test sẵn có)
- Task 5.3 → TC-VIS-01..05
- Task 7.5 → TC-RULE-01..06 (đặc biệt TC-RULE-01 snapshot, TC-RULE-04 server-validate)
- Task 9.5 → TC-MSG-01..06, TC-P7 (note không lộ)
- Task 10.4 → TC-HK-01..06
- Task 3.3 → TC-AUTH-01..04
- Task 4.4 → TC-QR-01..06
- Task 12.4/13.3 → TC-I18N-01, TC-FE-01..04
- Task 19.2 → E2E-GUEST/ADMIN/PRIVACY + TC-SEC-01..04
- **Bổ sung cần thêm test** (chưa có subtask riêng, nên gắn vào task tương ứng): P5 fallback (task 6), P14 default-language & P15 concurrency (task 7/8), settings gating (task 11.3), P4 snapshot (task 7.2).

## 7. Kiểm toán độ phủ (traceability audit)

Rà thủ công toàn bộ tag `_Requirements:` của 19 nhóm task đối chiếu 14 requirement (thực hiện trước khi code).

**Kết luận:** mọi acceptance criterion đều được ít nhất một task triển khai và (với các tiêu chí kiểm thử được) có kịch bản trong §2/§3 — **trừ 2 mục cố ý không có task**:
- `10.7` — thiết kế đường mở map PMS/booking: ghi rõ "không triển khai ở giai đoạn này" (chỉ để schema không khoá đường mở).
- `12.3` — "không yêu cầu backup tự động": là non-requirement, không cần task.

**Các gap đã phát hiện & vá trong lần kiểm toán này** (trước đó chưa được tag/không có task):
- `10.8` (cascade khi kết thúc lượt lưu trú) → bổ sung task 11.2 (đóng thủ công) + 5.2 (khi Expired); test TC-VIS-04.
- `12.5` (HTTPS/secure context) → tag vào task 1; xác minh ở tầng deployment (không unit-test).
- `1.6` (không nhúng số phòng vào URL) → tag task 4.2; test TC-QR-05.
- `7.6` (CUD phòng/QR = Admin-only) → tag task 4.3; test TC-AUTH-03.
- `13.2` (tải nhanh + lazy-load SignalR) → tag task 12.1; xác minh E2E/thủ công (không unit-test).

**Tiêu chí không unit-test được** (xác minh bằng E2E/thủ công/review, không phải bỏ qua): 12.5, 13.1, 13.2, 13.3, 11.7 (ngôn từ UI), 3.10 (xem lại nội quy — có e2e).
