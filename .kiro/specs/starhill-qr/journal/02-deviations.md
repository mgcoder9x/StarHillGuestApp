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
