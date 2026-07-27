# Requirements Document

## Introduction

Gói hardening "military-grade" cho sản phẩm **StarHill QR** (`starhill/`), bổ sung 7 nhóm cổng kiểm chứng còn thiếu so với baseline hiện tại: health probe cấp container, fuzz biên HTTP, chuỗi cung ứng build (SBOM + scan + pin digest), phân tầng CI (PR nhanh / nightly nặng), chaos hạ tầng fail-closed, cổng SLO độ trễ p99, soak độ bền chạy dài, và quan sát được tối thiểu cho vận hành.

Phạm vi vận hành mục tiêu: **một resort ~60 phòng**, một instance Host + một PostgreSQL, RabbitMQ là opt-in qua overlay. Mọi ngưỡng trong tài liệu này được chốt theo quy mô đó, không theo quy mô multi-tenant.

Tài liệu này **chỉ đặc tả cổng kiểm chứng và hành vi quan sát được**, không thay đổi nghiệp vụ của 8 module QR đã hoàn thành.

Ba ràng buộc nền đã kiểm chứng trong phiên audit và được dùng làm sự thật nền:

- `Dockerfile` của Host (`starhill/src/Host/StarHill.Api/Dockerfile`) **không có** chỉ thị `HEALTHCHECK`; stage runtime dùng tag `mcr.microsoft.com/dotnet/aspnet:10.0`, chạy `USER $APP_UID`.
- `starhill/docker-compose.yml` chỉ khai báo `healthcheck` cho service `postgres`; service `host` **không có** `healthcheck`.
- `.github/workflows/starhill-ci.yml` có 4 job (`build-test`, `frontend`, `docker-image`, `migration-bundle`), chạy trên `push` (main/master/develop) và `pull_request`; **không có** job theo lịch (nightly).

## Glossary

- **Host**: tiến trình ASP.NET Core `StarHill.Api` (`starhill/src/Host/StarHill.Api`), phơi `/health/live` và `/health/ready` do Bedrock cung cấp.
- **Runtime_Image**: image Docker sinh từ stage `runtime` của `starhill/src/Host/StarHill.Api/Dockerfile`.
- **Health_Probe**: chỉ thị `HEALTHCHECK` trong `Runtime_Image` cùng lệnh mà chỉ thị đó thực thi bên trong container.
- **Compose_Stack**: tập hợp `starhill/docker-compose.yml` và các overlay `docker-compose.messaging.yml`, `docker-compose.dev.yml`.
- **Fuzz_Harness**: bộ test sinh dữ liệu vào ngẫu nhiên bắn vào biên HTTP của `Host` qua `WebApplicationFactory` (in-memory, không cần Docker).
- **Chaos_Harness**: bộ test tích hợp dùng Testcontainers, có khả năng dừng/khởi động lại container PostgreSQL và RabbitMQ giữa vòng đời một kịch bản.
- **Latency_Harness**: bộ đo độ trễ phát tải tuần tự/song song vào `Host` và tính phân vị p50/p99 từ mẫu đã ghi.
- **Soak_Harness**: bộ chạy tải kéo dài vào `Host` và lấy mẫu định kỳ các chỉ số bộ nhớ, handle, kết nối, tỉ lệ lỗi, tồn đọng outbox.
- **Supply_Chain_Gate**: tập bước CI sinh SBOM, quét lỗ hổng phụ thuộc .NET và npm, và kiểm tra ghim digest image base.
- **SBOM**: tệp Software Bill of Materials định dạng CycloneDX JSON.
- **Observability_Pipeline**: cấu hình xuất trace và metric của `Host` qua OTLP, cùng cấu hình structured log.
- **Guard_Test_Suite**: các test trong `starhill/tests/**` chạy bởi `dotnet test Platform.slnx`, gồm `StarHill.ArchitectureTests`.
- **CI_PR**: workflow `.github/workflows/starhill-ci.yml`, kích hoạt bởi `push` nhánh tích hợp và `pull_request`.
- **CI_Nightly**: workflow GitHub Actions mới chạy theo lịch (`schedule`) và cho phép kích hoạt tay (`workflow_dispatch`).
- **Local_Docker**: máy phát triển Windows có Docker Desktop đang chạy.
- **Journal_QR**: các tệp `.kiro/specs/starhill-qr/journal/01-decisions.md`, `02-deviations.md`, `03-tradeoffs.md`, `04-notes.md`, `05-anti-drift.md`.
- **INV_6**: bất biến trong `StarHillJournalConsistencyTests` yêu cầu mọi mục `QR-AD` ở trạng thái `Implemented` phải khai `- Guard-Tests:` trỏ tới test class tồn tại thật trong `starhill/tests/**/*.cs`.
- **Guest_Endpoints**: tập endpoint khách, tất cả khai `AllowAnonymous` và định danh phiên bằng cookie thiết bị `__Host-starhill_guest` (không dùng JWT). Route thật đã xác minh trong `starhill/src/Modules/**/*EndpointModule.cs`:
  - `POST /v1/guest/resolve` (`GuestAccessEndpointModule`; có `RequestSizeLimitAttribute` giới hạn kích thước body)
  - `GET /v1/guest/rules` (`RulesGuestEndpointModule`)
  - `POST /v1/guest/rules/acknowledge` (`RulesGuestEndpointModule`)
  - `GET /v1/guest/faq` (`FaqGuestEndpointModule`)
  - `POST /v1/guest/messages` (`ConciergeGuestEndpointModule`)
  - `GET /v1/guest/conversation` (`ConciergeGuestEndpointModule`)
  - `POST /v1/guest/housekeeping` (`HousekeepingGuestEndpointModule`)
  - `GET /v1/guest/housekeeping` (`HousekeepingGuestEndpointModule`)
