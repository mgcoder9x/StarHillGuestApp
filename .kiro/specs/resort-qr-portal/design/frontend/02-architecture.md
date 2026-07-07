# 02 — Kiến trúc nền Frontend (2 SPA Vue 3, chuẩn thương mại)

> **File authoritative cho:** kiến trúc FE base — monorepo, stack, layering, guest-web, admin-web, cross-cutting, security, testing, performance. Đồng bộ hợp đồng với backend (`../backend/`).

## 1. Stack đã chốt (khuyến nghị chuyên gia)

| Hạng mục | Chọn | Lý do (commercial, lâu dài) |
|---|---|---|
| Framework | Vue 3.5+ (Composition API, `<script setup>`) | Hiện đại; reference Vue 2 đã EOL (3.6 còn preview → không dùng) |
| Ngôn ngữ | TypeScript strict | An toàn kiểu, đồng bộ hợp đồng backend |
| Build | Vite 8 (Rolldown) | Nhanh nhất hiện tại, chuẩn Vue 3 |
| State | Pinia (mới nhất) | TS-first, gọn, chuẩn mới |
| Router | Vue Router 4 | Guard theo role/session |
| i18n | vue-i18n (mới nhất Vue 3) | Tách UI-text vs content |
| Monorepo | pnpm workspaces (Node 24 LTS) | Chia app + package dùng chung, cài nhanh |
| Admin UI | Element Plus (mới nhất) | DataTable/form mạnh; đổi từ PrimeVue do repo PrimeVue archive 28/6/2026 (TRD-007) |
| Guest UI | Tối giản (component tự viết + CSS/Tailwind nhẹ) | Bundle nhỏ, mobile-first |
| HTTP | fetch wrapper typed | Nhẹ, kiểm soát ProblemDetails tập trung |
| Realtime | @microsoft/signalr + fallback polling | Khớp backend Hub |
| Test | Vitest + Vue Test Utils; Playwright (E2E) | Chuẩn Vite; E2E cho luồng quan trọng |
| Type sinh | từ OpenAPI backend | Chống lệch hợp đồng BE↔FE (TRD-006) |

## 2. Cấu trúc monorepo

```text
/frontend
  package.json                 # pnpm workspaces
  pnpm-workspace.yaml
  tsconfig.base.json           # strict: true, noUncheckedIndexedAccess...
  .eslintrc / .prettierrc      # lint chung
  packages/
    api-client/                # SDK REST typed; map ProblemDetails → ApiError{code}; gửi cookie
    shared-types/              # type/enum dùng chung (ErrorCode khớp AppErrors, DTO) — sinh từ OpenAPI
    realtime/                  # wrapper SignalR (reconnect + fallback polling), event trừu tượng
    ui-kit/                    # design tokens + component tối thiểu dùng chung (Button, Field, Toast)
  apps/
    guest-web/                 # mobile-first, en mặc định, KHÔNG auth
    admin-web/                 # vi, đăng nhập, Element Plus, phân quyền
```

**Vì sao tách 2 app:** guest cần bundle cực nhẹ tải nhanh trên điện thoại; admin cần nhiều component dashboard. Tách app tách bundle lẫn bề mặt tấn công. Dùng chung qua `packages/*` để không lặp code. (Ngược hẳn reference: một app khổng lồ, đăng ký toàn cục — FE-E7/E8.)

## 3. Layering trong mỗi app

```text
apps/<app>/src/
  app/          # bootstrap: main.ts, App.vue, plugins (i18n, router, pinia, element-plus)
  router/       # routes + guards
  stores/       # Pinia stores (state + actions gọi api-client)
  views/        # màn hình (route-level)
  components/   # component cục bộ app
  composables/  # logic tái dùng (useXxx)
  locales/      # i18n JSON theo ngôn ngữ
  assets/
```

**Quy tắc:** view gọi store; store gọi `api-client`/`realtime` (packages); component "ngu" nhận props/emit. **Không** gọi `fetch` trực tiếp trong component (giống backend: đi qua abstraction).

## 4. Cross-cutting: api-client & hợp đồng lỗi

```typescript
// packages/api-client — hình dạng lỗi thống nhất với backend AppErrors
export class ApiError extends Error {
  constructor(public code: string, public status: number, message: string,
              public fields?: Record<string, string[]>) { super(message); }
}

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    credentials: 'include',                 // gửi cookie guest/refresh
    headers: { 'Content-Type': 'application/json', ...(init?.headers ?? {}) },
    ...init,
  });
  if (!res.ok) {
    const pd = await res.json().catch(() => ({} as any));   // ProblemDetails
    throw new ApiError(pd.code ?? 'unexpected', res.status, pd.title ?? res.statusText, pd.errors);
  }
  return res.status === 204 ? (undefined as T) : res.json();
}
```

- **Interceptor lỗi chung** ánh xạ theo `code` ổn định (Property B5):
  - `session_expired` → guest: điều hướng màn "quét lại QR"; admin: thử refresh, thất bại → `/login`.
  - `rule_ack_required` → mở RuleGate.
  - `rate_limited` (429) → toast + tôn trọng `retry-after`.
  - `concurrency_conflict` → thông báo "nội dung đã đổi, tải lại".
