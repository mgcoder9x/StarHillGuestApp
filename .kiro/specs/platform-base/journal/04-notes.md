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

---

### N-044 — Task 16.3 (arch tests module boundary + single composition root + CP11) VERIFY THẬT (2026-07-09)
- Verified: ✅ `dotnet test Platform.slnx` → ArchitectureTests **31** (+10) + UnitTests 54 + Identity.UnitTests 6 + Api.Tests 33 + StarHill.Api.Tests 2 + Infrastructure.Tests 66 = **192 passed, 0 failed**, build 0 warning.
- Đã tạo `ModuleBoundaryTests.cs` + mở rộng `CoreAssemblies.cs` (thêm `ModuleAssemblies` trỏ 5 assembly Identity qua marker public) + `Bedrock.ArchitectureTests.csproj` thêm 5 ProjectReference Identity + `FrameworkReference Microsoft.AspNetCore.App` (cần để CLR nạp type Identity.Api/Bedrock.Api khi NetArchTest truy cập `typeof(...).Assembly`).
- **CP4** (Property 4/I5/F30): Identity.Contracts thuần DTO (chỉ ref `Bedrock.Messaging.Contracts`); Domain/Application ⊥ Infrastructure/Api. Negative control: type giả giữ `IdentityDbContext` (internal module) bị luật bắt → chứng minh cơ chế "chỉ Contracts" kiểm được.
- **CP5** (Property 5): enforce từ PHÍA MODULE — `Identity.Api` ⊥ mọi Infrastructure; `Identity.Infrastructure` ⊥ mọi Api. Kết hợp CP2 (`Bedrock.Api` ⊥ `Bedrock.Infrastructure`) ⇒ KHÔNG library/module nào bắc cầu Api+Infra → chỉ Host (exe) làm được ⇒ "composition root duy nhất".
  - **Lý do KHÔNG nạp assembly Host để test trực tiếp (quyết định phương pháp, nhìn bản chất):** `StarHill.Api` là Web SDK exe, `Program` top-level = internal (không có public type để `typeof`), và nạp web-exe vào AppDomain của arch-test dễ kích hoạt initializer/asset không mong muốn — mong manh. Bản chất CP5 = "KHÔNG nơi nào NGOÀI Host được bắc cầu"; điều này enforce ĐẦY ĐỦ bằng cách chứng minh mọi library/module KHÔNG bắc cầu (đã làm) + Host là exe DUY NHẤT (theo cấu trúc, 1 project Web). Vai trò tích cực của Host (thật sự ráp cả hai) đã được `HostSmokeTests` (16.2) kiểm runtime (boot 200 + endpoint hoạt động). Đây là enforce đúng gốc, không vá ngọn.
- **CP11**: use case Identity (`RefreshAccessTokenUseCase : IUseCase<,>`) ⊥ `Bedrock.Application.Messaging.Dispatch` (cùng luật đã có cho Bedrock ở `UseCaseSeamTests`; negative control chung ở đó chứng minh engine bắt được rò rỉ Dispatch).
- Mỗi luật positive đi kèm negative control kiểu "phụ thuộc CÓ THẬT phải bị bắt" (Identity.Contracts→Messaging.Contracts; Identity.Api→Bedrock.Api; Identity.Infrastructure→Bedrock.Infrastructure) → luật không false-pass.
- Task 16 (16.1/16.2/16.3) HOÀN TẤT. CP4/CP5 nâng PENDING → **ENFORCED**; CP11 mở rộng phủ module. Kế tiếp theo tasks.md: task 17 (versioning) — hoặc task 14 (RabbitMq adapter, cần Docker) khi có môi trường.
- Provenance: `platform/tests/Bedrock.ArchitectureTests/{ModuleBoundaryTests.cs,CoreAssemblies.cs,Bedrock.ArchitectureTests.csproj}`; `dotnet test` 192 xanh (phiên này).

---

### N-045 — Task 17 (Versioning API + integration event, F32) VERIFY THẬT (2026-07-09)
- Verified: ✅ `dotnet test Platform.slnx` → ArchitectureTests 31 + Identity.UnitTests 6 + **Bedrock.ContractTests 1 (mới)** + UnitTests 54 + Api.Tests 33 + StarHill.Api.Tests 2 + **Infrastructure.Tests 68 (+2)** = **195 passed, 0 failed**, build 0 warning.
- **Part A — integration-event versioning (R22.2/R22.3 + snapshot R32.4 phần event):**
  - `Bedrock.Infrastructure.Tests/OutboxSerializationTests` — khoá HÀNH VI tolerant-reader của CHÍNH `OutboxSerialization.Options` (internal): payload "v2" thêm field lạ → consumer "v1" vẫn deserialize (bỏ qua) + guard `UnmappedMemberHandling != Disallow`. Nếu ai đổi options phá tolerant → FAIL BUILD.
  - Project MỚI `tests/Bedrock.ContractTests` (+ Platform.slnx) — `IntegrationEventSchemaSnapshotTests`: reflect mọi `IntegrationEvent` concrete trong `*.Contracts` → descriptor `{EventType} v{SchemaVersion} {{ Prop:Type }}` so với snapshot đã duyệt. Đổi breaking (đổi/xoá field) → FAIL, buộc quyết định có ý thức (bump version / EventType v2). Đọc EventType/SchemaVersion qua `RuntimeHelpers.GetUninitializedObject` (trả literal, không đụng ctor). Task 20 sẽ mở rộng project này cho snapshot `Error.Code`.
- **Part B — HTTP API versioning (R22.1):** package thật `Asp.Versioning.Http` **10.0.0**; `Bedrock.Api/Versioning/BedrockApiVersioning` (`AddBedrockApiVersioning` URL-segment + default v1 + report; helper `MapVersionedGroup`) — **AD-043**. Identity endpoint → `/v1/identity/token/refresh` (`.MapToApiVersion(V1)`); `HostSmokeTests` đổi path + assert header `api-supported-versions: 1.0`.
- **OpenAPI document-per-version HOÃN sang Host — DV-015** (base chưa có hạ tầng OpenAPI; endpoint đã mang metadata ApiVersion để Host nhóm khi bật; acceptance task 17 = snapshot + /v1 + 0 warning VẪN đạt).
- Kế tiếp theo `tasks.md`: task 18 (Telemetry & correlation unity — OpenTelemetry 3 trụ + W3C traceparent, CP10) hoặc task 14 (RabbitMq adapter — cần Docker). Task 19 (secrets), task 20 (contract tests hoàn tất + no-business-in-core cuối), task 21 (DoD).
- Tổng bản ghi journal: AD 43, DV 15, TO 9, N 45.

---

### N-046 — Task 18 (Telemetry & correlation unity, F34/F21) VERIFY THẬT (2026-07-09)
- Verified: ✅ `dotnet test Platform.slnx` → ArchitectureTests 31 + UnitTests 54 + ContractTests 1 + Identity.UnitTests 6 + **Api.Tests 35 (+2)** + StarHill.Api.Tests 2 + Infrastructure.Tests 68 = **197 passed, 0 failed**, build 0 warning.
- Package thật (Bedrock.Api): `OpenTelemetry.Extensions.Hosting` / `OpenTelemetry.Instrumentation.AspNetCore` / `OpenTelemetry.Exporter.OpenTelemetryProtocol` = **1.16.0**.
- Đã tạo `Bedrock.Api/Observability/BedrockTelemetry` (ActivitySource+Meter "Bedrock") + `BedrockObservabilityExtensions.AddBedrockObservability` (OTel 3 trụ: traces [AspNetCore + source Bedrock/Npgsql], metrics [AspNetCore + meter Bedrock/RateLimiting/EF/Npgsql], logs [ILogger→OTel]); wire trong `AddBedrockApi`. Exporter OTLP CHỈ bật khi `Observability:Otlp:Endpoint` có giá trị (dev/test không export — span/metric vẫn tạo, kiểm qua listener).
- **Correlation fix tận gốc (AD-044):** `CorrelationContext.CurrentTraceId = Activity.Current.TraceId` (W3C 32-hex); middleware bỏ override client; propagation qua `traceparent`. Test CP10 đổi sang gửi `traceparent` → header == body.traceId == trace hiện hành.
- **DV-016:** logs pillar qua `Microsoft.Extensions.Logging` + OTel exporter, KHÔNG Serilog (base đã chuẩn hoá ILogger; tránh 2 hệ logging).
- Điểm cần biết:
  - **Hai biểu diễn cùng một trace:** HTTP `X-Correlation-Id`/`ProblemDetails.traceId` = **bare traceId** (32-hex, human/log-facing); outbox `correlation_id` = **full `Activity.Id`** (traceparent `00-trace-span-flags`, để consumer nối trace qua bus). Cùng trace, khác format có chủ đích.
  - **`BedrockTelemetry` hiện ở `Bedrock.Api`** → Infrastructure (dispatcher outbox-lag) CHƯA emit được metric dưới nguồn chung. Khi cần instrument outbox-lag/consumer/external-auth (R24.3 phần custom), NÊN chuyển `BedrockTelemetry` xuống `Bedrock.Application` (ActivitySource/Meter là BCL `System.Diagnostics.DiagnosticSource`, có trong shared framework — không phải tech-SDK, hợp lệ ở Application như Logging.Abstractions). Chưa làm nay vì chưa có consumer (tránh premature).
  - Metric test dùng `MeterListener` + poll 3s (metric `http.server.request.duration` ghi lúc request hoàn tất — sau khi `GetAsync` trả về; race timing, không phải lỗi wiring).
- R24.3 catalog: request rate/latency/error ✅ (AspNetCore instrumentation); rate-limit/EF/Npgsql meters đã AddMeter (thu khi phát); outbox lag + dead-letter count + consumer time + external-auth success/fail = custom metric của component (một số component chưa dựng — task 14 adapter; instrument khi finalize).
- Kế tiếp theo `tasks.md`: task 19 (secrets & config governance), task 20 (contract tests + no-business-in-core cuối, CP12), task 21 (DoD). Task 14 (RabbitMq, CP3) cần Docker.
- Tổng bản ghi journal: AD 44, DV 16, TO 9, N 46.

---

### N-047 — Task 19 (Secrets & config governance, F35/R25) VERIFY THẬT (2026-07-09)
- Verified: ✅ `dotnet test Platform.slnx` → ArchitectureTests 31 + ContractTests 1 + Identity.UnitTests 6 + UnitTests 54 + Api.Tests 35 + **StarHill.Api.Tests 3 (+1)** + Infrastructure.Tests 68 = **198 passed, 0 failed**, build 0 warning.
- **GAP thật đã đóng:** cơ chế fail-fast/validate-on-start/required-ports đã có từ task 10.2; task 19 đóng chỗ **appsettings vẫn chứa secret** (JWT `Secret` base64 + `Password=postgres`).
- Đã làm:
  - `appsettings.json` (Host): CHỈ non-secret + placeholder — `Jwt.Keys[0].Secret=""`, connection string BỎ `Password`. `_note` hướng dẫn nạp secret qua User-Secrets (dev) / env/Key Vault (prod).
  - `StarHill.Api.csproj`: thêm `<UserSecretsId>` → dev `dotnet user-secrets set 'Jwt:Keys:0:Secret' <base64-32B>` (WebApplication.CreateBuilder tự load ở Development).
  - `HostSmokeTests`: `SecretInjectingHostFactory` nạp secret qua test-config (giống dev nạp User-Secrets, KHÔNG lấy từ repo) → 2 test boot xanh; thêm `Host_fails_fast_when_required_jwt_secret_is_missing` (không cấp secret → boot fail "HS256") = ACCEPTANCE task 19 + guard "appsettings KHÔNG chứa JWT secret" (nếu có secret thật trong repo, boot đã KHÔNG fail).
