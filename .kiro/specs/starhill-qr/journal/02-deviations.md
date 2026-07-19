# 02 — Deviations (pha QR) — chỗ AI phải đổi so với yêu cầu/spec gốc

> Journal RIÊNG cho pha `starhill-qr`. ID tiền tố `QR-DV-###`. Ghi khi thiết kế QR-on-Bedrock buộc lệch so với spec QR gốc (`docs/resort-qr-portal/`) hoặc so với bản cũ `resort-qr/`. Mỗi bản ghi có Provenance thật.

---

### QR-DV-001 — Route API đổi từ `/api/{guest,admin}/...` (spec gốc) sang `/v1/<group>/...` (convention Bedrock)
- Status: Proposed (chờ user duyệt design)
- Date: 2026-07-11
- Provenance/Evidence: spec QR gốc `docs/resort-qr-portal/design.md` §API dùng `/api/guest/resolve/{token}`, `/api/admin/rooms`. Base `starhill/` versioning: `IdentityEndpointModule` dùng `MapVersionedGroup("/identity", V1)` → route thật `/v1/identity/...` (đọc mã).
- Deviation: đổi tiền tố route sản phẩm sang `/v1/<group>/...` (group: guest/rooms/rules/faq/concierge/housekeeping/auth) → vd `/v1/guest/resolve/{token}`. Frontend guest-web/admin-web chỉnh base path `/api/...`→`/v1/...`.
- Lý do (bản chất): giữ versioning nền của base (F32) + health/observability/error-shape (ProblemDetails) thống nhất một cơ chế; KHÔNG đổi nghiệp vụ, chỉ đổi hình dạng path. Reverse proxy có thể alias `/api/*`→`/v1/*` nếu cần tương thích ngược. QR vật lý chỉ chứa `GuestWebBaseUrl`+`/r/{token}` (path web tĩnh, KHÔNG phải path API) → không ảnh hưởng mã QR đã in.
- Consequences: cập nhật client base path; tài liệu API sản phẩm cần ghi chú path thật `/v1/...`.
- Reversibility: Easy. Traceability: design.md §5.

---

### QR-DV-002 — i18n (`ITranslation`/`Translated<T>`/`ITranslationResolver`) đặt ở Contracts, KHÔNG kèm marker `ISingletonService`; resolver đăng ký thủ công
- Status: Proposed (design Wave B; áp khi build slice B.1)
- Date: 2026-07-11
- Provenance/Evidence: nguồn `resort-qr/.../Application/Localization/ITranslationResolver.cs` khai `: ISingletonService` (auto-scan). Đọc `starhill/.../Identity.Contracts.csproj` — Contracts CHỈ ref `Bedrock.Messaging.Contracts` (matrix §3.3). Đọc `Bedrock.Application/DependencyInjection/ServiceMarkers.cs` — `ISingletonService` nằm ở `Bedrock.Application`. → Contracts không thể kế thừa marker đó.
- Deviation: so với resort-qr, `ITranslationResolver` (+ `ITranslation`, `Translated<T>`) chuyển vào `ResortConfig.Contracts` (để Rules/Faq dùng qua cross-module Contracts) và BỎ kế thừa `ISingletonService`; impl `TranslationResolver` (thuật toán thuần, logic giữ nguyên) đặt ở `ResortConfig.Application`, đăng ký `services.AddSingleton<ITranslationResolver, TranslationResolver>()` trong `AddResortConfigInfrastructure` (thay auto-scan).
- Lý do (bản chất): i18n phải là surface công khai chia sẻ đa-module; Contracts không được ref Bedrock.Application → không thể mang marker DI. Manual-register là chi phí nhỏ, rõ ràng, không phá ref-graph bất biến.
- Consequences: mọi module dùng i18n ref `ResortConfig.Contracts`; đăng ký resolver thủ công (một dòng) thay vì auto-scan.
- Reversibility: Easy. Traceability: design-modules/01-resortconfig.md §3; QR-AD-002 (cross-module qua Contracts).

