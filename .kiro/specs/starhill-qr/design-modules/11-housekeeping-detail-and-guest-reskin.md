# Module design — Housekeeping detail extension + Guest Journey re-skin (Wave FE.6)

> **Design-first, CHƯA code.** Mọi hợp đồng BE dưới đây ĐỌC TỪ CODE thật (`Housekeeping.*`, `guest-web/src/core/*`,
> `guest-web/src/views/HomeView.vue`, `guest-web/src/style.css`) — KHÔNG suy đoán. Tiếp nối design-module 10
> (Guest Journey). Mục tiêu do user chốt: **giữ Y giao diện mockup đẹp** (đang ở `/demo`) cho hành trình THẬT +
> **mở rộng backend Housekeeping để LƯU THẬT** các trường form (loại dịch vụ, đếm vật dụng, thời gian, ghi chú) —
> KHÔNG bịa form (không để form đẹp mà backend vứt dữ liệu).

## 0. Bối cảnh & vấn đề (đã verify)

- **FE.5 (design-module 10) đã nối 4 capability thật** (rules/faq/chat/housekeeping) nhưng dùng **shell trơn PrimeVue +
  token `--sh-*`** → user phản hồi "giao diện đã bị thay đổi", muốn giữ mockup đẹp tự thiết kế.
- **Mockup đẹp** = `guest-web/src/views/HomeView.vue` (~924 dòng, route `/demo`) + CSS global `guest-web/src/style.css`
  (`phone-container`, `app-header`, `bottom-nav`, 5 theme `body.theme-*`, `--primary/--bg-card/--radius-md/--shadow-soft`,
  language drawer, toast). Mockup có **3 chỗ dữ liệu GIẢ**:
  1. Chat auto-reply `setTimeout` giả (`chat.autoResponse`).
  2. Language drawer liệt kê **48 ngôn ngữ** nhưng chỉ 4 locale thật (`vi/en/ko/zh`).
  3. **Housekeeping form giả**: chọn loại dịch vụ (Full/Towel/Trash/Refill) + đếm 4 vật dụng (toothbrush/towel/water/soap,
     0–5) + thời gian (Now/1h/Specific HH:mm) + ghi chú + timeline `setTimeout` 5-10-15s. **Backend hiện chỉ nhận
     `POST /v1/guest/housekeeping {roomId}`** — vứt toàn bộ trường form.
- **User CHỌN**: mở rộng backend để LƯU các trường (giữ form đẹp + lưu thật), thay vì cắt form về nút trơn.

## 1. Đối soát nguồn BE Housekeeping hiện tại (đọc code, KHÔNG bịa)

| Artifact | Trạng thái hiện tại |
|---|---|
| `HousekeepingTicket` (Domain) | `Entity, IHasConcurrencyToken`; Id (uuidv7 ValueGeneratedNever), ResortId, RoomId, RequestedByGuestSessionId?, GuestVisitId?, Status, CreatedAt, StartedAt?, CompletedAt?, CompletedByUserId?, CompletionMethod?, RowVersion (xmin). Table `housekeeping.housekeeping_ticket`. |
| `HousekeepingEnums` | `HousekeepingStatus{Requested,InProgress,Done,Cancelled}`, `HousekeepingCompletionMethod{App,StaffScan}`, `HousekeepingActorType{Guest,Staff,System}`. |
| `RequestHousekeepingInput` (Application) | `(Guid ResortId, Guid RoomId, Guid GuestSessionId, Guid GuestVisitId)`. |
| `RequestHousekeepingUseCase` | fail-closed: config→`HousekeepingEnabled`→rule-gate(`GuestFeature.Housekeeping`)→**idempotent 1-mở/phòng** (FindOpenTicket→existing) → tạo `Requested` + `HousekeepingEvent(Guest)` cùng transaction; race 23505→re-query. |
| `HousekeepingTicketView` | `(TicketId, RoomId, Status, CreatedAt, StartedAt?, CompletedAt?)` — guest-facing. |
| `HousekeepingBoardItem` | `(TicketId, RoomId, GuestVisitId?, Status, CreatedAt, StartedAt?, CompletedAt?, CompletedByUserId?, CompletionMethod?)` — admin board. |
| `RequestHousekeepingRequest` (Api DTO) | `(Guid RoomId)`. |
| EF config | `HousekeepingTicketConfiguration`: Id ValueGeneratedNever; Status/CompletionMethod `HasConversion<string>().HasMaxLength(16)`; `ux_hk_open_ticket_room` partial-unique filter `status IN ('Requested','InProgress')`; `ix_hk_ticket_resort_status`. |
| `HousekeepingErrors` | `Disabled`(403 `housekeeping_disabled`), `ConfigurationUnavailable`(`configuration_unavailable`), `NoOpenTicket`(404), `TicketNotFound`(404), `InvalidTransition`(validation), `QrInvalid`(reuse `qr_invalid`). |
| Migration | `20260716124601_InitialCreate` (mới nhất) + `HousekeepingDbContextModelSnapshot`. |
| Call-sites `RequestHousekeepingInput` | Contracts (def) · UseCase · DI extension (registration) · GuestEndpointModule (construct) · `HousekeepingUseCaseTests` (construct) · `RoomsUseCaseFakes.FakeRequestHousekeeping` (type-param only). |

