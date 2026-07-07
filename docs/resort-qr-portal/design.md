# Design Document

## Overview

Resort QR Portal (Star Hill Guest App) gồm một backend ASP.NET Core (+ EF Core) cung cấp REST API + SignalR Hub, và hai frontend Vue 3 riêng biệt: **Guest Web** (public trong mạng nội bộ, mobile-first, không đăng nhập) và **Admin Dashboard** (đăng nhập, phân quyền). Dữ liệu lưu trong **PostgreSQL** qua EF Core.

**Bối cảnh triển khai (quyết định D1):** hệ thống chạy trong **WiFi nội bộ resort, không public ra internet**. Khách phải kết nối WiFi resort mới mở được portal. Nhờ vậy:
- Rủi ro "khách cũ giữ link vẫn nhắn tin" gần như không còn (muốn dùng phải đang ở tại chỗ).
- Bề mặt tấn công nhỏ → không cần threat model nặng, không cần backup/observability phức tạp.
- Vẫn giữ vài lớp cơ bản vì khách là người ngoài dùng chung mạng: sanitize HTML, rate limit nhẹ, đăng nhập cho nội bộ, phiên khách ngắn hạn.

Thiết kế xoay quanh 5 nghiệp vụ lõi:
1. Phân giải QR token → nhận diện phòng (và phân biệt ngữ cảnh khách vs nhân viên đã đăng nhập).
2. Đa ngôn ngữ tự động nhận diện ngôn ngữ điện thoại.
3. Nội quy Draft → Publish + force-read + acknowledgement (bản ghi vận hành, không phải pháp lý).
4. FAQ dạng cây do lễ tân tự soạn + nhắn tin gom theo phòng.
5. Yêu cầu dọn phòng (housekeeping ticket) + đường tắt nhân viên quét QR đánh dấu đã dọn.

> **Đã chốt:** runtime **.NET 10 LTS**, database **PostgreSQL** (Npgsql). Guest web mặc định **tiếng Anh**; admin dashboard **tiếng Việt**.

## Architecture

### Sơ đồ tổng thể

```
        WiFi nội bộ resort (không ra internet)
┌─────────────────────┐        ┌──────────────────────┐
│   Guest Web (Vue 3)  │        │ Admin Dashboard (Vue3)│
│  mobile-first SPA    │        │   SPA + auth          │
└─────────┬───────────┘        └──────────┬────────────┘
          │  REST + SignalR (WebSocket)    │
          ▼                                ▼
┌─────────────────────────────────────────────────────┐
│                 ASP.NET Core API                      │
│  Api:      Controllers (REST), SignalR Hub, Middleware│
│  Application: Use cases/Services, DTOs, Validators    │
│  Domain:   Entities, Enums                            │
│  Infra:    EF Core DbContext, QrService, PdfService   │
│  Cross:    Auth (JWT+refresh / guest cookie),         │
│            Localization, RateLimiting, StructuredLog  │
│  Background: dọn GuestVisit hết hạn (idle) — KHÔNG đụng QR token │
└─────────┬─────────────────────────────┬───────────────┘
          ▼                             ▼
   ┌────────────┐               ┌──────────────┐
   │ PostgreSQL │               │  Local disk   │
   │ (EF Core)  │               │ (QR/PDF tạm)  │
   └────────────┘               └──────────────┘
```

### Cấu trúc solution backend (modular monolith)

```
/backend
  ResortQr.Api            → Program.cs, Controllers, ChatHub, Middleware, DI
  ResortQr.Application    → Use cases/Services, DTOs, Interfaces, Validators
  ResortQr.Domain         → Entities, Enums
  ResortQr.Infrastructure → Ef DbContext, Migrations, QrService, PdfService, Seed
  ResortQr.Tests          → Unit/integration tests
```

Module theo nghiệp vụ (không tách microservice): Identity, Rooms, QrTokens, GuestAccess, Rules, Faq, Messaging, Housekeeping, Notes, Dashboard.

### Cấu trúc frontend

```
/guest-web   (Vite + Vue 3 + TS + Pinia + vue-router + vue-i18n)
  src/
    views/        ResolveLoading, TokenError, RuleGate, GuestHome,
                  RulesViewer, FaqFlow, Chat
    components/    RuleSection, ProgressBar, FaqTree, ChatBox,
                  LangSwitcher, QuickRequest, HousekeepingButton
    stores/        session, rules, faq, chat, housekeeping, i18n
    services/      api client, signalr client
    i18n/          vi.json, en.json, ko.json, zh.json

/admin-web   (Vite + Vue 3 + TS + Pinia + vue-router + PrimeVue|Element Plus)
  src/
    views/        Login, Dashboard, Inbox, Rooms, Rules, Faq,
                  Housekeeping, Notes, Users, Settings
    components/    RoomTable, QrDialog, RuleEditor, FaqEditor,
                  ConversationPanel, NotePanel, HousekeepingBoard, StaffScan
    stores/        auth, rooms, rules, faq, inbox, housekeeping, stats
    services/      api client, signalr client
```

**Lý do 2 SPA:** guest cần cực nhẹ, tải nhanh trên điện thoại; admin cần nhiều thành phần dashboard. Tách biệt bundle và bề mặt.

## Phân biệt ngữ cảnh quét QR (khách vs nhân viên)

**QR chỉ có một mục đích: cho biết chính xác đó là phòng nào** (chứa token → resolve ra phòng). QR không mang thông tin vai trò.

