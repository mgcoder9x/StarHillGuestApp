# 10 — Production Hardening (chuẩn sản phẩm thương mại)

> **File authoritative cho:** các mối quan tâm cấp production/thương mại chưa được nói sâu ở 00–09: quản lý bí mật, bảo mật vận hành, quyền riêng tư & vòng đời dữ liệu, quản trị migration/release, CI/CD, backup/DR, versioning API, threat model tóm tắt.
>
> **Nguyên tắc:** mục nào là **giả định** hoặc **cần user quyết** đều đánh dấu ⚠️ và trỏ tới `../../ai-notes/`. Không bịa con số/hạ tầng cụ thể ngoài những gì docs/requirements đã nêu.

## 1. Quản lý bí mật (secrets)

- **Không hardcode** bất kỳ bí mật nào trong mã hay `appsettings.json` commit vào repo (reference vi phạm: connection string + mật khẩu `123` nằm thẳng trong appsettings — E-series).
- Nguồn bí mật theo môi trường:
  - Dev: **User Secrets** (`dotnet user-secrets`) hoặc `.env` không commit.
  - Staging/Prod: biến môi trường/secret store của hạ tầng (ví dụ Docker/OS env, hoặc vault nếu có). ⚠️ Nguồn cụ thể tùy hạ tầng resort — xem `../../ai-notes/04-things-to-know.md` TK-007.
- Các bí mật tối thiểu: `ConnectionStrings:Postgres`, `Jwt:SigningKey` (đối xứng ≥ 256-bit hoặc khóa bất đối xứng), `Seed:AdminPassword`.
- Khóa ký JWT SHALL xoay được (key rotation) — thiết kế cho phép nhiều khóa hợp lệ đồng thời trong giai đoạn chuyển.
- **Fail-fast**: thiếu bí mật bắt buộc → app từ chối khởi động với thông báo rõ (không chạy với default không an toàn).

## 2. Bảo mật vận hành (đối tượng người ngoài dùng chung mạng)

Dù chạy WiFi nội bộ, khách là người ngoài → giữ các lớp cơ bản (không "an toàn tuyệt đối"):

| Hạng mục | Yêu cầu |
|---|---|
| HTTPS | Bắt buộc secure context (camera/QR). HSTS bật khi cert ổn định (docs Req 12.5). |
| Cookie admin refresh | `HttpOnly; Secure; SameSite=Strict/Lax`; path giới hạn; xoay khi refresh. |
| Cookie guest | `HttpOnly; Secure; SameSite=Lax`; chỉ chứa session key ngẫu nhiên (không phòng/token). |
| CSP | `default-src 'self'; script-src 'self'; frame-ancestors 'none'; connect-src 'self' wss:` (siết theo build FE). |
| Headers | `X-Content-Type-Options=nosniff`, `Referrer-Policy`, `X-Frame-Options=DENY`. |
| Password hashing | Argon2id (khuyến nghị) hoặc PBKDF2 tham số mạnh; **không bao giờ plaintext** (sửa gốc lỗi reference E10). |
| Sanitize HTML | Bắt buộc trên đường ghi nội quy/FAQ (Property B7). |
| Rate limit | resolve/guest-write theo ngưỡng cấu hình (Req 13). |
| Log an toàn | Không log token/mật khẩu/refresh (mask) — Req 14.2. |
| Lockout | ⚠️ Chống brute-force login admin (khóa tạm sau N lần sai). Cần user chốt ngưỡng — TK-008. |

## 3. Quyền riêng tư & vòng đời dữ liệu (data lifecycle)

Sản phẩm thương mại cần chính sách dữ liệu rõ ràng, kể cả khi chưa thuộc GDPR:

- **Phân loại dữ liệu**: khách gần như ẩn danh (chỉ device cookie + nội dung chat/ack/ticket theo GuestVisit). Không thu thập PII trực tiếp ở base.
- **Retention** ⚠️: cần chính sách giữ/ẩn dữ liệu lượt lưu trú cũ (chat/ack/ticket) sau X ngày. Base **chuẩn bị chỗ** (soft-delete + trạng thái Expired/Closed) nhưng **chưa xóa tự động**; chờ user chốt thời hạn — TK-009.
- **Xuất/xóa theo yêu cầu**: thiết kế để truy vấn theo `GuestVisit`/`GuestSession` nhằm hỗ trợ xóa/ẩn khi cần (không triển khai UI ở base).
- **Acknowledgement**: là bản ghi vận hành, KHÔNG tuyên bố là chứng cứ pháp lý (docs Req 11.7) — UI/tài liệu phải nhất quán.