- **Fix tận gốc AD-045:** JWT validation chuyển từ eager-lúc-đăng-ký → options `.ValidateOnStart()` (đúng design §9.4 "validate-on-start") + `IValidateOptions` (`JwtKeyRingOptionsValidator`) + concrete singleton lazy `sp => IOptions.Value` + bỏ `TryAddSingleton(keyRing)` eager ở `AddBedrockAuthCore`. Lý do: chỉ validate SAU khi mọi nguồn config hợp nhất (post-Build) mới đồng thời thỏa fail-fast + nạp secret muộn (mô hình secret-ngoài-repo). `JwtKeyRingValidation.Validate` (unit test cũ) GIỮ NGUYÊN — validator chỉ bọc.
- Điều cần biết: eager-read `builder.Configuration` TRƯỚC `Build()` KHÔNG thấy config `WebApplicationFactory.ConfigureAppConfiguration`/User-Secrets nạp lúc Build → mọi validation cần "thấy config muộn" phải dùng `.ValidateOnStart()` (post-Build), KHÔNG validate lúc đăng ký. Áp dụng cho options bắt buộc khác sau này.
- R25 phủ: R25.1 (không secret trong repo — appsettings sạch + UserSecretsId + guard test) ✅; R25.2 (validate-on-start options bắt buộc → chặn boot) ✅ (JWT qua ValidateOnStart; connection string qua Program throw; ports qua RequiredPortsValidator).
- Kế tiếp theo `tasks.md`: task 20 (contract tests hoàn tất — snapshot `Error.Code` registry + no-business-in-core cuối, CP1/CP12), task 21 (DoD). Task 14 (RabbitMq, CP3) cần Docker.
- Tổng bản ghi journal: AD 45, DV 16, TO 9, N 47.

---

### N-048 — Task 20 (Contract tests + hoàn tất no-business-in-core, CP1/CP12) VERIFY THẬT (2026-07-09)
- Verified: ✅ `dotnet test Platform.slnx` → **ArchitectureTests 31** + **ContractTests 2 (+1)** + Identity.UnitTests 6 + UnitTests 54 + Api.Tests 35 + StarHill.Api.Tests 3 + Infrastructure.Tests 68 = **199 passed, 0 failed**, build 0 warning.
- **Part A — CP1 literal (AD-046):** `NoBusinessInCoreLiteralTests` (Mono.Cecil 0.11.6) quét `ldstr` + `const string` trên 5 assembly `Bedrock.*` → 0 vi phạm (lõi sạch), + negative control (scan assembly test bẩn → bắt seed). `NoBusinessInCoreTests` (name-scan) mở rộng ra đủ 5 assembly (gộp 3 fact → 1 loop `CoreAssemblies.AllBedrock`). CP1 nay phủ CẢ tên/namespace LẪN literal → **AD-022 resolved**.
- **Part B — CP12 (Error code contract):** `Bedrock.ContractTests/ErrorCodeSnapshotTests` reflect MỌI code ổn định (static `Error` field + static method trả `Error` với mọi tham số optional) trong Bedrock.Domain + Identity.Domain → so snapshot đã duyệt (9 code: concurrency_conflict/conflict/forbidden/identity.invalid_refresh_token/not_found/rate_limited/unauthorized/unexpected/validation_error). Code template `{entity}.not_found` (cần tham số) KHÔNG vào registry. Đổi/xoá code → FAIL BUILD.
- **Part C — CP coverage:** guard map 05-anti-drift cập nhật CP1 (full)/CP12 (enforced)/AD-022 (resolved)/+AD-046. Trạng thái CP1–CP15: **ENFORCED**: CP1,2,4,5,9,10,11,12,13,14. **PARTIAL** (đơn-luồng/SQLite xong; race/Postgres chờ Testcontainers): CP6,CP8,CP15 (task 7.4). **PENDING** (cần Docker/tính năng): CP3 (adapter, task 14), CP7 (rotation race, task 8.3). Mọi CP đều có test/đường-đi xác định — không CP nào "mồ côi".
- Điều cần biết: `ContractTests` giờ ref thêm `Bedrock.Domain` + `Identity.Domain` (để reflect Error registry). `ArchitectureTests` ref thêm `Bedrock.Api` + `Mono.Cecil` (quét literal 5 assembly). Literal match là substring case-insensitive — hiện 0 false-positive; nếu tương lai có literal hạ tầng chứa token (vd "showroom") thì chuyển word-boundary.
- Kế tiếp theo `tasks.md`: **task 21 (Definition of Done)** — task cuối, xác nhận toàn bộ DoD §16 + chạy full suite. Các task cần Docker (14 RabbitMq/CP3, 7.4 Testcontainers/CP6/CP8/CP15, 8.3/CP7) là điều kiện Docker — DoD sẽ ghi rõ skip-có-điều-kiện.
- Tổng bản ghi journal: AD 46, DV 16, TO 9, N 48.

---

### N-049 — Task 21 (Definition of Done) — MA TRẬN KIỂM CHỨNG (2026-07-09)
- Verified: ✅ `dotnet test Platform.slnx` → **200 passed, 0 failed, build 0 warning**. Phân rã: ArchitectureTests 31 · ContractTests 2 · Identity.UnitTests 6 · UnitTests 54 · Api.Tests **36 (+1 readiness-503)** · StarHill.Api.Tests 3 · Infrastructure.Tests 68.
- Môi trường: **KHÔNG có Docker** (`docker` not recognized — verified). ⇒ mọi test Testcontainers/Postgres/RabbitMQ KHÔNG chạy được ở đây (N-012). Đây là RESIDUAL tường minh — KHÔNG đánh dấu done khống (nguyên tắc không-bịa).
- Ma trận DoD §16 (8 mục):
  1. no-business-in-core (CP1 tên+literal) + Api⊥Infra (CP2) → ✅ `NoBusinessInCoreTests` + `NoBusinessInCoreLiteralTests` (Mono.Cecil) + `ApiBoundaryTests`.
  2. module 5-project + compose Host + boundary (CP4) → ✅ Identity module + `ModuleBoundaryTests` + `HostSmokeTests` (compose 2 nửa Infra/Api — DV-013 giữ I7; "1 dòng" → 2 nửa có lý do, ghi DV-013).
  3. adapter RabbitMQ + 0-file-lõi-sửa (CP3) → ⏳ **PENDING (Docker)** — task 14.
  4. outbox 1-tx (CP6) + inbox idempotent (CP8) + claim exclusive+dead-letter (CP15) + domain-event atomic (CP14) → 🟡 **PARTIAL**: CP14 ✅ (`DomainEventDispatchTests`); CP6 same-transaction ✅ + CP8 idempotency đơn-luồng ✅ + CP15 backoff/threshold/dead-letter ✅ (SQLite, task 6/7.3); **claim skip-locked đa-instance + race Postgres CP6/CP8/CP15 chờ Docker — task 7.4**.
  5. boot fail-fast thiếu config/port (CP9) → ✅ `RequiredPortsValidatorTests` + `HostSmokeTests.Host_fails_fast_when_required_jwt_secret_is_missing` (AD-045 ValidateOnStart).
  6. telemetry 3 trụ + `traceId`==`X-Correlation-Id`==trace (CP10) → ✅ `BedrockPipelineTests` (OTel providers + traceparent unity + metric http.server.request.duration) — AD-044.
  7. health live/ready + dependency-down→503/live-200 (R34) → ✅ `BedrockPipelineTests` (live/ready 200 + **readiness-503 khi check "ready" Unhealthy** — thêm ở task 21 để verify TRỰC TIẾP thay vì tin framework).
  8. không secret trong repo + validate-on-start options bắt buộc (F35) → ✅ task 19 (appsettings sạch + `UserSecretsId` + Host fail-fast test).
- **Kết luận task 21:** DoD phần **không-Docker HOÀN TẤT + verify thật** (6/8 mục ✅; mục 4 phần đơn-luồng ✅). **Chưa thể sign-off toàn bộ** vì mục 3 + phần race/đa-instance của mục 4 cần Docker (task 14/7.4) + CP7 rotation-race (task 8.3) — môi trường hiện không có Docker. Task 21 GIỮ `[ ]` (không đánh dấu done khi acceptance "Testcontainers ... tất cả xanh" chưa chạy được) — trung thực. Khi có Docker: chạy task 7.4/8.3/14 → CP3/CP6-full/CP7/CP8-full/CP15 → sign-off DoD trọn vẹn.
- Trạng thái CP1–CP15: ENFORCED CP1,2,4,5,9,10,11,12,13,14 (10/15); PARTIAL CP6,8,15 (đơn-luồng ✅/race Docker); PENDING CP3,7 (Docker). Không CP nào mồ côi.
- Tổng bản ghi journal: AD 46, DV 16, TO 9, N 49.

---

### N-050 — Task 7.5 (Outbox retention/cleanup) VERIFY THẬT (2026-07-09)
- Verified: ✅ `dotnet test Platform.slnx` → **203 passed, 0 failed, build 0 warning**. Phân rã: ArchitectureTests 31 · ContractTests 2 · Identity.UnitTests 6 · UnitTests 54 · Api.Tests 36 · StarHill.Api.Tests 3 · **Infrastructure.Tests 71 (+3 retention)**.
- **Phát hiện quan trọng:** task 7.5 làm được KHÔNG cần Docker (khác 7.4/8.3/14). Logic retention là thao tác DELETE theo điều kiện trên bảng quan hệ → SQLite in-memory (`PersistenceHarness`) đủ để chứng minh "chọn đúng tập row hết hạn" (đúng yêu cầu nghiệm thu R8.5). Testcontainers chỉ cần cho hành vi Postgres-specific (skip-locked/xmin), không phải cho retention.
- **Kiến trúc (AD-047):** base cấp LOGIC `EfOutboxRetention<TContext>.PurgeAsync()` + `AddOutboxRetention<TContext>()` (scoped, named-options per-context); Host lên lịch chạy — **nhất quán với dispatcher (base KHÔNG có hosted-service)**. Đây là điểm dễ drift nếu vô ý cho base tự chạy `BackgroundService` → sẽ có 2 mô hình lịch trái ngược trong platform. Giữ 1 mô hình.
- **An toàn (bản chất R8.5):** vị từ xoá processed LUÔN kèm `dead_lettered_at == null` → không đụng dead-letter theo nhánh processed; pending (`processed_at == null`) không bao giờ khớp → không mất event chưa gửi. Dead-letter chỉ bị dọn khi app CHỦ ĐỘNG set `DeadLetterRetention` (mặc định `null` = giữ vô thời hạn — mất dead-letter = mất bằng chứng poison).
- **Provider-conditional (mirror `EfOutboxDispatcher`):** Npgsql → `ExecuteDeleteAsync` (set-based, không nạp entity); provider khác (SQLite test) → `ToListAsync` + lọc client-side + `RemoveRange` + `SaveChanges` (SQLite không dịch được so sánh `DateTimeOffset` — cùng lý do DV-010). Vị từ `SelectExpired` client-side khớp đúng vị từ SQL để hành vi 2 provider trùng khớp. RESIDUAL: nhánh `ExecuteDeleteAsync` Npgsql chưa test được ở đây (chờ Docker/task 7.4) — cùng giới hạn DV-010.
- **Files:** `platform/src/Bedrock.Infrastructure/Persistence/Messaging/OutboxRetentionOptions.cs` + `EfOutboxRetention.cs`; DI `AddOutboxRetention<TContext>` trong `OutboxDispatcherExtensions.cs`; test `platform/tests/Bedrock.Infrastructure.Tests/OutboxRetentionTests.cs` (3 fact).
- **Trạng thái tasks.md:** `7.5` → `[x]`. Parent `7.` GIỮ `[ ]` vì `7.4` (Testcontainers CP6/CP8/CP15) vẫn `[ ]` (cần Docker). Không đánh dấu parent done khống.
- Kế tiếp: các task còn lại đều Docker-gated (7.4/8.3/14) → không chạy được trong môi trường này; task 21 sign-off trọn vẹn chờ Docker (N-049).
- Tổng bản ghi journal: AD 47, DV 16, TO 9, N 50.

