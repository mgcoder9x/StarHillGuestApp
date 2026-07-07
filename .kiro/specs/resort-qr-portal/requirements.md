# Requirements Document

## Introduction

Tài liệu này đặc tả **yêu cầu cho phần nền (foundational base)** của hệ thống Resort QR Portal (Star Hill Guest App), suy ra trực tiếp từ `design.md` của spec này. Phần nền bao gồm: cấu trúc & phân tầng backend (modular monolith), các abstraction lõi (Entity, Result/Error, Repository/UnitOfWork, các port cross-cutting), cross-cutting concerns (error handling, validation, xác thực kép, rate limiting, logging, health check, CORS/security headers/HTTPS), nền data model (base entity conventions, mẫu đa ngôn ngữ, DB constraints), các module nền được hiện thực đầy đủ (Identity, Settings/Localization, Rooms + QR token, GuestAccess), khung mở rộng (extension points) cho module nghiệp vụ, và nền Frontend (2 SPA + shared packages).

Phần nền **không** hiện thực toàn bộ nghiệp vụ (nội quy/FAQ/chat/housekeeping) — các nghiệp vụ đó được đặc tả tại `docs/resort-qr-portal/`. Tài liệu này chỉ đặc tả yêu cầu để dựng nền vững chắc và cung cấp **chỗ cắm (extension points)** cho các module nghiệp vụ về sau.

Mục tiêu chất lượng dẫn dắt mọi yêu cầu: correctness-by-construction (bất biến enforce ở DB + domain), testability (loại bỏ tính bất định qua abstraction), low coupling giữa module, explicit boundaries giữa guest surface và admin surface, và ergonomics (boilerplate tối thiểu, không "magic" khó debug).

Ghi chú ngôn ngữ: các từ khoá EARS (WHEN, WHILE, IF, THEN, WHERE, THE, SHALL) giữ nguyên tiếng Anh theo quy ước; phần mô tả bằng tiếng Việt để đồng bộ với bộ tài liệu hiện có.

## Glossary

- **Platform**: Toàn bộ hệ thống nền Resort QR Portal (backend + frontend base) được đặc tả trong tài liệu này.
- **Solution**: Cấu trúc backend gồm 5 project: `ResortQr.SharedKernel`, `ResortQr.Domain`, `ResortQr.Application`, `ResortQr.Infrastructure`, `ResortQr.Api`.
- **Architecture_Test**: Bộ kiểm thử (NetArchTest) enforce dependency rule và naming convention giữa các project của Solution.
- **DI_Container**: Cơ chế đăng ký phụ thuộc theo convention (Scrutor + marker interface) đăng ký service từ assembly tường minh.
- **Domain_Entity**: Lớp thực thể trong `ResortQr.Domain` kế thừa `Entity`/`AuditableEntity`.
- **DbContext**: `AppDbContext` trong `ResortQr.Infrastructure` (EF Core + Npgsql/PostgreSQL).
- **Repository**: Thành phần thao tác ChangeTracker (Add/Update/Remove/Query) cho một `Domain_Entity`, KHÔNG tự ghi DB.
- **UnitOfWork**: Thành phần cung cấp điểm ghi DB duy nhất (`SaveChangesAsync`) và giao dịch tường minh (`ExecuteInTransactionAsync`).
- **UseCase**: Lớp use-case-per-operation trong `ResortQr.Application`, nhận input DTO và trả `Result<T>`.
- **Result**: Kiểu `Result<T>` trung lập (Ok/Fail) chứa giá trị hoặc `Error` với `code` ổn định.
- **AppError**: Tập mã lỗi ổn định (`AppErrors`) với `code`, `message`, `ErrorType`.
- **Error_Middleware**: Middleware ánh xạ exception chưa bắt và `Result.Fail` sang `application/problem+json` (ProblemDetails) kèm `code`.
- **Validation_Pipeline**: Behavior FluentValidation chạy trước UseCase.
- **Clock**: Port `IDateTimeProvider` cung cấp `UtcNow`.
- **Token_Generator**: Port `ITokenGenerator` sinh PublicToken bằng CSPRNG (base64url).
- **Html_Sanitizer**: Port `IHtmlSanitizer` làm sạch HTML theo allowlist.
- **Current_User**: Port `ICurrentUser` cung cấp ngữ cảnh admin/staff (UserId, Role).
- **Guest_Context**: Port `IGuestContext` cung cấp ngữ cảnh guest (GuestSessionId, SessionKey) từ cookie.
- **Realtime_Notifier**: Port `IRealtimeNotifier` trừu tượng hoá SignalR cho UseCase.
- **Qr_Service**: Port `IQrService` render ảnh QR PNG từ URL.
- **Pdf_Service**: Port `IPdfService` render PDF nhãn phòng.
- **Translation_Resolver**: Port `ITranslationResolver` resolve bản dịch theo ngôn ngữ với fallback.
- **Auth_Layer**: Thành phần xác thực gồm JWT Bearer (admin/staff) và guest cookie.
- **Guest_Access_Service**: UseCase resolve token, tạo/nối `GuestVisit`, và enforce cửa sổ thao tác (portal window).
- **Rate_Limiter**: Cơ chế giới hạn tần suất (Microsoft.AspNetCore.RateLimiting) cho guest surface.
- **Logger**: Serilog structured logging.
- **Seeder**: Thành phần seed data khởi tạo (idempotent).
- **Frontend_Base**: Monorepo 2 SPA (guest-web, admin-web) + shared packages (api-client, shared-types, realtime, ui-kit).
- **Api_Client**: Wrapper fetch tập trung của Frontend_Base ánh xạ ProblemDetails → `ApiError{code}`.
- **PublicToken**: Chuỗi ngẫu nhiên, duy nhất, không đoán được, gắn với một phòng, nhúng vào URL QR.
- **GuestSession**: Định danh thiết bị của khách (cookie HttpOnly dài hạn 30–90 ngày).
- **GuestVisit**: Lượt lưu trú của một phòng; trạng thái `Active`/`Closed`/`Expired`.
- **Portal_Window**: Cửa sổ thao tác ngắn (mặc định 30 phút) tính từ hoạt động gần nhất.
- **ResortSettings**: Bộ cấu hình vận hành của resort.
- **Security_Layer**: Thành phần cấu hình CORS, security header (CSP, `X-Content-Type-Options`, HSTS) và bắt buộc HTTPS cho mọi bề mặt HTTP của Platform.

