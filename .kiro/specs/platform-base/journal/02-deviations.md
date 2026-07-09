# 02 — Deviations (chỗ AI phải ĐỔI so với yêu cầu/blueprint ban đầu)

> Ghi các điểm thiết kế **khác** với `FOUNDATION-BLUEPRINT.md` / `foundation/*` gốc hoặc bản spec trước đó. Trong `design.md` các điểm này được gắn nhãn `[Tinh chỉnh so với Blueprint]`. Mỗi độ lệch nêu **root cause** vì sao buộc phải đổi, không chỉ mô tả.

---

### DV-001 — `*.Contracts` được phép tham chiếu `Bedrock.Application` (cho base `IntegrationEvent`)
- Status: **SUPERSEDED by AD-017 (2026-07-07)** — `IntegrationEvent` đã tách sang assembly trung tính `Bedrock.Messaging.Contracts`; `*.Contracts` KHÔNG còn ref `Bedrock.Application`. Bản ghi này giữ làm lịch sử (không xoá dấu vết).
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `review.md` D1; `design.md` §3.3 dependency matrix (footnote `[Tinh chỉnh so với Blueprint]`).
- Original (blueprint): `Modules.<M>.Contracts` "ref — (DTO thuần)", KHÔNG ref gì; và `M.Application` không được ref chính Contracts của nó.
- Changed to: `Contracts` **được** ref `Bedrock.Application` (CHỈ để kế thừa base `IntegrationEvent`); `M.Application` ref `M.Contracts` + `B.Contracts` của module khác.
- Root cause (vì sao buộc đổi): `RoomCreatedIntegrationEvent : IntegrationEvent` sống trong `Contracts` nhưng base `IntegrationEvent` ở `Bedrock.Application` → matrix gốc **không thể biên dịch ngày đầu tiên** (mâu thuẫn compile thật, không phải khẩu vị).
- Consequences / cảnh báo: "Contracts = DTO thuần" bị pha loãng nhẹ (Contracts giờ phụ thuộc Application). **Phương án sạch hơn cần cân nhắc về sau (xem TO-005):** đặt base `IntegrationEvent` ở một assembly trung tính (kernel/Contracts-base) để Contracts không phụ thuộc Application. Hiện chấp nhận vì đơn giản.
- Reversibility: Medium (tách base event ra assembly riêng là refactor có kiểm soát).
- Traceability: F30/I5, design §3.3, TO-005.

---

### DV-002 — `IUnitOfWork` BỎ `Repository<T>()`; inject `IRepository<T>` trực tiếp
- Status: Confirmed (in design)
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `review.md` D7; `design.md` §5.1 (nhãn `[Tinh chỉnh so với Blueprint]`); `tasks.md` task 3.1 "`IUnitOfWork` (KHÔNG `Repository<T>()`)".
- Original (blueprint §5.1): `IUnitOfWork` có `IRepository<T> Repository<T>() where T : Entity`.
- Changed to: Bỏ `Repository<T>()`; use case inject thẳng `IRepository<T>` (cùng scoped DbContext).
- Root cause: `Repository<T>()` là **service-locator** — constructor không lộ ra use case chạm những aggregate nào → khó đọc phụ thuộc, khó fake một repo trong test. Inject trực tiếp làm phụ thuộc tường minh + testable.
- Consequences: `IUnitOfWork` chỉ còn `SaveChangesAsync` + `ExecuteInTransactionAsync`. Nhiều repo cần nhiều tham số constructor (đánh đổi chấp nhận được, rõ ràng hơn).
- Reversibility: Medium.
- Traceability: F9, design §5.1, R7.

---

### DV-003 — Thứ tự pipeline behavior: Authorization TRƯỚC Validation
- Status: Confirmed (in design)
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `review.md` D12; `design.md` §8 (thứ tự `Logging → Authorization → Validation → Idempotency → Transaction`).
- Original (blueprint §14): `Logging/Tracing → Validation → Authorization → Idempotency → Transaction → UseCase` (Validation trước Authorization).
- Changed to: Authorization TRƯỚC Validation.
- Root cause: Validation trước Authorization **rò chi tiết validation cho caller chưa được phép** (kẻ chưa auth vẫn biết field nào sai) + tốn công validate cho request sẽ bị từ chối. Authorization trước = fail sớm, không rò.
- Consequences: Định nghĩa thêm ngữ nghĩa idempotency trùng key (`idempotency_conflict`, v1 chưa replay response).
- Reversibility: Medium (đổi thứ tự đăng ký decorator).
- Traceability: F13/F23, design §8, R26.3, task 15.

