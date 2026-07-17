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


### QR-TO-010 — Rule-gate: truyền guestVisitId vs gate tự resolve GuestVisit
- Chosen: `IRuleGate.EnsureAcknowledgedAsync(resortId, guestVisitId, feature)` nhận Id trần; consumer (Faq/Concierge/Housekeeping) tự resolve GuestVisit qua `ICurrentGuestContextResolver` (GuestAccess.Contracts) rồi truyền vào.
- Provenance/Evidence: `Rules.Contracts` mirror các Contracts khác (thuần, chỉ ref Bedrock.Messaging.Contracts). Nếu gate tự resolve visit thì Rules.Contracts phải ref GuestAccess.Contracts → coupling Contracts↔Contracts.
- Phía chọn: Rules.Contracts giữ thuần (không coupling module khác); mỗi guest endpoint đằng nào cũng phải resolve visit (để enforce portal-window) nên có sẵn guestVisitId — truyền vào rẻ và rõ.
- Phía gate tự resolve (bỏ): tiện cho caller (một call) nhưng kéo GuestAccess.Contracts vào Rules.Contracts và nhân đôi logic resolve/window ở gate.
- Chi phí: caller gọi 2 bước (resolve visit → gate). Chấp nhận vì đúng chuẩn boundary + tách trách nhiệm.
- Reversibility: High. Ref: QR-AD-030/032; `design-modules/04-rules.md` §6.

### QR-TO-011 — Sanitize-on-save authoritative vs sanitize cả hai chiều (save + read)
- Chosen: sanitize-on-save là bất biến chuẩn (cột `BodyHtmlSanitized`); Publish copy nội dung ĐÃ sanitize; guest read trả field đã sanitize (KHÔNG double-sanitize khi trả).
- Provenance/Evidence: Req 8.6 nói "trước khi lưu VÀ trước khi trả về". Data model đặt tên cột `BodyHtmlSanitized` (đã sanitize khi lưu). Guest chỉ đọc từ RulePublicationSectionTranslation (copy từ Draft đã sanitize).
- Phía chọn: một điểm kiểm soát (lúc ghi) dễ guard (test: lưu ra không còn script — CP12); read an toàn by-construction vì mọi thứ khách đọc đều đã sanitize khi ghi/publish. Tránh chi phí sanitize lặp mỗi read.
- Phía sanitize cả hai chiều (cân nhắc): defense-in-depth nếu dữ liệu bị sửa ngoài luồng use case (SQL trực tiếp). Nhưng trong kiến trúc này mọi ghi đi qua use case → nguồn tin cậy là lúc ghi.
- Chi phí/điều kiện: nếu sau này có đường ghi nội dung KHÔNG qua sanitize (import, migration nội dung cũ) → bật thêm sanitize-on-read hoặc re-sanitize batch. Ghi rõ để không quên.
- Reversibility: High. Ref: QR-AD-031; `design-modules/04-rules.md` §7; CP12/Req 8.6.


### QR-TO-012 — OpenAPI: JSON native (không UI) vs thêm Swagger/Scalar UI
- Chosen: chỉ phơi native OpenAPI JSON (`/openapi/v1.json`) ở Development; KHÔNG thêm UI (Swagger/Scalar) lúc này.
- Provenance/Evidence: base `BedrockOpenApiExtensions` dùng native `Microsoft.AspNetCore.OpenApi` + ghi rõ DV-015 "không nhồi stack lớn"; "Swagger UI là tầng trình bày do Host chọn". Không có package UI pin ở `starhill/Directory.Packages.props`.
- Phía chọn (JSON-only): 0 dependency mới, đúng DV-015; đủ để nhập vào Postman/Insomnia/VS Code REST/bất kỳ OpenAPI viewer → vẫn "test API qua web/công cụ". Bề mặt tối thiểu.
- Phía thêm UI (hoãn): trình duyệt bấm-gọi trực quan hơn cho QA/demo, nhưng kéo thêm package (Scalar.AspNetCore/Swashbuckle) — mâu thuẫn DV-015; và vẫn cần auth+data để gọi endpoint admin. Chỉ thêm khi có nhu cầu demo rõ + chấp nhận dependency, và cũng nên dev-only.
- Chi phí chấp nhận: dev không có trang bấm sẵn; dùng công cụ ngoài để nạp JSON. Chấp nhận để giữ base/product gọn.
- Điều kiện xem xét lại: cần demo click-through cho stakeholder → thêm Scalar UI (nhẹ hơn Swashbuckle) gated Development.
- Reversibility: High. Ref: QR-AD-033; DV-015.


