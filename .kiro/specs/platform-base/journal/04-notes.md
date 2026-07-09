# 04 — Notes / Things To Know (giả định, cạm bẫy, trạng thái nền)

> Mọi điều AI/người đời sau CẦN biết trước khi động vào. Ưu tiên các thứ dễ gây hiểu nhầm hoặc dễ làm sai. Mỗi note ghi rõ đã verify hay là giả định.

---

### N-001 — Greenfield: `platform/` KHÔNG tồn tại trên đĩa
- Verified: ✅ `list_directory` phiên 2026-07-07 — gốc workspace chỉ có `.kiro/`, `.vscode/`, `docs/`, `foundation/`, `Reference/`, `resort-qr/`, `end.md`. KHÔNG có `platform/`.
- Ý nghĩa: chưa có một dòng code nào của base. Mọi đoạn C# trong `design.md` là **contract mục tiêu**, không phải trích từ file có sẵn. "Kiểm tra" build/test chỉ chạy được SAU khi làm task 1–2.
- Nguyên nhân: một checkpoint restore đã hoàn tác bản dựng thử trước đó (Domain + Application từng được tạo và test 13-pass, rồi biến mất).

---

### N-002 — Thứ tự nguồn sự thật (authority order)
- `design.md` = CHUẨN DUY NHẤT cho HOW. Nếu journal/tài liệu khác mâu thuẫn với design → design thắng, và phải sửa cái kia.
- `requirements.md` (R1–R34) = WHAT/WHY. `tasks.md` (21 task) = plan. `README.md` (spec) = bản đồ.
- `foundation/FOUNDATION-BLUEPRINT.md` (I1–I10) + `foundation/ARCHITECTURE-REVIEW.md` (F1–F35) = nguồn rationale ĐÓNG BĂNG.
- `review.md` = critique của AI review (D1–D14/R1–R7/T1–T5) ĐÓNG BĂNG.
- Reading order (khác authority order): `README → design → requirements → tasks` (+ journal này để hiểu "vì sao").

---

### N-003 — `review.md` CỐ Ý còn tên `BuildingBlocks` — đừng mass-rename
- Verified: ✅ grep phiên này — 4 file spec đã sạch `BuildingBlocks`; `review.md` vẫn còn (đó là báo cáo tại thời điểm TRƯỚC rename).
- Ý nghĩa: `review.md` là bằng chứng lịch sử. KHÔNG chạy find-replace `BuildingBlocks→Bedrock` trên nó (sẽ phá tính "ảnh chụp thời điểm"). Nếu cần, thêm ghi chú ở đầu review chứ không sửa nội dung.

---

### N-004 — PostgreSQL là DB production DUY NHẤT (đã codify)
- Verified: ✅ `design.md` §1.2 Non-goals.
- Ý nghĩa: cho phép `uint RowVersion` (DV-004), concurrency map `xmin` ở Infrastructure. SQLite CHỈ dùng cho test provider-agnostic; Testcontainers/PostgreSQL cho test Postgres-specific. Đừng thêm provider production khác mà không mở lại Non-goals + TO-006.

---

### N-005 — `CA1711` sẽ nổ trên `IIntegrationEventHandler<T>` (đuôi `EventHandler`)
- Verified: ✅ trong bản dựng thử trước (đã bị xoá) lỗi này từng làm fail build vì `TreatWarningsAsErrors=true`.
- Ý nghĩa: khi tạo interface này (task 3.2), phải `[SuppressMessage("Naming","CA1711", Justification=...)]` CÓ LÝ DO rõ (giữ tên đúng ngữ nghĩa seam) để build 0 warning. Đừng đổi tên interface chỉ để né analyzer.

---

### N-006 — Version package: "verify at install", KHÔNG assert tương thích
- Provenance: `review.md` Assumptions; `design.md` §17 dependencies.
- Đã nêu: FluentValidation `12.1.1`, SDK `10.0.301`, EF `10.x`. Đây là số ghi lại, PHẢI verify tương thích .NET 10 lúc `dotnet add` thật; không coi là cam kết.
- Verified: ✅ SDK `10.0.301` có mặt (`dotnet --list-sdks` phiên này).

---

### N-007 — Cổng chất lượng bất biến: build 0 warning + test xanh mỗi lát
- Provenance: `design.md` I10; `requirements.md` R31; `Directory.Build.props` (mục tiêu) `TreatWarningsAsErrors=true`.
- Ý nghĩa: KHÔNG merge/tiến bước khi còn warning. Architecture test (NetArchTest) là lưới an toàn cho ranh giới; mỗi luật phải có **negative control** (chứng minh vi phạm bị bắt).

---

### N-008 — Cạm bẫy DI: KHÔNG resolve scoped port từ root provider
- Provenance: `review.md` D5; `design.md` §9.4; đã ghi AD-011.
- Ý nghĩa: `design.md` bật `ValidateScopes=true` tường minh → mọi validator/startup code chạm scoped service PHẢI qua `IServiceScopeFactory`. Đây là lỗi thật đã suýt lọt trong mẫu validator cũ.

---

### N-009 — Ba quyết định nền đã CHỐT (đừng mở lại nếu không có lý do mới)
- `Bedrock.*` (AD-001), `Result = sealed class + Success/Failure` (AD-002), dead-letter = cột (AD-003).
- Verified: ✅ đã áp + đồng bộ header `design.md`, `README.md` §1, `tasks.md` header/notes; grep xác nhận không còn flag "đang mở".

---

### N-010 — Hai điểm triết lý — ĐÃ CHỐT (2026-07-07)
1. **Scrutor/DI trong `Bedrock.Application`** (TO-004) → **allow** (AD-018): abstraction chuẩn .NET + plumbing, không phải tech swap được.
2. **`Contracts` → `Bedrock.Application`** (TO-005/DV-001) → **extract** (AD-017): tách `IntegrationEvent` sang `Bedrock.Messaging.Contracts` zero-dep; `Contracts` KHÔNG còn trỏ lên Application.
- Cả hai giờ đã quyết; không còn "điểm loãng" decoupling nào treo. Nếu đảo lại phải mở AD mới (supersede).

---

### N-011 — Bản đồ phủ Correctness Property → task (để kiểm chứng nghiệm thu)
- Provenance: `tasks.md` Notes (verified). CP1(task 4/20), CP2(5.5), CP3(14), CP4/CP5/CP11(16.3), CP6/CP8/CP15(7.4), CP7(8.3), CP9(10.2), CP10(18), CP12(20), CP13(5.2), CP14(6.4).
- Ý nghĩa: mỗi CP1–CP15 đều có task test tương ứng. Khi nghiệm thu, đối chiếu bảng này; nếu thêm CP mới → phải thêm task test + cập nhật bảng.

---

### N-012 — Testcontainers/Docker cần cho một số test
- Provenance: `tasks.md` (task 7.4 outbox/inbox + race, task 8.3 rotation race, task 14 adapter RabbitMQ).
- Ý nghĩa: các test Postgres-specific/adapter cần Docker. Nếu môi trường thiếu Docker → skip-CÓ-ĐIỀU-KIỆN (không xoá test). Đừng coi "skip vì thiếu Docker" là "đã kiểm chứng".

---

### N-013 — Định dạng spec phải luôn valid (0 diagnostic)
- Verified: ✅ sau mỗi đợt sửa phiên này, `getDiagnostics` cho `requirements.md`/`design.md`/`tasks.md`/`README.md` = No diagnostics.
- Ý nghĩa: sau BẤT KỲ sửa nào chạm 3 file spec chính, chạy lại `getDiagnostics` để đảm bảo không vỡ format Kiro (heading `### Requirement N: Title`, EARS, Correctness Properties có `**Validates: Requirements X**`, tasks có `## Task Dependency Graph` với Mermaid + JSON `waves`).

---