## 2. Thiết kế BE — mở rộng Housekeeping lưu chi tiết yêu cầu (Wave FE.6a)

### 2.1 Enum mới (`HousekeepingEnums.cs`)

```csharp
/// Loại dịch vụ dọn phòng khách chọn (Req 6.1) — khớp mockup serviceFull/Towel/Trash/Refill.
public enum HousekeepingServiceType { Full, Towel, Trash, Refill }

/// Thời điểm khách muốn phục vụ (Req 6.1) — khớp mockup timeNow/time1h/timeSpecific.
public enum HousekeepingPreferredTime { AsSoonAsPossible, WithinOneHour, SpecificTime }
```

Lưu STRING (`HasConversion<string>().HasMaxLength(16)`) — mirror `HousekeepingStatus` (đọc được ở DB, provider-agnostic).

### 2.2 Trường mới trên `HousekeepingTicket` (Domain) — set TẠI request-time

| Field | Kiểu | Nullable | Ghi chú |
|---|---|---|---|
| `ServiceType` | `HousekeepingServiceType?` | **có** | Guest set bắt buộc; **null** với ticket staff tạo chủ động (Req 6.9 — vận hành, không form). |
| `PreferredTime` | `HousekeepingPreferredTime?` | **có** | như trên. |
| `PreferredTimeText` | `string?` | có | CHỈ có khi `PreferredTime==SpecificTime`, định dạng `HH:mm` (24h); ngược lại null. MaxLength 5. |
| `AmenityToothbrush` | `int` | không | default 0, [0..5]. |
| `AmenityTowel` | `int` | không | default 0, [0..5]. |
| `AmenityWater` | `int` | không | default 0, [0..5]. |
| `AmenitySoap` | `int` | không | default 0, [0..5]. |
| `Note` | `string?` | có | plain-text, trim; rỗng→null; MaxLength 500. KHÔNG sanitize HTML (không render v-html — hiển thị text). |

**Lý do nullable ServiceType/PreferredTime**: 2 đường tạo ticket — guest (form đầy đủ) và staff chủ động (`CreateHousekeepingByStaffUseCase`,
không form). Non-nullable sẽ buộc staff-path bịa giá trị. Nullable = trung thực nguồn dữ liệu. Guest-path enforce hiện diện qua validation (§2.4).

### 2.3 Bundle contract `HousekeepingRequestDetails` (Application)

Gom 8 trường form vào MỘT record (tránh phình signature + dễ mock/validate). Guest luôn gửi (non-nullable trong Input):

```csharp
public sealed record HousekeepingRequestDetails(
    HousekeepingServiceType ServiceType,
    HousekeepingPreferredTime PreferredTime,
    string? PreferredTimeText,
    int AmenityToothbrush,
    int AmenityTowel,
    int AmenityWater,
    int AmenitySoap,
    string? Note);
```

- `RequestHousekeepingInput` → `(Guid ResortId, Guid RoomId, Guid GuestSessionId, Guid GuestVisitId, HousekeepingRequestDetails Details)`.
- `RequestHousekeepingRequest` (Api DTO) → `(Guid RoomId, HousekeepingRequestDetails Details)` — client gửi form; endpoint map sang Input (không tin ResortId/session client — vẫn từ context).
- `HousekeepingTicketView` (guest) **+** `(ServiceType?, PreferredTime?, PreferredTimeText?, AmenityToothbrush, AmenityTowel, AmenityWater, AmenitySoap, Note?)` — guest xem lại tóm tắt yêu cầu ĐÃ gửi (summary box mockup).
- `HousekeepingBoardItem` (admin) **+** cùng 8 trường — **lễ tân PHẢI thấy khách yêu cầu gì** (nếu không thì mở rộng vô nghĩa cho vận hành). Đây là lý do cốt lõi "lưu thật".

### 2.4 Validation (guest use case) — mã lỗi mới `housekeeping_invalid_request`

