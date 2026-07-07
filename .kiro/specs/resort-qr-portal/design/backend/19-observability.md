# 19 — Observability (structured logging, correlation, mask bí mật, health)

> **File authoritative cho:** catalog trường log có cấu trúc, correlation id (kể cả qua SignalR), quy tắc **mask bí mật** chính xác (token/mật khẩu/refresh), health check. Nền cho Req 12.1–12.2, 14.1–14.8. Giữ "nhẹ" đúng bối cảnh nội bộ (docs Observability), nhưng đủ để chẩn đoán sự cố sản phẩm thương mại.

## 1. Nguyên tắc

- **Structured logging (Serilog) JSON** — log là dữ liệu truy vấn được, không phải chuỗi tự do.
- **Đủ để debug, không lộ bí mật/PII** — hai mục tiêu phải cân bằng (Req 12.1 vs 11.6).
- Không dựng observability nặng ở base (docs D7); OpenTelemetry để mở (TK-015).

## 2. Correlation id (một sợi chỉ xuyên request)

- Mỗi request có **CorrelationId**: nhận từ header `X-Correlation-Id` nếu client gửi (hợp lệ), ngược lại sinh mới (dùng `Activity.TraceId` của ASP.NET Core làm nguồn để đồng bộ với distributed tracing sau này).
- Gắn vào: **mọi dòng log của request** (Serilog `LogContext`/enricher), trường **`traceId` trong ProblemDetails** (Req 4.2), và **header response** `X-Correlation-Id` (để client/đối soát).
- **Qua SignalR:** khi client kết nối hub, đọc/sinh correlation trên `HubConnectionContext`; mỗi hub method enrich cùng CorrelationId → nối được luồng realtime với request REST liên quan.
- Middleware CorrelationId chạy **sớm** (sau ForwardedHeaders `03` §8, trước logging scope).

## 3. Catalog trường log (structured fields)

| Trường | Khi nào | Ghi chú |
|---|---|---|
| `CorrelationId` | mọi log | sợi chỉ xuyên suốt |
| `RequestPath`, `Method`, `StatusCode`, `ElapsedMs` | mỗi request (middleware) | path đã **mask token** (§4) |
| `ResortId` | khi biết ngữ cảnh resort | |
| `RoomId` | **chỉ khi resolve thành công** (Req 14.1) | không log khi token lỗi (tránh lộ dò phòng) |
| `ConversationId` | thao tác messaging | |
| `GuestVisitId` | thao tác guest | định danh lượt, không phải danh tính người |
| `UserId`, `Role` | hành động admin/staff | từ `ICurrentUser` |
| `ErrorCode` | khi trả lỗi | thuộc catalog `14` |
| `EventName` | hành động nhạy cảm | revoke QR, close visit, publish, đổi settings, đổi role |

- **Level:** `Information` cho request thành công + hành động nghiệp vụ; `Warning` cho lỗi nghiệp vụ 4xx (rate_limited, session_expired, validation) — **không** phải lỗi hệ thống; `Error` cho 5xx/unhandled (kèm exception server-side, KHÔNG trả client — Req 4.5).

## 4. Mask bí mật (chính xác — làm ở đâu)

**Bản chất:** log request tự động dễ vô tình ghi token/mật khẩu. Phải mask **có chủ đích tại nguồn**, không dựa may rủi.

- **Path chứa token:** request-logging middleware **mask** segment token của `/r/{token}`, `/api/guest/resolve/{token}` → `/r/***` (Req 12.2). Không ghi query `?lang=` nhạy cảm (không nhạy cảm, giữ được).
- **Header:** **không** log `Authorization`, `Cookie`, `Set-Cookie` (chứa JWT/refresh/guest cookie).
- **Body:** nếu bật request/response body logging (chỉ Dev), **redact** field `password`, `signingKey`, `token`, `refreshToken` bằng Serilog destructuring policy (allowlist field được log, hoặc denylist redact).
- **QR token trong DB log:** khi log entity `RoomQrToken`, chỉ log `TokenPreview` (đã che), **không** `Token` đầy đủ (Req 11.6).
- **Mật khẩu/refresh:** không bao giờ vào log dưới mọi hình thức (kể cả hash — không cần thiết).
- **PII/nội dung riêng tư:** **không** log **nội dung tin nhắn** ở level Information (quyền riêng tư khách); log `ConversationId` + độ dài nếu cần, không log body. Debug body chỉ ở Dev.

## 5. Health check (Req 14.2, 12.2)

- **`/health/live`** (liveness): chỉ kiểm tiến trình còn sống, **không** phụ thuộc DB/ngoại vi; trả nhanh (< 2s — Req 14.3). Dùng cho orchestrator restart.
- **`/health/ready`** (readiness): kiểm **kết nối DB** (Npgsql health check) với timeout (mặc định 5s — Req 14.6); DB không sẵn sàng → `503` + chỉ báo **không lộ chuỗi kết nối/secret** (Req 14.7). Dùng cho load balancer đưa/rút traffic.
- Tách hai endpoint bằng **tag** (`live` vs `ready`) trong `Microsoft.Extensions.Diagnostics.HealthChecks`.

## 6. Background service `VisitIdleSweeper` (quan sát)

- Log mỗi lượt quét: số visit xử lý, số lỗi (Warning nếu có), thời gian. Không để 1 visit lỗi chặn cả lượt (Req 14.8; `13` §6).
- **Tuyệt đối không** đụng `RoomQrToken` (Req 14.5) — QR vật lý chỉ revoke thủ công.

## 7. Ranh giới với Audit trail

- **Log** = chẩn đoán kỹ thuật (có thể xoay vòng/xóa theo retention).
- **Audit trail** (hành động nhạy cảm: revoke QR, đổi settings, publish, đổi role) = bản ghi **nghiệp vụ/tuân thủ**, nên bền hơn log — cân nhắc bảng `AuditTrail` riêng (TK-010). `HousekeepingEvent` là ví dụ audit theo nghiệp vụ đã có.
- Đừng lẫn hai thứ: đừng dựa log để đối soát "ai làm gì" (log có thể bị xoay vòng).

## 8. Truy vết
- **Validates: Requirements 4.2, 4.5, 11.6, 12.1, 12.2, 14.1–14.3, 14.5–14.8**
