# 11 — Architecture Review & Gaps (audit nghiêm ngặt)

> **File authoritative cho:** kết quả rà soát kiến trúc "đã đầy đủ chưa / đã chuẩn chưa". Ghi trung thực: điểm đạt, **gap tìm được**, cách xử lý (fix gốc), và rủi ro còn lại. Ngày audit: 2026-07-03.

## A. Traceability: 20 Requirements ↔ Design (đã map)

| Req | Chủ đề | Design phủ ở |
|---|---|---|
| 1 | Kiến trúc phân tầng & dependency rule | `01-architecture` §1–3; `08` B1 |
| 2 | DI theo convention | `01-architecture` §5; `08` — |
| 3 | Base entity (audit/soft-delete/concurrency) | `02` §1; `03` §3; `08` B6/B10 |
| 4 | Result/Error & ProblemDetails | `02` §2; `03` §1; `08` B5 |
| 5 | Repository & UoW nguyên tử | `02` §3; `05` §2; `08` B2 |
| 6 | Cross-cutting ports & tính tất định | `02` §5; `08` B4 |
| 7 | PublicToken an toàn | `05` §1; `08` B3; `10` §10 |
| 8 | Data model & invariant ở DB | `04` §3–5; `08` B3 |
| 9 | Đa ngôn ngữ & fallback | `04` §2; `08` — |
| 10 | Resolve token, GuestVisit, portal window | `05` §3; `08` B8 |
| 11 | Auth kép & tách guest/admin | `03` §4; `08` B9; `10` §2 |
| 12 | Validation & sanitize | `03` §2; `04` §1; `08` B7 |
| 13 | Rate limiting | `03` §5; `10` §2 |
| 14 | Logging/health/sweeper | `03` §6; `10` §9 |
| 15 | Seed & ResortSettings | `04` §6; `10` §1 |
| 16 | Room & QR token | `04` §3; `05` §1; `10` §8 |
| 17 | Nền Frontend | `../frontend/02` |
| 18 | Extension points | `01` §3; `04` §4 |
| 19 | Testing & enforce kiến trúc | `07`; `08`; `17` |
| 20 | CORS, security headers & HTTPS | `03` §7 (CORS/CSP/HSTS), §8 (ForwardedHeaders); `20` (deploy/TLS/headers); Property B9 |

**Kết luận traceability:** mọi requirement đều có chỗ phủ trong design. Không có requirement "mồ côi".

## B. Verdict tổng thể

Kiến trúc nền **đạt chuẩn** cho một sản phẩm thương mại: Clean/Onion layering + Dependency Inversion đúng chiều, invariant enforce ở DB (không chỉ code), loại bỏ tính bất định để test, tách bạch guest/admin surface, hợp đồng lỗi ổn định, concurrency an toàn. Các điểm dưới đây là **tinh chỉnh (refinement)**, không phải lỗi nền tảng — nhưng cần chốt trước khi code để "chuẩn thật".

## C. Gap tìm được + cách xử lý (fix gốc)

### GAP-1 — Xác thực Guest với SignalR Hub — ✅ ĐÃ GIẢI QUYẾT (`21`)
- **Bản chất:** guest dùng cookie (không JWT). Hub phục vụ cả guest lẫn admin; rủi ro guest nghe lén conv của lượt/khách khác nếu tin `conversationId` client.
- **Xử lý (đã đặc tả đầy đủ ở `21-realtime-signalr.md`):** admin gửi JWT qua query `access_token` (WS không set được header Authorization) + đọc ở `OnMessageReceived`; guest dùng cookie tự gửi trên handshake. `JoinConversation` **authorize bằng dữ liệu server** (suy `IGuestContext`→visit→conv), không tin id client; guest chỉ join conv của chính visit mình; guest không join group staff. Notify post-commit qua `IRealtimeNotifier`. Fallback polling khi WS lỗi.
- Trạng thái: ✅ đặc tả xong ở base (`21`); chi tiết nghiệp vụ messaging điền ở wave Messaging trên nền này.

### GAP-2 — CSRF trên endpoint guest ghi (cookie-auth)
- **Bản chất:** `POST /messages`, `/housekeeping` xác thực bằng **cookie** → về lý thuyết dính CSRF (khác admin dùng JWT ở header, không dính). 
- **Xử lý:** (a) cookie `GuestSession` đặt `SameSite=Lax` (chặn phần lớn cross-site POST); (b) yêu cầu **custom header** (ví dụ `X-Requested-With`) cho các POST guest — request giả mạo cross-site không tự thêm được; (c) CORS chặt same-origin. Bối cảnh WiFi nội bộ làm rủi ro thấp nhưng **vẫn phải có (b)+(c)** cho commercial. Bổ sung vào `10-production-hardening` §2 khi triển khai auth.
- Trạng thái: đã ghi TK-020.

