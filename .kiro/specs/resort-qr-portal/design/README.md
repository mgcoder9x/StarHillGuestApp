# Thiết kế Resort QR Portal — Bản đồ tài liệu (Design Map)

> Đây là **chỉ mục điều hướng** cho toàn bộ thiết kế "base". Nội dung chi tiết nằm trong hai thư mục con `backend/` và `frontend/`. File `../design.md` là bản tổng quan cấp cao (master overview) cho Kiro spec workflow; **nguồn chân lý chi tiết theo từng chủ đề là các file trong hai thư mục dưới đây** (mỗi chủ đề có đúng một file authoritative để tránh trùng lặp/sai lệch).

## Nguyên tắc tổ chức

1. **Một chủ đề = một file authoritative.** Không lặp nội dung giữa các file (tránh divergence khi sửa).
2. **Truy vết được (verifiable).** Mọi khẳng định về code tham chiếu đều dẫn chứng file/thành phần cụ thể trong `Reference/Backend`.
3. **Sửa tận gốc.** Mỗi cải tiến so với reference nêu rõ "Bản chất vấn đề" trước khi nêu "Giải pháp".
4. **Base ≠ full feature.** Tài liệu này dựng nền + chỉ ra extension point; logic nghiệp vụ (Rules/FAQ/Chat/Housekeeping) triển khai ở wave sau, không đụng nền.

## Ma trận công nghệ (toàn dự án)

| File | Nội dung |
|------|----------|
| [technology-stack.md](./technology-stack.md) | **Phiên bản đã verify web** (.NET 10 LTS, EF Core 10, PostgreSQL 18, Vite 8, Vue 3.5+, Node 24 LTS, Element Plus) + lý do "stable/LTS = cao nhất" |

## Backend (`backend/`)

