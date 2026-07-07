# Wave Design — Enrich OpenAPI response schema (nợ TK-054)

## 1. Vấn đề & nguyên nhân gốc (đã verify từ code, không suy đoán)

`/openapi/v1.json` hiện có đủ path/verb/param/request-body nhưng **response phần lớn rỗng schema**.

**Nguyên nhân gốc (verify):** các endpoint minimal-API khai báo delegate trả `Task<IResult>` / `IResult` (ví dụ `AuthEndpoints.LoginAsync`, `GuestAccessEndpoints.ResolveAsync`). Kiểu trả bị xóa (type-erased) → `ApiExplorer` KHÔNG suy được kiểu response → document thiếu schema. Ngoại lệ: endpoint trả kiểu cụ thể như `Ok<MeResponse>` (`/auth/me`) thì 200 ĐƯỢC suy tự động.

**Không phải nguyên nhân:** không phải do generator/OpenApi package thiếu (đã chạy, document hợp lệ) — mà do thiếu **metadata `Produces`** ở endpoint.

**Fix gốc:** khai báo tường minh `.Produces<TDto>(status)` (success) + `.ProducesProblem(status)` (lỗi, `application/problem+json`) theo đúng hành vi thực của từng endpoint. KHÔNG đổi logic runtime (chỉ thêm metadata).

## 2. Phương pháp khai báo status CHÍNH XÁC (không bịa)

Status của mỗi endpoint suy ra từ 3 nguồn đã đọc trong code:

1. **Success**: đọc thẳng endpoint — `TypedResults.Ok(dto)`→200+dto; `Created`→201+dto; `NoContent`→204 (không body); `File`→200 image/png.
2. **Lỗi nghiệp vụ**: đọc `*Errors` của module + map `ErrorType→HTTP` (`ErrorTypeToHttp`): Validation→400, NotFound→404, Conflict→409, Forbidden→403, Unauthorized→401, RateLimited→429.
3. **Lỗi pipeline (kết cấu)**:
   - `RequireAuthorization` (không `AllowAnonymous`) → **401** (+**403** nếu policy Role, ví dụ RequireStaff/RequireAdmin có thể 403 khi sai role).
   - `RequireRateLimiting` → **429**.
   - Có **validator** (FluentValidation) cho input → **400** (`validation_error`).
   - `ExceptionHandlingMiddleware` → **500** (unexpected) luôn có thể; concurrency/unique → **409**.

**Chỉ khai báo status có căn cứ.** Ví dụ đã verify: `LoginCommand`/`RefreshCommand` **KHÔNG có validator** → login/refresh KHÔNG khai 400; guest rules/faq/housekeeping **có validator** → có 400.

## 3. Hợp đồng lỗi (ProblemDetails)

Mọi lỗi trả `application/problem+json` (schema ProblemDetails chuẩn) + extension `code` (∈ catalog `14`), `traceId`, và `errors` (khi validation). `.ProducesProblem(status)` document schema ProblemDetails chuẩn.

**Giới hạn đã biết (ghi TK):** `.ProducesProblem` document ProblemDetails cơ bản, **chưa** mô tả extension `code`/`errors` trong schema. FE map theo `code` — sẽ bổ sung schema tùy biến ở wave sau nếu cần (không chặn type-gen success DTO). Ghi rõ để không ảo tưởng "contract đã đủ 100%".

## 4. Cơ chế (helper dùng lại)

`OpenApi/OpenApiConventions.cs`:
```csharp
public static RouteHandlerBuilder ProducesProblems(this RouteHandlerBuilder b, params int[] statusCodes)
    → foreach → b.ProducesProblem(status);   // application/problem+json (mặc định)
```
Success dùng `.Produces<TDto>()` (mặc định 200) / `.Produces<TDto>(201)` / `.Produces(204)`.

## 5. Phạm vi wave này: NHÓM AUTH (nhỏ, ổn định nhất — làm trước)

| Endpoint | Success | Lỗi (đã verify) |
|---|---|---|
| `POST /auth/login` | 200 `TokenResponse` | 401 (invalid_credentials, Unauthorized) · 429 (AuthPolicy) · 500 |
| `POST /auth/refresh` | 200 `TokenResponse` | 401 (invalid_refresh_token) · 429 · 500 |
| `POST /auth/logout` | 204 (no body) | 429 · 500 |
| `GET /auth/me` | 200 `MeResponse` (đã tự suy từ `Ok<MeResponse>`) | 401 · 429 · 500 |

Ghi chú: cả nhóm nằm trong group `RequireRateLimiting(AuthPolicy)` → 429 áp cho tất cả. login/refresh `AllowAnonymous` (không 401 do authz mà do nghiệp vụ Unauthorized). `/me` `RequireAuthorization` → 401 khi ẩn danh.

## 6. Kịch bản test (mở rộng `OpenApiEndpointTests`)

- **TC-OA-Auth**: fetch `/openapi/v1.json` (OpenApi bật) → tại `paths./auth/login.post.responses` phải có key `"200"` và `"401"`; response 200 phải tham chiếu schema (content `application/json`). Khẳng định fix có hiệu lực thật ở document, không chỉ ở code.

## 7. Ngoài phạm vi (wave sau, theo thứ tự)

- Guest group (`resolve`/`rules`/`faq`/`housekeeping`) — có validator → thêm 400; resolve 404/409/429.
- Admin Rooms (200/201/204, 400 validator, 401/403 authz, 404/409, 500) + QR png (200 image/png).
- Admin Rules/FAQ/Housekeeping/Notes/Settings/Dashboard.
- (Tùy chọn) schema tùy biến cho ProblemDetails `code`.

## 8. An toàn & bất biến giữ nguyên

- Chỉ thêm metadata; KHÔNG đổi logic/response runtime → 278 test cũ phải vẫn xanh.
- `foundation/` không đụng. Build 0 warning (TreatWarningsAsErrors) — helper phải sạch analyzer.
