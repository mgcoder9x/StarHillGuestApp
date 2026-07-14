# Design — StarHill QR trên nền Bedrock (`starhill/`)

> **Vai trò tài liệu**: đây là **HOW-on-Bedrock** — bản đồ đưa sản phẩm Resort QR Portal (WHAT ở
> `docs/resort-qr-portal/requirements.md` + design/tasks/CP) lên nền Bedrock vật lý duy nhất tại `platform/src/`,
> được product tree `starhill/` tham chiếu trực tiếp theo D1-a (QR-AD-012). Đây là artifact design-first;
> `resort-qr/` chỉ là nguồn legacy để port có chọn lọc, không phải runtime tree hiện hành.
>
> **Đọc kèm**: `README.md` (authority map), `journal/01-decisions.md` (QR-AD), `journal/03-tradeoffs.md` (QR-TO).
> Mọi quyết định tự-ra trong tài liệu này được ghi song song vào journal với Provenance/Evidence.

---

## Overview

StarHill QR đưa sản phẩm Resort QR Portal (WHAT ở `docs/resort-qr-portal/`) lên modular-monolith Bedrock. Bedrock chỉ có **một nguồn vật lý** tại `platform/src`; `starhill/` chỉ chứa Host/module/test nghiệp vụ và ProjectReference trực tiếp base qua `$(PlatformSrc)` (QR-AD-012). Nghiệp vụ phân rã thành **8 module 5-project** + Dashboard ghép ở Host, mỗi module một schema Postgres và keyed persistence. Identity/ResortConfig/Rooms đã được port; GuestAccess là module kế tiếp; Rules/Faq/Concierge/Housekeeping dựng mới.

## 0. Trạng thái nguồn (đã verify trên đĩa, KHÔNG suy đoán)

| Nguồn | Thực trạng đã kiểm | Ý nghĩa cho pha này |
|---|---|---|
| `platform/src/` | Bedrock domain-agnostic, nguồn vật lý duy nhất; base test ở `platform/tests`. | Sửa năng lực nền ở đây; không nhồi nghiệp vụ StarHill. |
| `starhill/` | Product tree hiện hành: Host + module `{Identity,ResortConfig,Rooms}` + test nghiệp vụ; ProjectReference trực tiếp `platform/src` theo D1-a. | GuestAccess chưa tồn tại và phải port theo design module riêng. |
| `resort-qr/` (bản cũ) | Monolith trên `ResortQr.SharedKernel`. **Đã làm tới wave 4**: Domain `{Identity, Resorts, Rooms, GuestAccess}`; Application `{Identity(login/refresh/logout), Rooms(CRUD+token+QR PNG), GuestAccess(resolve), Localization}`; Infra `{AppDbContext đơn, QrCoderQrService, Argon2/JWT/HtmlSanitizer/Sha256 hashers, ResortSeeder, migration InitialCreate}`. **CHƯA có** Rules/Faq/Messaging/Housekeeping/Dashboard (task 6–19 còn `[ ]`). | Port được: Identity, Resorts, Rooms, GuestAccess, Localization, QR render, các adapter security. Phần còn lại **dựng mới** trên Bedrock (bản cũ chưa có). |
| Bedrock ports sẵn có | `IClock`(Time), `ICurrentUser`(Users), `IRepository`/`IUnitOfWork`(Persistence), `IJwtTokenService`/`IPasswordHasher`/`IRefreshTokenStore`/`ITokenGenerator`(Security), `IHtmlSanitizer`(Html), Caching(`IIdempotencyStore`/`IAppCache`), Email/Search/Storage/ExternalAuth. | QR **dùng lại port**, không tự tạo hạ tầng trùng (token gen, Argon2, JWT, refresh-store, sanitize, idempotency đã có ở base). |

**Hệ quả then chốt**: bản cũ `resort-qr/` **tự cuộn** SharedKernel + hạ tầng (Argon2, JWT, token-gen, sanitize, refresh-store, UoW, repo, DbContext). Base Bedrock **đã có tất cả những thứ đó** ở dạng port + adapter. → Port nghiệp vụ **bỏ lớp hạ tầng tự cuộn**, nối vào port Bedrock. Đây là giá trị lớn nhất của việc dựng lại trên base.

---

## Architecture

Kiến trúc = Bedrock modular-monolith (ràng buộc kế thừa §1) + phân rã module QR (§2) + chiến lược dữ liệu chéo-module (§3) + reconcile route (§5) + Dashboard-ở-Host (§6). Sơ đồ tổng thể sản phẩm xem `docs/resort-qr-portal/design.md` §Architecture; phần dưới đây đặc tả phần **on-Bedrock**.

