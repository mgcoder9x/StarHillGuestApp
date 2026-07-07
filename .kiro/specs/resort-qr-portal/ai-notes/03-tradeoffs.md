# 03 — Trade-off đã cân nhắc

> Mỗi mục nêu các lựa chọn, cái đã chọn, và cái đánh đổi. Ghi lại để sau kiểm chứng "vì sao chọn thế".

### TRD-001: Modular monolith vs Microservices
- Status: Accepted
- Date: 2026-07-03
- Options: (A) modular monolith; (B) microservices.
- Chosen: (A).
- Đánh đổi: Bỏ khả năng scale/deploy độc lập từng module để đổi lấy đơn giản vận hành, một DB, một deploy — phù hợp resort chạy WiFi nội bộ, tải nhỏ. Giữ module boundary để sau tách được nếu cần.
- Refs: `../design/backend/01` §3.

### TRD-002: Schema đầy đủ ngay vs Schema tăng dần theo module
- Status: ✅ RESOLVED → chọn (B) incremental (2026-07-03)
- Options: (A) migration đầu tạo hết bảng theo docs; (B) chỉ tạo bảng nền, module sau tự thêm migration per-wave.
- Chosen: **(B)** — theo khuyến nghị chuyên gia cho sản phẩm thương mại.
- Lý do (fix gốc): migration là lịch sử phiên bản schema; EF migrations vốn additive nên thêm bảng theo wave là an toàn. Tạo bảng "chết" cho module chưa code vi phạm YAGNI, cam kết sớm, migration khổng lồ khó review/rollback. Nỗi lo "vỡ dữ liệu giữa chừng" là fix ngọn; fix gốc = quy trình migration per-wave có review.
- Đánh đổi chấp nhận: cần kỷ luật thêm FK cross-module ở migration sau (EF hỗ trợ tốt).
- Đã cập nhật: requirements Req 8.6–8.7, Req 18.1–18.2; `../design/backend/04` §4–5.
- Refs: DEC-010, DEV-006.

### TRD-003: Nguồn sinh khóa uuid — client-side v7 vs DB uuidv7 vs v4 vs bigint
- Status: ✅ Resolved → client-side UUIDv7, fallback DB `uuidv7()` (2026-07-03)
- Options: (A) client-side `Guid.CreateVersion7()`; (B) DB `DEFAULT uuidv7()` PG18; (C) v4 ngẫu nhiên; (D) bigint identity.
- Chosen: **(A)**, với **(B) là fallback zero-schema** nếu benchmark locality xấu.
- Lý do chọn (A) hơn (B): Id có sẵn **trước khi lưu** (giá trị kiến trúc: FK/graph, idempotency, 201 Created) — (B) chỉ có Id sau insert. Locality của (A) kỳ vọng tốt nhờ Npgsql canonical serialize + v7 time-ordered.
- Lý do (A)/(B) hơn (C): (C) v4 phá B-tree locality (fragmentation) — đã biết rõ.
- Lý do uuid hơn (D) bigint: không lộ số lượng/khó đoán (dù bigint nhỏ hơn 8 vs 16 byte).
- Điều kiện chuyển sang (B): benchmark foundation (~100k insert) cho thấy (A) mất locality. Đổi (A)→(B) không đổi kiểu cột `uuid`.
- Refs: DEC-003, TK-002.

### TRD-004: Result<T> vs Exception cho lỗi nghiệp vụ
- Status: Accepted
- Date: 2026-07-03
- Options: (A) Result<T>; (B) ném exception + middleware bắt.
- Chosen: (A) cho lỗi nghiệp vụ dự kiến; exception chỉ cho lỗi bất thường.
- Đánh đổi: Result rõ ràng, không tốn chi phí throw, ép caller xử lý; đổi lại code hơi dài hơn try/catch. Phù hợp yêu cầu "hợp đồng lỗi ổn định".
- Refs: DEC-004, `../design/backend/02` §2.