---

## Requirements

### Requirement 1: Kiến trúc phân tầng & Dependency Rule

**User Story:** Là kiến trúc sư hệ thống, tôi muốn một cấu trúc modular monolith phân tầng rõ ràng với dependency rule được enforce tự động, để mọi module nghiệp vụ về sau xây trên nền mà không phá vỡ ranh giới.

#### Acceptance Criteria
1. THE Solution SHALL gồm đúng năm project (không nhiều hơn và không ít hơn): `ResortQr.SharedKernel`, `ResortQr.Domain`, `ResortQr.Application`, `ResortQr.Infrastructure`, `ResortQr.Api`.
2. THE `ResortQr.Domain` SHALL KHÔNG khai báo project reference hoặc package reference tới EF Core, `ResortQr.Infrastructure` hoặc `ResortQr.Api` (kiểm tra ở mức assembly).
3. THE `ResortQr.Application` SHALL KHÔNG khai báo project reference tới `ResortQr.Api` hoặc tới phần hiện thực trong `ResortQr.Infrastructure` (kiểm tra ở mức assembly).
4. THE `ResortQr.Infrastructure` SHALL cung cấp đúng một lớp hiện thực cho mỗi port khai báo trong `ResortQr.Application`, sao cho KHÔNG còn port nào chưa có hiện thực khi Platform khởi động.
5. WHEN Architecture_Test chạy THEN Architecture_Test SHALL fail kèm báo cáo chỉ rõ project và reference vi phạm nếu có bất kỳ vi phạm nào ở cấu trúc project (Acceptance Criteria 1) hoặc dependency rule (Acceptance Criteria 2, 3); IF không có vi phạm THEN Architecture_Test SHALL pass.
6. THE `ResortQr.Application` và `ResortQr.Infrastructure` SHALL đặt mỗi module nghiệp vụ trong đúng một thư mục con riêng dưới `Modules/`, và code của một module SHALL KHÔNG nằm ngoài thư mục con của module đó.

### Requirement 2: Đăng ký phụ thuộc theo convention (DI)

**User Story:** Là nhà phát triển, tôi muốn thêm một service mới chỉ bằng cách khai báo tại chỗ mà không sửa `Program.cs`, để mở rộng module không đụng nền.

#### Acceptance Criteria
1. WHEN Platform khởi động THEN DI_Container SHALL quét và đăng ký mỗi class hiện thực một trong các marker interface với lifetime tương ứng: `IScopedService` → Scoped, `ISingletonService` → Singleton, `ITransientService` → Transient.
2. THE DI_Container SHALL chỉ quét các assembly được khai báo tường minh qua `AssemblyMarker` của từng project; các assembly KHÔNG khai báo `AssemblyMarker` SHALL KHÔNG bị quét và các class trong đó SHALL KHÔNG được đăng ký.
3. IF một interface kế thừa marker interface chưa có class hiện thực nào THEN DI_Container SHALL bỏ qua interface đó và KHÔNG tạo đăng ký nào cho nó.
4. IF không có interface nào phát sinh lỗi đăng ký THEN Platform SHALL hoàn tất khởi động thành công (KHÔNG ném exception liên quan tới DI).
5. IF một interface có nhiều hơn một class hiện thực THEN DI_Container SHALL đăng ký tất cả các class hiện thực đó với cùng lifetime của interface và SHALL KHÔNG ném exception.
6. IF một class hiện thực đồng thời nhiều marker interface có lifetime khác nhau THEN DI_Container SHALL dừng quá trình đăng ký và Platform SHALL báo lỗi khởi động cho biết có xung đột lifetime, KHÔNG đăng ký mập mờ class đó.

### Requirement 3: Quy ước base entity (audit, soft-delete, concurrency)

**User Story:** Là nhà phát triển, tôi muốn mọi entity kế thừa quy ước chung về khoá chính, audit, soft-delete và concurrency token, để giảm boilerplate và đảm bảo tính nhất quán.

#### Acceptance Criteria
1. THE Domain_Entity SHALL dùng khoá chính kiểu `uuid` (`Guid`), sinh phía ứng dụng bằng `Guid.CreateVersion7()` (UUIDv7 time-ordered); WHERE benchmark locality yêu cầu THE hệ thống MAY chuyển nguồn sinh sang `DEFAULT uuidv7()` của PostgreSQL 18 mà KHÔNG đổi kiểu cột (cùng `uuid`).
2. WHEN một `Domain_Entity` hiện thực `IAuditable` được thêm mới THEN DbContext SHALL set `CreatedAt` bằng `Clock.UtcNow` và set `CreatedByUserId` bằng `Current_User.UserId` nếu có ngữ cảnh người dùng đã đăng nhập, ngược lại để `CreatedByUserId = null`, trong `SaveChangesAsync`.
3. WHEN một `Domain_Entity` hiện thực `IAuditable` được cập nhật THEN DbContext SHALL set `UpdatedAt` bằng `Clock.UtcNow` và set `UpdatedByUserId` bằng `Current_User.UserId` nếu có ngữ cảnh người dùng đã đăng nhập (ngược lại `null`), trong `SaveChangesAsync`, và SHALL KHÔNG thay đổi giá trị `CreatedAt` và `CreatedByUserId` đã ghi lúc tạo.
4. WHEN một `Domain_Entity` hiện thực `ISoftDeletable` được yêu cầu xoá THEN DbContext SHALL set `IsDeleted = true` và set `DeletedAt` bằng `Clock.UtcNow` thay vì xoá cứng, giữ nguyên bản ghi trong bảng (không xoá vật lý) và không thay đổi khoá chính.
5. WHEN truy vấn một `Domain_Entity` hiện thực `ISoftDeletable` THEN DbContext SHALL loại trừ các bản ghi có `IsDeleted = true` qua global query filter.
6. THE DbContext SHALL ánh xạ tên bảng và cột sang snake_case.
7. WHERE một `Domain_Entity` hiện thực `IConcurrencyAware` THE DbContext SHALL cấu hình `RowVersion` là concurrency token ánh xạ tới `xmin` của PostgreSQL.
8. IF phát hiện xung đột concurrency khi lưu một `Domain_Entity` hiện thực `IConcurrencyAware` (giá trị `RowVersion` hiện tại không khớp với giá trị đã đọc) THEN DbContext SHALL ném lỗi concurrency và KHÔNG ghi đè dữ liệu, để nguyên trạng thái bản ghi trong database.