### QR-TO-013 — StarHill default compose: tối giản (không RabbitMQ) vs always-on messaging demo
- Chosen: default tối giản (postgres+host, messaging off); RabbitMQ qua overlay explicit (QR-AD-034).
- Provenance/Evidence: messaging opt-in sẵn (`Bedrock:Messaging:Enabled`); chỉ Identity produce outbox; cascade QR-AD-027 defer; user yêu cầu sạch–đơn giản cho 60 phòng.
- Phía chọn (default tối giản): giảm phức tạp/tài nguyên vận hành thực; đúng nhu cầu 60 phòng; vẫn bật được khi cần. Đường e2e messaging KHÔNG mất: overlay `docker-compose.messaging.yml` + Testcontainers (`RabbitMq*Tests` platform) + platform compose vẫn chứng minh.
- Phía always-on (bỏ): default luôn chứng minh Outbox→RabbitMQ trên artifact thật; nhưng ép broker thường trực cho sản phẩm nhỏ = phức tạp thừa, trái "sạch đơn giản".
- Chi phí chấp nhận: muốn test event-driven phải nhớ thêm `-f docker-compose.messaging.yml`; off-mode có outbox event Identity tích lũy nhẹ (đến khi làm emission opt-in ở base).
- Điều kiện xem xét lại: khi cascade GuestVisitEnded (Concierge/Housekeeping) vào production → cân nhắc bật messaging mặc định HOẶC dùng in-process dispatch cho single-instance nhỏ (đánh giá broker-vs-in-process lúc đó, QR-AD-027).
- Reversibility: High. Ref: QR-AD-034; QR-AD-015; QR-AD-027.

### QR-TO-014 — Board hội thoại: order `LastMessageAt` DESC (nghiệp vụ recency, test Postgres) vs Id-v7 (provider-agnostic, test SQLite)
- Chosen: `OrderByDescending(LastMessageAt).ThenByDescending(Id)` cho board read-model (`EfConciergeReader.ListConversationsAsync`); test ordering trên Postgres Testcontainers (`ConciergeBoardTests`). (K-Con.2b, QR-AD-044, QR-N-064.)
- Provenance/Evidence: Req 5.3 board lễ tân "gom theo phòng + sắp MỚI NHẤT (theo hoạt động)". `Conversation.LastMessageAt` cập nhật mỗi tin (guest gửi / staff reply); `Conversation.Id` = UUIDv7 = thứ tự TẠO cố định. Bài học QR-N-059: SQLite KHÔNG ORDER BY DateTimeOffset (đã fail thật ở Housekeeping) → khi cần test SQLite thì dùng Id-v7. Verify: `Board_orders_by_last_message_at_desc` — convA tạo TRƯỚC (Id nhỏ hơn) nhưng LastMessageAt mới nhất → đứng ĐẦU board (đúng recency); nếu order Id-v7 thì convA sẽ chìm dưới → SAI.
- Phía chọn (LastMessageAt): ĐÚNG ngữ nghĩa nghiệp vụ "mới-hoạt-động-nhất trước" — một hội thoại cũ vừa có tin mới PHẢI nổi lên đầu cho lễ tân xử lý. Đây là bản chất của board (không phải danh sách theo thứ tự tạo). Tie-break `Id` DESC cho ổn định phân trang.
- Phía Id-v7 (bỏ cho board): provider-agnostic → test cục bộ SQLite được; NHƯNG Id-v7 phản ánh thời-điểm-TẠO, KHÔNG phản ánh hoạt-động gần nhất → hiển thị SAI thứ tự khi hội thoại cũ có tin mới. Bài học QR-N-059 (Id-v7 ≈ CreatedAt) CHỈ đúng khi "latest" = chính lúc tạo entity (ticket/event); board hội thoại KHÁC vì LastMessageAt biến thiên độc lập Id. Áp Id-v7 ở đây là fix NGỌN (để test dễ) đánh đổi bằng SAI nghiệp vụ → loại.
- Chi phí chấp nhận: board CHỈ test được trên Postgres (SQLite không kiểm được ordering DateTimeOffset) → 5 test `ConciergeBoardTests` là SkippableFact chạy Testcontainers (skip khi thiếu Docker; đã chạy THẬT 5/5 phiên này). Nhất quán chiến lược: "logic provider-agnostic → SQLite; ràng buộc/ordering provider-specific → Postgres". Detail read-model (`GetConversationAsync`) vẫn order tin theo Id-v7 (CŨ→MỚI trong 1 hội thoại = đúng thứ tự tạo tin → provider-agnostic, không mâu thuẫn).
- Điều kiện xem xét lại: nếu sau này cần board test không-Docker HOẶC đổi nguồn sinh Id → cân nhắc thêm cột sequence đơn điệu riêng cho "last activity". Hiện `LastMessageAt` là trường tự nhiên + Postgres là DB production → không over-engineer.
- Reversibility: High (đổi ORDER BY trong reader). Ref: QR-AD-044; QR-N-059; QR-N-064.

