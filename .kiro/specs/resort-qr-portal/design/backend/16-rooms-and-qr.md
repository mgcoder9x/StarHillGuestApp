# 16 — Rooms & QR Token (module nền: cấp/thu hồi token, sinh QR/PDF)

> **File authoritative cho:** hành vi module Rooms + RoomQrToken của base — CRUD phòng, cấp/thu hồi (rotate) token có xử lý race, sinh QR PNG, xuất PDF nhãn hàng loạt, phân quyền.
>
> Base **hiện thực** module này (đủ để "admin tạo phòng → sinh QR → guest resolve" — đường xương sống). Đặc tả kỹ vì đây là nơi giữ hai bất biến sống còn: **1 token Active/phòng** và **token duy nhất toàn cục** (resolve không nhập nhằng).

## 1. Phân quyền (Req 7.6)

- **Admin**: tạo/sửa/vô hiệu/soft-delete phòng; sinh/thu hồi QR; xuất PDF. (policy `RequireAdmin`)
- **Staff**: chỉ **xem** danh sách/thông tin phòng phục vụ vận hành. (policy `RequireStaff` cho GET)
- Enforce ở tầng Api (policy) **và** không có đường vòng: mọi mutation phòng/token nằm sau `RequireAdmin`.

## 2. Room CRUD

- Thuộc tính: `RoomNumber` (1–20 ký tự), `Building` (≤50), `Floor` (−10..200), `Status ∈ {Active, Inactive, Maintenance}` (Req 16.1).
- **Unique số phòng trong resort (trừ bản đã soft-delete):**
  ```sql
  CREATE UNIQUE INDEX ux_room_number ON room(resort_id, room_number) WHERE is_deleted = false;
  ```
  > Lý do partial `WHERE is_deleted=false`: cho phép "xóa mềm" phòng cũ rồi tạo lại cùng số phòng, nhưng không cho hai phòng **đang sống** trùng số. Trùng số khi tạo/sửa → `validation_error` (Req 16.6).
- **Soft-delete** (`ISoftDeletable`): "xóa" chỉ set `IsDeleted` — giữ token/visit/lịch sử, không vỡ FK (`04` §7.3).

## 3. Cấp token khi tạo phòng (issue)

```pascal
ALGORITHM IssueTokenForNewRoom(room, now, actorUserId)
BEGIN
  token ← GenerateUniqueToken()                 // §5, chống trùng
  add RoomQrToken { RoomId=room.Id, Token=token, TokenPreview=mask(token),
                    Status='Active', Version=1, CreatedAt=now, CreatedByUserId=actorUserId }
  saveChanges()                                  // ux_qr_active + ux_qrtoken_token bảo vệ
END
```

- `TokenPreview` = dạng che (ví dụ 6 ký tự đầu + `…`) để hiển thị/đối soát mà **không lộ token đầy đủ** (Req 11.6). Cột `Token` lưu **plaintext** (đây là *capability token* nằm sẵn trong URL QR công khai — không phải bí mật kiểu mật khẩu; MVP không cần hash, docs Security). Nhưng **không log** `Token` đầy đủ (mask ở log — `03` §6).

## 4. Thu hồi & cấp lại (rotate) — nguyên tử + chịu race

```pascal
ALGORITHM RotateToken(roomId, reason, now, actorUserId)
PRE: room tồn tại (policy RequireAdmin)
BEGIN
  RETURN uow.ExecuteInTransactionAsync( token =>
    current ← query RoomQrToken WHERE RoomId=roomId AND Status='Active' LIMIT 1
    IF current != null THEN
       current.Status ← 'Revoked'; current.RevokedAt ← now
       current.RevokedByUserId ← actorUserId; current.RevocationReason ← reason
    newToken ← GenerateUniqueToken()
    add RoomQrToken { RoomId=roomId, Token=newToken, Status='Active',
                      Version=(current?.Version ?? 0)+1, CreatedAt=now, CreatedByUserId=actorUserId }
    saveChanges()          // có thể ném UniqueViolation(ux_qr_active) nếu race
  )
CATCH UniqueViolation(ux_qr_active):
    // 2 admin revoke đồng thời: chỉ 1 token Active mới được commit
    RETURN error(qr_generation_failed)   // hoặc retry 1 lần rồi trả token Active hiện hành
POST:
  - Token cũ giữ lại (Status=Revoked) — lịch sử "QR nào đã in" (Req 7.4), KHÔNG xóa cứng.
  - Sau rotate tồn tại ĐÚNG 1 token Active/phòng (ux_qr_active).
  - QR cũ (URL chứa token cũ) → resolve trả `qr_revoked` (Req 1.4).
END
```