### Requirement 4: Hợp đồng Result/Error & ánh xạ ProblemDetails

**User Story:** Là nhà phát triển frontend và backend, tôi muốn mọi lỗi nghiệp vụ trả về một hợp đồng lỗi ổn định với `code`, để frontend xử lý theo `code` thay vì đoán chuỗi message.

#### Acceptance Criteria
1. THE UseCase SHALL trả về `Result<T>` trung lập (KHÔNG chứa status HTTP, header hay media-type) cho cả trường hợp thành công và thất bại; một `Result` thất bại SHALL mang đúng một `ErrorType` và đúng một `code` thuộc tập `AppError`.
2. WHEN một `Result` thất bại được trả về THEN Error_Middleware SHALL trả `application/problem+json` với HTTP status không thuộc dải 2xx và chứa đầy đủ các trường `type`, `title`, `status`, `code` (thuộc tập `AppError`) và `traceId`.
3. THE Error_Middleware SHALL ánh xạ `ErrorType` sang HTTP status: `Validation`→400, `NotFound`→404, `Conflict`→409, `Forbidden`→403, `RateLimited`→429, `Unexpected`→500.
4. IF một `Result` thất bại mang `ErrorType` không nằm trong bảng ánh xạ ở Acceptance Criteria 3 THEN Error_Middleware SHALL trả HTTP 500 với `code` ứng với `Unexpected`.
5. IF một exception chưa được bắt xảy ra THEN Error_Middleware SHALL trả HTTP 500 với ProblemDetails có `code` ứng với `Unexpected` và `traceId`, mà KHÔNG chứa stack trace hoặc chi tiết exception nội bộ.
6. THE Api_Client SHALL ánh xạ trường `code` trong ProblemDetails sang `ApiError.code` tương ứng một-một với tập `AppError`.
7. IF ProblemDetails không chứa trường `code` HOẶC `code` không thuộc tập `AppError` THEN Api_Client SHALL dùng một `ApiError.code` mặc định (unexpected) và SHALL KHÔNG crash.

### Requirement 5: Repository & Unit of Work (ghi DB nguyên tử)

**User Story:** Là nhà phát triển, tôi muốn gom nhiều thay đổi vào một lần ghi nguyên tử, để đảm bảo bất biến liên bảng (ví dụ publish nội quy) không bị ghi dở dang.

#### Acceptance Criteria
1. THE Repository SHALL chỉ thao tác ChangeTracker (Add/Update/Remove/Query) và SHALL KHÔNG gọi `SaveChanges` bên trong các thao tác đó.
2. THE UnitOfWork SHALL là điểm duy nhất ghi thay đổi xuống DB qua `SaveChangesAsync`.
3. WHEN `UnitOfWork.SaveChangesAsync` hoàn tất thành công THEN UnitOfWork SHALL đã ghi tất cả thay đổi đang chờ trong một lần ghi.
4. IF một giao dịch chạy qua `ExecuteInTransactionAsync` gặp lỗi giữa chừng THEN UnitOfWork SHALL rollback sao cho KHÔNG thay đổi nào được ghi xuống DB, giữ nguyên trạng thái DB đúng như trước giao dịch, và truyền lỗi ra caller.
5. THE Repository và UnitOfWork SHALL nhận và truyền `CancellationToken` xuống tới tầng EF Core.
6. WHEN `SaveChangesAsync` gặp `DbUpdateConcurrencyException` THEN Platform SHALL trả `AppError` `concurrency_conflict` (HTTP 409) mà KHÔNG ghi đè dữ liệu, không ghi một phần, giữ nguyên dữ liệu hiện có.
7. WHEN một giao dịch chạy qua `ExecuteInTransactionAsync` hoàn tất không lỗi THEN UnitOfWork SHALL commit tất cả thay đổi trong giao dịch theo nguyên tắc tất-cả-hoặc-không.
8. IF `CancellationToken` bị huỷ trong khi thực hiện thao tác THEN UnitOfWork SHALL huỷ thao tác, KHÔNG commit thay đổi, và báo hiệu việc huỷ ra caller.

### Requirement 6: Cross-cutting ports & tính tất định

**User Story:** Là nhà phát triển, tôi muốn mọi phụ thuộc vào thời gian, ngẫu nhiên, sanitize, ngữ cảnh và realtime đi qua port trừu tượng, để mọi UseCase test được bằng unit test thuần.

#### Acceptance Criteria
1. THE UseCase SHALL lấy thời gian hiện tại qua `Clock` (`IDateTimeProvider`) và SHALL KHÔNG gọi trực tiếp `DateTime.Now` hoặc `DateTimeOffset.UtcNow` (kiểm chứng được bằng review mã nguồn hoặc phân tích tĩnh).
2. THE UseCase SHALL sinh mọi giá trị ngẫu nhiên (token, id ngẫu nhiên) qua `Token_Generator` và SHALL KHÔNG khởi tạo `System.Random` hay nguồn ngẫu nhiên ẩn nào khác bên trong UseCase.
3. WHEN một UseCase được chạy trong unit test THEN Platform SHALL cho phép thay thế toàn bộ các port `Clock`, `Token_Generator`, `Html_Sanitizer`, `Current_User`, `Guest_Context` và `Realtime_Notifier` bằng bản giả lập (fake/mock) thông qua cơ chế tiêm phụ thuộc, mà KHÔNG cần sửa mã của UseCase.
4. THE `Realtime_Notifier` SHALL cung cấp phương thức để UseCase gửi thông báo realtime tới nhóm staff qua interface trừu tượng, và UseCase SHALL KHÔNG tham chiếu trực tiếp tới SignalR Hub.
5. THE `Realtime_Notifier` SHALL cung cấp phương thức để UseCase gửi thông báo realtime tới một conversation cụ thể qua interface trừu tượng, và UseCase SHALL KHÔNG tham chiếu trực tiếp tới SignalR Hub.
6. WHEN nhận một URL hợp lệ THEN THE `Qr_Service` SHALL render ảnh QR định dạng PNG mã hoá đúng URL đó.
7. WHEN nhận một danh sách nhãn phòng có tối thiểu 1 phần tử THEN THE `Pdf_Service` SHALL render tài liệu PDF chứa nhãn cho từng phòng trong danh sách.
8. IF `Qr_Service` nhận URL rỗng/không hợp lệ HOẶC `Pdf_Service` nhận danh sách nhãn rỗng THEN THE service tương ứng SHALL trả về lỗi cho caller chỉ báo đầu vào không hợp lệ và SHALL KHÔNG tạo file kết quả.