## 1. Ràng buộc kế thừa từ Bedrock (bất biến — không phá khi làm QR)

Từ đọc mã `starhill/`:

1. **Module = 5 project** (khuôn `Modules/Identity`): `<M>.Domain`, `<M>.Application`, `<M>.Contracts`, `<M>.Infrastructure`, `<M>.Api`. Api ⊥ Infrastructure (I7 — Api không ref Infra; Host ghép cả hai qua `Add<M>Infrastructure` + `Add<M>Api`).
2. **Data ownership per-module** (F31/I6): mỗi module có **DbContext riêng** kế thừa `PlatformDbContext` + **schema Postgres riêng** (vd `identity`). `PlatformDbContext` cấp cơ chế generic: snake_case, soft-delete filter, audit tự động, concurrency `xmin` (chỉ Npgsql), dispatch domain-event **cùng transaction** SaveChanges (CP14). **Lõi Bedrock KHÔNG biết entity nghiệp vụ** (CP1/F3/F4) — nghiệp vụ QR nằm trong Modules.
3. **Một DB vật lý, nhiều schema**: mỗi module nhận connection string riêng ở Host (`AddXxxInfrastructure(UseNpgsql(cs))`) — trỏ **cùng một PostgreSQL**, khác **schema**. Cho phép migration độc lập/module.
4. **Cross-module KHÔNG chia bảng, KHÔNG FK chéo schema**: tham chiếu bằng **Id trần** (vd `Conversation.RoomId : Guid` không FK sang schema `rooms`). Nhất quán chéo-module bằng: (a) kiểm ở tầng Application trong cùng Host, hoặc (b) integration event (outbox/inbox) — xem QR-TO-002.
5. **Endpoint qua `IEndpointModule`** + versioned group `MapVersionedGroup("/x", V1)` → route thật `/v1/x/...`. Host tự discovery `IEnumerable<IEndpointModule>`.
6. **Result → HTTP** chỉ qua `ProblemDetailsBuilder` (nguồn shape lỗi DUY NHẤT). Lỗi domain là `Error`/`ErrorType`.
7. **Pipeline behaviors** (Validation/Authorization/Idempotency/Logging/Transaction) bọc use case qua `AddBedrockCore` SAU khi module đăng ký use case (Scrutor decorate).
8. **Posture port an toàn**: Host luôn gọi `AddXxxCore` cho mọi port (default degrade/fail-loud); adapter thật Override.
9. **Messaging event-driven opt-in** (`Bedrock:Messaging:Enabled`) + **migrate-on-startup opt-in** (`Bedrock:ApplyMigrationsOnStartup`) — production migrate out-of-band (AD-050).

> **CP1 no-business-in-core vẫn hiệu lực trong `starhill/`**: guard `NoBusinessInCore*Tests` chỉ quét assembly `Bedrock.*`; module QR nằm ngoài nên không bị quét, NHƯNG tuyệt đối không nhồi entity QR vào `Bedrock.*`.

---

## 2. Phân rã module (QR domain → Bedrock module)

Design sản phẩm liệt kê 11 nhóm (Identity, Resorts, Rooms, QrTokens, GuestAccess, Rules, Faq, Messaging, Housekeeping, Notes, Dashboard). Áp nguyên tắc **cohesion cao + tránh phân mảnh thừa (I10)**, gom thành **8 module** + Dashboard ghép ở Host:

| # | Module (`starhill/src/Modules/<M>`) | Schema | Gồm domain QR | Nguồn port |
|---|---|---|---|---|
| 1 | **Identity** | `identity` | AppUser (Role Admin/Staff), auth login/refresh/logout/me | `resort-qr` Identity + Bedrock Identity mẫu |
| 2 | **ResortConfig** | `resort_config` | Resort, ResortSettings, ResortLanguage, **i18n translation resolver** (nền cho Rules/Faq) | `resort-qr` Resorts + Localization |
| 3 | **Rooms** | `rooms` | Room, RoomQrToken (QrTokens gộp vào — 1-1 chặt), QR PNG + PDF nhãn | `resort-qr` Rooms + QrCoderQrService |
| 4 | **GuestAccess** | `guest_access` | GuestSession, GuestVisit, resolve token, portal window, cascade khi đóng visit | `resort-qr` GuestAccess + ResolveTokenUseCase |
| 5 | **Rules** | `rules` | RuleSet/Section/Translation (Draft), RulePublication* (snapshot), RuleAcknowledgement, rule-gate | **DỰNG MỚI** (bản cũ chưa có) |
| 6 | **Faq** | `faq` | FaqCategory/Item + Translation (cha-con), FaqEvent (tuỳ chọn) | **DỰNG MỚI** |
| 7 | **Concierge** | `concierge` | Conversation, Message, InternalNote, SignalR ChatHub | **DỰNG MỚI** |
| 8 | **Housekeeping** | `housekeeping` | HousekeepingTicket, HousekeepingEvent, complete-by-room/token | **DỰNG MỚI** |
|  | **Dashboard** (không phải module) | — | Thống kê vận hành gộp từ nhiều module | ghép ở Host (xem §6) |

