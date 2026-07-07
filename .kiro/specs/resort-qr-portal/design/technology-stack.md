# Technology Stack — Ma trận phiên bản (đã kiểm chứng qua web)

> **File authoritative cho:** phiên bản công nghệ toàn dự án (BE + FE). Áp dụng cho cả `backend/` và `frontend/`.
>
> **Nguyên tắc thương mại:** dùng **bản stable/LTS mới nhất**, KHÔNG dùng preview/RC vào nền production. "Cao nhất" ở đây = cao nhất trong nhóm ổn định phù hợp production, không phải bleeding-edge.
>
> **Ngày kiểm chứng:** 2026-07-03 (qua web search, có nguồn). Version patch cụ thể sẽ pin ở `Directory.Packages.props` (BE) và `package.json` (FE) khi implement — xem `../ai-notes/04-things-to-know.md` TK-005.

## Backend

| Thành phần | Chọn | Trạng thái | Nguồn kiểm chứng |
|---|---|---|---|
| Runtime | **.NET 10 (LTS)** | LTS, phát hành 11/2025, hỗ trợ tới 11/2028 | [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy) |
| Ngôn ngữ | **C# 14** (đi kèm .NET 10) | stable | (đi kèm SDK .NET 10) |
| ORM | **EF Core 10 (LTS)** | LTS, hỗ trợ tới 11/2028 | [What's New EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew) |
| DB provider | **Npgsql.EntityFrameworkCore.PostgreSQL** (dòng 10.x khớp EF10) | ⚠️ verify khớp EF10 khi pin | (Npgsql theo major EF) |
| Database | **PostgreSQL 18** (mới nhất 18.4) | stable | [postgresql.org news](https://www.postgresql.org/about/news/postgresql-184-1710-1614-1518-and-1423-released-3297/) |
| Web | ASP.NET Core 10 | LTS | (đi kèm .NET 10) |

> **Vì sao KHÔNG .NET 11?** .NET 11 đang ở giai đoạn preview (đã có tài liệu breaking changes EF Core 11) — chưa stable/LTS, không đưa vào nền production thương mại. **Vì sao KHÔNG .NET 9?** .NET 9 là STS, hết hỗ trợ 10/11/2026 — không phù hợp lâu dài. ⇒ .NET 10 LTS là lựa chọn "cao nhất ổn định". Nguồn: [.NET 8/9 end of support](https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/).

## Frontend

| Thành phần | Chọn | Trạng thái | Nguồn kiểm chứng |
|---|---|---|---|
| Build tool | **Vite 8** (bundler Rolldown, Rust) | stable | [Vite 8 announcement](https://vite.dev/blog/announcing-vite8) |
| Framework | **Vue 3.5+** (bản 3.x stable mới nhất) | stable (3.6 còn preview → không dùng) | [Vue 2025/2026 review](https://vueschool.io/articles/news/vue-js-2025-in-review-and-a-peek-into-2026/) |
| Ngôn ngữ | TypeScript (bản mới nhất) | stable | — |
| State | Pinia (bản mới nhất cho Vue 3) | stable | — |
| Router | Vue Router 4 (mới nhất) | stable | — |
| i18n | vue-i18n (bản mới nhất hỗ trợ Vue 3) | stable | — |
| Node (tooling) | **Node.js 24 LTS** (Krypton) | Active LTS | [Node 24.17 LTS](https://nodejs.org/en/blog/release/v24.17.0) |
| Package manager | pnpm (mới nhất) | stable | — |
| Admin UI kit | **Element Plus** (mới nhất) | actively maintained | (xem cảnh báo PrimeVue bên dưới) |
| Test | Vitest + Playwright (mới nhất) | stable | — |

> **Vì sao ĐỔI PrimeVue → Element Plus (quan trọng):** khi kiểm chứng 2026-07-03, repo `primefaces/primevue` hiển thị **"archived by the owner on Jun 28, 2026 — read-only"** kèm issue cộng đồng "Is this project still on track?". Chọn UI lib vừa bị archive cho sản phẩm thương mại là rủi ro bảo trì cao. ⇒ chuyển sang **Element Plus** (đang bảo trì tích cực, phổ biến, TS tốt). Nguồn: [primefaces/primevue GitHub](https://github.com/primefaces/primevue). ⚠️ Trạng thái archive của một lib lớn khá bất thường (có thể do dời repo); **re-verify khi implement** — xem `../ai-notes/04-things-to-know.md` TK-017. *Nội dung được diễn giải lại để tuân thủ giới hạn trích dẫn.*

## Vì sao "cao nhất" = stable/LTS (không phải preview)

Với sản phẩm thương mại lâu dài: bản LTS/stable có vá bảo mật dài hạn, hệ sinh thái thư viện tương thích, ít breaking change bất ngờ. Preview/RC (.NET 11, Vue 3.6) chỉ nên dùng để thử nghiệm, không đưa vào nền. Đây là fix gốc cho "an toàn lâu dài", không chạy theo con số cao nhất bằng mọi giá.

## Audit giấy phép dependency (bắt buộc cho sản phẩm thương mại — đã verify web 2026-07-03)

| Thư viện | License | Kết luận cho commercial | Nguồn |
|---|---|---|---|
| **AutoMapper** | Thương mại/RPL 1.5 (từ 2/7/2025, Lucky Penny Software) | ❌ **KHÔNG dùng** → thay bằng Mapperly | [jimmybogard.com](https://www.jimmybogard.com/automapper-and-mediatr-commercial-editions-launch-today/), [dotnetfoundation.org](https://dotnetfoundation.org/news-events/detail/automapper-graduates-from-the-.net-foundation) |
| **MediatR** | Thương mại (cùng đợt) | ❌ Không dùng (base vốn không dùng MediatR — dùng use-case interface trực tiếp) | như trên |
| **Riok.Mapperly** | **Apache-2.0** (miễn phí, đã verify file LICENSE — Copyright 2022 riok GmbH), source-generator | ✅ Thay AutoMapper (nhanh hơn, không reflection, không tốn phí) | [LICENSE](https://raw.githubusercontent.com/riok/mapperly/main/LICENSE) |
| **FluentValidation** | Apache-2.0 (vẫn open source; xin sponsor) | ✅ Dùng được | [github FluentValidation](https://github.com/FluentValidation/FluentValidation/) |
| **FluentAssertions** | Thương mại từ v8 (Xceed) | ❌ **KHÔNG dùng** trong test (base dùng xUnit + NSubstitute, không FluentAssertions) | [aaronstannard.com](https://aaronstannard.com/relicense-or-die/) |
| **QuestPDF** | Community MIT nếu doanh thu < $1M/năm; trên ngưỡng phải mua | ⚠️ **Dùng có điều kiện** — hợp lệ giai đoạn đầu; nếu vượt $1M/năm phải mua license (90 ngày ân hạn) | [questpdf.com/license](https://www.questpdf.com/license/community.html) |
| **QRCoder** | MIT | ✅ | — |
| **Scrutor / Serilog / Npgsql / Ganss.Xss** | MIT/Apache | ✅ | — |
| **Element Plus / Vue / Vite / Pinia** | MIT | ✅ | — |

> **Bài học (fix gốc):** với sản phẩm thương mại, **audit license là bước bắt buộc** trước khi chốt dependency — không chỉ chọn theo độ phổ biến. Hệ .NET gần đây có làn sóng thương mại hoá (AutoMapper, MediatR, MassTransit, FluentAssertions). ⚠️ Re-verify license tại thời điểm implement (điều khoản có thể đổi tiếp) — `../ai-notes/04-things-to-know.md` TK-019. *Nội dung diễn giải lại để tuân thủ giới hạn trích dẫn.*