### Requirement 7: Sinh PublicToken an toàn

**User Story:** Là chủ hệ thống, tôi muốn token QR không thể đoán được và không lộ số phòng, để chống brute-force và rò rỉ thông tin.

#### Acceptance Criteria
1. WHEN Token_Generator sinh một PublicToken THEN Token_Generator SHALL dùng nguồn ngẫu nhiên CSPRNG với tối thiểu 32 byte (256 bit) entropy.
2. THE Token_Generator SHALL trả PublicToken dạng chuỗi base64url (URL-safe, không padding) với độ dài tối thiểu 43 ký tự và chỉ gồm các ký tự trong tập base64url (`A–Z`, `a–z`, `0–9`, `-`, `_`).
3. THE PublicToken SHALL KHÔNG chứa số phòng dưới dạng substring hoặc thông tin nhận dạng phòng dạng trần, và SHALL KHÔNG cho phép suy ra phòng nếu không tra cứu database.
4. WHEN một PublicToken base64url được decode rồi encode lại THEN kết quả SHALL bằng chuỗi PublicToken ban đầu (round-trip encoding).
5. IF một PublicToken vừa sinh trùng với token đã tồn tại THEN Token_Generator SHALL sinh lại token (tối đa 5 lần); IF vẫn trùng sau 5 lần THEN Platform SHALL trả lỗi thay vì cấp token trùng.
6. IF nguồn CSPRNG không khả dụng THEN Token_Generator SHALL trả lỗi và SHALL KHÔNG cấp một PublicToken kém an toàn.

### Requirement 8: Nền data model & bất biến enforce ở DB

**User Story:** Là chủ hệ thống, tôi muốn các bất biến quan trọng được enforce ở tầng DB constraint, để dữ liệu luôn đúng kể cả khi bỏ qua tầng service.

#### Acceptance Criteria
1. THE DbContext SHALL định nghĩa các entity nền: `Resort`, `ResortSettings`, `ResortLanguage`, `AppUser`, `RefreshToken`, `Room`, `RoomQrToken`, `GuestSession`, `GuestVisit`.
2. IF một thao tác ghi làm tồn tại hai `RoomQrToken` có `Status = 'Active'` cho cùng một `Room` THEN THE DbContext SHALL từ chối lần ghi thứ hai qua partial unique index, ném lỗi vi phạm ràng buộc duy nhất cho caller, KHÔNG lưu bản ghi thứ hai, và giữ nguyên bản ghi `Active` hiện có.
3. IF một thao tác ghi làm tồn tại hai `ResortLanguage` có `IsDefault = true` cho cùng một resort THEN THE DbContext SHALL từ chối lần ghi thứ hai qua partial unique index, ném lỗi vi phạm ràng buộc duy nhất cho caller, KHÔNG lưu bản ghi thứ hai, và giữ nguyên bản ghi mặc định hiện có.
4. IF một thao tác ghi làm tồn tại hai `RulePublication` có `IsCurrent = true` cho cùng một resort THEN THE DbContext SHALL từ chối lần ghi thứ hai qua partial unique index, ném lỗi vi phạm ràng buộc duy nhất cho caller, KHÔNG lưu bản ghi thứ hai, và giữ nguyên publication `IsCurrent` hiện có.
5. IF một thao tác ghi làm tồn tại hai `Conversation` cho cùng một `GuestVisit` THEN THE DbContext SHALL từ chối lần ghi thứ hai qua unique index, ném lỗi vi phạm ràng buộc duy nhất cho caller, KHÔNG lưu bản ghi thứ hai, và giữ nguyên `Conversation` hiện có.
6. THE DbContext SHALL định nghĩa **migration nền (foundation)** chứa: các entity nền nêu ở tiêu chí 1 và tất cả partial/unique index liên quan tới các entity nền (tối thiểu các ràng buộc ở tiêu chí 2–3). THE các entity module nghiệp vụ (Rules, Faq, Messaging, Housekeeping, Notes) SHALL được bổ sung bằng **migration riêng theo từng wave** khi module đó được hiện thực, KHÔNG dựng bảng cho module chưa có code (tránh schema "chết").
7. THE mỗi migration SHALL là additive/reviewable độc lập và SHALL có khả năng áp dụng tuần tự lên một DB đã có dữ liệu mà không mất dữ liệu hiện có; các ràng buộc unique/partial index của một module (ví dụ `RulePublication.IsCurrent`, `Conversation.GuestVisitId` ở tiêu chí 4–5) SHALL được thêm trong chính migration của module đó.

### Requirement 9: Mẫu đa ngôn ngữ & fallback

**User Story:** Là khách quốc tế, tôi muốn nội dung luôn hiển thị bằng ngôn ngữ đang chọn hoặc fallback về mặc định, để không gặp nội dung trống.

