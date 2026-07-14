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
| Journal | `journal/{01..05}.md` | AD/DV/TO/N + anti-drift INV-1..5 |

## Bất biến

- D1-a: không tái tạo/copy Bedrock vào `starhill/`; base chỉ có một bản ở `platform/src`.
- Mỗi module có 5 project, DbContext/schema/keyed persistence riêng; không FK chéo schema.
- Cross-module chỉ qua `<Module>.Contracts`; API không reference Infrastructure.
- Endpoint dùng Bedrock versioning/ProblemDetails; command khai đúng `PersistenceKey`.
- Mỗi thay đổi: design trước, provenance thật, journal + `05-anti-drift`, rồi build/test/JournalConsistency.
- Không chạm hoặc stage nested stale `StarHillGuestApp/StarHillGuestApp/`.

## Trạng thái đã kiểm chứng

- Base hardening và D1-a/P0 remediation đã hoàn tất; Identity, ResortConfig, Rooms đang ở product tree.
- Rooms admin/query API và ResortConfig settings API đã hoàn tất (journal tới QR-AD-023).
- GuestAccess C-GA.1..3 đã có trong `starhill/`: 5-project/schema/key riêng, resolve transaction hẹp + PostgreSQL
  row-lock race guard, public POST/cookie/Host/compose/CI wiring (QR-N-024..027).
- Reconciliation C-GA.3a đang xử lý: canonical input/log guard, response contract và migration history per-schema.
- Design có hiệu lực: `design-modules/03-guestaccess.md`; Task 5 trong docs chỉ phản ánh legacy.

## Thứ tự tiếp theo

1. Hoàn tất/verify C-GA.3a bằng build + full test Docker + Compose/DB + JournalConsistency.
2. Thiết kế Rules trước code; khi Rules là consumer đầu, triển khai GuestAccess C-GA.4 current-context/sweeper.
3. C-GA.5 cascade end-visit chỉ triển khai cùng consumer Concierge/Housekeeping, dùng outbox/inbox at-least-once.
4. GPU không liên quan tới .NET/PostgreSQL/RabbitMQ.