### N-014 — Rate-limit là HAI TẦNG (đừng nhầm là một)
- Provenance/Evidence: `design.md` §3.5 slot #9 (ASP.NET `RateLimiter` middleware) + §5.4 port `IRateLimitStore` (verified grep); `review.md` D13 "two-tier rate-limit clarification".
- Ý nghĩa:
  - **Tầng biên (edge):** ASP.NET `RateLimiter` middleware trong `Bedrock.Api`, partition theo **IP thật đã resolve** (sau ForwardedHeaders slot #1). Chặn abuse thô ở cổng.
  - **Tầng nghiệp vụ (distributed):** port `IRateLimitStore.TryAcquireAsync(partition, limit, window)` cho giới hạn tùy biến theo khóa nghiệp vụ (vd theo user/tenant), có thể dùng adapter Redis.
- Cạm bẫy: đừng gộp hai thứ này làm một; middleware biên KHÔNG thay thế `IRateLimitStore` và ngược lại.

---

### N-015 — Health check tách liveness/readiness, pluggable, lõi không biết công nghệ
- Provenance/Evidence: `requirements.md` R34; `design.md` §9.6 + slot #11 (`MapBedrockHealth` → `/health/live` + `/health/ready`, verified grep); mỗi module/persistence đóng góp check qua tag `ready`.
- Ý nghĩa: `/health/live` = process còn sống (không phụ thuộc dependency); `/health/ready` = sẵn sàng nhận traffic (gồm check DB tag `ready`, timeout ngắn). Orchestrator dùng để restart (live) vs route traffic (ready).
- Cạm bẫy: đừng nhét check dependency nặng vào `live` (sẽ bị restart oan khi dependency chập chờn).

---

### N-016 — Lượt validate journal (2026-07-07)
- Đã đối chiếu `design.md` (grep nhãn `[Tinh chỉnh/Bổ sung so với Blueprint]` + các mục trọng yếu) với journal.
- Kết quả: phát hiện & BỔ SUNG 5 quyết định còn thiếu → **AD-012** (UoW reentrancy), **AD-013** (pipeline order), **AD-014** (`IEndpointModule`), **AD-015** (serialization contract), **AD-016** (atomic claim + backoff); thêm **N-014** (rate-limit 2 tầng), **N-015** (health).
- Xác nhận đã có từ trước: DV-001 (Contracts→Application), AD-004 (outbox per-module), DV-002 (bỏ Repository<T>()), AD-007 (domain events) — khớp 4 nhãn deviation/bổ sung mà design tự đánh dấu.
- Verified: `getDiagnostics` toàn bộ journal = No diagnostics.

---

### N-017 — Lượt validate journal #2 (2026-07-07)
- Phạm vi: soi tầng `requirements.md` (các tiêu chí do review R1–R7 đổi/thêm) — nguồn chưa mined kỹ ở lượt #1 (vốn tập trung `design.md`).
- Đối chiếu từng tiêu chí: R7.4 (reentrancy → đã có AD-012), R8.5/R8.6 (backoff/claim → AD-016), R9.4 (handler re-publish qua `IOutboxWriter` → là **hệ quả** của AD-005, không phải quyết định mới), R16.4 (fail-loud → AD-009), R13.1 (RequiredPorts → AD-011).
- Phát hiện & BỔ SUNG: **DV-008** (R32.4 bỏ tham chiếu FE trực tiếp — deviation tầng requirement, verify qua review R5 + đọc R32.4).
- Kết luận: sau lượt #2, journal đã phủ toàn bộ deviation/decision verify được trong `design.md` + `requirements.md` + `review.md`. Không thêm mục nào nếu không có nguồn kiểm chứng (tránh bịa).
- Tổng bản ghi hiện tại: AD 16, DV 8, TO 9, N 17.

---

### N-018 — Lượt cập nhật journal #3 (2026-07-07) — áp quyết định extract/allow
- Kích hoạt: user chốt "1: allow, 2: extract" + tên assembly `Bedrock.Messaging.Contracts` + "áp đi".
- Thay đổi đã áp (verified qua str_replace phiên này):
  - `design.md`: §3.2 layout (+project `Bedrock.Messaging.Contracts`), §3.3 matrix (+row zero-dep; `Application` ref thêm nó; `Contracts` ref nó thay vì Application) + footnote¹ viết lại, §4.5 `IntegrationEvent` đổi namespace, components (+bullet mới, gỡ `IntegrationEvent` khỏi bullet Application).
  - `requirements.md`: R17.2 ghi rõ `IntegrationEvent` ở `Bedrock.Messaging.Contracts`.
  - journal: +AD-017 (extract), +AD-018 (allow), DV-001 → Superseded by AD-017, TO-004/TO-005 → Resolved, N-010 → đã chốt.
- `tasks.md` (ĐÃ xong lượt này): task 2 nay tạo cả `Bedrock.Messaging.Contracts`; task 3.2 ghi rõ `IntegrationEvent` đến từ assembly mới (không định nghĩa lại); task 4 cập nhật luật dependency; node graph T2 + wave 2 rationale cập nhật.
- Verified: `getDiagnostics` toàn bộ 3 file spec + 4 file journal = No diagnostics (chạy cuối lượt #3).
- Tổng bản ghi: AD 18, DV 8 (DV-001 superseded), TO 9 (TO-004/005 resolved), N 18.

---

### N-019 — Task 1 + Task 2 hoàn tất và VERIFY THẬT (2026-07-08)
- Verified: ✅ `dotnet test Platform.slnx` → **Passed: 27, Failed: 0, Total: 27** (chạy phiên này, không phải suy đoán).
- Đã tạo trên đĩa: khung solution (`global.json` SDK 10.0.301, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig` + CA1000 suppress, `Platform.slnx`); `Bedrock.Domain` đầy đủ (Entities: `Entity`/`AuditableEntity`/`Abstractions` IAuditable/ISoftDeletable/IHasConcurrencyToken; Events: `IDomainEvent`; Guards: `Guard`; Primitives: `ValueObject`; Results: `Result`/`Result<T>`/`Error`/`ErrorType`/`CommonErrors`/`ConcurrencyConflictException`); `Bedrock.Messaging.Contracts` (`IntegrationEvent`); `tests/Bedrock.UnitTests` (27 test: Result/Entity/ValueObject/Guard/IntegrationEvent).
- Hai lỗi build thật gặp và fix tận gốc (ghi đủ ở AD-019, AD-020): CA1000 trên `Result<T>` factory (suppress có lý do — đúng chủ đích thiết kế) + CS0111 trùng overload `NotFound` (đổi tên `NotFoundGeneric`, không xoá tiện ích).
- Một lỗi TEST (không phải lỗi code Guard) tự sửa: `Assert.Throws<ArgumentException>` không chấp nhận `ArgumentNullException` (kiểu dẫn xuất) → đổi `Assert.ThrowsAny<ArgumentException>` (đúng hợp đồng thật của `ArgumentException.ThrowIfNullOrWhiteSpace`).
- Trạng thái greenfield (N-001) nay ĐÃ THAY ĐỔI: `platform/` KHÔNG còn trống — có code thật, build xanh. Ghi chú này SUPERSEDES phần "chưa có code" của N-001 cho 2 project đã xong; N-001 vẫn đúng cho phần còn lại (Infrastructure/Api/Adapters/Modules/Host chưa tạo).
- Điểm tiếp theo: task 3 (`Bedrock.Application` — ports/seams/behaviors) theo `tasks.md`.

---

### N-020 — Task 3 (`Bedrock.Application`) hoàn tất và VERIFY THẬT (2026-07-08)
- Verified: ✅ `dotnet test Platform.slnx` → **Passed: 37, Failed: 0, Total: 37** (chạy phiên này). Build 0 warning.
- Đã tạo: `Bedrock.Application` (ref CHỈ `Bedrock.Domain` + `Bedrock.Messaging.Contracts` + FluentValidation — đúng matrix §3.3):
  - `DependencyInjection/ServiceMarkers` (IScopedService/ISingletonService/ITransientService/IManualRegistration).
  - `Ports/Time/IClock`, `Ports/Users/ICurrentUser` (Roles/Permissions/TenantId/SessionId), `Ports/Html/IHtmlSanitizer` (AD-021), `Ports/Security/{ITokenGenerator,IPasswordHasher,IRefreshTokenStore+RefreshTokenSnapshot}`, `Ports/Persistence/{IRepository (no Query),IUnitOfWork (no Repository<T>, reentrancy R7.4)}`.
  - `Messaging/{IOutboxWriter,IIntegrationEventHandler}` + `Messaging/Dispatch/{OutboxMessage,IOutboxDispatcher,IEventBusPublisher,IInboxStore,IIntegrationEventTypeRegistry}`.
  - `Events/{IDomainEventHandler,IDomainEventDispatcher}`; `UseCases/{IUseCase,ICommandUseCase,Paging}`; `Behaviors/{ValidationUseCaseDecorator,ValidationCommandUseCaseDecorator}`.
- QUYẾT ĐỊNH THIẾT KẾ (theo design §5.7, KHÔNG phải phát sinh mới): ports là interface THUẦN — KHÔNG kế thừa DI marker (khác foundation cũ nơi ICurrentUser:IScopedService). Lifetime quyết định ở lúc đăng ký implementation (marker gắn trên impl ở Infrastructure). Ports sạch, không phụ thuộc DI-marker semantics.
- `CA1711` (đuôi EventHandler) xử lý bằng `[SuppressMessage]` cấp interface (không tắt toàn cục — khác CA1000/AD-019) trên `IIntegrationEventHandler`/`IDomainEventHandler` — chính xác đúng chỗ, có Justification.
- Điểm tiếp theo: task 4 (Architecture Tests — NetArchTest lưới ranh giới + no-business-in-core, CP1).
- Tổng bản ghi journal: AD 21, DV 8, TO 9, N 20.

---

### N-021 — Task 4 (Architecture Tests) hoàn tất và VERIFY THẬT (2026-07-08)
- Verified: ✅ `dotnet test Platform.slnx` → UnitTests **37** + ArchitectureTests **11** = **48 passed, 0 failed**, build 0 warning.
- Đã tạo `tests/Bedrock.ArchitectureTests` (NetArchTest.Rules 1.3.2):
  - `NoBusinessInCoreTests` — reflection scan tên type/namespace 3 assembly lõi (Domain/Messaging.Contracts/Application) tìm token `guest|room|resort|admin|staff`; + negative control (seed `GuestRoomLeakSample`/`ResortAdminStaffSample` → scanner bắt đúng 2).
  - `DependencyRuleTests` — Domain & Messaging.Contracts zero-dep; Application không ref Infrastructure/Api/EF/ASP.NET/Npgsql; + negative control (Application→Domain PHẢI bị "NotHaveDependencyOn" bắt = IsSuccessful false).
  - `UseCaseSeamTests` — CP11: use case không ref `Messaging.Dispatch` (vacuous vì chưa có use case) + negative control `LeakyUseCase` (giữ `IEventBusPublisher`) bị bắt.
- GIỚI HẠN CÔNG CỤ đã ghi rõ (AD-022): NetArchTest/reflection chỉ phủ tên/namespace của CP1; **string literal trong method body CHƯA phủ** → hoãn task 20 bằng source/IL scan. Docstring `NoBusinessInCoreTests` nói rõ điều này — KHÔNG tuyên bố phủ literal.
- Luật 2 (Api⊥Infra), 3 (Adapters), 4 (Module boundary), 5 (Host) HOÃN tới task 5.5/14/16.3 vì project chưa tồn tại — KHÔNG viết luật rỗng giả tạo (đúng tasks.md).
- Tiến độ: task 1 ✅ · 2 ✅ · 3 ✅ · 4 ✅ (hết Giai đoạn 0). Kế tiếp: task 5 (P0 — `Bedrock.Api` cơ chế HTTP thuần, không ref Infrastructure).
- Tổng bản ghi journal: AD 22, DV 8, TO 9, N 21.

---

### N-022 — Task 5.1 (Bedrock.Api scaffold + ProblemDetails + ErrorTypeToHttp) VERIFY THẬT (2026-07-08)
- Verified: ✅ `dotnet test Platform.slnx` → UnitTests 37 + ArchitectureTests 11 + Api.Tests 9 = **57 passed, 0 failed**, build 0 warning.
- Đã tạo: `Bedrock.Api` (`Microsoft.NET.Sdk` + `FrameworkReference Microsoft.AspNetCore.App`, ref CHỈ `Bedrock.Application` — F14); `ErrorHandling/ErrorTypeToHttp` (map ErrorType→HTTP: Validation400/Unauthorized401/Forbidden403/NotFound404/Conflict409/Failure500); `ErrorHandling/ProblemDetailsBuilder` (title=Message, extensions code+traceId+errors — F20/F21).
- Chi tiết cấu trúc tôi TỰ QUYẾT (spec không nêu tên test project cho Api): (1) tạo project riêng `tests/Bedrock.Api.Tests` cho test tầng Api (thay vì nhét vào `Bedrock.UnitTests` — vì UnitTests không nên kéo ASP.NET framework); (2) test project Api PHẢI khai `FrameworkReference Microsoft.AspNetCore.App` mới nạp được type ASP.NET (ProblemDetails) từ Bedrock.Api — đây là yêu cầu kỹ thuật thật của test project tham chiếu ASP.NET class library. Cả hai là quyết định cấu trúc nhỏ, reversible, không lệch thiết kế.
- CÒN LẠI của task 5 (chưa làm): 5.2 (ObservabilityOptions + PathMasker + dùng ở exception handler & request logging — CP13), 5.3 (AddBedrockAuthCore + JwtKeyRingOptions + JwtBearer — cần pin package `Microsoft.AspNetCore.Authentication.JwtBearer`), 5.4 (MapBedrockApi thứ tự pipeline §3.5 + MapBedrockHealth + IEndpointModule), 5.5 (arch test Api⊥Infrastructure — CP2).
- Tổng bản ghi journal: AD 22, DV 8, TO 9, N 22.

---

### N-023 — Task 5.2 (ObservabilityOptions + PathMasker) VERIFY THẬT (2026-07-08)
- Verified: ✅ `dotnet test Platform.slnx` → ArchitectureTests 11 + UnitTests 37 + Api.Tests 16 = **64 passed, 0 failed**, build 0 warning.
- Đã tạo: `Observability/ObservabilityOptions` (`MaskedPathPrefixes` get-only collection để config binding — tránh CA1819; `MaskPlaceholder`); `Observability/PathMasker` (che đuôi path khi khớp prefix nhạy cảm, case-insensitive, prefix TỪ OPTIONS — gỡ hardcode F2). +7 test PathMasker.
- Bản chất F2/F15 (không vá ngọn): masker là MỘT component dùng chung, prefix do app khai qua options — lõi không biết path nghiệp vụ. Việc "dùng ở request-logging + exception handler" (CP13) sẽ được nối ở 5.4 (middleware) — masker này là component chung mà cả hai sẽ inject.
- CÒN LẠI task 5: 5.3 (AddBedrockAuthCore + JwtKeyRingOptions + JwtBearer — cần pin `Microsoft.AspNetCore.Authentication.JwtBearer`), 5.4 (MapBedrockApi pipeline §3.5 + MapBedrockHealth + IEndpointModule; nối PathMasker vào 2 middleware → chốt CP13), 5.5 (arch test Api⊥Infrastructure — CP2).
- Tổng bản ghi journal: AD 22, DV 8, TO 9, N 23.

---

### N-024 — Task 5.3 (auth mechanism) VERIFY THẬT (2026-07-08)
- Verified: ✅ `dotnet test Platform.slnx` → UnitTests 37 + ArchitectureTests 11 + Api.Tests 21 = **69 passed, 0 failed**, build 0 warning.
- Package thật (không bịa, NuGet resolve): `Microsoft.AspNetCore.Authentication.JwtBearer` **10.0.9** (Bedrock.Api), `Microsoft.AspNetCore.TestHost` **10.0.9** (Bedrock.Api.Tests) — cùng dòng .NET 10.
- Đã tạo: `Authentication/JwtKeyRingOptions` (+`JwtSigningKey`, F22 key-ring: ActiveKid ký / all Keys verify); `Authentication/HttpContextCurrentUser` (impl `ICurrentUser` đọc claims, không hardcode role — F3); `Authentication/BedrockAuthExtensions.AddBedrockAuthCore` (JwtBearer + IssuerSigningKeyResolver theo kid + 401/403 ProblemDetails events + `ICurrentUser` binding); `Observability/CorrelationContext` (helper 1-nguồn traceId cho CP10 — 5.4 middleware sẽ SET, mọi nơi đọc qua Resolve).
- Test: `HttpContextCurrentUserTests` (claim mapping + anonymous), `AuthMechanismTests` (TestHost: 401 no-token / 403 authenticated-missing-role với JWT ký thật / 200 valid-token) — chứng minh cơ chế end-to-end, không chỉ đơn vị.
- Quyết định phát sinh: AD-023 (MapInboundClaims=false + claim names JWT-native) — RÀNG BUỘC task 9 (IJwtTokenService) + 12.3 (external-auth) + 16 (Identity) phải emit đúng `sub`/`role`/`permission`/`tenant_id`/`sid`.
- CÒN LẠI task 5: 5.4 (MapBedrockApi thứ tự pipeline §3.5 + CorrelationId middleware SET Items → chốt CP10 + PathMasker nối vào exception+request-logging → chốt CP13 + MapBedrockHealth + IEndpointModule), 5.5 (arch test Api⊥Infrastructure — CP2).
- Tổng bản ghi journal: AD 23, DV 8, TO 9, N 24.

---

### N-025 — Anti-drift mechanism + task 5.5 (CP2) VERIFY THẬT (2026-07-08)
- Verified: ✅ `dotnet test Platform.slnx` → UnitTests 37 + ArchitectureTests 14 + Api.Tests 24 = **75 passed, 0 failed**, build 0 warning.
- Cơ chế chống drift "cực mạnh" (theo yêu cầu user): biến quyết định/invariant thành GUARD TEST chạy mỗi build → lệch là FAIL BUILD. Tài liệu đầy đủ: `journal/05-anti-drift.md` (5 lớp phòng thủ, KEYSTONE RULE "không quyết định code-enforceable nào thiếu guard test", bản đồ CP1–15 → test, bản đồ AD → test, vòng lặp bắt buộc, khoảng hở chưa auto-enforce).
- Thêm mới (turn journal → executable):
  - `DecisionGuardTests` (ArchitectureTests): AD-002 (Result=sealed class), AD-017 (IntegrationEvent ở Messaging.Contracts, không ở Application), AD-021 (IHtmlSanitizer ở Ports.Html) — 3 guard reflection.
  - `ApiBoundaryTests` (Bedrock.Api.Tests, +NetArchTest.Rules): CP2 Api⊥Infrastructure + Api⊥EF/Npgsql + negative control (engine bắt Api→Application). = TASK 5.5 HOÀN TẤT.
- Trạng thái enforce hiện tại: CP1(tên)/CP2/CP11 ✅ ENFORCED; CP13 🟡 PARTIAL (masker xong, wiring task 5.4); AD-002/017/021/023 ✅ ENFORCED. Còn lại PENDING theo task (đã liệt kê trung thực trong 05-anti-drift.md, không giấu khoảng hở).
- Task 5: 5.1 ✅ · 5.2 ✅ · 5.3 ✅ · **5.5 ✅** (làm sớm cùng anti-drift vì Api đã tồn tại). CÒN 5.4 (MapBedrockApi pipeline §3.5 + CorrelationId middleware → chốt CP10 + wiring PathMasker → chốt CP13 + MapBedrockHealth + IEndpointModule).
- Tổng bản ghi journal: AD 23, DV 8, TO 9, N 25 + file mới 05-anti-drift.md.

---

### N-026 — Task 5.4 xong → P0 (task 5) HOÀN TẤT, VERIFY THẬT (2026-07-08)
- Verified: ✅ `dotnet test Platform.slnx` → ArchitectureTests 14 + UnitTests 37 + Api.Tests 29 = **80 passed, 0 failed**, build 0 warning.
- Đã tạo: `Endpoints/IEndpointModule` (discovery §6); middleware theo §3.5: `Observability/CorrelationIdMiddleware` (#2, set Items+header 1-nguồn), `ErrorHandling/ExceptionHandlingMiddleware` (#3, ConcurrencyConflict→409, mask path, ProblemDetails), `HttpSecurity/SecurityHeadersMiddleware` (#5), `Observability/RequestLoggingMiddleware` (#6, masked); `Observability/ApiLog` ([LoggerMessage] source-gen tránh CA1848); `Health/HealthEndpoints.MapBedrockHealth` (/live no-check + /ready tag "ready"); `ProblemDetailsWriter` (DRY — auth+exception dùng chung); `BedrockApiExtensions.AddBedrockApi`+`UseBedrockApi` (áp pipeline slot Bedrock sở hữu; #1/#4/#8/#9 = task 11).
- Test tích hợp (TestHost + capturing logger): liveness/readiness 200; IEndpointModule discovery; correlation header; **CP10** (header==body.traceId với client-provided id); **CP13** (masked "/r/***", KHÔNG có raw token).
- 2 lỗi/finding thật, fix tận gốc: (1) **CA1873** — đối số log là method-call inline → hoist local + guard `IsEnabled` (chỉ tính khi level bật); (2) **AD-024** — framework `Microsoft.AspNetCore.Hosting` tự log raw path → posture Host phải hạ Warning; Bedrock request-logging (masked) thay thế. Đây là F15 nhìn đúng bản chất (không chỉ mask site của ta).
- CP10/CP13 nâng PARTIAL/PENDING → **ENFORCED** trong 05-anti-drift.md.
- Task 5 (P0): 5.1 ✅ · 5.2 ✅ · 5.3 ✅ · 5.4 ✅ · 5.5 ✅ → **P0 XONG**. Kế tiếp: **P1 / task 6** (`Bedrock.Infrastructure` — PlatformDbContext base + EfRepository/EfUnitOfWork + domain-event dispatch; cần EF Core + Npgsql).
- Tổng bản ghi journal: AD 24, DV 8, TO 9, N 26.

---

### N-027 — Task 6 (`Bedrock.Infrastructure` — EF base) HOÀN TẤT, VERIFY THẬT (2026-07-08)
- Verified: ✅ `dotnet test Platform.slnx` → ArchitectureTests **16** + UnitTests 37 + Infrastructure.Tests **14** + Api.Tests 29 = **96 passed, 0 failed**, build 0 warning.
- Package thật (NuGet resolve, pin vào `Directory.Packages.props`): `Microsoft.EntityFrameworkCore` **10.0.9**, `Npgsql.EntityFrameworkCore.PostgreSQL` **10.0.2**, `EFCore.NamingConventions` **10.0.1**, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` **10.0.9**, (test) `Microsoft.EntityFrameworkCore.Sqlite` **10.0.9** + transitive pin `SQLitePCLRaw.lib.e_sqlite3` **3.50.3** (AD-028).
- Đã tạo `src/Bedrock.Infrastructure` (ref CHỈ `Bedrock.Application` — matrix §3.3; guard Infrastructure⊥Api ở `DependencyRuleTests`):
  - `Persistence/PlatformDbContext` (abstract): override `SaveChangesAsync(bool,ct)` = vòng dispatch domain-event (design §7.5) → soft-delete → audit → 1 `base.SaveChanges` (CP14); `OnModelCreating` = soft-delete query filter (agnostic) + concurrency-token map **conditional Npgsql** (RowVersion→`xmin`/`xid`, DB-gen, IsConcurrencyToken — vì Npgsql EF10 đã bỏ `UseXminAsConcurrencyToken()`); `SaveChanges` đồng bộ bị chặn (AD-026); `MaxDomainEventDispatchDepth` virtual=25, vượt → ném (AD-027).
  - `Persistence/EfRepository<T>` (không phơi IQueryable — F9), `Persistence/EfUnitOfWork` (reentrancy-aware AD-012 + map `DbUpdateConcurrencyException`→`ConcurrencyConflictException`), `Persistence/DomainEventDispatcher` (generic invoker, không MethodInfo.Invoke — AD-025), `Time/SystemClock`, `DependencyInjection/BedrockPersistenceExtensions.AddBedrockPersistence<TContext>` (snake_case + repo/UoW/dispatcher scoped + DB readiness check tag `ready` timeout 5s).
- Test SQLite (DB quan hệ thật, không Docker — persistence §6): audit add/modify, soft-delete filter + IgnoreQueryFilters, snake_case DDL (pragma), transaction rollback/commit, reentrancy (nested join + nested rollback), concurrency-map, sync-blocked; CP14 (handler-effect atomic / handler-throw rollback / max-depth throw / no-handler no-op). Postgres-specific (xmin thật, partial index, race đa-connection) HOÃN Testcontainers (N-012).
- 1 finding thật fix tận gốc: dispatcher ban đầu dùng `MethodInfo.Invoke` → bọc lỗi handler vào `TargetInvocationException` (test CP14 FAIL) → đổi sang generic invoker (AD-025). 1 finding bảo mật: EF Sqlite kéo SQLitePCLRaw 2.1.11 lỗ hổng HIGH → pin 3.50.3 (AD-028).
- Task 6: 6.1 ✅ · 6.2 ✅ · 6.3 ✅ · 6.4 ✅ → **P1/task 6 XONG**. Kế tiếp theo `tasks.md`: task 7 (Outbox/Inbox per-module + dispatcher claim — cần Testcontainers cho CP6/CP8/CP15) hoặc task tiếp trong build order P1.
- Tổng bản ghi journal: AD 28, DV 8, TO 9, N 27.

---

### N-028 — Task 7.1 + 7.2 (Outbox schema + EF config helper + `EfOutboxWriter`) VERIFY THẬT (2026-07-08)
- Verified: ✅ `dotnet test Platform.slnx` → ArchitectureTests 16 + UnitTests 37 + Infrastructure.Tests **19** + Api.Tests 29 = **101 passed, 0 failed**, build 0 warning.
- Đã tạo trong `src/Bedrock.Infrastructure/Persistence/Messaging/`:
  - `InboxMessage` (persistence-only, ẩn trong Infrastructure — F19; PK tổ hợp `(MessageId, Consumer)`).
  - `OutboxInboxModelBuilderExtensions.AddOutboxInbox(isNpgsql, schema?)` — map `outbox_message` + `inbox_message` vào chính DbContext/schema module (per-module, design §4.6); partial index `ix_outbox_pending` (filter `processed_at IS NULL AND dead_lettered_at IS NULL`); payload `jsonb` CHỈ khi Npgsql (SQLite giữ text). Đổi chữ ký so với design no-arg → **DV-009** (root cause: ModelBuilder ext không truy cập được provider; jsonb provider-specific).
  - `OutboxSerialization` (options cố định camelCase + bỏ null — AD-015) + `EfOutboxWriter` (impl `IOutboxWriter`: serialize theo KIỂU THỰC của event, `OutboxMessage.Id = event.Id` — **AD-029**, `CorrelationId` từ `Activity.Current`, ghi vào ChangeTracker KHÔNG commit).
  - `AddBedrockPersistence` nay `TryAddScoped<IOutboxWriter, EfOutboxWriter>` (dùng chung PlatformDbContext/scope → enqueue cùng transaction với state).
- Test SQLite (5 mới): enqueue+state commit atomically / rollback cùng nhau (nền CP6), serialize payload + copy field event (Id==event.Id), capture correlation từ Activity, snake_case cột outbox/inbox. Race đa-connection + Postgres thật (CP6 full/CP15) HOÃN Testcontainers task 7.4 (N-012).
- CP6 nâng PENDING → **PARTIAL** (same-transaction atomic đã chứng minh trên SQLite — DB quan hệ thật; publish-path + Postgres → 7.4).
- CÒN LẠI task 7: **7.3** (`IOutboxDispatcher` claim/backoff/dead-letter + `EfInboxStore` idempotent + `IIntegrationEventTypeRegistry` + unit test backoff/threshold), **7.4** (Testcontainers — CP6/CP8/CP15), **7.5** (retention/cleanup job). Lưu ý claim nguyên tử (skip-locked) là Postgres-specific → logic backoff/threshold unit-test được không-Docker, race thật cần 7.4.
- Tổng bản ghi journal: AD 29, DV 9, TO 9, N 28.

---

### N-029 — Task 7.3 (outbox dispatcher + inbox store + type registry) VERIFY THẬT (2026-07-09)
- Verified: ✅ `dotnet test Platform.slnx` → ArchitectureTests 16 + UnitTests 37 + Infrastructure.Tests **27** + Api.Tests 29 = **109 passed, 0 failed**, build 0 warning.
- Đã tạo trong `src/Bedrock.Infrastructure/Persistence/Messaging/`:
  - `OutboxDispatcherOptions` (BatchSize=100, MaxAttempts=10, BaseDelay=5s, MaxDelay=30m; named-options theo context → per-module tinh chỉnh riêng).
  - `OutboxBackoff` (static, non-generic — tránh CA1000): `ComputeBackoff = BaseDelay×2^(errorCount-1)` cắp `MaxDelay`; unit-test giá trị chính xác.
  - `EfOutboxDispatcher<TContext>` (impl `IOutboxDispatcher`): claim batch pending tới hạn trong transaction → publish `IEventBusPublisher` → mark `processed_at`; fail → `error_count`++ + `next_attempt_at`=now+backoff+jitter (jitter TẤT ĐỊNH theo Id, KHÔNG RNG → test được + né CA5394), vượt `MaxAttempts` → `dead_lettered_at`. Bắt rộng (`CA1031` suppress có lý do) để cách ly poison, không rethrow.
  - `EfInboxStore` (impl `IInboxStore`): idempotency phía consumer, stage `InboxMessage` (không tự commit) → mark + business cùng transaction; guard trùng cấp DB là PK `(message_id, consumer)`.
  - `IntegrationEventTypeRegistry` (impl `IIntegrationEventTypeRegistry`): quét assembly `*.Contracts`, đọc `EventType` qua `RuntimeHelpers.GetUninitializedObject` (không chạy ctor — EventType là mã ổn định/literal); trùng EventType → ném fail-fast; EventType lạ → `Resolve` trả null (dead-letter, không crash — R17.3).
  - `DependencyInjection/OutboxDispatcherExtensions`: `AddOutboxDispatcher<TContext>(configure?)` (+ inbox store) và `AddIntegrationEventRegistry(params Assembly[])` (singleton).
- `InternalsVisibleTo Bedrock.Infrastructure.Tests` thêm vào csproj Infrastructure → test thuần `OutboxBackoff` (helper internal) mà không phơi ra API công khai.
- Test SQLite (8 mới): publish→marks processed; fail→error+next_attempt; dead-letter sau MaxAttempts (advance clock để claim lại); dead-letter/chưa-tới-hạn KHÔNG bị claim; ComputeBackoff exponential+capped; inbox first-true-then-false + khác-consumer=lần-đầu; registry resolve known/null.
- 1 lỗi build thật fix tận gốc: **CA1859** (field registry nên dùng `Dictionary` cụ thể thay `IReadOnlyDictionary` — perf). 1 finding provider thật → **DV-010** (SQLite không dịch được so sánh/sắp xếp `DateTimeOffset` → claim tách nhánh: Npgsql lọc+sort ở SQL, provider khác client-side).
- Ranh giới task giữ đúng (không over-claim): claim exclusive đa-instance (Postgres `FOR UPDATE SKIP LOCKED`) + race 2-dispatcher (CP15) + CP6 full/CP8 vẫn thuộc **task 7.4 (Testcontainers)** — logic backoff/threshold/dead-letter/registry/inbox đã unit-test không-Docker.
- CÒN LẠI task 7: **7.4** (Testcontainers — CP6 full/CP8/CP15), **7.5** (retention/cleanup job). Kế tiếp thực chất theo build order: 7.4/7.5, hoặc nhánh song song wave 6 (task 8 refresh-token store, 9 crypto/JWT, 10 DI+startup validation, 11 HTTP hardening) đều đã đủ tiền đề (task 6 xong).
- Tổng bản ghi journal: AD 29, DV 10, TO 9, N 29.

---

### N-030 — ⚠️ Repo-trong-repo: `StarHillGuestApp/StarHillGuestApp/` là GIT REPO riêng (KHÔNG được xoá) — cần user reconcile cấu trúc
- Verified: ✅ phiên 2026-07-09 — `file_search` trả 2 đường dẫn cho `01-decisions.md`/`tasks.md`: (a) GỐC `…/StarHillGuestApp/.kiro/specs/platform-base/…` và (b) LỒNG `…/StarHillGuestApp/StarHillGuestApp/.kiro/specs/platform-base/…`. `grep_search` với glob `**/platform-base/journal/*.md` khớp bản LỒNG, và bản lồng **chỉ có DV-001..008** (THIẾU DV-009/DV-010 + N-029 tôi vừa thêm) → bản lồng là ẢNH CHỤP CŨ/stale.
- Ý nghĩa: code `platform/` sống + journal tôi đang bảo trì đều ở cây GỐC (`…/StarHillGuestApp/…`, KHÔNG phải bản lồng). Mọi edit phiên này (7.3, anti-drift) đều vào GỐC — đã xác nhận qua đường dẫn tuyệt đối.
- Rủi ro: nếu ai đó mở nhầm bản lồng để sửa → công sức mất/nhầm nguồn sự thật. Đây là drift ở tầng filesystem (không phải nội dung).
- ĐÍNH CHÍNH (2026-07-09, sau điều tra `list_directory` depth 2): bản lồng KHÔNG phải "bản sao rác của journal" — nó là **MỘT GIT REPO đầy đủ** (`StarHillGuestApp/StarHillGuestApp/.git/`) chứa **dự án `resort-qr/`** (frontend/src/tests), `foundation/`, `Reference/`, `docs/`, `end.md`, và bản `.kiro/specs/platform-base` CŨ (journal tới DV-008). Nó **KHÔNG có `platform/`**. → TUYỆT ĐỐI KHÔNG xoá (sẽ phá repo + dự án thật + lịch sử git).
- Hành động ĐÚNG: đây là vấn đề **cấu trúc repo (repo-trong-repo / hai checkout)** — chỉ USER biết cây nào canonical. AI KHÔNG được tự quyết/gỡ. Khuyến nghị: user xác định repo chuẩn; nếu cây gốc `…/StarHillGuestApp/` (có `platform/`) là workspace thật thì cân nhắc di chuyển/hợp nhất bản lồng RA NGOÀI khối lồng bằng thao tác git có kiểm soát (không phải xoá thô). `JournalConsistencyTests` chỉ gác bản GỐC (discovery đi lên cây từ `platform/tests/` → gặp `.kiro` gốc trước) → an toàn dù bản lồng còn đó.
- Provenance/Evidence: `list_directory` `…/StarHillGuestApp/StarHillGuestApp/` depth 2 phiên 2026-07-09 — thấy `.git/`, `resort-qr/`, `foundation/`, `Reference/`, `.kiro/`, `end.md`, KHÔNG có `platform/`; đối chiếu số bản ghi DV (lồng=8 vs gốc=10).

---

### N-031 — Phiên củng cố anti-drift + audit journal (2026-07-09)
- Kích hoạt: user yêu cầu "cần 1 cách cực mạnh để tránh drift" + xác nhận thư mục 4-loại-ghi-chép (đã tồn tại từ trước: `01`–`04` + `05-anti-drift` + README).
- Đánh giá bản chất: thư mục journal + 4 loại (AD/DV/TO/N) ĐÃ đầy đủ; điểm yếu gốc là **L4/L5 thủ công** (journal có thể tự lệch mà không cổng nào bắt). Bằng chứng lệch thật: AD-003 thiếu khỏi bảng guard 05.
- Đã làm (verified):
  - Thêm **AD-030** + `JournalConsistencyTests` (INV-1..5) → L4 nay TỰ ĐỘNG (build gate). Chi tiết: 05-anti-drift.md mục "Cổng journal-consistency".
  - Viết lại bảng guard AD trong 05 thành **token tường minh AD-001..AD-030** (bỏ dạng nén `AD-004/005/...` khó quét) + bổ sung AD-003; cập nhật trạng thái sau task 7.3 (AD-006/015 ENFORCED, AD-016 PARTIAL, CP8 PARTIAL).
  - Ghi N-030 (bản sao lồng stale).
- Bất biến enforced thêm: INV-1 (ID liên tục), INV-2 (KEYSTONE tự động: mọi AD có guard), INV-3 (ref không dangling), INV-4 (AD/DV có bằng chứng), INV-5 (CP 1..15).
- Trạng thái ID sau phiên (verified qua grep heading): AD-001..030 (30), DV-001..010 (10), TO-001..009 (9), N-001..031 (31). Liên tục, không trùng.
- Verified cuối: `dotnet test Platform.slnx` (gồm JournalConsistencyTests) + `getDiagnostics` journal — xem kết quả cùng phiên.
- Tổng bản ghi journal: AD 30, DV 10, TO 9, N 31.

---

### N-032 — Task 8.1 + 8.2 (refresh-token store nguyên tử) VERIFY THẬT (2026-07-09)
- Verified: ✅ `dotnet test Platform.slnx` → ArchitectureTests 21 + UnitTests 37 + Infrastructure.Tests **33** + Api.Tests 29 = **120 passed, 0 failed**, build 0 warning.
- Đã tạo trong `src/Bedrock.Infrastructure/Persistence/Security/`:
  - `RefreshTokenRecord` (**internal sealed** — ẩn tối đa, F19/AD-010; không audit/xmin).
  - `RefreshTokenModelBuilderExtensions.AddRefreshTokens(schema?)` (public) — map `refresh_token` vào schema module (per-module, giống AddOutboxInbox); **UNIQUE `ux_refresh_hash`** (F10) + `ix_refresh_user`/`ix_refresh_family`; KHÔNG lộ RefreshTokenRecord ra ngoài.
  - `EfRefreshTokenStore` (impl `IRefreshTokenStore`): `TryConsumeAsync`/`RevokeFamilyAsync` = `ExecuteUpdateAsync` một câu `UPDATE ... WHERE ... AND revoked_at IS NULL` (nguyên tử, row-lock — fix gốc race, không lock ứng dụng); `GetByHashAsync` trả theo hash bất kể revoked (AD-031); `AddAsync` chỉ stage (CreatedAt đóng dấu bằng IClock).
  - `AddBedrockPersistence` nay `TryAddScoped<IRefreshTokenStore, EfRefreshTokenStore>` (cơ chế ở lõi — AD-010).
- QUYẾT ĐỊNH THIẾT KẾ khi triển khai: **AD-031** (đổi `GetActiveByHashAsync`→`GetByHashAsync`, hoà giải mâu thuẫn §5.7↔§7.4 — reuse-detection cần record đã revoked) + **DV-011** (deviation tên method so với design literal). Đã đồng bộ design §5.7/§7.4 + port.
- Test SQLite (6 mới): GetByHash trả token + null-khi-vắng; GetByHash trả token ĐÃ REVOKED (nền reuse-detection); consume once-true-then-false; RevokeFamily thu hồi đúng family (family khác giữ nguyên); consume+add rollback cùng transaction (§7.4); UNIQUE hash bị chặn (F10). Race 2-request đồng thời (CP7) + Postgres → task 8.3 (Testcontainers, N-012).
- CP7 nâng PENDING → **PARTIAL**; AD-010 nâng PENDING → **PARTIAL** (store xong; use case rotation task 16).
- CÒN LẠI task 8: **8.3** (Testcontainers — race rotation đa-connection, CP7 full). Kế tiếp build-order: task 9 (crypto/JWT key-ring — unit-test được, không Docker) là ứng viên tốt để giữ nhịp.
- Tổng bản ghi journal: AD 31, DV 11, TO 9, N 32.

---

### N-033 — Task 9.1 + 9.2 (crypto Argon2id + CSPRNG token + JWT key-ring signer) VERIFY THẬT (2026-07-09)
- Verified: ✅ `dotnet test Platform.slnx` → ArchitectureTests 21 + UnitTests 37 + Infrastructure.Tests **52** + Api.Tests 29 = **139 passed, 0 failed**, build 0 warning.
- Trạng thái trước khi làm (kiểm tra kỹ theo yêu cầu user): đĩa ĐÃ có sẵn port `IJwtTokenService` + `JwtKeyRingOptions` trong `Bedrock.Application/Ports/Security/` (do phiên trước, giống lúc phát hiện task 6/7) — đĩa đi trước narrative. Đã đọc contract thật trước khi code (không suy đoán). Baseline 120 test xanh xác nhận lại.
- Package thật (pin, verify qua project.assets.json + build): `Microsoft.IdentityModel.JsonWebTokens` **8.0.1** (= transitive của JwtBearer 10.0.9 → không xung đột version), `Microsoft.Extensions.Configuration.Binder` **10.0.0** (Infra cần `.Bind`; Api có sẵn qua framework ref). `Konscious.Security.Cryptography.Argon2` 1.3.1 đã reference sẵn.
- Đã tạo (theo cấu trúc folder design §6.4 — tách `Cryptography/` + `Tokens/` khỏi "Security" cũ):
  - `Cryptography/PasswordHashingOptions` + `Cryptography/Argon2idPasswordHasher` (IPasswordHasher, PHC string tự-mô-tả, verify FixedTimeEquals hằng-thời-gian, hash hỏng → false không ném).
  - `Tokens/CryptoTokenGenerator` (ITokenGenerator, CSPRNG base64url, sàn 16 byte).
  - `Tokens/JwtTokenService` (IJwtTokenService, `JsonWebTokenHandler` ký HS256 + kid header, thời gian từ IClock — AD-032).
  - `Tokens/JwtKeyRingValidation` (internal, validate-on-start F35: Keys non-empty, ActiveKid ∈ Keys, secret base64 ≥256-bit, Issuer/Audience, lifetime>0).
  - `DependencyInjection/BedrockSecurityExtensions.AddBedrockSecurity(config)` — đăng ký impl THẬT cho 3 port bảo mật BẮT BUỘC (KHÔNG default no-op — fail-secure §5.5) + bind/validate key-ring.
- QUYẾT ĐỊNH khi triển khai: **AD-032** (JsonWebTokenHandler thay legacy JwtSecurityTokenHandler — tránh static `DefaultOutboundClaimTypeMap` remap tên claim, bảo vệ AD-023). Sửa nhỏ Api: `AddSingleton(keyRing)` → `TryAddSingleton` (idempotent khi Host gọi cả AddBedrockAuthCore + AddBedrockSecurity cùng bind section "Jwt" — AD-008; không đổi hành vi khi Api chạy một mình, 29 test Api vẫn xanh).
- AD-008 (JwtKeyRingOptions ký/verify) nâng PARTIAL → **ENFORCED** (cả hai phía có guard).
- Test (19 mới): hasher (roundtrip/wrong-pw/malformed/PHC-format/salt-ngẫu-nhiên), token (urlsafe-no-padding/duy-nhất/min-bytes), JWT (verify+kid+claims, rotation verify khóa cũ, khóa retire không verify, ctor-throw-active-kid-thiếu, exp theo lifetime), validation (6 case).
- Lưu ý Host (task 16): `JwtKeyRingOptions` hiện bind ở CẢ Api (verify) và Infra (sign) — cùng section "Jwt", cùng dùng TryAddSingleton nên idempotent; nếu muốn một-nguồn tuyệt đối, cân nhắc gom bind vào một `AddBedrockCore`/options pattern lúc dựng Host.
- CÒN LẠI theo build order: task 10 (DI convention + startup validation — RequiredPortsValidator gộp cả validate key-ring này), task 11 (HTTP hardening). Đều unit-test được, không Docker. Task 8.3/7.4 chờ Docker.
- Tổng bản ghi journal: AD 32, DV 11, TO 9, N 33.

---

### N-034 — Task 10.1 + 10.2 (DI convention + startup validation) VERIFY THẬT (2026-07-09)
- Verified: ✅ `dotnet test Platform.slnx` → ArchitectureTests 21 + UnitTests 37 + Infrastructure.Tests **60** + Api.Tests 29 = **147 passed, 0 failed**, build 0 warning.
- Package thật (pin): `Microsoft.Extensions.DependencyInjection.Abstractions` **10.0.9** (Application) + `Microsoft.Extensions.Hosting.Abstractions` **10.0.9** (Infrastructure). Ban đầu pin 10.0.0 → NU1109 (EF Core 10.0.9 kéo transitive ≥10.0.9); nâng lên 10.0.9 khớp EF (sửa GỐC version floor, không hạ EF).
- Đã tạo:
  - `Application/DependencyInjection/StartupValidationOptions` (singleton registry — DV-012) + `BedrockRegistrationExtensions` (`AddBedrockConventions` marker-scan reflection AD-033, `AddRequiredPort`/`AllowMultipleImplementations`, `ValidateSingleImplementationPorts` duplicate-guard F18).
  - `Infrastructure/Startup/RequiredPortsValidator` (IHostedService, scope-aware + aggregate — sửa 2 lỗi bản phác §9.4) + `Infrastructure/DependencyInjection/BedrockStartupValidationExtensions.AddBedrockStartupValidation`.
  - Nối task 9↔10: `AddBedrockSecurity` khai `IPasswordHasher`/`ITokenGenerator`/`IJwtTokenService` là RequiredPort → validator chặn boot nếu thiếu.
- QUYẾT ĐỊNH: **AD-033** (marker-scan reflection, không Scrutor — giảm dep lõi), **DV-012** (StartupValidationOptions singleton, không IOptions — giữ Application tối thiểu dep).
- Diễn giải "TryAdd" của design §6.2 (ghi rõ để không nhầm): TryAdd là convention cho PORT-DEFAULT ở các `AddXxxCore` (đã dùng `TryAddSingleton/TryAddScoped` ở AddBedrockPersistence/Security). Marker-SCAN thì Add (append) — để multi-impl (handlers) đăng ký đủ — và `ValidateSingleImplementationPorts` (F18) mới là chốt chặn trùng single-impl. Hai cơ chế bổ trợ, không mâu thuẫn.
- CÒN LẠI (Host — task 16.2): bật `ValidateOnBuild`/`ValidateScopes=true` TƯỜNG MINH lúc `BuildServiceProvider`/`UseDefaultServiceProvider` (không ép được từ library) + gọi `AddBedrockStartupValidation()` + `ValidateSingleImplementationPorts()` trước Build. Test validator đã dùng `ValidateScopes=true` chứng minh scope-aware.
- CP9 nâng PENDING → **ENFORCED**; AD-011 → **ENFORCED**.
- Kế tiếp build-order: task 11 (HTTP hardening — ForwardedHeaders + rate-limit theo IP thật + cookie/CORS). Unit/integration-test được (TestHost), không Docker.
- Tổng bản ghi journal: AD 33, DV 12, TO 9, N 34.

---

### N-035 — Task 11.1 + 11.2 (HTTP hardening: ForwardedHeaders + rate-limit + cookie/CORS) VERIFY THẬT (2026-07-09)
- Verified: ✅ `dotnet test Platform.slnx` → ArchitectureTests 21 + UnitTests 37 + Infrastructure.Tests 60 + Api.Tests **33** = **151 passed, 0 failed**, build 0 warning.
- Đã tạo `Bedrock.Api/HttpSecurity/`:
  - `HttpSecurityOptions` (KnownProxies/KnownNetworks/ForwardLimit/RateLimit*/CookieSameSiteMode/CorsAllowedOrigins/CorsPolicyName; collection get-only tránh CA1819).
  - `CookieSameSiteMode` enum + `CookieSecurityDefaults` (SameSite=Lax vs None + Secure/HttpOnly + RequiresCsrf).
  - `RateLimitPartitioning` (internal — partition theo RemoteIpAddress thật).
  - `BedrockHttpSecurityExtensions.AddBedrockHttpSecurity`: Configure ForwardedHeaders (chỉ tin proxy/network khai), AddRateLimiter (fixed-window partition IP, 429 problem+json + Retry-After — AD-034), AddCors (same-site đóng / cross-site mở origin khai + credentials).
  - `BedrockApiExtensions`: `AddBedrockApi` gọi `AddBedrockHttpSecurity`; `UseBedrockApi` điền slot #1 `UseForwardedHeaders`, #8 `UseCors`, #9 `UseRateLimiter` (đúng §3.5).
- QUYẾT ĐỊNH: **AD-034** (429 = edge concern, không nhét vào ErrorType domain), **AD-035** (HSTS/HTTPS-redirect slot #4 = trách nhiệm Host, base không ép).
- 2 lỗi API .NET 10 gặp & fix đúng API hiện hành (không né): `ForwardedHeadersOptions.KnownNetworks` obsolete (ASPDEPR005) → dùng **`KnownIPNetworks`**; `IPNetwork` ambiguous (HttpOverrides vs System.Net) → dùng **`System.Net.IPNetwork`** (bản không-obsolete). Thiếu `using Microsoft.AspNetCore.Builder` (ForwardedHeadersOptions) → thêm.
- Test: ForwardedHeaders bind đúng options (unit); cookie flags theo mode (theory 2 mode); rate-limit partition theo X-Forwarded-For qua proxy tin cậy → 429 sau PermitLimit, IP khác = bucket độc lập (integration TestHost — giả lập peer=proxy để ForwardedHeaders chấp nhận XFF trong TestServer).
- F16/F17 nay có cơ chế + test. Pipeline §3.5 còn slot #4 (HSTS/HTTPS) là Host (AD-035).
- CÒN LẠI build-order: hết P1 (task 6–11 xong, trừ 7.4/8.3 chờ Docker). Kế tiếp **P1.5** — task 12 (định nghĩa port mở rộng contract-first: Search/Email/Cache/Storage/ExternalAuth) + task 13 (khung Extension Architecture AddXxxCore/AddYyy + default an toàn). Unit-test được, không Docker.
- Tổng bản ghi journal: AD 35, DV 12, TO 9, N 35.

---

### N-036 — KHÔNG dùng hook auto-spawn cho anti-drift; validate INLINE mỗi lượt (2026-07-09)
- Bối cảnh: từng tạo hook `runCommand` (`journal-consistency-guard`) chạy `JournalConsistencyTests` khi lưu file journal. User phản hồi: hook `runCommand` mở terminal tab mỗi lần lưu → phiền. Đã **tắt rồi XOÁ hẳn** hook (`.kiro/hooks/journal-consistency-guard.kiro.hook`).
- Chốt cách làm (cho AI đời sau — ĐỪNG tạo lại hook runCommand cho việc này): cơ chế `runCommand` LUÔN mở terminal (bản chất). Anti-drift KHÔNG dựa hook mà dựa 2 lớp đã đủ mạnh: (1) **build gate** — `JournalConsistencyTests` chạy trong `dotnet test` mỗi lần build; (2) **AI tự chạy INLINE** filter `JournalConsistencyTests` trong chính lượt chat mỗi khi sửa journal (không mở tab, kết quả hiện trong hội thoại).
- Nếu muốn "nhắc" tự động khi USER sửa journal thủ công: dùng hook loại `askAgent` (chạy inline trong hội thoại, không tab) — KHÔNG dùng `runCommand`. Nhưng hiện người sửa journal là AI nên (2) đã phủ.
- Provenance/Evidence: `delete_file` hook phiên 2026-07-09; AD-030 (cổng JournalConsistencyTests) không đổi — hook chỉ là trigger tuỳ chọn, gỡ đi không giảm sức mạnh cổng.
- Tổng bản ghi journal: AD 35, DV 12, TO 9, N 36.

---

### N-037 — Task 12.1/12.2/12.3 (port mở rộng contract-first) VERIFY THẬT (2026-07-09)
- Verified: ✅ `dotnet test Platform.slnx` → **151 passed, 0 failed**, build 0 warning. Task 12 là contract-first (chỉ interface + DTO) → KHÔNG thêm test hành vi; guard là build 0-warning + `DependencyRuleTests` (Application zero-tech vẫn xanh với port mới).
- Đã tạo trong `Bedrock.Application/Ports/`:
  - `Search/SearchPorts.cs` — `ISearchIndex<TDoc>`/`ISearchQuery<TDoc>` + `SearchRequest`/`SearchResult<TDoc>` (F26, read-model/CQRS-lite).
  - `Email/EmailPorts.cs` — `EmailMessage` + `IEmailSender` (F28).
  - `Caching/CachingPorts.cs` — `CacheEntryOptions`/`IAppCache`/`ILockHandle`/`IDistributedLock`/`IIdempotencyStore`/`IRateLimitStore` (F28, port HẸP chống leaky).
  - `Storage/StoragePorts.cs` — `FileBlob` + `IFileStorage` (F28).
  - `ExternalAuth/ExternalAuthPorts.cs` — `ExternalAuthRequest`/`Challenge`/`Callback`/`ExternalUserProfile` + `IExternalAuthProvider` + `IExternalAuthProviderRegistry` (F27).
- QUYẾT ĐỊNH: **AD-036** (field shape 3 record External Auth — design chỉ đặt tên; chốt tối thiểu bám OAuth, PKCE/nonce adapter-internal).
- Analyzer: `ILockHandle : IAsyncDisposable` rỗng → `#pragma CA1040` có lý do (marker dispose chủ đích, giống ServiceMarkers). `Uri` cho RedirectUri (né CA1056). ct `= default` nhất quán port hiện có.
- Chưa làm (đúng phạm vi contract-first): impl default (`NullAppCache` degrade / `Throwing*` fail-loud) + khung `AddXxxCore/AddYyy` → **task 13**. Port bảo mật bắt buộc (IHtmlSanitizer chưa impl — Sanitization adapter) sẽ vào RequiredPorts khi có impl.
- Vào **P1.5**: task 12 ✅. Kế tiếp **task 13** (Extension Architecture: cặp `AddXxxCore()`/`AddYyyXxx(cfg)` + default phân loại §5.5 + registry multi-impl) — unit-test được (gọi port fail-loud khi chưa adapter → exception rõ; IAppCache miss-through).
- Tổng bản ghi journal: AD 36, DV 12, TO 9, N 37.

---

### N-038 — Task 13 (khung Extension Architecture: AddXxxCore + default an toàn + registry) VERIFY THẬT (2026-07-09)
- Verified: ✅ `dotnet test Platform.slnx` → ArchitectureTests 21 + UnitTests 37 + Infrastructure.Tests **65** + Api.Tests 33 = **156 passed, 0 failed**, build 0 warning.
- Đã tạo trong `Bedrock.Infrastructure`:
  - `Extensions/Defaults/NullAppCache` (degrade miss-through) + `ThrowingPortDefaults` (fail-loud: Email/FileStorage/DistributedLock/IdempotencyStore/RateLimitStore/EventBusPublisher/SearchIndex&lt;&gt;/SearchQuery&lt;&gt; + helper `NoAdapterError`).
  - `Extensions/ExternalAuthProviderRegistry` (resolve theo Name case-insensitive; trùng Name → ném boot; unknown → fail-loud).
  - `DependencyInjection/BedrockExtensionArchitectureExtensions`: `AddMessagingCore`/`AddSearchCore`/`AddEmailCore`/`AddCacheCore`/`AddStorageCore`/`AddExternalAuthCore` — TryAdd default; `AddExternalAuthCore` whitelist `IExternalAuthProvider` vào `AllowMultipleImplementations` (nối task 10).
- Hiện thực TRUNG THÀNH AD-009 + §5.5 + §6.1/§6.2 (KHÔNG phát sinh AD mới):
  - **Default ở Infrastructure** — theo §Components ("default port an toàn" ở Infrastructure).
  - **Override bằng `Replace`** (không Add) — theo §6.2 "Replace tường minh"; giữ đúng 1 registration → duplicate-guard (task 10) không báo nhầm. Test `Adapter_overrides_default_via_replace` chứng minh.
  - **Phân loại degrade vs fail-loud** đúng bảng §5.5: chỉ `IAppCache` degrade; còn lại fail-loud.
- Test (5): cache degrade miss-through; 8 port fail-loud throw `InvalidOperationException` khi chưa adapter; override qua Replace (đúng 1 registration); registry resolve by-name + fail-loud unknown; core idempotent (TryAdd gọi 2 lần vẫn 1 registration).
- AD-009 nâng PENDING → **ENFORCED**.
- Vào **P1.5 gần xong**: task 12 ✅ · 13 ✅. Kế tiếp **task 14** (Adapter mẫu `Adapters.Messaging.RabbitMq` chứng minh "cắm không sửa lõi" + resilience biên + arch test Adapters chỉ ref Application + git-diff không đụng lõi) — **CẦN Docker/Testcontainers cho integration RabbitMQ** (N-012). Nếu thiếu Docker: làm phần arch-test + adapter code + Replace wiring (unit-test được), hoãn integration publish.
- Tổng bản ghi journal: AD 36, DV 12, TO 9, N 38.

### N-039 — Logging behavior (§8): source-gen + đo monotonic + KHÔNG try/catch; tracing hoãn task 18
- `LoggingUseCaseDecorator`/command variant dùng `PipelineLog` — static partial + `[LoggerMessage]` source-generator (delegate cache, zero-alloc khi level tắt) → tránh `CA1848` mà build 0-warning (`TreatWarningsAsErrors`).
- Đo thời lượng bằng `Stopwatch.GetTimestamp()` + `Stopwatch.GetElapsedTime(start)` (đồng hồ MONOTONIC) — CỐ Ý không dùng `IClock` (wall-clock, có thể nhảy do NTP/DST → sai elapsed).
- KHÔNG try/catch trong behavior: chỉ log KẾT QUẢ `Result` (Ok/Fail + `Error.Code`); exception kỹ thuật TRUYỀN LÊN middleware ProblemDetails ở `Bedrock.Api` xử lý tập trung → tránh nuốt lỗi + `CA1031` (catch general).
- Tracing spans (OpenTelemetry) + correlation đầy đủ HOÃN tới task 18 (§9.3) — task 15 chỉ làm structured log outcome. Đây là ranh giới cố ý: behavior §8 khoá thứ tự + log; telemetry 3-trụ là cross-cutting riêng ở task 18.
- Provenance: `platform/src/Bedrock.Application/Behaviors/PipelineLog.cs` + `LoggingUseCaseDecorator.cs` + `LoggingCommandUseCaseDecorator.cs`; build 0 warning; `PipelineOrderTests` chứng minh Logging là lớp ngoài cùng (pass-through, không đổi Result).

### N-040 — Module Identity skeleton (task 16.1): phạm vi + điểm cần biết + landmine multi-module
- **Phạm vi 16.1:** 5 project (Contracts/Domain/Application/Infrastructure/Api) + use case rotation §7.4 + unit test với fakes. Login/logout/external-login, user store, permission policy → NGOÀI phạm vi 16.1 (rotation là lát mỏng chứng minh khuôn module + dùng lại IRefreshTokenStore/IJwtTokenService/crypto đã build).
- **Contract-first event:** `UserTokenRefreshedIntegrationEvent` khai ở Identity.Contracts nhưng CHƯA emit (giống ports task 12–13 khai trước consumer). Emission qua `IOutboxWriter` trong transaction rotation sẽ nối khi có consumer/telemetry thật hoặc ở DoD task 21 — giữ §7.4 đúng nguyên văn (không thêm bước ngoài pseudocode).
- **Claim tối thiểu:** access token rotation chỉ mang `sub` = UserId (AD-023 liệt role/permission/tenant/sid). role/permission cần user-profile store (chưa có ở skeleton) → bổ sung khi module có user store thật.
- **SHA-256 inline:** hash refresh token tính trong use case (`Convert.ToHexStringLower(SHA256.HashData(...))`) đúng §7.4 ("hash ← SHA256"). Token 256-bit CSPRNG entropy cao → KHÔNG cần Argon2 (Argon2 cho password). Primitive chuẩn, không phải "công nghệ swap được" → không cần port.
- **AuthErrors đặt ở Identity.Domain:** mã lỗi nghiệp vụ khai ở module (design §4.4 — lõi không giữ). Một mã chung `identity.invalid_refresh_token` cho MỌI nhánh fail (không tồn tại/hết hạn/revoked/thua race) — chống oracle dò token.
- **⚠️ LANDMINE multi-module (fix ở 16.2):** `AddBedrockPersistence<TContext>` hardcode health-check name `"database"` (`DatabaseHealthCheckName`). Hai module cùng gọi → ASP.NET ném "duplicate health check registration". 16.1 (một module Identity) CHƯA trigger. **Fix tận gốc ở 16.2** (nơi Host ráp ≥2 module — kiểm chứng được): đổi name thành per-context (vd `$"database:{typeof(TContext).Name}"`), giữ tag `ready`; thêm guard test 2-context không trùng name. KHÔNG fix vá tạm ở module.
- Provenance: các file dưới `platform/src/Modules/Identity/**` + `tests/Modules/Identity.UnitTests/**`; build 0 warning; 179 test xanh.

### N-041 — Map Result → IResult ở endpoint dùng ProblemDetailsBuilder (nguồn lỗi DUY NHẤT); cân nhắc trích helper
- `IdentityEndpointModule.RefreshAsync` map `Result` → HTTP inline: success → `Results.Ok`, failure → `Results.Problem(ProblemDetailsBuilder.Build(error, CorrelationContext.Resolve(http)))` — dùng LẠI builder single-source (không tự dựng shape lỗi → không drift với exception/auth path).
- Hiện chỉ 1 endpoint nên inline chấp nhận được. Khi có nhiều endpoint/module, cân nhắc trích một helper `ToHttpResult(this Result/Result<T>, HttpContext)` ở `Bedrock.Api` (mechanism dùng chung) — hoãn tới khi thực sự có ≥2 nơi lặp (tránh trừu tượng sớm). Ghi lại để không quên.
- Provenance: `platform/src/Modules/Identity/Identity.Api/IdentityEndpointModule.cs`; dùng `ProblemDetailsBuilder`/`CorrelationContext` public của Bedrock.Api.

### N-042 — Pipeline behaviors phụ thuộc port-default → Host PHẢI gọi AddXxxCore; ValidateOnBuild KHÔNG bắt lỗi sau Scrutor factory
- **Triệu chứng phát hiện (task 16.2):** POST /identity/token/refresh trả 500 `Unable to resolve service 'IIdempotencyStore' while activating IdempotencyUseCaseDecorator`.
- **Bản chất:** `AddBedrockCore` decorate MỌI `IUseCase<,>` bằng Idempotency behavior, decorator này inject `IIdempotencyStore` lúc CONSTRUCT (gating `IIdempotentCommand` chỉ ở runtime). Nên use case nào cũng cần `IIdempotencyStore` tồn tại trong DI. Default của nó (`ThrowingIdempotencyStore`) đăng ký bởi `AddCacheCore()` (task 13) — Host quên gọi → không resolve được use case đã decorate.
- **Fix tận gốc:** Host (composition root) gọi ĐẦY ĐỦ posture default an toàn: `AddMessagingCore/AddCacheCore/AddEmailCore/AddSearchCore/AddStorageCore/AddExternalAuthCore` (design §5.5/§6.1/§13 "AddXxxCore luôn gọi"). Mỗi port có default degrade/fail-loud; adapter thật override bằng Replace. Không phải vá endpoint — sửa đúng nơi (thiếu default port).
- **⚠️ Giới hạn ValidateOnBuild (quan trọng):** `ValidateOnBuild=true` KHÔNG bắt lỗi thiếu dependency này lúc boot vì Scrutor `Decorate` thay registration bằng FACTORY (ImplementationFactory), mà ValidateOnBuild chỉ construct registration có ImplementationType — factory bị BỎ QUA. Vì vậy fail-fast (I9) KHÔNG phủ được dependency ẩn sau decorator. GIẢM THIỂU: (1) Host gọi đủ AddXxxCore; (2) smoke test POST endpoint (HostSmokeTests) resolve use case THẬT lúc runtime → bắt lỗi này. Cân nhắc (tương lai): startup check chủ động resolve từng `IUseCase` đã đăng ký để kéo lỗi về boot-time — hoãn (chưa cần; smoke test đã phủ).
- Provenance: `platform/src/Host/StarHill.Api/Program.cs` (6 AddXxxCore) + `tests/Host/StarHill.Api.Tests/HostSmokeTests.cs` (POST→400 chứng minh pipeline resolve runtime).

### N-043 — Host StarHill.Api: chi tiết composition + dev-secret placeholder + package bump
- **Composition root DUY NHẤT (I7/luật-5):** `StarHill.Api` là project duy nhất ref đồng thời Bedrock.Api + Bedrock.Infrastructure + Identity.Api + Identity.Infrastructure. Thứ tự Program.cs: (1) AddBedrockApi/Security/StartupValidation + 6 AddXxxCore; (2) modules nửa-Infra `AddIdentityInfrastructure(UseNpgsql)` + nửa-Api `AddIdentityApi` (DV-013); (3) `AddBedrockCore` decorate pipeline (SAU khi use case đăng ký — AD-037); (4) `ValidateSingleImplementationPorts` trước Build; fail-fast DI (`ValidateOnBuild/ValidateScopes=true`, I9).
- **Provider DB:** Host CHỌN Npgsql (design §1.2 — Postgres là provider đích); connection string từ `ConnectionStrings:Identity`, thiếu → fail-fast (F35).
- **HSTS/HTTPS-redirect (slot #4):** KHÔNG bật ở sample Host — chạy sau reverse-proxy terminate TLS (AD-035/§3.5); deployment thật tự bật.
- **⚠️ Dev-secret placeholder (F35 → task 19):** `appsettings.json` chứa Jwt dev key (giải mã ra ASCII rõ ràng ⇒ hiển nhiên không phải khóa thật) + connection string dummy, để Host boot dev + smoke test chạy KHÔNG cần hạ tầng. Task 19 sẽ chuyển secret sang user-secrets/env/Key Vault + kiểm không secret thật trong repo. Đây là nợ có chủ đích, đã khoanh vùng.
- **Package:** thêm `Microsoft.AspNetCore.Mvc.Testing` 10.0.9 (WebApplicationFactory smoke); nâng `Microsoft.Extensions.Configuration.Binder` 10.0.0→10.0.9 (Mvc.Testing→Hosting kéo ≥10.0.9, transitive-pinning gây NU1109 nếu để 10.0.0). Verify build 0 warning + toàn test xanh sau nâng.
- Provenance: `platform/src/Host/StarHill.Api/**`, `Directory.Packages.props`.