---

### DV-004 — Giữ `uint RowVersion` (hình dạng theo provider) dù F8 yêu cầu kernel trung lập
- Status: Confirmed (in design)
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `review.md` D14; `design.md` §4.1/§4.3 (nêu tradeoff tường minh) + Non-goals §1.2 (Postgres-only).
- Original intent (F8): kernel KHÔNG rò khái niệm provider; `IHasConcurrencyToken` đổi tên khỏi `IConcurrencyAware` để bỏ chữ `xmin`.
- Deviation: Interface đã trung lập tên, NHƯNG **kiểu token vẫn là `uint`** — trùng hình dạng `xid` 32-bit của PostgreSQL → vẫn là "rò provider ngầm" ở mức kiểu dữ liệu.
- Root cause / lý do chấp nhận: Non-goals §1.2 đã **codify PostgreSQL là target production duy nhất**; ép kiểu token trừu tượng hoàn toàn (vd `byte[]`) sẽ phức tạp hoá vô ích cho một provider duy nhất. Tradeoff được ghi TƯỜNG MINH thay vì giấu.
- Consequences: Nếu sau này thêm SQL Server (`rowversion` = `byte[8]`) → breaking change ở token type; đã tuyên bố ngoài phạm vi (Non-goals).
- Reversibility: Low-Medium (đổi kiểu token là breaking cho mọi entity `AuditableEntity`).
- Traceability: F8, design §4.1/§4.3/§1.2, TO-006.

---

### DV-005 — Đổi tên tiến hoá: `Foundation.*` → `BuildingBlocks.*` → `Bedrock.*`
- Status: Confirmed
- Date: 2026-07-07
- Decider: user (chốt cuối) + AI (các bước trung gian)
- Provenance/Evidence: blueprint gốc dùng `BuildingBlocks` (§2); base cũ dùng `Foundation.*`; phiên này đổi sang `Bedrock.*` (verified grep 0 `BuildingBlocks` còn trong 4 file spec).
- Original: base cũ `Foundation.*`; blueprint đề xuất `BuildingBlocks.*`.
- Changed to: `Bedrock.*` (xem AD-001 cho lý do đầy đủ).
- Root cause: xem AD-001 (MAX_PATH, part/whole, một-từ).
- Consequences: `review.md` cố ý GIỮ tên `BuildingBlocks` (bằng chứng lịch sử) → đừng mass-rename file đó (xem N-003).
- Reversibility: High (greenfield).
- Traceability: AD-001.

---

### DV-006 — `Result` đổi từ đề xuất `struct` (bản design trung gian) sang `class`
- Status: Confirmed
- Date: 2026-07-07
- Decider: user (chốt) + AI
- Provenance/Evidence: `design.md` §4.4 trước đây đề xuất `readonly struct` + invariant `default=failure`; phiên này đổi sang `sealed class` (verified).
- Original (bản design do AI review dựng): `readonly struct` với default-state invariant.
- Changed to: `sealed class` (xem AD-002).
- Root cause: xem AD-002 (I/O-bound → alloc không đáng kể; loại footgun `default(struct)`).
- Consequences: bỏ mô tả struct/default-invariant khỏi §4.4.
- Reversibility: Medium.
- Traceability: AD-002, design §4.4.

---

### DV-007 — Trạng thái spec chuyển từ "đã dựng/đã test" sang "greenfield"
- Status: Confirmed
- Date: 2026-07-07
- Decider: AI(Kiro) (đính chính sự thật)
- Provenance/Evidence: `list_directory` phiên này xác nhận `platform/` KHÔNG tồn tại; đã sửa `design.md` §10, `requirements.md` Introduction, `tasks.md` Overview/notes, `README.md` §4 về đúng greenfield.
- Original: cả 4 file từng khẳng định `platform/` đã có (Domain hoàn chỉnh + đã test, Application đang dở, lỗi CA1711 còn tồn).
- Changed to: greenfield — chưa có dòng code nào; task 1 dựng khung solution.
- Root cause: một checkpoint restore đã hoàn tác toàn bộ code dựng thử trước đó → các khẳng định "đã có/đã test" trở thành **sai sự thật**. Sửa tận gốc = viết lại phần trạng thái, không vá từng câu.
- Consequences: task 1/2 là "tạo mới" thay vì "hoàn tất/kiểm chứng"; "kiểm tra" (build/test) chỉ chạy được sau khi triển khai.
- Reversibility: N/A (đây là đính chính sự thật).
- Traceability: design §10, N-001.