### Vì sao gộp như vậy (lý do chính xác, không gold-plate)

- **QrTokens → gộp Rooms**: RoomQrToken 1-1 với Room, mọi vòng đời token (issue/rotate/revoke) đi cùng Room. Tách module riêng chỉ thêm ranh giới không giá trị.
- **Localization/i18n → gộp ResortConfig**: `ResortLanguage.IsDefault` là **nguồn sự thật ngôn ngữ mặc định** (CP14) và translation resolver phụ thuộc danh sách ngôn ngữ của resort. Đặt cùng ResortConfig để giữ ngôn ngữ + fallback một chỗ; Rules/Faq **dùng** resolver này qua `ResortConfig.Contracts`.
- **Notes → gộp Concierge**: InternalNote gắn phòng/hội thoại, dùng chung màn Inbox với hội thoại; cùng ngữ cảnh vận hành lễ tân.
- **Messaging đặt tên "Concierge"** (QR-AD-004): tránh **đụng tên** `Bedrock.Messaging.Contracts` (integration-event/outbox của base). "Concierge" = nhắn tin khách↔lễ tân (nghiệp vụ), phân biệt rõ với "Messaging" = bus sự kiện nội bộ (hạ tầng). Chống nhầm lẫn đọc mã lâu dài.
- **Dashboard KHÔNG là module**: nó **đọc chéo** nhiều module (đếm unread/ticket/room/ack). Làm module riêng sẽ buộc ref chéo domain (phá cô lập). Thay vào đó **ghép ở Host** `StarHill.Api`: mỗi module lộ **query đếm** qua `<M>.Contracts`; Host tổng hợp. Xem §6 + QR-TO-003.

---

## 3. Chiến lược dữ liệu chéo-module (quyết định cốt lõi — QR-AD-002 / QR-TO-002)

Nhiều entity QR tham chiếu chéo domain: `GuestVisit(RoomId)`, `RuleAcknowledgement(RoomId, GuestVisitId, RulePublicationId)`, `Conversation(RoomId, GuestVisitId)`, `HousekeepingTicket(RoomId, GuestVisitId)`. Bedrock cấm FK chéo-schema (mỗi module một schema).

**Quyết định**: tham chiếu chéo-module bằng **Id trần (Guid), KHÔNG FK chéo schema**. Nhất quán bảo đảm bằng **kiểm tra tầng Application trong cùng Host** (single-process modular monolith — không phải microservice, gọi qua **query port** của module đích để verify tồn tại/hợp lệ). Các **partial unique index** vẫn đặt được **trong schema của module sở hữu** (đều là một PostgreSQL vật lý):

| Ràng buộc (design §DB constraints) | Đặt ở schema/module | Loại |
|---|---|---|
| 1 token Active/phòng | `rooms` (Rooms) | partial unique `RoomQrToken(RoomId) WHERE Status='Active'` |
| 1 hội thoại/GuestVisit | `concierge` (Concierge) | unique `Conversation(GuestVisitId)` |
| 1 ticket mở/phòng | `housekeeping` | partial unique `HousekeepingTicket(RoomId) WHERE Status IN (...)` |
| 1 ack/(visit,publication) | `rules` | unique `RuleAcknowledgement(GuestVisitId, RulePublicationId)` |
| 1 ngôn ngữ mặc định/resort | `resort_config` | partial unique `ResortLanguage(ResortId) WHERE IsDefault` |
| 1 publication IsCurrent/resort | `rules` | partial unique `RulePublication(ResortId) WHERE IsCurrent` |
| unique translation (entity+lang) | schema của entity | unique composite |

**Cascade khi kết thúc GuestVisit** (Req 10.8) span ba owner/schema. Thiết kế cũ QR-AD-002 chọn gọi đồng bộ trong Host với giả định “một transaction/scope”; giả định đó không đúng với cấu hình hiện tại: mỗi module có DbContext/key riêng và một DI scope không tạo shared transaction.

