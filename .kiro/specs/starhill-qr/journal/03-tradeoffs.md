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

### QR-TO-002 — Cross-module cascade: đồng bộ-trong-Host (A) vs event-driven outbox (B)
- Chosen: **(A) đồng bộ trong Host** (QR-AD-002).
- Provenance/Evidence: base có sẵn cả hai — synchronous (gọi query/command port trong cùng scope/transaction) và event-driven (outbox/inbox opt-in `Bedrock:Messaging:Enabled`, đọc `Program.cs`). Cascade Req 10.8 spans 3 module (GuestAccess/Concierge/Housekeeping).
- Phía (A) chọn: nhất quán TỨC THÌ (một transaction/một process); đơn giản; không cần RabbitMQ chạy. Hợp resort một-instance.
- Phía (B) bỏ: nới ghép module + chịu tải tốt hơn KHI tách; nhưng eventual consistency (cửa sổ khách cũ vẫn post được trước khi consume), cần RabbitMQ + xử lý retry/dead-letter.
- Chi phí chấp nhận: cascade ghép Host chặt hơn với 3 module (qua Contracts, không qua Infra) — vẫn giữ ranh giới. Khi tách tải/nhiều instance → chuyển sang (B) qua `GuestVisitEndedIntegrationEvent`.
- Điều kiện xem xét lại: multi-instance hoặc module tách process. Reversibility: Medium. Ref: QR-AD-002; design.md §3.

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