- **Khách** mở `/r/{token}` trên **guest-web** → luồng khách (nội quy/FAQ/chat/yêu cầu dọn phòng).
- **Nhân viên** đăng nhập vào **admin-web (link riêng)**. Cách hoàn tất dọn phòng:
  - **Chính (mọi lúc)**: nhân viên **chọn phòng trong app → bấm "Xác nhận đã dọn"**.
  - **Lối tắt cho tiện**: nhân viên bấm "Quét QR" trong admin-web (camera qua `html5-qrcode`) → đọc token → app tự chọn đúng phòng → xác nhận đã dọn.

Vai trò được xác định bằng **việc đang ở app nào / có đăng nhập hay không**, không phụ thuộc bản thân QR. Mọi hành động dọn phòng (tạo/nhận/hoàn tất) đều được **ghi lại nhật ký** (ai, lúc nào, bằng cách nào) — xem `HousekeepingEvent`.

## Components and Interfaces

### Service chính (Application layer)

- **IRoomService**: CRUD phòng, sinh/thu hồi token (1 token active/phòng, giữ lịch sử), resolve token → phòng.
- **IQrService / IPdfService**: sinh PNG QR và PDF nhãn nhiều phòng (QRCoder + QuestPDF).
- **IGuestAccessService**: tạo/đọc GuestSession (cookie), tạo/gia hạn/đóng GuestVisit, kiểm tra hết hạn.
- **IRuleService**: quản lý RuleSet Draft, publish → RulePublication (version), lấy nội quy theo ngôn ngữ, ghi + kiểm tra acknowledgement, enforce rule gate.
- **IFaqService**: CRUD category/item (cha-con) + bản dịch, dựng cây theo ngôn ngữ (fallback), (tuỳ chọn) FAQ event.
- **IMessagingService**: tạo hội thoại theo phòng, thêm/đọc tin, đếm unread, đóng hội thoại.
- **IHousekeepingService**: tạo ticket dọn phòng, chống trùng, chuyển trạng thái, hoàn tất qua app hoặc qua staff-scan token.
- **INoteService**: CRUD ghi chú nội bộ (không lộ cho guest).
- **II18nService**: nội dung DB theo ngôn ngữ + fallback + cờ `isFallback`.
- **IAuthService**: login, refresh, phân quyền Role.
- **IDashboardService**: thống kê vận hành.

### REST API — Guest (`/api/guest`, cookie phiên; rule gate ở backend)

| Method | Endpoint | Mô tả |
|---|---|---|
| GET | `/resolve/{token}` | Phân giải token → room, resort, ngôn ngữ, trạng thái ack, features. Tạo session/visit nếu chưa có. Từ chối nếu token/phòng không hợp lệ. |
| GET | `/rules?lang=vi` | Section nội quy đã dịch (published) + version + cấu hình đọc. |
| POST | `/rules/acknowledge` | `{ version, lang }` → ghi acknowledgement. |
| GET | `/faq?lang=vi` | Cây FAQ đã dịch (chỉ active). `403 rule_ack_required` nếu chưa ack. |
| GET | `/conversation` | Hội thoại của phiên hiện tại (dùng cho polling fallback). |
| POST | `/messages` | Gửi tin `{ body }` (rate-limited). `403` nếu chưa ack. |
| POST | `/housekeeping` | Tạo/nhắc yêu cầu dọn phòng cho phòng hiện tại (idempotent theo phòng). |
| GET | `/housekeeping` | Trạng thái ticket dọn phòng hiện tại của phòng. |

**Contract `/resolve` (trả rõ để frontend không phải đoán):**
```json
{
  "room": { "id": "...", "number": "A-203", "building": "A", "floor": "2" },
  "resort": { "id": "...", "name": "Star Hill", "logoUrl": "..." },
  "languages": ["en", "vi", "ko", "zh"],
  "defaultLanguage": "en",
  "visit": { "id": "...", "portalWindowExpiresAt": "2026-07-02T09:30:00Z" },
  "rules": { "currentVersion": 12, "acknowledged": false, "acknowledgedVersion": null },
  "features": {
    "faqEnabled": true, "chatEnabled": true, "housekeepingEnabled": true,
    "ruleAckRequiredForFaq": true, "ruleAckRequiredForChat": true, "ruleAckRequiredForHousekeeping": true
  }
}
```

**Quy tắc quan trọng:**
- `POST /rules/acknowledge`: **server tự xác định publication published hiện tại**, KHÔNG tin `version` client gửi (client gửi để đối chiếu, server là nguồn sự thật). Ack gắn với GuestVisit hiện tại.
- `POST /messages` và `POST /housekeeping`: hỗ trợ **idempotency** (client gửi `Idempotency-Key` hoặc server chống double-tap trong cửa sổ ngắn) để tránh nhân đôi khi bấm nhanh; housekeeping còn được chống trùng theo "1 ticket mở/phòng".

### REST API — Admin/Staff (`/api/admin`, JWT; phân quyền Role)