Thêm `HousekeepingErrors.InvalidRequest => Error.Validation("housekeeping_invalid_request", "...")`. **Cập nhật `ErrorCodeSnapshotTests`** (QR-AD-018 — chống drift mã lỗi client-facing).

Thứ tự use case (chèn validation SAU gate, TRƯỚC idempotency — malformed luôn bị từ chối nhất quán, kể cả khi đã có ticket mở):
1. config → `configuration_unavailable`
2. `HousekeepingEnabled=false` → `housekeeping_disabled`
3. rule-gate `GuestFeature.Housekeeping` (CP3)
4. **VALIDATE Details** (mới) → `housekeeping_invalid_request` nếu:
   - `!Enum.IsDefined(ServiceType)` hoặc `!Enum.IsDefined(PreferredTime)`.
   - `PreferredTime==SpecificTime` mà `PreferredTimeText` không khớp regex `^([01]\d|2[0-3]):[0-5]\d$`.
   - Bất kỳ amenity ∉ [0,5].
   - `Note` (sau trim) dài > 500.
   - **Chuẩn hoá**: `PreferredTime≠SpecificTime` → ép `PreferredTimeText=null`; `Note` trim, rỗng→null.
5. idempotent: FindOpenTicket → trả existing `AlreadyOpen=true` (KHÔNG áp Details mới — giữ nguyên yêu cầu đầu; §9 QR-TO).
6. tạo ticket set 8 trường + `HousekeepingEvent(Guest)` cùng transaction; race 23505→re-query.

Validate INLINE trong use case (private static `ValidateDetails` trả `Error?`) — provider-agnostic, unit-test SQLite/in-memory cục bộ (không cần validator pipeline; mirror lối fail-closed sẵn có của use case).

### 2.5 EF config + migration

- `HousekeepingTicketConfiguration` thêm: `ServiceType`/`PreferredTime` `HasConversion<string>().HasMaxLength(16)`; `PreferredTimeText` `HasMaxLength(5)`; `Note` `HasMaxLength(500)`. Amenity int (default 0 — không cần cấu hình đặc biệt; đảm bảo NOT NULL default 0 ở migration).
- **Migration ADDITIVE mới** `AddHousekeepingRequestDetails` (`dotnet ef migrations add` qua factory): thêm 8 cột nullable/defaulted vào `housekeeping.housekeeping_ticket`. **GIỮ nguyên** `ux_hk_open_ticket_room`, `ix_hk_ticket_resort_status`, `xmin`. An toàn (cột mới nullable + int default 0 → không phá dữ liệu cũ, không đổi ràng buộc).
- Cập nhật `HousekeepingDbContextModelSnapshot` (EF tự sinh). `.editorconfig generated_code` đã miễn analyzer cho Migrations (QR-AD-009).

### 2.6 Use case/endpoint khác chịu ảnh hưởng (giữ compile + ngữ nghĩa)

- `HousekeepingGuestEndpointModule.RequestAsync`: nhận `request.Details`, dựng `RequestHousekeepingInput(context..., request.Details)`. Vẫn resolve context (không tin client ResortId/session). `no-store`, touch-after-success giữ nguyên.
- `HousekeepingInfrastructureExtensions`: registration không đổi chữ ký (vẫn `IUseCase<RequestHousekeepingInput,...>`).
- `IHousekeepingReader` impl (`EfHousekeepingReader`): map thêm 8 trường vào `HousekeepingTicketView` (guest GetCurrent) + `HousekeepingBoardItem` (admin board). **Đọc file thật khi code** để map đúng.
- `CreateHousekeepingByStaffUseCase`: KHÔNG set Details (null/0) — không đổi chữ ký.
- Test/fake: `HousekeepingUseCaseTests` + `FakeRequestHousekeeping` cập nhật construct Input có Details.

### 2.7 Guard tests BE (INV-6 — Status "Implemented" phải khai Guard-Tests C# có thật)

- `HousekeepingUseCaseTests` (SQLite local, KHÔNG Docker) thêm:
  - `Request_persists_all_detail_fields` (Full + SpecificTime "14:30" + amenities + note → đọc lại entity đủ 8 trường).
  - `Request_rejects_missing_or_invalid_details` (Theory: undefined enum / SpecificTime thiếu text / text sai regex / amenity=6 / note>500 → `housekeeping_invalid_request`, KHÔNG ghi ticket).
  - `Request_normalizes_note_and_time` (Now + PreferredTimeText="x" → text bị ép null; Note "  " → null).
  - `Idempotent_returns_existing_without_applying_new_details` (ticket mở sẵn từ Full → gửi Towel → trả existing AlreadyOpen, entity vẫn Full).
  - `Guest_view_and_board_expose_detail_fields` (reader trả đủ 8 trường) — nếu cần Docker cho reader, đặt integration; ưu tiên SQLite nếu chỉ map.