---

### QR-DV-003 — Rooms lệch resort-qr do ranh giới modular Bedrock: bỏ FK chéo-schema + đọc settings qua query port
- Status: Proposed (design Rooms; áp slice B-Rooms.2)
- Date: 2026-07-11
- Provenance/Evidence: `resort-qr/.../Configurations/RoomConfigurations.cs` có FK `Room→Resort` (`HasOne<Resort>().WithMany().HasForeignKey(ResortId)`) vì cùng `AppDbContext`. `RenderRoomQrPngUseCase` (resort-qr) đọc `ResortSettings` qua `_unitOfWork.Repository<ResortSettings>()` (cùng DbContext). Bedrock: Rooms (schema `rooms`) và ResortConfig (schema `resort_config`) là DbContext/schema RIÊNG; QR-AD-002 cấm FK chéo schema.
- Deviation: (a) **BỎ FK `Room→Resort`** — `Room.ResortId` là Guid trần (không navigation/FK); tính hợp lệ resortId đảm bảo ở app (lấy từ ResortConfig khi tạo room). (b) `RenderRoomQrPng` đọc `GuestWebBaseUrl` qua **`ResortConfig.Contracts.IResortSettingsQuery`** thay vì repository ResortSettings (khác module/schema).
- Lý do (bản chất): per-module DbContext/schema là bất biến nền Bedrock (F31); cross-module chỉ qua Contracts (F30). Giữ FK chéo-schema sẽ phá cô lập module + không migrate độc lập được.
- Consequences: mất ràng buộc FK DB cho Room.ResortId (chấp nhận: single-resort, app kiểm); Rooms.Application ref ResortConfig.Contracts.
- Reversibility: Medium. Traceability: design-modules/02-rooms.md §6/§4; QR-AD-002; QR-AD-011.

---

### QR-DV-004 — `CreateRoomInput` THÊM `ResortId` (caller truyền) thay vì use case đọc single-Resort từ repo
- Status: Accepted (áp slice B-Rooms.2b-i)
- Date: 2026-07-11
- Provenance/Evidence: nguồn `resort-qr/.../Rooms/CreateRoomUseCase.cs` đọc `_unitOfWork.Repository<Resort>().FirstOrDefaultAsync(_ => true)` (single-resort cùng `AppDbContext`) rồi gán `room.ResortId = resort.Id`; `CreateRoomInput(string RoomNumber, string? Building, int? Floor)` KHÔNG mang ResortId. Bedrock: `Resort` ở module ResortConfig (schema/DbContext RIÊNG) → `Rooms.Application` KHÔNG có `IRepository<Resort>` (và QR-DV-003 cấm FK/ref chéo-schema). Đọc `IUnitOfWork` Bedrock: KHÔNG có `Repository<T>()` accessor (DV-002) — use case inject `IRepository<T>` trực tiếp.
- Deviation: `CreateRoomInput(Guid ResortId, string RoomNumber, string? Building, int? Floor)` — THÊM `ResortId`; `CreateRoomUseCase` dùng `input.ResortId` (KHÔNG đọc Resort từ repo). Caller (`Rooms.Api`, slice sau) phân giải resortId single-resort (qua `IResortSettingsQuery`/claim) rồi truyền vào. `CreateRoomValidator` thêm `RuleFor(x => x.ResortId).NotEmpty()`.
- Lý do (bản chất): Resort không thuộc bounded-context Rooms; đọc nó qua repo cùng-context là artifact của monolith cũ (1 DbContext). Modular Bedrock: dependency chéo-module phải TƯỜNG MINH qua Contracts hoặc do caller phân giải. Đẩy resolve resortId lên Api giữ `Rooms.Application` KHÔNG phụ thuộc ResortConfig.Contracts ở 2b-i (I10 — chỉ thêm khi RenderQrPng ở 2b-ii thật sự cần GuestWebBaseUrl).
- Consequences: `Rooms.Api` (slice sau) chịu trách nhiệm phân giải resortId (một lần, single-resort) trước khi gọi use case; input rộng hơn 1 field. Không mất tính đúng (validator chặn Guid rỗng).
- Reversibility: Easy. Traceability: design-modules/02-rooms.md §4; QR-DV-003; DV-002 (base — IUnitOfWork không có Repository accessor).


