# 14 — Catalog mã lỗi ổn định (hợp đồng BE ↔ FE)

> **File authoritative cho:** toàn bộ tập `AppErrors.code`. Đây là **API công khai** giữa backend và frontend (Property B5, DEC-004). `02-core-abstractions.md` §2 chỉ liệt kê ví dụ (`// ...`); **file này là danh mục đầy đủ và chuẩn**.
>
> **Vì sao cần catalog tường minh (không để `// ...`):** FE hiển thị/điều hướng **theo `code`** chứ không đoán chuỗi `message` (message có thể i18n/đổi). Nếu tập code không cố định & không đầy đủ, FE và BE sẽ lệch hợp đồng — lỗi âm thầm khó bắt. Catalog này khóa hợp đồng đó lại và cho phép test đối chiếu 1-1 (`shared-types.ErrorCode` ↔ `AppErrors`).

## 1. Quy tắc hợp đồng

- Mỗi lỗi nghiệp vụ trả `ProblemDetails` `application/problem+json` với các trường: `type`, `title`, `status`, **`code`** (thuộc catalog này), `traceId` (Req 4.2).
- `message`/`title` **có thể đổi/đa ngôn ngữ**; **`code` là bất biến** để FE xử lý.
- **Thêm** code mới = **backward-compatible**. **Đổi tên/xóa** code = **breaking** → phải versioned (API versioning, `10` §8).
- `ErrorType` → HTTP status theo bảng ánh xạ cố định (`03` §1): Validation→400, NotFound→404, Conflict→409, Forbidden→403, RateLimited→429, Unexpected→500.

## 2. Catalog (nguồn chân lý)

### 2.1 Guest surface

| code | ErrorType | HTTP | Khi nào phát (chính xác) | FE xử lý |
|---|---|---|---|---|
| `qr_invalid` | NotFound | 404 | token không tồn tại trong `room_qr_token` | Màn TokenError; không lộ phòng khác |
| `qr_revoked` | NotFound | 404 | token có `Status='Revoked'` | Màn TokenError ("mã đã thay, hỏi lễ tân") |
| `room_inactive` | Conflict | 409 | phòng `IsDeleted` hoặc `Status ∈ {Inactive, Maintenance}` | Màn TokenError |
| `session_expired` | Forbidden | 403 | portal window quá hạn (`now-LastSeenAt>PortalWindow`) HOẶC visit không `Active`/đã `Expired`/`Closed` (kể cả lazy-expiry) | Màn "Phiên hết hạn — quét QR lại" |
| `rule_ack_required` | Forbidden | 403 | gọi FAQ/messages/housekeeping khi settings yêu cầu ack mà chưa ack publication `IsCurrent` | Mở RuleGate (đọc nội quy) |
| `message_too_long` | Validation | 400 | body tin nhắn vượt `MaxMessageLength` | Lỗi tại ô nhập |
| `rate_limited` | RateLimited | 429 | vượt ngưỡng policy `resolve`/`guest-write` | Toast + tôn trọng `Retry-After` |
| `language_not_supported` | Validation | 400 | (dự phòng) `?lang=` là mã không hợp lệ khi endpoint yêu cầu ngôn ngữ hợp lệ | Thường **fallback im lặng** về `en` (Req 2.2); code chỉ dùng khi cần báo tường minh |

> Ghi chú `language_not_supported`: theo Req 2.2 guest-web **fallback im lặng** khi ngôn ngữ không được bật; do đó code này **hiếm khi** trả ra (giữ trong catalog cho nhất quán/ổn định hợp đồng, không phát bừa).

### 2.2 Admin/Staff surface

| code | ErrorType | HTTP | Khi nào phát | FE xử lý |
|---|---|---|---|---|
| `validation_error` | Validation | 400 | FluentValidation fail (kèm danh sách field lỗi trong `errors`) | Hiện lỗi theo field |
| `concurrency_conflict` | Conflict | 409 | `DbUpdateConcurrencyException` (xmin mismatch) khi 2 người sửa cùng nội dung | "Nội dung đã đổi, tải lại" |
| `unauthorized` | (đặc biệt) | 401 | thiếu/sai chữ ký/hết hạn JWT — **do auth scheme phát (challenge)**, không qua Result | Thử refresh → thất bại thì về `/login` |
| `forbidden` | Forbidden | 403 | JWT hợp lệ nhưng sai Role (policy `RequireAdmin`/`RequireStaff`) | "Không đủ quyền" |
| `invalid_configuration` | Validation | 400 | sinh QR khi `GuestWebBaseUrl` thiếu/không phải https hợp lệ (Req 15.6) | Nhắc admin cấu hình Settings |
| `qr_generation_failed` | Conflict | 409 | phòng không `Active` khi yêu cầu QR, hoặc render QR lỗi (Req 16.7) | Thông báo + hướng xử lý |
| `pdf_limit_exceeded` | Validation | 400 | xuất PDF > 500 phòng/lần (Req 16.8) | Nhắc chia nhỏ danh sách |
| `not_found` | NotFound | 404 | tài nguyên admin theo id không tồn tại (room/user/... ) | Thông báo không tìm thấy |