### N-051 — Task 14 (Adapter RabbitMQ): core F29 XONG & verify; Testcontainers publish deferred (Docker, cùng nhóm 7.4/8.3)
- **Đã xong & verify (KHÔNG cần Docker):** adapter code (publisher + mapper + resilience + options + `AddRabbitMqMessaging`) build 0 warning; CP3 `AdapterIsolationTests` xanh (Adapters.* chỉ ref Bedrock.Application, KHÔNG Api/Infra/module/EF/ASP.NET/Npgsql) + negative control; 13 unit test thuần (mapper/resilience-retry/options-validate/Replace-override). Toàn suite xanh.
- **F29 "cắm không sửa lõi" — CHỨNG MINH:** thêm adapter CHỈ tạo file mới dưới `src/Adapters/**` + test + pin package (CPM) + slnx; KHÔNG sửa file lõi `Bedrock.*` nào (git-diff sạch phía lõi). CP3 khoá điều này bằng máy.
- **API RabbitMQ.Client 7.x** xác minh qua tài liệu chính thức rabbitmq.com (không bịa): async TAP, `BasicPublishAsync(exchange,routingKey,mandatory,basicProperties,body,ct)`, publisher-confirms qua `CreateChannelOptions(publisherConfirmationsEnabled,publisherConfirmationTrackingEnabled)`.
- **Integration ĐÃ CHẠY (Docker có sẵn — 2026-07-10):** `RabbitMqPublishIntegrationTests` (Testcontainers.RabbitMq 4.13.0, image `rabbitmq:3.13`) spin broker thật, publish `OutboxMessage` qua `RabbitMqEventBusPublisher` → consumer bind topic `identity.user_token_refreshed` nhận lại ĐÚNG payload + headers (event-type/schema-version) + MessageId + CorrelationId. PASS (51s gồm pull image). `[SkippableFact]` + `Skip.IfNot(dockerAvailable)` → tự bỏ qua nếu môi trường thiếu Docker (N-012, KHÔNG xoá). Task 14 nay `[x]` trọn vẹn.
- Provenance: `platform/src/Adapters/Messaging.RabbitMq/**`, `platform/tests/Adapters/Adapters.Messaging.RabbitMq.Tests/**`, `platform/tests/Bedrock.ArchitectureTests/AdapterIsolationTests.cs`, `Directory.Packages.props` (RabbitMQ.Client 7.2.1, Microsoft.Extensions.Resilience 10.7.0, Microsoft.Extensions.Configuration 10.0.9).

### N-052 — Task 7.4 (Outbox/Inbox Testcontainers/PostgreSQL) ĐÃ CHẠY — CP6/CP8/CP15 enforced + fix gốc SKIP LOCKED (2026-07-10)
- **3 integration test Postgres xanh** (`PostgresOutboxInboxTests`, Testcontainers.PostgreSql 4.13.0 image `postgres:16-alpine`, `[SkippableFact]` skip khi thiếu Docker):
  - **CP6** `State_and_outbox_commit_and_rollback_together`: state + outbox commit CÙNG transaction; throw → rollback cả hai (Postgres thật).
  - **CP15** `Concurrent_dispatchers_never_double_claim`: 2 dispatcher đồng thời (BatchSize=3, 20 message) → mỗi message publish ĐÚNG 1 lần (Distinct==Count==20). LỘ RA bug: nhánh claim Npgsql trước đó CHƯA có SKIP LOCKED → **fix tận gốc AD-049** (raw SQL FOR UPDATE SKIP LOCKED, tên bảng từ model).
  - **CP8** `Concurrent_inbox_mark_is_idempotent_at_db`: 4 task đồng thời mark cùng (message_id, consumer) → đúng 1 thắng, PK guard, đúng 1 row inbox.
- **Bản chất fix gốc (AD-049):** comment code cũ nói "skip-locked" nhưng LINQ chỉ SELECT thường → task 7.4 integration test phơi bày. Đúng tinh thần "test là lưới bắt drift": guard đồng thời buộc hiện thực đúng design §4.5.
- **Điểm kỹ thuật đã học (để không lặp):** (1) `EnsureDeletedAsync` KHÔNG drop được DB đang mở (Postgres 55006) → dùng `EnsureCreated` + `TRUNCATE` reset; (2) tên bảng EF mặc định = tên DbSet property (`States`→`states`), KHÔNG phải tên entity — TRUNCATE phải đúng tên bảng; (3) `PostgreSqlBuilder`/`RabbitMqBuilder` ctor rỗng obsolete ở Testcontainers 4.13 → truyền image tường minh; (4) `PgOutboxDbContext` cô lập (entity state KHÔNG audit/concurrency) để tránh mapping xmin nhiễu vào test outbox.
- Provenance: `platform/tests/Bedrock.Infrastructure.Tests/PostgresOutboxInboxTests.cs` + `EfOutboxDispatcher.cs` (BuildNpgsqlClaimSql). Sequential SQLite tests (inbox dup / dead-letter no-reclaim) vẫn phủ nhánh provider-agnostic.

### N-053 — Task 8.3 (rotation race Testcontainers/PostgreSQL) ĐÃ CHẠY — CP7 enforced (2026-07-10)
- **2 integration test Postgres xanh** (`RefreshTokenRotationRaceTests`, chung container qua `PostgresFixtureDefinition` collection):
  - `Two_concurrent_consumes_exactly_one_wins` (CP7): 2 request đồng thời `TryConsumeAsync` cùng token → `UPDATE ... WHERE revoked_at IS NULL` row-lock ở Postgres → ĐÚNG 1 thắng (không lock ứng dụng).
  - `Consume_and_insert_roll_back_together_when_insert_conflicts` (F5): consume (ExecuteUpdate trong transaction) + insert trùng hash → UNIQUE violation → rollback CẢ HAI → token gốc chưa revoked (không mất token).
- **Chia sẻ container:** đổi `PostgresFixture` từ `IClassFixture` sang `ICollectionFixture` (`[CollectionDefinition("postgres-integration")]`) → outbox/inbox + rotation race dùng CHUNG 1 broker Postgres, chạy tuần tự (mỗi test TRUNCATE reset). `PgOutboxDbContext` giờ map thêm `AddRefreshTokens()` để phục vụ cả hai.
- **Điểm đã học:** class định nghĩa collection KHÔNG được kết thúc bằng "Collection" (CA1711) → đặt tên `PostgresFixtureDefinition`, collection nhận diện qua const `Name`.
- Provenance: `platform/tests/Bedrock.Infrastructure.Tests/RefreshTokenRotationRaceTests.cs` + `PostgresOutboxInboxTests.cs` (collection fixture).

### N-054 — Task 21 (Definition of Done) HOÀN TẤT với Docker — toàn bộ CP1–CP15 enforced (2026-07-10)
- **Chạy TOÀN BỘ suite (Docker sẵn sàng)**: build 0 warning + **224 test xanh, 0 fail, 0 skip** (Testcontainers RabbitMQ + Postgres CHẠY THẬT, không skip): Bedrock.UnitTests 54 · Identity.UnitTests 6 · Bedrock.ContractTests 2 · Bedrock.ArchitectureTests 33 · Bedrock.Api.Tests 36 · StarHill.Api.Tests 3 · Bedrock.Infrastructure.Tests 76 (gồm 5 Postgres integration) · Adapters.Messaging.RabbitMq.Tests 14 (gồm 1 RabbitMQ integration).
- **Ma trận DoD §16 (mỗi mục ↔ guard enforced):**
  - Thêm module = 5 project + Host ráp → CP4/CP5 (`ModuleBoundaryTests`) + module Identity thật.
  - Thêm tech = 1 adapter, 0 file lõi sửa → CP3 (`AdapterIsolationTests`) + adapter RabbitMQ (chỉ thêm file mới).
  - Command ghi + outbox 1 transaction → CP6 (`PostgresOutboxInboxTests`).
  - Domain event atomic → CP14 (`DomainEventDispatchTests`).
  - Dispatcher claim exclusive → CP15 (`Concurrent_dispatchers_never_double_claim`, SKIP LOCKED).
  - Boot fail-fast mọi môi trường → CP9 (`RequiredPortsValidatorTests`) + `HostSmokeTests`.
  - Telemetry 3 trụ + correlation thống nhất → CP10 (task 18).
  - Health live/ready → `HealthEndpoints` + `HostSmokeTests`.
  - Không secret trong repo → AD-045 (task 19, validate-on-start + secret ngoài repo).
- **TOÀN BỘ 21 task `[x]`.** CP1–CP15 đều ✅ ENFORCED trong guard map (05-anti-drift). Anti-drift INV-1..5 xanh; getDiagnostics 4 file spec + journal sạch.
- **Còn lại (ngoài phạm vi spec base):** adapter công nghệ khác (Elasticsearch/Redis/S3/Email/ExternalAuth) + module nghiệp vụ thật là phần MỞ RỘNG dùng base — base đã chứng minh đủ "cắm không sửa lõi".

### N-055 — Đóng nốt khoảng hở AD-047: nhánh Npgsql `ExecuteDeleteAsync` của retention đã integration-test (2026-07-10)
- Rà soát cuối phát hiện AD-047 (retention task 7.5) chỉ mới test nhánh SQLite client-side; nhánh **Npgsql set-based `ExecuteDeleteAsync`** (production) ghi chú hoãn "→ task 7.4". Docker sẵn sàng → đóng gốc ngay.
- Thêm 2 test Postgres (`PostgresOutboxInboxTests.Retention_purge_deletes_expired_processed_keeps_pending_and_dead_letter` + `..._deletes_expired_dead_letter_when_ttl_set`): chứng minh câu DELETE set-based xoá ĐÚNG processed-cũ (giữ pending/processed-mới/dead-letter khi TTL null), và xoá dead-letter-cũ khi bật `DeadLetterRetention`. Cả hai xanh.
- `BuildAsync` (harness Postgres) nay đăng ký thêm `AddOutboxRetention<PgOutboxDbContext>` (tham số `configureRetention`). Bất biến an toàn (không xoá pending) giữ nguyên trên cả 2 provider.
- Kết quả: AD-047 → ENFORCED cả 2 nhánh provider. KHÔNG còn AD/CP nào ở trạng thái PARTIAL/PENDING trong guard map.
- Provenance: `platform/tests/Bedrock.Infrastructure.Tests/PostgresOutboxInboxTests.cs` (2 test retention) + `EfOutboxRetention.cs` (nhánh Npgsql).

### N-056 — Vận hành hoá base cho production (post-base, 2026-07-10) — migrations + Docker + CI
- **Bối cảnh:** user chọn hướng "vận hành hoá base cho production" sau khi spec base (21 task) hoàn tất. Đây là NHÁNH MỞ RỘNG post-base (không thuộc 21 task gốc; theo dõi bằng journal AD-050/051/052 thay vì thêm task vào `tasks.md` — giữ spec base nguyên vẹn 21/21).
- **A. EF migrations per-module (AD-050):** `dotnet-ef` 10.0.9 pin ở `.config/dotnet-tools.json`; `IdentityDbContextFactory` (design-time, khớp options runtime); migration `InitialCreate` (schema identity, jsonb, unique/partial index); verify bằng `IdentityMigrationTests` (MigrateAsync trên Postgres thật). Thay `EnsureCreated` (dev-only). Migrate **out-of-band** lúc deploy (không auto-migrate → an toàn đa-instance).
- **B. Dockerfile Host (AD-051):** multi-stage (SDK build → aspnet runtime), non-root (`$APP_UID`), cổng 8080; `.dockerignore` lọc bin/obj. Verified: `docker build` OK; `docker run` thiếu secret → **fail-fast** (đúng F35); có secret qua env → `/health/live`=200.
- **C. CI (AD-052):** `.github/workflows/ci.yml` (repo ROOT — N-030: cây gốc là repo authoritative) — job build+test (0-warning + full suite gồm Testcontainers + JournalConsistency) + job docker-build. Nâng anti-drift lên tầng CI/PR.
- **Điểm đã học / cần biết:**
  - `dotnet new tool-manifest` tạo file ở CWD (`platform/dotnet-tools.json`) — đã DI CHUYỂN về chuẩn `.config/dotnet-tools.json` để `dotnet tool restore` tìm chắc chắn trên CI.
  - Migration files EF Core 10 sinh ra QUA được analyzer + `TreatWarningsAsErrors` (build full 0 warning) — không cần loại trừ.
  - `Microsoft.EntityFrameworkCore.Design` để `PrivateAssets="all"` (chỉ design-time, không chảy runtime/consumer).
  - `appsettings.json` Host (task 19/AD-045) đã để secret RỖNG (F35) → container/prod PHẢI cấp secret qua env; đây là lý do container fail-fast khi chạy trần (hành vi đúng, không phải bug).