- **Admin_Endpoints**: tập endpoint quản trị, dùng JWT với policy `RequireAdmin` hoặc `RequireStaff`, trừ hai endpoint cấp token khai `AllowAnonymous`. Route thật đã xác minh trong `starhill/src/Modules/**/*EndpointModule.cs`:
  - `POST /v1/identity/token/login` (`IdentityEndpointModule`; `AllowAnonymous` vì chính endpoint này cấp token)
  - `POST /v1/identity/token/refresh` (`IdentityEndpointModule`; `AllowAnonymous`)
  - `POST /v1/rooms`, `PUT /v1/rooms/{roomId}`, `DELETE /v1/rooms/{roomId}`, `POST /v1/rooms/{roomId}/rotate-token` (`RoomsEndpointModule`; `RequireAdmin`)
  - `POST /v1/rules/sections`, `PUT /v1/rules/sections/{sectionId}`, `PUT /v1/rules/sections/{sectionId}/translations/{lang}`, `POST /v1/rules/publish` (`RulesAdminEndpointModule`; `RequireStaff`)
  - `POST /v1/faq/categories`, `POST /v1/faq/items`, `PUT /v1/faq/items/{itemId}`, `POST /v1/faq/reorder/categories` (`FaqAdminEndpointModule`; `RequireStaff`)
  - `GET /v1/conversations`, `GET /v1/conversations/{conversationId}`, `POST /v1/conversations/{conversationId}/reply`, `POST /v1/notes` (`ConciergeAdminEndpointModule`; `RequireStaff`)
  - `GET /v1/housekeeping`, `POST /v1/housekeeping/{ticketId}/status`, `POST /v1/housekeeping/complete-by-token` (`HousekeepingAdminEndpointModule`; `RequireStaff`)
  - `GET /v1/resort/settings`, `PUT /v1/resort/settings` (`ResortConfigEndpointModule`; `RequireAdmin`)
- **Phân biệt hai đường đọc hội thoại**: `GET /v1/conversations` là endpoint **quản trị** (`RequireStaff`, trả bảng hội thoại cho lễ tân) và thuộc Admin_Endpoints; endpoint để **khách** đọc hội thoại của chính mình là `GET /v1/guest/conversation` (`AllowAnonymous`, thuộc Guest_Endpoints). Hai route này không thay thế được cho nhau trong bất kỳ bài đo nào của tài liệu này.
- **Guest_Poll_Endpoints**: hai endpoint mà frontend khách gọi theo chu kỳ: `GET /v1/guest/conversation` với chu kỳ ~4 giây (QR-N-085) và `GET /v1/guest/housekeeping` với chu kỳ ~5 giây (QR-N-086).

## Requirements

Thứ tự requirement dưới đây **là thứ tự ưu tiên triển khai đã chốt**: health probe → fuzz → chuỗi cung ứng → phân tầng CI → chaos → SLO → soak → quan sát được.

### Requirement 1: Health probe cấp container

**User Story:** Là người vận hành resort, tôi muốn container Host tự báo trạng thái sức khỏe cho orchestrator, để tiến trình treo hoặc mất phụ thuộc được phát hiện và thay thế mà không cần ai ngồi nhìn log.

#### Acceptance Criteria

1. THE Runtime_Image SHALL khai báo một chỉ thị `HEALTHCHECK` với `--interval=10s`, `--timeout=3s`, `--retries=3`, `--start-period=20s`.
2. WHEN Health_Probe thực thi và `Host` trả HTTP 200 tại `/health/live` trong vòng 3 giây, THE Health_Probe SHALL kết thúc với mã thoát 0.
3. WHEN Health_Probe thực thi và `Host` không trả HTTP 200 tại `/health/live` trong vòng 3 giây, THE Health_Probe SHALL kết thúc với mã thoát 1.
4. WHEN `Host` ngừng trả HTTP 200 tại `/health/live`, THE Runtime_Image SHALL đạt trạng thái Docker `unhealthy` trong vòng 40 giây kể từ lần Health_Probe thất bại đầu tiên.
5. THE Runtime_Image SHALL chứa đúng 0 lệnh cài đặt package hệ điều hành (`apt-get install`, `apk add`, `yum install`) trong stage runtime.
6. IF `Host` không mở được kết nối tới PostgreSQL, THEN THE Host SHALL trả HTTP 503 tại `/health/ready` kèm body `application/json` liệt kê tên check thất bại, ngay tại lần kiểm tra readiness đầu tiên phát hiện lỗi kết nối, không áp dụng khoảng ân hạn.
7. WHERE overlay `docker-compose.messaging.yml` được bật, IF `Host` không mở được kết nối tới RabbitMQ, THEN THE Host SHALL trả HTTP 503 tại `/health/ready` ngay tại lần kiểm tra readiness đầu tiên phát hiện lỗi kết nối.
8. THE Host SHALL trả HTTP 200 tại `/health/live` khi tiến trình còn phục vụ được yêu cầu HTTP, độc lập với trạng thái PostgreSQL và RabbitMQ; `/health/live` SHALL không kiểm tra tình trạng khóa chết (deadlock) của logic nghiệp vụ.
9. THE Compose_Stack SHALL khai báo `healthcheck` cho service `host` với cùng bộ tham số ở tiêu chí 1.
10. WHEN Guard_Test_Suite chạy trên CI_PR, THE Guard_Test_Suite SHALL fail nếu `Dockerfile` của Host thiếu chỉ thị `HEALTHCHECK`, hoặc `docker-compose.yml` thiếu `healthcheck` cho service `host`, hoặc stage runtime chứa lệnh cài package hệ điều hành.
11. WHEN Chaos_Harness dừng container PostgreSQL trên Local_Docker hoặc CI_Nightly, THE Host SHALL trả HTTP 503 tại `/health/ready` trong vòng 15 giây kể từ lúc container PostgreSQL dừng.