> **Vì sao nguyên tử (một transaction):** "hạ cờ token cũ" + "tạo token mới Active" phải cùng thành công hoặc cùng thất bại; nếu tách hai lần lưu, một sự cố giữa chừng để phòng **không có token Active nào** (QR dán chết) hoặc **hai Active** (nhập nhằng). Dùng `IUnitOfWork.ExecuteInTransactionAsync` (`02` §3).
> **Vì sao dựa DB race, không lock app:** `ux_qr_active` là trọng tài cuối cùng; hai transaction đua nhau chỉ một cái commit được token Active mới, cái kia rollback — không cần distributed lock.

## 5. Sinh token duy nhất (chống trùng — Req 7.5)

```pascal
ALGORITHM GenerateUniqueToken()   // kết hợp app-retry + DB unique
BEGIN
  FOR attempt IN 1..5
     candidate ← tokenGenerator.NewPublicToken(bytes=32)   // CSPRNG base64url (05 §1)
     IF NOT exists RoomQrToken WHERE Token=candidate THEN RETURN candidate
  RETURN error(qr_generation_failed)   // cực hiếm (256-bit); dùng code có trong catalog 14, KHÔNG tạo code mới
END
```

- Kiểm tra tồn tại là **best-effort** (giảm va chạm); `ux_qrtoken_token` (`04` §7.1) là chốt chặn thực sự khi insert (chịu cả race). Xác suất trùng 256-bit ~ 0 nên 5 lần là quá đủ.

## 6. Sinh QR PNG (Req 7.2, 16.2)

```pascal
ALGORITHM RenderRoomQrPng(roomId)
BEGIN
  baseUrl ← ResortSettings.GuestWebBaseUrl
  IF baseUrl thiếu OR không phải https hợp lệ THEN RETURN error(invalid_configuration)   // Req 15.6
  room ← get room; IF room.Status != 'Active' THEN RETURN error(qr_generation_failed)     // Req 16.7
  token ← active token của room
  url ← $"{baseUrl}/r/{token}"
  png ← qrService.RenderPng(url)     // QRCoder; ≤5s
  RETURN ok(png)
END
```

- URL dựng từ `ResortSettings.GuestWebBaseUrl` (Req 14.3) — **không hardcode host**.
- QR **chỉ chứa URL** (không chứa số phòng trần — Req 1.6).

## 7. Xuất PDF nhãn hàng loạt (Req 7.3, 16.3, 16.8)

```pascal
ALGORITHM RenderRoomLabelsPdf(roomIds[])
BEGIN
  IF roomIds.Count > 500 THEN RETURN error(pdf_limit_exceeded)      // Req 16.8
  labels ← for each room Active: { roomNumber, qrImage(url), logo, hướng dẫn }
  pdf ← pdfService.RenderRoomLabels(labels)     // QuestPDF; ≤30s
  RETURN ok(pdf)
END
```

- Mỗi nhãn: **ảnh QR + số phòng + logo + text hướng dẫn**. **Không in URL/token dài** (docs — tránh lộ token dạng chữ dễ chép).
- ⚠️ QuestPDF license theo doanh thu (`technology-stack.md` audit; TK-019).

## 8. API (khớp docs)

| Method | Endpoint | Quyền |
|---|---|---|
| GET | `/api/admin/rooms`, `/rooms/{id}` | Staff, Admin |
| POST/PUT/DELETE | `/api/admin/rooms`, `/rooms/{id}` | Admin |
| GET | `/api/admin/rooms/{id}/qr.png` | Admin |
| POST | `/api/admin/rooms/qr-labels.pdf` `{ roomIds[] }` | Admin |
| POST | `/api/admin/rooms/{id}/revoke-token` `{ reason? }` | Admin |

## 9. Truy vết
- **Validates: Requirements 1.1, 1.5, 1.6, 7.1–7.6, 14.3, 15.6, 16.x**
- Bất biến: `04` §7.1 (`ux_qrtoken_token`, `ux_qr_active`), Property B2 (nguyên tử rotate), B3 (unique ở DB).