- **CÒN LẠI (nếu đi tiếp production):** đẩy image lên registry + deploy manifest (k8s/compose) + bước `dotnet ef database update`/bundle trong pipeline deploy; các adapter công nghệ khác + module nghiệp vụ thật. Đều là mở rộng, base + đường vận hành đã sẵn sàng.
- Provenance: `.config/dotnet-tools.json`, `src/Modules/Identity/Identity.Infrastructure/Persistence/{IdentityDbContextFactory.cs,Migrations/*}`, `src/Host/StarHill.Api/Dockerfile`, `platform/.dockerignore`, `.github/workflows/ci.yml`, `tests/Modules/Identity.IntegrationTests/*`.

### N-057 — Capstone vận hành hoá: docker-compose full-stack chạy end-to-end (post-base, 2026-07-10)
- **Đã làm:** `platform/docker-compose.yml` (Postgres + Host) + opt-in migrate (`Bedrock:ApplyMigrationsOnStartup`, mặc định TẮT — AD-053). Verified in-session: `docker compose up -d --build` → postgres healthy → host áp migration → `/health/ready`=200 (readiness DB-backed, log `SELECT 1`) + `/health/live`=200 → `docker compose down -v` sạch.
- **Ý nghĩa (bản chất):** đây là bằng chứng TÍCH HỢP — build image + nối Postgres thật + migrate + boot fail-fast pass + serve — toàn chuỗi chạy CÙNG NHAU. Trước đó mỗi mảnh verify riêng (migration `IdentityMigrationTests`, image `docker build/run`); compose khoá phần "chúng chạy chung".
- **Quyết định giữ kỷ luật:** opt-in migrate mặc-định-tắt → KHÔNG phá AD-050 (prod đa-instance out-of-band). Compose là dev/staging single-instance nên bật an toàn.
- **Cố ý KHÔNG over-build:** chưa thêm RabbitMQ + outbox dispatcher vào compose vì Host chưa wire adapter/dispatcher (là feature riêng, cần AddRabbitMqMessaging + AddOutboxDispatcher + lịch chạy worker ở Host). Ghi lại để bước sau nếu cần.
- **CÒN LẠI (production thật):** push image lên registry + k8s/compose-prod manifest + bước `dotnet ef database update`/EF bundle trong pipeline deploy (thay opt-in-startup) + wire messaging (RabbitMq adapter + dispatcher worker) nếu dùng event-driven. Nền tảng + đường vận hành (migrate/image/compose/CI) đã sẵn sàng và kiểm chứng.
- Provenance: `platform/docker-compose.yml`, `src/Host/StarHill.Api/Program.cs` (opt-in migrate block).

### N-058 — Đóng loop migrate prod (EF bundle) + thêm .gitignore (post-base, 2026-07-10)
- **EF migration bundle (AD-055):** đóng nốt vòng "prod migrate out-of-band" mà AD-050/AD-053 tự mở. `dotnet ef migrations bundle --self-contained -r <rid>` → executable độc lập. Verified THẬT: chạy bundle → áp `InitialCreate` lên Postgres container → 3 bảng schema `identity`. CI job `migration-bundle` sinh bundle linux-x64 + upload artifact cho CD. CD chạy `./efbundle-identity --connection "$CONN"` TRƯỚC rollout Host (không auto-migrate prod).
- **.gitignore (hygiene thương mại):** repo TRƯỚC ĐÓ KHÔNG có `.gitignore` (rủi ro commit nhầm bin/obj/bundle). Thêm `platform/.gitignore` chuẩn .NET (`bin/ obj/ *.user .vs/ efbundle*`). Fix gốc: artifact tái sinh từ source, không thuộc VCS.
- **Ba tầng migrate — dùng đúng chỗ:** (1) test integration → `MigrateAsync` (IdentityMigrationTests); (2) dev/compose single-instance → opt-in `ApplyMigrationsOnStartup` (AD-053); (3) prod đa-instance → EF bundle out-of-band ở bước deploy (AD-055). Cả ba nhất quán về migration set (cùng `Migrations/`), khác cách ÁP theo môi trường.
- **CÒN LẠI (cần user quyết — không tự suy đoán):** registry cụ thể (ghcr/ACR/ECR...) để push image; k8s manifest hay compose-prod; có dùng messaging event-driven không (wire RabbitMq adapter + dispatcher worker ở Host). Các mục này cần lựa chọn hạ tầng + credential, KHÔNG kiểm chứng được trong môi trường hiện tại nếu thiếu.
- Provenance: `.github/workflows/ci.yml` (job migration-bundle), `platform/.gitignore`; bundle verified in $env:TEMP (không commit).

### N-059 — Backbone event-driven verify END-TO-END + phát hiện jsonb KHÔNG byte-exact (post-base, 2026-07-10)
- **Đã làm:** `tests/Messaging.IntegrationTests/OutboxToRabbitMqEndToEndTests` (Testcontainers Postgres + RabbitMQ THẬT) — chuỗi `Outbox → EfOutboxDispatcher (claim SKIP LOCKED) → RabbitMqEventBusPublisher → RabbitMQ → consumer` chạy tích hợp: seed outbox → dispatch → consumer nhận payload/MessageId → `ProcessedAt` set. Khoá phần "các mảnh chạy CHUNG" (trước chỉ test riêng: dispatcher+publisher-giả; publisher+broker).
- **⚠️ PHÁT HIỆN QUAN TRỌNG (điều nên biết) — payload `jsonb` KHÔNG byte-exact:** cột `payload jsonb` (design §4.5, isNpgsql=true) → Postgres NORMALIZE text JSON khi round-trip (thêm space sau `:`, có thể đổi thứ tự key, bỏ whitespace thừa). Ví dụ lưu `{"hello":"world"}` → đọc lại `{"hello": "world"}`. Dispatcher publish bản ĐÃ normalize.
  - **Bản chất:** jsonb là lưu trữ JSON NGỮ NGHĨA (semantic), không phải chuỗi byte-exact. Payload publish semantically-equal nhưng KHÁC text.
  - **Hệ quả (không phải bug):** khớp design §9.1 — consumer PHẢI là **tolerant reader** (deserialize theo schema, KHÔNG so byte/hash payload text). Ai giả định payload byte-exact (vd ký/hash chuỗi payload) sẽ sai. Nếu cần byte-exact → phải lưu payload dạng `text` (mất query/index jsonb) — tradeoff, chưa cần.
  - **Sửa ở TEST (không phải code):** assert bằng `JsonNode.DeepEquals` (so ngữ nghĩa) thay vì so string. Code đúng (jsonb là chủ đích §4.5).
- **CÒN LẠI của event-driven (chưa làm — cần user quyết):** wire dispatcher như BackgroundService trong Host + AddRabbitMqMessaging ở Host + lịch poll (base cố ý KHÔNG có hosted-service dispatcher — Host quyết lịch). Backbone đã chứng minh chạy; phần "lịch chạy tự động ở Host" là lựa chọn vận hành.
- Provenance: `platform/tests/Messaging.IntegrationTests/**`; verified in-session 2026-07-10 (Postgres + RabbitMQ containers).

### N-060 — Worker tự phát outbox (opt-in) đóng khoảng hở runtime N-059 (post-base, 2026-07-10)
- **Đã làm (AD-056):** base cấp `OutboxDispatcherHostedService<TContext>` + `AddOutboxDispatcherWorker<TContext>()` (opt-in — `AddBedrockPersistence`/`AddOutboxDispatcher` KHÔNG tự đăng ký); Host `StarHill.Api` gate messaging sau `Bedrock:Messaging:Enabled` (mặc định TẮT): bật → `AddRabbitMqMessaging` + `AddOutboxDispatcher<IdentityDbContext>` + `AddOutboxDispatcherWorker<IdentityDbContext>` + `AddIntegrationEventRegistry`.
- **Verify THẬT:** `Messaging.IntegrationTests/OutboxDispatcherWorkerEndToEndTests` (Postgres+RabbitMQ) — seed outbox → worker qua đường Host thật (`AddOutboxDispatcherWorker`→`AddHostedService`→`BackgroundService.StartAsync`) TỰ ĐỘNG phát + mark, KHÔNG gọi `DispatchPendingAsync` thủ công. Full suite **229 xanh, 0 warning, 0 skip**; `StarHill.Api.Tests` (3) vẫn xanh với messaging TẮT (không hồi quy).
- **Vì sao KHÔNG mâu thuẫn AD-047:** AD-047 cấm base *tự chạy* scheduler (2 mô hình lịch). Worker opt-in chỉ chạy khi Host gọi tường minh → lịch VẪN do Host quyết (một mô hình). Tiền lệ: base đã ship hosted-service opt-in `RequiredPortsValidator`.
- **Ba bất biến vận hành của worker (đọc kỹ nếu sửa):** (1) scope MỖI lượt qua `IServiceScopeFactory` — dispatcher là Scoped, resolve-từ-root sẽ ném với `ValidateScopes=true` (N-008); (2) nuốt+log Warning lỗi hạ tầng CẢ LƯỢT rồi poll lại (lỗi per-message đã do dispatcher backoff/dead-letter); (3) shutdown êm theo `stoppingToken` (OperationCanceledException lúc delay/dispatch KHÔNG log như lỗi).
- **⚠️ Bài học lặp (đã dính lại):** class định nghĩa xUnit collection KHÔNG được kết thúc bằng "Collection" (CA1711) — y hệt N-053. Đã đặt `MessagingIntegrationDefinition`. Ghi lại lần 2 để KHÔNG tái phạm lần 3.
- **Fix flaky tận gốc:** 2 lớp e2e messaging (mỗi lớp tự spin Postgres+RabbitMQ) chạy SONG SONG → 4 container tranh tài nguyên Docker → fail 4s không tất định. Đưa cả hai vào cùng collection `messaging-integration` → xUnit chạy TUẦN TỰ (collection = đơn vị song song hoá). Cùng tinh thần serialize N-053. KHÔNG dùng fixture chung (mỗi lớp cần container/exchange riêng để cô lập dữ liệu; chỉ cần bỏ song song).
- **CÒN LẠI (bước kế, tách có chủ đích — không over-build):** EMIT `UserTokenRefreshedIntegrationEvent` từ use case rotation qua `IOutboxWriter` (N-040 còn mở) để chuỗi "refresh token → event trên bus" chạy trong sample Host. Nay ĐÃ có consumer path (worker→RabbitMQ) nên emission không còn là đầu cơ — nhưng chạm business logic Identity + cần test riêng → làm thành increment kế tiếp. Thêm RabbitMQ vào docker-compose là hệ quả của bước đó (cần producer để demo có ý nghĩa). Registry push + k8s vẫn cần hạ tầng/credential (không kiểm chứng cục bộ).
- Provenance: `platform/src/Bedrock.Infrastructure/Persistence/Messaging/{OutboxDispatcherHostedService.cs,OutboxDispatcherWorkerOptions.cs}`, `DependencyInjection/OutboxDispatcherExtensions.cs`, `src/Host/StarHill.Api/{Program.cs,StarHill.Api.csproj}`, `tests/Messaging.IntegrationTests/{OutboxDispatcherWorkerEndToEndTests.cs,MessagingIntegrationDefinition.cs}`.

