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