### TRD-005: Scrutor (marker interface) vs giữ AutoDependency attribute
- Status: Accepted
- Date: 2026-07-03
- Options: (A) Scrutor + marker interface; (B) giữ attribute AutoDependency đổi engine; (C) đăng ký DI thủ công.
- Chosen: (A).
- Đánh đổi: (A) an toàn, phổ biến, ít magic; bỏ "hương vị" attribute quen thuộc của team reference. (C) rõ ràng nhất nhưng nhiều boilerplate. Có ghi chú giữ (B) nếu team muốn.
- Refs: DEC-002, `../design/backend/01` §5.

### TRD-006: Sinh type FE từ OpenAPI vs viết tay
- Status: Proposed
- Date: 2026-07-03
- Options: (A) sinh `shared-types`/`api-client` từ OpenAPI backend; (B) viết tay type FE.
- Chosen (đề xuất): (A).
- Đánh đổi: (A) đồng bộ tự động BE↔FE, ít lệch hợp đồng; đổi lại cần pipeline sinh code + backend expose OpenAPI ổn định. Chưa chốt vì FE chưa được user gửi.
- Refs: `../design/frontend/01-frontend-base.md` §1.

### TRD-007: Admin UI kit — ĐÃ CHỐT Element Plus (loại PrimeVue vì repo archive)
- Status: ✅ Resolved (2026-07-03) → Element Plus
- Options: (A) PrimeVue; (B) Element Plus; (C) Naive UI.
- Chosen: **(B) Element Plus**.
- Lý do quyết định (fix gốc bằng kiểm chứng): khi verify web 2026-07-03, repo `primefaces/primevue` hiển thị archived/read-only (28/6/2026) + issue "Is this project still on track?". Chọn lib vừa archive cho sản phẩm thương mại là rủi ro bảo trì. Element Plus đang bảo trì tích cực, TS tốt, phổ biến (phù hợp admin tiếng Việt).
- Đánh đổi: rời khỏi hệ theming PrimeVue; Element Plus cũng đủ mạnh cho DataTable/form. Naive UI là phương án dự phòng.
- ⚠️ Re-verify trạng thái PrimeVue khi implement (archive lib lớn khá bất thường) — TK-017.
- Refs: DEC-013, `../design/technology-stack.md`, nguồn: github.com/primefaces/primevue.

### TRD-008: Phân quyền FE — role guard đơn giản vs CASL
- Status: Accepted (base: role guard; CASL để mở)
- Date: 2026-07-03
- Options: (A) role guard đơn giản theo RequireAdmin/RequireStaff; (B) CASL ability (như reference).
- Chosen: (A) cho base; giữ đường mở lên (B) nếu sau này cần phân quyền chi tiết theo hành động.
- Đánh đổi: (A) đơn giản, đủ cho 2 role hiện tại; (B) mạnh nhưng phức tạp sớm (YAGNI).
- Refs: `../design/frontend/02-architecture.md` §8; reference FE-E12.

### TRD-009: Test persistence bằng SQLite in-memory (ngay) vs chờ Testcontainers/Postgres (Docker)
- Status: Accepted (2026-07-04) — dùng CẢ HAI, phân vai rõ.
- Options: (A) chờ có Docker rồi mới test persistence (đúng provider production 100%); (B) test provider-agnostic bằng SQLite in-memory ngay (Docker-free); (C) EF InMemory provider (bị DEC-021 cấm — không enforce ràng buộc).
- Chosen: **(B) ngay + (A) bổ sung sau** — KHÔNG (C).
- Lý do (bản chất): user chưa có Docker/DB (TK-034) nhưng cần base "cực tốt" ngay. SQLite là **DB quan hệ THẬT** (enforce transaction, `UPDATE...WHERE`, unique) → kiểm được đúng đắn hành vi PROVIDER-AGNOSTIC (audit, xóa mềm, UoW rollback, atomic consume-if-not-revoked, snake_case) mà không cần Docker. Khác hẳn EF InMemory (chỉ là dictionary, false-green).
- Đánh đổi / ranh giới KHÔNG được vượt: SQLite 1-connection in-memory KHÔNG chứng minh được (1) race đa-connection thật (row-lock Postgres), (2) `xmin` runtime, (3) partial unique index Postgres, (4) `timestamptz`. Những điểm này BẮT BUỘC Testcontainers/Postgres (TK-036) — mỗi test ghi nhãn nhóm để không kết luận nhầm từ SQLite. Model Npgsql vẫn verify được OFFLINE (build model không mở kết nối) → bắt lỗi mapping sớm (đã bắt được DEV-020).
- Refs: DEC-021/047/048, DEV-020, TK-036/037.


