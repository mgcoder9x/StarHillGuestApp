# Deep Solution Design - Resort QR Guest Portal

> ⚠️ **Lưu ý đọc**: Mục 1–20 là **phân tích/khảo sát phương án** (có bàn cả các lựa chọn không chọn như SQL Server, .NET 8, role Manager...). **Nguồn chân lý cuối cùng là Decision Log ở mục 21–22** và bộ `requirements.md` / `design.md` / `tasks.md`. Khi có mâu thuẫn, ưu tiên Decision Log + 3 file spec.

## 1. Kết luận nhanh

Thiết kế hiện tại trong `requirements.md`, `design.md`, `tasks.md` đã đi đúng hướng cho bài toán: khách quét QR trong phòng, web tự biết phòng, tự chọn ngôn ngữ, bắt đọc nội quy, xem FAQ, gửi phản hồi; admin có dashboard tạo phòng, sinh QR, quản lý nội dung và xử lý tin nhắn.

Điểm cần nâng cấp trước khi code là biến tài liệu từ "đặc tả kỹ thuật" thành "thiết kế vận hành thật": QR ngoài đời bị dán cố định, khách cũ có thể giữ link, nội quy cần publish/version rõ, FAQ cần đo hiệu quả, tin nhắn cần trạng thái xử lý, dashboard cần phù hợp nghiệp vụ lễ tân, và hệ thống cần audit/rate limit/backup ngay từ MVP.

Khuyến nghị chính:

- Dùng **ASP.NET Core modular monolith** cho backend, không tách microservice ở giai đoạn đầu.
- Dùng **2 Vue SPA riêng**: `guest-web` cực nhẹ cho khách, `admin-web` đầy đủ dashboard cho nội bộ.
- Dùng **PostgreSQL** làm database mặc định, trừ khi bạn đã có hạ tầng SQL Server.
- Dùng **SignalR** cho realtime, có polling fallback cho guest.
- Dùng **QR token ngẫu nhiên + bảng lịch sử QR token**, không dùng số phòng trong URL.
- Thêm khái niệm **GuestVisit/GuestSession** để giảm rủi ro khách cũ vẫn nhắn tin theo phòng.
- Tách content thành **Draft -> Published**, mọi chỉnh sửa nội quy phải publish thành version mới.
- Dashboard không chỉ "xem tin nhắn", mà phải có inbox, trạng thái xử lý, ghi chú nội bộ, assignment, canned reply, audit.

Ghi chú stack ngày 2026-07-01: nếu bắt đầu dự án mới, nên cân nhắc **.NET 10 LTS** thay vì .NET 8, vì .NET 10 đang là LTS active tới 2028-11-14, còn .NET 8 ở maintenance và hết hỗ trợ 2026-11-10 theo trang support chính thức của Microsoft:

- https://dotnet.microsoft.com/en-us/platform/support/policy
- https://dotnet.microsoft.com/en-us/download/dotnet

Nếu bạn vẫn muốn dùng .NET 8 vì môi trường triển khai đã chuẩn hóa thì vẫn được, nhưng phải có kế hoạch nâng lên .NET 10 trước tháng 11/2026.

---

## 2. Đánh giá bộ docs hiện tại

### 2.1 Điểm mạnh

Tài liệu hiện tại có những nền tảng tốt:

- Requirement viết theo acceptance criteria, có thể trace sang task.
- Đã nhận ra đúng 4 nghiệp vụ lõi: QR -> phòng, đa ngôn ngữ, force-read nội quy, realtime messaging.
- Đã tách Guest Web và Admin Dashboard thành 2 SPA riêng, đây là quyết định đúng.
- Data model có các bảng cốt lõi: Room, RuleSection, FAQ, GuestSession, Conversation, Message, Note.
- Có correctness properties, giúp sau này viết test theo bất biến hệ thống.
- Có task graph theo wave, phù hợp để triển khai tuần tự hoặc chia nhiều agent/dev.

### 2.2 Lỗ hổng cần bổ sung

Các phần còn mỏng nếu đem đi code ngay:

| Nhóm | Vấn đề | Rủi ro |
|---|---|---|
| QR thực địa | QR dán cố định theo phòng, link có thể bị chụp/lưu/chia sẻ | Khách cũ vẫn có thể gửi tin như phòng đó |
| Phiên lưu trú | Chưa tách rõ room, guest session, visit/stay | Khó reset trạng thái khi phòng đổi khách |
| Nội quy | Chưa có workflow draft/publish/preview | Admin sửa nháp có thể vô tình bắt khách đọc lại |
| FAQ | Chưa có tracking lượt xem/từ khóa thất bại | Không biết câu hỏi nào hữu ích hoặc thiếu |
| Tin nhắn | Chưa có assignment, priority, SLA, category, canned reply | Lễ tân xử lý khó khi nhiều phòng nhắn cùng lúc |
| Dashboard | Chưa có IA/wireframe nghiệp vụ | Dễ làm UI có đủ CRUD nhưng khó dùng |
| Bảo mật | Chưa nêu CSRF, audit log, token history, upload isolation | Guest public surface dễ bị spam/XSS |
| Vận hành | Chưa có backup, log, health check, seed data, deploy topology | Khó đưa vào resort thật |
| Pháp lý | Force-read chỉ chứng minh đã tick/scroll, không chứng minh thật sự đọc | Không nên quảng cáo quá mức giá trị pháp lý |

### 2.3 Điều quan trọng nhất cần chốt

Câu hỏi kiến trúc nghiệp vụ:

**QR nhận diện phòng hay nhận diện phiên khách đang ở phòng?**

Vì yêu cầu của bạn là "quét sẽ biết số phòng", khả năng cao QR là mã cố định dán trong phòng. Cách này đơn giản và đúng trải nghiệm, nhưng không biết ai là khách hiện tại. Vì vậy hệ thống nên nói rõ:

- QR xác định **Room**.
- Cookie xác định **GuestSession** trên thiết bị.
- Một lớp tùy chọn **GuestVisit** xác định phiên khách dùng portal trong một khoảng thời gian.
- Nếu chưa tích hợp PMS/booking, GuestVisit có thể tự hết hạn sau N ngày hoặc được staff đóng thủ công khi checkout.
- Nếu sau này tích hợp PMS, GuestVisit sẽ map với booking/check-in/check-out thật.

---

## 3. Phạm vi sản phẩm đề xuất

### 3.1 MVP nên làm

MVP nên đủ dùng trong resort thật, không chỉ demo:

- Guest quét QR qua camera điện thoại, mở web `/r/{token}`.
- Backend resolve token ra phòng, resort, ngôn ngữ bật, trạng thái acknowledge.
- Guest web auto-detect ngôn ngữ theo `?lang`, localStorage, `navigator.language`, default.
- Force-read nội quy theo section, có scroll-end, min seconds, checkbox đồng ý.
- Guest xem FAQ dạng category + flow câu hỏi cha-con.
- Guest gửi tin nhắn text; ảnh đính kèm có thể để phase 2 nếu muốn giảm rủi ro.
- Staff/Admin dashboard:
  - đăng nhập,
  - xem tổng quan,
  - tạo/sửa phòng,
  - sinh QR PNG/PDF,
  - quản lý nội quy draft/publish,
  - quản lý FAQ,
  - inbox hội thoại theo phòng,
  - trả lời tin,
  - ghi chú nội bộ,
  - đóng hội thoại.
