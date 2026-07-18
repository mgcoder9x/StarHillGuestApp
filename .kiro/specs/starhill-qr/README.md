# starhill-qr — Spec sản phẩm StarHill QR trên Bedrock

## Nguồn sự thật

| Loại | Vị trí | Vai trò |
|---|---|---|
| WHAT | `docs/resort-qr-portal/requirements.md` | Yêu cầu nghiệp vụ EARS |
| HOW-product legacy | `docs/resort-qr-portal/design.md`, `tasks.md`, `test-plan.md` | Thiết kế/kế hoạch của monolith cũ; checkbox không đồng nghĩa đã port |
| HOW-on-Bedrock | `design.md`, `design-modules/*.md` | Thiết kế có hiệu lực cho product tree `starhill/` |
| Base vật lý duy nhất | `platform/src/` | Bedrock domain-agnostic; `starhill/` reference trực tiếp qua `$(PlatformSrc)` |
| Product tree | `starhill/` | Chỉ Host, module và test nghiệp vụ StarHill |
| Legacy để port | `resort-qr/` | Nguồn tham khảo logic; không phải runtime/product tree hiện hành |
| Journal | `journal/{01..05}.md` | AD/DV/TO/N + anti-drift INV-1..6 |

## Bất biến

- D1-a: không tái tạo/copy Bedrock vào `starhill/`; base chỉ có một bản ở `platform/src`.
- Mỗi module có 5 project, DbContext/schema/keyed persistence riêng; không FK chéo schema.
- Cross-module chỉ qua `<Module>.Contracts`; API không reference Infrastructure.
- Endpoint dùng Bedrock versioning/ProblemDetails; command khai đúng `PersistenceKey`.
- Mỗi thay đổi: design trước, provenance thật, journal + `05-anti-drift`, rồi build/test/JournalConsistency.
- Không chạm hoặc stage nested stale `StarHillGuestApp/StarHillGuestApp/`.

## Trạng thái đã kiểm chứng (2026-07-18)

- Backend StarHill có đủ **8/8 module**: Identity, ResortConfig, Rooms, GuestAccess, Rules, Faq, Housekeeping, Concierge; cascade GuestVisitEnded + SignalR + Dashboard stats đã có.
- Frontend hiện hành ở `starhill/web/`: Guest Web nền + Admin login/shell/dashboard + Rooms read/mutations + Rules Draft editor/publish/preview/history + FAQ full-tree editor. FE.4b hoàn tất trên admin read-model thật và stale-edit token contract.
- Browser gate thực: production build cả hai SPA PASS; Playwright toàn suite **42/42 PASS** sau visual QA Rules/FAQ mobile/desktop. CI có job frontend build hai SPA + Chromium + Playwright.
- Cổng local/CI đã fail-closed: thiếu .NET SDK trả FAIL/127 và test BLOCKED, không còn xanh giả; hai Python workflow validator PASS không cần PyYAML.
- Giới hạn phiên hiện tại: máy không có .NET SDK nên C# guard mới chỉ được source-review; CI phải compile/run `VerificationGateTests` + `FrontendDeliveryGuardTests`.

## Thứ tự tiếp theo

1. Tiếp tục Guest Web vertical slices thật (force-read Rules → FAQ → chat/housekeeping) trên các backend contracts đã có.
2. Sau khi backend read shape đúng, triển khai Rules editor/publish + FAQ tree theo các vertical slice nhỏ có Playwright behavior test và screenshot.
3. Khi môi trường có .NET SDK/CI chạy, xác nhận full `vp all` cả `platform/` và `starhill/` trước khi gọi baseline mới hoàn tất.
4. `foundation/` + `resort-qr/` giữ archival/reference cho tới khi có quyết định xoá riêng; không phát triển tiếp ở hai cây này.
