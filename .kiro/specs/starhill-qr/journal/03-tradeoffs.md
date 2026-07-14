# 03 — Trade-offs (pha QR) — các cân nhắc AI phải đánh đổi

> Journal RIÊNG cho pha `starhill-qr`. ID tiền tố `QR-TO-###`.

---

### QR-TO-001 — Vendored-copy base (`starhill/`) vs tham chiếu Bedrock (project-ref/NuGet)
- Chosen: vendored-copy (QR-AD-001) — sản phẩm có bản copy Bedrock.* riêng.
- Provenance/Evidence: `robocopy platform starhill` (đã thực hiện); user directive "copy ra để giữ sạch base".
- Phía vendored-copy (chọn): base gốc SẠCH tuyệt đối + tách repo dễ; sản phẩm TỰ CHỦ (sửa base-copy cho nhu cầu QR không đụng base gốc); không phụ thuộc hạ tầng package.
- Phía tham chiếu (bỏ): "một nguồn base" → fix/nâng base tự chảy sang mọi sản phẩm; nhưng ghép base↔sản phẩm chặt hơn, cần đóng gói NuGet hoặc cross-solution ref, và base dễ bị sửa "vì sản phẩm" (mất sạch).
- Chi phí chấp nhận: **cải tiến base về sau KHÔNG tự sang sản phẩm** (và ngược lại). Phải sync thủ công nếu muốn (copy lại phần Bedrock.* thay đổi). Với sản phẩm thương mại một-khách-hàng (resort), tự chủ > đồng bộ tự động.
- Điều kiện xem xét lại: nếu có NHIỀU sản phẩm cùng chia base → chuyển sang NuGet package hoá Bedrock (một nguồn, versioned) để tránh N bản copy phân kỳ.
- Reversibility: Medium. Ref: QR-AD-001.

---

### QR-TO-002 — Cross-module cascade: đồng bộ-trong-Host (A) vs event-driven outbox (B) — SUPERSEDED
- Chosen lịch sử: **(A) đồng bộ trong Host** (QR-AD-002). **Superseded 2026-07-14 bởi QR-AD-027/QR-TO-006**.
- Provenance/Evidence lúc chọn: base có cả synchronous ports và outbox/inbox; Req 10.8 span GuestAccess/Concierge/Housekeeping. Design cũ giả định cùng scope có thể tạo một transaction.
- Lý do supersede: mỗi module hiện có DbContext/key riêng; cùng DI scope không tạo shared transaction. Sequential synchronous calls có partial-failure mà không có durable recovery, nên mệnh đề “nhất quán tức thì trong một transaction” không kiểm chứng được.
- Lựa chọn hiện hành: outbox/inbox at-least-once, idempotent consumer; guest write chặn tức thời bằng GuestVisit status, cleanup chéo module eventual. Xem QR-TO-006.
- Reversibility: Medium. Ref: QR-AD-002/027; design.md §3.

### QR-TO-003 — Dashboard: ghép-ở-Host vs module Dashboard riêng
- Chosen: **ghép ở Host** `StarHill.Api` (QR-AD-002).
- Provenance/Evidence: Dashboard đọc chéo ≥4 module (unread/ticket/room/ack). Host là composition root (được ref mọi `<M>.Contracts` — đọc `Program.cs`).
- Phía ghép-Host chọn: không tạo module đọc-chéo (module không được ref module khác → nếu làm module Dashboard sẽ phải ref nhiều domain, phá cô lập). Host tổng hợp query-port từng module là hợp lệ.
- Phía module riêng bỏ: "gói gọn" dashboard nhưng buộc ref chéo domain (phá ModuleBoundary) hoặc trùng lặp read-model.
- Chi phí chấp nhận: Host phình một ít (endpoint dashboard + tổng hợp). Nếu dashboard phức tạp lên (report nặng) → cân nhắc read-model/CQRS riêng sau.
- Reversibility: Medium. Ref: QR-AD-002; design.md §6.

