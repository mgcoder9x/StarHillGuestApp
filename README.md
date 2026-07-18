# StarHillGuestApp — bản đồ nguồn sự thật

| Cây | Trạng thái | Quy tắc |
|---|---|---|
| `platform/` | **Base Bedrock hiện hành** | Nguồn code domain-agnostic duy nhất; phát triển base tại đây. |
| `starhill/` | **Sản phẩm StarHill hiện hành** | Host/module/test nghiệp vụ + `web/`; reference base qua `$(PlatformSrc)`. |
| `foundation/` | **Legacy/archival** | Blueprint và bản dựng cũ đã được hấp thụ; không sửa để làm tính năng mới. |
| `resort-qr/` | **Legacy/reference** | Nguồn logic/UI cũ để đối chiếu khi port; không phải runtime hiện hành. |
| `.kiro/specs/platform-base/` | Spec/journal base | `design.md` + journal là hồ sơ quyết định; audit mới nhất: `current-audit-2026-07-18.md`. |
| `.kiro/specs/starhill-qr/` | Spec/journal sản phẩm | Design module + journal QR; trạng thái hiện tại tới FE.3b. |

Không xoá `foundation/` hoặc `resort-qr/` trong một thay đổi phát triển thông thường. Chỉ xoá bằng quyết định riêng sau khi kiểm tra mọi tham chiếu/rationale cần giữ.

## Cổng nhanh

```powershell
# Base: build + CI validator + test (fail-closed)
platform\scripts\vp.cmd all

# StarHill backend: build + CI validator + test (fail-closed)
starhill\scripts\vp.cmd all

# Frontend StarHill
pnpm --dir starhill/web build
pnpm --dir starhill/web e2e
```

Nếu máy thiếu .NET SDK, hai lệnh `vp all` phải FAIL/127 và BLOCK test; đó là hành vi đúng, không phải lỗi cần che.
