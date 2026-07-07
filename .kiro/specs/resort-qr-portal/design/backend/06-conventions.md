# 06 — Quy ước & Coding Standards

> **File authoritative cho:** naming, async, guard, kiểm soát kiến trúc.

## Backend

- **Namespace/thư mục** theo module: `ResortQr.Application.Modules.GuestAccess`, `...Infrastructure.Modules.GuestAccess`.
- **Đặt tên:** interface `I{Name}`; use case `I{Verb}{Noun}UseCase` + impl `{Verb}{Noun}UseCase`; DTO input `{Verb}{Noun}Input`, output `{Noun}Response`.
- **Async:** mọi I/O `async` + `CancellationToken` propagate tới tận repo (khắc phục thiếu sót reference).
- **Không** trả entity domain ra API — luôn map sang DTO bằng **Mapperly** (source-generator, Apache-2.0 miễn phí) hoặc mapping thủ công gọn. **KHÔNG dùng AutoMapper** (đã chuyển license thương mại 7/2025).
- **Không** dùng `DateTime.Now`/`UtcNow` trực tiếp — dùng `IDateTimeProvider`.
- **Nullable enable**, warnings-as-errors cho nhóm nullability/async.
- **Guard clauses** đầu use case; lỗi nghiệp vụ trả `Result.Fail(AppErrors.*)`, không throw.
- **ArchitectureTests** (NetArchTest) chặn vi phạm dependency rule (Domain không tham chiếu EF, Application không tham chiếu Api...).
- **SharedKernel phải sạch tuyệt đối:** ArchitectureTest thêm rule `ResortQr.SharedKernel` **không** reference EF Core / ASP.NET / Infrastructure (vì Domain phụ thuộc SharedKernel — GAP-7).
- **Realtime post-commit:** không gọi `IRealtimeNotifier` bên trong transaction; chỉ gọi sau khi commit thành công (GAP-4).
- **Mapping:** dùng Mapperly hoặc thủ công; cấm AutoMapper (license thương mại).

## Frontend

- ESLint + Prettier + `vue-tsc` strict; component `PascalCase`; store `use{Name}Store`.
- Chi tiết nền frontend xem `../frontend/01-frontend-base.md`.

## Kiểm soát kiến trúc tự động (bắt buộc CI xanh)

- Domain **không** reference `Microsoft.EntityFrameworkCore`.
- Application **không** reference `ResortQr.Api` hay `ResortQr.Infrastructure`.
- Mọi implementation port nằm ở `Infrastructure` (không ở Application).
- Tên use case theo pattern `*UseCase`; validator theo `*Validator`.