### N-061 — Emission event rotation nối xong → N-040 ĐÓNG (post-base, 2026-07-10)
- **Đã làm (AD-057):** `RefreshAccessTokenUseCase` enqueue `UserTokenRefreshedIntegrationEvent` qua `IOutboxWriter` trong CÙNG transaction rotation (sau consume+insert, trước SaveChanges) — chỉ trên đường thành công.
- **Verify THẬT 2 tầng:** (1) unit `RefreshAccessTokenUseCaseTests` — emit-on-success (đúng UserId/EventType, enqueue trước SaveChanges) + `Failed_rotations_do_not_emit_event` (4 nhánh fail: unknown/expired/reuse/lost-race đều KHÔNG emit); (2) integration `RefreshRotationEmitsEventTests` (Postgres THẬT) — rotation trên `IdentityDbContext` thật → row `outbox_message` (`identity.user_token_refreshed`, payload có UserId, KHÔNG có token thô, ProcessedAt null) đọc lại ở SCOPE MỚI = persist thật cùng transaction.
- **Vì sao cần CẢ integration (không chỉ unit):** unit fake writer; CP6 dùng DbContext test khác; smoke dừng ở 400. Chỉ integration mới chứng minh IdentityDbContext map outbox đúng + EfOutboxWriter cùng-context ghi được (bắt lỗi mapping/wiring thật). Đóng đúng seam N-040 để ngỏ.
- **N-040 status:** phần emission ĐÓNG (AD-057). Phần "claim access token chỉ `sub`, role/permission cần user store" của N-040 VẪN đúng (chưa có user store — ngoài phạm vi).
- **Full suite:** 231 xanh (Identity.UnitTests 7, Identity.IntegrationTests 2), 0 warning, 0 skip. ArchTest 33 (CP11 use case ⊥ Messaging.Dispatch giữ nguyên — IOutboxWriter ở Messaging).
- **CÒN LẠI (hạ tầng, tách riêng):** thêm RabbitMQ vào docker-compose + bật `Bedrock:Messaging:Enabled` → demo chuỗi "gọi /v1/identity/token/refresh → event lên bus" trong compose thật (giờ đã đủ mảnh: producer AD-057 + worker AD-056 + adapter). Registry push + k8s vẫn cần credential (không kiểm chứng cục bộ).
- Provenance: `platform/src/Modules/Identity/Identity.Application/RefreshToken/RefreshAccessTokenUseCase.cs`, `Identity.Contracts/Events/UserTokenRefreshedIntegrationEvent.cs` (comment cập nhật), `tests/Modules/Identity.UnitTests/RefreshAccessTokenUseCaseTests.cs`, `tests/Modules/Identity.IntegrationTests/{RefreshRotationEmitsEventTests.cs,IdentityIntegrationDefinition.cs}`.

### N-062 — Compose capstone event-driven chạy trên IMAGE THẬT + phát hiện guest-loopback (post-base, 2026-07-10)
- **Đã làm (AD-058):** `docker-compose.yml` thêm RabbitMQ + bật `Bedrock:Messaging:Enabled` → verify chuỗi Outbox→worker→RabbitMQ trên IMAGE BUILD THẬT (không phải Testcontainers/ServiceCollection).
- **Verify in-session (dứt khoát):** `docker compose up -d --build` → postgres+rabbitmq healthy → host /health/live+ready=200 → log "Outbox dispatcher worker started ... poll every 00:00:05" → seed 1 row outbox qua psql → 10s sau `processed_at` non-null + `error_count=0` + không dead-letter → worker image thật đã publish tới RabbitMQ compose (publisher-confirms). `docker compose down -v` sạch.
- **⚠️ PHÁT HIỆN QUAN TRỌNG (deployment thật) — RabbitMQ `guest` chỉ LOOPBACK:** user `guest` mặc định chỉ connect được từ loopback; container→container (mạng compose) là non-loopback → guest BỊ TỪ CHỐI. Testcontainers KHÔNG lộ lỗi này (map localhost). Fix gốc: dùng user riêng `bedrock` (`RABBITMQ_DEFAULT_USER`/`PASS`) khớp `RabbitMq__UserName/Password` — KHÔNG bật loopback cho guest. Bài học: mọi deployment RabbitMQ thật PHẢI có credential riêng, không dựa guest.
- **Kỹ thuật verify (để tái dùng):** publisher-confirms ack kể cả message unroutable (mandatory=false) → chỉ cần seed outbox + kiểm `processed_at` flip (không cần bind queue/management API). `processed=t & error_count=0` = kết nối+publish OK; `error_count≥1 & processed=null` = broker unreachable/auth-refused. Tất định, ít mảnh.
- **jsonb qua psql -c:** payload JSON literal `{"..."}` bị PowerShell/cmd escape hỏng → dùng `jsonb_build_object('userId','...')` (chỉ single-quote) để tránh escaping. Ghi lại để không mất thời gian lần sau.
- **Ranh giới phủ (không trùng lặp):** Testcontainers phủ CODE PATH; compose phủ ARTIFACT (Dockerfile publish + config binding env + service networking + boot ordering + credential). Chuỗi event-driven nay verify ở CẢ hai lớp.
- **CÒN LẠI (cần credential/hạ tầng — không kiểm chứng cục bộ):** push image lên registry (ghcr/ACR/ECR) + k8s manifest + wire messaging cho module nghiệp vụ thật. Nền tảng + đường vận hành đã đầy đủ và kiểm chứng.
- Provenance: `platform/docker-compose.yml`; verified in-session 2026-07-10 (compose up/seed/down).

### N-063 — Phân tích phạm vi nửa CONSUME của backbone (design-first, CHƯA triển khai) (post-base, 2026-07-10)
- **Bối cảnh:** sau khi nửa PUBLISH hoàn tất + verify 2 lớp (producer AD-057 → outbox → worker AD-056 → adapter AD-048 → broker; compose AD-058), rà soát "cách nhìn sâu rộng" để tìm mảnh còn lại. Nửa đối xứng là CONSUME (§7.3).
- **Trạng thái thực (đã verify bằng grep toàn `platform/src`):** building-blocks CONSUME đều CÓ + đã test ở tầng đơn vị/store:
  - `IInboxStore` + `EfInboxStore` (idempotency, PK `(message_id, consumer)`) — CP8 enforced.
  - `IIntegrationEventTypeRegistry` + `IntegrationEventTypeRegistry` (EventType→CLR, unknown→null=dead-letter) — test resolve known/null.
  - `IIntegrationEventHandler<T>` (contract, multi-impl).
  - NHƯNG: **KHÔNG thành phần nào INJECT/dùng** `IInboxStore`/`IIntegrationEventTypeRegistry`; `IIntegrationEventHandler<T>` **chưa có impl nào**. ⇒ KHÔNG có consumer orchestrator (dispatch một message → registry resolve → inbox mark + handler cùng transaction → ack/nack/dead-letter) và KHÔNG có subscriber (RabbitMQ `BasicConsume` → feed orchestrator).
  - **Hệ quả loose-end:** `AddIntegrationEventRegistry(...)` tôi wire trong Host (AD-056 messaging block) hiện **registered-but-unused** (chỉ có nghĩa khi có consumer). Vô hại (singleton boot, forward-looking, đúng ý đồ design) nhưng ghi rõ để không nhầm là đã có consumer.
- **Design nói gì (§7.3):** ĐỊNH NGHĨA PROTOCOL consumer (sequence: Bus→Consumer→`TryMarkProcessedAsync`→ nếu lần đầu: `HandleAsync` + COMMIT inbox+business cùng transaction; nếu trùng PK: rollback/ACK bỏ qua; unknown EventType→dead-letter R17.3; handler phát event mới → qua `IOutboxWriter` cùng transaction R9.4). NHƯNG **KHÔNG chỉ rõ "Consumer" là project nào** — base 21-task cố ý chỉ dựng building-blocks (inbox store/registry/handler contract), KHÔNG dựng orchestrator/subscriber (không có task nào cho consumer pump).
- **Khuyến nghị thiết kế (đối xứng nửa publish — chưa áp, chờ chốt phạm vi):**
  1. **Agnostic dispatch core = Bedrock.Infrastructure** (MIRROR `EfOutboxDispatcher`): một component nhận `(rawPayload, eventTypeHeader, messageId)` → registry resolve type (unknown→dead-letter, không crash) → mở transaction → `IInboxStore.TryMarkProcessedAsync` → nếu lần đầu gọi `IEnumerable<IIntegrationEventHandler<T>>.HandleAsync` → COMMIT (inbox+business nguyên tử) → trả quyết định Ack/Nack/DeadLetter. Đây là CƠ CHẾ tái dùng, race-sensitive, agnostic → đúng chỗ ở base (cùng lý lẽ AD-010). Verifiable Testcontainers (Postgres): feed message → handler chạy đúng-một-lần + redeliver→idempotent + unknown→dead-letter.
  2. **Subscriber = Adapters.Messaging.RabbitMq** (MIRROR `RabbitMqEventBusPublisher`): `BasicConsume` trên queue → feed dispatch core → Ack/Nack theo kết quả. Verifiable Testcontainers (Postgres+RabbitMQ): publish→consume→handler→inbox.
  3. **Topology = APP/Host quyết** (KHÔNG bake vào base/sample — đây là chỗ dễ suy đoán sai): tên queue, handler nào subscribe event nào, số competing-consumer + prefetch, chính sách nack/requeue vs DLQ, có bind DLQ exchange không. Những cái này phụ thuộc nghiệp vụ/vận hành cụ thể.
- **VÌ SAO CHƯA TỰ TRIỂN KHAI (đúng nguyên tắc user):** subsystem lớn + chứa quyết định APP-SPECIFIC thật (topology/competing-consumer/nack policy). Bake đơn phương = suy đoán nhu cầu app. Theo "chuẩn bị thiết kế rõ → valid → verifiable → mới triển khai" + "không suy đoán": trình khuyến nghị + chờ chốt phạm vi (build agnostic core base — rõ ràng in-scope; +/− subscriber adapter; topology để app). Nửa publish là điểm hoàn tất trung thực; consumer là bước MỞ RỘNG có chủ đích.
- Provenance: grep `platform/src` (không component nào inject IInboxStore/registry; không impl IIntegrationEventHandler); design §4.2/§5.2/§7.3; đối chiếu 21-task (không task consumer pump).

### N-064 — Nửa CONSUME hoàn tất (core+subscriber+topology) — chạy end-to-end thật (post-base, 2026-07-10)
- **Đã làm (AD-059+AD-060):** đóng khoảng hở N-063. Port `IIntegrationEventDispatcher` (Application) + core agnostic `EfIntegrationEventDispatcher` (Infrastructure) + subscriber `RabbitMqConsumer` (adapter) + Host topology tối thiểu (queue `starhill.identity`, binding `identity.#`, handler demo `UserTokenRefreshedLogHandler`). Registry (N-063 loose-end) NAY được dùng.
- **Verify 2 tầng:** (1) core `PostgresIntegrationEventDispatcherTests` (Postgres, 3 test: handled-once+inbox / redeliver-idempotent-Duplicate / unknown→DeadLettered không inbox); (2) e2e `RabbitMqConsumeEndToEndTests` (Postgres+RabbitMQ: publish→subscriber thật→dispatch→handler 1 lần+inbox). Full suite **235 xanh, 0 warning, 0 skip** (+4).
- **CP3 giữ bằng PORT (điểm mấu chốt):** adapter subscriber chỉ ref `Bedrock.Application` → logic dispatch phải ở PORT `IIntegrationEventDispatcher` (Application), impl `EfIntegrationEventDispatcher` ở Infrastructure resolve qua DI. Đối xứng publish. CP11 nguyên vẹn (port ở `Messaging.Dispatch` → use case cấm ref).
- **⚠️ Chính sách poison consume (AD-060) — production PHẢI biết:** handler lỗi → NACK requeue=false (tránh hot-loop vô hạn của requeue=true). KHÔNG có DLX = message BỊ DROP khi handler lỗi liên tục. Production PHẢI cấu hình `x-dead-letter-exchange` qua `RabbitMqConsumerOptions.QueueArguments` để poison đi vào DLX. Base cố ý KHÔNG áp DLX mặc định (topology app — N-063).
- **⚠️ Race timing ở TEST (đã fix, bản chất):** publish RabbitMQ nằm TRONG transaction dispatcher NHƯNG TRƯỚC Postgres commit → consumer nhận message TRƯỚC khi `ProcessedAt` commit (đúng at-least-once). `OutboxDispatcherWorkerEndToEndTests` assert `ProcessedAt` tức thì → flaky dưới tải (3 test serial). Fix: POLL `ProcessedAt` KHI worker CÒN CHẠY (không stop trước, không assert tức thì). Production code ĐÚNG — chỉ test cần robust với eventual-consistency giữa broker-delivery và DB-commit.
- **Header event-type = byte[]:** AMQP field table encode string thành longstr → consume ra `byte[]`, decode UTF-8 (không giả định string). MessageId qua `BasicProperties.MessageId`.
- **Multi-module (ghi để sau):** core/inbox/IUnitOfWork resolve theo scope → single-module (sample) dùng chung 1 context. Multi-module: mỗi module một consumer + scope riêng (như dispatcher per-module) để inbox+business đúng context module.
- **CÒN LẠI:** demo consume trong docker-compose (subscriber đã wire ở Host messaging block — chạy khi `Bedrock:Messaging:Enabled=true`; compose AD-058 sẽ tự khởi động subscriber, có thể verify qua log "RabbitMQ consumer started"). Registry push + k8s vẫn cần credential.
- Provenance: `platform/src/Bedrock.Application/Messaging/Dispatch/*`, `platform/src/Bedrock.Infrastructure/Persistence/Messaging/EfIntegrationEventDispatcher.cs`, `platform/src/Adapters/Messaging.RabbitMq/{RabbitMqConsumer,RabbitMqConsumerOptions}.cs`, `platform/src/Host/StarHill.Api/{Program.cs,UserTokenRefreshedLogHandler.cs}`, tests nêu trên.