### QR-DV-005 — Checkbox Task 5 legacy không được dùng làm trạng thái hoàn thành GuestAccess trong `starhill/`
- Status: Accepted (đối soát design 2026-07-14)
- Date: 2026-07-14
- Provenance/Evidence: `docs/resort-qr-portal/tasks.md` Task 5 có 5.1/5.3 `[x]`, 5.2 `[~]`; source tương ứng tồn tại ở root `resort-qr/` từ commit `c7622a5`. `starhill/src/Modules/` không có GuestAccess; handoff HEAD `16df850` ghi GuestAccess là bước kế tiếp. Không có bằng chứng active module từng tồn tại rồi bị xóa.
- Deviation: với HOW-on-Bedrock, Task 5 được phân loại “legacy implemented/partially implemented, active port not started”; tiến độ active theo `design-modules/03-guestaccess.md` C-GA.0..5, không sửa checkbox legacy thành bằng chứng product hiện hành.
- Lý do (bản chất): hai kế hoạch nhắm hai architecture khác nhau; trộn trạng thái sẽ tạo false-completion và bỏ qua keyed persistence/module boundary.
- Consequences: legacy chỉ là input port; mọi completion StarHill cần code/test/journal trong `starhill/`.
- Reversibility: Easy. Traceability: QR-N-023; `design-modules/03-guestaccess.md` §0.

### QR-DV-006 — Resolve API đổi từ GET path-token sang POST body-token
- Status: Proposed (design C-GA.0; chưa code)
- Date: 2026-07-14
- Provenance/Evidence: design gốc/legacy dùng `GET /api/guest/resolve/{token}`; Bedrock route mapping trước đây dự kiến `/v1/guest/resolve/{token}`. Resolve thực tế tạo/touch GuestSession/GuestVisit và set cookie; Req 11.6 cấm log full token.
- Deviation: API active dùng `POST /v1/guest/resolve` với JSON body `{token}`; physical Guest Web URL vẫn `/r/{token}` theo Req 1.2. Không tạo GET alias nếu chưa có compatibility requirement.
- Lý do (bản chất): GET phải safe nhưng operation này mutate; route token dễ bị ghi bởi proxy/access-log/APM. POST body giảm bề mặt rò capability và phản ánh command semantics.
- Consequences: Guest Web phải POST, scrub URL bằng history API và reverse proxy redact `/r/*`; client legacy chưa tồn tại trong product tree nên chưa có breaking runtime consumer.
- Reversibility: Medium. Traceability: QR-AD-025; QR-DV-001; `design-modules/03-guestaccess.md` §7.