### QR-TO-015 — Staff reply vào hội thoại đã Closed: TỰ mở lại (Open) vs yêu cầu reopen tường minh trước khi reply
- Chosen: `ReplyConversationUseCase` khi hội thoại đang Closed → tự đặt `Status=Open` + xoá `ClosedAt`/`ClosedByUserId` rồi append tin (K-Con.2b, QR-AD-044).
- Provenance/Evidence: Req 5.10 (guest gửi lại sau close → reopen) đã có precedent guest-side ở `SendGuestMessageUseCase.AppendGuestMessage` (reopen). Design `07-concierge.md` §9 flag câu hỏi "reply có tự-reopen không" → chốt tự-reopen. Verify: `Reply_reopens_closed_conversation` (Closed → reply → Open, ClosedAt/actor về null).
- Phía chọn (tự reopen): đối xứng với guest reopen (Req 5.10) — cả hai phía "gửi tin vào hội thoại Closed mà cần tiếp tục" đều mở lại đúng hội thoại đó (1 hội thoại/visit, không tạo mới). Staff chủ động tiếp tục hỗ trợ = hành vi tự nhiên; giảm bước thừa (không bắt gọi close→reopen→reply).
- Phía reopen-tường-minh (bỏ): giữ "Closed = trạng thái cuối bất biến trừ khi mở lại có chủ đích" → rõ ràng audit hơn; NHƯNG thêm 1 round-trip API cho thao tác thường gặp (lễ tân trả lời tiếp), và mâu thuẫn với đối xứng guest-side. Loại vì UX vận hành + tính nhất quán.
- Chi phí chấp nhận: "Closed" không còn là bất biến tuyệt đối phía staff — reply làm sống lại. Chấp nhận vì hội thoại gắn theo visit còn hiệu lực; khi visit kết thúc, `CloseConversationForVisitUseCase` (System, C-GA.5) đóng và guest hết session nên không thể reopen tiếp.
- Điều kiện xem xét lại: nếu nghiệp vụ cần "archived vĩnh viễn" (không cho reply) → thêm trạng thái thứ ba (Archived) tách khỏi Closed. Hiện chỉ 2 trạng thái (Open/Closed) theo design.
- Reversibility: High. Ref: QR-AD-044; QR-AD-043 (guest reopen); QR-N-064.
