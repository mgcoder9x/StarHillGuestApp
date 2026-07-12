# 03 — Trade-offs (các đánh đổi AI phải cân nhắc)

> Ghi các đánh đổi có hai mặt thật sự. Mỗi mục nêu: hai phía, phía đã chọn + vì sao, và **điều kiện kích hoạt xem xét lại** (khi nào nên đảo lựa chọn). Không mục nào là "miễn phí".

---

### TO-001 — `Result`: `readonly struct` vs `sealed class`
- Chosen: `sealed class` (AD-002).
- Phía struct (bỏ): zero-allocation, value semantics — TỐT cho hot-path in-memory nhiều triệu lần/giây.
- Phía class (chọn): đơn giản, không bẫy `default(struct)` = trạng thái rỗng hợp lệ; chi phí = 1 alloc gen0/kết quả.
- Vì sao chọn class: platform **I/O-bound** (web API) → alloc gen0 không đáng kể so với DB/HTTP latency; loại footgun quan trọng hơn micro-perf.
- Điều kiện xem xét lại: nếu profiling chỉ ra một hot-path in-memory thực sự (vòng lặp tính toán thuần, không I/O) tạo áp lực GC đo được → cân nhắc struct riêng cho path đó.
- Reversibility: Medium. Ref: AD-002, DV-006.

---

### TO-002 — Dead-letter: cột `dead_lettered_at` vs bảng DLQ riêng
- Chosen: cột (AD-003).
- Phía cột (chọn): một bảng, claim nguyên tử đơn giản, không move chéo bảng; đây là hạ tầng nội bộ → đổi sau rẻ.
- Phía bảng DLQ (bỏ hiện tại): outbox "hot" gọn hơn, có nơi chuyên dụng để ops soi/replay; nhưng thêm bộ phận + thao tác move (delete+insert) nguyên tử.
- Vì sao chọn cột: đơn giản + đúng trước; chưa có yêu cầu ops replay chuyên dụng (YAGNI).
- Điều kiện xem xét lại: khi ops cần công cụ replay/triage dead-letter chuyên biệt, hoặc bảng outbox phình gây ảnh hưởng query dù đã retention → migrate lên bảng DLQ (rủi ro thấp vì nội bộ).
- Reversibility: High. Ref: AD-003, task 7.5.

---

### TO-003 — Tên `Bedrock`: danh tính riêng vs va chạm `Bedrock.Framework`
- Chosen: `Bedrock` (AD-001).
- Phía chọn: một-từ, ngắn (MAX_PATH), ẩn dụ nền đúng, đóng gói NuGet đẹp về sau.
- Phía bất lợi: tồn tại thư viện niche `Bedrock.Framework` (David Fowler, networking) → va chạm TÊN nếu publish package công khai.
- Vì sao chấp nhận: chỉ dùng làm **namespace nội bộ**, không publish trùng package-id → rủi ro thực tế ~0.
- Điều kiện xem xét lại: nếu quyết định publish NuGet công khai → kiểm tra package-id trống và cân nhắc prefix công ty (vd `<Org>.Bedrock`).
- Reversibility: High khi greenfield. Ref: AD-001.

---

### TO-004 — Cho `Scrutor` + `Microsoft.Extensions.DependencyInjection/Logging.Abstractions` vào `Bedrock.Application`
- Chosen: cho phép (whitelist làm "plumbing", không tính là "công nghệ").
- Provenance/Evidence: `review.md` phần Assumptions + design §17 (dependency whitelist per project).
- Phía cho phép (chọn): ergonomics — `AddBedrockCore()` sống ở Application, Host gọi gọn; auto-scan bằng Scrutor tiện.
- Phía thuần Clean-Arch (bỏ): Application lý tưởng KHÔNG phụ thuộc thư viện thứ ba (kể cả DI plumbing); Scrutor là thư viện scan DI.
- Vì sao chấp nhận: coi DI/Logging.Abstractions + Scrutor là **hạ tầng composition trung lập**, không phải "công nghệ có thể thay" như DB/bus. Đánh đổi có ý thức, đã ghi.
- Điều kiện xem xét lại / phương án đảo: nếu muốn Application tinh khiết tuyệt đối → dời `AddBedrockCore` (đăng ký DI) xuống `Bedrock.Infrastructure`, đổi lại Host mất một chút ergonomics.
- ✅ **RESOLVED 2026-07-07:** user chốt **allow** (giữ Scrutor/DI-abstractions trong Application) → AD-018. Căn cứ: chúng là abstraction chuẩn .NET + composition plumbing, KHÔNG phải công nghệ swap được → không vi phạm I2.
- Reversibility: Medium. Ref: AD-018, review Assumptions, design §17.

