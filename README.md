# StarHillGuestApp — bản đồ nguồn sự thật

| Cây | Trạng thái | Quy tắc |
|---|---|---|
| `platform/` | **Base Bedrock hiện hành** | Nguồn code domain-agnostic duy nhất; phát triển base tại đây. |
| `starhill/` | **Sản phẩm StarHill hiện hành** | Host/module/test nghiệp vụ + `web/`; reference base qua `$(PlatformSrc)`. |
| `.kiro/specs/platform-base/` | Spec/journal base | `design.md` + journal là hồ sơ quyết định; audit mới nhất: `current-audit-2026-07-18.md`. |
| `.kiro/specs/starhill-qr/` | Spec/journal sản phẩm | Design module + journal QR; trạng thái hiện tại tới FE.4. |
| `docs/resort-qr-portal/` | Spec nghiệp vụ nguồn | Requirements/design/tasks/CP gốc của sản phẩm (giữ — nguồn nghiệp vụ, không phải code). |

**Một phiên bản duy nhất.** Các cây legacy `foundation/` (blueprint + base cũ đã hấp thụ) và `resort-qr/`
(app cũ standalone để port) ĐÃ ĐƯỢC XOÁ (2026-07-18) sau khi xác minh không project/CI/solution nào của
`platform/`+`starhill/` tham chiếu. Rationale/quyết định port vẫn còn nguyên trong `.kiro/specs/` +
`docs/resort-qr-portal/` (không xoá). Cần khôi phục thì lấy lại từ git history.

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