- Bảo mật MVP:
  - HTTPS,
  - JWT/refresh token hoặc cookie auth cho admin,
  - guest session HttpOnly cookie,
  - sanitize HTML,
  - rate limit,
  - audit log cho admin actions,
  - backup DB.

### 3.2 Nên để phase 2

Các tính năng nên thiết kế đường mở, nhưng không nhồi vào MVP:

- Upload ảnh từ guest.
- Tích hợp PMS/booking/check-in/check-out.
- Push notification cho staff.
- Chatbot/AI gợi ý FAQ.
- QR theo từng booking thay vì QR cố định.
- Survey/NPS sau khi checkout.
- Báo cáo SLA, workload staff.
- Export dữ liệu acknowledgement/tin nhắn.
- Multi-resort SaaS đầy đủ billing/tenant isolation.

### 3.3 Không nên làm ở MVP

- Native mobile app.
- Microservice.
- Realtime quá phức tạp kiểu read receipt từng staff.
- CMS tổng quát quá rộng.
- Workflow approval nhiều cấp cho nội dung, trừ khi resort yêu cầu nghiêm ngặt.

---

## 4. Thiết kế nghiệp vụ tổng thể

### 4.1 Actor

| Actor | Mục tiêu | Quyền |
|---|---|---|
| Guest | Đọc nội quy, tra FAQ, gửi yêu cầu | Không đăng nhập, chỉ trong room portal |
| Staff | Xử lý tin nhắn, ghi chú, đóng hội thoại | Inbox, reply, notes, stats cơ bản |
| Admin | Cấu hình hệ thống | Rooms, QR, rules, FAQ, users, languages, settings |
| Manager | Theo dõi vận hành | Dashboard, báo cáo, audit, có thể cùng role Admin |

### 4.2 Object nghiệp vụ

| Object | Ý nghĩa |
|---|---|
| Resort | Khách sạn/resort sở hữu cấu hình, phòng, nội dung |
| Room | Phòng vật lý, ví dụ A-203 |
| RoomQrToken | Token trong QR, có lịch sử, có thể revoke |
| GuestSession | Phiên trình duyệt/thiết bị của khách, lưu bằng cookie |
| GuestVisit | Phiên sử dụng portal cho một phòng, có thể hết hạn/đóng |
| RuleSet | Bộ nội quy đang draft/published |
| RulePublication | Một lần publish nội quy, có version |
| RuleAcknowledgement | Khách/session đã xác nhận version nào |
| FaqCategory/FaqItem | Cây FAQ/flow |
| Conversation | Hội thoại giữa guest session/visit và staff |
| Message | Tin nhắn trong hội thoại |
| InternalNote | Ghi chú nội bộ không hiển thị cho guest |
| AuditLog | Nhật ký hành động admin/staff |

### 4.3 Trạng thái phòng

Room nên có:

- `Active`: QR dùng được.
- `Inactive`: không cho resolve QR.
- `Maintenance`: có thể resolve nhưng hiển thị thông báo đặc biệt hoặc chặn chat tùy cấu hình.
- `Deleted`: soft delete, không nên xóa cứng nếu đã có message/ack.

### 4.4 Trạng thái QR token

Không nên chỉ lưu `Room.PublicToken`. Nên có bảng riêng:

```text
RoomQrToken
  Id
  RoomId
  TokenHash
  TokenPreview
  Status: Active | Revoked | Expired
  Version
  CreatedAt
  CreatedByUserId
  RevokedAt
  RevokedByUserId
  RevocationReason
```

Lưu ý:

- Có thể lưu token plaintext trong DB nếu cần resolve nhanh, nhưng tốt hơn là lưu hash và resolve bằng hash của token gửi lên.
- `TokenPreview` chỉ để admin nhận diện, ví dụ 6 ký tự cuối.
- Mỗi phòng chỉ có 1 token active.
- Revoke token cũ không xóa lịch sử, để audit được QR đã từng in.

### 4.5 Trạng thái GuestVisit

Nếu chưa tích hợp PMS, nên có GuestVisit nhẹ:

```text
GuestVisit
  Id
  RoomId
  GuestSessionId
  Status: Active | Closed | Expired | Blocked
  StartedAt
  LastSeenAt
  ExpiresAt
  ClosedAt
  ClosedByUserId
  CloseReason
```

Quy tắc:

- Lần đầu một thiết bị quét QR phòng, tạo GuestSession và GuestVisit.
- Nếu cùng thiết bị quay lại cùng phòng trước `ExpiresAt`, dùng lại GuestVisit.
- Nếu quá hạn, tạo GuestVisit mới hoặc yêu cầu xác nhận lại "Bạn đang ở phòng X?".
- Staff có nút đóng visit/hội thoại khi checkout.
- Nếu có PMS phase 2, GuestVisit map sang booking stay.

### 4.6 Trạng thái hội thoại

Conversation nên có:

- `Open`: đang cần xử lý hoặc đang trao đổi.
- `PendingGuest`: staff đã trả lời, chờ guest.
- `PendingStaff`: guest vừa nhắn, cần staff.
- `Closed`: đã xong.
- `Archived`: cũ, chỉ đọc.

Các field nên có:

```text
Conversation
  Id
  ResortId
  RoomId
  GuestSessionId
  GuestVisitId nullable
  Status
  Priority: Low | Normal | High | Urgent
  Category: Housekeeping | Reception | Maintenance | F&B | Other
  AssignedToUserId nullable
  LastMessageAt
  LastGuestMessageAt
  LastStaffMessageAt
  UnreadForStaff
  UnreadForGuest
  ClosedAt
  ClosedByUserId
```

MVP có thể chỉ hiển thị status + unread; nhưng schema nên chừa chỗ cho priority/category/assignment.

---

## 5. Guest experience design

### 5.1 Flow chính

```text
Quét QR
  -> /r/{token}
  -> Resolve token
  -> Chọn ngôn ngữ
  -> Nếu token lỗi: trang lỗi thân thiện
  -> Nếu chưa xác nhận nội quy version hiện tại: Rule Gate
  -> Home phòng
  -> FAQ hoặc Chat hoặc xem lại nội quy
```

### 5.2 Màn hình Guest Web

Guest web nên có ít màn hình, tốc độ cao:

