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

### N-010 — Hai điểm triết lý cần user xác nhận CÓ Ý THỨC (chưa phải lỗi)
1. **Scrutor/DI trong `Bedrock.Application`** (TO-004) — ergonomics vs thuần Clean-Arch.
2. **`Contracts` → `Bedrock.Application`** (TO-005 / DV-001) — biên dịch được vs Contracts thuần DTO.
- Cả hai reversible (Medium). Nếu user muốn tinh khiết tuyệt đối, có phương án đảo đã ghi. Chưa chặn triển khai.

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