### TRD-010: Draft — optimistic concurrency (RuleSet.RowVersion round-trip) HOÃN cho MVP (last-write-wins)
- Bối cảnh: `RuleSet`/`RuleSection`/`RuleSectionTranslation` là `AuditableEntity` → CÓ cột `RowVersion` (↔ `xmin`, DEC-005/047) sẵn sàng cho optimistic concurrency. Sub-slice B (PUT draft) có thể kiểm RowVersion round-trip để chặn 2 admin ghi đè âm thầm (Req optimistic concurrency `04` §8).
- **Quyết định: HOÃN kiểm concurrency ở tầng use case draft cho MVP** — chấp nhận last-write-wins.
- Lý do (bản chất, cân nhắc thật):
  1. **Không kiểm chứng được trên SQLite** (`xmin` là cột hệ thống Npgsql-only — TRD-009/TK-036). Bật concurrency check nhưng không có test xanh chứng minh = "đúng trên giấy" (vi phạm nguyên tắc verify-nhiều-lần của user). Muốn test thật phải chờ Testcontainers/Postgres.
  2. **Xác suất + tác hại thấp:** soạn nội quy là thao tác admin hiếm, gần như không có 2 admin sửa CÙNG draft đồng thời (khác hẳn hot-path guest). Last-write-wins ở đây mất mát tối đa = 1 lần chỉnh của admin kia, phát hiện ngay khi xem lại.
  3. **Không mất đường lùi:** cột `RowVersion` VẪN giữ trong schema → bật lại chỉ là thêm round-trip ở use case + test Testcontainers, KHÔNG cần migration.
- Ranh giới KHÔNG vượt: quyết định này CHỈ cho draft authoring. `PublishRulesUseCase` (sub-slice C) vẫn PHẢI nguyên tử + chặn race qua `ux_pub_current` (unique partial index ở DB — không phụ thuộc xmin) để không có 2 current publication.
- Refs: DEC-061, DEC-005/047, TRD-009, TK-036, TK-041.


### TRD-011: Inbox hội thoại order theo `Id DESC` (creation-proxy) vs `LastMessageAt DESC` (recency thật)
- Status: Accepted (2026-07-06) — chọn `Id DESC` cho MVP, có đường nâng cấp (TK-056).
- Bối cảnh: inbox admin (`EfConversationQueries.ListAsync`) cần một thứ tự. "Đúng nghiệp vụ" = hoạt động mới nhất trước = `LastMessageAt DESC` (design §2 còn gợi index `(resort_id, room_id, last_message_at desc)`).
- Options: (A) `LastMessageAt DESC` — recency đúng, có index Postgres; (B) `Id DESC` (UUIDv7 ≈ thời điểm TẠO hội thoại) — provider-agnostic; (C) order theo max(message.Id) qua subquery — recency-proxy provider-agnostic.
- Chosen: **(B)** cho MVP.
- Lý do (bản chất, kiểm chứng được):
  1. **(A) ném lỗi trên test provider:** TK-047 (đã kiểm chứng, từng làm hỏng `EfHousekeepingQueries`) — SQLite `ORDER BY DateTimeOffset` → `NotSupportedException`. Chọn (A) = MỌI lời gọi list ném lỗi trên SQLite → mất sạch test filter/paging/projection/RoomNumber (những thứ provider-agnostic, giá trị nhất, chạy Docker-free). Verify (A) recency thật CHỈ được trên Postgres/Testcontainers (TK-036) — chưa có.
  2. **KHÔNG dùng provider-branch** (order khác nhau SQLite vs Postgres): test sẽ verify hành vi KHÁC production → false green (vi phạm nguyên tắc verify-thật của user).
  3. **(C) không chắc + rủi ro:** đơn điệu lexicographic của chuỗi Guid UUIDv7 trên SQLite CHƯA tự kiểm chứng (DEC-003 ghi nhận báo cáo cộng đồng trái chiều); `Max(Guid)` trong subquery OrderBy trên SQLite chưa verify → có thể lại ném. Không dựa vào hành vi chưa kiểm chứng.