### N-065 — FULL-LOOP event-driven verify trên IMAGE THẬT (compose, cả publish+consume) (post-base, 2026-07-10)
- **Đã verify in-session (docker compose up --build):** cả worker VÀ consumer khởi động trong image thật — log "Outbox dispatcher worker started for IdentityDbContext (poll every 00:00:05)" + "RabbitMQ consumer started (queue=starhill.identity, consumer=starhill.identity)". /health/ready=200.
- **Toàn vòng (bằng chứng dứt khoát):** seed 1 row outbox `identity.user_token_refreshed` (payload userId=...000009) qua psql → sau 12s:
  - PUBLISH: `outbox_message.processed_at` non-null (published=t), `error_count=0` → worker publish tới RabbitMQ OK.
  - CONSUME: `inbox_message` có 1 row (consumer=`starhill.identity`, processed=t) → subscriber nhận → dispatch → handler → inbox mark.
  - Handler chạy: log "Consumed UserTokenRefreshed (userId=00000000-...-000000000009, eventId=0…0)". (eventId=empty vì payload seed tối giản chỉ có userId — deserialize tolerant điền default; userId đúng = payload chảy xuyên produce→broker→consume→handler.)
- **Ý nghĩa:** TOÀN BỘ backbone event-driven (produce→outbox→worker claim/publish→RabbitMQ→subscribe→dispatch→handler→inbox idempotency) chạy CHUNG trên ARTIFACT THẬT (Dockerfile image + Postgres + RabbitMQ + mạng compose + credential bedrock). Hai nửa (AD-056/057/058 publish + AD-059/060 consume) khép kín. `docker compose down -v` sạch.
- Provenance: `platform/docker-compose.yml` (đã có messaging + rabbitmq từ AD-058); verified in-session 2026-07-10.

### N-066 — CI anti-drift gate được "đánh thức" trên nhánh develop (post-base, 2026-07-10)
- **Bối cảnh (đối chiếu đĩa sau git-sync):** máy này đọc đĩa = lịch sử hợp nhất đầy đủ từ origin/develop (working tree clean, HEAD `fbf7495`). Xác nhận nền tảng HOÀN TẤT: tasks.md 54/54 `[x]`, journal AD-001..AD-060 + N-001..N-065, build 0-warning, 206 test không-Docker xanh; 10 test Docker-gated (`PostgresOutboxInboxTests`/`RefreshTokenRotationRaceTests` trong Bedrock.Infrastructure.Tests) chỉ fail `DockerUnavailableException` — môi trường máy này không có Docker (N-012), KHÔNG phải lỗi logic (máy kia chạy Docker → 235 xanh N-064/N-065).
- **Khiếm khuyết THẬT phát hiện + đóng (AD-061):** CI (AD-052) trigger `push:[main,master]`+`pull_request`, nhưng nhánh tích hợp thực tế = `develop` (origin/HEAD→develop) và `git log --merges` = **0 merge toàn lịch sử** (mọi commit đổ thẳng develop, chưa từng có PR) ⇒ cổng anti-drift (0-warning + Testcontainers + JournalConsistency INV-1..5) **chưa từng chạy**. Fix gốc: thêm `develop` vào `push.branches`. Kèm hardening có căn cứ: `concurrency` cancel-in-progress theo ref (chống contention Testcontainers N-053/N-064 + tiết kiệm runner), `permissions: contents: read` (least-privilege F35/AD-038), `timeout-minutes` (chặn treo).
- **Verify (tối đa cục bộ):** PyYAML 6.0.2 `safe_load` OK + assert shape (push.branches⊇develop, concurrency/permissions/timeouts) chạy in-session. GIỚI HẠN trung thực: KHÔNG execute được GitHub Actions cục bộ (cần push) — lần chạy thật đầu tiên do chính push develop kích hoạt. Đây là bản chất "verifiable tới đâu nói tới đó".
- **Ý nghĩa anti-drift:** đây là mảnh làm cơ chế chống-drift mạnh nhất (CI gate) THỰC SỰ hoạt động trên nhánh nơi công việc đổ vào — trước đó nó "ngủ". Không phải thêm tính năng, mà là kích hoạt đúng chỗ lá chắn đã có.
- **CÒN LẠI (không đổi, credential/hạ tầng-gated):** push image lên registry + k8s manifest + wire messaging cho module nghiệp vụ thật — không kiểm chứng cục bộ, không build đầu cơ (giữ nguyên N-062/N-064).
- Provenance: `.github/workflows/ci.yml`; `git log --merges`/`git branch -a` (0 merge, origin/HEAD→develop); PyYAML validate in-session 2026-07-10.
- Tổng bản ghi journal: AD 61, DV 16, TO 9, N 66.

### N-067 — Launcher verify cố định (`vp`) + fix gốc fixture-skip → suite sạch trên máy KHÔNG Docker (post-base, 2026-07-10)
- **Bối cảnh:** user ra luật command-governance (mọi lệnh qua entry-point cố định, cấm `python -c`/one-liner tuỳ biến). Ba script user nêu (`scripts\vp.cmd`, `tests\validate_ci.py`, `tools\*.ps1`) verify bằng file_search = **CHƯA tồn tại** → dựng mới (repo root).
- **Đã làm — 2 việc gắn nhau:**
  1. **AD-062 launcher cố định:** `scripts/vp.cmd` (mỏng) → `tools/verify.ps1` (orchestrator: `build|ci|test|journal|all`) + `tests/validate_ci.py` (validate `.github/workflows/ci.yml` — thay logic `python -c` ad-hoc lần trước). Anti-drift tầng THAO TÁC (bổ sung guard-test tầng code + JournalConsistency tầng tài liệu).
  2. **AD-063 fix gốc fixture-skip:** phát hiện 16 test integration FAIL (không SKIP) trên máy không Docker dù có `[SkippableFact]`+`Skip.IfNot` + comment "skip nếu thiếu Docker". Bản chất: container tạo bằng field-init `new ...Builder(...).Build()`, mà `.Build()` ping Docker và NÉM `DockerUnavailableException` — nằm NGOÀI `try` (chỉ bọc `StartAsync`) → fixture ném lúc ctor → FAIL trước khi tới `Skip.IfNot`. Fix: đưa `.Build()` VÀO trong `try` (7 file), field `= null!`. Thiếu Docker → catch → Available=false → SKIP. Có Docker → no-op.
- **VERIFY (qua chính `vp` — tuân governance):** `scripts\vp.cmd` → build 0-warning OK + `VALIDATE CI: OK` + test **total 235, failed 0, succeeded 219, skipped 16** → exit 0. 16 skip = 10 (Bedrock.Infrastructure.Tests: PostgresOutboxInbox×5, RotationRace×2, IntegrationEventDispatcher×3) + Identity.IntegrationTests×2 + Messaging.IntegrationTests×3 + Adapters×1. Trước fix: 10 FAIL riêng Infrastructure.Tests.
- **Ý nghĩa:** (a) cổng verify cục bộ giờ CÓ NGHĨA trên mọi máy dev kể cả không Docker (0 fail, integration skip minh bạch); (b) mọi thao tác verify tái lập được qua `vp` — chống drift tầng lệnh; (c) hành vi CI/máy-có-Docker KHÔNG đổi (16 test chạy thật khi có Docker).
- **7 file fix:** `Bedrock.Infrastructure.Tests/PostgresOutboxInboxTests.cs`, `Identity.IntegrationTests/{IdentityMigrationTests,RefreshRotationEmitsEventTests}.cs`, `Messaging.IntegrationTests/{RabbitMqConsumeEndToEndTests,OutboxToRabbitMqEndToEndTests,OutboxDispatcherWorkerEndToEndTests}.cs`, `Adapters.Messaging.RabbitMq.Tests/RabbitMqPublishIntegrationTests.cs`.
- **Governance từ nay:** verify chạy `vp` (all|build|ci|test|journal); logic mới → sửa `tools/verify.ps1`/`tests/validate_ci.py` (KHÔNG one-liner, KHÔNG đổi tên lệnh). CI runner vẫn dùng `ci.yml` trực tiếp (không phụ thuộc vp).
- Provenance: `scripts/vp.cmd`, `tools/verify.ps1`, `tests/validate_ci.py`; 7 fixture file; `scripts\vp.cmd` run in-session 2026-07-10.
- Tổng bản ghi journal: AD 63, DV 16, TO 9, N 67.

### N-068 — RabbitMQ readiness health-check + gom ConnectionFactory (post-base commercial hardening, 2026-07-10)
- **Khoảng hở phát hiện (rà "thương mại + an toàn"):** adapter RabbitMQ (publisher AD-048 + consumer AD-060) KHÔNG đăng ký health-check nào, dù design §9.6 nói rõ adapter đăng ký check riêng + R34 (readiness phản ánh dependency). ⇒ khi `Bedrock:Messaging:Enabled=true`, `/health/ready` KHÔNG biết broker chết → k8s readiness probe vẫn "ready" → pod nhận traffic nhưng outbox dồn ứ / consumer chết âm thầm. Grep `platform/src` "Health" xác nhận chỉ persistence có check (AD-042).
- **Đã làm (AD-064):** `RabbitMqHealthCheck` (mở connection ngắn → Healthy/Unhealthy) đăng ký trong `AddRabbitMqMessaging` tag `"ready"` → chảy vào `/health/ready` qua `MapBedrockHealth`. Gom `RabbitMqConnectionFactory.Create` (publisher/consumer/health-check dùng chung — trước trùng 2 chỗ). Package `Microsoft.Extensions.Diagnostics.HealthChecks` 10.0.9.
- **Bẫy đã tránh:** `AddTypeActivatedCheck<T>` dùng ActivatorUtilities → chỉ tìm ctor PUBLIC. Để `RabbitMqHealthCheck` `internal` sẽ fail lúc runtime (CI) mà test đăng ký KHÔNG bắt được (không instantiate). → để `public` (như RabbitMqEventBusPublisher).
- **VERIFY qua `vp`:** build 0-warning + validate-ci OK + test **total 237, failed 0, succeeded 220, skipped 17** (+2 so với 235: registration test PASS không-Docker; health-check-integration SKIP). CP3 (`AdapterIsolationTests`) vẫn xanh → HealthChecks (abstraction chuẩn .NET, §9.6 sanction) KHÔNG phá cô lập adapter. Messaging TẮT (mặc định) → không thêm check → HostSmokeTests không hồi quy.
- **Verify được tới đâu:** đăng ký check (tag "ready") verify cục bộ (không Docker); hành vi Healthy khi broker sống → `[SkippableFact]` chạy ở CI/Docker. Broker-down→Unhealthy là nhánh catch-all (mọi exception → Unhealthy), rủi ro thấp.
- Provenance: `platform/src/Adapters/Messaging.RabbitMq/{RabbitMqHealthCheck,RabbitMqConnectionFactory}.cs`, `RabbitMqMessagingExtensions.cs` (đăng ký), publisher/consumer (dùng helper), `AddRabbitMqMessagingTests.cs` + `RabbitMqPublishIntegrationTests.cs`; `vp` run in-session 2026-07-10.
- Tổng bản ghi journal: AD 64, DV 16, TO 10, N 68.