| Screen | Mục tiêu | Nội dung |
|---|---|---|
| ResolveLoading | Chờ resolve QR | Logo nhỏ, loading, không text dài |
| TokenError | QR lỗi/thu hồi | Thông báo, hotline/lễ tân, không lộ room |
| RuleGate | Bắt đọc nội quy | Section, progress, countdown, checkbox |
| GuestHome | Hub sau khi ack | Phòng, quick actions, FAQ, chat |
| RulesViewer | Xem lại nội quy | Nội dung đã publish |
| FaqFlow | FAQ dạng flow | Category, search, câu hỏi con |
| Chat | Nhắn tin | Thread, input, trạng thái gửi |
| LanguageSheet | Đổi ngôn ngữ | vi/en/ko/zh... |

### 5.3 Home sau khi đọc nội quy

Không nên làm landing page marketing. Home là một tool nhỏ cho khách:

```text
Star Hill Resort
Room A-203                 [VI]

[ Nội quy ]  [ FAQ ]  [ Liên hệ lễ tân ]

Yêu cầu thường dùng:
- Wifi
- Giờ ăn sáng
- Dọn phòng
- Xe điện / shuttle

Tin nhắn gần nhất:
"Lễ tân đã phản hồi lúc 14:20"
```

### 5.4 Force-read nội quy

Mục tiêu của force-read:

- Giảm việc khách bỏ qua thông tin quan trọng.
- Có log rằng khách/session đã xác nhận.
- Không gây ức chế quá mức.

Thiết kế nên:

- Chia thành section ngắn, mỗi section 1 chủ đề.
- Có progress rõ: `2/6`.
- `MinReadSeconds` vừa phải, ví dụ 3-8 giây/section, không đặt quá dài.
- Nếu section rất dài, dùng `RequireScrollEnd`.
- Nút "Tiếp tục" chỉ bật khi đủ điều kiện.
- Section cuối có checkbox đồng ý.
- Sau khi ack, home mở khóa FAQ/chat.
- Nếu đổi ngôn ngữ giữa flow, giữ tiến độ theo section id, không reset vô lý.

Backend vẫn phải enforce rule gate, không chỉ chặn ở frontend:

- `GET /faq` nếu chưa ack có thể trả `403 rule_ack_required`.
- `POST /messages` nếu chưa ack trả `403 rule_ack_required`.
- `POST /rules/acknowledge` phải kiểm tra version hiện tại và session hợp lệ.

### 5.5 FAQ flow

FAQ nên hỗ trợ 3 cách dùng:

- Browse theo category.
- Search nhanh.
- Flow câu hỏi cha-con.

Ví dụ:

```text
Wifi
  -> Mật khẩu wifi là gì?
  -> Wifi yếu thì làm sao?
Ăn uống
  -> Giờ ăn sáng
  -> Gọi room service
Di chuyển
  -> Gọi xe điện
  -> Thuê xe máy
```

Mỗi FAQ item nên có:

- câu hỏi,
- câu trả lời rich text đã sanitize,
- câu hỏi con,
- CTA tùy chọn: "Gửi tin nhắn về vấn đề này".

Khi guest bấm CTA từ FAQ, chat nên prefill context:

```text
Tôi cần hỗ trợ về: Wifi yếu thì làm sao?
```

### 5.6 Chat guest

Chat cần đơn giản:

- Chỉ 1 hội thoại active cho mỗi GuestVisit/Room tại một thời điểm trong MVP.
- Nếu hội thoại đã closed mà guest nhắn tiếp, tự reopen hoặc tạo hội thoại mới tùy rule.
- Tin nhắn có trạng thái:
  - sending,
  - sent,
  - failed,
  - read by staff.
- Nếu SignalR mất kết nối, vẫn cho gửi REST và polling nhận tin.
- Nên có một số quick request:
  - Dọn phòng,
  - Cần thêm khăn,
  - Gọi lễ tân,
  - Báo lỗi thiết bị,
  - Khác.

---

## 6. Admin Dashboard design

### 6.1 Information architecture

Dashboard nên chia theo công việc thật:

```text
Dashboard
Inbox
Rooms & QR
Rules
FAQ
Notes
Reports
Users
Settings
Audit
```

Với Staff:

```text
Dashboard
Inbox
Rooms (read-only cơ bản)
Notes
```

Với Admin:

```text
Toàn bộ menu
```

### 6.2 Dashboard tổng quan

Màn tổng quan không cần hero. Nên là operational dashboard:

```text
Today
[Unread conversations: 8] [Open: 14] [Rule acks: 42] [Active rooms: 86]

Needs attention
Room A-203  Pending  3m ago  "Wifi is not working"
Room B-110  Urgent   7m ago  "Need medical help"

FAQ insights
Top viewed: Wifi password, Breakfast time, Shuttle
No-result searches: "laundry", "late checkout"
```

### 6.3 Inbox

Inbox là màn quan trọng nhất cho lễ tân:

```text
Left: conversation list
  search room
  filters: Open, PendingStaff, Assigned to me, Closed
  sort: newest, oldest pending, priority

Center: message thread
  room header
  guest language
  rule ack status
  messages
  reply box
  canned replies

Right: room context
  room info
  visit/session info
  internal notes
  previous conversations
  actions: assign, priority, close, reopen
```

MVP có thể làm 2 cột trước: list + thread/context.

### 6.4 Rooms & QR

Màn Rooms:

- Table columns:
  - Room number,
  - Building,
  - Floor,
  - Status,
  - QR status,
  - Last scanned,
  - Open conversations,
  - Actions.
- Actions:
  - Edit,
  - Download QR PNG,
  - Print label/PDF,
  - Revoke QR,
  - Disable room.

QR label nên in rõ:

```text
Star Hill Resort
Scan for room guide & reception
Room A-203
[QR]
```

Không nên in URL token dài dưới QR nếu lo khách copy/share, nhưng có thể in short support text.

### 6.5 Rules editor

Rules không nên là CRUD section đơn thuần. Nên là content workflow:

```text
Rule Set
  Current published version: v12
  Draft has changes: yes/no

Sections
  Order
  Title per language
  Required
  Require scroll end
  Min read seconds
  Missing translations

Actions
  Preview as guest
  Save draft
  Publish new version
  View version history
```

Publish rule:

- Save draft không bắt khách đọc lại.
- Publish mới tăng version và bắt guest ack lại.
- Version history lưu ai publish, lúc nào, ghi chú thay đổi.

### 6.6 FAQ editor

FAQ editor cần hỗ trợ cây:

```text
Category: Wifi
  [1] Mật khẩu wifi là gì?
      [1.1] Tôi nhập đúng nhưng không vào được
      [1.2] Wifi yếu trong phòng
  [2] Có wifi ở hồ bơi không?
```

Chức năng:

- Drag-drop order.
- Active/inactive.
- Missing translation indicator.
- Preview guest language.
- Link FAQ item to canned reply hoặc chat category.
- Basic analytics: views, helpful/not helpful, escalated to chat.

### 6.7 Notes

Ghi chú nội bộ cần rõ phạm vi:

- Room note: gắn phòng, lâu dài, ví dụ "Phòng này remote TV hay hỏng".
- Conversation note: gắn hội thoại, ví dụ "Khách yêu cầu extra towel".
- Visit note: gắn phiên khách, ví dụ "Gia đình có trẻ nhỏ".