- `ErrorCodeSnapshotTests` cập nhật snapshot +`housekeeping_invalid_request`.

## 3. Thiết kế FE — Guest Journey re-skin theo mockup (Wave FE.6b)

### 3.1 Nguyên tắc (giữ INV1-8 + DI-1..5 của design-module 10)

- **Giữ NGUYÊN kiến trúc B** (JourneyCore + ApiGateway + thin views + router guards server-authoritative). Re-skin CHỈ đổi
  lớp trình bày (shell + CSS + component tab), KHÔNG đổi luồng gate/rescan/ack (INV2/INV3 bất biến).
- **Tái dùng CSS global `style.css`** (phone-container/app-header/bottom-nav/theme/drawer/toast) — đã tồn tại, license-clean (tự viết).
- **Bỏ dữ liệu giả**: chat auto-reply (nối polling thật đã có FE.5c); 48 ngôn ngữ → CHỈ `resolvedContext.languages` (4 thật);
  housekeeping timeline giả → REAL status poll (Requested→InProgress→Done từ backend).

### 3.2 Cấu trúc component (router-based, KHÔNG monolith)

Mockup là 1 file tab-switch nội bộ. Real path GIỮ router + guards (gating server-authoritative). Giải pháp: **layout dùng chung**.

```text
guest-web/src/
  layouts/GuestShell.vue     MỚI — phone-container + app-header (brand=resolvedContext.resort.name; lang pill→drawer;
                             room badge=resolvedContext.room.number; theme dots 5 preset) + <router-view> trong .app-content
                             + bottom-nav 4 tab (điều hướng route; khoá theo core.mustReadRules/capabilities) + toast
                             + language drawer (CHỈ resolvedContext.languages). Đọc JourneyCore (KHÔNG fetch — DI-2).
  views/RulesView.vue        RE-SKIN dùng .rule-card/.agree-box/.btn-confirm; GIỮ force-read state-machine THẬT (§3 dm10).
  views/HousekeepingView.vue RE-SKIN form đẹp (.service-grid/.time-grid/.amenities-list/.counter/.textarea) + timeline THẬT.
  views/FaqView.vue          RE-SKIN .faq-item accordion; GIỮ cây FAQ THẬT (đệ quy children) + CTA→chat.
  views/ChatView.vue         RE-SKIN .chat-container/.chat-bubble/.chat-input-area; GIỮ polling THẬT (bỏ auto-reply giả).
  router/index.ts            capability routes nest dưới GuestShell (component layout); guard beforeEach giữ nguyên.
  views/HomeView.vue         GIỮ ở /demo (tham chiếu tĩnh) — hoặc gỡ sau khi real-path đạt (§9 quyết định).
```

**Gating ở bottom-nav (advisory UX; server vẫn chốt — INV2)**: tab housekeeping/faq/chat hiện khoá (`.locked`) khi
`core.mustReadRules` hoặc capability chưa mở; bấm → toast "đọc nội quy trước" + điều hướng /rules. Đây là lặp lại UX mockup
nhưng nguồn = `core` derived (KHÔNG `ruleConfirmed` boolean local).

### 3.3 HousekeepingView re-skin — form đẹp nối endpoint mở rộng

- **Chưa có ticket mở** (null/Done/Cancelled): hiện FORM đẹp (service-grid 4 lựa chọn + time-grid Now/1h/Specific+picker HH:mm
  + amenities counter 4 món 0–5 + textarea note). Submit → `apiGateway.requestHousekeeping(roomId, details)` (chữ ký MỞ RỘNG).
  Validate client (chọn service + time + nếu Specific chọn đủ giờ:phút) trước gửi; server validate lại (chốt — `housekeeping_invalid_request` hiện toast).
- **Có ticket mở** (Requested/InProgress): hiện **summary box** (service + time + amenities + note ĐỌC TỪ ticket view mở rộng)
  + **timeline THẬT** (Requested→InProgress→Done theo `ticket.status`, poll ~5s — lễ tân đổi trạng thái phản ánh ngay) + disable nút.
  **KHÔNG** timeline `setTimeout` giả.
- `alreadyOpen=true` → toast "đã có yêu cầu mở".
- **Cancel**: mockup có nút huỷ (giả). Backend **CHƯA có guest-cancel endpoint** (chỉ staff complete/cancel-for-visit).
  → §9 QR-DV: ẩn nút huỷ ở guest (không bịa hành vi backend không hỗ trợ) HOẶC chỉ "quay về" — quyết định: ẩn cancel, giữ trung thực.

