# Vertical slice: Render QR PNG cho phòng — design-first

> Bám `16` §6 (RenderRoomQrPng), `02` §5 (port `IQrService`), Req 1.6/7.2/14.3/15.6/16.2/16.7. Test Docker-free (SQLite + kiểm bytes PNG).

## 0. Phạm vi
**Trong slice:** render **QR PNG** cho token Active của phòng qua endpoint admin. Đây là output khép kín trải nghiệm "admin sinh QR → dán → khách quét /r/{token}".
**Hoãn:** PDF nhãn hàng loạt (QuestPDF — license theo doanh thu TK-019) → increment riêng.

## 1. Thư viện: QRCoder (`PngByteQRCode`)
- Chọn **QRCoder** + renderer **`PngByteQRCode`** — sinh PNG **thuần managed, KHÔNG phụ thuộc System.Drawing/SkiaSharp** → chạy đa nền tảng (đúng lý do dùng .NET Core, không kén OS/native lib). ⚠️ verify version/API thật bằng `dotnet add` + reflection (no-fabrication).
- QR **chỉ chứa URL** `{GuestWebBaseUrl}/r/{token}` — KHÔNG chứa số phòng trần (Req 1.6).

## 2. Port `IQrService` (`02` §5)
```
public interface IQrService : ISingletonService { byte[] RenderPng(string url); }
```
- Impl `QrCoderQrService` (Infrastructure) — `PngByteQRCode(generator.CreateQrCode(url, ECCLevel.Q)).GetGraphic(pixelsPerModule)`. Sync (CPU-bound, ≤5s Req 16.2; thực tế ms). Auto-đăng ký (ISingletonService, ngoài namespace Persistence).
- ECC Level Q (~25% phục hồi) — cân bằng độ bền QR dán (trầy/bẩn) vs mật độ. pixelsPerModule=20 (ảnh đủ nét in nhãn).

## 3. `RenderRoomQrPngUseCase` (`16` §6)
```
IUseCase<RenderRoomQrPngInput(RoomId), RenderRoomQrPngResult(byte[] Png)>
- room = repo<Room>.FindById(RoomId); null → not_found; Status != Active → qr_generation_failed (Req 16.7)
- settings = repo<ResortSettings>.FirstOrDefault(s => s.ResortId == room.ResortId)
- baseUrl = settings?.GuestWebBaseUrl
- IF baseUrl thiếu OR không phải https tuyệt đối hợp lệ → invalid_configuration (Req 15.6)
- token = repo<RoomQrToken>.FirstOrDefault(t => t.RoomId==RoomId && Status==Active); null → qr_generation_failed
- url = $"{baseUrl.TrimEnd('/')}/r/{token.Token}"
- png = qrService.RenderPng(url)
- RETURN Ok(png)
```
- Validate baseUrl: `Uri.TryCreate(baseUrl, Absolute) && scheme==https` (Req 15.6 — "https hợp lệ"). Không hardcode host (Req 14.3).

## 4. Errors (∈ catalog `14`)
`RoomsErrors.InvalidConfiguration` = invalid_configuration (Validation 400, `14` §2.2); dùng lại QrGenerationFailed, RoomNotFound.

## 5. Endpoint (`16` §8) — RequireAdmin
`GET /api/admin/rooms/{id}/qr.png`:
- success → `image/png` (bytes); failure → ProblemDetails.
- Cache-Control: no-store (token nhạy cảm, không cache proxy).

## 6. Test (SQLite Docker-free)
- success: seed room Active + token + settings.GuestWebBaseUrl="https://guest.local" → PNG bytes non-empty + đúng chữ ký PNG (89 50 4E 47).
- GuestWebBaseUrl thiếu/không-https (http://...) → invalid_configuration.
- room Inactive → qr_generation_failed; room không tồn tại → not_found; phòng Active nhưng không token Active → qr_generation_failed.
- (Endpoint HTTP: hoãn happy-path — cần admin JWT; authz 401 đã bao ở AdminRoomAuthzTests pattern. Có thể thêm 401 cho qr.png.)

## 7. Truy vết
- `16` §6/§8, `02` §5, `14` §2.2; Req 1.6/7.2/14.3/15.6/16.2/16.7. Base: DEC-057 (Rooms), DEC-056 (token).