#### Acceptance Criteria
1. THE Platform SHALL lưu nội dung đa ngôn ngữ theo mẫu entity gốc + bảng `*Translation`, với ràng buộc unique theo `(ParentId, LanguageCode)` sao cho mỗi cặp `(ParentId, LanguageCode)` có tối đa một bản dịch; `LanguageCode` SHALL là một mã ngôn ngữ đang được bật của resort.
2. WHEN Translation_Resolver resolve bản dịch của một mục cho một ngôn ngữ yêu cầu mà tồn tại bản dịch với nội dung không rỗng cho ngôn ngữ đó THEN Translation_Resolver SHALL trả bản dịch đúng ngôn ngữ đó với `IsFallback = false`.
3. IF ngôn ngữ yêu cầu không có bản dịch cho một mục HOẶC bản dịch tồn tại nhưng nội dung rỗng HOẶC ngôn ngữ yêu cầu không nằm trong danh sách ngôn ngữ được bật của resort, VÀ tồn tại bản dịch không rỗng của ngôn ngữ mặc định của resort THEN Translation_Resolver SHALL trả bản dịch của ngôn ngữ mặc định với `IsFallback = true`.
4. THE Translation_Resolver SHALL trả về một giá trị nội dung không rỗng cho một mục khi tồn tại tối thiểu bản dịch không rỗng của ngôn ngữ mặc định của resort.
5. WHEN Translation_Resolver resolve một tập gồm nhiều mục THEN Translation_Resolver SHALL áp dụng quy tắc chọn ngôn ngữ và fallback độc lập cho từng mục, và gắn cờ `IsFallback` riêng cho từng mục trong tập.
6. IF một mục KHÔNG có bản dịch với nội dung không rỗng cho cả ngôn ngữ yêu cầu lẫn ngôn ngữ mặc định của resort THEN Translation_Resolver SHALL đánh dấu mục đó là thiếu nội dung (missing) và tiếp tục resolve các mục còn lại của tập mà KHÔNG làm lỗi toàn bộ yêu cầu.

### Requirement 10: Resolve token, vòng đời GuestVisit & cửa sổ thao tác

**User Story:** Là khách, tôi muốn quét QR mở đúng trang phòng và dùng liền mạch trong một lượt lưu trú, nhưng cửa sổ thao tác hết hạn thì phải quét lại, để tránh sót dữ liệu giữa các lượt khách.

#### Acceptance Criteria
1. WHEN Guest_Access_Service nhận một `token` khớp đúng một `RoomQrToken` có `Status = 'Active'` THEN Guest_Access_Service SHALL phân giải ra `Room` và `Resort` tương ứng.
2. IF `token` không tồn tại THEN Guest_Access_Service SHALL trả `AppError` `qr_invalid`; IF `RoomQrToken` có `Status = 'Revoked'` THEN Guest_Access_Service SHALL trả `AppError` `qr_revoked`; IF `Room` không ở trạng thái `Active` THEN Guest_Access_Service SHALL trả `AppError` `room_inactive`; trong mọi trường hợp lỗi trên Guest_Access_Service SHALL KHÔNG tiết lộ thông tin phòng khác và SHALL KHÔNG tạo hoặc nối lại `GuestVisit`.
3. WHEN một thiết bị quét QR của một phòng mà chưa có `GuestVisit` `Active` cho cặp (session, phòng) THEN Guest_Access_Service SHALL tạo `GuestSession` (nếu thiết bị chưa có cookie định danh) và một `GuestVisit` mới trạng thái `Active` với `LastSeenAt = Clock.UtcNow` và `ExpiresAt = LastSeenAt + Portal_Window`.
4. WHEN cùng thiết bị quét lại QR của cùng phòng và `GuestVisit` vẫn `Active` THEN Guest_Access_Service SHALL nối lại đúng `GuestVisit` đó (không tạo mới, giữ nguyên hội thoại và acknowledgement) và cập nhật `LastSeenAt = Clock.UtcNow` cùng `ExpiresAt = LastSeenAt + Portal_Window`.
5. WHILE khoảng cách giữa `Clock.UtcNow` và `GuestVisit.LastSeenAt` vượt `Portal_Window` (mặc định 30 phút, cấu hình qua `PortalWindowMinutes` trong `ResortSettings`) THE Platform SHALL trả `AppError` `session_expired` cho mọi endpoint tương tác (FAQ, nhắn tin, tạo/cập nhật ticket, xác nhận nội quy) và SHALL KHÔNG cập nhật `LastSeenAt` hoặc `ExpiresAt`.
6. WHEN một endpoint tương tác được xử lý thành công trong hạn `Portal_Window` THEN Platform SHALL, SAU khi xử lý xong, cập nhật `LastSeenAt = Clock.UtcNow` và `ExpiresAt = LastSeenAt + Portal_Window`.
7. WHEN GuestVisit ở trạng thái `Active` mà khoảng cách giữa `Clock.UtcNow` và `GuestVisit.LastSeenAt` vượt `VisitIdleExpiryHours` (mặc định 24 giờ, cấu hình qua `ResortSettings`) THEN Platform SHALL chuyển `GuestVisit` sang trạng thái `Expired`; WHEN cùng thiết bị quét lại QR của phòng đó sau khi visit đã `Expired` THEN Guest_Access_Service SHALL tạo một `GuestVisit` mới (dữ liệu lượt trước không hiển thị cho lượt mới).
8. WHEN resolve thành công THE Guest_Access_Service SHALL trả về `ResolveResponse` chứa: thông tin `Room` (số phòng, toà nhà, tầng), thông tin `Resort`, danh sách ngôn ngữ được bật, trạng thái `GuestVisit` hiện tại (`Active`/`Expired`/`Closed`), và các cờ tính năng (`faqEnabled`, `chatEnabled`, `ruleAckRequiredForFaq`, `ruleAckRequiredForChat`, `ruleAckRequiredForHousekeeping`) lấy từ `ResortSettings`.

### Requirement 11: Xác thực kép & tách bạch bề mặt guest/admin

**User Story:** Là chủ hệ thống, tôi muốn tách bạch bề mặt admin (đăng nhập) và guest (không đăng nhập), để không rò rỉ dữ liệu hay đặc quyền giữa hai nhóm người dùng.

#### Acceptance Criteria
1. THE Auth_Layer SHALL xác thực Admin và Staff bằng JWT access token ngắn hạn (mặc định 15 phút, cấu hình trong khoảng 5–60 phút) kèm refresh token lưu dạng hash trong cookie HttpOnly (mặc định 30 ngày, cấu hình trong khoảng 7–90 ngày).
2. IF một request tới endpoint `/api/admin/*` không có JWT hợp lệ (thiếu, sai chữ ký, hoặc đã hết hạn) THEN Auth_Layer SHALL từ chối request (HTTP 401/403) và SHALL KHÔNG trả về dữ liệu nghiệp vụ.
3. WHEN một request tới endpoint `/api/guest/*` được nhận THEN Auth_Layer SHALL phát hoặc đọc cookie `GuestSession` (HttpOnly, Secure, SameSite=Lax, thời hạn 30–90 ngày định danh thiết bị) và nạp `Guest_Context` mà KHÔNG yêu cầu JWT.
4. THE cookie `GuestSession` SHALL KHÔNG chứa số phòng hoặc PublicToken.
5. THE Auth_Layer SHALL đăng ký `UseAuthentication` trước `UseAuthorization` với các policy `RequireAdmin` và `RequireStaff`.
6. THE Platform SHALL giới hạn quyền quản lý phòng/QR/người dùng/settings cho Role `Admin`, và quyền xử lý vận hành/nội dung cho Role `Staff`.
7. WHEN access token hết hạn và request kèm refresh token hợp lệ THEN Auth_Layer SHALL cấp access token mới (và refresh token mới) mà KHÔNG bắt đăng nhập lại.
8. IF refresh token hết hạn hoặc không hợp lệ THEN Auth_Layer SHALL từ chối, xoá cookie refresh token, và yêu cầu người dùng đăng nhập lại.