---

### DV-008 — Contract test bỏ tham chiếu trực tiếp "FE ErrorCode"; đồng bộ FE ra ngoài phạm vi base
- Status: Confirmed (in requirements)
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `review.md` R5; `requirements.md` R32.4 hiện tại (verified read): "snapshot registry `Error.Code` (reflection) + snapshot schema integration-event; đồng bộ FE (khi FE tồn tại) qua artifact export từ registry này, ngoài phạm vi base".
- Original: R32.4 gốc = contract test "error code ↔ FE `ErrorCode` (reflection)" — giả định có một frontend để đối chiếu.
- Changed to: base chỉ đảm bảo (a) snapshot registry `Error.Code` (phát hiện code bị đổi/mất) + (b) snapshot schema integration-event (chống breaking vô ý). Việc đồng bộ với FE thực hiện qua **artifact export** từ registry, và **ngoài phạm vi base**.
- Root cause: base là thư viện nền greenfield **KHÔNG có frontend** trong phạm vi → tiêu chí gốc "unsatisfiable as written" (không có FE nào để reflection đối chiếu). Sửa tận gốc = định nghĩa lại hợp đồng test cho tự-đủ ở tầng base, đẩy phần FE-sync sang app tiêu thụ.
- Consequences: contract test của base độc lập, không phụ thuộc sự tồn tại của FE; app tiêu thụ tự lo đồng bộ qua artifact.
- Reversibility: N/A (đính chính phạm vi).
- Traceability: F20/F32, R32.4, review R5.

---

### DV-009 — `AddOutboxInbox()` nhận tham số `isNpgsql` (+ `schema`) thay vì no-arg như design ghi
- Status: Confirmed
- Date: 2026-07-08
- Decider: AI (implementation-time)
- Provenance/Evidence: design §4.6 ghi literal `modelBuilder.AddOutboxInbox()` (no-arg); impl thật `AddOutboxInbox(this ModelBuilder, bool isNpgsql, string? schema = null)` (verified file `OutboxInboxModelBuilderExtensions.cs`); test map + build/test xanh.
- Original (design §4.6): helper không tham số.
- Changed to: thêm `isNpgsql` (bắt buộc) + `schema` (tuỳ chọn).
- Root cause (vì sao buộc đổi): (1) **jsonb là provider-specific** — payload phải `HasColumnType("jsonb")` trên Postgres nhưng KHÔNG trên SQLite (SQLite không có jsonb, sẽ sai affinity). Một `ModelBuilder` extension KHÔNG có đường truy cập `DbContext.Database.IsNpgsql()` → phải nhận provider-flag từ caller (caller ở `OnModelCreating` có `Database.IsNpgsql()`). (2) `schema` cần để map bảng vào schema của module (design §4.6 "vào schema của module") — no-arg không truyền được schema. Giữ helper THUẦN (không phụ thuộc runtime provider detection ngầm) → testable + tường minh.
- Consequences: caller phải truyền `Database.IsNpgsql()` (một dòng, rõ ràng). Nếu sau này bọc thêm overload tiện lợi nhận `DatabaseFacade` thì thêm được, không phá API hiện tại.
- Reversibility: High (đổi chữ ký nội bộ base, chưa có consumer ngoài).
- Traceability: design §4.5/§4.6, AD (xmin conditional cùng nguyên lý provider-conditional), task 7.1.


---