| Method | Endpoint | Quyền |
|---|---|---|
| POST | `/api/auth/login`, `/refresh`, `/logout`, GET `/me` | — |
| GET | `/rooms`, `/rooms/{id}` (xem) | Staff, Admin |
| POST/PUT/DELETE | `/rooms`, `/rooms/{id}` (tạo/sửa/xoá) | Admin |
| GET | `/rooms/{id}/qr.png` ; POST `/rooms/qr-labels.pdf` | Admin |
| POST | `/rooms/{id}/revoke-token` | Admin |
| GET/PUT | `/rules/draft` ; POST `/rules/publish` ; GET `/rules/publications` | Staff, Admin |
| GET | `/rules/preview?lang=` | Staff, Admin |
| GET/POST/PUT/DELETE | `/faq/categories`, `/faq/items` ; POST `/faq/reorder` | Staff, Admin |
| GET | `/conversations` (gom theo phòng, filter, unread) ; GET `/conversations/{id}` | Staff, Admin |
| POST | `/conversations/{id}/reply`, `/read`, `/close` | Staff, Admin |
| GET/POST/PUT/DELETE | `/notes` | Staff, Admin |
| GET | `/housekeeping` (board theo trạng thái) | Staff, Admin |
| POST | `/housekeeping/{id}/status` (`InProgress`/`Done`) | Staff, Admin |
| POST | `/housekeeping/complete-by-room` (chọn phòng + xác nhận: `{ roomId }`) | Staff, Admin |
| POST | `/housekeeping/complete-by-token` (lối tắt quét QR: `{ token }`) | Staff, Admin |
| POST | `/guest-visits/{id}/close` | Staff, Admin |
| GET/POST/PUT/DELETE | `/users` | Admin |
| GET/PUT | `/settings` (ResortSettings) | GET: Staff, Admin · PUT: Admin |
| GET | `/dashboard/stats` | Staff, Admin |

### SignalR Hub (`/hubs/chat`)

- Groups: `resort-{resortId}-staff` (lễ tân online), `conversation-{conversationId}` (hội thoại cụ thể).
- Server → Client: `MessageReceived`, `MessageRead`, `ConversationUpdated`, `HousekeepingUpdated`.
- Client → Server: `JoinConversation`, `LeaveConversation`.
- Backend kiểm tra quyền khi join (guest chỉ join hội thoại của chính session; không tin id client gửi).
- Fallback: guest polling `GET /conversation` mỗi ~15s khi WebSocket lỗi.

## Data Models

```
Resort
  Id, Name, Timezone, LogoUrl, CreatedAt
  -- ngôn ngữ mặc định KHÔNG lưu ở đây; xác định bởi ResortLanguage.IsDefault

ResortSettings
  ResortId (PK/FK, 1-1 với Resort),
  RequireRuleAckForFaq (bool), RequireRuleAckForChat (bool), RequireRuleAckForHousekeeping (bool),
  FaqEnabled (bool), ChatEnabled (bool), HousekeepingEnabled (bool),
  PortalWindowMinutes (int, default 30), VisitIdleExpiryHours (int, default 24),
  GuestWebBaseUrl (string, dùng dựng URL QR),
  MaxMessageLength (int), MessageRateLimitPerMinute (int), HousekeepingRateLimitPerHour (int),
  UpdatedAt
  -- thiếu giá trị nào → fallback hằng số/appsettings

ResortLanguage
  Id, ResortId, Code ("en"/"vi"/"ko"/"zh"), DisplayName,
  IsEnabled, IsDefault, SortOrder
  -- bất biến: đúng một bản ghi IsDefault=true cho mỗi resort (mặc định "en")

Room
  Id, ResortId, RoomNumber, Building, Floor,
  Status (enum: Active/Inactive/Maintenance), CreatedAt, UpdatedAt, DeletedAt?

RoomQrToken
  Id, RoomId, Token (random, indexed), TokenPreview,
  Status (enum: Active/Revoked), Version,
  CreatedAt, CreatedByUserId, RevokedAt?, RevokedByUserId?, RevocationReason?
  -- ràng buộc: mỗi Room chỉ 1 token Active

GuestSession
  Id (guid), SessionKey (cookie value, indexed),
  PreferredLanguage, FirstSeenAt, LastSeenAt
  -- định danh THIẾT BỊ, dài hạn; không gắn ResortId (single-resort)

GuestVisit
  Id, ResortId, RoomId, GuestSessionId,
  Status (enum: Active/Closed/Expired),
  StartedAt, LastSeenAt, ExpiresAt, ClosedAt?, ClosedByUserId?
  -- lượt lưu trú của phòng; nối lại nếu cùng thiết bị+phòng còn Active
  -- ExpiresAt = LastSeenAt + VisitIdleExpiryHours (idle hard-expiry, mặc định 24h)
  -- cửa sổ thao tác (PortalWindowMinutes, mặc định 30') suy ra từ **GuestVisit.LastSeenAt** (không lưu cột riêng)

RuleSet  (bản DRAFT đang soạn — mutable, chỉ nội bộ thấy)
  Id, ResortId, UpdatedAt, RowVersion (concurrency token)

RuleSection  (thuộc Draft RuleSet — admin sửa ở đây, KHÔNG phải nguồn khách đọc)
  Id, RuleSetId, Key, SortOrder, IsRequired, RequireScrollEnd, MinReadSeconds, RowVersion

RuleSectionTranslation  (Draft)
  Id, RuleSectionId, LanguageCode, Title, BodyHtmlSanitized, RowVersion
  (unique: RuleSectionId + LanguageCode)

RulePublication  (SNAPSHOT BẤT BIẾN mỗi lần publish — KHÁCH ĐỌC TỪ ĐÂY)
  Id, ResortId, Version (int), PublishedAt, PublishedByUserId, ChangeNote?, IsCurrent (bool)
  -- bất biến: đúng một RulePublication.IsCurrent=true / resort

RulePublicationSection  (bản sao đông cứng của section tại thời điểm publish)
  Id, RulePublicationId, Key, SortOrder, IsRequired, RequireScrollEnd, MinReadSeconds

RulePublicationSectionTranslation  (bản sao đông cứng nội dung theo ngôn ngữ)
  Id, RulePublicationSectionId, LanguageCode, Title, BodyHtmlSanitized
  (unique: RulePublicationSectionId + LanguageCode)

RuleAcknowledgement
  Id, ResortId, RoomId, GuestSessionId, GuestVisitId (non-null),
  RulePublicationId, Version, LanguageCode, AcceptedAt
  -- ack LUÔN gắn với GuestVisit; unique (GuestVisitId, RulePublicationId)
  -- bản ghi vận hành (KHÔNG lưu như chứng cứ pháp lý; không cần IpHash phức tạp)

FaqCategory
  Id, ResortId, Key, SortOrder, IsActive
FaqCategoryTranslation
  Id, FaqCategoryId, LanguageCode, Name
FaqItem
  Id, ResortId, CategoryId, ParentId? (self), SortOrder, IsActive
FaqItemTranslation
  Id, FaqItemId, LanguageCode, Question, AnswerHtmlSanitized
  (unique: FaqItemId + LanguageCode)

Conversation
  Id, ResortId, RoomId, GuestSessionId, GuestVisitId,
  Status (enum: Open/Closed),
  LastMessageAt, LastGuestMessageAt?, LastStaffMessageAt?,
  UnreadForStaff (int), CreatedAt, ClosedAt?, ClosedByUserId?
  -- scope theo GuestVisit: mỗi visit đúng MỘT conversation (reopen khi guest gửi lại sau khi bị đóng); dashboard gom hiển thị theo phòng
  -- (chừa cột cho Priority/AssignedToUserId nếu mở rộng sau; MVP không dùng)

Message
  Id, ConversationId, SenderType (enum: Guest/Staff/System),
  SenderUserId?, Body, CreatedAt, ReadByStaffAt?, ReadByGuestAt?

HousekeepingTicket
  Id, ResortId, RoomId, RequestedByGuestSessionId?, GuestVisitId?,
  Status (enum: Requested/InProgress/Done/Cancelled),
  CreatedAt, StartedAt?, CompletedAt?, CompletedByUserId?, CompletionMethod?
  -- CompletionMethod: App | StaffScan

HousekeepingEvent
  Id, HousekeepingTicketId, NewStatus, ActorType (Guest/Staff/System),
  ActorUserId?, Method? (App/StaffScan), CreatedAt
  -- nhật ký từng lần chuyển trạng thái để đối soát "ai làm gì, khi nào"

InternalNote
  Id, ResortId, RoomId?, ConversationId?, AuthorUserId, Body, CreatedAt, UpdatedAt?

AppUser
  Id, ResortId, Email (unique), DisplayName, PasswordHash,
  Role (enum: Admin/Staff), IsActive, LastLoginAt?, CreatedAt

RefreshToken
  Id, UserId, TokenHash, ExpiresAt, RevokedAt?, CreatedAt
```