### Requirement 12: Pipeline validation & sanitize HTML

**User Story:** Là chủ hệ thống, tôi muốn input xấu bị chặn sớm và nội dung rich text luôn được làm sạch, để chống input không hợp lệ và XSS.

#### Acceptance Criteria
1. WHEN một UseCase có validator được gọi THEN Validation_Pipeline SHALL chạy validator trước phần thân UseCase.
2. IF input không hợp lệ THEN Validation_Pipeline SHALL trả ProblemDetails với `code = validation_error` kèm danh sách field lỗi, và SHALL KHÔNG thực thi phần thân UseCase (không thay đổi trạng thái dữ liệu).
3. WHEN nội dung rich text (nội quy/FAQ) được ghi xuống DB THEN Platform SHALL làm sạch nội dung qua `Html_Sanitizer` theo allowlist trước khi lưu.
4. THE nội dung rich text được lưu SHALL KHÔNG chứa thẻ `<script>`, thuộc tính sự kiện (`on*`), hoặc URI `javascript:` sau khi qua `Html_Sanitizer`.
5. WHEN nội dung rich text được trả về cho client THEN Platform SHALL đảm bảo nội dung đã được sanitize (không chứa phần tử thực thi được).
6. IF `Html_Sanitizer` nhận nội dung rỗng hoặc null THEN Platform SHALL lưu giá trị rỗng chuẩn hoá mà KHÔNG ném lỗi.

### Requirement 13: Rate limiting cho guest surface

**User Story:** Là chủ hệ thống, tôi muốn giới hạn tần suất các thao tác guest nhạy cảm, để chống brute-force token và spam.

#### Acceptance Criteria
1. THE Rate_Limiter SHALL áp policy `resolve` phân vùng theo IP cho endpoint `GET /api/guest/resolve/{token}`, với ngưỡng mặc định 20 request trong cửa sổ trượt 60 giây cho mỗi IP khi không có cấu hình khác.
2. THE Rate_Limiter SHALL áp policy `guest-write` phân vùng theo `GuestSessionId` cho các endpoint ghi của guest (gửi tin nhắn, tạo ticket), với ngưỡng mặc định 10 request trong cửa sổ trượt 60 giây cho mỗi `GuestSessionId` khi không có cấu hình khác.
3. IF một request tới endpoint ghi của guest không mang `GuestSessionId` hợp lệ THEN Rate_Limiter SHALL phân vùng request đó theo IP và áp cùng ngưỡng của policy `guest-write`.
4. WHEN số request trong cửa sổ thời gian của một policy vượt ngưỡng cấu hình THEN Rate_Limiter SHALL từ chối request bằng `AppError` `rate_limited` (HTTP 429) kèm chỉ dẫn số giây khách phải chờ trước khi thử lại, và SHALL KHÔNG xử lý request bị từ chối (không phân giải token, không tạo tin nhắn/ticket).
5. THE Rate_Limiter SHALL đọc ngưỡng và cửa sổ thời gian của mỗi policy theo thứ tự ưu tiên `ResortSettings` trước rồi `appsettings` sau, và SHALL dùng giá trị mặc định (resolve: 20 request/60 giây theo IP; guest-write: 10 request/60 giây theo `GuestSessionId`) khi cả hai nguồn đều không cấu hình.

### Requirement 14: Logging có cấu trúc, che bí mật & health check

**User Story:** Là dev vận hành, tôi muốn đủ log có cấu trúc và health check để chẩn đoán sự cố, mà không lộ token hay mật khẩu.

#### Acceptance Criteria
1. THE Logger SHALL ghi log dạng JSON có cấu trúc cho mỗi request, enrich `RequestId`, `ResortId`, `ConversationId` (khi có) và `error code`; WHERE token được resolve thành công THE Logger SHALL enrich thêm `RoomId`.
2. THE Logger SHALL KHÔNG ghi PublicToken đầy đủ, mật khẩu, hoặc refresh token vào log; WHERE cần ghi các giá trị này để đối soát THE Logger SHALL chỉ ghi dạng đã che (masked), tuyệt đối không ghi giá trị đầy đủ.
3. WHEN nhận request tới `/health/live` THE Platform SHALL trả về trạng thái sức khoẻ tiến trình trong vòng 2 giây.
4. THE `VisitIdleSweeper` SHALL chạy định kỳ theo chu kỳ cấu hình được (mặc định mỗi 5 phút); WHEN tới chu kỳ quét, với mỗi `GuestVisit` có thời điểm hiện tại vượt `ExpiresAt`, THE service SHALL chuyển visit sang `Expired`, đóng các conversation đang Open và huỷ các ticket đang mở do visit đó tạo.
5. THE `VisitIdleSweeper` SHALL KHÔNG thay đổi `RoomQrToken`.
6. WHEN nhận request tới `/health/ready` và thiết lập được kết nối DB thành công trong vòng 5 giây THE Platform SHALL trả về trạng thái sẵn sàng (ready).
7. IF không thiết lập được kết nối DB trong vòng 5 giây THEN THE `/health/ready` endpoint SHALL trả về trạng thái không sẵn sàng kèm chỉ báo lỗi, và SHALL KHÔNG tiết lộ chuỗi kết nối hay bí mật.
8. IF việc xử lý một `GuestVisit` trong một lượt quét thất bại THEN THE `VisitIdleSweeper` SHALL bỏ qua visit đó, tiếp tục xử lý các visit còn lại trong lượt, và giữ nguyên trạng thái của visit lỗi để lượt quét kế tiếp thử lại.