- `shared-types` chứa union `ErrorCode` **sinh/đồng bộ từ backend** để FE không đoán chuỗi.

## 5. i18n nền

- Tách **UI text** (JSON trong app) và **content** (từ API, có cờ `isFallback`).
- guest-web resolve ngôn ngữ theo thứ tự: `?lang=` → localStorage → `navigator.language` (chuẩn hóa `ko-KR`→`ko`) → default resort (`en`); nếu ngôn ngữ không được bật → fallback `en`.
- admin-web: UI tiếng Việt (`vi`) là chính; kiến trúc vẫn cho thêm ngôn ngữ sau.

## 6. Realtime wrapper

`packages/realtime` bọc `@microsoft/signalr`:
- Tự reconnect (backoff, tối đa số lần cấu hình — khớp Req 17.6).
- **Fallback polling** khi WebSocket lỗi (guest poll `GET /conversation`, chu kỳ cấu hình — Req 17.7).
- Chỉ expose event trừu tượng (`onMessageReceived`, `onHousekeepingUpdated`, `onConversationUpdated`); UI không đụng chi tiết Hub. Bỏ `socket.io` thừa của reference (FE-E11).

## 7. guest-web (mobile-first, bundle nhỏ)

- **Auth:** không đăng nhập; dựa cookie `GuestSession` (backend phát). Không lưu token/phòng ở client.
- **Routing:**
  ```text
  /r/:token → ResolveLoading → (TokenError) → (RuleGate nếu chưa ack) → GuestHome → { RulesViewer, FaqFlow, Chat }
  ```
- **Store nền:** `useSessionStore` (resolve response: room/resort/features/visit expiry/ngôn ngữ), `useI18nStore`. Store nghiệp vụ (rules/faq/chat/housekeeping) = **khung rỗng** cho wave sau.
- **Performance:** lazy-load route nghiệp vụ; SignalR/chat chỉ tải khi vào mục nhắn tin (docs Req 13.2); nút lớn, contrast tốt, dùng một tay; tránh UI kit nặng.
- Base chỉ dựng skeleton: ResolveLoading, TokenError, GuestHome shell, LangSwitcher, error boundary.

## 8. admin-web (đăng nhập, phân quyền)

- **Auth:** `useAuthStore` giữ access token **trong memory** (không localStorage — chống XSS token theft); refresh qua cookie HttpOnly; tự refresh khi 401 rồi retry; thất bại → `/login`.
- **Guard router theo policy** `RequireAdmin`/`RequireStaff` (khớp backend). Kế thừa **ý tưởng** permission của reference (CASL) nhưng base dùng role guard đơn giản; CASL tùy chọn sau (TRD-008).
- **Routing (skeleton):**
  ```text
  /login → [guard] /dashboard, /inbox, /rules, /faq, /housekeeping, /notes,
           /rooms (Admin), /users (Admin), /settings (Admin)
  ```
- **UI:** Element Plus (layout, table, form, dialog). StaffScan dùng `html5-qrcode` (camera, cần secure context — docs Req 12.5).
- Base chỉ dựng: layout (sidebar/topbar/breadcrumb), Login, Dashboard shell, guard, i18n `vi`, error boundary, api-client tích hợp refresh. Màn nghiệp vụ để wave sau.

## 9. Security phía FE (commercial)

- Access token **chỉ trong memory**; refresh token là cookie HttpOnly (JS không đọc được).
- Không nhét bí mật vào bundle; `.env` chỉ chứa base URL/flags công khai.
- Render nội dung do nội bộ nhập: tin tưởng backend đã sanitize (Property B7), FE vẫn tránh `v-html` không kiểm soát; nếu cần dùng, chỉ với nội dung đã sanitize từ API.
- CSP do server đặt (`../backend/03` §... / `10` §2); FE tránh inline script để tương thích CSP.
- Bỏ pattern nạp route động từ config runtime của reference (FE-E10) — rủi ro bảo mật/bảo trì.

## 10. Testing & chất lượng

- **Vitest + Vue Test Utils:** api-client (map ProblemDetails→ApiError), resolve ngôn ngữ, guard router, store logic.
- **Playwright (E2E, khuyến nghị):** luồng xương sống guest (resolve → gate → home) và admin (login → dashboard).
- ESLint + Prettier + `vue-tsc` (type-check) bắt buộc xanh trong CI.
- Bundle budget cho guest-web (theo dõi kích thước) — mobile-first.

## 11. Đồng bộ hợp đồng với backend (chống lệch)

- `shared-types` sinh từ **OpenAPI** backend (TRD-006): DTO, enum, và **tập `ErrorCode`** khớp `AppErrors`.
- CI có bước kiểm tra type FE khớp OpenAPI mới nhất để phát hiện lệch hợp đồng sớm.

## 12. Phạm vi base FE (rõ ràng)

Base dựng: monorepo + packages (api-client, shared-types, realtime, ui-kit), 2 app skeleton (bootstrap/router/guard/i18n/error boundary/LangSwitcher/layout admin), tích hợp auth admin + session guest, và luồng "xương sống" resolve. **Không** dựng chi tiết từng màn nghiệp vụ (để wave sau, sau khi backend module tương ứng sẵn sàng).