### Quyết định dữ liệu

- **RoomQrToken tách bảng + lịch sử**: 1 token Active/phòng, thu hồi giữ lịch sử để biết QR đã in. **Token chỉ có Active/Revoked — không có ExpiresAt, không auto-expire**; chỉ đổi khi admin revoke thủ công.
- **Draft → Publish có SNAPSHOT**: admin sửa `RuleSection`/`RuleSectionTranslation` (Draft, mutable). Khi **Publish**, hệ thống **đóng băng** toàn bộ section+bản dịch vào `RulePublicationSection(+Translation)` của một `RulePublication` mới (Version++), đặt `IsCurrent=true` cho bản mới và `false` cho bản trước. **Khách luôn đọc từ publication `IsCurrent`**, nên sửa Draft không bao giờ lọt ra khách; preview/lịch sử đọc từ các publication cũ. Acknowledgement trỏ `RulePublicationId`.
- **Phiên (mặc định)**: `GuestVisit` idle expiry **24h** (hoặc lễ tân đóng); **PortalWindow 30'** là cửa sổ thao tác (quá hạn quét lại, nối lại visit). Hai mốc này khác nhau, đều tính từ `GuestVisit.LastSeenAt`.
- **HousekeepingTicket** có `CompletionMethod` (App/StaffScan) để phân biệt cách nhân viên hoàn tất.
- **Conversation gom theo phòng + UnreadForStaff**: đúng nhu cầu vận hành, MVP đơn giản.
- **Translation tách bảng**: query theo ngôn ngữ, phát hiện thiếu bản dịch.

## Luồng nghiệp vụ chi tiết

### Resolve QR + lượt lưu trú & cửa sổ thao tác

1. Guest mở `/r/{token}` → `GET /api/guest/resolve/{token}`.
2. Backend: token Active + phòng hợp lệ → trả room/resort/ngôn ngữ/trạng thái ack/features (features lấy từ `ResortSettings`).
3. Định danh thiết bị qua cookie `GuestSession` (tạo nếu chưa có, cookie dài hạn).
4. Xác định `GuestVisit`:
   - Nếu tồn tại GuestVisit `Active` cho (GuestSession + Room) → **nối lại** (cập nhật `LastSeenAt`, đẩy `ExpiresAt = now + VisitIdleExpiryHours`).
   - Ngược lại → tạo GuestVisit mới.
5. **Cửa sổ thao tác** (thứ tự quan trọng để không bao giờ "không expire được"):
   - Với **API tương tác của guest** (`/rules`, `/rules/acknowledge`, `/faq`, `/messages`, `/housekeeping`, `/conversation`): **kiểm tra `now - GuestVisit.LastSeenAt > PortalWindowMinutes` TRƯỚC**; nếu quá hạn → trả `session_expired` và **KHÔNG** cập nhật `LastSeenAt`. Nếu còn trong hạn → xử lý rồi mới cập nhật `LastSeenAt`.
   - Chỉ **`/resolve`** (khi khách quét lại) mới được phép **refresh cửa sổ** (đặt lại `LastSeenAt = now`) và nối lại visit nếu còn Active.
   - Lý do: nếu cập nhật `LastSeenAt` trước khi kiểm tra ở mọi request, cửa sổ sẽ tự gia hạn vô hạn và không bao giờ hết hạn.