---

### TO-005 — `Contracts` phụ thuộc `Bedrock.Application` (để có base `IntegrationEvent`)
- Chosen (final): **extract** (AD-017) — ban đầu nghiêng "cho phép (DV-001)", đã đổi khi user chốt "extract".
- Phía cho phép (chọn): biên dịch được ngày đầu, đơn giản.
- Phía "Contracts thuần DTO" (bỏ): Contracts lẽ ra không phụ thuộc gì để mọi module nhúng cực nhẹ; giờ kéo theo Application.
- Vì sao chấp nhận hiện tại: tránh thêm một assembly "kernel-events" ngay lúc greenfield khi chưa có nhiều module.
- Điều kiện xem xét lại: khi số module tăng và muốn Contracts nhẹ tuyệt đối → tách base `IntegrationEvent` (+ `IntegrationEvent` chỉ-metadata) ra một assembly trung tính rất nhỏ mà cả Application lẫn Contracts cùng ref.
- ✅ **RESOLVED 2026-07-07:** user chốt **extract** → `IntegrationEvent` tách sang `Bedrock.Messaging.Contracts` (zero-dep); `Contracts` KHÔNG còn ref Application. Xem AD-017 (supersedes DV-001).
- Reversibility: Medium. Ref: AD-017, DV-001.

---

### TO-006 — `uint RowVersion` / PostgreSQL-only vs đa-provider
- Chosen: `uint` + Postgres-only (DV-004, Non-goals §1.2).
- Phía Postgres-only (chọn): đơn giản, khớp `xid`, không trừu tượng hoá thừa.
- Phía đa-provider (bỏ): token trừu tượng (vd `byte[]`) để hỗ trợ SQL Server `rowversion`; phức tạp hơn cho lợi ích chưa cần.
- Vì sao chấp nhận: production target duy nhất là PostgreSQL (đã codify).
- Điều kiện xem xét lại: nếu yêu cầu đa-provider production xuất hiện → đổi token type (breaking) + map concurrency theo provider ở Infrastructure.
- Reversibility: Low-Medium (breaking cho `AuditableEntity`). Ref: DV-004, F8.

---

### TO-007 — At-least-once + idempotent consume (KHÔNG exactly-once)
- Chosen: at-least-once publish (Outbox) + idempotent consume (Inbox) = "hiệu ứng đúng-một-lần về nghiệp vụ".
- Provenance/Evidence: `design.md` §7 + R8.6 (at-least-once chấp nhận khi crash sau publish trước mark) + R9 (inbox idempotency).
- Phía chọn: khả thi, đúng thực tế phân tán; DB là source-of-truth, bus là kênh.
- Phía exactly-once (bỏ): bất khả thi rẻ trong hệ phân tán (two-generals); cố ép sẽ phức tạp/giòn.
- Vì sao chấp nhận: đây là chuẩn công nghiệp cho outbox/inbox; duplicate delivery được trung hoà bởi inbox idempotency.
- Điều kiện xem xét lại: không — đây là ràng buộc bản chất, không phải khẩu vị.
- Reversibility: N/A (bản chất). Ref: CP6/CP8/CP15, R8.6/R9.

---

### TO-008 — Outbox per-module: nguyên tử/tự chủ vs một dispatcher mỗi module
- Chosen: per-module (AD-004).
- Phía per-module (chọn): CP6 giữ by construction (cùng DbContext = cùng transaction); module tự chủ dữ liệu (F31).
- Phía shared (bỏ): một dispatcher trung tâm gọn hơn về vận hành; nhưng phá nguyên tử một-transaction.
- Vì sao chấp nhận chi phí: đúng đắn (atomicity) quan trọng hơn tiện vận hành; số dispatcher = số module vẫn quản được.
- Điều kiện xem xét lại: nếu số module rất lớn khiến nhiều dispatcher thành gánh nặng vận hành → cân nhắc gộp dispatcher đọc nhiều schema (vẫn giữ ghi outbox cùng transaction ở phía producer).
- Reversibility: Medium. Ref: AD-004, CP6, F31.