**Quyết định cập nhật (QR-AD-027, supersede chỉ phần cascade QR-AD-002):** transaction GuestAccess chuyển visit Closed/Expired và ghi `GuestVisitEndedIntegrationEvent` vào outbox cùng commit. Concierge/Housekeeping consume qua inbox/idempotent handler để đóng conversation/huỷ ticket. Delivery là **at-least-once**, cleanup eventual; guest write vẫn bị chặn tức thời bằng authoritative GuestVisit status. Initial GuestAccess resolve chưa map outbox cho tới khi consumer thật tồn tại. Xem `design-modules/03-guestaccess.md` §9 và QR-TO-006.

---

## Components and Interfaces

## 4. Bản đồ chi tiết từng module

Ký hiệu: **[P]** = port từ `resort-qr/` (sửa SharedKernel→Bedrock); **[N]** = dựng mới; ports Bedrock in đậm.

### 4.1 Identity (`identity`)
- **Entity/domain**: AppUser (Email, DisplayName, PasswordHash, Role: Admin/Staff, IsActive, LastLoginAt) **[P]**. RefreshToken dùng **`IRefreshTokenStore`** của Bedrock (đã có bảng refresh_token opt-in trong schema module).
- **Use case**: Login **[P]**, RefreshToken (Bedrock mẫu đã có) **[P]**, Logout **[P]**, Me (query).
- **Ports Bedrock**: **`IPasswordHasher`** (Argon2id — bỏ Argon2 tự cuộn của resort-qr), **`IJwtTokenService`**, **`IRefreshTokenStore`**, **`ITokenGenerator`**, **`ICurrentUser`**, **`IClock`**.
- **Role → policy**: Bedrock dùng permission-based (`RequirePermissionAttribute`/policy). Map: Role Admin → policy `RequireAdmin`; Staff → `RequireStaff` (Admin là superset). Ghi QR-AD-005 (role→policy adapter).
- **Endpoint**: `/v1/auth/login|refresh|logout|me` (product path `/api/auth/*` — reconcile ở §5, QR-DV-001).

### 4.2 ResortConfig (`resort_config`)
- **Entity [P]**: Resort, ResortSettings (1-1, concurrency token), ResortLanguage (unique default). i18n: `ITranslationResolver`/`Translated<T>` **[P]** (từ resort-qr Localization).
- **Use case [P/N]**: GetResolveConfig (features/languages/defaultLanguage cho `/resolve`), Get/Update ResortSettings (Admin PUT, Staff GET), quản lý ResortLanguage.
- **Ports**: **`IClock`**, **`ICurrentUser`**, `IUnitOfWork`/`IRepository`.
- **Contracts lộ ra**: `IResortSettingsQuery` (GuestAccess/Rules/Faq/Concierge/Housekeeping đọc feature flags + PortalWindowMinutes/VisitIdleExpiryHours + rate limits), `ITranslationResolver` (Rules/Faq dùng).

### 4.3 Rooms (`rooms`)
- **Entity [P]**: Room (Status Active/Inactive/Maintenance, soft-delete), RoomQrToken (Active/Revoked, 1 active/phòng, giữ lịch sử — KHÔNG auto-expire).
- **Use case [P]**: Create/Update/ChangeStatus/Delete room, issue token (khi tạo), Rotate token (atomic), RenderRoomQrPng. **[N]**: PDF nhãn nhiều phòng (QuestPDF — bản cũ HOÃN; xem §8 license).
- **Ports**: **`ITokenGenerator`** (CSPRNG — bỏ CryptoTokenGenerator tự cuộn), **`IClock`**, **`ICurrentUser`**. QR PNG: QrCoder (adapter Infra module). URL QR = `ResortSettings.GuestWebBaseUrl` (đọc qua ResortConfig.Contracts).
- **Contracts lộ ra**: `IRoomQuery` (resolve token→room cho GuestAccess & Housekeeping complete-by-token; verify RoomId tồn tại/Active).
- **Endpoint**: admin `/v1/rooms` (GET Staff+Admin; CUD + qr.png + qr-labels.pdf + revoke-token Admin-only).

### 4.4 GuestAccess (`guest_access`) — C-GA.1..3 ĐÃ TRIỂN KHAI
- Thiết kế chi tiết/có hiệu lực: **`design-modules/03-guestaccess.md`** (baseline + C-GA.3a reconciliation).
- **Entity**: GuestSession (hash cookie thiết bị, không fingerprint), GuestVisit (Active/Closed/Expired;
  portal/idle tách ngữ nghĩa); không FK chéo schema.