### QR-TO-004 — D1-a cross-tree ProjectReference vs vendored-copy (QR-TO-001) vs NuGet — ĐẢO chọn QR-TO-001
- Chosen: **D1-a cross-tree ProjectReference** (QR-AD-012) — starhill xóa bản-copy base, ref thẳng `platform/src`. ĐẢO NGƯỢC lựa chọn vendored-copy ở QR-TO-001.
- Provenance/Evidence: drift đo thật same=86/differ=50/only-platform=7/only-starhill=2 → vendored-copy đã phân kỳ 50 file (thiếu keyed persistence → P0-1 catastrophic). user duyệt D1-a phiên 2026-07-13.
- Phía D1-a (chọn): MỘT base vật lý → drift BẤT KHẢ THI (fix tận gốc). 0 công re-vendor. Base tiến hóa → sản phẩm nhận lúc compile (fail-fast). Đúng "một nguồn sự thật".
- Phía vendored-copy (QR-TO-001, bỏ): sản phẩm tự-chứa không cần platform/ cạnh nó + tự do sửa base-copy. NHƯNG chính "tự do sửa + không sync" = gốc drift đã xảy ra thực tế (50 file). Chi phí "phải sync thủ công" mà QR-TO-001 chấp nhận đã KHÔNG được trả → hỏng.
- Phía NuGet (D1-b, để dành): version-isolation thật (mỗi sản phẩm ghim version base) — chỉ cần khi sản phẩm TÁCH REPO/nhịp release riêng. Trong monorepo hiện tại là premature; vendored-copy KHÔNG cho isolation đó mà vẫn gánh chi phí sync.
- Chi phí chấp nhận: starhill build lệ thuộc platform/ hiện diện cạnh (cùng repo — chấp nhận); Docker build context = repo root (QR-AD-015). Không còn version-isolation giữa base↔sản phẩm (đánh đổi lấy zero-drift; khi cần isolation → D1-b).
- Điều kiện xem xét lại: sản phẩm tách sang repo riêng → chuyển D1-b (NuGet nội bộ versioned).
- Reversibility: Medium (git). Ref: QR-AD-012; SUPERSEDES lựa chọn QR-TO-001/QR-AD-001.


### QR-TO-005 — GuestSession toàn deployment + cookie 60 ngày vs scope theo resort/fingerprint
- Chosen: session thiết bị toàn deployment, cookie 60 ngày; không ResortId/UA/IP fingerprint.
- Provenance/Evidence: Req glossary/11.2 định nghĩa cookie thiết bị 30–90 ngày; legacy GuestSession không ResortId và GuestOptions mặc định 60. Product hiện single-resort; GuestVisit đã mang ResortId/RoomId.
- Phía chọn: một thiết bị có identity ổn định; model nhỏ; không thu thập fingerprint/IP không cần thiết; tương thích multi-resort vì isolation nghiệp vụ nằm ở visit.
- Phía resort-scoped/fingerprint bỏ: giảm correlation giữa resort nhưng tăng dữ liệu nhận dạng, false split khi UA/IP đổi và không có requirement hiện tại.
- Chi phí chấp nhận: session có thể liên kết các visit của cùng device trong deployment; phải có retention policy sau khi vận hành xác định nhu cầu.
- Reversibility: Medium (migration/model). Ref: QR-AD-025; `design-modules/03-guestaccess.md` §3/§4.

### QR-TO-006 — Cascade end-visit: outbox eventual vs synchronous multi-DbContext
- Chosen: outbox/inbox **at-least-once**, idempotent consumers (QR-AD-027); đảo phần cascade của QR-TO-002.
- Provenance/Evidence: mỗi module dùng DbContext/key riêng; một DI scope không phải transaction chung. Base outbox/inbox/RabbitMQ đã có persistence/retry; Req 10.8 span ba owner.
- Phía chọn: local commit + event atomic trong GuestAccess; broker lỗi không mất intent; duplicate an toàn qua inbox; module boundary sạch.
- Phía sync bỏ: cleanup tức thời nhưng một call fail giữa chuỗi tạo partial state không có durable recovery; claim “một transaction” là sai với setup hiện tại.
- Chi phí chấp nhận: conversation/ticket cleanup eventual; guest write vẫn bị chặn tức thời bởi authoritative visit status. Cần vận hành outbox/consumer/DLQ.
- Điều kiện: chỉ bật khi Concierge/Housekeeping consumer tồn tại; initial resolve không thêm outbox thừa.
- Reversibility: Medium. Ref: QR-AD-027; supersedes cascade choice QR-TO-002.
### QR-TO-007 — Resolve limiter: dùng global IP budget đã kiểm chứng, chưa thêm named policy tùy tiện
- Chosen: giữ Bedrock global fixed-window limiter theo `RemoteIpAddress` sau trusted ForwardedHeaders; không chồng named resolve limiter cho tới khi có SLO/traffic và mô hình shared-NAT thật.
- Provenance/Evidence: `BedrockHttpSecurityExtensions` đã áp global limiter cho mọi endpoint, mặc định 100 request/60s/IP; GuestAccess design cũ yêu cầu named policy nhưng không nêu ngưỡng. Resort Wi-Fi có thể đặt nhiều khách sau cùng một NAT, nên tự chọn ngưỡng thấp dễ từ chối người dùng hợp lệ.
- Phía chọn: baseline DoS guard đang chạy, một nguồn cấu hình; không tạo con số “an toàn” không có dữ liệu.
- Phía named bỏ tạm: có thể bảo vệ resolve chặt hơn endpoint khác, nhưng chỉ đúng khi budget dựa capacity/SLO và test 429 riêng; partition raw token bị cấm vì tăng cardinality và biến capability thành key telemetry.
- Bù trừ: canonical 43-char guard trước resolver/DB + request body 1 KiB giảm chi phí mỗi request; edge/WAF vẫn là lớp volumetric.
- Điều kiện xem xét lại: có load test, request-rate percentile, số client/shared IP và mục tiêu false-positive; khi đó thêm policy configurable + 429/Retry-After guard.
- Reversibility: High. Ref: QR-AD-025; `design-modules/03-guestaccess.md` §7.