### Requirement 2: Fuzz biên HTTP với bất biến "không 5xx, không rò"

**User Story:** Là chủ sản phẩm, tôi muốn mọi dữ liệu vào rác ở biên HTTP đều bị từ chối bằng lỗi 4xx có cấu trúc, để một khách quét QR gửi payload méo không thể tạo lỗi 500 hay lộ nội bộ hệ thống.

#### Acceptance Criteria

1. THE Fuzz_Harness SHALL sinh và gửi tối thiểu 500 dữ liệu vào cho mỗi endpoint thuộc Guest_Endpoints và mỗi endpoint thuộc Admin_Endpoints, dùng đúng các route thật khai trong Glossary, trong đó có `POST /v1/identity/token/login`, `POST /v1/guest/housekeeping`, và `GET /v1/guest/conversation`.
2. WHERE một endpoint nhận body JSON (phương thức `POST` hoặc `PUT`), THE Fuzz_Harness SHALL bao phủ tối thiểu 6 lớp biến dạng cho endpoint đó: JSON sai cú pháp, kiểu dữ liệu sai, thiếu trường bắt buộc, trường phụ không khai báo, chuỗi vượt giới hạn độ dài, và body vượt giới hạn 1 KiB.
3. WHERE một endpoint không nhận body (phương thức `GET`), THE Fuzz_Harness SHALL bao phủ tối thiểu 4 lớp biến dạng cho endpoint đó: tham số truy vấn sai kiểu, tham số truy vấn vượt giới hạn độ dài, tham số đường dẫn `{...}` sai định dạng, và giá trị cookie `__Host-starhill_guest` sai định dạng.
4. WHEN Fuzz_Harness gửi một dữ liệu vào biến dạng tới một endpoint, THE Host SHALL trả mã trạng thái HTTP trong khoảng 400–499.
5. WHEN Host trả mã trạng thái trong khoảng 400–499 cho một dữ liệu vào biến dạng, THE Host SHALL đặt header `Content-Type` bắt đầu bằng `application/problem+json`.
6. IF `Host` trả mã trạng thái trong khoảng 500–599 cho bất kỳ dữ liệu vào nào do Fuzz_Harness sinh ra, THEN THE Fuzz_Harness SHALL fail và in ra mã trạng thái, tên endpoint, và dữ liệu vào gây lỗi.
7. THE Fuzz_Harness SHALL khẳng định body phản hồi không chứa các chuỗi `Exception`, `StackTrace`, `at Bedrock.`, `at StarHill.`, `Password=`, `Username=`, và giá trị khóa ký JWT đang cấu hình.
8. WHEN Fuzz_Harness fail, THE Fuzz_Harness SHALL in ra hạt giống (seed) sinh dữ liệu để lặp lại đúng ca lỗi.
9. THE CI_PR SHALL chạy Fuzz_Harness trong job `build-test` bằng `WebApplicationFactory` in-memory, không phụ thuộc Docker.
10. THE Fuzz_Harness SHALL hoàn tất trong vòng 180 giây trên runner `ubuntu-latest`.
11. WHEN Fuzz_Harness gửi dữ liệu vào tới một endpoint thuộc Admin_Endpoints có khai `RequireStaff` hoặc `RequireAdmin`, THE Fuzz_Harness SHALL đính kèm header `Authorization: Bearer` chứa một JWT hợp lệ có claim `role` giá trị `staff` cho endpoint `RequireStaff` và giá trị `admin` cho endpoint `RequireAdmin`, để phản hồi phản ánh kết quả kiểm tra dữ liệu vào thay vì kết quả kiểm tra xác thực.
12. IF `Host` trả mã trạng thái 401 cho một dữ liệu vào do Fuzz_Harness gửi tới một endpoint thuộc Admin_Endpoints có khai `RequireStaff` hoặc `RequireAdmin`, THEN THE Fuzz_Harness SHALL fail và in ra tên endpoint kèm giá trị claim `role` của token đã dùng.
13. WHEN Fuzz_Harness gửi dữ liệu vào tới một endpoint thuộc Guest_Endpoints khác `POST /v1/guest/resolve`, THE Fuzz_Harness SHALL đính kèm cookie thiết bị `__Host-starhill_guest` lấy từ một lần gọi `POST /v1/guest/resolve` thành công trước đó, trừ lớp biến dạng cookie sai định dạng ở tiêu chí 3.

### Requirement 3: Chuỗi cung ứng và build tái lập

**User Story:** Là người chịu trách nhiệm bảo mật, tôi muốn biết chính xác artifact xuất xưởng gồm những thành phần nào và không chứa lỗ hổng nghiêm trọng đã biết, để có thể trả lời câu hỏi kiểm toán bằng bằng chứng thay vì bằng lời.

#### Acceptance Criteria