- **Resolve**: cross-module qua `IRoomTokenResolver` + `IResortGuestConfigQuery`; `IUseCase` tự mở transaction
  `guest_access` hẹp chỉ quanh session/visit; session `FOR UPDATE` loại race; lazy idle-expiry.
- **API**: POST-body public + `__Host-` cookie; canonical token/cookie guard trước resolver/hash; body 1 KiB;
  no-store/no-secret-log; Bedrock global IP limiter. Named limiter defer tới khi có SLO/shared-NAT budget (QR-TO-007).
- **Persistence deploy**: schema/key/history ledger per-module; Host/compose/CI migration bundle đã wiring (QR-AD-028).
- **Sau resolve [N]**: current guest context check-before-touch, sweeper `SKIP LOCKED`, staff close. Cascade chỉ thêm
  khi Concierge/Housekeeping tồn tại: outbox/inbox at-least-once (QR-AD-027), không synchronous multi-DbContext giả-atomic.
- Physical Guest Web route vẫn `/r/{token}`; frontend scrub URL rồi gọi `POST /v1/guest/resolve`.

### 4.5 Rules (`rules`) — DỰNG MỚI
- **Entity [N]**: RuleSet (Draft, RowVersion), RuleSection(+Translation) (Draft, concurrency), RulePublication (snapshot bất biến, IsCurrent) + RulePublicationSection(+Translation), RuleAcknowledgement (gắn GuestVisit).
- **Use case [N]**: CRUD Draft section/translation (sanitize HTML qua **`IHtmlSanitizer`**), Publish (snapshot Draft→Publication, Version++, đổi IsCurrent — trong 1 transaction), Preview (render Draft), GetCurrentRules (guest đọc từ IsCurrent), Acknowledge (**server tự xác định IsCurrent**, không tin version client — CP13), EnforceRuleGate (403 `rule_ack_required`).
- **Ports**: **`IHtmlSanitizer`** (CP12), **`IClock`**, **`ICurrentUser`**, concurrency `xmin` (409 — CP15).
- **Contracts lộ ra**: `IRuleGate` (Faq/Concierge/Housekeeping gọi để chặn khi chưa ack).
- **Cross-module đọc**: GuestVisit hiện tại (GuestAccess.Contracts), ngôn ngữ/settings (ResortConfig.Contracts).

### 4.6 Faq (`faq`) — DỰNG MỚI
- **Entity [N]**: FaqCategory(+Translation), FaqItem (self ParentId, concurrency) (+Translation).
- **Use case [N]**: CRUD category/item cha-con + reorder + active/inactive (sanitize qua **`IHtmlSanitizer`**), GetFaqTree (guest, chỉ active, theo lang + fallback isFallback — dùng `ITranslationResolver`), (tuỳ chọn) ghi FaqEvent.
- **Gate**: `GET /faq` gọi `IRuleGate` → 403 nếu chưa ack (theo settings).
- **Endpoint**: guest `/v1/guest/faq?lang=`; admin `/v1/faq/*` (CRUD + reorder).

### 4.7 Concierge (`concierge`) — DỰNG MỚI
- **Entity [N]**: Conversation (Open/Closed, UnreadForStaff, gắn RoomId+GuestVisitId, 1/visit), Message (Guest/Staff/System), InternalNote (không lộ guest — CP7).
- **Use case [N]**: SendGuestMessage (tạo/reopen hội thoại theo visit, `IRuleGate` + rate-limit + idempotency qua **`IIdempotencyStore`**, giới hạn độ dài), GetConversation (guest polling), Reply/MarkRead/Close (staff), Notes CRUD.
- **Realtime**: SignalR `ChatHub` **[N]** đặt ở **Concierge.Api** (map trong Host). Groups `resort-{id}-staff`, `conversation-{id}`; kiểm quyền join (guest chỉ join hội thoại của session mình — CP6). Fallback polling.
- **Ports**: **`IIdempotencyStore`** (chống double-tap — đã có ở base Caching), **`IClock`**, **`ICurrentUser`**.
- **Cross-module**: verify RoomId (Rooms.Contracts), GuestVisit hiện tại + portal-window (GuestAccess.Contracts), gate (Rules.Contracts).

