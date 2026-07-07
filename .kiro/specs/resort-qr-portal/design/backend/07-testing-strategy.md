# 07 — Testing Strategy & Dependencies

> **File authoritative cho:** chiến lược test phần nền và danh sách dependency pin.

## Chiến lược test

| Loại | Phạm vi ở base | Công cụ |
|---|---|---|
| **Unit (Domain/Application)** | use case với port giả lập (clock/token/sanitizer); invariant token/ngôn ngữ/cửa sổ phiên | xUnit + NSubstitute |
| **Property-based** | sinh token duy nhất; fallback ngôn ngữ luôn có giá trị; cửa sổ phiên luôn có thể hết hạn; unread không âm | **FsCheck** (hoặc CsCheck) |
| **Integration (API+EF)** | resolve hợp lệ/revoked không lộ phòng; partial unique index thực sự chặn (2 token active, 2 default lang...); concurrency → 409; guest không gọi được admin | xUnit + **Testcontainers PostgreSQL** |
| **Architecture** | dependency rule + naming | NetArchTest |
| **Frontend unit** | api-client ánh xạ ProblemDetails→ApiError; resolve ngôn ngữ | Vitest |

> Property-based test đặc biệt hợp với các invariant "by construction" của base (đơn điệu unread, an toàn token, fallback). Sẽ được liệt kê thành task PBT ở `tasks.md`.

## Dependencies (pin qua Central Package Management)

- **Backend:** `Microsoft.AspNetCore.App` (net10), `Npgsql.EntityFrameworkCore.PostgreSQL`, `Scrutor`, `FluentValidation` (open source), `Riok.Mapperly` (mapping, **Apache-2.0 miễn phí** — **thay AutoMapper đã chuyển thương mại**; hoặc map thủ công), `Serilog.AspNetCore`, `QRCoder`, `QuestPDF` (⚠️ license theo doanh thu — xem `../technology-stack.md`), `Ganss.Xss` (HtmlSanitizer), `Microsoft.AspNetCore.SignalR`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Konscious.Security.Cryptography` (Argon2) hoặc ASP.NET Identity hasher.
- **Test:** `xunit`, `NSubstitute`, `FsCheck.Xunit`, `Testcontainers.PostgreSql`, `NetArchTest.Rules`, `Microsoft.AspNetCore.Mvc.Testing`.
- **Frontend:** `vue`, `vue-router`, `pinia`, `vue-i18n`, `@microsoft/signalr`, `@vueuse/core`; admin: `element-plus`; dev: `vite`, `typescript`, `vitest`, `@playwright/test`, `eslint`, `prettier`, `vue-tsc`.

> **Ma trận phiên bản (đã verify web):** xem `../technology-stack.md` — nguồn chân lý về version (.NET 10, EF Core 10, PostgreSQL 18, Vite 8, Vue 3.5+, Node 24 LTS).
> ⚠️ **Version patch cụ thể** pin ở `Directory.Packages.props` (BE) / `package.json` (FE) khi implement. Xem `../../ai-notes/04-things-to-know.md` TK-005, TK-018.