1. WHEN CI_PR chạy, THE Supply_Chain_Gate SHALL sinh một SBOM CycloneDX JSON cho `starhill/Platform.slnx` và một SBOM CycloneDX JSON cho workspace `starhill/web`.
2. WHEN Supply_Chain_Gate sinh SBOM, THE CI_PR SHALL nạp cả hai tệp SBOM lên artifact của lần chạy đó.
3. WHEN báo cáo quét của Supply_Chain_Gate chứa tối thiểu một mục có mức severity `High` hoặc `Critical`, THE CI_PR SHALL fail job chứa Supply_Chain_Gate, căn cứ trên mức severity do bộ quét công bố và không phụ thuộc đánh giá khả năng khai thác thực tế.
4. WHEN Supply_Chain_Gate quét phụ thuộc và mức cao nhất phát hiện được là `Medium` hoặc thấp hơn, THE CI_PR SHALL ghi danh sách lỗ hổng vào log job và kết thúc job với trạng thái thành công.
5. THE Dockerfile của Host SHALL ghim cả hai image base (`sdk:10.0` và `aspnet:10.0`) bằng digest `sha256:` tường minh.
6. WHEN Guard_Test_Suite chạy trên CI_PR, THE Guard_Test_Suite SHALL fail nếu bất kỳ chỉ thị `FROM` trong `Dockerfile` của Host thiếu phần `@sha256:`.
7. WHEN Guard_Test_Suite fail vì thiếu digest, THE CI_PR SHALL fail job chứa Guard_Test_Suite và SHALL cho các job không khai `needs` tới job đó tiếp tục chạy tới khi kết thúc.
8. THE Supply_Chain_Gate SHALL khai báo một tệp danh sách miễn trừ tường minh, mỗi dòng gồm định danh lỗ hổng, lý do, và ngày hết hiệu lực.
9. IF một dòng miễn trừ có ngày hết hiệu lực trước ngày chạy, THEN THE Supply_Chain_Gate SHALL fail job chứa Supply_Chain_Gate.
10. THE Supply_Chain_Gate SHALL hoàn tất trong vòng 10 phút trên runner `ubuntu-latest`.

### Requirement 4: Phân tầng CI — PR nhanh, nightly nặng

**User Story:** Là lập trình viên, tôi muốn cổng trên PR vẫn nhanh sau khi thêm hardening, để các bài kiểm chứng nặng không biến mỗi lần push thành 60 phút chờ.

#### Acceptance Criteria

1. THE CI_Nightly SHALL được khai báo trong một tệp workflow riêng dưới `.github/workflows/`, với trigger `schedule` chạy một lần mỗi ngày và trigger `workflow_dispatch`.
2. THE CI_Nightly SHALL khai báo `timeout-minutes` không vượt quá 90 cho mỗi job.
3. THE CI_Nightly SHALL khai báo `permissions: contents: read` ở cấp workflow.
4. THE CI_PR SHALL chạy Fuzz_Harness và Supply_Chain_Gate và các guard test của Requirement 1.
5. THE CI_PR SHALL loại trừ Soak_Harness, Latency_Harness, và Chaos_Harness khỏi mọi job của mình.
6. THE CI_Nightly SHALL chạy Chaos_Harness, Latency_Harness, và Soak_Harness.
7. THE CI_PR SHALL giữ `timeout-minutes` của mỗi job không vượt quá 30.
8. WHEN CI_Nightly kết thúc, THE CI_Nightly SHALL nạp lên artifact một báo cáo cho mỗi harness đã chạy, kèm các chỉ số đo được và ngưỡng đối chiếu.
9. THE Soak_Harness và Latency_Harness và Chaos_Harness SHALL được đánh dấu bằng trait phân loại cho phép `dotnet test` lọc bỏ chúng bằng một biểu thức `--filter` duy nhất.
10. WHEN Guard_Test_Suite chạy trên CI_PR, THE Guard_Test_Suite SHALL fail nếu một test thuộc Soak_Harness, Latency_Harness, hoặc Chaos_Harness thiếu trait phân loại tương ứng.
11. WHERE Docker khả dụng trên Local_Docker, THE Soak_Harness và Latency_Harness và Chaos_Harness SHALL chạy được bằng một lệnh `dotnet test` có `--filter` tường minh, ghi trong `starhill/README` hoặc tệp hướng dẫn tương đương.
12. IF Docker không khả dụng lúc một harness thuộc nhóm nặng khởi chạy, THEN THE harness đó SHALL kết thúc với trạng thái bỏ qua (skipped) kèm thông báo nêu lý do Docker không khả dụng.

### Requirement 5: Chaos hạ tầng và hành vi fail-closed

**User Story:** Là người vận hành, tôi muốn biết hệ thống hỏng theo cách nào khi PostgreSQL hay broker chết, để chắc chắn rằng hỏng có nghĩa là từ chối phục vụ chứ không phải mất dữ liệu hay xử lý trùng.

#### Acceptance Criteria

