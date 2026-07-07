# OpenAPI wave — expose `/openapi/v1.json` (GAP-6 / TK-022)

> Mục tiêu: cung cấp **hợp đồng API máy-đọc-được** để frontend sinh `shared-types` + test hợp đồng
> `ErrorCode`↔`AppErrors` (DEC-029, DEC-032 contract-first). Đây là điều kiện tiên quyết để bắt đầu FE
> mà không phỏng đoán contract.

## 1. Quyết định nền: dùng `Microsoft.AspNetCore.OpenApi` (built-in .NET 10), KHÔNG Swashbuckle/NSwag

- **Lý do (bản chất):**
  - Là gói CHÍNH THỨC của ASP.NET Core 10, cùng nhịp version/patch bảo mật với runtime (10.0.x) → không
    lệ thuộc bên thứ ba trễ nhịp (Swashbuckle nhiều giai đoạn không theo kịp .NET mới; NSwag nặng hơn nhu cầu).
  - Sinh tài liệu từ ApiExplorer của Minimal API sẵn có → không thêm tầng phản chiếu riêng.
  - Chỉ cần document JSON (không cần UI Swagger nhúng — FE tool đọc JSON). Nếu sau cần UI thì thêm Scalar/Swagger UI
    tĩnh ở proxy, không ràng buộc vào runtime.
- **Version:** pin theo band ASP.NET Core hiện dùng (JwtBearer/EF = 10.0.9). Verify bằng `dotnet restore`
  (không bịa số — nếu sai, restore báo version khả dụng).

## 2. Bảo mật: KHÔNG expose công khai mặc định ở Production

- **Vấn đề:** document OpenAPI lộ toàn bộ bề mặt API (route, tham số, schema) → ở sản phẩm thương mại, phơi
  công khai không kiểm soát là rò rỉ thông tin trinh sát (reconnaissance) cho kẻ tấn công.
- **Quyết định — cổng bật/tắt tường minh (`OpenApi:Enabled`):**
  - Mặc định: BẬT khi `IsDevelopment()` (để FE type-gen chạy ngay khi dev), TẮT ở môi trường khác.
  - Ghi đè: cờ `OpenApi:Enabled=true|false` trong config luôn thắng suy luận theo môi trường.
  - Deployment thương mại: giữ TẮT ở public; nếu cần cho FE build pipeline → bật sau reverse proxy nội bộ /
    CI, KHÔNG phơi ra internet công cộng. (Đồng nhất D1: hệ chạy mạng nội bộ.)
- **Lý do fix gốc:** an toàn-mặc-định (secure-by-default). Không dựa DUY NHẤT vào `IsDevelopment()` vì (a) một số
  pipeline build type chạy ở môi trường non-Development, (b) cờ tường minh test được tất định (không phụ thuộc
  biến môi trường ẩn của `WebApplicationFactory`).

## 3. Nội dung document (làm hợp đồng dùng được, không chỉ rỗng)

- **Info:** title `Resort QR Portal API`, version `v1`, description ngắn (mục đích + lưu ý auth).
- **Security scheme `Bearer`** (HTTP bearer, format JWT) trong `components.securitySchemes` — tài liệu hóa rằng
  endpoint admin cần `Authorization: Bearer <access_token>`.
- **Áp requirement Bearer theo operation** bằng operation transformer: chỉ operation có metadata authorization
  (endpoint `.RequireAuthorization(...)`) mới gắn security requirement Bearer. Endpoint guest (AllowAnonymous /
  cookie) KHÔNG gắn Bearer → phản ánh đúng mô hình auth (admin=JWT, guest=cookie/ẩn danh).
  - Lý do: contract chính xác giúp client sinh code gọi đúng (biết route nào cần token). Gắn Bearer toàn cục sẽ
    SAI cho guest routes.

## 4. Wiring — LUÔN đăng ký generator, chỉ GATE việc map endpoint (fix bẫy timing config)

- **Bẫy timing đã phát hiện (fix tận gốc):** đọc `builder.Configuration.GetValue<bool?>("OpenApi:Enabled")`
  EAGER ở top-level `Program` chạy TRƯỚC khi `WebApplicationFactory` tiêm config test (config test áp lúc
  `builder.Build()`). Hệ quả: cờ override trong test KHÔNG được thấy → fallback `IsDevelopment()`=true → luôn bật
  (test "tắt" fail vì nhận 200). (Config JWT của factory "thấy được" vì Options bind LAZY sau build; đọc eager thì không.)
- **Quyết định:** `AddResortQrOpenApi()` (KHÔNG tham số) LUÔN `AddOpenApi("v1", ...)` — chỉ tạo document service,
  KHÔNG phơi endpoint nào → không có rủi ro bảo mật khi đăng ký. Việc PHƠI `/openapi/v1.json` gate ở
  `MapResortQrOpenApi()`, đọc `app.Configuration`/`app.Environment` **POST-build** (đã gồm mọi nguồn config gồm
  test/host override) → chính xác + test được tất định. Thứ nhạy cảm bảo mật là *phơi endpoint*, không phải đăng ký.
  - `OpenApi:Enabled` (bool?, null → BẬT khi `IsDevelopment()`, TẮT nơi khác); cờ tường minh THẮNG suy luận môi trường.
- Transformers: `AddDocumentTransformer` (Info + securityScheme Bearer) + `AddOperationTransformer` (gắn Bearer cho op có authz).
- `Program.cs`: `builder.Services.AddResortQrOpenApi();` (trước build) + `app.MapResortQrOpenApi();` (sau build).

## 5. Phạm vi (giữ hẹp, đúng mục tiêu)

- Wave này CHỈ expose document + Info + security scheme + gắn Bearer theo authz.
- **KHÔNG** trong scope lần này: chú thích `.Produces<T>()`/`.Accepts<T>()` cho TỪNG endpoint để schema response
  đầy đủ (khối lượng lớn, rải khắp endpoint). Ghi nợ TK để enrich dần khi FE thực sự cần từng nhóm. Document vẫn
  hợp lệ và liệt kê đủ path/verb/tham số route + request body record types mà ApiExplorer suy được.

## 6. Test (kiểm chứng)

- HTTP integration (WebApplicationFactory), factory bật `OpenApi:Enabled=true`:
  1. `GET /openapi/v1.json` → 200, `content-type` JSON, parse được, có `info.title == "Resort QR Portal API"`,
     có `paths` không rỗng, có `components.securitySchemes.Bearer`.
  2. (an toàn) factory bật `OpenApi:Enabled=false` → `GET /openapi/v1.json` → 404 (không đăng ký khi tắt).
- Không cần unit test riêng (transformer thuần cấu hình, verify qua document thật là mạnh nhất).

## 7. Rủi ro đã biết

- **Microsoft.OpenApi 2.0 API surface:** .NET 10 kéo `Microsoft.OpenApi` bản mới; kiểu/khởi tạo
  `OpenApiSecurityScheme`/`OpenApiSecurityRequirement`/reference có thể khác 1.x. → hiện thực TĂNG DẦN, để trình
  biên dịch xác nhận API thật (không bịa chữ ký). Build sau mỗi bước.