6. Nếu token/phòng lỗi → trả `qr_invalid`/`qr_revoked`/`room_inactive`; guest web hiện trang lỗi thân thiện, không lộ phòng khác.
7. GuestVisit kết thúc khi lễ tân đóng (checkout) hoặc quá `ExpiresAt` (idle 24h). **Khi kết thúc: đóng hội thoại Open + huỷ ticket dọn phòng đang mở của visit đó (Cancelled); guest của visit cũ không post được nữa.** Lần quét sau tạo visit mới, dữ liệu lượt trước không hiển thị. `ExpiresAt` và cửa sổ thao tác đều tính từ **`GuestVisit.LastSeenAt`** (không dùng LastSeenAt của GuestSession).

### Auto-detect ngôn ngữ

```
lang = urlParam('lang')
     || localStorage.getItem('guestLang')
     || matchSupported(navigator.language, resort.languages)   // "ko-KR" → "ko"
     || resort.defaultLanguage
```

### Nội quy Draft → Publish + force-read

- Admin/Staff soạn Draft (`RuleSection`/translation, rich text đa ngôn ngữ), có **Preview như khách** (render từ Draft), rồi **Publish** → **snapshot** Draft thành `RulePublication` mới (Version++, `IsCurrent=true`, các bản trước `IsCurrent=false`).
- Guest **luôn đọc từ publication `IsCurrent`** (`RulePublicationSection(+Translation)`), không đọc Draft. Nếu chưa ack version `IsCurrent` trong lượt lưu trú hiện tại → RuleGate: đi từng section theo `SortOrder`; `RequireScrollEnd` dùng IntersectionObserver; `MinReadSeconds` đếm ngược nút "Tiếp tục"; progress bar; section cuối tick đồng ý → `POST /rules/acknowledge`.
- **Backend enforce** (khi ResortSettings bật): `GET /faq`, `POST /messages`, `POST /housekeeping` trả `403 rule_ack_required` nếu chưa ack. Không chỉ dựa vào frontend. `POST /rules/acknowledge` server tự xác định publication `IsCurrent`, không tin version client gửi.
- Sau ack, home mở khóa FAQ/chat/ticket. Đổi ngôn ngữ giữ tiến độ theo section id.

### FAQ (lễ tân tự soạn)

- Lễ tân tạo category + item cha-con qua editor (drag-drop, active/inactive, đa ngôn ngữ).
- Guest xem theo ngôn ngữ (fallback default, cờ `isFallback`), duyệt theo category hoặc theo flow câu hỏi con.
- FAQ item có CTA "Gửi tin nhắn về vấn đề này" → mở chat prefill: "Tôi cần hỗ trợ về: <câu hỏi>".

### Nhắn tin gom theo phòng

1. Guest gửi `POST /messages` → tạo Conversation cho phòng nếu chưa có, lưu Message (Guest), `UnreadForStaff++`.
2. Backend broadcast `MessageReceived`/`ConversationUpdated` tới `resort-{id}-staff`.
3. Dashboard hiện hội thoại **gom theo phòng**, badge unread. Lễ tân mở → `read` (reset unread) → reply.
4. Reply broadcast tới `conversation-{id}` → guest thấy realtime (hoặc khi quay lại, trong hạn phiên).
5. Rate limit nhẹ + giới hạn độ dài. Lễ tân có thể đóng hội thoại.
6. **Reopen (MVP)**: một GuestVisit dùng **một hội thoại duy nhất**. Nếu lễ tân đã đóng hội thoại (Status=Closed) mà guest **trong cùng visit còn Active** gửi tin tiếp → **mở lại (reopen) đúng hội thoại đó** (Status=Open), append tin, `UnreadForStaff++` — KHÔNG tạo hội thoại thứ hai. (Khác với trường hợp visit đã kết thúc: khi đó guest phải quét lại → visit mới → hội thoại mới.)

### Housekeeping (dọn phòng)

1. Khách bấm "Yêu cầu dọn phòng" → `POST /api/guest/housekeeping`. Nếu phòng đã có ticket mở → không tạo trùng, trả ticket hiện có. (Nhân viên cũng có thể chủ động tạo ticket cho một phòng.)
2. Ticket `Requested` hiện trên **HousekeepingBoard** của dashboard (theo phòng/trạng thái) + realtime `HousekeepingUpdated`.
3. Nhân viên hoàn tất, ưu tiên theo cách chính rồi tới lối tắt:
   - **Chính — chọn phòng + xác nhận**: `POST /housekeeping/{id}/status` (`InProgress`/`Done`) hoặc `POST /housekeeping/complete-by-room { roomId }` → `CompletionMethod=App`.
   - **Lối tắt — quét QR**: `POST /housekeeping/complete-by-token { token }` → resolve token ra phòng → hoàn tất ticket mở của đúng phòng → `CompletionMethod=StaffScan`.
   - Cả hai đều ghi `CompletedByUserId`, `CompletedAt`.
4. **Ghi nhật ký**: mỗi lần chuyển trạng thái tạo một `HousekeepingEvent` (ticket, trạng thái mới, ActorUserId, phương thức, thời điểm) để đối soát "ai đã làm gì, khi nào".
5. Khách xem được trạng thái ("đã tiếp nhận / đang làm / đã xong").

