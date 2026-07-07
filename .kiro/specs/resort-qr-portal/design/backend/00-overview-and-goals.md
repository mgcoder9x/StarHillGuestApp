# 00 — Tổng quan & Mục tiêu chất lượng của Base (Backend)

> **File authoritative cho:** phạm vi phần base backend và các mục tiêu chất lượng.

## Phạm vi

Đây là thiết kế cho **phần nền móng (foundational base)** của backend Resort QR Portal (Star Hill Guest App). Mục tiêu: dựng một cái "base" thật vững — cấu trúc solution, phân tầng, các abstraction lõi, cross-cutting concerns, nền data model, và quy ước code — để **mọi tính năng nghiệp vụ về sau xây trên đó mà không phải sửa nền**.

Tài liệu này **không** triển khai toàn bộ nghiệp vụ (nội quy/FAQ/chat/housekeeping...). Các nghiệp vụ đó đã được đặc tả trong `docs/resort-qr-portal/requirements.md` và `docs/resort-qr-portal/design.md`; ở đây chỉ dựng nền và **chỉ ra chỗ cắm (extension points)** cho từng module nghiệp vụ.

Tài liệu kế thừa và **cải tiến** kiến trúc tham chiếu `Reference/Backend` (`FresherDev.HMS.*`). Các cải tiến đều dựa trên **bằng chứng đã kiểm chứng trong code thật** — xem `09-reference-reconciliation.md`.

## Thành phần hệ thống

- **1 Backend** ASP.NET Core (.NET 10 LTS) + EF Core + PostgreSQL, cung cấp REST API + SignalR Hub, chạy như **modular monolith**.
- **2 Frontend** Vue 3 SPA riêng biệt: **guest-web** (mobile-first, tiếng Anh mặc định, không đăng nhập) và **admin-web** (tiếng Việt, đăng nhập, phân quyền) — xem thư mục `../frontend/`.
- Chạy trong **WiFi nội bộ resort** (không public internet) — là giả định hạ tầng, **không** phải cơ chế app tự enforce.

## 5 nhóm nền móng cần chốt trước khi code nghiệp vụ

1. **Cấu trúc & phân tầng Backend** — modular monolith, dependency rule, module boundary, extension point (xem `01-architecture.md`).
2. **Các abstraction lõi** — Entity/Repository/UnitOfWork/UseCase/Result, CurrentContext, Clock, TokenGenerator, HtmlSanitizer, TranslationResolver (xem `02-core-abstractions.md`).
3. **Cross-cutting concerns** — error handling + ProblemDetails, validation pipeline, auth kép JWT/guest-cookie, rate limiting, structured logging, health check, CORS/security headers (xem `03-cross-cutting-concerns.md`).
4. **Nền data model** — base entity conventions, mẫu đa ngôn ngữ Translation, concurrency token, soft-delete, audit, partial unique index + entity nền dùng chung (xem `04-data-model.md`).
5. **Nền Frontend** — monorepo 2 SPA + shared packages (xem `../frontend/`).

## Đã chốt (kế thừa từ Decision Log của docs cũ)

Runtime **.NET 10 LTS**; DB **PostgreSQL** (Npgsql); guest web mặc định **en**; admin web **vi**; SignalR + fallback polling; QRCoder + QuestPDF; không public internet.

## Mục tiêu chất lượng (định hướng mọi quyết định)

| Mục tiêu | Ý nghĩa cụ thể trong dự án này |
|---|---|
| **Correctness-by-construction** | Bất biến quan trọng (1 token active/phòng, 1 ngôn ngữ default/resort...) enforce ở **DB constraint + domain**, không chỉ ở code service. |
| **Testability** | Mọi use case test được không cần HTTP thật; abstraction cho `IClock`, `ITokenGenerator`, `ICurrentContext` để loại bỏ tính bất định. |
| **Low coupling giữa module** | Module giao tiếp qua interface trong tầng Application, không tham chiếu chéo implementation. |
| **Explicit boundaries** | Guest surface và Admin surface tách bạch (controller, policy, DTO, rate limit) để không rò rỉ dữ liệu. |
| **Ít bất ngờ khi mở rộng** | Thêm 1 module nghiệp vụ = thêm 1 thư mục theo khuôn mẫu, không sửa Program.cs thủ công. |
| **Ergonomics** | Boilerplate tối thiểu: base class + generic repo + convention DI; nhưng không "magic" khó debug. |

## Lộ trình dựng base (thứ tự đề xuất — chi tiết ở tasks.md sau)

1. Solution skeleton 5 project + `Directory.*.props` (net10, Central Package Management) + ArchitectureTests khung.
2. SharedKernel: `Result/Error/AppErrors`, base entity, marker DI, `Guard`, `IDateTimeProvider`/`ITokenGenerator` ports.
3. Infrastructure EF: `AppDbContext` (audit/soft-delete/concurrency/snake_case), `Repository`/`UnitOfWork` **không auto-save**, **migration NỀN (foundation)**: chỉ entity nền + partial unique index của chúng (KHÔNG dựng bảng module chưa code — migration incremental per-wave, xem `04` §4), seeder.
4. Cross-cutting: ProblemDetails middleware, validation pipeline, auth kép + policy, rate limiting, Serilog, health check, CORS/headers.
5. Module nền hiện thực: Identity (login/refresh), Settings/Localization, Rooms + RoomQrToken (issue/revoke, QR PNG/PDF), GuestAccess (resolve + visit + portal window).
6. Module nghiệp vụ còn lại: **chỉ tạo khung thư mục + interface use case rỗng + khung controller** (extension point); **entity + migration của module thêm khi hiện thực** (per-wave, `04` §4) — KHÔNG dựng bảng "chết".
7. Frontend monorepo: packages + 2 app skeleton.
8. Kết nối E2E "đường xương sống": admin tạo phòng → sinh QR → guest resolve → nhận session/visit/features.
