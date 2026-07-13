# Design — B-Rooms.4: Rooms query slice (GET danh sách + chi tiết phòng)

> Slice tiếp theo (đã defer có chủ đích ở B-Rooms.3 Q4). Hoàn tất Req 7.6 "**Staff chỉ được xem** danh sách/thông
> tin phòng phục vụ vận hành" — hiện Staff CHƯA có endpoint nào để xem danh sách phòng. Contained + read-only +
> verify KHÔNG cần Docker. Mọi khẳng định đã kiểm từ code thật.

## 0. Nguồn sự thật (đã verify)
- Pattern READ của sản phẩm = **interface query + EF impl inject `RoomsDbContext` + đăng ký scoped thủ công** (KHÔNG
  qua use case pipeline) — precedent `EfRoomTokenResolver` (`Rooms.Contracts`) + `EfResortSettingsQuery` (ResortConfig).
  Read không cần transaction/validation pipeline.
- Base có sẵn `PagedRequest(Page,PageSize)` (SafePage/SafePageSize cap 100/Skip) + `PagedResult<T>(Items,Page,PageSize,Total)`
  ở `Bedrock.Application.UseCases` (`platform/src/Bedrock.Application/UseCases/Paging.cs`) → TÁI DÙNG, không chế mới.
- `Room : AuditableEntity` có `CreatedAt`; soft-delete có global query filter (verify `EfRoomTokenResolver` cmt "query
  filter loại phòng xóa mềm"). `RoomQrToken` có `Status`(Active/Revoked) + `TokenPreview` + `Version`; ux_qr_active ⇒ ≤1 Active/phòng.
- `StarHillPolicies.RequireStaff` (Admin superset) đã có (QR-AD-020). JsonStringEnumConverter toàn cục (QR-AD-021)
  ⇒ `Status` serialize dạng string trong response. Query-string enum bind bằng tên (minimal API TryParse) — không cần converter.

## 1. Quyết định (kèm lý do)
- **D-A đặt read-model ở `Rooms.Application`** (KHÔNG Contracts): list/detail là read admin **nội-module** (Api cùng
  module tiêu thụ), không cross-module → Contracts (surface cross-module) giữ sạch. DTO `RoomListItem` + port `IRoomQueries`.
- **D-B role = RequireStaff** cho cả GET list + GET detail (Req 7.6 "Staff xem"; Admin superset). Nhất quán qr.png.
- **D-C phân trang chuẩn** (PagedRequest/PagedResult) ngay từ đầu: list endpoint không phân trang mà sau thêm =
  **breaking change** response shape (array→object). Phân trang là table-stakes API thương mại → thêm bây giờ là
  bảo hiểm rẻ, KHÔNG gold-plate. Cap 100/trang (SafePageSize base).
- **D-D đọc 2 truy vấn đơn giản + map in-memory** (page rooms rồi load active token theo `ids.Contains`) thay vì
  correlated-subquery lồng — TRÁNH rủi ro dịch LINQ phức tạp trên SQLite (provider test) + Postgres; ux_qr_active ⇒
  ≤1 active/phòng nên map dictionary an toàn.
- **D-E ordering**: `Building` rồi `RoomNumber` (thứ tự admin tự nhiên, ổn định phân trang).
- **D-F response**: trả thẳng `PagedResult<RoomListItem>` (đã là DTO đọc, không phải entity Domain) — không cần Api-DTO
  riêng (khác endpoint ghi: ghi cần tách DTO chống mass-assignment; đọc không có rủi ro đó). Token thô KHÔNG lộ (chỉ preview — CP1/D-B B-Rooms.3).

## 2. Hợp đồng HTTP
- `GET /v1/rooms?status={Active|Inactive|Maintenance}&page=1&pageSize=20` [RequireStaff] → 200 `PagedResult<RoomListItem>`.
- `GET /v1/rooms/{roomId:guid}` [RequireStaff] → 200 `RoomListItem` | 404 `not_found` (ProblemDetails).
- `RoomListItem(Guid RoomId, string RoomNumber, string? Building, int? Floor, RoomStatus Status, string? ActiveTokenPreview, int ActiveTokenVersion, DateTimeOffset CreatedAt)`.

## 3. Cấu trúc code
- `Rooms.Application/IRoomQueries.cs` (MỚI): `RoomListItem` + `IRoomQueries.ListAsync(status,paging)/GetByIdAsync(id)`.
- `Rooms.Infrastructure/Persistence/EfRoomQueries.cs` (MỚI): impl inject RoomsDbContext; đăng ký scoped ở `AddRoomsInfrastructure`.
- `Rooms.Api/RoomsEndpointModule.cs`: +2 GET endpoint (RequireStaff).

## 4. Verify (KHÔNG Docker)
- `RoomQueriesTests` (Rooms.IntegrationTests, SQLite in-memory — mirror `RoomsUseCaseTests`): list trả item kèm
  ActiveTokenPreview/Version; total đúng; phòng xóa-mềm bị loại; filter status; phân trang (page 2); GetById có/không (null).
- `RoomsEndpointAuthTests` (+fake IRoomQueries): Staff GET list+detail = 200; Admin = 200 (superset); no-token = 401; GET detail không thấy = 404.
- `vp all` + `vp journal`.

## 5. Anti-drift
- **QR-AD-022** — Rooms query slice (read-model nội-module ở Application + phân trang chuẩn + role Staff). Guard map row.