### 3.4 apiGateway mở rộng (DI-1)

- `requestHousekeeping(roomId: string, details: HousekeepingRequestDetails): Promise<RequestHousekeepingResult>` — body `{roomId, details}`.
- Type `HousekeepingRequestDetails` (FE) + enum `HousekeepingServiceType`/`HousekeepingPreferredTime` (string literal union khớp BE JSON string-enum — QR-AD-021 JsonStringEnumConverter toàn cục).
- `HousekeepingTicket` (FE type) + 8 trường mới (guest view mở rộng) để summary box đọc.
- MOCK DEV-only cập nhật: `requestHousekeeping` lưu details vào `mockTicket`; timeline mock vẫn theo status (không setTimeout).

### 3.5 i18n

- Tái dùng khóa i18n mockup (`housekeeping.*`, `rules.*`, `faq.*`, `chat.*`, `common.*`) đã có trong `guest-web/src/i18n` (verify khi code).
  Bổ sung khóa cho label mới nếu thiếu (4 locale vi/en/ko/zh — INV5). KHÔNG thêm 48 ngôn ngữ.

### 3.6 Guard FE = Playwright (KHÔNG INV-6 C#-guard → Status "Accepted")

- Ma trận viewport (mobile-first) assert: **no-horizontal-overflow** + **no-console-error** + screenshots trọn hành trình
  (home/rules/faq/chat/housekeeping) trên shell mới. Force-read gate (Next disabled tới scroll-end+countdown). Housekeeping
  form submit → summary+timeline. Chat polling append. Language drawer 4 lang. Theme switch 5 preset.
- Chạy `pnpm --filter guest-web build` (EXIT=0) + Playwright suite XANH → journal.

## 4. Bất biến & fail-closed (kế thừa + mới)

- **INV-HK1** (mới): guest-request PHẢI có ServiceType + PreferredTime hợp lệ; SpecificTime ⇒ PreferredTimeText khớp `HH:mm`.
- **INV-HK2** (mới): amenity ∈ [0,5]; Note ≤ 500 sau trim; PreferredTime≠SpecificTime ⇒ PreferredTimeText=null.
- **INV-HK3**: idempotency KHÔNG cho phép ghi đè Details của ticket mở (yêu cầu đầu thắng — tránh khách/kẻ tấn công đổi yêu cầu ngầm).
- Kế thừa INV2 (server-authoritative ack), INV3 (session_expired→rescan), INV8 (chat plain-text, note plain-text render textContent).

## 5. Slice triển khai (mỗi slice: build 0-warning + full test + vp journal xanh → journal → commit)

1. **FE.6a — BE Housekeeping detail** (§2): enum + entity fields + Details bundle + Input/DTO/View/BoardItem + validation +
   EF config + migration + reader map + endpoint + tests + ErrorCodeSnapshot. Gate: `starhill vp all` (Docker skip mềm) +
   `vp journal`. Journal QR-AD-058 (Implemented + Guard-Tests) + QR-N.
2. **FE.6b — FE re-skin** (§3): GuestShell layout + re-skin 4 view + apiGateway mở rộng + router nest + i18n. Gate:
   `pnpm build` EXIT=0 + Playwright suite XANH. Journal QR-AD-059 (Accepted — Playwright guard) + QR-N + QR-DV + QR-TO.

## 6. Self-validation trước code

- [x] Hợp đồng BE + call-sites `RequestHousekeepingInput` đọc TỪ CODE thật (§1) — 6 nơi, không bịa.
- [x] Enum/field/validation/migration ADDITIVE thiết kế cụ thể; nullable ServiceType/PreferredTime có lý do (2 đường tạo ticket).
- [x] Idempotency giữ ticket đầu (INV-HK3) — không ghi đè Details (bảo mật + đúng nghiệp vụ).
- [x] BoardItem + guest View lộ 8 trường (lễ tân thấy yêu cầu — lý do cốt lõi "lưu thật").
- [x] FE giữ kiến trúc B + gate server-authoritative; re-skin CHỈ trình bày; bỏ 3 dữ liệu giả; tái dùng CSS global.
- [x] Guard: BE = C# INV-6 (QR-AD-058 Implemented); FE = Playwright (QR-AD-059 Accepted) — đúng mẫu QR-AD-049/050.
- [x] Mã lỗi mới `housekeeping_invalid_request` → cập nhật ErrorCodeSnapshot (QR-AD-018).
- [ ] User review design (đặc biệt §2.2 nullable, §2.4 validation order, §3.3 ẩn cancel) trước khi code FE.6a.