### Sinh QR & in

- `GET /rooms/{id}/qr.png`: QRCoder tạo PNG từ URL `https://<host>/r/{token}`.
- `POST /rooms/qr-labels.pdf`: danh sách phòng → PDF nhãn (số phòng + logo + text hướng dẫn). Không in URL token dài.
- `POST /rooms/{id}/revoke-token`: sinh token mới, token cũ `Revoked` (giữ lịch sử).

## Information Architecture & Wireframe (giải thích + đề xuất)

> **IA (Information Architecture)** = cách tổ chức thông tin và điều hướng: có những màn hình nào, sắp theo menu ra sao, cái gì nằm ở đâu để người dùng tìm được nhanh.
> **Wireframe** = bản phác khung màn hình (hộp và chữ), chỉ ra bố cục và vị trí các thành phần, chưa quan tâm màu mè/hình ảnh. Dưới đây là đề xuất dạng text để bạn hình dung.

### Guest Web — các màn hình & bố cục

Màn hình: ResolveLoading → (TokenError nếu lỗi) → RuleGate (nếu chưa ack) → GuestHome → {RulesViewer, FaqFlow, Chat}.

GuestHome (sau khi đọc nội quy):
```
┌───────────────────────────────┐
│ Star Hill Resort      [ VI ▾ ] │
│ Phòng A-203                    │
├───────────────────────────────┤
│ [  Nội quy  ] [   FAQ   ]      │
│ [ Nhắn lễ tân ] [ Dọn phòng ]  │
├───────────────────────────────┤
│ Yêu cầu nhanh:                 │
│  • Wifi   • Ăn sáng            │
│  • Dọn phòng • Xe điện         │
├───────────────────────────────┤
│ Trạng thái dọn phòng: Đã nhận  │
│ Tin gần nhất: Lễ tân đã trả lời│
└───────────────────────────────┘
```

RuleGate:
```
┌───────────────────────────────┐
│ Nội quy 2/6         [====   ]  │
│ Tiêu đề section                │
│ ...nội dung, cuộn tới cuối...  │
│                     (đáy nội dung) │
│ [ Tiếp tục (3s) ]  ← đếm ngược │
└───────────────────────────────┘
... section cuối:
│ [x] Tôi đã đọc và đồng ý        │
│ [ Xác nhận ]                    │
```

### Admin Dashboard — IA (menu theo công việc)

```
Staff:  Dashboard | Inbox | Housekeeping | Rules | FAQ | Notes | (Rooms xem)
Admin:  + Rooms & QR | Users | Settings
```

Dashboard tổng quan (operational, không cần hero marketing):
```
┌──────────────────────────────────────────────┐
│ [Chưa đọc: 8] [Đang mở: 14] [Dọn phòng: 5]    │
│ [Ack nội quy hôm nay: 42] [Phòng active: 86]  │
├──────────────────────────────────────────────┤
│ Cần xử lý:                                     │
│  A-203  3' trước  "Wifi không vào được"        │
│  B-110  7' trước  yêu cầu dọn phòng            │
└──────────────────────────────────────────────┘
```

Inbox (gom theo phòng):
```
┌───────────┬───────────────────────┬──────────────┐
│ Phòng      │ Hội thoại A-203        │ Ngữ cảnh phòng│
│ [tìm/lọc]  │ header phòng + ngôn ngữ│ thông tin phòng│
│ A-203 (2)  │ ...tin nhắn...         │ trạng thái phiên│
│ B-110 (1)  │ [ ô trả lời        ]   │ ghi chú nội bộ │
│ C-305      │                        │ ticket dọn phòng│
└───────────┴───────────────────────┴──────────────┘
```
MVP có thể làm 2 cột (list phòng + thread) trước, cột ngữ cảnh thêm sau.

HousekeepingBoard:
```
Requested        InProgress        Done (hôm nay)
A-203            B-110             A-101
C-305                              A-102
[Bắt đầu]        [Hoàn tất]        + nút "Quét QR để hoàn tất"
```

## Error Handling

- Chuẩn hoá lỗi bằng `ProblemDetails` kèm `code` ổn định cho guest: `qr_invalid`, `qr_revoked`, `room_inactive`, `rule_ack_required`, `rate_limited`, `message_too_long`, `session_expired`, `language_not_supported`.
- `session_expired`: guest-web hiển thị "Phiên đã hết hạn, vui lòng quét QR lại".
- Bản dịch thiếu → fallback default + `isFallback: true`.
- Realtime mất kết nối → guest polling; hiển thị trạng thái kết nối.
- Auth: 401 → admin-web thử refresh rồi về login; 403 → sai quyền Role.

## Security (phù hợp mạng nội bộ)

- **Cách ly mạng là yêu cầu hạ tầng, không phải cơ chế của app**: việc "chỉ truy cập nội bộ" do mạng resort (VLAN/tường lửa) bảo đảm; app không tự enforce. Rủi ro còn lại (thiết bị khách cũ vào lại WiFi) được giảm bằng GuestVisit + lễ tân đóng phiên khi checkout. Không tuyên bố "tuyệt đối an toàn".
- Vẫn giữ lớp cơ bản:
  - Đăng nhập Admin/Staff: access token (memory) + refresh token (cookie HttpOnly), rotation, logout revoke.
  - Guest: cookie `GuestSession` HttpOnly định danh thiết bị; không lưu số phòng/token trong cookie.
  - Token QR: random không đoán được (MVP không cần hash phức tạp).
  - Sanitize HTML nội dung nội quy/FAQ (allowlist thẻ; cấm script/iframe/onclick) trước khi lưu và trước khi trả về.
  - Rate limit nhẹ (ngưỡng lấy từ ResortSettings): resolve token, gửi tin nhắn, tạo ticket.
  - Không log full QR token / mật khẩu / refresh token.