### DV-010 — Claim outbox tách nhánh theo provider (SQL cho Npgsql, client-side cho provider khác)
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI (implementation-time)
- Provenance/Evidence: design §7.2 mô tả claim thuần SQL (`WHERE ... next_attempt_at <= now ORDER BY occurred_at`); impl thật `EfOutboxDispatcher<TContext>.ClaimBatchAsync` tách nhánh `context.Database.IsNpgsql()`; test SQLite trước khi tách nhánh FAIL với `InvalidOperationException: The LINQ expression ... could not be translated`.
- Original (design §7.2): một truy vấn LINQ→SQL duy nhất lọc `next_attempt_at`/`dead_lettered_at`/`processed_at` + `ORDER BY occurred_at`.
- Changed to: điều kiện `processed_at IS NULL AND dead_lettered_at IS NULL` LUÔN ở SQL (khớp partial index `ix_outbox_pending`). Với **Npgsql** (production): lọc `next_attempt_at` + `ORDER BY occurred_at` cũng ở SQL. Với **provider khác** (SQLite test): tải tập pending rồi lọc due-time + sort `occurred_at` **client-side**.
- Root cause (vì sao buộc đổi): EF Core **SQLite KHÔNG dịch được so sánh/sắp xếp `DateTimeOffset`** (lưu dạng TEXT có offset, không sortable theo instant) → `NextAttemptAt <= now` và `OrderBy(OccurredAt)` ném không-dịch-được. Base phải chạy cả trên SQLite (test provider-agnostic không-Docker, N-012) lẫn Postgres. Nhánh theo `IsNpgsql()` (đã có sẵn, không cần thêm package Sqlite vào Infrastructure) giữ production hiệu quả (dùng partial index) + test chạy được.
- Consequences: đường client-side chỉ dùng cho test/provider phi-Postgres; tập pending nhỏ (dispatcher poll thường xuyên) nên chi phí không đáng kể. Production (Npgsql) không đổi hành vi so với design. Claim exclusive `FOR UPDATE SKIP LOCKED` (đa-instance, CP15) vẫn thuộc task 7.4 — cùng nguyên lý provider-conditional (Npgsql-specific SQL).
- Reversibility: High (nội bộ base, chưa có consumer ngoài; có thể gộp lại nếu bỏ SQLite test provider).
- Traceability: design §7.2, N-012/N-028, task 7.3/7.4, cùng họ provider-conditional với DV-009 (jsonb) + AD (xmin conditional).


---

### DV-011 — Đổi tên method port: `GetActiveByHashAsync` → `GetByHashAsync` (so với design §5.7 literal)
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI(Kiro)
- Provenance/Evidence: `design.md` §5.7 + §7.4 GỐC ghi literal `GetActiveByHashAsync` (verified grep trước khi sửa); đã đổi cả hai + port `IRefreshTokenStore.cs`. Chi tiết lý do đầy đủ ở AD-031.
- Original (design §5.7/§7.4 literal): `Task<RefreshTokenSnapshot?> GetActiveByHashAsync(string tokenHash, ...)` với doc "active-only".
- Changed to: `GetByHashAsync` — trả record theo hash bất kể revoked/expiry.
- Root cause: tên/doc §5.7 ("active-only") MÂU THUẪN với hành vi reuse-detection §7.4 (cần record đã revoked). Đổi tên (không chỉ sửa doc) để interface không tự gây misuse (I8). Xem AD-031 cho phân tích 3-nguồn.
- Consequences: đồng bộ design §5.7/§7.4 + port; use case task 16 dùng tên mới.
- Reversibility: High (greenfield, chưa có consumer).
- Traceability: AD-031, design §5.7/§7.4, F19/F10, task 8.


---

### DV-012 — `StartupValidationOptions` là singleton registry (không `IOptions<>`) so với bản phác §9.4
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI(Kiro)
- Provenance/Evidence: design §9.4 bản phác `RequiredPortsValidator(... IOptions<StartupValidationOptions> options)`; impl thật inject `StartupValidationOptions` (singleton instance) trực tiếp; `StartupValidationOptions.cs` + `BedrockRegistrationExtensions.BedrockStartupValidation()` (find-or-add singleton). Test `RequiredPortsValidatorTests` xanh.
- Original (design §9.4): validator nhận `IOptions<StartupValidationOptions>`; các AddXxxCore đóng góp qua `Configure<StartupValidationOptions>`.
- Changed to: `StartupValidationOptions` đăng ký như MỘT singleton instance; AddXxxCore lấy nó qua `services.BedrockStartupValidation()` rồi `.RequirePort(...)`; validator inject thẳng instance.
- Root cause (vì sao đổi): dùng `IOptions<>` buộc kéo `Microsoft.Extensions.Options` vào **lõi Application**. Để giữ Application tối thiểu dependency (chỉ `DependencyInjection.Abstractions` — đúng lớp AD-018 đã whitelist), tôi dùng singleton-registry accumulate lúc compose (mọi AddXxxCore chạy trước Build nên accumulate an toàn). Ngữ nghĩa tương đương bản phác, chỉ khác cơ chế chứa.
- Consequences: không cần Options package ở Application; validator đơn giản hơn (không `.Value`). Nếu sau muốn hot-reload options thì cân nhắc IOptionsMonitor (chưa cần — danh sách port cố định lúc boot).
- Reversibility: Medium (đổi sang IOptions là refactor cục bộ Application + validator).
- Traceability: design §9.4, AD-018 (giữ dependency tối thiểu), R13.1, task 10.2.