1. WHEN Chaos_Harness dừng container PostgreSQL trong lúc một yêu cầu ghi đang thực thi, THE Host SHALL trả một mã trạng thái trong khoảng 500–599 kèm body `application/problem+json` không chứa chuỗi kết nối và không chứa dấu vết stack.
2. WHEN Chaos_Harness khởi động lại container PostgreSQL sau khi đã dừng, THE Host SHALL phục vụ thành công cùng yêu cầu ghi đó trong vòng 30 giây, không cần khởi động lại tiến trình `Host`.
3. WHEN Chaos_Harness dừng container PostgreSQL sau khi một transaction đã commit, THE Host SHALL trả đúng dữ liệu đã commit đó sau khi container PostgreSQL khởi động lại.
4. WHERE overlay messaging được bật, WHEN Chaos_Harness dừng container RabbitMQ trong lúc outbox còn bản ghi chưa gửi, THE Host SHALL giữ nguyên số bản ghi outbox chưa gửi trong cơ sở dữ liệu.
5. WHERE overlay messaging được bật, WHEN Chaos_Harness khởi động lại container RabbitMQ, THE Host SHALL gửi hết các bản ghi outbox tồn đọng trong vòng 60 giây và đưa số bản ghi chưa gửi về 0.
6. WHERE overlay messaging được bật, WHEN Chaos_Harness làm consumer dừng giữa lúc xử lý một message đã nhận, THE Host SHALL xử lý message đó đúng một lần có hiệu lực sau khi consumer chạy lại, đo bằng số dòng tác dụng phụ trong cơ sở dữ liệu bằng 1.
7. WHERE overlay messaging được bật, WHEN Chaos_Harness gửi lại cùng một message với cùng định danh 5 lần, THE Host SHALL giữ số dòng tác dụng phụ trong cơ sở dữ liệu bằng 1.
8. THE Chaos_Harness SHALL bao phủ tối thiểu 3 kịch bản: PostgreSQL chết giữa yêu cầu, broker chết khi outbox còn tồn đọng, và consumer chết giữa transaction.
9. THE CI_Nightly SHALL chạy Chaos_Harness với Testcontainers PostgreSQL và RabbitMQ thật.
10. THE Chaos_Harness SHALL hoàn tất trong vòng 15 phút trên runner `ubuntu-latest`.

### Requirement 6: Cổng SLO độ trễ p99

**User Story:** Là chủ sản phẩm, tôi muốn có ngưỡng độ trễ đo được cho các endpoint khách dùng nhiều nhất, để hồi quy hiệu năng bị phát hiện bằng số chứ không bằng cảm nhận "hôm nay hơi chậm".

#### Acceptance Criteria

1. THE Latency_Harness SHALL đo độ trễ cho đúng 5 endpoint sau, chọn theo tần suất gọi thật của frontend khách:
   - `POST /v1/guest/resolve` — điểm vào duy nhất sau khi quét QR, mỗi lần quét gọi một lần và khách chờ trước màn hình trắng.
   - `GET /v1/guest/rules` — force-read chặn đường vào mọi capability khác, gọi trước khi khách dùng được chat hay housekeeping.
   - `GET /v1/guest/conversation` — endpoint khách đọc hội thoại, frontend POLL chu kỳ ~4 giây (QR-N-085), tần suất gọi cao nhất trong hệ.
   - `GET /v1/guest/housekeeping` — endpoint khách đọc trạng thái ticket, frontend POLL chu kỳ ~5 giây (QR-N-086), tần suất gọi cao thứ hai.
   - `POST /v1/guest/messages` — đường ghi của khách, đi qua rule-gate và phát thông báo realtime, nên đắt nhất trong nhóm ghi.
2. THE Latency_Harness SHALL gửi 200 yêu cầu khởi động (warm-up) cho mỗi endpoint trong 5 endpoint ở tiêu chí 1 và loại các yêu cầu này khỏi mẫu tính phân vị.
3. THE Latency_Harness SHALL thu tối thiểu 2000 mẫu độ trễ cho mỗi endpoint trong 5 endpoint ở tiêu chí 1 sau giai đoạn khởi động.
4. THE Latency_Harness SHALL đo trên một stack gồm `Host` in-process và PostgreSQL chạy bằng Testcontainers, với dữ liệu gieo mầm gồm 60 phòng và 60 phiên khách, khớp biên vật lý dùng trong Requirement 7.
5. WHEN Latency_Harness hoàn tất thu mẫu cho một endpoint, THE Latency_Harness SHALL tính p50 và p99 từ mẫu đã thu và ghi cả hai giá trị vào báo cáo; báo cáo SHALL chứa đúng 5 dòng, một dòng cho mỗi endpoint ở tiêu chí 1.
6. IF p50 đo được của một endpoint lớn hơn 80 milliseconds, THEN THE Latency_Harness SHALL fail và in ra tên endpoint kèm giá trị p50 đo được; giá trị p50 đúng bằng 80 milliseconds là đạt.
7. IF p99 đo được của một endpoint lớn hơn 400 milliseconds, THEN THE Latency_Harness SHALL fail và in ra tên endpoint kèm giá trị p99 đo được; giá trị p99 đúng bằng 400 milliseconds là đạt.
8. THE CI_Nightly SHALL chạy Latency_Harness và coi kết quả fail của Latency_Harness là fail của job.
9. THE Latency_Harness SHALL hoàn tất trong vòng 20 phút trên runner `ubuntu-latest`.
10. THE Latency_Harness SHALL ghi vào báo cáo số phiên bản commit, tên runner, và số mẫu đã dùng cho mỗi endpoint trong 5 endpoint ở tiêu chí 1.

### Requirement 7: Soak độ bền chạy dài

**User Story:** Là người vận hành, tôi muốn biết Host chạy liên tục nửa giờ dưới tải thật vẫn giữ nguyên mức tiêu thụ tài nguyên, để không phải khởi động lại dịch vụ mỗi đêm vì rò rỉ.

