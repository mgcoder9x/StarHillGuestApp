# Module design — Frontend (Wave FE): 2 SPA Vue 3 + responsive cực đỉnh mọi thiết bị

> **Design-first, CHƯA triển khai code.** WHAT: `docs/resort-qr-portal/requirements.md` Req 2 (đa ngôn ngữ + auto-detect),
> Req 3 (force-read nội quy), Req 4/5/6/7/8/9 (FAQ cây / chat realtime / housekeeping / phòng+QR / nội quy Draft-Publish /
> dashboard vận hành), Req 11 (role Staff/Admin), Req 12 (deploy same-origin nội bộ) + `tasks.md` task 12-18 (Guest Web +
> Admin Dashboard). Stack đã CHỐT với user: **PrimeVue (MIT) + layout tự dựng** (không template dựng sẵn). Mọi kết luận
> dựa trên file/lệnh đã ĐỌC/CHẠY (mục §0). Đây là pha FE — BE 8/8 module + cascade CP9 đã xong.

## 0. Đối soát nguồn (đã verify trên đĩa/web — KHÔNG suy đoán)

- **PrimeVue = MIT** (verify raw `github.com/primefaces/primevue/master/LICENSE.md`: "The MIT License (MIT) Copyright (c) 2018-2025 PrimeTek" + cam kết "Existing MIT versions remain MIT, forever"). Element Plus/Naive UI cũng MIT (đã verify) — dự phòng.
- **PrimeVue v4 theming** (verify primevue.org/theming/styled): styled mode = base (CSS-var placeholder) + preset design-token (Aura/Lara/Nora); tùy biến qua `definePreset` + Pass Through (`pt`) + unstyled option; cầu Tailwind chính thức `tailwindcss-primeui`.
- **starhill chưa có FE** (grep `package.json|*.vue|vite.config` trong `starhill/` = 0 — chỉ có ở `Reference/EPS.Vuexy` [Vuexy — ThemeForest THƯƠNG MẠI, Bootstrap-Vue, KHÔNG dùng] và `resort-qr/frontend/apps/admin-web` [FE cũ pnpm — tham chiếu port logic]). FE StarHill là **greenfield**.
- **Host KHÔNG cấu hình CORS** (grep `AddCors|UseCors|WithOrigins` Program.cs = 0) → chủ trương **same-origin** (guest + admin + api + hub cùng origin sau reverse proxy). Dev khác origin → dùng **Vite proxy** (không bật CORS).
- **BE Api đã có** (đọc journal + code): Identity `/v1/token/*`; Rooms `/v1/rooms*` (+qr.png); ResortConfig settings/languages; GuestAccess `/v1/guest/resolve` (cookie `__Host-starhill_guest`); Rules guest+admin; Faq guest tree+admin; Housekeeping guest+admin board; Concierge guest `/v1/guest/conversation`+`/messages` & admin conversations/notes; SignalR hub `/hubs/chat` (JWT-qua-query cho staff, AllowAnonymous+per-method auth). **Dashboard stats `GET /dashboard/stats` (task 11) CHƯA có** → làm BE trước khi ráp trang KPI.
- **Realtime**: SignalR (client `@microsoft/signalr`). Guest join hội thoại của mình; staff join theo role + board. Fallback polling luôn có.

## 1. Mục tiêu và bất biến