### QR-DV-007 — `ITranslation` đặt trên read-DTO tầng Application (không phải entity Domain như design §8)
- Status: Accepted (áp slice D-Rules.4a)
- Date: 2026-07-15
- Provenance/Evidence: `design-modules/04-rules.md` §3/§8 ghi "`RuleSectionTranslation`/`RulePublicationSectionTranslation` implement `ITranslation`". Nhưng `ITranslation` nằm ở `ResortConfig.Contracts.Localization` (đọc `ResortConfig.Contracts/Localization/ITranslation.cs`); nếu entity Domain implement nó thì `Rules.Domain` phải ref `ResortConfig.Contracts` (Domain phụ thuộc Contracts module khác). Đọc `design-modules/04-rules.md` §2: dependency graph khai `Rules.Application -> ... ResortConfig.Contracts`, KHÔNG khai `Rules.Domain -> ResortConfig.Contracts`. Đọc `RulesBoundaryTests` — Domain phải ⊥ Infra/Api và giữ tối thiểu.
- Deviation: `ITranslation` được hiện thực trên **read-DTO** `PublishedTranslationSnapshot` (record ở `Rules.Application`, đã ref `ResortConfig.Contracts`) THAY VÌ entity Domain `RulePublicationSectionTranslation`/`RuleSectionTranslation`. Entity Domain giữ nguyên (LanguageCode/Title/BodyHtmlSanitized) KHÔNG implement `ITranslation`.
- Lý do (bản chất): i18n-resolution (fallback/HasContent) là concern RENDERING/Application (render theo request khách), KHÔNG phải bất biến Domain. Đặt `ITranslation` trên entity leak concern rendering vào Domain + ép `Rules.Domain → ResortConfig.Contracts` (Domain nội-cùng không nên phụ thuộc Contracts module khác). DTO-implements-ITranslation ở đúng tầng resolve (Application) → giữ `Rules.Domain` thuần, đúng ranh giới.
- Consequences: read-model reader map entity→DTO (đã làm ở `EfRulePublicationReader`); nếu sau này Faq cần i18n, cùng pattern (DTO implements ITranslation ở tầng Application của Faq). Không entity Domain nào implement ITranslation.
- Reversibility: Easy. Traceability: `design-modules/04-rules.md` §3/§8; QR-DV-002 (i18n ở Contracts); QR-AD-030; QR-N-038.


### QR-DV-008 — UI tương tác guest hiện tại là MOCKUP tĩnh (chưa nối backend) — FE.5 phải THAY, không "bổ sung"
- Status: Recorded (phát hiện khi chuẩn bị FE.5a, design-first — CHƯA sửa)
- Date: 2026-07-18
- Provenance/Evidence: đọc `guest-web/src/views/HomeView.vue` (~900 dòng): tất cả nội quy (`rules.rule1_1..`), FAQ (`faq.q1..q6`), chat (auto-reply `chat.autoResponse` + timeout giả), housekeeping (form + `startTimelineSimulation` 5s/10s/15s giả, ticketId random) đều **hardcode trong i18n JSON / client** — KHÔNG gọi bất kỳ endpoint nào trong 8 guest endpoint thật (§0 design-module 10). Dùng `stores/rules.ts` boolean `ruleConfirmed` (drift QR-AD-057). Có 40+ ngôn ngữ list + theme switcher (ngoài 4-locale thật en/vi/ko/zh). Router `/rules//faq//chat//housekeeping` đều trỏ `HomeView` (tab nội bộ).
- Deviation so với yêu cầu: Req 3/4/5/6 yêu cầu nội dung THẬT do lễ tân soạn (rules publication, FAQ tree, chat gom-theo-visit, housekeeping ticket gắn visit) + rule-gate server. UI hiện tại là **demo thị giác tĩnh**, phản ánh SAI hành vi sản phẩm (chat/housekeeping giả lập, force-read chỉ scroll-to-bottom-1-lần thay vì per-section scroll-end+MinReadSeconds, ack không ghi server). → FE.5 KHÔNG phải "thêm tính năng" mà là **thay mockup bằng journey nối backend thật** (kiến trúc B, QR-AD-057).
- Lý do (bản chất): giữ mockup = drift design↔UI nghiêm trọng (UI nói một đằng, backend làm một nẻo); là "fix ngọn" nếu chỉ vá rules.ts mà giữ phần còn lại giả. Phải thay tận gốc: shell thật + core + view nối 8 endpoint.
- Consequences: FE.5a là rewrite lớn (thay HomeView mockup) + xoá `rules.ts` + có thể mất demo thị giác hiện tại (theme switcher, 40 ngôn ngữ). Giữ lại phần dùng được (bố cục thẻ mobile, 4-locale). **CẦN user xác nhận phạm vi rewrite trước khi thực thi** (thay đổi lớn + xoá demo — không tự ý phá).
- Reversibility: Medium (mockup còn trong git history nếu muốn tham chiếu thị giác). Traceability: `design-modules/10-guest-journey.md` §0/§8; QR-AD-057; QR-N-082.