MVP có thể chỉ có room note và conversation note.

### 6.8 Settings

Settings nên có:

- Resort profile: name, logo, timezone, default language.
- Languages: bật/tắt, default, thứ tự hiển thị.
- Guest portal:
  - chat enabled,
  - FAQ enabled,
  - require rule ack before FAQ,
  - require rule ack before chat,
  - guest visit expiry days.
- Messaging:
  - rate limit,
  - max message length,
  - attachment enable/disable,
  - allowed MIME.
- QR:
  - base URL,
  - label template,
  - token rotation policy.

---

## 7. Kiến trúc kỹ thuật đề xuất

### 7.1 Backend

Nên dùng modular monolith:

```text
ResortQr.Api
  Controllers
  SignalR Hubs
  Middleware
  Auth setup

ResortQr.Application
  Use cases
  DTOs
  Validators
  Interfaces

ResortQr.Domain
  Entities
  Enums
  Domain rules

ResortQr.Infrastructure
  EF Core
  Repositories
  Storage
  QR/PDF
  Email/notification adapters

ResortQr.Tests
  Unit
  Integration
```

Module boundary:

```text
Identity
Resorts
Rooms
QrTokens
GuestAccess
Rules
Faq
Messaging
Notes
Dashboard
Audit
Files
```

Không nên tạo repository generic quá sớm. Với EF Core, service/use case có thể dùng DbContext trực tiếp trong Application qua interface nếu bạn muốn giảm boilerplate.

### 7.2 Frontend

Hai app riêng:

```text
/apps/guest-web
/apps/admin-web
/packages/shared-api-client
/packages/shared-types
```

Nếu muốn đơn giản ban đầu:

```text
/guest-web
/admin-web
/backend
```

Guest app:

- Vue 3 + TypeScript + Vite.
- Pinia rất nhẹ.
- Vue Router.
- vue-i18n.
- SignalR client chỉ lazy-load sau khi vào chat.
- CSS nhẹ, hạn chế UI library nặng.

Admin app:

- Vue 3 + TypeScript + Vite.
- Pinia, Vue Router.
- UI lib: PrimeVue hoặc Element Plus.
- TanStack Query cho server state nếu muốn dashboard mượt.
- Rich text editor: TipTap hoặc Quill, nhưng phải sanitize backend.

### 7.3 Database

Khuyến nghị PostgreSQL:

- JSONB tốt nếu sau này cần metadata.
- Index tốt cho search/filter.
- Chi phí triển khai linh hoạt.
- Dễ chạy local bằng Docker.

SQL Server cũng ổn nếu:

- Bạn deploy trên Windows/IIS/Azure SQL.
- Team quen SQL Server.
- Resort/công ty đã có license/hạ tầng.

### 7.4 Storage

MVP:

- QR PNG/PDF có thể generate on-demand, không cần lưu file lâu dài.
- Attachment nếu bật: lưu local disk trong dev, S3-compatible/Blob ở production.

Production:

- Dùng S3-compatible storage, Azure Blob hoặc MinIO.
- File guest upload không public trực tiếp.
- Serve qua signed URL ngắn hạn hoặc endpoint kiểm quyền.

### 7.5 QR/PDF

Thư viện đề xuất:

- QR: QRCoder.
- PDF: **QuestPDF** (đã chốt — dễ kiểm soát layout label, thuần managed, không cần native runtime). *DinkToPdf từng được cân nhắc nhưng loại vì kéo theo native lib không cần thiet.*

QR URL:

```text
https://guest.starhill.example/r/{token}
```

Không dùng:

```text
https://guest.starhill.example/r?room=203
```

Token:

- 32 bytes random trở lên.
- Encode base64url hoặc ULID-like random.
- Không dùng GUID tuần tự.
- Rate limit resolve để chống brute force.

---

## 8. Data model đề xuất

### 8.1 Bảng cốt lõi

```text
Resort
  Id
  Name
  Slug
  DefaultLanguage
  Timezone
  LogoUrl
  CreatedAt
  UpdatedAt

ResortLanguage
  Id
  ResortId
  Code
  DisplayName
  IsEnabled
  IsDefault
  SortOrder

Room
  Id
  ResortId
  RoomNumber
  Building
  Floor
  Status
  CreatedAt
  UpdatedAt
  DeletedAt nullable

RoomQrToken
  Id
  RoomId
  TokenHash
  TokenPreview
  Status
  Version
  CreatedAt
  CreatedByUserId
  RevokedAt nullable
  RevokedByUserId nullable
  RevocationReason nullable

GuestSession
  Id
  SessionKeyHash
  FirstSeenAt
  LastSeenAt
  PreferredLanguage
  UserAgentHash

GuestVisit
  Id
  ResortId
  RoomId
  GuestSessionId
  Status
  StartedAt
  LastSeenAt
  ExpiresAt
  ClosedAt nullable
```

### 8.2 Nội quy

```text
RuleSet
  Id
  ResortId
  Status: Draft | Published | Archived
  DraftBaseVersion nullable
  CreatedAt
  UpdatedAt

RulePublication
  Id
  ResortId
  Version
  RuleSetId
  PublishedAt
  PublishedByUserId
  ChangeNote

RuleSection
  Id
  RuleSetId
  Key
  SortOrder
  IsRequired
  RequireScrollEnd
  MinReadSeconds

RuleSectionTranslation
  Id
  RuleSectionId
  LanguageCode
  Title
  BodyHtmlSanitized
  PlainTextSearch

RuleAcknowledgement
  Id
  ResortId
  RoomId
  GuestSessionId
  GuestVisitId nullable
  RulePublicationId
  Version
  LanguageCode
  AcceptedAt
  IpHash
  UserAgent
```

Vì sao nên có `RulePublicationId` thay vì chỉ `RuleVersion`:

- Query rõ khách đồng ý chính xác publication nào.
- Version history dễ audit.
- Nếu cần export pháp lý, có snapshot nội dung đã publish.

### 8.3 FAQ

```text
FaqCategory
  Id
  ResortId
  Key
  SortOrder
  IsActive

FaqCategoryTranslation
  Id
  FaqCategoryId
  LanguageCode
  Name

FaqItem
  Id
  ResortId
  CategoryId
  ParentId nullable
  SortOrder
  IsActive
  ChatCategory nullable

FaqItemTranslation
  Id
  FaqItemId
  LanguageCode
  Question
  AnswerHtmlSanitized
  SearchText

FaqEvent
  Id
  ResortId
  RoomId nullable
  GuestSessionId nullable
  FaqItemId nullable
  EventType: View | Helpful | NotHelpful | EscalateToChat | SearchNoResult
  Query nullable
  LanguageCode
  CreatedAt
```

`FaqEvent` không bắt buộc trong ngày đầu, nhưng rất đáng làm sớm vì dashboard sẽ biết khách cần gì.

### 8.4 Messaging