### GAP-3 — Chiến lược đọc (read model) cho truy vấn phức tạp
- **Bản chất:** `IRepository<T>.Query()` trả `IQueryable` — tiện nhưng (a) hơi "leaky" (Application chạm khái niệm truy vấn của EF), (b) không tối ưu cho **dashboard stats** và **inbox gom theo phòng + đếm unread** (Req 9, Dashboard).
- **Xử lý (CQRS-lite):** cho phép **read query service** riêng: interface đọc khai ở `Application` (ví dụ `IInboxQueries`, `IDashboardQueries`) trả **DTO read-model**, implement ở `Infrastructure` bằng EF projection (`Select` → DTO, `AsNoTracking`). Generic repo giữ cho **ghi + đọc đơn giản**. Không dùng `IQueryable` xuyên tầng cho read phức tạp.
- Trạng thái: bổ sung nguyên tắc này vào `02-core-abstractions` (đã ghi DEV-007).

### GAP-4 — Dual-write: realtime notify vs commit DB
- **Bản chất:** nếu `IRealtimeNotifier` được gọi **trong** transaction/trước `SaveChanges`, có thể bắn sự kiện realtime rồi DB **rollback** → client thấy dữ liệu "ma".
- **Xử lý (quy tắc nền):** realtime notify **luôn gọi SAU khi `SaveChangesAsync`/transaction commit thành công** (post-commit). MVP đủ dùng. Nếu cần đảm bảo mạnh hơn (at-least-once) → **Outbox pattern** ở giai đoạn sau (ghi TK-021, không làm ở base).
- Trạng thái: quy tắc bổ sung vào `02`/`03` (DEV-008).

### GAP-5 — Mapping library thương mại (AutoMapper)
- **Bản chất:** AutoMapper đã chuyển thương mại (RPL/commercial) 7/2025 → dùng trong sản phẩm thương mại là rủi ro pháp lý.
- **Xử lý:** thay bằng **Mapperly** (Apache-2.0 miễn phí, source-generator, không reflection, nhanh) hoặc mapping thủ công. Đã sửa trong `06`, `07`, `technology-stack` (audit license).
- Trạng thái: ✅ đã fix.

### GAP-6 — OpenAPI cho backend (nguồn sinh type FE)
- **Bản chất:** FE dự định sinh `shared-types` từ OpenAPI (TRD-006) nhưng backend chưa nêu nguồn OpenAPI.
- **Xử lý:** backend expose **OpenAPI** qua `Microsoft.AspNetCore.OpenApi` (native .NET) hoặc NSwag; publish ổn định để FE sinh type. Bổ sung vào `10` §8 (API versioning) — endpoint `/openapi/v1.json`.
- Trạng thái: đã ghi TK-022.

### GAP-7 — SharedKernel phải "sạch" tuyệt đối
- **Bản chất:** Domain → SharedKernel. Nếu SharedKernel lỡ tham chiếu EF/ASP.NET thì Domain gián tiếp dính.
- **Xử lý:** ArchitectureTest thêm rule: `ResortQr.SharedKernel` **không** reference EF Core/ASP.NET/Infrastructure. Bổ sung vào `06`/`07` (ArchitectureTests).
- Trạng thái: đã ghi vào conventions.

### GAP-8 — Design-time DbContext factory cho migration
- **Bản chất:** tạo migration cần `IDesignTimeDbContextFactory` (reference có `ApplicationContextFactory`) nếu Api là startup khác Infrastructure.
- **Xử lý:** thêm `AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>` trong Infrastructure. Nhỏ, ghi để không quên (TK-023).

## D. Rủi ro còn lại (trung thực, chưa "đóng")
- Các mục phụ thuộc hạ tầng (secrets/backup/CI/deploy) — TK-007..016.
- License QuestPDF theo ngưỡng doanh thu — TK-019.
- Trạng thái archive PrimeVue (đã né bằng Element Plus) — TK-017.
- Version patch/tương thích chéo — TK-005/018.
- GAP-1/GAP-2 chi tiết hoá khi làm module Auth/Messaging.

## E. Kết luận
Nền **đủ và chuẩn về kiến trúc** để bắt đầu triển khai, **sau khi** ghi nhận GAP-1..8 (đa số là quy tắc/ghi chú, hai gap đã fix ngay là mapping-license và read-model). Không có lỗi nền tảng buộc thiết kế lại.
