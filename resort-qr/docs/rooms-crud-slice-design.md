# Vertical slice: Room CRUD còn lại (list/detail/update/status/soft-delete) — design-first

> Bám `16` §2/§8, `02` §6.1 (read query service — CQRS-lite), `04` §7.1 (ux_room_number), `14`. Test SQLite Docker-free.

## 0. Phạm vi
Hoàn thiện quản trị phòng: **GET list (paged) + GET detail** (Staff|Admin), **PUT update + PUT status + DELETE soft-delete** (Admin). Kèm token preview của token Active trong list/detail (KHÔNG lộ token đầy đủ — Req 11.6; QR đầy đủ qua endpoint qr.png).

## 1. Read model (`02` §6.1) — KHÔNG xuyên IQueryable ra ngoài
- **`RoomListItem`** (Application DTO): `Id, RoomNumber, Building?, Floor?, RoomStatus Status, string? ActiveTokenPreview, DateTimeOffset CreatedAt`.
- **`IRoomQueries : IScopedService`** (Application port): `ListAsync(PagedRequest)`, `GetByIdAsync(id)`.
- **`EfRoomQueries`** (Infrastructure, **namespace Persistence** → loại auto-scan + wire tường minh trong `AddResortQrPersistence` — nhất quán DEC-049, vì coupled DbContext). EF projection `AsNoTracking`; query filter tự loại soft-deleted. Token preview qua **correlated subquery** (`Set<RoomQrToken>().Where(active).Select(preview).FirstOrDefault()`).
- **Status enum trong DTO + `JsonStringEnumConverter`** (API): tránh dịch `enum.ToString()` trong SQL (rủi ro không-translate); JSON serialize enum→string (hợp đồng API rõ ràng, chuẩn commercial). Áp global qua `ConfigureHttpJsonOptions`.

## 2. Mutations (use case)
- **`UpdateRoomUseCase`** (`UpdateRoomInput(RoomId, RoomNumber, Building?, Floor?)`): FindById (filter→null nếu deleted→not_found); set field; Save; trùng số (ux_room_number) → validation_error. `UpdateRoomValidator` (RoomNumber 1–20, Building ≤50, Floor −10..200 — Req 16.1).
- **`ChangeRoomStatusUseCase`** (`ChangeRoomStatusInput(RoomId, RoomStatus Status)`): FindById→not_found; set Status; Save. (Endpoint parse string→enum; sai → validation problem.)
- **`DeleteRoomUseCase`** (`RoomId`): FindById→not_found; `Repository.Remove(room)` → convention chuyển soft-delete (IsDeleted=true, DeletedAt) — giữ token/visit/lịch sử, không vỡ FK (`04` §7.3). Save.

## 3. Endpoint (`16` §8) — phân quyền per-endpoint
Bỏ auth mức group; mỗi endpoint tự khai policy:
- `GET /api/admin/rooms?page=&pageSize=` → **RequireStaff** (Staff|Admin).
- `GET /api/admin/rooms/{id}` → **RequireStaff**.
- `PUT /api/admin/rooms/{id}` (update) → **RequireAdmin**.
- `PUT /api/admin/rooms/{id}/status` `{status}` → **RequireAdmin**.
- `DELETE /api/admin/rooms/{id}` → **RequireAdmin**.
- (create/rotate/qr.png giữ RequireAdmin.)

## 4. Errors
Dùng lại `RoomsErrors.RoomNumberTaken` (validation_error), `RoomNotFound` (not_found). Status string sai ở endpoint → `CommonErrors.Validation` (validation_error).

## 5. Test (SQLite Docker-free)
- Queries: list phân trang (Total đúng, loại soft-deleted, có ActiveTokenPreview); detail theo id; detail id không tồn tại → null.
- Update: đổi field OK; đổi sang số đã tồn tại (phòng khác) → validation_error; không tồn tại → not_found.
- ChangeStatus: Active→Maintenance persisted.
- Delete: soft-delete (IsDeleted=true, biến khỏi list); token giữ nguyên (không xóa cứng).
- Endpoint: happy-path tạo phòng qua HTTP với **admin JWT** (IJwtTokenService.Issue role Admin) → 201; GET list KHÔNG token → 401.

## 6. Truy vết
- `16` §2/§8, `02` §6.1, `04` §7.1/§7.3, `14`; Req 7.6, 16.1/16.6. Base: DEC-053/054/057, DEC-049 (exclude persistence từ auto-scan).
