# 02 — Deviations (chỗ AI phải ĐỔI so với yêu cầu/blueprint ban đầu)

> Ghi các điểm thiết kế **khác** với `FOUNDATION-BLUEPRINT.md` / `foundation/*` gốc hoặc bản spec trước đó. Trong `design.md` các điểm này được gắn nhãn `[Tinh chỉnh so với Blueprint]`. Mỗi độ lệch nêu **root cause** vì sao buộc phải đổi, không chỉ mô tả.

---

### DV-001 — `*.Contracts` được phép tham chiếu `Bedrock.Application` (cho base `IntegrationEvent`)
- Status: Confirmed (in design)
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