- Đánh đổi (chấp nhận): hội thoại CŨ (Id thấp) được reopen + có tin mới sẽ KHÔNG nhảy lên đầu (order theo thời điểm tạo, không theo hoạt động cuối). Chấp nhận cho MVP vì FE gom theo phòng + badge `UnreadForStaff` là tín hiệu ưu tiên chính (staff nhìn badge unread, không dựa thứ tự toàn cục). Chi tiết hội thoại (`GetDetailAsync`) order message theo `Id` là ĐÚNG TUYỆT ĐỐI (message append-only → Id = thời gian thật), không đánh đổi.
- Ranh giới nâng cấp: nếu cần recency thật → đổi sang `LastMessageAt DESC` + tạo index + migration, verify bằng Testcontainers/Postgres (TK-056/036). Đổi rẻ (chỉ EfConversationQueries + 1 migration index), không phá dữ liệu.
- Refs: DEC-087, TK-047, TK-036, TK-056, DEC-003, `messaging-wave-design.md` §11.1-(3).


### TRD-012: Test SignalR dùng LongPolling (không WebSocket thật) qua TestServer
- Status: Accepted (2026-07-06).
- Bối cảnh: cần test hub `/hubs/chat` Docker-free qua `WebApplicationFactory`/`TestServer`. TestServer KHÔNG phục vụ WebSocket URL thật như server production.
- Options: (A) WebSocket thật (Kestrel + cổng thật) — đúng transport production nhưng cần host mạng thật, không Docker-free/CI-friendly, chậm; (B) LongPolling qua `TestServer.CreateHandler()` (`HttpMessageHandlerFactory`) — in-process, nhanh, tin cậy; (C) không test hub (chỉ tin thủ công).
- Chosen: **(B) LongPolling qua TestServer handler** cho test tự động; **KHÔNG (C)**.
- Lý do (bản chất): LOGIC cần kiểm (authorize-on-join B9, evict, staff group, adapter→group delivery, notify post-commit) **độc lập transport** — chạy y hệt trên LongPolling hay WebSocket (cùng hub method, cùng group manager). Transport chỉ khác cách khung tin đi trên dây. Vậy (B) kiểm đúng phần rủi ro (bảo mật/logic) mà không cần hạ tầng WS thật.
- Ranh giới KHÔNG được kết luận nhầm từ (B): (1) **path JWT-qua-query `access_token`** là ĐẶC THÙ WebSocket (LongPolling gửi token qua header) → (B) KHÔNG chạy path query đó ⇒ tách `HubAccessToken.ReadFromHubRequest` + **unit-test riêng** (đã làm). (2) Hành vi reconnect/backoff, giới hạn khung WS, backplane đa-instance — vẫn cần kiểm ở môi trường thật khi triển khai.
- Refs: DEC-089, TK-058/059; `Realtime/*Tests.cs`; `21-realtime-signalr.md` §2/§7.