### 4.8 Housekeeping (`housekeeping`) — DỰNG MỚI
- **Entity [N]**: HousekeepingTicket (Requested/InProgress/Done/Cancelled, 1 mở/phòng, CompletionMethod App/StaffScan), HousekeepingEvent (nhật ký từng chuyển trạng thái).
- **Use case [N]**: CreateTicket (guest hoặc staff; chống trùng khi phòng có ticket mở; `IRuleGate` + rate-limit + idempotent theo phòng), ChangeStatus, CompleteByRoom (`{roomId}`→App), CompleteByToken (`{token}`→resolve qua Rooms.Contracts→StaffScan), mỗi chuyển trạng thái ghi HousekeepingEvent.
- **Realtime**: `HousekeepingUpdated` qua ChatHub (Concierge) hoặc hub riêng — MVP dùng chung hub.
- **Endpoint**: guest `/v1/guest/housekeeping` (POST/GET); admin board + `/status` + `/complete-by-room` + `/complete-by-token`.

---

## Data Models

Data model nghiệp vụ (entity/field/enum/quan hệ) là **nguồn sản phẩm** ở `docs/resort-qr-portal/design.md` §Data Models — TÁI DÙNG nguyên, không viết lại. Phần on-Bedrock chỉ quyết **nơi đặt** (schema/module) + **ràng buộc DB**: xem bảng §3 (partial/unique index theo schema module) và §4 (entity per-module, [P]=port / [N]=dựng mới). Nguyên tắc: entity thuộc module sở hữu; tham chiếu chéo-module bằng Id trần (Guid), không FK chéo schema.

## 5. Reconcile route: product `/api/...` ↔ Bedrock `/v1/...` (QR-DV-001/006)

Mọi API dùng Bedrock URL versioning `/v1/<group>/...`. Riêng resolve đổi thêm method/credential placement: physical QR vẫn mở Guest Web `/r/{token}`, nhưng SPA gọi **`POST /v1/guest/resolve`** với token trong body (QR-DV-006), không `GET .../{token}`. Resolve tạo/touch session+visit nên POST đúng semantics và tránh capability token trong API request-target/access log. Frontend phải scrub URL; reverse proxy redact `/r/*`. Các endpoint khác giữ group versioned như `/v1/rooms`, `/v1/resort/settings`, `/v1/rules`... Reverse-proxy alias `/api/*` chỉ thêm khi có consumer compatibility thật.

---

## 6. Dashboard = ghép ở Host (QR-TO-003)

`GET /v1/dashboard/stats` đặt trong **StarHill.Api** (Host), tổng hợp từ query port mỗi module:
- unread + hội thoại mở ← `Concierge.Contracts.IConciergeStats`
- ticket mở ← `Housekeeping.Contracts.IHousekeepingStats`
- phòng active ← `Rooms.Contracts.IRoomQuery`
- ack hôm nay ← `Rules.Contracts.IRuleStats`

Host được phép ref mọi `<M>.Contracts` (nó là composition root). **Không** cho module này ref module khác → giữ cô lập. Tradeoff: Host phình một chút vs cô lập module — chấp nhận (QR-TO-003).

---

## Correctness Properties

**§7 — Bản đồ 15 Correctness Property → guard test** (keystone: mỗi CP có test)

> Định nghĩa CP đầy đủ ở `docs/resort-qr-portal/design.md` §Correctness Properties. Dưới đây map mỗi CP → **module đặt guard test** + kiểu test trong solution `starhill/`.

### Property 1: Resolve token an toàn
Token lỗi/phòng vô hiệu KHÔNG lộ phòng; URL không chứa số phòng trần. **Guard**: GuestAccess + Rooms — unit + HTTP integration.
**Validates: Requirements 1.1, 1.3, 1.4, 1.6, 7.4**

### Property 2: 1 token Active/phòng, không auto-expire
Mỗi phòng tối đa 1 RoomQrToken Active; revoke giữ lịch sử; không auto-expire. **Guard**: Rooms — unit + DB partial index (Testcontainers).
**Validates: Requirements 1.5, 7.4, 7.5**

### Property 3: Rule-gate enforce ở backend
Chưa ack version IsCurrent → 403 ở FAQ/messages/housekeeping. **Guard**: Rules + Faq/Concierge/Housekeeping — integration.
**Validates: Requirements 3.2, 3.11, 5.1, 6.1**

### Property 4: Draft không ảnh hưởng khách (snapshot)
Khách đọc từ RulePublication IsCurrent; sửa Draft không lọt ra tới khi Publish. **Guard**: Rules — unit + integration.
**Validates: Requirements 3.9, 8.1, 8.3**

### Property 5: Toàn vẹn bản dịch/fallback
Thiếu dịch → default + isFallback, không rỗng. **Guard**: ResortConfig (i18n) — unit.
**Validates: Requirements 2.2, 2.5, 8.7**