**Lý do chốt tải ở biên vật lý (Rationale):** 60 phòng là **biên vật lý cứng** của quy mô mục tiêu, nên số phiên khách poll đồng thời tối đa bằng 60. Bài soak đo ở worst case có biên vật lý mạnh hơn bài soak đo ở một giả định tỉ lệ phần trăm phòng có khách đang mở app, vì tỉ lệ đó hiện không có dữ liệu vận hành để chốt. Đo ở 60 phiên loại bỏ hoàn toàn nhóm rủi ro "bài test xanh vì tải giả định quá thấp".

#### Acceptance Criteria

1. THE Soak_Harness SHALL phát tải liên tục trong 30 phút với tổng thông lượng 30 yêu cầu mỗi giây, phân bổ tường minh: 15 yêu cầu mỗi giây `GET /v1/guest/conversation`, 12 yêu cầu mỗi giây `GET /v1/guest/housekeeping`, 1 yêu cầu mỗi giây `GET /v1/guest/rules`, 1 yêu cầu mỗi giây `GET /v1/guest/faq`, và 1 yêu cầu mỗi giây `POST /v1/guest/messages`.
2. THE Soak_Harness SHALL ghi vào báo cáo công thức suy ra thông lượng ở tiêu chí 1 theo dạng `tải poll = số phiên khách hoạt động đồng thời ÷ chu kỳ poll`, với các tham số đã chốt: 60 phòng, 60 phiên khách hoạt động đồng thời (biên vật lý của quy mô mục tiêu), chu kỳ poll 4 giây cho `GET /v1/guest/conversation` cho 60 ÷ 4 = 15 yêu cầu mỗi giây, chu kỳ poll 5 giây cho `GET /v1/guest/housekeeping` cho 60 ÷ 5 = 12 yêu cầu mỗi giây, tổng poll = 27 yêu cầu mỗi giây, cộng 3 yêu cầu mỗi giây biên cho đường đọc và ghi còn lại thành 30 yêu cầu mỗi giây; báo cáo SHALL ghi tổng số yêu cầu dự kiến của bài chạy bằng 30 phút × 30 yêu cầu mỗi giây = 54.000 yêu cầu.
3. THE Soak_Harness SHALL mô phỏng 60 phòng và 60 phiên khách hoạt động đồng thời trong suốt bài chạy, mỗi phiên dùng một cookie thiết bị `__Host-starhill_guest` riêng.
4. THE Soak_Harness SHALL lấy mẫu mỗi 60 giây các chỉ số: managed heap sau thu gom rác cưỡng bức, số handle của tiến trình, số kết nối PostgreSQL đang mở, số phản hồi 5xx tích lũy, và số bản ghi outbox chưa gửi.
5. IF managed heap tại phút thứ 30 vượt managed heap tại phút thứ 10 quá 10 phần trăm, THEN THE Soak_Harness SHALL fail và in ra cả hai giá trị đo được.
6. IF số handle của tiến trình tăng đơn điệu qua 5 mẫu liên tiếp, THEN THE Soak_Harness SHALL fail và in ra dãy 5 giá trị đó.
7. IF số kết nối PostgreSQL đang mở tại phút thứ 30 vượt số kết nối tại phút thứ 10, THEN THE Soak_Harness SHALL fail và in ra cả hai giá trị đo được.
8. IF số phản hồi 5xx tích lũy lớn hơn 0 tại bất kỳ mẫu nào, THEN THE Soak_Harness SHALL fail và in ra endpoint cùng mã trạng thái của phản hồi 5xx đầu tiên.
9. IF số bản ghi outbox chưa gửi tại phút thứ 30 vượt 10, THEN THE Soak_Harness SHALL fail và in ra dãy giá trị outbox chưa gửi của toàn bộ mẫu.
10. WHEN Soak_Harness hoàn tất, THE Soak_Harness SHALL ghi toàn bộ 30 mẫu vào một tệp báo cáo dạng bảng, kèm thông lượng thực đạt được của từng endpoint trong 5 endpoint ở tiêu chí 1.
11. IF thông lượng thực đạt được, tính trung bình trên toàn bộ thời lượng bài chạy, thấp hơn mục tiêu 30 yêu cầu mỗi giây quá 10 phần trăm, THEN THE Soak_Harness SHALL fail và in ra thông lượng thực đạt được của từng endpoint trong 5 endpoint ở tiêu chí 1 kèm mục tiêu tương ứng.
12. THE CI_Nightly SHALL chạy Soak_Harness và coi kết quả fail của Soak_Harness là fail của job; job SHALL vẫn có thể fail vì các nguyên nhân khác (hết `timeout-minutes`, lỗi hạ tầng runner) khi Soak_Harness báo thành công.
13. WHERE Docker khả dụng trên Local_Docker, THE Soak_Harness SHALL chạy được bằng một lệnh `dotnet test` có `--filter` tường minh và cho phép rút thời lượng bằng một biến môi trường có giá trị mặc định 30 phút.

### Requirement 8: Quan sát được tối thiểu cho vận hành

**User Story:** Là người vận hành một resort, tôi muốn nhìn thấy trace, metric và log có cấu trúc của Host, để chẩn đoán sự cố lúc 2 giờ sáng mà không cần bật lại dịch vụ ở chế độ debug.