## 4. Audit & truy vết (commercial governance)

- Mọi entity `IAuditable` tự ghi `CreatedAt/UpdatedAt` + actor (Property B6).
- Hành động nhạy cảm (revoke QR, đóng visit, đổi settings, publish nội quy, đổi role user) SHALL sinh **audit log có cấu trúc** (ai/khi nào/đối tượng) — bổ sung ngoài audit cột entity. ⚠️ Có cần bảng `AuditTrail` riêng không? Đề xuất: có, ở wave sớm. Xem TK-010.
- `HousekeepingEvent` (docs) là ví dụ audit theo nghiệp vụ đã có.

## 5. Quản trị migration & release

- Migration **incremental per-wave** (TRD-002 đã chốt): mỗi migration additive, review độc lập, áp lên DB có dữ liệu không mất mát.
- **Không dùng `EnsureCreated`**; luôn `Migrate()` có kiểm soát. Áp migration ở bước deploy riêng (không tự migrate âm thầm khi khởi động ở prod) ⚠️ — chốt quy trình deploy với user, TK-011.
- Đặt tên migration theo wave: `Foundation_Init`, `Rules_Init`, `Faq_Init`...
- Có script kiểm tra "migration pending" trong CI để chặn drift schema.

## 6. CI/CD & chất lượng

- Pipeline tối thiểu: `restore → build (warnings-as-errors nhóm nullability/async) → unit → architecture test → integration (Testcontainers) → publish`.
- **Gate bắt buộc xanh**: ArchitectureTests (dependency rule), unit, integration index-constraint (Property B3), property-based (Req 19).
- Phân tích tĩnh: bật analyzers .NET + (khuyến nghị) kiểm tra bí mật rò rỉ (secret scanning) trong CI.
- Quét dependency (supply-chain): `dotnet list package --vulnerable`/tương đương trong CI. ⚠️ Công cụ cụ thể tùy hạ tầng — TK-012.

## 7. Backup / DR

- Docs Req 12.3: MVP **không** backup tự động, nhưng trước prod thật SHALL có tối thiểu **script dump DB thủ công**.
- Commercial nâng lên: khuyến nghị **pg_dump định kỳ** (cron) + kiểm thử restore định kỳ. ⚠️ Tần suất/nơi lưu tùy hạ tầng — TK-013.
- Migration + seed idempotent để tái dựng môi trường nhanh.

## 8. API versioning & ổn định hợp đồng

- Prefix `/api/guest/*` và `/api/admin/*` (đã có). Bổ sung **versioning** cho tương lai thương mại: `/api/v1/...` ⚠️ hoặc header version. Đề xuất path-based `v1`. Chốt với user — TK-014.
- **Hợp đồng lỗi ổn định**: tập `AppErrors.code` là API công khai với FE; thay đổi phải versioned/không phá vỡ (Property B5).
- OpenAPI xuất bản ổn định để sinh type FE (TRD-006).

## 9. Observability nâng (nhẹ nhưng đủ commercial)

- Structured logging (Serilog) + correlation id (đã có).
- **Health**: `/health/live`, `/health/ready` (đã có).
- ⚠️ Metrics/tracing (OpenTelemetry) — tùy chọn giai đoạn sau; base để sẵn `IRealtimeNotifier`/logging trừu tượng nên thêm sau không phá nền. Xem TK-015.

## 10. Threat model tóm tắt (không nặng, đúng bối cảnh)

| Mối đe dọa | Giảm thiểu |
|---|---|
| Đoán/brute-force PublicToken | Token CSPRNG ≥256-bit (Req 7) + rate limit resolve theo IP (Req 13.1) |
| Khách cũ còn giữ link | GuestVisit + portal window + lễ tân đóng phiên (docs Req 10) |
| XSS giữa người dùng chung mạng | Sanitize HTML allowlist (B7) + CSP |
| Chiếm phiên admin | JWT ngắn hạn + refresh HttpOnly rotation + policy (Req 11) |
| Rò rỉ dữ liệu lượt khác | Scope theo GuestVisit + tách guest/admin surface (B9) |
| Lộ bí mật qua log/repo | Mask log + secrets ngoài repo (§1, §2) |
| Leo thang qua concurrency | Optimistic concurrency `xmin` → 409 (B10) |

> **Tuyên bố phạm vi:** cách ly mạng là **yêu cầu hạ tầng**, app không tự enforce (docs). Không tuyên bố "an toàn tuyệt đối".