### QR-TO-008 — Tách migration history per-schema ngay trước release vs giữ ledger public chung
- Chosen: ledger `__EFMigrationsHistory` riêng trong `identity`, `resort_config`, `rooms`, `guest_access`; chuyển ledger chung bằng script copy idempotent và giữ public ledger trong rollout đầu để rollback.
- Provenance/Evidence: Compose PostgreSQL thật ngày 2026-07-14 có bốn schema nhưng chỉ `public.__EFMigrationsHistory`, chứa 5 MigrationId của bốn DbContext. Design/bundle ownership yêu cầu migration chain module độc lập.
- Phía chọn: module/bundle không nhìn ledger của module khác; loại rủi ro va chạm MigrationId và coupling deploy ngầm; runtime/factory cùng cấu hình.
- Phía giữ chung bỏ: hiện vẫn boot vì EF lọc MigrationId của context, nhưng bảng chung là shared mutable deployment state trái schema ownership và làm rollback/release module khó lý giải.
- Chi phí: binary mới không được chạy trên DB cũ trước bước transition; cần verify fresh DB và upgrade DB. Không xóa public ledger tự động.
- Reversibility: Medium. Ref: QR-AD-028; `design-modules/03-guestaccess.md` §10.


### QR-TO-009 — Traceability gate: field `Guard-Tests` + quét source vs reflection cross-assembly vs attribute
- Chosen: field cấu trúc `- Guard-Tests:` trong mỗi QR-AD + INV-6 quét source `starhill/tests/**` xác nhận test class tồn tại (chỉ ràng buộc AD Status chứa "Implemented").
- Provenance/Evidence: `StarHillJournalConsistencyTests` hiện parse markdown thuần, robust, không nạp assembly. Drift C-GA.3a là design-text↔code, INV-2 keystone chỉ khớp chuỗi "AD xuất hiện" nên không bắt.
- Phía chọn: structured (không nhập nhằng như backtick trong prose), robust (đọc file như các INV khác — cùng triết lý, không reflection), tỉ lệ thuận (chỉ AD đã tuyên bố xong mới bị ràng buộc). Bắt được xóa/đổi tên guard và tuyên bố Implemented rỗng guard.
- Phía reflection cross-assembly (bỏ): mạnh về "test có chạy" nhưng cần ArchitectureTests ref mọi test project + nạp assembly + phân biệt token test/không-test → fragile, dễ false-fail, trái triết lý parse-file của cổng hiện có.
- Phía attribute `[Traces("QR-AD-###")]` (để dành): bidirectional chặt nhất (test→decision + decision→test) nhưng tốn annotate rộng toàn bộ test cũ + vẫn cần reflection. Nâng cấp tương lai nếu cần độ chặt cao hơn; hiện chưa cần.
- Chi phí chấp nhận: quét source có thể match `class` trong comment (false-positive làm gate lỏng hơn, KHÔNG false-fail) — chấp nhận vì mục tiêu chính là bắt xóa/đổi tên/tuyên bố rỗng; không nhằm phân tích ngữ nghĩa test.
- Điều kiện xem xét lại: cần bảo đảm "test THỰC SỰ assert đúng bất biến" (không chỉ tồn tại) → nâng lên attribute + reflection có kiểm nội dung.
- Reversibility: High. Ref: QR-AD-029; `journal/05-anti-drift.md` INV-6.