### Property 6: Cô lập lượt lưu trú & hội thoại
Khách chỉ thấy dữ liệu visit của mình; SignalR join đúng quyền. **Guard**: Concierge + GuestAccess — integration + SignalR.
**Validates: Requirements 5.2, 5.9, 10.1, 11.2**

### Property 7: Ghi chú nội bộ riêng tư
InternalNote không bao giờ xuất hiện response guest. **Guard**: Concierge — contract/integration.
**Validates: Requirements 9.4**

### Property 8: Phân quyền
Staff không gọi được endpoint Admin; guest không gọi được admin. **Guard**: Identity + admin endpoints — integration (401/403).
**Validates: Requirements 11.1, 11.3**

### Property 9: Nối lại visit, cửa sổ thao tác & cascade
Nối lại visit Active; quá 30' phải quét lại; visit kết thúc → đóng hội thoại + huỷ ticket. **Guard**: GuestAccess — unit + integration.
**Validates: Requirements 10.2, 10.3, 10.4, 10.5, 10.8**

### Property 10: Housekeeping không trùng & hoàn tất đúng phòng
Không tạo ticket trùng; complete-by-room/token đúng phòng; ghi HousekeepingEvent. **Guard**: Housekeeping — unit + DB.
**Validates: Requirements 6.2, 6.4, 6.5, 6.8**

### Property 11: Đơn điệu của unread
UnreadForStaff tăng khi guest gửi, về 0 khi staff đọc, không âm. **Guard**: Concierge — unit.
**Validates: Requirements 5.3, 5.4**

### Property 12: An toàn nội dung
HTML nội quy/FAQ luôn sanitize (không script thực thi). **Guard**: Rules + Faq (qua `IHtmlSanitizer`) — unit.
**Validates: Requirements 8.6, 11.4**

### Property 13: Acknowledge do server xác thực
Server tự xác định publication IsCurrent; không tin version client. **Guard**: Rules — unit + integration.
**Validates: Requirements 3.7, 3.9**

### Property 14: Đúng một ngôn ngữ mặc định
Mỗi resort đúng 1 ResortLanguage.IsDefault. **Guard**: ResortConfig — DB partial index.
**Validates: Requirements 2.1, 2.2**

### Property 15: Chống ghi đè đồng thời
Hai người sửa cùng nội dung → người sau nhận 409. **Guard**: Rules/Faq/ResortConfig — integration (xmin, Testcontainers).
**Validates: Requirements 8.1, 8.5**

> **Test cần PostgreSQL thật** (Testcontainers): CP2/CP10/CP14/CP15 — Docker chưa có ở máy này → SKIP (không fail), khớp base.

---

## Error Handling

Chuẩn lỗi kế thừa Bedrock: mọi `Result.Error` → HTTP **chỉ** qua `ProblemDetailsBuilder`. GuestAccess active map unknown/revoked/soft-deleted cùng `qr_invalid` vì `IRoomTokenResolver` cố ý không phân biệt; room resolve được nhưng inactive dùng `room_inactive`, không metadata. Các module sau thêm `rule_ack_required`, `rate_limited`, `message_too_long`, `session_expired`, `language_not_supported`. Không tạo `qr_revoked` ở GuestAccess nếu Rooms contract chưa/không lộ reason; tuyệt đối không bypass contract để phân biệt.

## Testing Strategy

- **Keystone**: mỗi CP code-được-kiểm có guard test trong solution `starhill/` (bản đồ §7). Chiến lược test theo tầng (unit/integration/SignalR/frontend/E2E) tái dùng `docs/resort-qr-portal/design.md` §Testing Strategy.
- **PostgreSQL thật (Testcontainers) bắt buộc** cho partial unique index, concurrency `xmin` và GuestAccess row-lock/race. SQLite/InMemory chỉ dùng cho logic provider-agnostic; bằng chứng cuối race/index không được là skip mềm. Trước khi chạy cần `docker version` có Server version.
- **JournalConsistency QR đã hoạt động** qua `StarHill.ArchitectureTests` INV-1..5; mọi AD mới phải map `05-anti-drift.md`. Diagnostics chạy cho design spec mỗi lần sửa.

## 8. Phụ thuộc & version package (không bịa — resolve qua `dotnet add`/reuse)

- **QuestPDF** (PDF nhãn QR): bản cũ HOÃN vì license (community MIT nếu doanh thu <$1M; ghi rõ khi thêm). Thêm qua CPM `Directory.Packages.props` của `starhill/`, để `dotnet add`/`dotnet package search` resolve version — KHÔNG tự bịa số.
- **QRCoder** (PNG): bản cũ đã dùng (`QrCoderQrService`) — reuse version có sẵn trong `resort-qr`.
- **HtmlSanitizer** (Ganss) cho `IHtmlSanitizer` adapter: base có port; adapter resort-qr (`HtmlSanitizerAdapter`) reuse.
- **SignalR**: `Microsoft.AspNetCore.SignalR` (in-framework .NET 10) — không cần package ngoài.
- Mọi CVE transitive: theo pattern pin AD-028 của base (`Directory.Packages.props`).