```text
Conversation
  Id
  ResortId
  RoomId
  GuestSessionId
  GuestVisitId nullable
  Status
  Priority
  Category
  AssignedToUserId nullable
  LastMessageAt
  LastGuestMessageAt nullable
  LastStaffMessageAt nullable
  UnreadForStaff
  UnreadForGuest
  CreatedAt
  ClosedAt nullable
  ClosedByUserId nullable

Message
  Id
  ConversationId
  SenderType: Guest | Staff | System
  SenderUserId nullable
  Body
  AttachmentId nullable
  CreatedAt
  DeliveredAt nullable
  ReadByStaffAt nullable
  ReadByGuestAt nullable

Attachment
  Id
  ResortId
  UploadedByType
  UploadedByUserId nullable
  StorageKey
  OriginalFileName
  ContentType
  SizeBytes
  ScanStatus
  CreatedAt

InternalNote
  Id
  ResortId
  RoomId nullable
  ConversationId nullable
  GuestVisitId nullable
  AuthorUserId
  Body
  CreatedAt
  UpdatedAt nullable
```

### 8.5 Identity/Audit

```text
AppUser
  Id
  ResortId
  Email
  DisplayName
  PasswordHash
  Role: Admin | Staff | Manager
  IsActive
  LastLoginAt
  CreatedAt

RefreshToken
  Id
  UserId
  TokenHash
  ExpiresAt
  RevokedAt nullable
  CreatedAt

AuditLog
  Id
  ResortId
  ActorUserId nullable
  ActorType: User | System
  Action
  EntityType
  EntityId
  BeforeJson nullable
  AfterJson nullable
  IpHash nullable
  UserAgent nullable
  CreatedAt
```

Audit nên ghi:

- tạo/sửa/xóa phòng,
- revoke QR,
- publish rules,
- sửa FAQ,
- tạo/sửa user,
- đóng/reopen conversation,
- đổi settings.

---

## 9. API design đề xuất

### 9.1 Guest API

Base:

```text
/api/guest
```

Resolve:

```http
GET /api/guest/resolve/{token}
```

Response:

```json
{
  "room": {
    "id": "room_123",
    "number": "A-203",
    "building": "A",
    "floor": "2"
  },
  "resort": {
    "id": "resort_1",
    "name": "Star Hill Resort",
    "logoUrl": "/assets/logo.png",
    "defaultLanguage": "vi",
    "languages": ["vi", "en", "ko", "zh"]
  },
  "guest": {
    "sessionId": "not-sensitive-display-id",
    "visitStatus": "Active",
    "preferredLanguage": "vi"
  },
  "rules": {
    "currentVersion": 12,
    "acknowledged": false,
    "acknowledgedVersion": null
  },
  "features": {
    "faqEnabled": true,
    "chatEnabled": true,
    "ruleAckRequiredForFaq": true,
    "ruleAckRequiredForChat": true
  }
}
```

Rules:

```http
GET /api/guest/rules?lang=vi
POST /api/guest/rules/acknowledgements
```

FAQ:

```http
GET /api/guest/faq?lang=vi
POST /api/guest/faq/events
```

Conversation:

```http
GET /api/guest/conversation
POST /api/guest/messages
POST /api/guest/messages/{id}/read
```

### 9.2 Admin API

Auth:

```http
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/auth/me
```

Rooms/QR:

```http
GET    /api/admin/rooms
POST   /api/admin/rooms
GET    /api/admin/rooms/{id}
PUT    /api/admin/rooms/{id}
DELETE /api/admin/rooms/{id}
POST   /api/admin/rooms/{id}/qr-token/revoke
GET    /api/admin/rooms/{id}/qr.png
POST   /api/admin/rooms/qr-labels.pdf
```

Rules:

```http
GET    /api/admin/rules/draft
PUT    /api/admin/rules/draft
POST   /api/admin/rules/publish
GET    /api/admin/rules/publications
GET    /api/admin/rules/publications/{id}
```

FAQ:

```http
GET    /api/admin/faq/tree
POST   /api/admin/faq/categories
PUT    /api/admin/faq/categories/{id}
DELETE /api/admin/faq/categories/{id}
POST   /api/admin/faq/items
PUT    /api/admin/faq/items/{id}
DELETE /api/admin/faq/items/{id}
POST   /api/admin/faq/reorder
```

Inbox:

```http
GET  /api/admin/conversations?status=PendingStaff&room=A-203
GET  /api/admin/conversations/{id}
POST /api/admin/conversations/{id}/reply
POST /api/admin/conversations/{id}/read
POST /api/admin/conversations/{id}/close
POST /api/admin/conversations/{id}/reopen
PUT  /api/admin/conversations/{id}/assignment
PUT  /api/admin/conversations/{id}/priority
```

Notes:

```http
GET    /api/admin/notes?roomId=&conversationId=
POST   /api/admin/notes
PUT    /api/admin/notes/{id}
DELETE /api/admin/notes/{id}
```

Dashboard:

```http
GET /api/admin/dashboard/stats
GET /api/admin/dashboard/activity
GET /api/admin/reports/faq
```

### 9.3 Error contract

Dùng ProblemDetails:

```json
{
  "type": "https://docs.starhill/errors/rule-ack-required",
  "title": "Rule acknowledgement required",
  "status": 403,
  "code": "rule_ack_required",
  "traceId": "00-..."
}
```

Mã lỗi guest nên ổn định:

- `qr_invalid`
- `qr_revoked`
- `room_inactive`
- `rule_ack_required`
- `rate_limited`
- `message_too_long`
- `attachment_not_allowed`
- `language_not_supported`

---

## 10. SignalR design

Hub:

```text
/hubs/chat
```

Groups:

```text
resort:{resortId}:staff
conversation:{conversationId}
room:{roomId}
```

Server events:

```text
ConversationCreated
ConversationUpdated
MessageReceived
MessageRead
Typing
StaffAssigned
ConversationClosed
```

Client commands:

```text
JoinConversation(conversationId)
LeaveConversation(conversationId)
Typing(conversationId)
```

Quan trọng:

- Staff khi login join `resort:{resortId}:staff`.
- Guest chỉ được join conversation của chính GuestSession.
- Backend phải kiểm tra quyền khi join group, không tin client truyền id.
- Khi WebSocket fail, guest polling `GET /api/guest/conversation` mỗi 10-20 giây.
- Admin có thể polling nhẹ cho inbox count nếu SignalR mất.

---

## 11. Bảo mật và riêng tư

### 11.1 Threat model

| Nguy cơ | Ví dụ | Giảm thiểu |
|---|---|---|
| Đoán QR token | Bot thử token | CSPRNG 32 bytes, hash token, rate limit resolve |
| Khách cũ giữ link | Khách checkout vẫn nhắn | GuestVisit expiry, staff close, optional PMS integration |
| XSS content | Admin paste script vào nội quy | Sanitize backend, CSP |
| Spam tin nhắn | Bot gửi liên tục | Rate limit theo session/IP/room, max length |
| Upload độc hại | File giả ảnh | MIME sniffing, size limit, storage private, scan nếu có |
| Lộ note nội bộ | Guest API trả note | Contract test đảm bảo notes không xuất hiện guest response |
| Staff vượt quyền | Staff sửa nội quy | Role policy + integration test 403 |
| CSRF | Website khác gọi POST guest/admin | SameSite cookie, anti-forgery cho cookie auth, CORS strict |
| Token trong log | URL token bị log | Không log full path token, mask token |