### DV-013 — "AddIdentityModule một dòng" → tách composition thành nửa-Infra + nửa-Api ráp ở Host
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI(Kiro)
- Provenance/Evidence: design §4.2 ("Đăng ký module một dòng ở Host: AddRoomsModule(cfg) → nội bộ gọi AddDbContext + AddRoomsPersistence + validators + đăng ký IEndpointModule"); mâu thuẫn với matrix §3.3 + invariant I7/luật-5 ("chỉ Host ref Api+Infra"). Impl: `Identity.Infrastructure/DependencyInjection/IdentityInfrastructureExtensions.AddIdentityInfrastructure` (DbContext + persistence + use case + validator) + `Identity.Api/DependencyInjection/IdentityApiExtensions.AddIdentityApi` (đăng ký `IEndpointModule`). Build 0 warning; test xanh.
- Original (design §4.2): MỘT method `AddIdentityModule(cfg)` gọi cả AddDbContext (Infrastructure) LẪN đăng ký IEndpointModule (Api).
- Changed to: HAI method — `AddIdentityInfrastructure(cfg)` (ở Identity.Infrastructure) + `AddIdentityApi()` (ở Identity.Api); Host (task 16.2) gọi cả hai (vẫn ~1–2 dòng ở composition root).
- Root cause (vì sao đổi): `IEndpointModule` PHẢI ở Identity.Api (cần `Bedrock.Api`), DbContext PHẢI ở Identity.Infrastructure (cần `Bedrock.Infrastructure`); matrix §3.3 CẤM Api↔Infra tham chiếu nhau (kể cả cùng module) và task 16.3 sẽ viết arch-test "chỉ Host ref Api+Infra". Một method bắc cầu buộc một project vừa thấy Api vừa thấy Infra → PHÁ invariant I7 (bị arch-test bắt). Invariant kiến trúc (I7) mạnh hơn tiện ích "một dòng" → giữ invariant, tách đôi. Đây là fix TẬN GỐC (tôn trọng ranh giới) thay vì lách luật.
- Consequences: Host gọi 2 method cho mỗi module (Infra-half + Api-half) — vẫn là composition-root một chỗ, gỡ module = xóa 2 dòng + 1 thư mục. Nếu muốn đúng "một dòng", có thể định nghĩa một helper Ở HOST (nơi hợp lệ thấy cả hai) gói 2 lời gọi — nhưng KHÔNG đặt helper đó trong Api/Infra module (sẽ phá I7).
- Reversibility: High (đổi cách gói composition không phá hợp đồng runtime).
- Traceability: design §4.2, matrix §3.3, I7, task 16.1/16.2/16.3.

### DV-014 — Rotation trả CẢ access token + refresh token mới (§7.4 pseudocode chỉ trả access token)
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI(Kiro)
- Provenance/Evidence: design §7.4 dòng cuối `RETURN Result.Success(Issue(newRecord))` (chỉ access token); impl `RefreshTokenResult(AccessToken, RefreshToken, RefreshTokenExpiresAt)` + use case trả `newRawToken`. Test `RefreshAccessTokenUseCaseTests.Valid_token_rotates_and_returns_new_tokens` (assert cả hai token). Build 0 warning.
- Original (design §7.4): trả về chỉ `Issue(newRecord)` = access token JWT.
- Changed to: trả `RefreshTokenResult` gồm access token + refresh token RAW mới + hạn refresh token.
- Root cause (vì sao đổi): rotation bản chất tạo token MỚI (`CreateRotated` → "hash mới"). Hash mới ⇒ tồn tại raw token mới (không thể có hash mà không có raw sinh ra nó). Client BẮT BUỘC nhận raw mới để dùng cho lần refresh kế — nếu chỉ trả access token thì client mất refresh token, rotation vô nghĩa (lần sau không refresh được). Pseudocode §7.4 lược bớt chi tiết trả-về; bổ sung raw token là hoàn thiện đúng bản chất cơ chế, KHÔNG phải thêm tính năng tùy tiện.
- Consequences: endpoint refresh trả access + refresh token; client thay thế refresh token cũ. Không đổi logic bảo mật §7.4 (mọi nhánh fail giữ nguyên `invalid_refresh_token`).
- Reversibility: High (shape kết quả nội bộ module Identity, chưa có client thật).
- Traceability: design §7.4, F5/F10, task 16.1.