### N-069 — Outbox observability metrics (R24.3/§7.2) + BedrockTelemetry về Application — đóng khoản hoãn N-046 (post-base, 2026-07-10)
- **Khoản hoãn N-046 (task 18):** đã ghi rõ "BedrockTelemetry ở Bedrock.Api → Infrastructure (Api⊥Infra) không emit được metric outbox-lag dưới nguồn chung; NÊN chuyển xuống" + R24.3 catalog "outbox lag + dead-letter count = instrument khi finalize". Grep xác nhận: Meter "Bedrock" được AddMeter nhưng **0 instrument** custom (chỉ AspNetCore/EF/Npgsql/RateLimiting generic). Đây là gap R24.3/§7.2 thật.
- **Đã làm (AD-065):** (1) move `BedrockTelemetry` Api→**Bedrock.Application** (shared, mọi tầng emit được — dùng BCL System.Diagnostics.Metrics). (2) `OutboxMetrics` phát dưới Meter "Bedrock": `bedrock.outbox.published` (counter), `bedrock.outbox.dead_lettered` (counter = R24.3 dead-letter count), `bedrock.outbox.publish.lag` (histogram giây = proxy outbox-lag). Emit INLINE trong `EfOutboxDispatcher` (không DB-poll gauge — tránh sync-over-async/scoped-context-trong-callback).
- **VERIFY qua `vp`:** build 0-warning + validate-ci OK + test **total 239, failed 0, succeeded 222, skipped 17** (+2 metric test PASS không-Docker). Telemetry test cũ (BedrockPipelineTests) vẫn xanh → move không phá OTel wiring (AddMeter("Bedrock") vẫn gom). 
- **Bẫy đã tránh:** Meter static process-global → test dispatch song song phát cùng `bedrock.outbox.*` làm sai đếm. Giải: collection `[CollectionDefinition("OutboxMetricsSerial", DisableParallelization = true)]` (tên KHÔNG kết thúc "Collection" — CA1711/N-060) → test metric chạy MỘT MÌNH → đếm chính xác trên mọi máy (kể cả CI có Postgres dispatch tests).
- **TO-011:** chọn publish-lag (tuổi-lúc-publish, inline) thay oldest-pending-age gauge (DB-poll, hoãn). Nếu cần cảnh báo tồn đọng khi dispatcher NGƯNG hẳn → thêm gauge sau (poller + cache).
- **Còn của R24.3 (component chưa finalize):** consumer processing-time + external-auth success/fail metric — instrument khi component đó cần (external-auth chưa có adapter thật).
- Provenance: `platform/src/Bedrock.Application/Observability/BedrockTelemetry.cs` (moved), `Bedrock.Infrastructure/Persistence/Messaging/OutboxMetrics.cs`, `EfOutboxDispatcher.cs` (emit), `Bedrock.Api/Observability/BedrockObservabilityExtensions.cs` (using), `Bedrock.Infrastructure.Tests/OutboxMetricsTests.cs`; `vp` run in-session 2026-07-10.
- Tổng bản ghi journal: AD 65, DV 16, TO 11, N 69.

### N-070 — Metric quan sát CONSUME (R24.3 consumer time) — đối xứng publish, đóng nốt R24.3 phần messaging (post-base, 2026-07-10)
- **Đã làm (AD-066):** `InboxMetrics` phát trong `EfIntegrationEventDispatcher.DispatchAsync` (core agnostic — MỌI transport đi qua): counter `bedrock.inbox.dispatched` (tag `outcome`) + histogram `bedrock.inbox.processing.duration` (s = "consumer time"), dưới Meter chung "Bedrock". Outcome: `handled`/`duplicate`/`dead_lettered`/`failed`.
- **Điểm bản chất:** đo ở CORE (không adapter) → phủ mọi transport tương lai + đúng nơi biết outcome. Tag `failed` bắt nhánh handler-NÉM (→ NACK, không thuộc 3 enum outcome) → quan sát TỈ LỆ LỖI consume (alert poison/hạ tầng). Đối xứng publish (AD-065): giờ có metric cả hai chiều outbox↔inbox.
- **VERIFY qua `vp`:** build 0-warning + test **total 243, failed 0, succeeded 226, skipped 17** (+4 inbox metric test PASS không-Docker: handled/duplicate/dead_lettered/failed — dispatch THẬT qua UoW/inbox/handler trên SQLite). Collection metric đổi tên `OutboxMetricsSerial`→`MessagingMetricsSerial` (dùng chung outbox+inbox, DisableParallelization — Meter static process-global chống flaky).
- **R24.3 status:** request rate/latency/error ✅ (AspNetCore); rate-limit/EF/Npgsql ✅ (AddMeter); outbox lag+dead-letter ✅ (AD-065); consumer time+outcome ✅ (AD-066). CÒN: external-auth success/fail metric — chờ adapter external-auth thật (chưa có; N-063 topology app). Phần messaging của R24.3 nay ĐẦY ĐỦ.
- **Fix nhỏ trong lúc làm:** wrap DispatchAsync bằng try/finally giữ indentation đúng (tránh IDE0055 với TreatWarningsAsErrors); 2 lần build-fail do thiếu using (`OutboxDispatcherOptions`, `Bedrock.Infrastructure.Persistence.Messaging` cho InboxMetrics) — đã sửa, đều lỗi compile lộ ngay ở `vp build`.
- Provenance: `platform/src/Bedrock.Infrastructure/Persistence/Messaging/InboxMetrics.cs`, `EfIntegrationEventDispatcher.cs` (emit try/finally), `Bedrock.Infrastructure.Tests/{InboxMetricsTests,OutboxMetricsTests}.cs`; `vp` run in-session 2026-07-10.
- Tổng bản ghi journal: AD 66, DV 16, TO 11, N 70.

### N-071 — Đóng lỗ hổng guard cho SecurityHeaders (§3.5 slot #5) — control bảo mật không có test (post-base, 2026-07-10)
- **Bài học quy trình (quan trọng):** grep đầu cho security headers bị **"[truncated: too many matches]"** → tôi SUÝT kết luận sai "platform không có security headers" và định build lại. Đọc pipeline thật (`BedrockApiExtensions.UseBedrockApi`) mới thấy slot #5 `SecurityHeadersMiddleware` ĐÃ tồn tại. → **KHÔNG kết luận từ grep bị cắt; luôn verify bằng đọc code thật.**
- **Gap thật tìm được:** `SecurityHeadersMiddleware` (nosniff/X-Frame-Options DENY/Referrer-Policy no-referrer/X-Permitted-Cross-Domain-Policies none) đã hiện thực NHƯNG (a) KHÔNG có guard test (grep Bedrock.Api.Tests = "No matches" — không truncation), (b) bộ header/giá trị là quyết định spec §3.5 không nêu, chưa ghi journal. Vi phạm keystone anti-drift ("mọi control code-enforceable phải có guard").
- **Đã làm (AD-067):** thêm `SecurityHeadersTests` (e2e Theory: /ok 200 + /boom 500) khoá 4 header + giá trị, kiểm cả trên response LỖI (headers set qua `OnStarting` → có mặt trước body kể cả 500 — bằng chứng "MỌI response"). Formalize quyết định: 4 header universally-safe set mặc định; **CSP KHÔNG set** (app-specific — ép sẽ vỡ app, cùng tinh thần AD-035); app tự thêm CSP.
- **VERIFY qua `vp`:** build 0-warning + test **total 245, failed 0, succeeded 228, skipped 17** (+2 test-case Theory). CP2 (Api⊥Infra) + pipeline cũ không hồi quy.
- **CÒN (tùy chọn, không over-engineer):** 4 header cấu hình được qua `HttpSecurityOptions` — hoãn tới khi có app cần giá trị khác (hiện universally-safe).
- Provenance: `platform/src/Bedrock.Api/HttpSecurity/SecurityHeadersMiddleware.cs` (đã có), `platform/tests/Bedrock.Api.Tests/HttpSecurity/SecurityHeadersTests.cs` (mới); `vp` run in-session 2026-07-10.
- Tổng bản ghi journal: AD 67, DV 16, TO 11, N 71.

### N-072 — Dời bộ launcher `vp` vào TRONG `platform/` để base tự-chứa (post-base, 2026-07-10)
- **Bối cảnh:** user xác nhận phạm vi — đang xây BASE (`platform/` + spec `.kiro/specs/platform-base`), KHÔNG dính QR; QR (`resort-qr/`) là dự án riêng sẽ xây LẠI trên base SAU. Nhấn mạnh cô lập base, lâu dài, thương mại. Đối chiếu `git status`: mọi thay đổi của tôi ở `platform/**` + journal + launcher/CI root; **0 file `resort-qr/`** bị đụng (đúng phạm vi).
- **Đã làm (refine AD-062):** dời `scripts/`+`tools/`+`tests/validate_ci.py` từ repo-root VÀO `platform/` → base tự-chứa (mọi thứ base gọn dưới `platform/`, có thể tách repo riêng sau mà không kéo theo QR/foundation). Mới: `platform/scripts/vp.cmd` → `platform/tools/verify.ps1` + `platform/tests/validate_ci.py`.
- **Path sau dời (đã sửa + verify):** `vp.cmd` gọi `%~dp0..\tools\verify.ps1` (trong platform); `verify.ps1` `$Platform = parent($PSScriptRoot)`, `$RepoRoot = parent($Platform)`; `validate_ci.py` `REPO_ROOT = parents[2]` (platform/tests → platform → root) để tới `.github/workflows/ci.yml`.
- **Điểm KHÔNG dời được (yêu cầu GitHub):** `.github/workflows/ci.yml` PHẢI ở `<repo-root>/.github/workflows/` (GitHub chỉ nhận diện workflow ở đó) → giữ repo-root, nhưng mọi job nhắm `working-directory: platform`. Đây là ràng buộc nền tảng CI, không phải rò base ra root.
- **VERIFY qua launcher đã dời:** `platform\scripts\vp.cmd` → build 0-warning + `VALIDATE CI: OK` (path parents[2]→root .github resolve đúng) + test **245, 0 failed, 228 pass, 17 skip** → exit 0. Chạy từ cwd bất kỳ (verify.ps1 dùng path tuyệt đối).
- **Governance từ nay:** verify chạy `platform\scripts\vp.cmd`; logic mới → sửa `platform/tools/verify.ps1` / `platform/tests/validate_ci.py`.
- **Dọn:** thư mục root `scripts/`,`tools/`,`tests/` sau khi xoá file thành rỗng → git không track dir rỗng (không lên commit). CI `ci.yml` không tham chiếu vp/tools (dùng `dotnet` trực tiếp) → không ảnh hưởng.
- Provenance: `platform/{scripts/vp.cmd,tools/verify.ps1,tests/validate_ci.py}` (mới); root cũ đã xoá; `git status` (0 resort-qr); `vp` run in-session 2026-07-10.
- Tổng bản ghi journal: AD 67, DV 16, TO 11, N 72.