### 11.2 Admin auth

Tốt nhất cho SPA:

- Access token ngắn hạn trong memory.
- Refresh token HttpOnly Secure cookie.
- Refresh token rotation.
- Logout revoke refresh token.
- Role claims: Admin, Staff, Manager.

Nếu muốn đơn giản hơn:

- JWT trong memory + refresh cookie vẫn là lựa chọn cân bằng.
- Tránh lưu long-lived JWT trong localStorage.

### 11.3 Guest session

Cookie:

```text
Name: shq_guest
HttpOnly: true
Secure: true
SameSite: Lax
MaxAge: 30-90 days
```

Không lưu:

- room number,
- token,
- thông tin nhạy cảm.

Session key trong cookie nên random; DB lưu hash.

### 11.4 Content security

Backend sanitize HTML bằng allowlist:

- Cho phép: `p`, `strong`, `em`, `ul`, `ol`, `li`, `a`, `br`, `h2`, `h3`, `table` nếu cần.
- Cấm: `script`, inline event handler, iframe, style nguy hiểm.
- Link `target=_blank` phải thêm `rel=noopener noreferrer`.

CSP:

```text
default-src 'self';
script-src 'self';
style-src 'self' 'unsafe-inline';
img-src 'self' data: blob:;
connect-src 'self' wss:;
frame-ancestors 'none';
```

Tùy UI lib có thể phải tinh chỉnh style-src.

### 11.5 Privacy

Vì khách không đăng nhập, không nên thu thập quá nhiều:

- Lưu IP dạng hash có salt xoay vòng hoặc app secret.
- UserAgent có thể lưu raw hoặc hash tùy nhu cầu đối soát.
- Tin nhắn có thể chứa dữ liệu cá nhân, cần chính sách retention.
- Notes nội bộ cần audit ai viết/sửa/xóa.

Retention đề xuất:

- Conversation/message: 12-24 tháng hoặc theo chính sách resort.
- Audit log: 12-24 tháng.
- Rule acknowledgement: giữ lâu hơn nếu có yêu cầu đối soát.

---

## 12. Internationalization

### 12.1 Ngôn ngữ hiển thị

Resolve ngôn ngữ:

```text
1. ?lang=
2. localStorage guestLang
3. navigator.languages[0..n]
4. resort default language
```

Chuẩn hóa:

```text
vi-VN -> vi
en-US -> en
ko-KR -> ko
zh-CN -> zh
zh-TW -> zh-Hant nếu hệ thống hỗ trợ
```

### 12.2 Tách UI text và content

UI text:

- nằm trong frontend JSON.
- ví dụ: nút, label, error text.

Content:

- nội quy, FAQ, canned replies.
- nằm trong DB theo language.

Fallback:

- Nếu thiếu bản dịch, trả default language.
- Response nên kèm `isFallback: true` để frontend/admin biết.

Ví dụ:

```json
{
  "title": "Breakfast time",
  "bodyHtml": "<p>Breakfast is served from 6:30 to 10:00.</p>",
  "language": "en",
  "requestedLanguage": "ko",
  "isFallback": true
}
```

### 12.3 Admin missing translation UX

Trong Rules/FAQ editor:

```text
Section "Pool rules"
[vi OK] [en OK] [ko Missing] [zh Missing]
```

Nên có filter:

- Show missing translations.
- Show inactive.
- Show changed since last publish.

---

## 13. Deployment design

### 13.1 Simple production topology

```text
Nginx/Caddy/IIS reverse proxy
  /               -> guest-web static
  /admin          -> admin-web static
  /api            -> ASP.NET Core API
  /hubs           -> ASP.NET Core SignalR

PostgreSQL
Object storage
```

Tách subdomain cũng tốt:

```text
guest.starhill.example
admin.starhill.example
api.starhill.example
```

Nếu dùng cookie cross-subdomain, cấu hình SameSite/CORS phải chặt hơn. Với MVP, deploy cùng origin dễ hơn:

```text
https://portal.starhill.example/r/{token}
https://portal.starhill.example/admin
https://portal.starhill.example/api
```

### 13.2 Environments

```text
local
staging
production
```

Mỗi environment cần:

- connection string riêng,
- storage riêng,
- JWT keys riêng,
- base URL QR riêng,
- CORS origin riêng.

### 13.3 Observability

Log có cấu trúc:

- request id,
- user id nếu admin,
- room id nếu guest resolve,
- conversation id,
- error code.

Không log:

- full QR token,
- password,
- refresh token,
- full message body nếu không cần.

Metrics:

- QR resolve count,
- invalid token count,
- message sent count,
- SignalR connected clients,
- unread conversations,
- API latency,
- 4xx/5xx rate.

Health checks:

```text
/health/live
/health/ready
```

Ready kiểm tra DB/storage.

### 13.4 Backup

Production tối thiểu:

- DB backup hằng ngày.
- Retention 14-30 ngày.
- Restore rehearsal mỗi tháng.
- Storage backup nếu có attachments.

---

## 14. Testing strategy

### 14.1 Unit tests

Nên test:

- Token generation entropy/format.
- Language fallback.
- Rule gate decision.
- Rule publication versioning.
- FAQ tree building.
- Conversation unread counters.
- Rate limit policy.

### 14.2 Integration tests

Nên test theo API:

- Resolve token hợp lệ.
- Token revoked trả lỗi không lộ room.
- Guest chưa ack không lấy được FAQ/chat nếu config yêu cầu.
- Ack version hiện tại thành công.
- Publish version mới làm guest phải ack lại.
- Staff không gọi được admin settings.
- Note không bao giờ xuất hiện ở guest API.

### 14.3 E2E tests

Playwright:

- Guest: scan route -> rules -> ack -> FAQ -> chat.
- Admin: login -> room -> QR -> rule publish -> inbox reply.
- Mobile viewport iPhone/Android.

### 14.4 Correctness properties nên thêm

Bên cạnh 10 properties hiện tại, nên thêm:

1. Chỉ một active QR token cho mỗi room.
2. Revoke QR không xóa audit/history.
3. Draft rules không ảnh hưởng guest cho tới khi publish.
4. Admin note không bao giờ trả qua guest endpoints.
5. Guest không thể join SignalR conversation của session khác.
6. Message gửi khi chưa ack bị chặn ở backend.
7. GuestVisit hết hạn không tự động cho phép khách cũ tiếp tục conversation nếu policy chặn.

---

## 15. Roadmap triển khai

### Wave 0 - Quyết định trước khi code

Chốt:

- .NET 10 LTS hay .NET 8.
- PostgreSQL hay SQL Server.
- Single resort hay multi-resort ngay từ đầu.
- GuestVisit expiry bao nhiêu ngày.
- Có cho upload ảnh ở MVP không.
- Có dùng subdomain riêng admin/api không.
- Danh sách ngôn ngữ đầu tiên: vi, en, ko, zh?