### Requirement 15: Seed data & cấu hình vận hành (ResortSettings)

**User Story:** Là admin, tôi muốn hệ thống có dữ liệu khởi tạo an toàn và các thông số vận hành chỉnh được ở một nơi, để không phải sửa code.

#### Acceptance Criteria
1. WHEN Seeder chạy trên một DB trống THEN Seeder SHALL tạo đúng một `Resort` (Star Hill), một `ResortSettings` mặc định, các `ResortLanguage` (`en` default, `vi`, `ko`, `zh`), và một tài khoản `Admin` với mật khẩu khởi tạo lấy từ `appsettings`/biến môi trường (KHÔNG hardcode trong mã).
2. WHEN Seeder chạy lại trên DB đã có dữ liệu THEN Seeder SHALL KHÔNG tạo bản ghi trùng lặp, nhận diện trùng theo khoá: `Resort` theo code/tên, `ResortLanguage` theo mã ngôn ngữ, `Admin` theo username/email (idempotent).
3. THE `ResortSettings` SHALL chứa tối thiểu: cờ bật/tắt ack trước FAQ/chat/housekeeping (mặc định bật), `PortalWindowMinutes` (mặc định 30, khoảng hợp lệ 1–1440), `VisitIdleExpiryHours` (mặc định 24, khoảng hợp lệ 1–168), `GuestWebBaseUrl` (một URL https hợp lệ), `MaxMessageLength` (mặc định 2000, khoảng 1–10000), và ngưỡng rate limit gửi tin (mặc định 10 request/60 giây) và tạo ticket (mặc định 5 request/60 giây).
4. WHEN Platform sinh QR THEN Platform SHALL dựng URL từ `GuestWebBaseUrl` trong `ResortSettings` mà KHÔNG hardcode host.
5. IF một giá trị trong `ResortSettings` không được đặt (null/rỗng) THEN Platform SHALL dùng giá trị mặc định an toàn đọc từ `appsettings` hoặc hằng số, và tính năng liên quan SHALL KHÔNG bị lỗi.
6. IF `GuestWebBaseUrl` thiếu hoặc không phải URL https hợp lệ khi sinh QR THEN Platform SHALL từ chối thao tác sinh QR, giữ nguyên trạng thái, và trả lỗi chỉ báo cấu hình không hợp lệ.

### Requirement 16: Nền quản lý Room & QR token

**User Story:** Là admin, tôi muốn tạo phòng, sinh và thu hồi QR (kèm in hàng loạt), với đảm bảo mỗi phòng chỉ có một token active.

#### Acceptance Criteria
1. THE Platform SHALL cho phép Admin tạo/sửa/vô hiệu/soft-delete `Room` với thuộc tính: số phòng (1–20 ký tự, duy nhất trong một resort), toà nhà (tối đa 50 ký tự), tầng (khoảng -10 đến 200), và trạng thái (`Active`/`Inactive`/`Maintenance`).
2. WHEN Admin yêu cầu sinh QR cho một `Room` ở trạng thái `Active` THEN Qr_Service SHALL tạo ảnh PNG chứa URL `https://<host>/r/{token}` trong vòng 5 giây.
3. THE Pdf_Service SHALL xuất PDF chứa QR của nhiều phòng (tối đa 500 phòng mỗi lần) kèm số phòng và logo trong vòng 30 giây để in hàng loạt.
4. WHEN Admin thu hồi QR của một `Room` THEN Platform SHALL đặt token cũ sang `Status = 'Revoked'`, sinh `PublicToken` mới ngẫu nhiên/duy nhất/không đoán được ở `Status = 'Active'`, và giữ lại lịch sử token (không xoá cứng).
5. THE Platform SHALL đảm bảo mỗi `Room` có tối đa một `RoomQrToken` ở `Status = 'Active'` kể cả khi có thao tác đồng thời.
6. IF số phòng trùng trong cùng resort hoặc không hợp lệ (rỗng/vượt 20 ký tự) THEN Platform SHALL từ chối tạo/sửa `Room`, giữ nguyên trạng thái, và trả lỗi chỉ báo.
7. IF việc sinh QR thất bại HOẶC `Room` không ở trạng thái `Active` THEN Platform SHALL KHÔNG tạo file QR và trả lỗi tương ứng.
8. IF số phòng yêu cầu xuất PDF vượt 500 THEN Pdf_Service SHALL từ chối yêu cầu và trả lỗi chỉ báo vượt giới hạn.

### Requirement 17: Nền Frontend (2 SPA + shared packages)

**User Story:** Là nhà phát triển frontend, tôi muốn một monorepo hai SPA với các package dùng chung và xử lý lỗi/i18n nền, để mọi màn nghiệp vụ sau xây trên đó.

#### Acceptance Criteria
1. THE Frontend_Base SHALL gồm hai app riêng (`guest-web` mobile-first mặc định tiếng Anh, `admin-web` tiếng Việt có đăng nhập) và các package dùng chung (`api-client`, `shared-types`, `realtime`, `ui-kit`).
2. IF Api_Client nhận response HTTP với status >= 400 THEN Api_Client SHALL parse ProblemDetails và ném `ApiError` chứa `code`, `status`, và `message`, và SHALL KHÔNG trả về dữ liệu response cho caller.
3. IF body lỗi không phải ProblemDetails hợp lệ THEN Api_Client SHALL ném `ApiError` với `code` mặc định (unexpected) kèm `status`, mà KHÔNG crash.
4. WHEN Api_Client gọi backend THEN Api_Client SHALL gửi cookie qua `credentials: 'include'`.
5. WHEN guest-web khởi tạo ngôn ngữ THEN Frontend_Base SHALL chọn theo thứ tự: `?lang=`, localStorage, `navigator.language` (chuẩn hoá dạng `ko-KR`→`ko`), rồi ngôn ngữ mặc định của resort (`en`); IF ngôn ngữ chọn được không thuộc danh sách ngôn ngữ được bật của resort THEN Frontend_Base SHALL fallback về `en`.
6. THE `realtime` package SHALL bọc SignalR với tự reconnect tối đa 5 lần theo backoff tăng dần từ 1 đến 30 giây.
7. IF WebSocket lỗi hoặc reconnect thất bại THEN `realtime` package SHALL chuyển sang fallback polling mỗi 10 giây.
8. IF người dùng chưa đăng nhập truy cập route được bảo vệ THEN admin-web SHALL redirect tới trang đăng nhập.
9. IF người dùng đã đăng nhập nhưng không thoả policy `RequireAdmin`/`RequireStaff` THEN admin-web SHALL chặn truy cập và thông báo không đủ quyền.

