# Vertical slice: Admin Rooms & QR token (issue + rotate) — design-first

> Bám `16-rooms-and-qr` (§2 CRUD, §3 issue, §4 rotate, §5 unique token), `04` §7.1 (ux_qr_active, ux_qrtoken_token, ux_room_number), `14` (mã lỗi), Property B2 (rotate nguyên tử)/B3 (unique ở DB). Test SQLite Docker-free.

## 0. Phạm vi
**Trong slice:** phần đúng-đắn cốt lõi — **tạo phòng + issue token**, **rotate token nguyên tử + chịu race**, **sinh token unique** (retry + DB guard), endpoint admin (RequireAdmin). Tận dụng `UniqueConstraintViolationException` (DEC-056) làm trọng tài race.
**Hoãn (lý do):**
- **QR PNG (QRCoder) + PDF nhãn (QuestPDF)** — Req 7.2/7.3, 16.6-8: là *render output*, không phải đúng-đắn miền; QuestPDF vướng license theo doanh thu (TK-019) → tách increment riêng (port `IQrService`/`IPdfService`).
- Room **update/list/delete** đầy đủ + validator sửa: tách sau (mechanical); slice này làm **create + rotate** (đường xương sống + 2 bất biến sống còn).

## 1. Nguồn `ResortId` (single-resort — `24`)
Instance-per-resort → mỗi deployment 1 Resort. Use case lấy resort bằng `repo<Resort>.FirstOrDefaultAsync(_ => true)` (không có resort → lỗi cấu hình). ⚠️ **TK**: khi chuyển shared-DB multi-tenant, `ResortId` phải lấy từ `ICurrentUser.ResortId` (claim JWT) — `24` §6 checklist. Ghi TK-040.

## 2. Sinh token unique (`16` §5) — helper dùng chung
```
RoomTokenFactory.GenerateUniqueTokenAsync(repo<RoomQrToken>, tokenGen, ct):
   FOR attempt IN 1..5:
      candidate = tokenGen.NewToken()            // CSPRNG base64url 256-bit (05 §1)
      IF NOT await repo.AnyAsync(t => t.Token == candidate): RETURN candidate
   RETURN null                                    // cực hiếm → qr_generation_failed
```
- Kiểm tồn tại là **best-effort** giảm va chạm; `ux_qrtoken_token` là chốt chặn thật khi insert (chịu race).
- `TokenPreview` = 6 ký tự đầu + "…" (che, không lộ token đầy đủ — Req 11.6).

## 3. CreateRoomUseCase (`16` §3) — tạo phòng + issue token NGUYÊN TỬ
```
IUseCase<CreateRoomInput(RoomNumber, Building?, Floor?), CreateRoomResult(RoomId, Token, TokenPreview)>
- resort = repo<Resort>.FirstOrDefault(_=>true); null → qr_generation_failed (chưa seed)  [hoặc invalid_configuration]
- token = GenerateUniqueToken(); null → qr_generation_failed
- room = new Room{ ResortId, RoomNumber, Building, Floor, Status=Active }
- qr = new RoomQrToken{ RoomId=room.Id, Token, TokenPreview, Status=Active, Version=1, CreatedAt=now, CreatedByUserId=currentUser }
- repo.Add(room); repo.Add(qr)
- TRY uow.Save()                                  // MỘT transaction → room+token nguyên tử
  CATCH UniqueConstraintViolation → validation_error ("Số phòng đã tồn tại")  // ux_room_number (Req 16.6)
- RETURN Ok(room.Id, token, preview)
```
- Validation input qua **FluentValidation** (`CreateRoomValidator`): RoomNumber 1–20, Building ≤50, Floor −10..200 (Req 16.1) → decorator tự chặn (validation_error + field errors) trước khi vào use case.
- Nguyên tử bằng **một SaveChanges** (room+token cùng transaction EF) — đơn giản hơn ExecuteInTransaction mà vẫn all-or-nothing (không cần multi-save). Ghi DEV.

## 4. RotateRoomTokenUseCase (`16` §4) — thu hồi + cấp lại NGUYÊN TỬ, chịu race
```
IUseCase<RotateRoomTokenInput(RoomId, Reason?), RotateRoomTokenResult(Token, TokenPreview)>
- room = repo<Room>.FindById(RoomId); null OR Status!=Active → (null→not_found; !Active→qr_generation_failed Req16.7)
- current = repo<RoomQrToken>.FirstOrDefault(t => t.RoomId==RoomId && t.Status==Active)  // ≤1 (ux_qr_active)
- IF current != null: current.Status=Revoked; current.RevokedAt=now; current.RevokedByUserId=actor; current.RevocationReason=Reason
- newToken = GenerateUniqueToken(); null → qr_generation_failed
- add RoomQrToken{ Active, Version=(current?.Version ?? 0)+1, CreatedAt=now, CreatedByUserId=actor }
- TRY uow.Save()                                  // revoke cũ + insert mới CÙNG transaction (nguyên tử — B2)
  CATCH UniqueConstraintViolation → qr_generation_failed  // 2 admin rotate đồng thời: ux_qr_active chỉ cho 1 Active mới
- POST: đúng 1 token Active/phòng; token cũ giữ (Revoked) — lịch sử (Req 7.4), KHÔNG xóa cứng.
```
- Nguyên tử bằng **một SaveChanges** (revoke+insert cùng transaction). Race → `ux_qr_active` phân xử → unique-violation → `qr_generation_failed` (`16` §4 cho phép; deterministic, không retry ẩn).

## 5. Errors (∈ catalog `14`)
`RoomsErrors`: `RoomNumberTaken`=validation_error(400, "Số phòng đã tồn tại"); `QrGenerationFailed`=qr_generation_failed(409); dùng `CommonErrors.NotFound` cho phòng không tồn tại.

## 6. Endpoint admin (`16` §8) — RequireAdmin
- `POST /api/admin/rooms` `{roomNumber, building?, floor?}` → 201 CreateRoomResult.
- `POST /api/admin/rooms/{id}/revoke-token` `{reason?}` → 200 RotateRoomTokenResult.
- Cả hai `.RequireAuthorization(RequireAdmin)`. (GET list/PUT/DELETE + qr.png/pdf hoãn.)

## 7. Test (SQLite Docker-free)
- CreateRoom: tạo phòng → đúng 1 token Active + Version=1 + preview che; số phòng trùng (chưa xóa) → validation_error; token unique toàn cục (2 phòng không trùng token).
- CreateRoom sau soft-delete cùng số → OK (ux_room_number partial).
- Rotate: revoke cũ (Revoked) + tạo Active mới (Version+1); đúng 1 Active; phòng không tồn tại → not_found; phòng Inactive → qr_generation_failed.
- Validator: RoomNumber rỗng/>20 → validation_error (qua decorator).
- Endpoint authz: POST /api/admin/rooms KHÔNG token → 401 problem+json (RequireAdmin).
- (race 2 rotate đồng thời: đúng-bởi-xây-dựng + ux_qr_active → Testcontainers TK-036.)

## 8. Truy vết
- `16` §2/§3/§4/§5/§8, `04` §7.1, `14` §2.1/§2.2, Property B2/B3; Req 7.1-7.6, 16.1/16.6/16.7. Base: DEC-054/056 (unique-violation), DEC-053 (Room entity).
