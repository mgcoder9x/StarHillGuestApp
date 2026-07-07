# 17 — Test Plan cho phần Base (Property → Test, có thể kiểm chứng)

> **File authoritative cho:** chiến lược biến 10 Correctness Property (`08`) + các bất biến nền thành **test cụ thể chạy được**. Bổ sung `07-testing-strategy.md` (tổng quan tầng test) bằng **ma trận Property→Test** + kịch bản Given/When/Then cho luồng rủi ro cao.
>
> **Mục tiêu (đúng tinh thần "valid nhiều lần, kiểm chứng được"):** mỗi bất biến nền có ít nhất một test **fail khi bất biến bị phá**. Design chỉ "đúng trên giấy" cho tới khi có test xanh chứng minh.

## 1. Tầng test & công cụ (nhắc lại từ `07`)

| Tầng | Phạm vi | Công cụ |
|---|---|---|
| Unit (Domain/Application) | use case với port giả lập (clock/token/sanitizer/currentUser) | xUnit + NSubstitute |
| Property-based (PBT) | bất biến sinh dữ liệu ngẫu nhiên, ≥100 iteration/property (Req 19.3) | FsCheck.Xunit |
| Integration (API+EF) | DB thật, ràng buộc, concurrency, auth boundary | xUnit + **Testcontainers PostgreSQL 18** + `Microsoft.AspNetCore.Mvc.Testing` |
| Architecture | dependency rule + naming | NetArchTest |

> **PostgreSQL 18 trong Testcontainers** để test đúng hành vi thật của partial unique index, `xmin`, và (nếu dùng) `uuidv7()`. Không dùng InMemory provider cho test ràng buộc (InMemory không enforce unique/partial index → false green).

## 2. Ma trận Property → Test (10 property nền)

| Property | Tầng test chính | Test cụ thể (fail khi bất biến bị phá) |
|---|---|---|
| **B1** Dependency rule | Architecture | NetArchTest: `Domain` không ref EFCore/Api; `Application` không ref Api/Infrastructure; `SharedKernel` không ref EFCore/ASP.NET. Fail → liệt kê vi phạm. |
| **B2** Ghi DB nguyên tử | Integration | Giả lập lỗi giữa `ExecuteInTransactionAsync` (ví dụ vi phạm ràng buộc ở bước 2) → assert **không** dòng nào của bước 1 được ghi (rollback). Rotate token: kill giữa chừng → không để phòng 0/2 token Active. |
| **B3** Bất biến ở DB | Integration | Cố INSERT trực tiếp (bỏ qua service): 2 token Active/phòng; 2 token cùng `Token`; 2 visit Active/(session,room); 2 lang default; 2 publication IsCurrent; 2 conversation/visit → **đều** ném unique violation. |
| **B4** Thời gian/ngẫu nhiên tất định | Unit | Inject `IDateTimeProvider` fake (thời gian cố định) + `ITokenGenerator` fake (chuỗi tất định) → use case cho kết quả tất định, không đọc `DateTime.UtcNow`/`Random` thật (kiểm bằng ArchitectureTest cấm dùng `DateTime.Now/UtcNow` trong Application). |
| **B5** Hợp đồng lỗi ổn định | Integration + Unit | Mỗi `AppErrors.code` → ProblemDetails có `code` đúng + HTTP status đúng bảng ánh xạ; không lộ stack trace ở 500. Test đối chiếu tập `AppErrors` (reflection) == union `ErrorCode` FE (`14` §5). |
| **B6** Actor được gắn | Integration | Tạo/sửa entity `IAuditable` khi có `ICurrentUser` → `CreatedAt/UpdatedAt/actor` set đúng; guest (không user) → actor null, không ghi được actor staff. |
| **B7** Sanitize bắt buộc | Unit + Integration | Ghi nội dung rich text chứa `<script>`, `onclick=`, `javascript:` → sau lưu **không còn** phần tử thực thi (theo allowlist `03` §9). Không có đường ghi nào bỏ qua sanitizer. |
| **B8** Cửa sổ luôn hết hạn được | PBT | Sinh chuỗi request tương tác với khoảng cách thời gian ngẫu nhiên; nếu tổng vượt PortalWindow kể từ LastSeenAt → **luôn** trả `session_expired`; chỉ `/resolve` refresh. Không tồn tại chuỗi nào gia hạn vô hạn. |
| **B9** Tách guest/admin | Integration | `/api/admin/*` không JWT → 401/403; `/api/guest/*` không cần JWT nhưng phát/đọc cookie GuestSession; guest không join được conversation của session khác. |
| **B10** Concurrency an toàn | Integration | Hai `SaveChanges` đồng thời cùng bản ghi `IConcurrencyAware` (đọc cùng xmin) → lần sau nhận `409 concurrency_conflict`, không ghi đè. |