| # | File | Nội dung |
|---|------|----------|
| 00 | [00-overview-and-goals.md](./backend/00-overview-and-goals.md) | Phạm vi base, 6 mục tiêu chất lượng định hướng mọi quyết định |
| 01 | [01-architecture.md](./backend/01-architecture.md) | Modular monolith, dependency rule, cấu trúc solution 5 project, module boundary, HTTP pipeline, DI convention |
| 02 | [02-core-abstractions.md](./backend/02-core-abstractions.md) | Entity nền, `Result<T>`/Error, Repository/UnitOfWork, UseCase, cross-cutting ports (code C# thật) |
| 03 | [03-cross-cutting-concerns.md](./backend/03-cross-cutting-concerns.md) | ProblemDetails, validation pipeline, EF audit/soft-delete/concurrency, auth kép, rate limit, logging, health, CORS |
| 04 | [04-data-model.md](./backend/04-data-model.md) | Quy ước entity, translation pattern, entity nền + module, DB partial unique index, seed |
| 05 | [05-algorithms-and-specs.md](./backend/05-algorithms-and-specs.md) | Thuật toán nền + đặc tả hình thức (token, SaveChanges nguyên tử, resolve + portal window) |
| 06 | [06-conventions.md](./backend/06-conventions.md) | Coding standards, naming, async, guard, kiểm soát kiến trúc |
| 07 | [07-testing-strategy.md](./backend/07-testing-strategy.md) | Unit/PBT/Integration/Architecture test, dependencies pin |
| 08 | [08-correctness-properties.md](./backend/08-correctness-properties.md) | 10 correctness property của base (truy vết Requirements) |
| 09 | [09-reference-reconciliation.md](./backend/09-reference-reconciliation.md) | Đối chiếu "giữ gì / sửa gì" — **đã kiểm chứng** với code thật |
| 10 | [10-production-hardening.md](./backend/10-production-hardening.md) | Chuẩn thương mại: secrets, bảo mật vận hành, quyền riêng tư/vòng đời dữ liệu, audit, migration/release governance, CI/CD, backup/DR, API versioning, threat model |
| 11 | [11-architecture-review-and-gaps.md](./backend/11-architecture-review-and-gaps.md) | **Audit kiến trúc**: traceability 20 req, verdict, 8 gap tìm được + cách xử lý, rủi ro còn lại |
| 12 | [12-identity-and-auth.md](./backend/12-identity-and-auth.md) | **Auth sâu**: Argon2id hashing, JWT claims/validation, refresh rotation + reuse detection, login/lockout, ranh giới guest/admin |
| 13 | [13-guest-access-flows.md](./backend/13-guest-access-flows.md) | **Luồng GuestAccess đầy đủ**: resolve, visit lifecycle, portal window, lazy idle-expiry, xử lý race, EndVisit cascade, ma trận 12 edge-case |
| 14 | [14-error-catalog.md](./backend/14-error-catalog.md) | **Catalog mã lỗi ổn định** (hợp đồng BE↔FE): code → ErrorType → HTTP → khi nào phát → FE xử lý |
| 15 | [15-configuration-and-options.md](./backend/15-configuration-and-options.md) | **Cấu hình**: nguồn & precedence, strongly-typed Options, **validate-on-startup fail-fast**, tách secret, appsettings vs ResortSettings |
| 16 | [16-rooms-and-qr.md](./backend/16-rooms-and-qr.md) | **Rooms & QR**: CRUD phòng, cấp/thu hồi token nguyên tử + chịu race, sinh QR PNG, xuất PDF nhãn |
| 17 | [17-test-plan-base.md](./backend/17-test-plan-base.md) | **Test plan nền**: ma trận Property B1–B10 → test cụ thể + Given/When/Then + PBT + DoD |
| 18 | [18-localization.md](./backend/18-localization.md) | **Localization**: `ITranslationResolver` fallback, chuẩn hóa mã ngôn ngữ (primary-subtag), phát hiện thiếu dịch |
| 19 | [19-observability.md](./backend/19-observability.md) | **Observability**: catalog trường log, correlation (qua SignalR), mask bí mật chính xác, health live/ready |
| 20 | [20-deployment-reverse-proxy.md](./backend/20-deployment-reverse-proxy.md) | **Deployment**: same-origin routing, Caddy/Nginx (WebSocket), TLS/HSTS, align ForwardedHeaders, migration khi deploy |
| 21 | [21-realtime-signalr.md](./backend/21-realtime-signalr.md) | **SignalR Hub**: auth kép (cookie guest/JWT admin qua query WS), authorize JoinConversation (server trọng tài), hợp đồng sự kiện, notify post-commit |
| 22 | [22-consistency-audit.md](./backend/22-consistency-audit.md) | **Cross-consistency audit**: mâu thuẫn tìm được & đã sửa, ma trận bất biến chéo, đối chiếu con số, traceability |
| 23 | [23-solution-skeleton.md](./backend/23-solution-skeleton.md) | **Solution skeleton**: cấu trúc file, Directory.Build.props, Central Package Management, tham chiếu csproj (enforce dependency rule), AssemblyMarker, design-time DbContext factory |
| 24 | [24-tenancy-model.md](./backend/24-tenancy-model.md) | **Tenancy**: phân tích single vs multi-tenant, quyết định **instance-per-resort** (khớp WiFi nội bộ), đường mở multi-tenant + chi phí |

## Frontend (`frontend/`)

| # | File | Nội dung |
|---|------|----------|
| — | [README.md](./frontend/README.md) | Trạng thái **ACTIVE** + stack đã chốt |
| 01 | [01-reference-assessment.md](./frontend/01-reference-assessment.md) | Khảo sát `EPS.Vuexy` — **đã kiểm chứng** (Vue 2 EOL) + phán quyết không port |
| 02 | [02-architecture.md](./frontend/02-architecture.md) | Kiến trúc nền: monorepo 2 SPA, stack, layering, guest/admin, cross-cutting, security, testing |
| 03 | [03-guest-web-flows.md](./frontend/03-guest-web-flows.md) | **guest-web**: state machine màn hình, session store, resolve ngôn ngữ, ánh xạ ErrorCode→UI, RuleGate |
| 04 | [04-admin-auth-guards.md](./frontend/04-admin-auth-guards.md) | **admin-web**: auth store (token in-memory), silent refresh single-flight, route guard theo Role |
| 05 | [05-monorepo-skeleton.md](./frontend/05-monorepo-skeleton.md) | **Monorepo skeleton**: pnpm workspaces, tsconfig strict, packages, sinh type từ OpenAPI, build/base-path, lint/test |
| 06 | [06-i18n-and-content.md](./frontend/06-i18n-and-content.md) | **i18n & content**: UI text vs content, isFallback, render HTML an toàn, thời gian UTC→timezone resort, CJK |

## Nhật ký kiểm chứng của AI (`../ai-notes/`)

Thư mục xuyên suốt, cập nhật liên tục để kiểm chứng về sau:

- `01-autonomous-decisions.md` — quyết định AI tự ra mà spec không nói.
- `02-deviations-from-spec.md` — chỗ AI phải đổi so với yêu cầu ban đầu.
- `03-tradeoffs.md` — các trade-off đã cân nhắc.
- `04-things-to-know.md` — bất kỳ điều gì cần biết (giả định, rủi ro, việc chưa xác minh).