**Lý do telemetry fail-open (Rationale):** telemetry là hệ thống **quan sát**, không phải hàm nhiệm vụ. Fail-closed lúc khởi động biến một sự cố collector thành sự cố phục vụ khách, và sẽ xảy ra đúng lúc tệ nhất: mất điện → restart cả stack → collector lên chậm hơn `Host` → `Host` tự chết dù PostgreSQL và nghiệp vụ đều lành. Fail-closed chỉ dành cho thứ ảnh hưởng **tính đúng đắn dữ liệu** — HTML sanitizer, cổng chặn mock build, secret bắt buộc — và telemetry không thuộc nhóm đó. Đánh đổi phải trả: mất telemetry trở thành lỗi im lặng, nên các tiêu chí 5–7 dưới đây bắt buộc mất kết nối exporter phải quan sát được và không bao giờ được backpressure vào đường xử lý yêu cầu.

#### Acceptance Criteria

1. THE Host SHALL xuất trace theo giao thức OTLP cho mọi yêu cầu HTTP tới Guest_Endpoints và Admin_Endpoints.
2. THE Host SHALL xuất tối thiểu 3 metric theo giao thức OTLP: histogram thời lượng yêu cầu HTTP có nhãn route và mã trạng thái, bộ đếm phản hồi 5xx có nhãn route, và gauge số bản ghi outbox chưa gửi.
3. WHERE khóa cấu hình bật xuất OTLP có giá trị `false`, THE Host SHALL khởi động thành công và phục vụ Guest_Endpoints mà không mở kết nối tới điểm nhận OTLP, với điều kiện các cấu hình bắt buộc khác (chuỗi kết nối, khóa ký JWT) hợp lệ.
4. WHERE khóa cấu hình bật xuất OTLP có giá trị `true`, IF `Host` không mở được kết nối tới điểm nhận OTLP lúc khởi động HOẶC trong lúc chạy, THEN THE Host SHALL tiếp tục khởi động và phục vụ Guest_Endpoints và Admin_Endpoints bình thường.
5. WHERE khóa cấu hình bật xuất OTLP có giá trị `true`, IF `Host` không mở được kết nối tới điểm nhận OTLP, THEN THE Host SHALL ghi một dòng log mức `Warning` nêu tên khóa cấu hình cùng địa chỉ điểm nhận đã thử, và SHALL phơi một chỉ báo trạng thái exporter dạng metric hoặc bộ đếm nhận đúng một trong hai giá trị `connected` và `disconnected`, để phân biệt trạng thái "telemetry đang mất" với trạng thái "hệ thống không có lưu lượng".
6. THE Host SHALL giới hạn bộ đệm telemetry bằng một số bản ghi tối đa tường minh trong cấu hình, và WHEN bộ đệm telemetry đầy, THE Host SHALL bỏ bản ghi telemetry mới thay vì chặn luồng xử lý yêu cầu HTTP, và SHALL tăng một bộ đếm số bản ghi telemetry đã bỏ.
7. WHERE khóa cấu hình bật xuất OTLP có giá trị `true`, IF `Host` không mở được kết nối tới điểm nhận OTLP, THEN THE Host SHALL tiếp tục trả HTTP 200 tại `/health/ready` khi PostgreSQL và các phụ thuộc readiness khác lành.
8. THE Host SHALL ghi mỗi dòng log ở dạng có cấu trúc chứa các trường `traceId`, `spanId`, `level`, và `message`.
9. WHEN một yêu cầu HTTP được `Host` xử lý, THE Host SHALL dùng cùng một giá trị `traceId` cho mọi dòng log phát sinh từ yêu cầu đó.
10. THE Guard_Test_Suite SHALL khẳng định log của `Host` không chứa token truy cập khách, mã QR đầy đủ, khóa ký JWT, và chuỗi kết nối cơ sở dữ liệu, cho tối thiểu 5 luồng nghiệp vụ thuộc Guest_Endpoints.
11. THE Host SHALL công bố 4 ngưỡng cảnh báo trong một tệp cấu hình được kiểm soát phiên bản: `/health/ready` trả mã khác 200 liên tục quá 2 phút, tỉ lệ phản hồi 5xx vượt 1 phần trăm trong cửa sổ 5 phút, số bản ghi outbox chưa gửi vượt 100 trong cửa sổ 10 phút, và chỉ báo trạng thái exporter telemetry ở tiêu chí 5 giữ giá trị `disconnected` liên tục quá 10 phút.
12. WHEN Guard_Test_Suite chạy trên CI_PR, THE Guard_Test_Suite SHALL fail nếu tệp cấu hình ngưỡng cảnh báo thiếu bất kỳ ngưỡng nào trong 4 ngưỡng ở tiêu chí 11.
13. THE Observability_Pipeline SHALL đặt toàn bộ mã cấu hình trong tầng Host hoặc Infrastructure, không đặt trong tầng Application của bất kỳ module QR nào.

### Requirement 9: Đăng ký quyết định và guard test chống trôi

**User Story:** Là người kế nhiệm dự án, tôi muốn mọi quyết định hardening đều có mục journal và guard test tương ứng, để 6 tháng sau không ai vô tình tháo cổng kiểm chứng mà CI vẫn xanh.

#### Acceptance Criteria