- **Pháp lý**: acknowledgement chỉ là bản ghi vận hành cho biết khách đã bấm đồng ý; UI/tài liệu KHÔNG tuyên bố là chứng cứ pháp lý.

## Internationalization

- **Guest web**: mặc định **tiếng Anh (`en`)**, hỗ trợ đa ngôn ngữ, auto-detect theo thiết bị (Req 2). Ngôn ngữ mặc định nội dung = `ResortLanguage` có `IsDefault=true` (mặc định `en`) — **nguồn sự thật duy nhất** (không có field `Resort.DefaultLanguage`).
- **Admin dashboard**: giao diện **tiếng Việt (`vi`)** (đối tượng dùng là nhân viên/lễ tân Việt Nam).
- Text giao diện: vue-i18n JSON theo ngôn ngữ ở mỗi SPA (guest có nhiều ngôn ngữ; admin chỉ cần `vi`, có thể thêm sau).
- Nội dung (nội quy/FAQ): lấy từ DB theo `LanguageCode`, fallback về ngôn ngữ mặc định (`IsDefault`), kèm cờ `isFallback`.
- Admin editor: chỉ báo ngôn ngữ thiếu bản dịch; filter "show missing / show inactive".

## Observability (nhẹ)

- Log có cấu trúc: request id, room id (khi resolve), conversation id, ticket id, error code — đủ để dev debug.
- **Không log path chứa token** (mask `/r/{token}`), không log mật khẩu/refresh token.
- Health check: `/health/live`, `/health/ready` (ready kiểm tra DB).
- Background service **chỉ dọn `GuestVisit` hết hạn (idle)** — chuyển `Active`→`Expired`. **KHÔNG bao giờ auto-expire/vô hiệu `RoomQrToken`**: QR dán vật lý chỉ đổi trạng thái khi admin revoke thủ công (tránh khách quét mã trong phòng bị lỗi hàng loạt).
- **Không** yêu cầu backup/restore tự động ở giai đoạn này (quyết định D7).

## Deployment (mạng nội bộ + HTTPS)

- Chạy sau reverse proxy (Nginx/Caddy/IIS): `/` → guest-web tĩnh, `/admin` → admin-web tĩnh, `/api` → API, `/hubs` → SignalR. MVP nên **cùng origin** (ví dụ `https://portal.starhill.local`).
- **Bắt buộc HTTPS với cert hợp lệ**: camera của trình duyệt (`getUserMedia`, dùng cho html5-qrcode ở StaffScan) và nhiều API web chỉ chạy trong **secure context**. Trên `http://` hoặc cert self-signed, điện thoại sẽ cảnh báo/chặn.
  - Dùng **domain nội bộ thật + cert hợp lệ** (cert công khai qua DNS nội bộ, hoặc internal CA đã cài root vào thiết bị). Tránh self-signed lẻ.
  - **DNS nội bộ** trỏ domain về server resort; QR chứa URL theo `ResortSettings.GuestWebBaseUrl`.
- HSTS bật khi đã ổn định cert.

## Ràng buộc dữ liệu (DB constraints)

Enforce ở tầng DB (không chỉ ở code) bằng partial/filtered unique index:
- **1 QR token Active/phòng**: unique index trên `RoomQrToken(RoomId)` WHERE `Status='Active'`.
- **1 hội thoại/GuestVisit**: unique index trên `Conversation(GuestVisitId)` (tổng thể — do dùng mô hình reopen, mỗi visit chỉ một hội thoại; reopen đổi Status chứ không tạo mới).
- **1 ticket dọn phòng mở/phòng**: unique index trên `HousekeepingTicket(RoomId)` WHERE `Status IN ('Requested','InProgress')`.
- **1 acknowledgement/(visit, publication)**: unique index trên `RuleAcknowledgement(GuestVisitId, RulePublicationId)`.
- **Bản dịch duy nhất**: unique `(RuleSectionId, LanguageCode)`, `(FaqItemId, LanguageCode)`, `(FaqCategoryId, LanguageCode)`, `(ResortLanguage: ResortId, Code)`.
- **Đúng một ngôn ngữ mặc định**: unique index trên `ResortLanguage(ResortId)` WHERE `IsDefault=true`.
- **Đúng một publication hiện hành**: unique index trên `RulePublication(ResortId)` WHERE `IsCurrent=true`.

## Optimistic concurrency

- `RuleSection`/`RuleSectionTranslation`, `FaqItem`/`FaqItemTranslation`, `ResortSettings` có **concurrency token** (PostgreSQL `xmin` hoặc cột `RowVersion`).
- Khi hai lễ tân sửa cùng nội dung, người lưu sau nhận `409 Conflict` (concurrency) thay vì ghi đè âm thầm; UI hiển thị "nội dung đã đổi, tải lại".

## Testing Strategy

