# 24 — Tenancy Model (single-resort vs multi-tenant) — phân tích & quyết định

> **File authoritative cho:** mô hình tenancy của sản phẩm — trả lời câu hỏi chiến lược "single-resort hay multi-tenant" cho một sản phẩm thương mại bán cho nhiều resort. Đây là quyết định **đắt nhất nếu sai** (ảnh hưởng data model + auth + bảo mật), nên phân tích kỹ, có lý do chính xác.

## 1. Câu hỏi & vì sao nó quan trọng

"Sản phẩm thương mại, lâu dài" ⇒ có thể bán cho **nhiều resort**. Vậy một cài đặt phục vụ một resort hay nhiều resort? Sai hướng ⇒ rework data model/auth sau khi đã có dữ liệu (rất đắt).

## 2. Sự thật quyết định (từ bối cảnh triển khai — docs)

Docs nêu **ràng buộc hạ tầng cứng** (Decision D1): hệ chạy **trong WiFi nội bộ của từng resort, KHÔNG public internet**; HTTPS + **DNS nội bộ trỏ về server resort**; camera/QR cần secure context tại chỗ.

⇒ **Hệ quả logic bắt buộc:** mỗi resort vốn đã là **một môi trường mạng tách biệt vật lý**, có server + DNS riêng. Một cài đặt **không thể** phục vụ nhiều resort qua internet (vì không public internet). Nói cách khác, **bối cảnh triển khai đã tự trả lời câu hỏi tenancy**.

## 3. Ba mô hình & đối chiếu

| Mô hình | Mô tả | Phù hợp bối cảnh? |
|---|---|---|
| **(A) Instance-per-resort** (DB-per-tenant **bằng triển khai**) | Mỗi resort = một cài đặt (app + DB) chạy trong mạng nội bộ resort đó | ✅ **Khớp hoàn hảo** với "WiFi nội bộ, DNS nội bộ, không public". Isolation bằng hạ tầng (mạnh nhất) |
| **(B) Shared-DB row-level multi-tenant** (một deployment tập trung, phân biệt bằng `ResortId`, phục vụ nhiều resort qua internet) | SaaS tập trung | ❌ **Mâu thuẫn trực tiếp** D1: buộc phải public internet + host tập trung → **phá vỡ mô hình bảo mật "mạng nội bộ"** của chính sản phẩm; thêm rủi ro rò rỉ chéo tenant |
| **(C) DB-per-tenant trong một deployment tập trung** | SaaS nhưng DB tách theo tenant | ❌ vẫn cần host tập trung/public → cùng mâu thuẫn D1 |

## 4. Quyết định (khuyến nghị chốt): **(A) Instance-per-resort**

**Lý do chính xác (không theo trend, theo bản chất bối cảnh):**
1. **Khớp mô hình bảo mật của sản phẩm.** Cách ly tenant mạnh nhất là cách ly **vật lý/mạng** — đã có sẵn (mỗi resort mạng riêng). Không cần (và không nên) tự xây tầng cách ly tenant trong app khi hạ tầng đã cách ly.
2. **Không mâu thuẫn D1.** Chọn (B)/(C) đồng nghĩa vứt bỏ tiền đề "không public internet" — thay đổi bản chất sản phẩm, không phải một tinh chỉnh.
3. **Đơn giản = an toàn.** Không có `ResortId` filter xuyên mọi query (nguồn lỗi rò rỉ chéo tenant kinh điển của shared-DB), không có tenant-resolution middleware, không rủi ro "quên WHERE ResortId".
4. **Vận hành thương mại vẫn ổn:** bán thêm resort = **nhân bản triển khai** (một quy trình cài đặt chuẩn hoá) + seed một resort. Mỗi resort có secret/DB/cert/`GuestWebBaseUrl` riêng (đã có ở `15` config).

## 5. "Đường mở" rẻ tiền đã có sẵn (không phải xây thêm)

Thiết kế hiện tại **đã** thân thiện cho khả năng mở rộng về sau mà không tốn công:
- **`ResortId` đã có** trên các entity tenant-scoped (Resort, ResortSettings, ResortLanguage, Room, RoomQrToken?, GuestVisit, và các entity nghiệp vụ). ⇒ schema **đã là multi-resort-capable** về mặt hình dạng, dù mỗi deployment chỉ có 1 resort.
- Seed tạo **một** resort/deployment (`15`/`04` §6) — đúng cho (A).
- Config per-deployment (connection string, SigningKey, GuestWebBaseUrl) — đã tách qua env (`15`).

⇒ Giữ `ResortId` như **bảo hiểm rẻ**: nếu **rất lâu về sau** có nhu cầu SaaS tập trung (đổi hẳn mô hình bảo mật), phần lớn schema đã sẵn cột phân tenant.

## 6. Nếu MỘT NGÀY cần shared-DB multi-tenant (chi phí đã lường trước — KHÔNG làm bây giờ)

Để minh bạch chi phí (fix gốc = biết trước sẽ đổi gì):
- Thêm `ResortId` vào **`GuestSession`** (hiện không có — thiết bị hiện coi là toàn cục/mỗi deployment).
- Đổi unique **`AppUser.Email`** từ global → **`(ResortId, Email)`**.
- Thêm **tenant-resolution middleware** (xác định resort theo host/subdomain/claim) + **global query filter theo `ResortId`** trên mọi tenant-entity (EF `HasQueryFilter`).
- **Per-tenant secret** (SigningKey theo tenant) + soát lại rate-limit/cache phân tenant.
- Soát lại mọi partial unique index để **scope theo `ResortId`** (ví dụ `ux_room_number` đã là `(resort_id, room_number)` — tốt; `ux_appuser_email` cần thành `(resort_id, email)`).

→ Đây là **thay đổi vừa phải, khu trú** (nhờ đã giữ `ResortId`), nhưng **chỉ làm khi thực sự đổi mô hình bảo mật** — không đầu cơ bây giờ (YAGNI + tránh phức tạp/rủi ro rò rỉ chéo không cần thiết).

## 7. Cập nhật giả định

- **A1 (single-resort)** trong `ai-notes` được **nâng cấp thành quyết định tenancy có chủ đích**: *Instance-per-resort (DB-per-tenant bằng triển khai)*, khớp D1. Không còn là "giả định ngầm" mà là **lựa chọn có lý do**.
- Ghi TK-033: checklist chuyển sang shared-DB multi-tenant (mục 6) — để sau này nếu cần thì có sẵn.

## 8. Truy vết
- Căn cứ: docs Decision D1 (WiFi nội bộ, không public internet), Deployment (DNS nội bộ per-site).
- Align: `15` (config/secret per-deployment), `04` §6 (seed một resort), `20` (deploy per-site).
- **Validates: Requirements (Introduction — network isolation), 12.5, 14.x.**