---

## 9. Quyết định còn mở → CHỐT với mặc định (user cho phép mặc định nếu không nêu)

| Vấn đề | Chốt (mặc định) | Lý do (chính xác) | Journal |
|---|---|---|---|
| Cùng-origin vs subdomain | **Cùng-origin** `https://portal.starhill.local` (`/`→guest, `/admin`→admin, `/v1`→API, `/hubs`→SignalR) | Design QR khuyến nghị MVP cùng-origin (đơn giản cookie/CORS; guest cookie SameSite=Lax đủ). Tách subdomain để dành khi cần. | QR-AD-003 |
| Nguồn cert HTTPS | **Internal CA** (root cài vào thiết bị) trên **DNS nội bộ thật** | Secure-context bắt buộc cho camera StaffScan (getUserMedia). Internal CA kiểm soát được, tránh self-signed lẻ (điện thoại chặn). Cert công khai qua DNS nội bộ là phương án thay thế nếu resort có domain thật. | QR-AD-003 |
| Cascade đóng visit | **Outbox/inbox at-least-once khi consumer tồn tại** | Multi-DbContext không có shared transaction mặc định; local visit+outbox atomic, consumer idempotent, guest bị chặn ngay theo visit status. | QR-AD-027/QR-TO-006 |
| Tên module messaging | **Concierge** | Tránh đụng `Bedrock.Messaging.Contracts`. | QR-AD-004 |

---

## 10. Build waves (thứ tự dựng module trên Bedrock)

Bám dependency graph của tasks.md, quy về module Bedrock:

- **Wave A — nền + config (đã xong)**: ResortConfig + journal/CI sản phẩm.
- **Wave B — Identity + Rooms (đã xong các slice hiện hành)**: auth/policy, Rooms CRUD/token/QR/admin/query và ResortConfig settings API.
- **Wave C — GuestAccess (đang ở C-GA.0 design)**: `design-modules/03-guestaccess.md`; resolve phụ thuộc Rooms + ResortConfig, sau đó portal context/sweeper.
- **Wave D**: Rules (rule-gate là contract dùng chung) → rồi Faq ‖ Concierge ‖ Housekeeping (đều gọi `IRuleGate`).
- **Wave E**: Dashboard (Host) + cascade CloseGuestVisit + SignalR hub hoàn chỉnh.
- **Wave F**: Frontend guest-web + admin-web (port từ `resort-qr/frontend`, chỉnh base path §5) + PDF nhãn (QuestPDF) + rà soát bảo mật + integration test end-to-end.

Mỗi wave: một increment verify (`starhill\scripts\vp.cmd build` 0-warning + test) → journal QR-AD/DV/TO/N với Provenance thật, **DESIGN-FIRST cho phần dựng mới** (Rules/Faq/Concierge/Housekeeping thiết kế entity/luồng chi tiết trước khi code).

---

## 11. Ranh giới sạch & anti-drift (nhắc lại — bắt buộc)

- `platform/src/` là **nguồn Bedrock vật lý duy nhất**; `starhill/` reference trực tiếp qua `$(PlatformSrc)` (QR-AD-012). Năng lực nền domain-agnostic sửa ở platform và được product nhận lúc compile; không re-copy/vendoring.
- Entity/nghiệp vụ QR chỉ ở `starhill/src/Modules/<M>` — KHÔNG lọt `platform/src/Bedrock.*` (CP1).
- Cross-module chỉ qua `<M>.Contracts` — KHÔNG ref `<M>.Infrastructure`/DbContext module khác (ModuleBoundaryTests).
- Mỗi CP code-được-kiểm phải có guard test trong solution `starhill/` (keystone).

---

## 12. Cổng hiện tại trước implementation GuestAccess

Identity/ResortConfig/Rooms và các API slice đã hoàn tất. Bước hiện tại là review/validate `design-modules/03-guestaccess.md` (C-GA.0). Chỉ bắt đầu C-GA.1 sau khi diagnostics + JournalConsistency INV-1..5 xanh và quyết định POST-body/row-lock/outbox cascade được chấp nhận. Mỗi slice GuestAccess phải theo cổng dừng §12 của design module; PostgreSQL race/index cần Docker Server thật.