- **Unit**: sinh token & 1-active-token/phòng; fallback ngôn ngữ; rule gate & versioning publish; dựng cây FAQ; đếm unread; TTL/hết hạn GuestVisit; chống trùng housekeeping ticket; hoàn tất qua StaffScan.
- **Integration (API + EF)**: resolve hợp lệ / token revoked không lộ phòng; chưa ack bị 403 ở FAQ/messages/housekeeping; ack version hiện tại; publish version mới bắt ack lại; Staff không gọi được endpoint Admin; note không xuất hiện ở guest API; complete-by-token đánh dấu đúng phòng.
- **SignalR**: gửi/nhận message, join group đúng quyền, HousekeepingUpdated, fallback polling.
- **Frontend**: auto-detect ngôn ngữ; điều kiện enable nút "Tiếp tục" (scroll-end + đếm ngược); FAQ tree; luồng housekeeping.
- **E2E (tuỳ chọn)**: guest scan → rules → ack → FAQ → chat → tạo ticket; admin login → room/QR → publish rule → inbox reply → StaffScan hoàn tất.

> Chỉ viết test ở các subtask có yêu cầu trong tasks.md để giữ đúng phạm vi.

## Correctness Properties

### Property 1: Phân giải token an toàn
Mỗi token Active map đúng một phòng hợp lệ; token revoked/phòng vô hiệu không resolve ra dữ liệu phòng. URL QR không chứa số phòng dạng trần.
**Validates: Requirements 1.1, 1.3, 1.4, 1.6, 7.4**

### Property 2: QR token — một active mỗi phòng & không tự hết hạn
Tại mọi thời điểm mỗi phòng có tối đa một `RoomQrToken` Active; thu hồi giữ lịch sử, không xoá cứng. Token **không có cơ chế auto-expire** — chỉ chuyển Revoked khi admin thao tác thủ công.
**Validates: Requirements 1.5, 7.4, 7.5**

### Property 3: Cổng nội quy enforce ở backend
Guest chưa ack version published hiện tại KHÔNG lấy được FAQ, KHÔNG gửi tin nhắn, KHÔNG tạo ticket (khi cấu hình yêu cầu) — enforced ở backend.
**Validates: Requirements 3.2, 3.11, 5.1, 6.1**

### Property 4: Draft không ảnh hưởng khách (đảm bảo bằng snapshot)
Khách luôn đọc từ `RulePublication` có `IsCurrent=true` (snapshot đông cứng). Sửa `RuleSection` Draft không thay đổi nội dung khách thấy cho tới khi Publish tạo publication mới (Version++, `IsCurrent` chuyển sang bản mới) và bắt ack lại.
**Validates: Requirements 3.9, 8.1, 8.3**

### Property 5: Toàn vẹn bản dịch/fallback
Nội dung hiển thị cho khách luôn có giá trị; thiếu bản dịch → trả default + `isFallback`, không chuỗi rỗng.
**Validates: Requirements 2.2, 2.5, 8.7**

### Property 6: Cô lập lượt lưu trú & hội thoại
Hội thoại/ticket gắn đúng phòng + GuestVisit; khách chỉ thấy dữ liệu của **lượt lưu trú của chính mình**; khách mới (visit mới) không thấy tin nhắn lượt trước; không join được SignalR conversation của phiên khác.
**Validates: Requirements 5.2, 5.9, 10.1, 11.2**

### Property 7: Ghi chú nội bộ riêng tư
InternalNote không bao giờ xuất hiện trong response tới guest.
**Validates: Requirements 9.4**

### Property 8: Phân quyền
Staff không gọi thành công endpoint chỉ dành cho Admin (rooms/QR/users/settings); guest không gọi được endpoint admin.
**Validates: Requirements 11.1, 11.3**

### Property 9: Nối lại visit, cửa sổ thao tác & cascade khi kết thúc
Quét lại cùng thiết bị+phòng khi visit còn Active thì nối lại đúng visit (giữ chat/ack); quá cửa sổ thao tác 30' phải quét lại để tiếp tục; khi visit kết thúc (lễ tân đóng hoặc idle 24h) thì **đóng hội thoại Open + huỷ ticket mở của visit**, guest cũ không post được, và quét lại tạo visit mới.
**Validates: Requirements 10.2, 10.3, 10.4, 10.5, 10.8**

### Property 10: Housekeeping không trùng & hoàn tất đúng phòng
Phòng đang có ticket mở thì không tạo ticket trùng; hoàn tất bằng chọn phòng (`complete-by-room`) hoặc lối tắt quét QR (`complete-by-token`) chỉ tác động đúng phòng; mỗi lần chuyển trạng thái ghi một `HousekeepingEvent` (ai/lúc nào/phương thức).
**Validates: Requirements 6.2, 6.4, 6.5, 6.8**

### Property 11: Đơn điệu của unread
`Conversation.UnreadForStaff` tăng khi khách gửi tin, về 0 khi lễ tân đọc; không âm.
**Validates: Requirements 5.3, 5.4**

### Property 12: An toàn nội dung
Nội dung HTML nội quy/FAQ do nội bộ nhập luôn được sanitize trước khi lưu/hiển thị (không còn script thực thi được).
**Validates: Requirements 8.6, 11.4**

### Property 13: Acknowledge do server xác thực
`POST /rules/acknowledge` luôn gắn với publication published hiện tại do **server** xác định; version do client gửi không được tin dùng để quyết định.
**Validates: Requirements 3.7, 3.9**

### Property 14: Đúng một ngôn ngữ mặc định
Mỗi resort có đúng một `ResortLanguage.IsDefault=true`; mọi fallback nội dung dùng ngôn ngữ này (mặc định `en`).
**Validates: Requirements 2.1, 2.2**

### Property 15: Chống ghi đè đồng thời nội dung
Sửa đồng thời một mục nội quy/FAQ/settings bởi hai người dùng: người lưu sau nhận 409 (optimistic concurrency), không ghi đè âm thầm.
**Validates: Requirements 8.1, 8.5**
