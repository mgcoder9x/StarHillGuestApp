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