Khuyến nghị mặc định:

```text
.NET 10 LTS
PostgreSQL
Single-resort-ready nhưng schema có ResortId
GuestVisit expiry 3-7 ngày
MVP chưa upload ảnh
Một domain portal.starhill.example
Ngôn ngữ: vi, en, ko, zh
```

### Wave 1 - Backend foundation

- Solution structure.
- EF Core, migrations.
- ProblemDetails.
- Auth admin/staff.
- Seed resort/admin/default languages.
- Audit middleware/helper.

### Wave 2 - Room/QR/Guest access

- Room CRUD.
- RoomQrToken.
- QR PNG/PDF.
- Resolve token.
- GuestSession/GuestVisit.
- Rate limit resolve.

### Wave 3 - Rules

- Rule draft/publish model.
- Rule section translations.
- Guest rules endpoint.
- Ack endpoint.
- Backend rule gate.

### Wave 4 - FAQ

- Category/item tree.
- Translation fallback.
- Guest FAQ endpoint.
- Admin FAQ editor API.
- FAQ event tracking basic.

### Wave 5 - Messaging

- Conversation/message.
- Guest send/read.
- Admin inbox/reply/close.
- SignalR.
- Polling fallback.
- Notes.

### Wave 6 - Guest Web

- Mobile app shell.
- Resolve + language.
- Rule gate.
- Home.
- FAQ.
- Chat.

### Wave 7 - Admin Web

- Login.
- Dashboard.
- Rooms/QR.
- Rules editor.
- FAQ editor.
- Inbox.
- Notes.
- Settings.

### Wave 8 - Hardening

- Security test.
- E2E.
- Backup/restore.
- Logs/metrics.
- Production deploy checklist.
- Staff training notes.

---

## 16. UI design principles

### 16.1 Guest Web

Guest web phải:

- mở nhanh,
- mobile-first,
- ít chữ hệ thống,
- nội dung dễ đọc,
- nút lớn,
- contrast tốt,
- dùng được bằng một tay,
- không bắt login,
- không giống app admin thu nhỏ.

Không nên:

- hero marketing,
- animation nặng,
- nhiều card trang trí,
- menu phức tạp,
- modal chồng modal.

### 16.2 Admin Dashboard

Admin dashboard phải:

- dense nhưng sạch,
- table/filter tốt,
- trạng thái rõ,
- thao tác nhanh,
- không lẫn content public và note nội bộ.

Màu/trạng thái:

```text
Open/PendingStaff: nổi bật
PendingGuest: trung tính
Closed: muted
Urgent: đỏ
Internal note: nền riêng, không giống message guest
```

### 16.3 Dashboard first screen

First screen của admin nên vào thẳng công việc:

```text
Top bar: resort, user, notifications
Sidebar: Dashboard, Inbox, Rooms, Rules, FAQ...
Main:
  KPI row
  Needs attention
  Recent messages
  Operational quick actions
```

---

## 17. Quyết định kỹ thuật chi tiết

### 17.1 ASP.NET Core

Khuyến nghị:

- Minimal API cho endpoints nhỏ hoặc Controllers cho tổ chức rõ; với app này Controllers dễ quản lý hơn.
- FluentValidation cho DTO validation.
- ProblemDetails chuẩn.
- EF Core migrations.
- Serilog hoặc built-in structured logging.
- RateLimiter middleware của ASP.NET Core.
- SignalR cho realtime.
- Background service cho cleanup expired visits/tokens.

### 17.2 Vue

Guest:

- Không dùng UI library lớn nếu không cần.
- CSS custom hoặc Tailwind nếu team quen.
- Lazy-load Chat/SignalR.
- Cache rules/FAQ theo version/language trong session/localStorage nếu cần.

Admin:

- PrimeVue/Element Plus để có table, dialog, tabs, form, dropdown.
- Route guards theo role.
- API client có refresh token flow.
- Rich text editor lazy-load.

### 17.3 Sanitization

Sanitize ở backend là bắt buộc. Frontend không được coi là lớp bảo vệ chính.

Flow:

```text
Admin nhập rich text
  -> API validate
  -> sanitize
  -> lưu BodyHtmlSanitized
  -> guest/admin preview render sanitized HTML
```

### 17.4 Search

MVP:

- Search FAQ bằng `ILIKE` hoặc full-text PostgreSQL đơn giản.

Phase 2:

- Unaccent,
- synonyms,
- analytics no-result,
- AI/semantic search nếu dữ liệu lớn.

---

## 18. Open decisions

| Quyết định | Khuyến nghị | Vì sao |
|---|---|---|
| .NET version | .NET 10 LTS nếu build mới | Support dài hơn .NET 8 |
| Database | PostgreSQL | Linh hoạt, chi phí tốt, dễ Docker |
| Single vs multi resort | Schema có ResortId, UI chạy single resort trước | Không khóa đường mở rộng |
| QR token storage | Hash token + token preview | Giảm rủi ro nếu DB lộ |
| GuestVisit | Có, expiry 3-7 ngày | Giảm rủi ro khách cũ |
| Upload ảnh | Phase 2 | MVP an toàn hơn |
| Admin auth | Access token memory + refresh cookie | Giảm rủi ro localStorage |
| Rule workflow | Draft -> Publish | Tránh chỉnh nháp ảnh hưởng khách |
| PDF QR | QuestPDF | Layout label tốt |
| UI library admin | PrimeVue hoặc Element Plus | Nhanh làm dashboard |

---

## 19. Checklist sẵn sàng code

Trước khi bắt đầu implement, nên trả lời xong:

- [ ] Dùng .NET 10 LTS hay .NET 8?
- [ ] Dùng PostgreSQL hay SQL Server?
- [ ] Có bắt buộc multi-resort ngay từ bản đầu không?
- [ ] GuestVisit hết hạn sau mấy ngày?
- [ ] Staff có cần assignment/priority ở MVP không?
- [ ] Tin nhắn MVP có upload ảnh không?
- [ ] Nội quy có cần phê duyệt nhiều cấp không?
- [ ] Ngôn ngữ bật ban đầu là gì?
- [ ] QR label dùng logo/file nào?
- [ ] Deploy cùng domain hay tách subdomain?

---

## 20. Bản thiết kế MVP đề xuất chốt

Nếu cần chốt nhanh để code, mình đề xuất:

```text
Backend:
  ASP.NET Core .NET 10 LTS
  EF Core
  PostgreSQL
  SignalR
  QRCoder
  QuestPDF
  FluentValidation
  HtmlSanitizer

Frontend:
  Vue 3 + TypeScript + Vite
  Guest Web riêng
  Admin Web riêng
  Pinia + Vue Router + vue-i18n
  Admin dùng PrimeVue

Core:
  ResortId trong schema
  RoomQrToken history
  GuestSession + GuestVisit
  Rule Draft/Publish + Acknowledgement
  FAQ tree + translation fallback
  Conversation + Message + InternalNote
  AuditLog

MVP UX:
  Guest: QR -> rules -> home -> FAQ/chat
  Admin: dashboard -> rooms/QR -> rules -> FAQ -> inbox/notes
```