1. WHEN một quyết định hardening mới được triển khai, THE Journal_QR SHALL chứa một mục `QR-AD` mô tả quyết định đó kèm dòng `- Guard-Tests:` liệt kê tên test class thực thi quyết định.
2. THE Guard_Test_Suite SHALL giữ INV_6 ở trạng thái xanh sau khi toàn bộ requirement của tài liệu này được triển khai.
3. THE Guard_Test_Suite SHALL fail nếu một test class được khai trong dòng `- Guard-Tests:` không tồn tại trong `starhill/tests/**/*.cs`.
4. THE cây `platform/` SHALL không chứa định danh nghiệp vụ QR nào phát sinh từ tài liệu này, đo bằng guard test tìm kiếm các định danh nghiệp vụ QR trong `platform/src/**`.
5. WHERE mã của Fuzz_Harness hoặc Chaos_Harness hoặc Latency_Harness hoặc Soak_Harness tồn tại trong repo, THE mã đó SHALL nằm dưới `starhill/tests/` và SHALL không nằm dưới `starhill/src/`.
6. THE Observability_Pipeline và Health_Probe SHALL không tạo tham chiếu từ tầng Application của bất kỳ module QR nào tới ASP.NET Core, Entity Framework Core, hoặc SignalR, đo bằng các architecture test module boundary hiện có.
7. WHEN CI_Nightly được thêm vào repo, THE `starhill/tests/validate_ci.py` SHALL kiểm tra CI_Nightly có `timeout-minutes` cho mọi job và có `permissions: contents: read` ở cấp workflow.

## Quyết định đã chốt

Không còn hạng mục nào chờ quyết định. Ba quyết định dưới đây đã chốt và đã phản ánh vào các requirement tương ứng; ghi lại để không mất lịch sử.

1. **Telemetry fail-open** (Requirement 8.4–8.7). Telemetry là hệ thống quan sát, không phải hàm nhiệm vụ; fail-closed lúc khởi động biến sự cố collector thành sự cố phục vụ khách và xảy ra đúng lúc tệ nhất (mất điện → restart stack → collector lên chậm hơn `Host`). Kèm theo: bộ đệm telemetry có giới hạn, bỏ bản ghi thay vì chặn hot path, chỉ báo trạng thái exporter để mất telemetry không im lặng, và exporter OTLP không phải phụ thuộc readiness. Fail-closed giữ lại cho nhóm ảnh hưởng tính đúng đắn dữ liệu (HTML sanitizer, cổng chặn mock build, secret bắt buộc).
2. **Soak đo ở biên vật lý 60 phiên đồng thời = 30 yêu cầu mỗi giây** (Requirement 7.1–7.3, 7.11; dữ liệu gieo mầm Requirement 6.4). 60 phòng là biên vật lý cứng nên số phiên poll đồng thời tối đa bằng 60; đo ở worst case có biên vật lý mạnh hơn đo ở một giả định tỉ lệ phần trăm không có dữ liệu vận hành. Kèm theo một cổng kiểm tra thông lượng thực để bài test không xanh vì generator tải quá yếu.
3. **Exactly-once đo bằng số dòng tác dụng phụ bằng 1** (Requirement 5.6–5.7, giữ nguyên). Tiêu chí dựa trên cờ nội bộ không kiểm chứng được từ bên ngoài và sẽ làm rỗng chính bất biến chống trôi INV_6 mà tài liệu này dựa vào.

## Phi phạm vi (Out of Scope)

Các hạng mục dưới đây **đã đạt và đã đo** trên baseline hiện tại, tài liệu này không đặc tả lại:

- Cổng 0-warning build Release và suite test đầy đủ trên CI_PR (job `build-test`).
- Cổng frontend: `pnpm install --frozen-lockfile`, build 2 SPA, Playwright 65/65 (job `frontend`).
- Ghim phiên bản phụ thuộc: CPM `starhill/Directory.Packages.props` không có version floating; `pnpm-lock.yaml` với `--frozen-lockfile`.
- Chống rò secret vào log ở luồng resolve (`GuestAccessResolveLogRedactionTests`).
- Container chạy non-root (`USER $APP_UID` trong `Dockerfile` Host).
- Fail-closed mock frontend: `vite build` fail khi `VITE_STARHILL_MOCK=1` (QR-AD-058, `FrontendDeliveryGuardTests`).
- Anti-drift keystone `StarHillJournalConsistencyTests` INV-1..6 — tài liệu này **dùng** bất biến đó (Requirement 9), không xây lại.
- `timeout-minutes` cho mọi job của CI_PR hiện có.
- EF migration bundle out-of-band cho 8 module (job `migration-bundle`).

Các hạng mục dưới đây **không thuộc phạm vi** vì thuộc dự án khác:

- Toàn bộ mục `CD-1..CD-9`, `S-1..S-7`, `A-1..A-6` của `ARCHITECTURE-REVIEW-2026-07-26.md`. Tệp đó review dự án `vision-platform/` (Python, SHM, RTSP); tìm kiếm định danh của các mục đó trong repo này trả về 0 kết quả khớp.

Các hạng mục dưới đây **chủ động hoãn**, ghi lại để không bị hiểu là bỏ sót:

- Đưa nghiệp vụ hardening vào `platform/`: `platform/` giữ nguyên vai trò base domain-agnostic.
- Kiểm thử tải multi-tenant hay nhiều resort đồng thời: ngoài quy mô mục tiêu 1 resort ~60 phòng.
- Hạ tầng triển khai (Kubernetes manifest, reverse proxy, TLS termination): tài liệu này chỉ yêu cầu container tự báo sức khỏe, không đặc tả nơi chạy production.
- Pentest thủ công, kiểm định WCAG, và đánh giá bảo mật do chuyên gia thực hiện: cần con người, không thể đóng thành cổng CI.