---

### TO-009 — Refresh-token store ở lõi (tái dùng) vs dồn vào Modules.Identity (thuần module)
- Chosen: cơ chế ở lõi, bảng ở schema module (AD-010).
- Phía lõi (chọn): tái dùng store rotation nguyên tử race-safe (khó viết đúng) cho mọi module cần refresh token.
- Phía dồn Identity (bỏ): lõi "sạch nghiệp vụ" tuyệt đối; nhưng mất tái dùng, mỗi module tự viết lại store an toàn race (rủi ro sai).
- Vì sao chấp nhận: refresh-token store là **cơ chế** (không phải policy nghiệp vụ) → lên lõi hợp lý; bảng vẫn thuộc schema module (giữ F31).
- Điều kiện xem xét lại: nếu chỉ Identity từng dùng và không module nào khác cần → có thể hạ store xuống Modules.Identity để lõi gọn hơn.
- Reversibility: Medium. Ref: AD-010, F19/F31.

---

### TO-010 — RabbitMQ health-check: connection NGẮN mỗi probe vs tái dùng connection
- Chosen: mở connection ngắn-hạn mỗi lần probe rồi dispose (AD-064).
- Provenance/Evidence: `RabbitMqHealthCheck.CheckHealthAsync` (`await using connection = CreateConnectionAsync`).
- Phía connection-ngắn (chọn): đơn giản + ĐỘC LẬP trạng thái publisher/consumer (probe đo đúng "broker có kết nối được không" bất kể publisher đã lazy-connect chưa); không ghép chặt internals.
- Phía tái-dùng-connection (bỏ): nhẹ hơn (không mở TCP+AMQP mỗi probe) nhưng phải chia sẻ connection của publisher/consumer → ghép chặt, và một connection "cached open" có thể che giấu lỗi mạng thoáng qua.
- Vì sao chấp nhận chi phí: readiness probe chạy thưa (vài giây/lần); chi phí mở connection ngắn không đáng kể so với giá trị "đo thật + độc lập". Đúng cho base/demo.
- Điều kiện xem xét lại: nếu tần suất probe cao + tải lớn khiến mở connection thành chi phí đáng kể → chuyển sang health-check tái dùng một connection chuyên dụng (long-lived) có phát hiện đứt.
- Reversibility: High (đổi impl health-check cục bộ). Ref: AD-064, design §9.6/R34.

---

### TO-011 — Outbox-lag: publish-lag INLINE (tuổi-lúc-publish) vs oldest-pending-age GAUGE (DB-poll)
- Chosen: histogram `bedrock.outbox.publish.lag` đo `now - occurred_at` LÚC publish thành công, phát inline trong dispatcher (AD-065).
- Provenance/Evidence: `OutboxMetrics.RecordPublished(lag)` gọi trong `EfOutboxDispatcher.TryPublishAsync`.
- Phía inline-publish-lag (chọn): rẻ + không DB-poll + không gauge sync-over-async + không scoped-DbContext-trong-callback; đo trễ end-to-end THỰC (message được gửi trễ bao lâu) — hữu ích trực tiếp cho alerting SLA.
- Phía oldest-pending-age gauge (bỏ giai đoạn này): phản ánh tồn đọng hiện thời kể cả message CHƯA publish (bắt được "dispatcher chết → pending phình"); nhưng cần ObservableGauge query `MIN(occurred_at) WHERE pending` mỗi lần collect → sync-over-async + tạo scope/DbContext trong callback process-lifetime (giòn) + per-context.
- Vì sao chấp nhận: publish-lag phủ phần lớn nhu cầu quan sát trễ; dead-letter count + error_count đã báo hiệu kẹt. "Dispatcher chết hoàn toàn" thì health-check/liveness + thiếu metric published (rate=0) cũng lộ.
- Điều kiện xem xét lại: nếu vận hành cần cảnh báo tồn đọng khi dispatcher NGƯNG hẳn (publish-lag không phát vì không publish) → thêm oldest-pending-age gauge (thiết kế cẩn thận: background poller cập nhật giá trị + gauge đọc cache, per-context).
- Reversibility: High (thêm gauge là cộng thêm). Ref: AD-065, R24.3/§7.2.