### Requirement 18: Extension points cho module nghiệp vụ

**User Story:** Là nhà phát triển, tôi muốn thêm một module nghiệp vụ bằng cách điền logic vào khung có sẵn, để không phải sửa cấu trúc nền, DI hay migration nền.

#### Acceptance Criteria
1. THE Platform SHALL cung cấp **khung thư mục module + interface UseCase (chưa hiện thực) + khung controller** làm extension point cho từng module nghiệp vụ (Rules, Faq, Messaging, Housekeeping, Notes) ngay ở nền; THE entity và migration của một module SHALL được bổ sung khi module đó được hiện thực (migration per-wave, xem Requirement 8.6–8.7), KHÔNG dựng bảng cho module chưa có code.
2. THE khung extension point của mỗi module SHALL đủ để wave sau chỉ cần thêm entity + migration + điền logic mà KHÔNG phải sửa cấu trúc 5 project, cơ chế DI, hay migration nền đã có.
3. WHERE một module A cần gọi module B THE module A SHALL gọi qua interface UseCase/port của module B đã đăng ký DI, và SHALL KHÔNG đọc trực tiếp entity nội bộ của module B.
4. WHEN một module nghiệp vụ được hiện thực về sau THEN nhà phát triển SHALL điền logic vào khung có sẵn mà KHÔNG cần sửa cấu trúc nền, DI hoặc migration nền.
5. IF một module A tham chiếu trực tiếp entity nội bộ của module B (vi phạm ranh giới) THEN Architecture_Test SHALL fail và chỉ rõ vi phạm.
6. IF migration nền không áp dụng được (schema lỗi) THEN Platform SHALL dừng khởi động và báo lỗi migration rõ ràng.

### Requirement 19: Kiểm thử & enforce kiến trúc

**User Story:** Là nhà phát triển, tôi muốn nền có đủ tầng test (unit, property-based, integration, architecture) để bảo vệ các bất biến nền.

#### Acceptance Criteria
1. THE Platform SHALL cho phép test mọi UseCase bằng unit test thuần dùng port giả lập (in-memory/fake), KHÔNG gọi HTTP thật, KHÔNG kết nối DB thật, và KHÔNG truy cập mạng ngoài.
2. THE Platform SHALL có property-based test cho: sinh token duy nhất trong tập sinh, fallback ngôn ngữ luôn trả giá trị khác rỗng, cửa sổ thao tác luôn có thời điểm hết hạn hữu hạn, và các bất biến đơn điệu (không đảo chiều).
3. THE property-based test SHALL chạy tối thiểu 100 iteration cho mỗi property.
4. IF một property-based test phát hiện counterexample THEN test SHALL fail, ghi lại seed để tái lập, và SHALL KHÔNG kết thúc thành công.
5. THE Platform SHALL có integration test (Testcontainers PostgreSQL) xác minh các partial/unique index thực sự chặn ghi vi phạm bằng lỗi ràng buộc và KHÔNG lưu bản ghi vi phạm.
6. THE Architecture_Test SHALL enforce dependency rule và SHALL fail khi có vi phạm.
7. THE Architecture_Test SHALL enforce naming convention và SHALL fail khi có vi phạm.
8. WHEN Architecture_Test fail THEN Architecture_Test SHALL liệt kê từng vi phạm cụ thể.

### Requirement 20: CORS, security headers & HTTPS

**User Story:** Là chủ hệ thống, tôi muốn giới hạn origin được phép, gắn security header và bắt buộc HTTPS, để giảm bề mặt tấn công và bảo đảm secure context cho camera quét QR trên thiết bị khách.

#### Acceptance Criteria
1. THE Security_Layer SHALL cấu hình CORS chỉ cho phép các origin được khai báo tường minh trong cấu hình (guest, admin), SHALL KHÔNG dùng wildcard `*` cho origin, SHALL chỉ cho phép các HTTP method cần thiết (`GET`, `POST`, `PUT`, `PATCH`, `DELETE`, `OPTIONS`), và SHALL cho phép gửi credentials (cookie) chỉ với các origin đã khai báo.
2. IF một request cross-origin đến từ origin KHÔNG nằm trong danh sách origin được cấu hình cho phép THEN THE Security_Layer SHALL từ chối request cross-origin đó và SHALL KHÔNG đặt header `Access-Control-Allow-Origin` cho origin đó.
3. WHEN THE Security_Layer nhận một preflight request (`OPTIONS`) từ một origin nằm trong danh sách được cấu hình cho phép THEN THE Security_Layer SHALL trả response preflight với các header CORS cho phép (`Access-Control-Allow-Origin`, `Access-Control-Allow-Methods`, `Access-Control-Allow-Headers`, `Access-Control-Allow-Credentials`) mà KHÔNG chuyển request tới UseCase.
4. WHEN Platform trả một HTTP response THEN THE Security_Layer SHALL gắn các security header: `Content-Security-Policy` với `default-src 'self'`, `frame-ancestors 'none'` và `connect-src 'self' wss:`, cùng `X-Content-Type-Options: nosniff`.
5. WHERE HTTPS đã sẵn sàng với certificate hợp lệ THE Security_Layer SHALL gắn header HSTS (`Strict-Transport-Security`) vào response với `max-age` tối thiểu 31.536.000 giây (365 ngày) và chỉ thị `includeSubDomains`.
6. WHEN một request tới Platform qua HTTP không mã hoá THEN THE Security_Layer SHALL chuyển hướng vĩnh viễn (permanent redirect) request đó sang HTTPS giữ nguyên path và query string, và SHALL KHÔNG phục vụ nội dung ứng dụng qua HTTP không mã hoá, để bảo đảm secure context.
7. THE Security_Layer SHALL đăng ký CORS và security header trong pipeline sao cho áp dụng cho cả bề mặt guest (`/api/guest/*`, `/r/*`), bề mặt admin (`/api/admin/*`) và SignalR Hub (`/hubs/*`).
