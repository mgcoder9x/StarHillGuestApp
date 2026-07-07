# Frontend Design — Nền tảng "Base" (2 SPA Vue 3)

## Trạng thái: ACTIVE (thiết kế lại từ đầu, chuẩn thương mại)

User chỉ đạo: reference FE tại `Reference/EPS.Vuexy` **chỉ để tham khảo**, "code kỹ sư viết rất kém", cần **xử lý lại như chuyên gia**. Đã khảo sát & kiểm chứng reference (xem `01-reference-assessment.md`). Kết luận: **KHÔNG port code**; thiết kế lại trên stack hiện đại.

## Nguyên tắc

1. Một chủ đề = một file authoritative (giống backend).
2. Mọi khẳng định về reference đều dẫn chứng file cụ thể (đã kiểm chứng).
3. Base FE = nền + skeleton màn hình; logic nghiệp vụ từng màn để wave sau.
4. Đồng bộ hợp đồng với backend: `AppErrors.code`, `/resolve` response, auth kép.

## File

| # | File | Nội dung |
|---|------|----------|
| 01 | [01-reference-assessment.md](./01-reference-assessment.md) | Khảo sát `EPS.Vuexy` — sự thật đã kiểm chứng + phán quyết "giữ ý tưởng gì / bỏ gì" |
| 02 | [02-architecture.md](./02-architecture.md) | Kiến trúc nền: monorepo 2 SPA + packages, stack đã chốt, layering, guest-web, admin-web, cross-cutting, security, testing, performance |

## Stack đã chốt (khuyến nghị chuyên gia — chi tiết & lý do ở 02)

- Vue 3.5+ + **TypeScript** + **Vite 8** + **Pinia** + Vue Router 4 + vue-i18n (mới nhất) — chi tiết version ở `../technology-stack.md`.
- Monorepo **pnpm workspaces**: 2 app (`guest-web`, `admin-web`) + packages (`api-client`, `shared-types`, `realtime`, `ui-kit`). Node.js 24 LTS.
- Admin UI kit: **Element Plus** (đổi từ PrimeVue — repo PrimeVue đã archive 28/6/2026; xem `../technology-stack.md`).
- guest-web: **tối giản**, bundle nhỏ, mobile-first (không dùng UI kit nặng).
- Realtime: `@microsoft/signalr` + fallback polling.
- Test: Vitest + Vue Test Utils; E2E Playwright (khuyến nghị).
- Type FE sinh từ OpenAPI của backend (đồng bộ hợp đồng).

> Ma trận phiên bản đầy đủ (đã verify web): xem `../technology-stack.md`.
>
> Các câu hỏi mở trước đây đã được giải quyết bằng khuyến nghị chuyên gia và ghi ở `../../ai-notes/` (DEC-012..DEC-015, TRD-007..TRD-008). User có thể phủ nhận từng điểm.