### 2.3 Chung (mọi surface)

| code | ErrorType | HTTP | Khi nào phát | FE xử lý |
|---|---|---|---|---|
| `unexpected` | Unexpected | 500 | exception chưa bắt / `ErrorType` ngoài bảng ánh xạ | Toast lỗi chung; **KHÔNG** lộ stack trace (Req 4.5) |

## 3. Lưu ý 401 vs 403 (chính xác về cơ chế)

- **401 `unauthorized`**: do **authentication scheme (JWT Bearer)** phát khi không xác thực được — đây là *challenge* của pipeline, **không** đi qua đường `Result<T>`→ProblemDetails. Vẫn nên trả ProblemDetails có `code=unauthorized` + `traceId` (cấu hình event `OnChallenge`) để FE nhất quán.
- **403 `forbidden`**: do **authorization policy** từ chối (đã xác thực nhưng sai quyền). Cũng gắn `code=forbidden`.
- Phân biệt với **guest** `session_expired`/`rule_ack_required` (ErrorType.Forbidden→403) — đây là lỗi *nghiệp vụ guest*, đi qua `Result`/middleware, khác bản chất với 401/403 auth admin.

## 4. `AppErrors` C# (đầy đủ — thay cho `// ...` ở `02` §2)

```csharp
public static class AppErrors
{
    // Guest
    public static readonly Error QrInvalid           = Error.NotFound("qr_invalid", "...");
    public static readonly Error QrRevoked           = Error.NotFound("qr_revoked", "...");
    public static readonly Error RoomInactive        = Error.Conflict("room_inactive", "...");
    public static readonly Error SessionExpired      = Error.Forbidden("session_expired", "...");
    public static readonly Error RuleAckRequired     = Error.Forbidden("rule_ack_required", "...");
    public static readonly Error MessageTooLong      = Error.Validation("message_too_long", "...");
    public static readonly Error RateLimited         = Error.Rate("rate_limited", "...");
    public static readonly Error LanguageNotSupported= Error.Validation("language_not_supported", "...");
    // Admin
    public static readonly Error ValidationError     = Error.Validation("validation_error", "...");
    public static readonly Error Concurrency         = Error.Conflict("concurrency_conflict", "...");
    public static readonly Error Unauthorized        = Error.Forbidden("unauthorized", "...");   // map 401 ở pipeline
    public static readonly Error Forbidden           = Error.Forbidden("forbidden", "...");
    public static readonly Error InvalidConfiguration= Error.Validation("invalid_configuration", "...");
    public static readonly Error QrGenerationFailed  = Error.Conflict("qr_generation_failed", "...");
    public static readonly Error PdfLimitExceeded    = Error.Validation("pdf_limit_exceeded", "...");
    public static readonly Error NotFound            = Error.NotFound("not_found", "...");
    // Chung
    public static readonly Error Unexpected          = new("unexpected", "...", ErrorType.Unexpected);
}
```

## 5. Đồng bộ FE (chống lệch hợp đồng)

- `frontend/packages/shared-types` khai `type ErrorCode = 'qr_invalid' | 'qr_revoked' | ... | 'unexpected'` **khớp 1-1** catalog này (sinh từ OpenAPI hoặc test đối chiếu — TRD-006).
- Test hợp đồng: liệt kê tất cả `code` trong `AppErrors` (reflection) và assert khớp union `ErrorCode` của FE → CI phát hiện lệch (thêm/bớt code mà quên đồng bộ).

## 6. Truy vết
- **Validates: Requirements 1.4, 3.11, 4.1–4.7, 8.6, 10.3, 13, 15.6, 16.7, 16.8**
- Property liên quan: B5 (hợp đồng lỗi ổn định).