Đây là hướng đủ chắc để triển khai thật, vẫn giữ codebase gọn và không tự làm khó mình bằng microservice hay app mobile.

---

## 21. Decision Log (chốt ngày 2026-07-01)

Đây là các quyết định của chủ dự án trả lời cho phần đánh giá/lỗ hổng ở trên. `requirements.md`, `design.md`, `tasks.md` đã được cập nhật theo các chốt này.

| # | Chủ đề | Quyết định | Ảnh hưởng thiết kế |
|---|---|---|---|
| D1 | Phạm vi mạng | Portal chỉ chạy trong **WiFi nội bộ resort**, KHÔNG publish ra internet. Khách phải kết nối WiFi resort (đã qua captive portal WiFi) mới truy cập được. | Bỏ threat model nặng (bot đoán token, CSRF cross-site nghiêm trọng). Vẫn giữ sanitize HTML + rate limit nhẹ. "Khách cũ giữ link" không còn là rủi ro vì phải ở tại chỗ. |
| D2 | Phiên khách | ⚠️ **SUPERSEDED bởi D11 (mục 22)**. (Bản cũ: hết hạn 30–60'.) Bản đúng: GuestSession = thiết bị (cookie 30–90 ngày); GuestVisit idle 24h/đóng thủ công; PortalWindow 30' là cửa sổ thao tác. | Xem D11. |
| D3 | Nội quy | Theo phương án **Draft → Publish** có version. Lễ tân/Admin tạo & sửa nội quy trên **giao diện** (rich text đa ngôn ngữ). | Giữ RuleSet/RulePublication. Quyền tạo/sửa mở cho cả Staff (không chỉ Admin) — cấu hình được. |
| D4 | FAQ | Lễ tân **tự tạo FAQ** qua giao diện vì họ hiểu khách hỏi gì. Dạng câu hỏi + **flow cha-con**. | FAQ editor có cây, drag-drop, đa ngôn ngữ. Quyền tạo/sửa mở cho Staff. |
| D5 | Tin nhắn | Dashboard **gom hội thoại theo phòng**, hiển thị **badge chưa đọc**, lễ tân bấm vào đọc/trả lời. | MVP bỏ assignment/priority/SLA/category phức tạp. Schema vẫn chừa cột để mở rộng sau. |
| D6 | IA/Wireframe | Chủ dự án chưa quen khái niệm → team đề xuất. | design.md bổ sung mục giải thích IA + wireframe dạng text cho guest & admin. |
| D7 | Bảo mật/Vận hành | Không public ngoài WiFi. **Không cần backup**. Chỉ cần **log có cấu trúc** đủ để dev debug. | Bỏ yêu cầu backup/restore rehearsal khỏi MVP. Giữ structured logging + health check nhẹ. |
| D8 | Pháp lý | **Không thổi phồng** giá trị pháp lý của việc đồng ý nội quy. | Bỏ từ ngữ "pháp lý"; acknowledgement chỉ là bản ghi xác nhận đã bấm đồng ý phục vụ vận hành. |
| D9 | Ticket dọn phòng (MỚI) | Khách có thể **tạo yêu cầu dọn phòng**. Nhân viên đánh dấu hoàn tất bằng: (a) vào app tick xong, hoặc (b) **login nhân viên rồi quét QR phòng** = đã dọn xong. Nhiều cách cho tiện. | Thêm nghiệp vụ Housekeeping: entity `HousekeepingTicket`, trạng thái, endpoint staff-scan-complete. |
| D10 | Stack | ⚠️ **Cập nhật bởi D11–D18**: đã chốt **.NET 10 LTS + PostgreSQL** (không còn để ngỏ .NET 8). Giữ ASP.NET Core + 2 Vue SPA + SignalR. | Không đổi hướng. |

### Ghi chú về bảo mật khi chạy trong WiFi nội bộ

Vì portal không ra internet, bề mặt tấn công giảm mạnh, nhưng vẫn giữ vài lớp cơ bản vì khách vẫn là người ngoài dùng chung mạng:
- Sanitize HTML nội dung admin/staff nhập (chống XSS lẫn nhau qua nội quy/FAQ).
- Rate limit nhẹ cho gửi tin nhắn & tạo ticket (chống nghịch/spam).
- Token QR vẫn random không đoán (đơn giản, không cần hash phức tạp ở MVP).
- Admin/Staff vẫn cần đăng nhập; guest vẫn dùng cookie phiên ngắn hạn.

---

## 22. Decision Log — bổ sung (chốt sau expert review)

| # | Chủ đề | Quyết định |
|---|---|---|
| D11 | Mô hình phiên (làm rõ) | **GuestSession** = thiết bị (cookie 30–90 ngày). **GuestVisit** = lượt lưu trú/phòng, kết thúc khi lễ tân đóng hoặc idle 24h; quét lại cùng thiết bị+phòng thì nối lại visit Active. **PortalWindow** = cửa sổ thao tác 30' (quá hạn quét lại, vẫn nối visit). Hội thoại + acknowledgement gắn theo GuestVisit. |
| D12 | QR token lifecycle | Token chỉ **Active/Revoked**, rotate thủ công. **KHÔNG auto-expire** mã đã in/dán. Background job chỉ dọn GuestVisit, không đụng token. |
| D13 | HTTPS/deploy | Bắt buộc **HTTPS cert hợp lệ + DNS nội bộ** (secure context cho camera). Tránh self-signed. MVP cùng origin sau reverse proxy. |
| D14 | Quyền Staff/Admin | CRUD phòng/QR/revoke/users/settings = **Admin-only**; Staff xem phòng + xử lý vận hành/nội dung (nội quy/FAQ/tin nhắn/ticket/notes). |
| D15 | ResortSettings | Thêm entity cấu hình: bật/tắt ack trước FAQ/chat/ticket, PortalWindowMinutes, VisitIdleExpiryHours, GuestWebBaseUrl, MaxMessageLength, rate limit. `/resolve` trả feature flags từ đây. |
| D16 | Nguồn ngôn ngữ mặc định | Dùng `ResortLanguage.IsDefault` (đúng một bản ghi), **bỏ** `Resort.DefaultLanguage`. |
| D17 | Tính đúng đắn thêm | acknowledge do **server** xác định publication (không tin client); DB partial unique index (1 token active/phòng, 1 conversation open/visit, 1 ticket mở/phòng); optimistic concurrency cho editor nội quy/FAQ/settings; `/resolve` trả contract rõ; idempotency cho messages/housekeeping; inbox phân biệt lượt hiện tại vs lịch sử. |
| D18 | Baseline bảo mật rẻ | SameSite cookie, CORS allowlist, Referrer-Policy, không log path chứa token, rate limit theo IP/session. |
