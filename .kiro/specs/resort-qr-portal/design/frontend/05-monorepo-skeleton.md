# 05 — Frontend Monorepo Skeleton (pnpm workspaces, tsconfig, packages, type-gen)

> **File authoritative cho:** bộ khung monorepo FE cụ thể (đối xứng backend `23`) — cấu trúc workspace, `tsconfig` base, hình dạng package dùng chung, sinh type từ OpenAPI, lint/format/test, build/base-path. Nền cho Req 13, 17.
>
> **No-fabrication:** không hardcode số version — placeholder + trỏ `../technology-stack.md` (đã verify major: Vue 3.5+, Vite 8, Node 24 LTS, Element Plus) để pin khi implement (TK-005/018).

## 1. Vì sao monorepo pnpm (bản chất)

- **pnpm workspaces**: hai app + packages dùng chung trong **một repo**, cài nhanh (content-addressable store, hard-link), version dependency nhất quán. Chia sẻ `api-client`/`shared-types`/`realtime` **không lặp code** giữa guest và admin (khác reference: một app khổng lồ đăng ký toàn cục — FE-E7/E8).
- Tách **2 app build riêng** → bundle guest cực nhẹ (mobile-first, Req 13), admin đầy đủ; tách cả bề mặt tấn công.

## 2. Cấu trúc file

```text
/frontend
  package.json                 # root: private, scripts orchestrate qua pnpm --filter
  pnpm-workspace.yaml
  tsconfig.base.json           # strict; các package/app extends
  .eslintrc / eslint.config.js # flat config
  .prettierrc  .npmrc  .nvmrc  # .nvmrc pin Node 24 LTS
  packages/
    shared-types/     # ErrorCode union (khớp AppErrors `14`), DTO types — SINH từ OpenAPI (§5)
    api-client/       # apiFetch + ApiError + typed endpoints; depends shared-types
    realtime/         # wrapper @microsoft/signalr (reconnect + fallback polling)
    ui-kit/           # component + design tokens tối giản dùng chung (guest ưu tiên nhẹ)
  apps/
    guest-web/        # Vite base '/'; en mặc định; KHÔNG auth; bundle nhỏ
    admin-web/        # Vite base '/admin/'; vi; Element Plus; auth
```

```yaml
# pnpm-workspace.yaml
packages:
  - 'packages/*'
  - 'apps/*'
```

## 3. `tsconfig.base.json` (strict — bắt lỗi kiểu sớm)

```jsonc
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "Bundler",
    "strict": true,                          // bật toàn bộ strict
    "noUncheckedIndexedAccess": true,        // truy cập mảng/obj an toàn hơn
    "noImplicitOverride": true,
    "exactOptionalPropertyTypes": true,
    "verbatimModuleSyntax": true,
    "skipLibCheck": true,
    "types": []
  }
}
```

> **Lý do strict + `noUncheckedIndexedAccess` (chính xác):** FE là nơi hợp đồng dễ lệch với BE; TS strict bắt lỗi null/undefined/kiểu **lúc build** (đối xứng `TreatWarningsAsErrors` của backend `23`). `verbatimModuleSyntax` tránh nhầm import type/value.

## 4. Quy tắc phụ thuộc package (dependency rule FE)

```
apps/guest-web ─┐
apps/admin-web ─┼─→ packages/api-client ─→ packages/shared-types
                ├─→ packages/realtime
                └─→ packages/ui-kit ─→ packages/shared-types (nếu cần)
```

- **apps** phụ thuộc **packages**; **packages KHÔNG** phụ thuộc apps (một chiều — giống dependency rule backend).
- `api-client` phụ thuộc `shared-types` (dùng `ErrorCode`, DTO). Không vòng.
- ESLint rule `import/no-cycle` + boundaries để chặn phụ thuộc sai chiều.

## 5. Sinh type từ OpenAPI (chống lệch hợp đồng BE↔FE) — TRD-006

- Backend expose `/openapi/v1.json` (`10` §8). FE dùng generator (ví dụ `openapi-typescript`) sinh `packages/shared-types` từ đó → DTO + enum **khớp backend tự động**.
- **`ErrorCode` union** khớp catalog `14` (§5 của `14`): test hợp đồng so `AppErrors` (BE reflection) ↔ `ErrorCode` (FE) → CI phát hiện lệch (thêm/bớt code mà quên đồng bộ).
- Bước sinh type chạy trong CI (hoặc script `pnpm gen:types`); commit kết quả để build tất định.

## 6. Build & base-path (khớp deployment `20`)

- `guest-web`: `vite build`, `base: '/'` → static phục vụ ở `/` (proxy `20` §2). SPA fallback `index.html`.
- `admin-web`: `vite build`, **`base: '/admin/'`** → asset path đúng khi phục vụ dưới `/admin` (nếu sai base → asset 404 sau deploy — lỗi phổ biến).
- Env công khai qua `import.meta.env.VITE_*` (ví dụ `VITE_API_BASE_URL`); **FE không chứa secret** (chỉ giá trị công khai). `.env` theo môi trường; `.env.production` không secret.
- Output static → **proxy phục vụ** (không nhúng vào ASP.NET) — decouple, cache tốt (`20` §6).

## 7. Lint / format / test / typecheck (gate CI)

- **ESLint** (flat config) + `eslint-plugin-vue` + `@typescript-eslint` + `import/no-cycle`; **Prettier** format.
- **`vue-tsc --noEmit`** typecheck (bắt lỗi template + type) — gate bắt buộc.
- **Vitest** unit (api-client map ProblemDetails→ApiError, resolveLang, guard, store); **Playwright** E2E luồng xương sống (guest resolve→gate→home; admin login→dashboard).
- Bundle budget cho `guest-web` (theo dõi kích thước — mobile-first).

## 8. Definition of Done (khung FE)
- `pnpm install` + `pnpm -r build` xanh; `vue-tsc` không lỗi.
- `shared-types` sinh từ OpenAPI + test hợp đồng ErrorCode xanh.
- ESLint/Prettier/`import/no-cycle` xanh; admin build với `base '/admin/'` chạy sau proxy.
- Test đối chiếu error-code (B5) xanh.

## 9. Truy vết
- **Validates: Requirements 13.1–13.3, 17.1–17.9**
- Align: backend `20` (deploy/base-path), `14` (error codes), `10` §8 (OpenAPI), `../technology-stack.md`.