### N-073 — OpenAPI doc-gen base (native .NET 10, opt-in) — hoàn tất phần base DV-015 (post-base, 2026-07-10)
- **Đã làm (AD-068):** `AddBedrockOpenApi()` (native `Microsoft.AspNetCore.OpenApi`, document "v1") + `MapBedrockOpenApi()`; `UseBedrockApi` tự map `/openapi/v1.json` KHI Host opt-in (marker `BedrockOpenApiMarker`). Swagger UI vẫn Host (giữ DV-015). Một doc "v1" (base chỉ có V1 — I10, tách per-version khi có v2).
- **Vì sao đây là hoàn tất chứ không ghi đè DV-015:** §120/R22.1 mandate OpenAPI ở Bedrock.Api; DV-015 chỉ HOÃN + ghi "bổ sung AddBedrockOpenApi SAU" (Reversibility High). Tách doc-gen(base infra) vs UI(Host trình bày) tôn trọng đúng ranh giới DV-015. DV-015 nay đánh dấu Resolved (phần base).
- **CVE gặp + fix gốc:** native OpenApi 10.0.9 kéo transitive `Microsoft.OpenApi` 2.0.0 → NU1903 GHSA-v5pm-xwqc-g5wc (HIGH) fail restore (TreatWarningsAsErrors + NuGetAudit). Pin transitive `Microsoft.OpenApi` 2.7.5 (bản resort-qr resolve trong repo, đã vá) — mirror cơ chế AD-028. restore-audit CHÍNH là verify (2.7.5 clear NU1903).
- **VERIFY qua `vp`:** build 0-warning + test **total 247, failed 0, succeeded 230, skipped 17** (+2). Test chứng minh: opt-in → `/openapi/v1.json` 200 + document endpoint VERSIONED ("oa/ping" — native OpenAPI + Asp.Versioning tích hợp OK, không cần WithGroupName thủ công); không opt-in → 404 (opt-in đúng).
- **CÒN (I10 — khi cần):** doc-per-version (ShouldInclude theo GroupName) khi có v2; Swagger UI ở Host; bảo vệ endpoint /openapi ở prod (Host quyết).
- Provenance: `platform/src/Bedrock.Api/OpenApi/BedrockOpenApiExtensions.cs`, `BedrockApiExtensions.cs` (wire opt-in), `Bedrock.Api.Tests/OpenApi/BedrockOpenApiTests.cs`, `Directory.Packages.props` (pin); `vp` run in-session 2026-07-10.
- Tổng bản ghi journal: AD 68, DV 16, TO 11, N 73.

---

### N-074 — Đợt hardening 2026-07-12 (review deep-architecture) + Hardening Ledger + drift cần đóng
- Verified: ✅ `platform\scripts\vp.cmd all` → build 0-warning + validate-ci + **248 test / 0 fail / 17 skip (Docker)**; `vp journal` → INV-1..5 = 5/5 (phiên 2026-07-12, AI Kiro).
- **Bối cảnh:** một AI khác chạy đợt hardening lớn theo `deep-architecture-review-2026-07-12.md` (~50 file, +1262/−448) nhưng hết token giữa chừng → worktree để lại **build/test GÃY**. AI Kiro khôi phục xanh + kiểm toán:
  - **FIX-1 (AD-070):** build gãy `CA1861` migration mới `AddOutboxClaimLease` → `.editorconfig generated_code` (mirror QR-AD-009).
  - **FIX-2 (AD-071):** regression JWT key-ring (A-18 dở) — eager-bind rỗng shadow instance lazy → IDX10703; fix = đăng ký IDEMPOTENT+LAZY chia sẻ 1 instance sign/verify.
- **Sổ theo dõi:** `.kiro/specs/platform-base/hardening-2026-07-12-ledger.md` — bảng A-01..A-35 với MỨC KIỂM CHỨNG (V0 đã-đọc-code / V1 compile+claim / V2 Docker-only / V3 chưa-làm). Đọc ledger trước khi tin "đã xong".
- **Đã V0 (đọc code + test):** A-01 (keyed persistence + `MultiModulePersistenceTests`), A-02 (`EfInboxStore` atomic ON CONFLICT), A-06 (`IdentityNpgsqlOptionsExtensions` MigrationsHistoryTable), A-07 (CI fail-closed 7 fixture), A-18 (JWT unify).
- **DRIFT LỚN (cạm bẫy — đọc kỹ):** AI đợt trước triển khai A-01..A-18 + thêm guard test nhưng **KHÔNG thêm AD entry nào** vào journal. INV-2 chỉ ép "AD đã ghi phải có guard", KHÔNG ép "code mới phải có AD" → journal đứng yên ở AD-069 dù code đi xa (under-documented). Đóng dần: mỗi hạng mục khi V0-hoá → thêm AD mới (số kế tiếp) — KHÔNG bịa; chỉ ghi khi đã đọc code.
- **NEXT (ưu tiên):** V0-hoá + hoàn thiện **A-08** (outbox lease dispatcher — migration `AddOutboxClaimLease` đã có, cần đọc `EfOutboxDispatcher` xác nhận claim-ngắn→publish-ngoài-transaction→mark). Rồi A-03/04/05 (RabbitMQ), A-13/14/15/16/17/26/27 (đọc code V1→V0).
- **Đồng bộ base→starhill (QR-N-002):** hardening chỉ ở `platform/`; `starhill/` còn dùng Bedrock cũ. A-01 (keyed persistence) sẽ đổi chữ ký `AddBedrockPersistence` → khi port phải sửa `Add{Rooms,ResortConfig,Identity}Infrastructure` của starhill + verify cả hai.

---

### N-075 — DEFER (A-20/AD-081): dời `OutboxMessage`→`OutboxRecord` internal + `InboxMessage` internal (2026-07-12)
- Trạng thái: DEFER có chủ đích (KHÔNG phải bỏ sót). Đã đóng phần RÒ RỈ PORT của A-20 (AD-081: `IEventBusPublisher` nay nhận `OutgoingIntegrationMessage` bất biến). Phần còn lại của review A-20 = tinh chỉnh layering thuần.
- Nội dung defer: review đề xuất dời `OutboxMessage` khỏi `Bedrock.Application.Messaging.Dispatch` xuống `Bedrock.Infrastructure` dưới tên `OutboxRecord` với visibility `internal` (persistence record không nên nằm ở Application), và làm `InboxMessage` internal; test truy cập qua `InternalsVisibleTo` hoặc query helper.
- Lý do defer (verifiable): (1) sau AD-081, `OutboxMessage` KHÔNG còn xuất hiện trên bất kỳ port nào lộ cho use case (CP11 đã chặn use case chạm namespace `Dispatch`) hay cho adapter transport → rủi ro rò rỉ thực tế đã hết; phần còn lại chỉ là "nhà ở đúng tầng". (2) `OutboxMessage` bị tham chiếu bởi ~6 file test ĐA-ASSEMBLY qua `db.Set<OutboxMessage>()` (`Bedrock.Infrastructure.Tests`, `Messaging.IntegrationTests`, `Modules/Identity.IntegrationTests`) → biến internal + dời assembly = đụng nhiều `InternalsVisibleTo` + đổi using hàng loạt → nên tách INCREMENT riêng để không trộn với thay đổi port (giữ mỗi tăng-tiến nhỏ, dễ verify, tránh drift).
- Khi làm tiếp: cập nhật AD-081 Consequences (đánh dấu phần defer đã đóng) + có thể thêm AD mới nếu đổi tên/đổi assembly là quyết định đáng ghi; thêm guard "OutboxMessage/OutboxRecord không thuộc assembly Application" (DependencyRuleTests) nếu thực thi.
- Traceability: review A-20 (bullet 2-3); AD-081; F19 (inbox persistence-only ẩn Infrastructure).

---

### N-076 — FE cho phần QR (starhill): PHẢI hỏi user chọn template; ưu tiên Vue.js (2026-07-12)
- Provenance: user chỉ thị trực tiếp phiên 2026-07-12: "khi qua sang qr thì nhớ phần FE cần hỏi tôi chọn temp cho FE nhé. Nên chọn vuejs".
- Ý nghĩa: khi bắt đầu Task C (đồng bộ base→starhill) HOẶC làm phần frontend của resort-qr-portal, TRƯỚC khi scaffold FE phải HỎI user xác nhận template/stack FE. Mặc định đề xuất **Vue.js** (user đã nghiêng về lựa chọn này) nhưng vẫn phải hỏi chốt (không tự quyết).
- Trạng thái: pending — chưa tới bước FE. Ghi để KHÔNG quên (base hiện chỉ backend; FE ngoài scope platform-base).
- Traceability: Task C (ledger §7 đồng bộ base→starhill); resort-qr-portal spec (FE slice).

---

### N-077 — Re-audit 2026-07-12 (`re-audit-architecture-implementation-2026-07-12.md`): đánh giá + xử lý (điểm 7.2/10)
- Provenance: user cung cấp bản re-audit chuyên sâu; yêu cầu đánh giá độ chính xác + xử lý cấp chuyên gia.
- **Đánh giá độ chính xác (đã verify từng phần bằng đọc code + test, KHÔNG tin mù):**
  - **P0-01 (Forwarded Headers trust-all) = ĐÚNG (đã xác nhận EMPIRICAL).** Test `ForwardedHeadersTrustTests` chứng minh trust-list rỗng → client spoof được IP (giả thuyết "doc .NET tự ignore" của tôi SAI). ĐÃ FIX (AD-091) + guard. Đây là bài học: verify runtime, không suy luận doc.
  - **P0-02 (transient handler → thẳng DLQ, thiếu retry tier) = ĐÚNG.** Consumer NACK requeue=false cho MỌI exception (kể cả DB/network tạm). Cần retry classification + delayed-retry topology. → OPEN (Gate 0).
  - **P1-01 (domain-event mất nếu SaveChanges fail SAU dispatch) = ĐÚNG một phần.** Restore chỉ bọc quanh DispatchAsync; base.SaveChanges/audit/soft-delete/depth fail sau đó → event đã clear, không restore. A-14 hạ PARTIAL. → Gate 0.
  - **P1-02 (schema-version chưa là invariant: default 1 khi thiếu/lỗi, long→int overflow) = ĐÚNG.** A-10 hạ PARTIAL. → Gate 0.
  - **P1-03 (idempotency scope: TenantId THAY user → collision xuyên user; anonymous chung namespace; thiếu fencing/hash) = ĐÚNG.** A-13 hạ PARTIAL. → Gate 0.
  - **P1-08 (`Error` invariant bị bypass qua public `init` + record `with`) = ĐÚNG.** A-26 hạ PARTIAL. → Wave 1.
  - **P1-07 (`SanitizeError` chỉ truncate, không redact secret/PII) = ĐÚNG.** Đổi tên/redact. → Wave 1.
  - P1-04..06, P1-09..16, P2-01..13: phần lớn HỢP LÝ (consumer lifecycle/health, DLQ per-module, lease renewal, command/query split, keyed-context bijection, auto-discover, registry uninitialized-object, verify-only JWT validate, trace vs correlation, messaging-off silent, operability, public surface, supply-chain pin, .gitattributes, coverage gate...). Chấp nhận là backlog thật.
  - **Ledger drift audit chỉ ra (P2-07) = ĐÚNG:** test count cũ (248) đã stale (thực tế ~328); bảng bỏ A-23/A-24; nhiều "DONE" nên là "PARTIAL" (invariant chưa đóng hết đường phá). Bài học: "DONE" nghĩa "invariant không còn đường phá + guard", không phải "đã thêm code/test một nhánh".
- **Phán quyết chấp nhận:** giữ kiến trúc lõi (không rewrite); ưu tiên **Gate 0** (P0-01✅, P0-02, P1-01, P1-02, P1-03) trước khi mở rộng module/QR. Nhiều "DONE" trước đây HẠ xuống PARTIAL trong ledger (trung thực > thành tích).
- **Đã xử lý ngay:** P0-01 (AD-091, fix + behavioral guard).
- Traceability: re-audit doc; Gate 0/Wave 1..4 (re-audit §8).