1. **RESPONSIVE CỰC ĐỈNH MỌI THIẾT BỊ (bất biến #1, Req 1/12)**: guest quét QR bằng **vô số điện thoại khác nhau** (Android cũ 320px → phone lớn 430px → gập/foldable → tablet), URL-bar co giãn, tai thỏ/notch, bàn phím ảo che input. Layout phải KHÔNG vỡ, KHÔNG scroll ngang, chạm dễ, chữ đọc được ở mọi bề rộng. Đây là gốc rễ (§3).
2. **Hai SPA tách biệt (Req/tasks)**: **Guest Web** (mobile-first, tiếng Anh mặc định + auto-detect, ẩn danh cookie) và **Admin Dashboard** (đăng nhập JWT, tiếng Việt, data-dense). Chia sẻ design-token + api-client + i18n qua package chung.
3. **Same-origin (Req 12.5, CP1)**: prod sau reverse proxy `/`→guest, `/admin`→admin, `/v1`→api, `/hubs`→SignalR. Cookie `__Host-` + SignalR không cần CORS. Dev: Vite proxy.
4. **Force-read nội quy (Req 3, CP3)**: guest phải cuộn hết + xác nhận mới mở FAQ/chat/housekeeping — enforce bằng backend rule-gate (đã có) + UI flow.
5. **A11y + i18n**: WCAG cơ bản (touch target ≥44px, contrast, focus-visible, reduced-motion, KHÔNG khóa zoom); vue-i18n (guest en/vi/ko/zh + auto-detect; admin vi).
6. **Realtime + fallback**: SignalR đẩy nhanh; polling là nguồn sự thật (khớp thiết kế BE K-Con.4).
7. **Bảo mật FE (sản phẩm thương mại)**: KHÔNG render `innerHTML` cho nội dung khách/chat (dùng `textContent` — khớp quyết định body-plain-text QR-AD-043); nội quy/FAQ đã sanitize server (QR-AD-031) → render an toàn có kiểm soát; token JWT lưu trong bộ nhớ + refresh cookie httpOnly (không localStorage cho access-token nhạy cảm nếu tránh được).
8. **Verify bằng browser thật (anti-drift FE)**: **Playwright** (headless đa-viewport) làm cổng — chính là "mở web bằng browser phát hiện lỗi" (§7). Chống drift FE tương đương INV-1..6 của BE.

## 2. Kiến trúc & cấu trúc thư mục

```text
starhill/web/                      (pnpm workspace — mirror resort-qr/frontend, tách khỏi .NET solution)
  package.json (workspaces)        pnpm-workspace.yaml  tsconfig.base.json
  packages/
    shared/                        design-token (CSS vars + fluid scale), api-client (fetch wrapper + refresh),
                                   signalr-client, i18n core, responsive composables (useViewport/useSafeArea)
  apps/
    guest-web/    Vite + Vue 3 + TS + PrimeVue + vue-i18n + vue-router (mobile-first, en default)
    admin-web/    Vite + Vue 3 + TS + PrimeVue + Pinia + vue-router + vue-i18n + signalr (vi, data-dense)
  e2e/            Playwright (responsive matrix + no-overflow + a11y + console-error) — cổng anti-drift FE
```

- **Stack CHỐT**: Vite 5 + Vue 3.5 (`<script setup>` TS) + PrimeVue 4 (styled Aura preset) + **Tailwind CSS v4** (layer LAYOUT/responsive: grid/flex/spacing/container-query/safe-area) qua `tailwindcss-primeui` (đồng bộ token) + Pinia (admin state) + vue-router + vue-i18n + `@microsoft/signalr`. Lý do Tailwind: tầng utility responsive mạnh nhất (container query, `dvh`, arbitrary value) + MIT + hòa PrimeVue token — layout tự dựng nhanh mà vẫn gọn/sở-hữu (xem §9 trade-off; có thể bỏ Tailwind, dùng CSS-token thuần nếu muốn ít dep hơn).
- PrimeVue cấp: DataTable (phòng/housekeeping board, paging/sort/filter), Tree/TreeTable (FAQ cây), **Editor** rich-text (rule editor đa ngữ), Chart (KPI), Dialog/Drawer/Toast/Form/DatePicker/FileUpload — phủ gần hết nhu cầu từ MỘT thư viện MIT (ít dep → an toàn/bảo trì).

## 3. RESPONSIVE STRATEGY — cực sâu cho "vô số điện thoại khác nhau" (TRỌNG TÂM)

> Nguyên tắc gốc: **fluid-first + container-query**, KHÔNG chỉ breakpoint cứng. Layout thích ứng theo KÍCH THƯỚC THẬT của khung
> chứa (container) chứ không chỉ theo viewport → bền vững ở MỌI bề rộng trung gian (foldable, split-screen, tablet dọc/ngang)
> mà không cần liệt kê từng thiết bị. Breakpoint chỉ là "tinh chỉnh", không phải nền tảng.

### 3.1 Viewport meta + đơn vị chiều cao động (diệt bug 100vh mobile)
- `<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover, interactive-widget=resizes-content">`.
  - `viewport-fit=cover` → dùng được vùng notch (kèm safe-area §3.3). `interactive-widget=resizes-content` → bàn phím ảo co nội dung thay vì đè (quan trọng cho ô chat).
  - **KHÔNG** đặt `maximum-scale=1`/`user-scalable=no` — vi phạm a11y (khách lớn tuổi cần zoom).
- Chiều cao màn hình full dùng **`100dvh`** (dynamic viewport height) — tự đúng khi URL-bar hiện/ẩn; fallback tầng: `min-height:100vh; min-height:100svh; min-height:100dvh` (trình cũ nhận vh, trình mới nhận dvh). `svh`/`lvh` cho trường hợp cần cận nhỏ/lớn.

### 3.2 Type scale + spacing FLUID bằng clamp()
- Chữ + khoảng cách nội suy mượt theo bề rộng: `font-size: clamp(<min>, <preferred-vw + rem>, <max>)`. Ví dụ base body `clamp(0.95rem, 0.9rem + 0.4vw, 1.125rem)`. → KHÔNG "nhảy bậc" giữa breakpoint; đọc tốt từ 320px đến tablet.
- Đơn vị **rem** (tôn trọng cỡ chữ hệ thống + zoom người dùng). KHÔNG px cho font. Thang type & spacing là design-token dùng chung (packages/shared).

### 3.3 Safe-area insets (notch / home-indicator / punch-hole)
- Padding khung cố định (header/bottom-nav/nút nổi) cộng `env(safe-area-inset-*)`: `padding-block-start: max(<base>, env(safe-area-inset-top))`, tương tự bottom/left/right. Kết hợp `viewport-fit=cover`. → không bị tai thỏ/thanh gạt che nội dung/nút.

### 3.4 Container queries (component thích ứng theo khung chứa, không theo viewport)
- Mỗi khối bố cục lớn đặt `container-type: inline-size`. Component con dùng `@container (min-width: …)` để đổi layout. Ví dụ: card KPI 1 cột khi container hẹp → 2/4 cột khi rộng; hàng tin nhắn đổi avatar/縮 khi hẹp. → Admin sidebar thu/mở, split-pane inbox... đều đúng ở mọi bề rộng vùng nội dung (không phụ thuộc tổng viewport).

### 3.5 Layout primitives (Grid + Flex, intrinsic responsive)
- **Auto-fit grid** cho lưới thẻ: `grid-template-columns: repeat(auto-fit, minmax(min(100%, <ideal>), 1fr))` → tự rớt cột khi hẹp, không media query. `min(100%, …)` chống tràn ở 320px.
- **Flex wrap** + `gap` cho toolbar/filter. Không dùng width cố định px; dùng `%`/`fr`/`ch`/`minmax`.
- **KHÔNG scroll ngang** (bug responsive #1): global `img,video,table,pre{max-width:100%}`, `overflow-wrap:anywhere` cho text dài (mã token, URL), bảng data-dense dùng `overflow-x:auto` CỤC BỘ trong wrapper (không đẩy tràn cả trang). Playwright assert `scrollWidth<=clientWidth` mọi viewport (§7).

### 3.6 Breakpoint refinement (bộ tối giản — chỉ tinh chỉnh)
- Tập nhỏ: `xs<480` (phone), `sm 480–767` (phone lớn/phablet), `md 768–1023` (tablet dọc), `lg≥1024` (tablet ngang/desktop admin). Dùng cho ĐỔI KHUNG lớn (guest: 1 cột luôn; admin: sidebar overlay <md → cố định ≥lg). Nội dung bên trong vẫn fluid/container-query.

### 3.7 Guest Web (mobile-first tuyệt đối)
- Bố cục 1 cột, `100dvh` app-shell: header phòng (safe-area) + nội dung cuộn + thanh hành động dưới (safe-area bottom). Chạm ≥44px.
- **Chat**: dùng `interactive-widget=resizes-content` + `visualViewport` composable (`useKeyboardInset`) để cuộn ô nhập lên trên bàn phím; danh sách tin `overflow-y:auto` trong vùng `dvh` trừ input.
- **Force-read nội quy**: vùng cuộn phát hiện chạm đáy (`IntersectionObserver` sentinel) → bật nút xác nhận; hoạt động mọi chiều cao màn hình.
- **QR/ảnh**: `srcset`/DPR-aware (retina nét); QR PNG từ BE hiển thị `image-rendering:pixelated` khi phóng.
- Font hệ thống stack (không tải web-font nặng → nhanh trên phone yếu/WiFi nội bộ): `system-ui, -apple-system, "Segoe UI", Roboto, ...` + fallback chữ CJK cho ko/zh.

### 3.8 Admin Dashboard (data-dense, vẫn responsive tablet/phone)
- App-shell: sidebar (Drawer overlay <md, cố định ≥lg) + topbar (safe-area) + nội dung container-query.
- **DataTable** PrimeVue: `scrollable` + `responsiveLayout="scroll"` (cuộn ngang cục bộ) HOẶC chuyển card-list <sm cho phòng/board (đọc được trên phone của lễ tân đi lại). Inbox split-pane (list|thread) ≥md → stack <md (list → mở thread toàn màn).
- KPI cards: auto-fit grid (§3.5). Charts `responsive:true` + ResizeObserver.

### 3.9 Preferences người dùng (prefers-*)
- `prefers-color-scheme` → dark/light (PrimeVue Aura + token `.p-dark`); `prefers-reduced-motion` → tắt animation/transition không thiết yếu; `prefers-contrast` → tăng tương phản. Tất cả qua CSS media + token, không JS nặng.

### 3.10 Hiệu năng trên phone yếu (WiFi nội bộ nhưng máy cũ)
- Route-level code-split (dynamic import) + PrimeVue tree-shakeable import từng component; lazy-load Chart/Editor (nặng) chỉ khi vào trang cần. Ảnh lazy `loading="lazy"`. Ngân sách: guest-web bundle nhỏ (mobile). Không polyfill thừa (target trình duyệt hiện đại — nội bộ kiểm soát thiết bị hợp lý; nêu rõ min-browser trong §9).

## 4. Design tokens & theming
- **Token dùng chung** (`packages/shared/tokens.css`): fluid type scale, spacing scale, radius, color (light/dark), z-index, breakpoint vars. PrimeVue Aura preset map vào `--p-*`; Tailwind v4 `@theme` map cùng token → PrimeVue + Tailwind + CSS-thuần đồng bộ MỘT nguồn token (chống drift màu/spacing).
- Tùy biến PrimeVue qua `definePreset(Aura, {...})` (brand color StarHill) + `pt` cho ca lẻ; tránh `!important`/override CSS bừa (khớp best-practice đã verify).

## 5. Mối quan tâm chung (packages/shared)
- **api-client**: fetch wrapper — base `/v1`, tự đính JWT (admin, từ Pinia in-memory), tự refresh khi 401 (gọi `/v1/token/refresh`, cookie httpOnly), map ProblemDetails → lỗi có `code` để i18n. Guest: gửi cookie `__Host-starhill_guest` (same-origin, `credentials:'include'` không cần vì same-origin).
- **signalr-client**: `HubConnectionBuilder` `/hubs/chat`; staff truyền `accessTokenFactory` (JWT-qua-query — khớp K-Con.4); auto-reconnect; guest join hội thoại mình; fallback: nếu hub fail → polling `GET /v1/guest/conversation` (đã có).
- **i18n**: vue-i18n; guest auto-detect `navigator.language` → map en/vi/ko/zh, fallback default (en); admin cố định vi. Chuỗi UI ở JSON FE (Req 2.4); nội dung DB do BE trả theo ngôn ngữ (isFallback).
- **responsive composables**: `useViewport()` (matchMedia breakpoint reactive), `useSafeArea()`, `useKeyboardInset()` (visualViewport), `usePrefersReducedMotion()`.

## 6. Kiến trúc thông tin (IA) theo vai trò
- **Guest Web** (Req 3-6): Resolve (quét QR → phòng) → Home (nội quy bắt buộc trước) → FAQ (cây) → Chat → Housekeeping. Cửa sổ thao tác ~30' (quá → nhắc quét lại). Không đăng nhập.
- **Admin Dashboard** (Req 7-9, role guard): Dashboard KPI (task 18.4) · Rooms+QR (Admin CRUD / Staff xem) · Rules editor Draft→Publish (Admin) · FAQ (Admin) · Inbox theo phòng realtime (Staff+Admin) · Housekeeping board · Notes · Settings (Admin). Route guard theo role (QR-AD-005: Admin superset Staff).

## 7. Verification — Playwright là "browser thật phát hiện lỗi" (anti-drift FE)
- **Cổng responsive** `e2e/`: chạy mỗi SPA qua **ma trận viewport** {320×568 (Android nhỏ nhất thực tế), 360×640, 375×667 (iPhone SE), 390×844, 414×896, 430×932 (phone lớn), 280×653 (Galaxy Fold gập), 768×1024 (tablet dọc), 1024×1366, 1280×800 (admin desktop)} + DPR 2/3. Mỗi viewport assert:
  1. **KHÔNG scroll ngang**: `document.scrollingElement.scrollWidth <= clientWidth` (bug #1).
  2. **Touch target** ≥44px cho nút/link chính (bounding box).
  3. **Không lỗi console** (page.on('console'/'pageerror')) — bắt lỗi runtime JS.
  4. **Ảnh chụp visual** per-viewport (regression) — phát hiện vỡ layout.
  5. **A11y smoke** (axe-core): landmark/contrast/label cơ bản.
  6. **Bàn phím ảo** (chat): focus input → assert input còn trong vùng nhìn.
- Chạy headless trong CI (thêm job FE vào `starhill-ci.yml`) + local. Đây là tầng chống drift design↔UI (song song INV-1..6 của BE). "Mở web bằng browser phát hiện cực nhiều lỗi" ↔ đúng vai trò Playwright.
- **Lưu ý máy hiện tại**: chưa cài Node/pnpm/Playwright → slice FE.0 sẽ cài + verify; nếu môi trường thiếu, ghi rõ "chạy CI/máy có Node" (trung thực, không giả xanh).

## 8. Build slices (từng bước chắc chắn, mỗi slice verify + journal)
1. **FE.0 — Nền + design system + cổng Playwright:** pnpm workspace `starhill/web/` + `packages/shared` (tokens fluid + responsive composables) + Vite/Vue/TS/PrimeVue/Tailwind cho 1 app khung + Playwright responsive gate (no-overflow trên ma trận viewport) chạy được. CHỐT stack + token trước khi xây trang. **Dashboard BE (task 11 stats)** làm trong slice này (template-independent) để trang KPI có API.
2. **FE.1 — Guest Web:** resolve → force-read nội quy → FAQ → chat (SignalR) → housekeeping. Responsive + i18n auto-detect. Playwright guest matrix.
3. **FE.2 — Admin auth + shell:** login JWT + route guard role + app-shell responsive (sidebar overlay/fixed) + i18n vi.
4. **FE.3 — Admin Rooms+QR** (DataTable + QR dialog + in PDF/PNG + rotate token).
5. **FE.4 — Admin Rules (editor Draft→Publish) + FAQ (tree)**.
6. **FE.5 — Admin Inbox realtime + Housekeeping board + Notes + Dashboard KPI + Settings**.
7. **FE.6 — Deploy same-origin:** reverse proxy (nginx/caddy) HOẶC Host phục vụ static SPA (`/`,`/admin`) + compose + CI FE job. Chốt ở §9.

Mỗi slice dừng nếu: build FE lỗi/warning; Playwright fail (overflow/console-error/a11y); lệch token; drift design↔UI.

## 9. Quyết định/trade-off (ghi journal khi chốt code)
- **QR-AD-0xx (stack FE)**: PrimeVue 4 (MIT, styled Aura) + Tailwind v4 (layout/responsive) + Vite+Vue3+TS+Pinia+router+vue-i18n+@microsoft/signalr. Lý do: component MIT phủ rộng (ít dep) + Tailwind cho responsive utility mạnh nhất. Trade-off: thêm Tailwind = 1 dep tooling; bù lại tốc độ + độ bền responsive. Phương án B: bỏ Tailwind, CSS-token thuần (ít dep, chậm hơn khi dựng layout).
- **QR-AD-0xx (responsive fluid-first + container-query, dvh, safe-area, no-maximum-scale)**: nền tảng thích ứng mọi thiết bị không liệt kê từng máy; a11y-zoom giữ. Trade-off: container-query + dvh cần trình duyệt hiện đại (Safari iOS 15.4+/Chrome 105+) — nội bộ resort kiểm soát thiết bị hợp lý; nêu min-browser. 
- **QR-AD-0xx (Playwright responsive gate)**: browser-substitute cho anti-drift FE (no-overflow/console/a11y/visual). Trade-off: cần Node/Playwright trong CI (thêm job).
- **QR-AD-0xx (serve SPA)**: same-origin — chốt Host-static (đơn giản, 1 instance nội bộ 60 phòng) vs nginx sidecar (tách build) ở FE.6.
- **QR-TO-0xx**: DataTable phone → cuộn-ngang-cục-bộ vs chuyển card-list <sm (chốt lúc code theo từng bảng).
- **QR-TO-0xx**: access-token in-memory (Pinia) vs localStorage — chọn in-memory + refresh cookie httpOnly (an toàn XSS) trừ khi cần "nhớ đăng nhập" mạnh.

## 10. Self-validation trước code
- [x] Stack FE dựa quyết định user (PrimeVue MIT) + license verify raw (§0). Placement/monorepo mirror resort-qr (đã đọc).
- [x] Same-origin (Host không CORS — verify grep) + Vite proxy dev; SignalR JWT-qua-query khớp K-Con.4.
- [x] Responsive: fluid-first + container-query + dvh + safe-area + no-maximum-scale + no-horizontal-overflow — kỹ thuật web-platform chuẩn, verify được bằng Playwright (§7).
- [x] BE Api tiêu thụ đã tồn tại (đọc journal/code); CHỈ Dashboard stats (task 11) thiếu → nằm trong FE.0.
- [x] Bảo mật render (textContent cho chat — khớp QR-AD-043; nội quy/FAQ sanitize server QR-AD-031).
- [x] Anti-drift FE = Playwright gate (song song INV BE); trung thực về môi trường thiếu Node ở máy hiện tại.
- [ ] User review design FE (đặc biệt §3 responsive + §9 quyết định Tailwind/serve) trước FE.0.


## FE.3 — Admin Rooms + QR (chi tiết triển khai, design-first bổ sung)

> Bổ sung cho §8 slice 4. Chia đôi cho "từng bước chắc chắn": **FE.3a** (read-only: danh sách + xem QR, role Staff+) →
> **FE.3b** (mutation Admin: tạo/sửa/đổi-trạng-thái/xoá/rotate-token + xác nhận). Mọi shape client dưới đây ĐỌC TỪ CODE
> BE thật (KHÔNG bịa) — `starhill/src/Modules/Rooms/Rooms.Api/RoomsEndpointModule.cs` + `Rooms.Application/IRoomQueries.cs`
> + `Rooms.Domain/RoomStatus.cs` + `Bedrock.Application/UseCases/Paging.cs` + `RoomsErrors.cs`.

### FE.3.0 — API surface đã verify (nguồn: RoomsEndpointModule.cs)
- `GET /v1/rooms?status=&page=&pageSize=` (RequireStaff) → `PagedResult<RoomListItem>`.
- `GET /v1/rooms/{roomId}` (RequireStaff) → `RoomListItem` | ProblemDetails 404 (`not_found`).
- `GET /v1/rooms/{roomId}/qr.png` (RequireStaff) → `image/png` (binary; CẦN header `Authorization: Bearer` → KHÔNG dùng `<img src>` trực tiếp, phải fetch blob).
- `POST /v1/rooms` (RequireAdmin) body `{roomNumber, building?, floor?}` → 201 `{roomId, tokenPreview}` (FE.3b).
- `PUT /v1/rooms/{roomId}` (RequireAdmin) body `{roomNumber, building?, floor?}` → 204 (FE.3b).
- `PATCH /v1/rooms/{roomId}/status` (RequireAdmin) body `{status}` (string enum) → 204 (FE.3b).
- `DELETE /v1/rooms/{roomId}` (RequireAdmin) → 204 (FE.3b).
- `POST /v1/rooms/{roomId}/rotate-token` (RequireAdmin) body `{reason?}` → 200 `{tokenPreview}` (FE.3b).
- **`RoomListItem`** = `{roomId: string, roomNumber: string, building: string|null, floor: number|null, status: RoomStatus, activeTokenPreview: string|null, activeTokenVersion: number, createdAt: string}`.
- **`PagedResult<T>`** = `{items: T[], page: number, pageSize: number, total: number}` (verify Paging.cs — field name chính xác `Items/Page/PageSize/Total`; JSON camelCase).
- **`RoomStatus`** = string enum `'Active' | 'Inactive' | 'Maintenance'` (Host JsonStringEnumConverter toàn cục — QR-AD-021).
- **Mã lỗi** (RoomsErrors.cs, cho FE.3b): `validation_error` (trùng số phòng), `qr_generation_failed`, `not_found`, `resort_not_found`, `invalid_configuration`. (FE.3a read-only chủ yếu gặp auth/network → thông báo generic; FE.3b sẽ map từng mã sau khi verify tiền tố `code` trong ProblemDetails.)

### FE.3a — Components (read-only)
- **`RoomsView.vue`** (route `/rooms`, child AdminShell, guard auth sẵn): PrimeVue **DataTable** lazy-paged (`:lazy :value=items :totalRecords=total :rows=pageSize :first @page`) — cột: Số phòng · Toà/Tầng · Trạng thái (Tag màu theo status) · Mã QR (`activeTokenPreview` + `v{version}`, hoặc "—" nếu null) · Hành động ("Xem QR", disable khi `activeTokenPreview==null`). Bộ lọc trạng thái = PrimeVue **Select** (Tất cả/Active/Inactive/Maintenance) → đổi = reset page 1 + refetch. Bọc DataTable trong wrapper `overflow-x:auto; min-width:0` → bảng cuộn CỤC BỘ, KHÔNG đẩy tràn trang (giữ Playwright no-overflow gate — §3.5). Empty-state + error Message.
- **`RoomQrDialog.vue`**: PrimeVue **Dialog** hiện QR của phòng. Fetch `qr.png` qua api-client (Bearer) → `blob` → `URL.createObjectURL` gán `<img>`; nút **Tải PNG** (`<a download>`); revoke objectURL khi đóng/unmount (chống rò bộ nhớ). Nhãn phòng + preview token.
- **api-client** thêm: `listRooms(status,page,pageSize,token)` + `getRoomQrObjectUrl(roomId,token)`. **MOCK DEV-only** (khớp QR-N-073): sinh danh sách phòng tất định (demo 42 phòng ~ khớp dashboard activeRooms, đủ paging + đủ 3 trạng thái) + `qr.png` mock = **data-URL SVG placeholder "QR demo"** (TRUNG THỰC — không giả QR quét được; real mode trả PNG thật từ BE).
- **NavList**: bật route thật cho mục `rooms` (`to:'/rooms'`), bỏ badge "Sắp có" cho phòng.
- **i18n**: `rooms.*` (title, cột, trạng thái, lọc, xemQr, taiPng, empty, loadError).

### FE.3a — Responsive (§3)
- DataTable trong wrapper cuộn-ngang-cục-bộ (desktop dày; phone cuộn trong khung, không tràn trang). Dialog QR `max-width` + ảnh `width:100%` fluid. Toolbar (title + filter) flex-wrap. Chạm ≥44px.

### FE.3a — Verification (Playwright = browser thật, anti-drift FE)
- `e2e/tests/rooms.spec.ts` (mock `page.route` cho `**/v1/rooms*` + `**/v1/rooms/*/qr.png` — KHÔNG cần backend): (1) login→vào /rooms→bảng hiện đúng số dòng + preview token; (2) lọc status → refetch đúng; (3) mở dialog "Xem QR" → ảnh hiển thị; (4) no-horizontal-overflow @ phone-390/tablet-820/desktop-1280; (5) no-console-error; (6) screenshot rooms desktop+phone cho user XEM.
- Gate build: `pnpm --filter @starhill/admin-web build` EXIT=0 (vue-tsc typecheck).

### FE.3 — Quyết định/trade-off (ghi journal khi code)
- **QR-AD-0xx (QR qua fetch-blob, không `<img src>`)**: endpoint qr.png RequireStaff → cần Bearer → `<img>` không đính header được → BẮT BUỘC fetch blob→objectURL. Lý do bản chất (không fix ngọn): bảo mật endpoint đúng (không mở ẩn danh chỉ để `<img>` tiện). Trade-off: quản objectURL (revoke) — chấp nhận, chuẩn web.
- **QR-TO-0xx (DataTable cuộn-ngang-cục-bộ vs card-list <sm)**: chọn DataTable-in-scroll-wrapper (dày cho desktop admin, phone cuộn cục bộ) — nếu Playwright phone-390 lộ tràn thì pivot card-list <sm (đã nêu §3.8). Verify bằng gate, không đoán.
- **QR-TO-0xx (mock QR = SVG placeholder)**: trung thực (không giả QR thật) để user xem layout dialog không cần backend; real mode PNG thật từ BE.

### FE.3a — Self-validation trước code
- [x] API shape đọc từ code BE thật (RoomsEndpointModule/IRoomQueries/RoomStatus/Paging/RoomsErrors) — không bịa.
- [x] qr.png cần Bearer → fetch-blob (không `<img src>`); revoke objectURL.
- [x] Route `/rooms` dưới AdminShell dùng guard auth sẵn có (router beforeEach); role BE-side RequireStaff (FE không tự phân quyền, chỉ hiển thị — BE là nguồn sự thật).
- [x] No-overflow giữ bằng wrapper cuộn cục bộ + verify Playwright ma trận viewport.
- [x] MOCK DEV-only (prod/real-fetch không đụng — khớp QR-N-073); mock QR trung thực (SVG demo).
- [ ] getDiagnostics design = 0 (kiểm sau khi ghi) → rồi mới code.