## 3. Given/When/Then — luồng rủi ro cao

### GA — Resolve + lazy idle-expiry (từ ma trận edge-case `13` §7)
- **E6 (quan trọng):**
  - *Given* một `GuestVisit` Active của (session S, room R) với `ExpiresAt` đã ở quá khứ (idle > 24h), sweeper **chưa** chạy.
  - *When* thiết bị S quét lại QR của R (`/resolve`).
  - *Then* visit cũ bị **lazy-expire** (Status=Expired, cascade đóng conversation/hủy ticket), tạo **visit mới**; response **không** chứa dữ liệu (ack/conversation) của lượt cũ.
- **E7 (race):**
  - *Given* thiết bị S chưa có visit Active cho R.
  - *When* hai request `/resolve` gần như đồng thời.
  - *Then* chỉ **một** `GuestVisit` Active tồn tại cho (S, R); request thua unique re-query và dùng lại đúng visit đó (không trả lỗi ra khách).

### GA — Portal window (B8)
- *Given* visit Active, `LastSeenAt = t0`, PortalWindow=30'.
- *When* gọi `POST /messages` tại `t0 + 31'`.
- *Then* `session_expired`; `LastSeenAt` **không** đổi; gọi lại vẫn `session_expired` cho tới khi `/resolve` mở khóa.

### RQ — Rotate token (B2 + B3)
- *Given* room R có token T1 Active.
- *When* hai admin đồng thời gọi `revoke-token`.
- *Then* kết thúc: **đúng 1** token Active mới; T1 Revoked (giữ lịch sử); request đua thua nhận lỗi/khớp token Active hiện hành; không có trạng thái 0 hoặc 2 Active.
- *And* resolve URL chứa T1 → `qr_revoked`.

### ID — Refresh token rotation + reuse detection (`12` §3)
- *Given* user đăng nhập, có refresh token RT1 (family F).
- *When* dùng RT1 refresh → nhận RT2 (RT1 revoked); sau đó **dùng lại RT1**.
- *Then* hệ thống phát hiện reuse → **thu hồi toàn bộ family F** (RT2 cũng vô hiệu) → buộc đăng nhập lại.

### ID — Chống user enumeration (`12` §4)
- *Given* email không tồn tại và email tồn tại nhưng sai mật khẩu.
- *When* login cả hai.
- *Then* thông báo lỗi **giống nhau** ("email hoặc mật khẩu không đúng"); thời gian phản hồi không tiết lộ (hash giả chạy kể cả khi user không tồn tại).

### FB — Fallback ngôn ngữ (B liên quan Req 9)
- *Given* mục nội dung chỉ có bản dịch `en` (default), yêu cầu `ko`.
- *When* Translation_Resolver.Resolve.
- *Then* trả bản `en` với `IsFallback=true`, giá trị **không rỗng**; mục khác thiếu cả `ko` lẫn `en` → đánh dấu missing nhưng **không** làm lỗi cả tập.

## 4. Property-based test (liệt kê — Req 19.2/19.3)
- **PBT-1** Token sinh: N token liên tiếp đều URL-safe base64url, độ dài ≥43, **không trùng** trong tập sinh, round-trip decode/encode bằng chính nó.
- **PBT-2** Fallback ngôn ngữ: với mọi tập bản dịch có ít nhất `en` không rỗng → resolve luôn trả giá trị không rỗng.
- **PBT-3** Portal window (B8): với mọi chuỗi thời điểm tương tác, tồn tại thời điểm hết hạn hữu hạn; không chuỗi nào gia hạn vô hạn.
- **PBT-4** Đơn điệu `UnreadForStaff`: chuỗi thao tác (guest gửi / staff đọc) → unread **không bao giờ âm**; đọc đưa về 0.
- Mỗi PBT ≥100 iteration; khi có counterexample → **ghi seed để tái lập** + fail (Req 19.4).

## 5. Definition of Done (test phần base)
- ArchitectureTests xanh (B1) — gate CI bắt buộc.
- Mọi partial/unique index (B3) có integration test chứng minh chặn ghi vi phạm trên **PostgreSQL 18 thật** (Testcontainers).
- Concurrency 409 (B10), token rotate nguyên tử (B2), resolve lazy-expiry + race (GA), auth boundary (B9), refresh reuse detection (ID) đều có test.
- 4 PBT đạt ≥100 iteration.
- Test đối chiếu `AppErrors` ↔ FE `ErrorCode` (B5) xanh → chống lệch hợp đồng.
- Không dùng InMemory provider cho test ràng buộc DB.

## 6. Truy vết
- **Validates: Requirements 19.1–19.8** (và gián tiếp toàn bộ property B1–B10 → Requirements tương ứng trong `08`).
