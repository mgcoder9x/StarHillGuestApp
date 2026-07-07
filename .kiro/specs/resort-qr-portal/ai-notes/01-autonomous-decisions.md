# 01 — Quyết định AI tự ra (mà spec/docs không nói)

> Các quyết định dưới đây KHÔNG được nêu rõ trong `docs/resort-qr-portal/*`. AI tự chọn dựa trên mục tiêu "base thương mại, lâu dài, an toàn". Mỗi cái có thể đảo ngược nếu user không đồng ý.

### DEC-001: Gom ~10 project của reference về 5 project
- Status: Accepted
- Date: 2026-07-03
- Context: Reference có ~10 project nhỏ (Api, Api.Core, Api.Core.Shared, Core, Core.Shared, Domain, EntityFramework, Infrastructure, Auth, Common), ranh giới mờ.
- Decision/Change: Gom về 5 project: SharedKernel, Domain, Application, Infrastructure, Api + tổ chức module theo thư mục bên trong.
- Rationale: Dễ định vị code, build nhanh, vẫn giữ dependency rule. Ranh giới bằng namespace/thư mục đủ mạnh cho monolith.
- Evidence/Refs: `../design/backend/01-architecture.md` §2; reconciliation E7/E6.
- Impact: Cấu trúc solution toàn bộ backend.
- Reversible?: Có — tách lại thành nhiều project nếu module phình to.

### DEC-002: Dùng Scrutor thay AutoDependency tự viết
- Status: Accepted
- Date: 2026-07-03
- Context: Docs không chỉ định cơ chế DI cụ thể. Reference tự viết AutoDependency (E4).
- Decision/Change: Dùng thư viện Scrutor + marker interface (`IScopedService`...) + assembly marker.
- Rationale: An toàn hơn (không crash khi 0/nhiều impl), bỏ ForceLoadAssembly rải rác, cộng đồng bảo trì.
- Evidence/Refs: `../design/backend/01-architecture.md` §5; reconciliation E4, E6.
- Impact: Cách đăng ký DI toàn hệ thống.
- Reversible?: Có — có thể giữ attribute style nhưng đổi engine.

### DEC-003: Khóa chính `uuid`, sinh client-side UUIDv7 (FIRM, có fallback zero-schema)
- Status: ✅ Accepted (firm) — 2026-07-03
- Context: Reference dùng `Entity<TKey>` generic. Docs không nêu kiểu khóa.
- Decision: Mọi entity dùng khóa `uuid` (`Guid`, bỏ generic TKey), **sinh client-side bằng `Guid.CreateVersion7()`**.
- Rationale (chính xác, kiểm chứng được):
  1. Client-side → Id có sẵn lúc tạo entity (set FK/đồ thị trong cùng transaction, idempotency key, trả 201 Created không cần round-trip) — lợi ích kiến trúc/bảo trì, trội hơn chênh index ở quy mô resort.
  2. v7 time-ordered + Npgsql serialize Guid theo canonical order (vì `id::text` PG khớp `guid.ToString()`) → **kỳ vọng** B-tree tuần tự (benchmark PG: index nhỏ ~26-27%, ordered scan ~3x). Nguồn: credativ/betterstack/neon/nerdleveltech.
  3. Guid > bigint: không lộ số lượng/khó đoán.
- Điểm chưa tự benchmark (trung thực): byte-layout cụ thể của `Guid.CreateVersion7()` đạt locality trên PG hay không — 1 báo cáo cộng đồng nói ngược. → verify 1 lần khi dựng foundation.
- Rủi ro kiềm chế: cột luôn `uuid`; nếu benchmark xấu → đổi nguồn sinh sang `DEFAULT uuidv7()` PG18, **không đổi schema/dữ liệu**.
- Evidence/Refs: `../design/backend/02` §1; TK-002; TRD-003.
- Impact: toàn bộ entity + FK. Reversible: cách sinh đổi rẻ (không đổi schema); kiểu uuid ổn định.

### DEC-004: `Result<T>` + `AppErrors` thay `IHttpResponse`
- Status: Accepted
- Date: 2026-07-03
- Context: Reference trả `IHttpResponse` trộn HTTP vào domain. Docs chỉ định danh sách error code guest.
- Decision/Change: Application trả `Result<T>` trung lập; Api map sang ProblemDetails; tập mã lỗi tập trung ở `AppErrors`.
- Rationale: Tách tầng, test dễ, giữ đúng hợp đồng error code của docs.
- Evidence/Refs: `../design/backend/02-core-abstractions.md` §2; docs design.md "Error Handling".
- Impact: Chữ ký mọi use case + controller.
- Reversible?: Có (nhưng lan rộng).

### DEC-005: Thêm concurrency token (xmin) cho entity nội dung
- Status: Accepted
- Date: 2026-07-03
- Context: Docs nghiệp vụ có nêu optimistic concurrency cho RuleSection/FaqItem/ResortSettings; reference chỉ vài chỗ.
- Decision/Change: Chuẩn hóa `IConcurrencyAware.RowVersion` ↔ Postgres `xmin` cho mọi entity có concurrency.
- Rationale: Đáp ứng "người lưu sau nhận 409". Sửa tận gốc thay vì thêm rời rạc.
- Evidence/Refs: `../design/backend/02` §1, `../design/backend/03` §3; docs "Optimistic concurrency".
- Impact: DbContext config + middleware map 409.
- Reversible?: Có.

### DEC-006: Lưu enum dạng string trong DB
- Status: Accepted
- Date: 2026-07-03
- Context: Partial unique index dùng điều kiện `WHERE status = 'Active'`. Docs không nói lưu enum kiểu gì.
- Decision/Change: Map enum → string trong Postgres.
- Rationale: Partial index đọc được theo tên, dễ debug/truy vấn thủ công; ổn định khi thêm giá trị enum.
- Evidence/Refs: `../design/backend/04-data-model.md` §5.
- Impact: EF config + migration.
- Reversible?: Có (migration đổi kiểu cột).

### DEC-007: Đặt tài liệu thiết kế trong `.kiro/specs/resort-qr-portal/design/` + `ai-notes/`
- Status: Accepted
- Date: 2026-07-03
- Context: User yêu cầu 2 folder design (BE/FE) + 1 folder ghi chú kiểm chứng. Đã có sẵn 2 nơi tài liệu: `docs/resort-qr-portal/` (nghiệp vụ) và `.kiro/specs/resort-qr-portal/` (spec Kiro).
- Decision/Change: Đặt `design/backend/`, `design/frontend/`, `ai-notes/` bên trong spec Kiro; `design.md` gốc làm master index; `docs/` giữ nguyên là nguồn nghiệp vụ.
- Rationale: Gom tài liệu base cùng spec; tránh phân mảnh; một chủ đề một file authoritative để không sai lệch.
- Evidence/Refs: `../design/README.md`.
- Impact: Vị trí mọi tài liệu thiết kế.
- Reversible?: Có (di chuyển thư mục).

### DEC-008: `VisitIdleSweeper` là background service DUY NHẤT; không đụng RoomQrToken
- Status: Accepted
- Date: 2026-07-03
- Context: Docs nêu background dọn GuestVisit idle và "không auto-expire QR token".
- Decision/Change: Chỉ một hosted service quét visit hết hạn + cascade; tuyệt đối không thay đổi trạng thái QR token tự động.
- Rationale: Đúng yêu cầu docs (QR vật lý chỉ revoke thủ công); tránh lỗi hàng loạt QR đã in.
- Evidence/Refs: `../design/backend/03` §6; docs "Observability".
- Impact: Một hosted service.
- Reversible?: Có.

### DEC-009: Các ngưỡng/giá trị mặc định cụ thể (sinh khi tạo requirements.md)
- Status: NeedsUserInput
- Date: 2026-07-03
- Context: `requirements.md` (sinh tự động ở bước Design-First → Requirements) đã đưa vào nhiều con số cụ thể mà `design.md` và `docs/*` KHÔNG nêu. Đây là default hợp lý nhưng là quyết định AI tự ra.
- Decision/Change (các giá trị đã ghi trong requirements — cần user duyệt):
  - Rate limit `resolve`: 20 request / 60s / IP (Req 13.1).
  - Rate limit `guest-write`: 10 request / 60s / GuestSessionId (Req 13.2).
  - JWT access token: mặc định 15 phút (khoảng 5–60) (Req 11.1).
  - Refresh token: mặc định 30 ngày (khoảng 7–90), lưu hash trong cookie HttpOnly (Req 11.1).
  - Token sinh lại tối đa 5 lần khi trùng, sau đó trả lỗi (Req 7.5); entropy ≥ 32 byte, base64url ≥ 43 ký tự (Req 7.1–7.2).
  - PDF tối đa 500 phòng/lần, render ≤ 30s; QR PNG render ≤ 5s (Req 16.2–16.3, 16.8).
  - Health: `/health/live` ≤ 2s; `/health/ready` kết nối DB ≤ 5s (Req 14.3, 14.6–14.7).
  - `VisitIdleSweeper` chu kỳ mặc định 5 phút (Req 14.4).
  - SignalR reconnect tối đa 5 lần backoff 1–30s; fallback polling mỗi 10s (Req 17.6–17.7).
  - `MaxMessageLength` mặc định 2000 (khoảng 1–10000); `PortalWindowMinutes` 30 (1–1440); `VisitIdleExpiryHours` 24 (1–168); ticket rate 5/60s (Req 15.3).
- Rationale: cần con số để test đo được (measurable acceptance criteria); chọn giá trị an toàn/thông dụng.
- Impact: cấu hình mặc định `ResortSettings`/`appsettings` + test.
- Reversible?: Có — chỉ là default, chỉnh trong cấu hình. **User nên xem lại các con số này.**

### DEC-010: Migration strategy — ĐÃ CHỐT incremental (per-wave)
- Status: ✅ Resolved (2026-07-03)
- Context: requirements ban đầu (Req 8.6, 18.1) lỡ "chốt cứng" hướng dựng toàn bộ schema. User chỉ đạo "theo khuyến nghị từng bước chắc chắn, sản phẩm thương mại".
- Decision: Chọn **migration incremental per-wave** (migration nền chỉ chứa entity nền; module thêm bảng khi hiện thực). Đã sửa requirements Req 8.6→8.7 và Req 18.1→18.2 cho khớp.
- Rationale: xem TRD-002. Fix gốc: migration = versioned schema history, additive an toàn.
- Impact: requirements + `../design/backend/04` §4–5.
- Reversible?: Có.

### DEC-011: Auth viết mới hoàn toàn (reference rỗng); toàn bộ base là "design new", chỉ kế thừa ý tưởng
- Status: Accepted
- Date: 2026-07-03
- Context: User yêu cầu đọc để đánh giá reference "đã ok chưa"; nếu chưa thì design mới hoặc dùng lại phần dùng được. Đã đọc sâu: Auth rỗng (E9), mật khẩu plaintext (E10), không cấu hình JWT (E11), phân tầng leaky (E12), DbContext thiếu actor/concurrency (E13), controller demo (E14).
- Decision/Change: Kết luận `Reference/Backend` là **scaffold học tập**, KHÔNG phải base production. Base mới **thiết kế lại**, chỉ kế thừa **ý tưởng/pattern** (layering, entity base concept, use-case-per-operation, seed). **Auth xây mới từ đầu** (JWT admin + guest cookie + Argon2/PBKDF2 hasher + refresh rotation). Tuyệt đối không lưu/ghi log mật khẩu dạng plaintext.
- Rationale: Mục tiêu user = "base cực tốt về kiến trúc và triển khai". Tái dùng code lỗi (plaintext password, leaky layering) sẽ kéo theo nợ kỹ thuật ngay từ nền.
- Evidence/Refs: `../design/backend/09-reference-reconciliation.md` §4 (bảng phán quyết), E9–E14.
- Impact: Toàn bộ module Identity + quyết định "không import code reference".
- Reversible?: Có, nhưng không khuyến nghị (tái dùng code lỗi).

### DEC-012: FE thiết kế lại trên stack hiện đại; KHÔNG port `EPS.Vuexy`
- Status: Accepted
- Date: 2026-07-03
- Context: User nói reference FE "code rất kém, chỉ tham khảo". Đã kiểm chứng: Vue 2.6 (EOL), JS, Vue CLI/webpack, Vuex 3, Bootstrap-Vue 2 (EOL), đăng ký component toàn cục, grab-bag nặng, key trùng trong package.json (FE-E1..E14).
- Decision: Base FE = Vue 3.5+ + TypeScript + Vite 8 + Pinia + Vue Router 4 + vue-i18n (mới nhất), Node 24 LTS, monorepo pnpm (2 app + packages). KHÔNG import code reference; chỉ mượn vài ý tưởng (layout admin, permission concept, SignalR, i18n tách UI/content). Version chi tiết: `../design/technology-stack.md`.
- Rationale: Vue 2 EOL, không thể là nền commercial lâu dài; TS/Vite/Pinia là chuẩn hiện đại.
- Evidence/Refs: `../design/frontend/01-reference-assessment.md` (FE-E1..E14), `02-architecture.md`.
- Impact: toàn bộ FE.
- Reversible?: Có (nhưng đi ngược mục tiêu commercial).

### DEC-013: guest-web tối giản (không UI kit nặng); admin-web dùng Element Plus
- Status: Accepted (Element Plus — xem TRD-007, đã đổi từ PrimeVue)
- Date: 2026-07-03 (cập nhật quyết định UI kit cùng ngày sau khi verify)
- Decision: guest-web tự viết component + CSS nhẹ (bundle nhỏ, mobile-first); admin-web dùng **Element Plus** cho table/form/dialog.
- Rationale: guest tải trên điện thoại cần nhẹ; admin cần component dashboard mạnh. Element Plus thay PrimeVue vì repo PrimeVue archive (TRD-007).
- Evidence/Refs: `../design/frontend/02-architecture.md` §1, §7, §8; `../design/technology-stack.md`.
- Reversible?: Có (Naive UI là dự phòng).

### DEC-015: Chính sách phiên bản = stable/LTS mới nhất (không preview)
- Status: Accepted
- Date: 2026-07-03
- Context: User yêu cầu "nâng công nghệ lên cao nhất". Đã verify web các version mới nhất.
- Decision: dùng bản **stable/LTS mới nhất** cho production, KHÔNG dùng preview/RC. Cụ thể (verify 2026-07-03): .NET 10 LTS + EF Core 10 LTS (không .NET 11 preview, không .NET 9 STS), PostgreSQL 18, Vite 8 (Rolldown), Vue 3.5+ (không 3.6 preview), Node.js 24 LTS. Ma trận đầy đủ + nguồn: `../design/technology-stack.md`.
- Rationale: LTS/stable có vá bảo mật dài hạn, hệ sinh thái tương thích, ít breaking bất ngờ — đúng "an toàn lâu dài thương mại". "Cao nhất" = cao nhất trong nhóm ổn định.
- Impact: toàn bộ version BE+FE.
- Reversible?: Có (nâng khi bản mới lên LTS/stable).

### DEC-014: Access token admin lưu trong MEMORY (không localStorage)
- Status: Accepted
- Date: 2026-07-03
- Decision: access token giữ trong memory (Pinia), refresh token là cookie HttpOnly; không lưu access token vào localStorage.
- Rationale: chống đánh cắp token qua XSS; chuẩn bảo mật SPA thương mại.
- Evidence/Refs: `../design/frontend/02-architecture.md` §8, §9.
- Reversible?: Có (nhưng giảm bảo mật).

### DEC-016: Auth — Argon2id + JWT HS256 + refresh rotation & reuse detection
- Status: Accepted (2026-07-03)
- Context: base phải hiện thực Identity (login/refresh); reference rỗng + plaintext.
- Decision & lý do chính xác:
  - **Argon2id** hash mật khẩu (memory-hard, kháng GPU/ASIC; biến thể id kháng cả side-channel lẫn GPU — khuyến nghị OWASP). Fallback PBKDF2 chỉ khi cần FIPS. Lưu PHC string + rehash-on-login để nâng tham số không phá dữ liệu.
  - **JWT HS256** (đối xứng) cho MVP vì chỉ backend vừa ký vừa verify → đơn giản/nhanh/đủ an toàn khi bảo vệ SigningKey; đường mở RS256/ES256 khi có bên thứ ba verify.
  - **Refresh token opaque CSPRNG, lưu hash, rotation one-time-use + reuse detection theo FamilyId** (token đã xoay bị trình lại → thu hồi cả family) — chuẩn chống trộm refresh token.
  - Access token giữ **in-memory** ở client (chống XSS); login trả lỗi mơ hồ + hash giả khi user không tồn tại (chống enumeration/timing).
- Evidence/Refs: `../design/backend/12-identity-and-auth.md`; reference E5/E9/E10.
- Reversible?: tham số/thuật toán chỉnh được; schema refresh cần migration nếu đổi.

### DEC-017: (bổ sung concurrency/token — đã ghi ở DEV-010/011, tham chiếu chéo)
- Xem DEV-010 (Token UNIQUE toàn cục), DEV-011 (phạm vi concurrency + delete behavior + index), DEV-012 (RefreshToken schema mở rộng).

### DEC-018: ForwardedHeaders (IP client thật) + allowlist HtmlSanitizer tường minh
- Status: Accepted (2026-07-03)
- Context: hệ sau reverse proxy; rate limit theo IP + log + secure-context phụ thuộc IP/scheme thật. Nội dung rich text là đường XSS.
- Decision & lý do chính xác:
  - Bật `ForwardedHeadersMiddleware` (X-Forwarded-For/Proto), chạy SỚM NHẤT; **chỉ tin đúng KnownProxies** (nếu tin mọi nguồn → giả mạo IP né rate limit/forge log). Không có bước này, rate limit "theo IP" (Req 13.1) gom mọi khách 1 bucket → sai.
  - HtmlSanitizer (`Ganss.Xss`, không tự viết) dùng **allowlist tường minh** (tag/attr/URI scheme); cấm `on*`, `style`, `script/iframe/...`, `javascript:/data:`; `<img>` tắt mặc định ở base. Sanitize trên đường ghi (Property B7).
- Evidence/Refs: `../design/backend/03` §8–9; Req 13.1, 8.6, 11.4.
- Reversible?: cấu hình chỉnh được (proxy IP, allowlist) không đổi schema.

### DEC-019: Catalog mã lỗi đầy đủ + bổ sung code admin (spec gốc chỉ liệt kê guest)
- Status: Accepted (2026-07-03)
- Context: docs gốc chỉ nêu tập code guest (qr_invalid, qr_revoked, room_inactive, rule_ack_required, rate_limited, message_too_long, session_expired, language_not_supported). Base cần thêm code cho admin/chung để hợp đồng đầy đủ.
- Decision: tạo `14-error-catalog.md` là nguồn chân lý; **AI tự bổ sung** các code: `validation_error`, `concurrency_conflict`, `unauthorized`(401), `forbidden`(403), `invalid_configuration`, `qr_generation_failed`, `pdf_limit_exceeded`, `not_found`, `unexpected`.
- Lý do chính xác: FE xử lý theo `code` ổn định (không đoán message); tập code phải đầy đủ & cố định để test đối chiếu 1-1 với `shared-types.ErrorCode` (Property B5). Thêm code là backward-compatible; đổi/xóa là breaking (versioned).
- Lưu ý cơ chế: `unauthorized`(401) do auth scheme phát (challenge), không qua Result→ProblemDetails như code nghiệp vụ; vẫn gắn code để FE nhất quán.
- Evidence/Refs: `../design/backend/14-error-catalog.md`; docs Error Handling; Req 4.
- Reversible?: có (chỉnh catalog); nhưng đổi/xóa code đã dùng là breaking.

### DEC-020: Cấu hình strongly-typed Options + validate-on-startup (fail-fast)
- Status: Accepted (2026-07-03)
- Context: reference nhét secret vào appsettings (E11), không validate cấu hình lúc khởi động.
- Decision & lý do chính xác:
  - Mọi nhóm cấu hình = Options class bind + `ValidateDataAnnotations().ValidateOnStart()` + `IValidateOptions` cho secret/ràng buộc chéo. Ở Production, thiếu `Jwt:SigningKey`/`ConnectionStrings:Postgres`/`Seed:AdminPassword` → **app từ chối khởi động**.
  - Lý do: phát hiện cấu hình sai lúc khởi động rẻ + an toàn hơn lúc chạy (khóa JWT rỗng = ai cũng giả token); "chạy tạm với default" là phản mẫu bảo mật.
  - Cấm đọc `IConfiguration[...]` rải rác; đi qua Options (test được, có kiểu, có validate).
  - Precedence giá trị vận hành: `ResortSettings`(DB) → `appsettings` → hằng số (Req 14.5); secret chỉ từ env/secret store, không commit.
- Evidence/Refs: `../design/backend/15-configuration-and-options.md`; E11; Req 14.5, 13.5.
- Reversible?: có (điều chỉnh Options/validator).

### DEC-021: Test plan nền = Property→Test chạy được trên PostgreSQL 18 (Testcontainers)
- Status: Accepted (2026-07-03)
- Decision: mỗi bất biến nền (B1–B10) có test fail-khi-phá; ràng buộc DB test trên **PostgreSQL 18 thật** (Testcontainers), KHÔNG InMemory (InMemory không enforce unique/partial → false green). 4 PBT ≥100 iteration. Test đối chiếu AppErrors↔FE ErrorCode.
- Lý do: design chỉ "đúng trên giấy" tới khi test xanh chứng minh; InMemory cho ràng buộc là false confidence.
- Evidence/Refs: `../design/backend/17-test-plan-base.md`; Req 19.
- Reversible?: có.

### DEC-022: Localization — so khớp primary-subtag + "row rỗng = thiếu" + phát hiện thiếu dịch
- Status: Accepted (2026-07-03)
- Context: docs nêu fallback + isFallback nhưng không đặc tả thuật toán/chuẩn hóa mã.
- Decision & lý do:
  - So khớp ngôn ngữ theo **primary-subtag** (`ko-KR→ko`, lowercase); nếu không bật → default (Req 2.2). FE và BE **cùng quy tắc** (`03` FE + `18` BE) để không lệch ngôn ngữ.
  - **"Có row dịch nhưng nội dung rỗng" = coi như thiếu** → fallback default (điểm tinh vi, tránh hiển thị trống).
  - Resolve theo TẬP: nạp 1 query (chống N+1), cờ `isFallback` riêng từng mục; mục thiếu cả 2 → missing, không lỗi cả request (Req 9.5/9.6).
  - Nguồn default = `ResortLanguage.IsDefault` (1 nguồn), không có `Resort.DefaultLanguage`.
- Refs: `../design/backend/18-localization.md`; `../design/frontend/03-guest-web-flows.md`.
- Reversible?: có.

### DEC-023: Observability — correlation xuyên SignalR, mask bí mật tại nguồn, không log nội dung tin nhắn
- Status: Accepted (2026-07-03)
- Decision & lý do:
  - CorrelationId (từ `Activity.TraceId`/header) enrich mọi log + `traceId` ProblemDetails + response header + **qua SignalR** (nối luồng realtime với REST).
  - Mask **tại nguồn có chủ đích**: path `/r/{token}`→`/r/***`, không log Authorization/Cookie/Set-Cookie, redact field password/token/refresh; log `RoomId` **chỉ khi resolve thành công** (không lộ dò phòng khi token lỗi).
  - **Không log nội dung tin nhắn** ở Information (quyền riêng tư) — chỉ ConversationId + độ dài.
  - Health tách `live` (không phụ thuộc DB) vs `ready` (kiểm DB, không lộ connection string khi lỗi).
- Refs: `../design/backend/19-observability.md`; Req 11.6, 12.1–12.2, 14.
- Reversible?: có.

### DEC-024: admin silent refresh single-flight
- Status: Accepted (2026-07-03)
- Context: access token in-memory (DEC-014) → reload/hết hạn cần lấy lại từ refresh cookie.
- Decision & lý do: nhiều request 401 song song **chia sẻ MỘT** lần refresh (single-flight). Nếu mỗi request tự refresh → refresh-storm + rotation đá nhau → **reuse detection thu hồi family** → đăng xuất oan. Retry đúng 1 lần; `/refresh` 401 (family revoked) → hard logout.
- Refs: `../design/frontend/04-admin-auth-guards.md` §2; backend `12` §3.
- Reversible?: có.

### DEC-025: Deployment same-origin + reverse proxy + migration là bước deploy riêng
- Status: Accepted (2026-07-03)
- Decision & lý do:
  - **Same-origin** (`/` guest, `/admin`, `/api`, `/hubs`) → không cần CORS phức tạp, cookie Secure/SameSite tự nhiên (docs MVP). Proxy terminate TLS + phục vụ static SPA + chuyển tiếp API/hub.
  - **Kestrel chỉ nghe loopback**, mọi traffic qua proxy (giảm bề mặt).
  - **WebSocket cho `/hubs`** phải khai upgrade headers (Nginx) — lỗi phổ biến làm SignalR rớt về polling; Caddy tự xử lý (nên khuyến nghị Caddy cho đơn giản + auto-TLS).
  - **Migration áp bằng bước deploy riêng** (ef bundle/update), KHÔNG auto-migrate lúc khởi động prod (tránh nhiều instance migrate nửa chừng) — TK-011.
- Evidence/Refs: `../design/backend/20-deployment-reverse-proxy.md`; docs Deployment; Req 12.5.
- Reversible?: có (đổi proxy/cách deploy).

### DEC-026: SignalR — JWT qua query trên WS (admin) + cookie (guest), authorize join bằng dữ liệu server
- Status: Accepted (2026-07-03)
- Context/lý do: trình duyệt không set header Authorization trên handshake WebSocket → SignalR gửi JWT qua query `access_token`; backend đọc ở `OnMessageReceived` cho path `/hubs`, và **mask access_token trong log**. Guest dùng cookie tự gửi. `JoinConversation` **không tin conversationId client** — server suy từ IGuestContext→visit→conv (chống guest nghe lén conv lượt/khách khác — Req 5.9, B9). Notify post-commit qua IRealtimeNotifier.
- Evidence/Refs: `../design/backend/21-realtime-signalr.md`; giải quyết GAP-1.
- Reversible?: có.

### DEC-027: Solution skeleton — CPM + Directory.Build.props + TreatWarningsAsErrors + dependency rule ở csproj
- Status: Accepted (2026-07-03)
- Decision & lý do chính xác:
  - **Central Package Management** (`Directory.Packages.props`, `ManagePackageVersionsCentrally=true`): pin version một chỗ → không lệch version giữa nhiều project, audit/nâng version một nơi (bug phổ biến ở solution nhiều project).
  - **Directory.Build.props** đặt target/nullable/analyzer chung — không lặp/không quên trong từng csproj.
  - **`TreatWarningsAsErrors=true`**: nullable/async warning (CS86xx/CS4014) là bug tiềm ẩn → buộc sửa lúc build (correctness-by-construction ở tầng biên dịch); khoanh vùng bằng `WarningsNotAsErrors` nếu generated code gây nhiễu.
  - **Dependency rule enforce TĨNH ở `.csproj`** (Domain không có ProjectReference tới EF/Infra → không compile được nếu vi phạm) + NetArchTest (runtime) — hai tầng bổ trợ.
  - **`AppDbContextFactory : IDesignTimeDbContextFactory`** vì startup (Api) khác project chứa DbContext (Infrastructure) — để `dotnet ef` tạo migration không cần chạy app.
  - **KHÔNG hardcode số version** trong tài liệu → pin khi implement theo `technology-stack.md` (TK-005/018).
- Evidence/Refs: `../design/backend/23-solution-skeleton.md`; `01` §5; `06`.
- Reversible?: có.

### DEC-028: Tenancy = Instance-per-resort (DB-per-tenant bằng triển khai) — KHÔNG shared-DB multi-tenant
- Status: Accepted (2026-07-03)
- Context: sản phẩm thương mại bán nhiều resort → câu hỏi tenancy (đắt nếu sai).
- Decision & lý do chính xác:
  - Chọn **instance-per-resort**: mỗi resort một cài đặt (app + DB) trong mạng nội bộ resort đó. Multi-resort = nhân bản triển khai.
  - Lý do bản chất: docs D1 (WiFi nội bộ, KHÔNG public internet, DNS nội bộ per-site) đã khiến mỗi resort là môi trường tách biệt vật lý. Shared-DB multi-tenant (SaaS tập trung) **mâu thuẫn trực tiếp** D1 (buộc public internet) → phá vỡ mô hình bảo mật của chính sản phẩm. Cách ly bằng hạ tầng mạnh hơn cách ly bằng app; tránh rủi ro rò rỉ chéo tenant (quên WHERE ResortId).
  - Giữ `ResortId` trên entity (đã có) như bảo hiểm rẻ để schema multi-resort-capable, nhưng KHÔNG xây tenant-isolation machinery bây giờ (YAGNI).
- Nâng cấp giả định **A1** (single-resort) → thành quyết định tenancy có chủ đích.
- Chi phí chuyển shared-DB multi-tenant nếu MỘT NGÀY cần: TK-033 (thêm ResortId vào GuestSession, email unique (ResortId,Email), tenant middleware + global query filter, per-tenant secret).
- Evidence/Refs: `../design/backend/24-tenancy-model.md`; docs D1/Deployment.
- Reversible?: hướng (A) rẻ; chuyển sang shared-DB là thay đổi mô hình bảo mật (chỉ khi thực sự cần).

### DEC-029: Frontend skeleton (pnpm workspaces, TS strict, type-gen OpenAPI) + i18n hai tầng, thời gian theo timezone resort
- Status: Accepted (2026-07-03)
- Decision & lý do:
  - Monorepo pnpm 2 app + packages; TS `strict`+`noUncheckedIndexedAccess` (bắt lỗi kiểu lúc build, đối xứng TreatWarningsAsErrors backend). Dependency rule FE một chiều (apps→packages).
  - **Sinh `shared-types` từ OpenAPI** + test hợp đồng `ErrorCode`↔`AppErrors` → chống lệch hợp đồng BE↔FE.
  - admin build `base:'/admin/'` (sai base → asset 404 sau deploy — lỗi phổ biến).
  - i18n **hai tầng** (UI text JSON vs content API); render content HTML **chỉ v-html cho nội dung backend đã sanitize** (defense-in-depth); `isFallback` hiển thị badge.
  - **Thời gian: API trả UTC → FE format theo `Resort.Timezone`** (Intl timeZone), không theo múi giờ trình duyệt (tránh lệch giờ). Font CJK fallback cho ko/zh; en/vi/ko/zh đều LTR (chưa cần RTL).
- Evidence/Refs: `../design/frontend/05-monorepo-skeleton.md`, `06-i18n-and-content.md`; `04`§9 (UTC), `14` (error codes).
- Reversible?: có.

### DEC-030: Portal window = SLIDING WINDOW theo hoạt động (chốt P0-2)
- Status: Accepted (2026-07-04)
- Context: Expert review P0-2 phát hiện hai semantics mâu thuẫn: `13` §1 nói "chỉ /resolve refresh cửa sổ" nhưng ngay dưới (§3) lại cập nhật ExpiresAt ở endpoint tương tác; requirements.md (line 181) nói endpoint tương tác thành công cập nhật LastSeenAt/ExpiresAt.
- Decision: chốt **sliding window theo hoạt động** — mọi request guest hợp lệ (resolve + các endpoint tương tác được xác thực qua GuestSession) đều gia hạn `ExpiresAt = now + PortalWindowMinutes` và cập nhật `LastSeenAt`. KHÔNG chọn "chỉ quét QR mới gia hạn".
- Lý do chính xác (fix gốc): mô hình nghiệp vụ là "khách còn đang dùng portal trong phòng thì còn quyền". Sliding-window phản ánh đúng "đang hoạt động"; nếu chỉ /resolve gia hạn thì khách đang chat/đọc nội dung liên tục vẫn bị hết hạn giữa chừng (UX sai + mâu thuẫn Req line 181). `VisitIdleSweeper` vẫn thu hồi visit *thực sự* idle (không có hoạt động trong cửa sổ).
- Đồng bộ: quy tắc gia hạn được áp tại MỘT chỗ (EnforcePortalWindow trong resolve use case + guest write path) để REST và SignalR dùng chung (xem DEC-031).
- Evidence/Refs: `../design/backend/13-guest-access-flows.md` §1/§3; requirements.md line 181; DEV-019.
- Reversible?: có (đổi sang chỉ-resolve chỉ là đổi nơi gọi refresh) nhưng đi ngược Req.

### DEC-031: SignalR enforce PortalWindow/ExpiresAt ở MỌI điểm + evict group khi EndVisit (chốt P0-3)
- Status: Accepted (2026-07-04)
- Context: Expert review P0-3: hub chỉ kiểm `activeVisitOf(...)` lúc `JoinConversation`; không kiểm PortalWindow/ExpiresAt và không remove group khi visit hết hạn → connection SignalR cũ vẫn nghe realtime dù REST đã trả `session_expired` (bypass bảo mật).
- Decision: (1) hub `OnConnectedAsync` + join/rejoin + mọi hub method dùng **cùng `EnforcePortalWindow`** như REST (một nguồn luật, không nhân bản). (2) Guest join vào group `visit-{visitId}-guest`. (3) `EndVisit` (và VisitIdleSweeper khi hết hạn) gọi `IRealtimeNotifier.NotifyVisitEndedAsync` → server evict/abort các connection trong group đó → client buộc reconnect và bị từ chối (session_expired).
- Lý do chính xác (fix gốc): bảo mật realtime phải dùng CHUNG bất biến với REST, và trạng thái server (visit đã kết thúc) phải chủ động đẩy tới connection đang mở — không thể dựa vào client tự ngắt. "Authorize chỉ lúc join" là fix ngọn; enforce liên tục + evict là fix gốc.
- Evidence/Refs: `../design/backend/21-realtime-signalr.md` §4/§4.1/§5/§6; `02` §5; `13` §5; DEV-019.
- Reversible?: không nên (đảo lại là tái tạo lỗ hổng bypass).

### DEC-032: Chiến lược thực thi = nền ngang trước → lát cắt dọc backend-first (KHÔNG dựng BE+FE song song từ số 0)
- Status: Accepted (2026-07-04) — user đã duyệt khuyến nghị
- Context: user hỏi "có nên tạo backend và FE luôn không". Design đã chốt, cần quyết cách triển khai để không sinh nợ kiến trúc.
- Decision: (1) tạo `tasks.md` trước (Design-First: design→requirements→tasks→implement). (2) Dựng **nền ngang** trước (skeleton → SharedKernel → Infrastructure → cross-cutting → module nền) vì nền là điều kiện tiên quyết cho mọi feature. (3) Phần feature đi **lát cắt dọc, backend contract-first**: BE định nghĩa contract/OpenAPI → sinh `shared-types` → FE tiêu thụ. KHÔNG code FE feature trước khi BE có contract; KHÔNG "làm hết BE rồi hết FE".
- Lý do chính xác (fix gốc): FE phụ thuộc contract BE (DEC-029: shared-types sinh từ OpenAPI + test hợp đồng ErrorCode↔AppErrors). Code FE trước contract = phỏng đoán = rework khi BE đổi. Lát cắt dọc phát hiện sai kiến trúc sớm (khi rẻ) thay vì dồn rủi ro về cuối. Nền vốn horizontal (không thể vertical-slice phần SharedKernel/Infra/cross-cutting).
- Cấu trúc tasks: 9 wave (0–8), 36 task, có Task Dependency Graph (JSON) + truy vết 20/20 requirements. Khép kín P0-1 (#17 trước #18), P0-3 (#26→#27).
- Evidence/Refs: `tasks.md`; `00-overview-and-goals.md` §Lộ trình; DEC-029.
- Reversible?: có (thứ tự task điều chỉnh được) nhưng contract-first là nguyên tắc nên giữ.

### DEC-033: Chấp nhận format solution `.slnx` (mặc định .NET 10 SDK) + kết quả scaffold Wave 0 task #1
- Status: Accepted (2026-07-04) — đã build-verified
- Context: `dotnet new sln` với SDK 10.0.301 sinh **`ResortQr.slnx`** (XML solution format, nay là mặc định) thay `.sln` cũ. Task #1 dựng solution skeleton.
- Decision & lý do:
  - Giữ **`.slnx`** (không ép về `.sln` cổ): là định dạng mặc định chính thức của .NET 10 SDK, `dotnet`/CI dùng được, gọn/ít nhiễu diff hơn `.sln`. Đúng tinh thần "công nghệ cao nhất trong nhóm ổn định" (DEC-015).
  - Scaffold đúng `23-solution-skeleton.md`: 5 project `src/` + 3 project `tests/`; dependency rule enforce TĨNH ở csproj (SharedKernel sạch; Domain→SK; Application→Domain,SK; Infrastructure→App,Domain,SK; Api→App,Infra); `Directory.Build.props` (net10, Nullable, `TreatWarningsAsErrors=true`, analyzers latest-Recommended); CPM bật (`Directory.Packages.props`); `global.json` pin SDK band 10.0.301 (rollForward latestFeature); `.editorconfig`; AssemblyMarker (App+Infra); xóa Class1.cs template.
  - **Version test PIN từ template thật (không bịa):** Microsoft.NET.Test.Sdk 17.14.1, xunit 2.9.3, xunit.runner.visualstudio 3.1.4, coverlet.collector 6.0.4. Package runtime (EF/Npgsql/Scrutor/...) thêm ở đúng wave để `dotnet add package` pin version thật.
  - **Kết quả kiểm chứng:** `dotnet build backend/ResortQr.slnx` = **Build succeeded, 0 Warning, 0 Error** (kể cả TreatWarningsAsErrors). Đây là DoD build của task #1.
- Toolchain đã cài (qua winget/npm): **.NET SDK 10.0.301** + **pnpm 11.9.0**. Node vẫn **v25.2.1** (chưa đổi 24 LTS — xem TK-034, cần confirm vì đổi Node ảnh hưởng toàn máy).
- Evidence/Refs: `../design/backend/23-solution-skeleton.md`; `tasks.md` Wave 0 #1; DEC-015/027.
- Reversible?: có (`.slnx`↔`.sln` convert được; cấu trúc project ổn định).

### DEC-034: Wave 0 task #2 — Architecture_Test (NetArchTest 1.3.2) + kiểm chứng cấu trúc 5-project KHÔNG vacuous
- Status: Accepted (2026-07-04) — test-verified
- Kết quả kiểm chứng dependency rule (3 lớp bằng chứng):
  1. **Đồ thị tham chiếu thật** (`dotnet list reference`): SharedKernel(0 ref) ← Domain ← Application ← Infrastructure; Api→{Application,Infrastructure}. Acyclic, khớp `23`§5.
  2. **NetArchTest 5/5 green**: Domain ⊥ {Infra,Api,EFCore,AspNetCore}; Application ⊥ {Api,Infra,EFCore,AspNetCore}; SharedKernel sạch.
  3. **Meta-test negative-control**: yêu cầu luật SAI ("SharedKernel phải phụ thuộc Domain") → PHẢI fail → chứng minh harness thực sự inspect IL, không false-green (tinh thần DEC-021).
- Fix gốc phát sinh: `CA1707` (analyzer cấm underscore) + TreatWarningsAsErrors chặn tên test `Method_should_x`. Root cause = CA1707 không dành cho tên method test (không phải public API surface) → khoanh vùng `dotnet_diagnostic.CA1707.severity = none` CHỈ cho `tests/**` trong `.editorconfig` (KHÔNG đổi tên test, KHÔNG tắt analyzer toàn cục). Production giữ CA1707.
- Marker: thêm AssemblyMarker cho cả SharedKernel/Domain/Api (ngoài App/Infra) làm anchor nạp assembly cho arch-test (ổn định hơn load theo chuỗi tên).
- **Đánh giá trung thực (điểm còn hở):** (a) arch-test hiện "nhẹ tải" vì code còn ít — giá trị bảo vệ tăng khi thêm module; (b) ranh giới GIỮA module (Rules/Faq/...) CHƯA được test (mới test theo tầng) → sẽ thêm ở task #29 (Wave 6); (c) SharedKernel có rủi ro thành "sọt rác" → giữ kỷ luật chỉ chứa primitive, cân nhắc arch-test giới hạn nội dung sau.
- Evidence/Refs: `backend/tests/ResortQr.ArchitectureTests/DependencyRuleTests.cs`; `tasks.md` #2; Req 1.2/1.3/19.6.
- Reversible?: có.

### DEC-035: Tách BASE generic `Foundation` (domain-agnostic, tái dùng mọi dự án) — bỏ tên ResortQr khỏi nền
- Status: Accepted (2026-07-04) — user chỉ đạo trực tiếp + build/test-verified
- Context: user làm rõ base này dùng cho **nhiều dự án cá nhân**, cần **base thật sạch (0 nghiệp vụ)**, "bỏ hết chữ QR", đủ Domain/Application/Api (rỗng cũng được), sau đó user tự copy ra dựng lại thành QR. → mô hình **template copy-được** (Model A), KHÔNG phải base+resort tham chiếu chéo trong cùng repo.
- Decision:
  - Tên base = **`Foundation`** (AI đề xuất, user đồng ý "theo bạn"): trung lập, mô tả đúng vai trò nền, hợp lệ làm root namespace. Đổi tên sau vẫn rẻ.
  - Tạo solution mới `foundation/Foundation.slnx`: 5 project `src/` (SharedKernel/Domain/Application/Infrastructure/Api) + 3 `tests/`, generic hoàn toàn, KHÔNG khái niệm resort.
  - Port nguyên governance từ scaffold cũ (Directory.Build.props, Directory.Packages.props CPM + NetArchTest 1.3.2, global.json, .editorconfig với CA1707-off cho tests) — nội dung không phụ thuộc tên.
  - Dependency rule + AssemblyMarker + Architecture_Test giữ nguyên, đổi namespace `ResortQr.*`→`Foundation.*`.
  - **XÓA `backend/ResortQr.*`** (scaffold DEC-033/034) — đã bị thay thế; tránh hai solution. DEC-033/034 vẫn giữ giá trị lịch sử (kỹ thuật scaffold + verify), chỉ đổi tên/vị trí.
  - Thêm `foundation/README.md` mô tả base + cách tái dùng.
- Kết quả kiểm chứng: `dotnet build foundation` = 0 Warning/0 Error; `Foundation.ArchitectureTests` = 5/5 pass (gồm meta-test chống false-green).
- Hệ quả cho spec: các design doc `.kiro/specs/resort-qr-portal/**` vẫn là **thiết kế QR** (dùng khi user dựng QR trên base sau). Building blocks generic (00–07, cross-cutting) sẽ được hiện thực trong `Foundation` ở Wave 1; phần resort (GuestVisit/Rooms/QR/Localization/SignalR guest) chỉ thêm khi dựng QR.
- Reversible?: có (đổi tên/gộp lại được), nhưng đi ngược mục tiêu "base tái dùng".

### DEC-036: Wave 1 — Nhóm 1 SharedKernel (Foundation) + quyết định analyzer & test UUIDv7
- Status: Accepted (2026-07-04) — build + test verified
- Đã hiện thực (generic, 0 nghiệp vụ): `Result`/`Result<T>` (factory tường minh, không implicit operator), `Error`/`ErrorType`, `CommonErrors` (tập code generic: validation_error/not_found/conflict/forbidden/unauthorized/concurrency_conflict/rate_limited/unexpected), `Entity` (UUIDv7 + identity-equality theo type+Id), `AuditableEntity`, interface `IAuditable/ISoftDeletable/IConcurrencyAware`, marker DI `IScopedService/ISingletonService/ITransientService`, `Guard` (CallerArgumentExpression).
- Quyết định do analyzer strict (`TreatWarningsAsErrors` + latest-Recommended) — fix GỐC:
  - **CA1000** (static member trên generic type): KHÔNG suppress — **restructure** dời factory sang `Result` non-generic (`Result.Ok(value)` suy luận kiểu, `Result.Fail<T>(error)`; ctor `Result<T>` internal). Ergonomics tốt hơn, đúng kiểu ErrorOr/FluentResults.
  - **CA1716** (`Error` trùng keyword VB): **tắt có chủ đích** trong `.editorconfig` + ghi lý do — base chỉ C# (không VB-interop), `Error` là tên idiomatic; đổi tên chỉ hại đọc. Đây là quyết định policy, không phải giấu bug.
- **Sửa hiểu lầm UUIDv7 (quan trọng):** test ban đầu dùng `Guid` sort để kiểm "monotonic" là SAI — `Guid.CompareTo` KHÔNG phản ánh thứ tự thời gian UUIDv7 (đúng cảnh báo TK-002). Test đúng: trích **48-bit timestamp prefix** (big-endian, `ToByteArray(bigEndian:true)`) và khẳng định không-giảm theo thứ tự sinh. Thứ tự index trên PostgreSQL là chuyện DB (benchmark riêng — TK-002), không kiểm ở unit test.
- **Không Docker (user):** integration test (Testcontainers) HOÃN; thêm placeholder `[Fact(Skip)]` ở `Foundation.IntegrationTests` để `dotnet test` không báo "No test available". Unit + Architecture test đủ chạy trên Windows.
- Kết quả: build 0/0; UnitTests 20/20, ArchitectureTests 5/5, IntegrationTests 1 skipped.
- Refs: `foundation/src/Foundation.SharedKernel/**`, `foundation/tests/Foundation.UnitTests/SharedKernel/**`; TK-002/034/035.
- Reversible?: có.

### DEC-037: Wave 1 — Nhóm 2 (Application abstractions) + Nhóm 3a (Infrastructure authN adapters, DB-free)
- Status: Accepted (2026-07-04) — build + test verified (38 unit, 5 arch, 1 skip)
- Ưu tiên user: làm login/xác thực/ủy quyền trước; phần cần Postgres HOÃN → chỉ làm lõi KHÔNG cần DB.
- **Nhóm 2 — Application abstractions (thuần interface):** `IRepository<T>` (chỉ ChangeTracker, không SaveChanges), `IUnitOfWork` (điểm ghi duy nhất + `ExecuteInTransactionAsync`), `IUseCase`/`IUseCase<TIn,TOut>`/`ICommandUseCase<TIn>`, `PagedRequest`/`PagedResult<T>`, port generic `IDateTimeProvider`/`ITokenGenerator`/`IHtmlSanitizer`/`ICurrentUser`, port auth `IPasswordHasher`(+`PasswordVerificationResult`) và `IJwtTokenService`(+`AccessToken`/`TokenIssueRequest`). Application vẫn ⊥ EFCore/AspNetCore (arch test 5/5).
- **Nhóm 3a — Infrastructure adapters (DB-free, tested):**
  - `SystemDateTimeProvider`.
  - `CryptoTokenGenerator`: `RandomNumberGenerator` → base64url (≥43 ký tự cho 32 byte); test entropy/charset/distinct/min-length.
  - `Argon2idPasswordHasher` (Konscious 1.3.1): hash → **PHC string** `$argon2id$v=19$m,t,p$salt$hash`; verify **hằng-thời-gian** (`CryptographicOperations.FixedTimeEquals`); **rehash detection** khi tham số lưu ≠ cấu hình; không throw khi hash hỏng (parse lỗi → Failed). Test: PHC format, không lộ plaintext, salt ngẫu nhiên (2 hash khác nhau), correct→Success, wrong→Failed, malformed→Failed, upgraded-params→SuccessRehashNeeded.
  - `JwtTokenService` (System.IdentityModel.Tokens.Jwt 8.19.1): HS256, claim sub/role/jti, thời gian từ `IDateTimeProvider`, `MapInboundClaims=false`, `ClockSkew=0`, `RoleClaimType="role"`/`NameClaimType="sub"`; Validate trả `ClaimsPrincipal?` (null nếu invalid, catch `SecurityTokenException`/`ArgumentException` — KHÔNG catch Exception, tránh CA1031). Test: roundtrip sub/roles, tampered→null, expired→null, wrong-issuer→null, extra-claims.
- Fix analyzer tận gốc (không suppress bừa): CA1861 (mảng literal trong test JWT) → `static readonly string[]`. Argon2/JWT catch **exception cụ thể** (không CA1031).
- Options POCO (`JwtOptions`, `PasswordHashingOptions`) inject qua constructor (test dựng trực tiếp) — DI wiring (Scrutor) + validate-on-startup để bước sau.
- CHƯA làm (cần DB hoặc bước sau): DbContext/Repository EF impl, refresh-token rotation store, ICurrentUser đọc HttpContext (tầng Api), authZ policy ASP.NET, DI Scrutor, validation behavior (FluentValidation), login use case.
- Refs: `foundation/src/Foundation.Application/**`, `foundation/src/Foundation.Infrastructure/{Time,Security}/**`, `foundation/tests/Foundation.UnitTests/**`; DEC-016 (auth design); TK-026 (tune Argon2).
- Reversible?: có (tham số/thuật toán chỉnh được; schema chưa đụng).

### DEC-038: Wave 1 — Identity flows (Login + Refresh rotation/reuse-detection + Logout), DB-free testable
- Status: Accepted (2026-07-04) — build + test verified (48 unit, 5 arch, 1 skip)
- Bám thiết kế `12-identity-and-auth.md` §1–4. Hiện thực ở Application (use case + port), impl adapter ở Infrastructure; DB tách qua abstraction (`IUserAuthStore`, `IRefreshTokenStore`) → test bằng fake in-memory, KHÔNG cần Postgres.
- **Ports (Application/Identity):** `IUserAuthStore` (FindByEmail/Id, UpdatePasswordHash), `IRefreshTokenStore` (FindByHash/Add/Update/RevokeFamily), `IRefreshTokenHasher`. Contracts: `AuthenticatedUser`, `AuthTokens`, `RefreshTokenRecord` (FamilyId/ReplacedByTokenId/RevokedReason — khớp `04`§3/`12`§3). `AuthErrors` (invalid_credentials/invalid_refresh_token/refresh_token_expired → 401).
- **LoginUseCase:** verify Argon2id constant-time; **chống user-enumeration** = LUÔN verify (hash giả cache tĩnh nếu user không tồn tại) + lỗi mơ hồ đồng nhất; **rehash-on-login** khi SuccessRehashNeeded; phát access JWT + refresh token (family mới), lưu **hash** refresh.
- **RefreshTokenUseCase:** rotation one-time-use (thu hồi cũ + cấp mới cùng family) + **reuse-detection** (trình lại token đã thu hồi → RevokeFamily toàn bộ → buộc login lại); expired/inactive-user xử lý riêng.
- **LogoutUseCase:** thu hồi token hiện tại, idempotent.
- **Infrastructure adapter:** `Sha256RefreshTokenHasher` (SHA-256 hex, tất định) — hợp lý vì refresh token entropy cao (khác mật khẩu cần Argon2).
- Fix GỐC (không fix ngọn): 1 test login fail vì `JwtSecurityTokenHandler` kiểm hạn theo **wall-clock** trong khi test dùng clock giả ngày cố định. → thêm `LifetimeValidator` dùng **`IDateTimeProvider` được inject** cho `JwtTokenService.Validate` → xác thực tất định, đúng nguyên tắc "IDateTimeProvider ở mọi nơi"; test expiry sạch bằng cách đẩy clock giả.
- Test: login valid/wrong-pw/unknown-email/inactive/rehash; refresh rotate-revoke-old / reuse→revoke-family / expired / unknown / logout-revoke. Application vẫn ⊥ EFCore/AspNetCore (arch 5/5).
- CHƯA (cần DB/bước sau): EF impl của 2 store + DbContext + migration; tầng Api (ICurrentUser từ HttpContext, JWT Bearer wiring, policy RequireAdmin/RequireStaff, endpoint /auth/login|refresh|logout, cookie refresh HttpOnly); lockout brute-force (TK-008); DI Scrutor.
- Refs: `foundation/src/Foundation.Application/Identity/**`, `Infrastructure/Security/Sha256RefreshTokenHasher.cs`, tests `Identity/**`; DEC-016/037, DEV-012.
- Reversible?: có.

### DEC-039: Wave 1 — DI theo convention (Scrutor + marker) thỏa Req 2, test không cần host/DB
- Status: Accepted (2026-07-04) — build + test verified (51 unit, 5 arch, 1 skip)
- `Foundation.Infrastructure/DependencyInjection/DependencyInjectionExtensions.AddFoundationServices`:
  - Scrutor (7.0.0) quét marker `IScopedService/ISingletonService/ITransientService` → đăng ký `AsImplementedInterfaces` + lifetime tương ứng (Req 2.1).
  - Chỉ quét assembly truyền tường minh (mặc định Application + Infrastructure qua `AssemblyMarker`) — Req 2.2; bỏ hẳn ForceLoadAssembly của reference (P9/DEV-005).
  - Interface 0 impl → Scrutor tự bỏ qua (Req 2.3); nhiều impl → đăng ký hết (Req 2.5).
  - **Guard xung đột lifetime (Req 2.6):** reflect concrete class; class hiện thực ≥2 marker → ném `InvalidOperationException` nêu tên type (fail-fast) TRƯỚC khi scan.
- Test (UnitTests, dùng `Microsoft.Extensions.DependencyInjection` 10.0.9): resolve đúng impl (ITokenGenerator→CryptoTokenGenerator, IPasswordHasher→Argon2idPasswordHasher, IDateTimeProvider→SystemDateTimeProvider, IRefreshTokenHasher→Sha256RefreshTokenHasher, IJwtTokenService→JwtTokenService); lifetime (singleton same-instance, use case scoped khác-instance giữa 2 scope); conflict fixture → throw.
- **Phát hiện xác nhận thiết kế (không phải bug):** resolve `LoginUseCase` cần `IUnitOfWork` + `IUserAuthStore`/`IRefreshTokenStore` — base KHÔNG có impl (thuộc EF/DB, hoãn). Test cấp fake để hoàn tất đồ thị. Đúng bản chất "base sạch = app phải cung cấp store/UoW + DB". `validateScopes:true` trong test để bắt lỗi scope.
- CHƯA (tầng Api / cần DB): bind Options từ IConfiguration + `ValidateOnStart` (fail-fast) thuộc host; JWT Bearer + policy; ProblemDetails middleware; ICurrentUser từ HttpContext; endpoint auth; EF UnitOfWork/stores + migration.
- Refs: `foundation/src/Foundation.Infrastructure/DependencyInjection/**`, tests `Infrastructure/DependencyInjectionTests.cs`; Req 2; DEC-002.
- Reversible?: có.

### DEC-040: Wave 1 — Tầng Api (Foundation.Api): Options fail-fast + ProblemDetails + JWT Bearer/AuthZ + endpoint /auth/* + integration test HTTP
- Status: Accepted (2026-07-04) — build + test verified (51 unit, 5 arch, 5 integration HTTP + 1 skip)
- **Options fail-fast (DEC-020):** `AddFoundationOptions` bind Jwt/PasswordHashing/RefreshToken + DataAnnotations + `ValidateOnStart`; `JwtOptionsValidator` (IValidateOptions) chặn SigningKey < 32 byte (256-bit). POCO bridge (`AddSingleton(sp=>IOptions<T>.Value)`) để service nhận giá trị đã validate.
- **Error handling (Req 4):** `ErrorTypeToHttp` (ErrorType→status), `ProblemDetailsBuilder` (đủ 5 trường type/title/status/code/traceId), `ResultExtensions.ToHttpResult` (Result→IResult), `ExceptionHandlingMiddleware` (unhandled→500 unexpected + traceId, KHÔNG lộ stack; OperationCanceled→không trả body). Dùng **LoggerMessage source-gen** (CA1848) + return type `ProblemHttpResult` (CA1859) — fix gốc, không suppress (trừ CA1031 khoanh vùng ở last-resort handler, có lý do).
- **Auth/AuthZ:** JWT Bearer (sub/role, ClockSkew=0, MapInboundClaims=false), policy `RequireAdmin`/`RequireStaff`, `HttpContextCurrentUser : ICurrentUser`. Endpoint minimal API `/auth/login|refresh|logout|me`: access token trả body (client giữ in-memory), refresh token cookie **HttpOnly/Secure/SameSite=Strict, Path=/auth**.
- **Fix GỐC quan trọng:** ban đầu `AddFoundationAuth` đọc config **eager** để dựng khóa Bearer → với WebApplicationFactory, in-memory config merge SAU → Bearer nhận SigningKey rỗng → 401 dù token hợp lệ (bên ký lazy đúng khóa, bên verify eager sai khóa). → cấu hình `JwtBearerOptions` **lazy qua `IOptions<JwtOptions>`** (bên ký & verify dùng chung nguồn đã merge/validate). Bài học: KHÔNG đọc config eager lúc đăng ký cho thứ phụ thuộc cấu hình động.
- **Integration test HTTP (DB-free):** `AuthApiFactory : WebApplicationFactory<Program>` cấp in-memory store/UoW (singleton giữ state) + config test; `Program` có `public partial class Program`. Test thật: login valid→token + `/me` OK; wrong-pw→401 problem code=invalid_credentials + traceId; no-token→401; refresh→rotate token mới; reuse cookie cũ→401. Cookie xử lý thủ công (HandleCookies=false) tránh vướng Secure-over-http.
- **Xác nhận thiết kế:** base KHÔNG đăng ký store/UoW (cần DB) → app tự cấp; thiếu → DI fail-fast lúc khởi động (đúng ý đồ). Program.cs ghi rõ điều này.
- Packages pin: JwtBearer 10.0.9, Mvc.Testing 10.0.9.
- Refs: `foundation/src/Foundation.Api/**`, `foundation/tests/Foundation.IntegrationTests/Auth/**`; Req 4/11; §03 §1; DEC-014/016/020/024.
- Reversible?: có.

### DEC-041: Wave 1 — Cross-cutting Req 12 (Validation pipeline + HtmlSanitizer), DB-free, test đủ
- Status: Accepted (2026-07-04) — build + test verified (61 unit, 5 arch, 5 integration + 1 skip)
- Tham khảo `Reference/Backend`: **không có** sanitizer/validation pipeline (đã đối chiếu cây file P-catalog) → không có gì để mượn ở mảng này; thiết kế mới theo `03`§1-2.
- **HtmlSanitizer (Req 12.3-12.6):** `HtmlSanitizerAdapter` dùng `Ganss.Xss` (HtmlSanitizer 9.0.892) — allowlist mặc định (đã loại script/on*/javascript:) + **tắt `<img>`** mặc định (DEC-018); rỗng/null → chuỗi rỗng. Cấu hình 1 lần trong ctor (dùng như singleton). Test: bỏ script/on*/javascript:/img, giữ định dạng cơ bản, rỗng→"".
  - TK phụ: Ganss `HtmlSanitizer` dùng singleton — cấu hình bất biến sau ctor nên Sanitize an toàn concurrent theo usage phổ biến; verify lại nếu mở rộng cấu hình động.
- **Validation pipeline (Req 12.1/12.2):** vì base KHÔNG dùng MediatR (use-case-per-operation trực tiếp), chọn **decorator** `ValidationUseCaseDecorator<TIn,TOut>` bọc mọi `IUseCase<,>` — chạy FluentValidation (12.1.1) trước thân; sai → `validation_error` + **field errors**, KHÔNG chạy thân (12.2); không có validator → pass-through. Đăng ký qua Scrutor **`TryDecorate(typeof(IUseCase<,>), ...)`** (không ném khi chưa có use case). Inject `IEnumerable<IValidator<TIn>>` (rỗng nếu app chưa thêm validator).
- **Mở rộng `Error`:** thêm `Details: IReadOnlyDictionary<string,string[]>?` + `WithDetails()` (immutable) để mang danh sách field lỗi; `ProblemDetailsBuilder` thêm extension `errors` khi có Details. Non-breaking (init property).
- Fix analyzer tận gốc: CA1859 (field `_validators` → `List<>` cụ thể). Namespace collision `Foundation.UnitTests.Application` che `Foundation.Application` trong DI test → fully-qualify `typeof(Foundation.Application.AssemblyMarker)`.
- Ảnh hưởng: `IUseCase<LoginCommand,AuthTokens>` giờ resolve ra decorator (bọc LoginUseCase); integration login vẫn chạy (chưa có validator cho LoginCommand → pass-through). Cập nhật DI test tương ứng.
- CHƯA (cross-cutting còn lại): security headers + CORS + HTTPS redirect (Req 20), rate limiting (Req 13), health check (Req 14.3/14.6-7 — /ready cần DB), Serilog structured logging (Req 14.1-2). Đều generic; security headers/health/logging làm được không cần DB.
- Refs: `foundation/src/Foundation.Infrastructure/Security/HtmlSanitizerAdapter.cs`, `Foundation.Application/Common/ValidationUseCaseDecorator.cs`, `Error.cs`, `ProblemDetailsBuilder.cs`; tests tương ứng; Req 12; §03§1-2; DEC-018.
- Reversible?: có.

### DEC-042: Wave 1 — Security headers + CORS + HSTS (Req 20), HTTPS-redirect ở proxy (không ở app)
- Status: Accepted (2026-07-04) — build + test verified (61 unit, 5 arch, 8 integration + 1 skip)
- **Security headers (Req 20.4/20.7):** `SecurityHeadersMiddleware` gắn `Content-Security-Policy` (default `default-src 'self'; frame-ancestors 'none'; connect-src 'self' wss:`) + `X-Content-Type-Options: nosniff` cho MỌI response (đặt sớm → có kể cả response 401/lỗi). Test: header có mặt trên /auth/me 401.
- **CORS (Req 20.1/20.2/20.3):** default policy allowlist origin tường minh (KHÔNG wildcard) + method cần thiết + AllowCredentials; `UseCors()` áp toàn cục kể cả preflight. Test: preflight allowed-origin→có ACAO đúng origin; disallowed-origin→204 KHÔNG có ACAO.
- **HSTS (Req 20.5):** `HstsOptions` MaxAge 365 ngày + IncludeSubDomains (header chỉ phát trên HTTPS → vô hại khi test http).
- **DEVIATION có chủ đích (Req 20.6 HTTPS redirect):** base **KHÔNG** bật `UseHttpsRedirection`. Lý do gốc: theo DEC-025, reverse proxy terminate TLS và forward http tới Kestrel loopback → nếu app tự redirect http→https sẽ gây **redirect loop** sau proxy (và vỡ integration test http). ⇒ HTTPS redirect là trách nhiệm **proxy** (Req 20.6 thỏa ở tầng deployment). App tự phục vụ TLS mới bật `UseHttpsRedirection` riêng. Ghi rõ trong `UseFoundation` + Program.
- **LẶP LẠI bài học DEC-040 (quan trọng):** ban đầu `AddFoundationSecurity` đọc config EAGER để dựng CORS policy → với WebApplicationFactory, in-memory config merge SAU → policy 0 origin → preflight 204 không ACAO. Fix gốc: cấu hình `CorsOptions`/`HstsOptions` **lazy qua `Configure<IOptions<SecurityOptions>>`**. ⇒ **Nguyên tắc chốt: mọi thứ phụ thuộc cấu hình động PHẢI đọc lazy qua IOptions, KHÔNG GetSection().Get<>() eager lúc đăng ký.** (đã dính 2 lần: JWT bearer + CORS).
- Pipeline `UseFoundation` (thứ tự): Exception → HSTS → SecurityHeaders → CORS → AuthN → AuthZ.
- CHƯA (cross-cutting còn lại DB-free): rate limiting (Req 13), health `/live` + Serilog structured logging + mask secret (Req 14.1-3). `/ready` + EF cần DB.
- Refs: `foundation/src/Foundation.Api/Security/{SecurityOptions,SecurityHeadersMiddleware,FoundationSecurityExtensions}.cs`, `FoundationApiExtensions.cs`; tests `Auth/SecurityHeadersTests.cs`; Req 20; §03§7; DEC-025/040.
- Reversible?: có (bật HttpsRedirection nếu app tự TLS).

### DEC-043: Expert review #2 (7 findings) — verify từng dòng + sửa tận gốc (2026-07-04)
- Status: Accepted — build + test verified (64 unit, 5 arch, 8 integration + 1 skip). Reviewer chấm 7.4/10; đã đóng cả 7.
- **#1 Race refresh rotation (đúng, nghiêm trọng):** trước đây read record (còn sống) → set RevokedAt → save (cửa sổ read-then-write) → 2 request đồng thời cùng thấy chưa-revoke → cùng cấp token mới. **Fix gốc:** thêm `IRefreshTokenStore.TryConsumeAsync` — **consume-if-not-revoked NGUYÊN TỬ** (EF impl PHẢI conditional `UPDATE ... WHERE revoked_at IS NULL` + rows-affected / optimistic concurrency). Use case: chỉ caller thắng TryConsume mới cấp token mới; thua → RevokeFamily + fail. Fake store mô phỏng bằng lock check-and-set; test `TryConsume` 2 lần → 1 true/1 false.
- **#2 Validator không đăng ký + command không decorate (đúng):** thêm `AddValidatorsFromAssemblies(assemblies, includeInternalTypes:true)` trong `AddFoundationServices` (đăng ký IValidator từ chính assembly được quét). Thêm `ValidationCommandUseCaseDecorator<TIn>` + `TryDecorate(typeof(ICommandUseCase<>))`. Test DI: `ICommandUseCase<LogoutCommand>` resolve ra decorator.
- **#3 401/403 không phải ProblemDetails (đúng):** thêm `JwtBearerEvents.OnChallenge` (HandleResponse → 401 problem `unauthorized`) + `OnForbidden` (403 problem `forbidden`), đủ code+traceId+`application/problem+json`. **Bẫy đã fix:** `WriteAsJsonAsync` mặc định ép `application/json` → dùng overload `(value, JsonSerializerOptions?, contentType)`; sửa cả ExceptionHandlingMiddleware. Test: /auth/me không token → 401 problem+json code=unauthorized.
- **#4 RequireStaff chặn nhầm Admin (đúng):** RBAC phân cấp — Admin là superset của Staff. `RequireStaff` = `RequireRole("Staff","Admin")`; `RequireAdmin` = `Admin`. (Quyết định do spec Req 11.6 không nói rõ Admin ⊇ Staff; chọn superset theo chuẩn thương mại.)
- **#5 Base không chạy độc lập / README (đúng):** viết lại `foundation/README.md` — nêu rõ đây là BASE/TEMPLATE, app PHẢI cấp `IUnitOfWork`/`IUserAuthStore`/`IRefreshTokenStore`/`AppDbContext`; thiếu → DI fail-fast; hướng dẫn Program.cs + config bắt buộc + lưu ý HTTPS-redirect-ở-proxy + Docker cho integration test.
- **#6 Integration DB/concurrency chưa thật (đúng — hoãn):** Testcontainers vẫn skip (không Docker — TK-034). Atomic-consume đã test ở mức store (logic); transaction/unique/xmin/race-DB thật còn chờ Docker → TK-036.
- **#7 Entity cho Guid.Empty lọt qua init (đúng):** `Entity.Id` init-guard ném `ArgumentException` khi `Guid.Empty` (chặn cả object initializer); bỏ né-Guid.Empty trong Equals (invariant đã đảm bảo). Test: `new X { Id = Guid.Empty }` → throw.
- Điểm mạnh reviewer ghi nhận (giữ nguyên): layering + arch test negative-control, TreatWarningsAsErrors, dummy-hash chống enumeration, refresh lưu hash, cookie HttpOnly/Secure/Strict, JWT clock inject.
- Refs: `RefreshTokenUseCase.cs`, `Identity/Abstractions.cs`, `DependencyInjectionExtensions.cs`, `ValidationCommandUseCaseDecorator.cs`, `FoundationAuthExtensions.cs`, `Entity.cs`, `foundation/README.md`; tests tương ứng.
- Reversible?: các fix là cải thiện đúng đắn, không nên đảo.

### DEC-044: Wave 1 — Observability nền (Req 14): Serilog JSON + CorrelationId + health /live + mask secret
- Status: Accepted (2026-07-04) — build + test verified (64 unit, 5 arch, 16 integration + 1 skip)
- Bám `19-observability.md`. DB-free (không cần Docker).
- **Structured logging (Serilog.AspNetCore 10.0.0):** `AddSerilog` JSON (`JsonFormatter renderMessage:true`) + `Enrich.FromLogContext`. `UseSerilogRequestLogging` với template **KHÔNG chứa RequestPath gốc** → dùng `RequestPathMasked` (Req 14.2). Serilog KHÔNG log header mặc định → Authorization/Cookie/Set-Cookie không lộ.
- **Mask token trong path:** `PathMasker` (hàm thuần, test được): `/r/{token}`→`/r/***`, `/api/guest/resolve/{token}`→`/api/guest/resolve/***`; path khác giữ nguyên.
- **CorrelationId (19 §2):** `CorrelationIdMiddleware` nhận `X-Correlation-Id` client gửi (hợp lệ, ≤128) hoặc `Activity.Id`/`TraceIdentifier`; đẩy vào Serilog `LogContext` (mọi log của request mang CorrelationId) + set header response. Test: header có mặt + echo id client gửi.
- **Health (Req 14.3):** `/health/live` liveness (Predicate `_=>false` → không chạy check nào → 200, không phụ thuộc DB). Test 200.
- **Deviation/hoãn:** `/health/ready` (kiểm DB Npgsql, timeout 5s — Req 14.6/14.7) HOÃN cùng EF/DB (cần Postgres). `VisitIdleSweeper` (Req 14.4-5-8) thuộc nghiệp vụ GuestVisit (resort) → wave sau.
- Pipeline `UseFoundation` cập nhật: Exception → **Observability (correlation + request logging)** → HSTS → SecurityHeaders → CORS → AuthN → AuthZ. Program thêm `MapFoundationHealthChecks`.
- Refs: `foundation/src/Foundation.Api/Observability/**`, `FoundationApiExtensions.cs`, `Program.cs`; tests `Observability/**`; Req 14; §19; DEC-023.
- Reversible?: có.

### DEC-045: Wave 1 — Rate limiting nền (Req 13), DB-free
- Status: Accepted (2026-07-04) — build + test verified (64 unit, 5 arch, 17 integration + 1 skip = 86 passed)
- Bám `03`§5/§8. Base cung cấp **cơ chế generic**; policy nghiệp vụ guest (resolve theo IP, guest-write theo GuestSessionId — Req 13.1-3) do resort thêm trên nền.
- `FoundationRateLimitExtensions.AddFoundationRateLimiting` (`Microsoft.AspNetCore.RateLimiting`, built-in shared framework): policy `auth` = **fixed-window partition theo IP**; `OnRejected` trả **429 ProblemDetails code=rate_limited** + header **Retry-After** (từ `MetadataName.RetryAfter`). Áp `.RequireRateLimiting("auth")` vào nhóm `/auth` (chống brute-force login — generic, default 100/60s không cản dùng thường).
- **Lazy config (áp dụng bài học DEC-040/042):** đọc `RateLimitOptions` từ DI **trong partition factory mỗi request** (`httpContext.RequestServices.GetRequiredService<RateLimitOptions>()`), KHÔNG capture eager lúc đăng ký → khớp cấu hình đã merge (quan trọng cho WebApplicationFactory + reload).
- Options bind + DataAnnotations + ValidateOnStart. Partition key = RemoteIpAddress (sau ForwardedHeaders ở deploy — app/proxy cấu hình KnownProxies, TK-027) hoặc "unknown".
- Pipeline `UseFoundation`: ... CORS → **UseRateLimiter** → AuthN → AuthZ (rate-limit trước auth để chặn flood ẩn danh).
- Test: factory riêng ngưỡng 2/60s (WithWebHostBuilder, tránh dính state test khác) → POST /auth/login 3 lần → lần 3 = 429 problem+json code=rate_limited. Preflight OPTIONS KHÔNG bị đếm (CORS short-circuit trước rate limiter).
- Refs: `foundation/src/Foundation.Api/RateLimiting/**`, `AuthEndpoints.cs`, `FoundationApiExtensions.cs`; tests `RateLimitTests.cs`; Req 13; §03§5/§8; DEC-018/040/042.
- Reversible?: có.

### DEC-046: Wave 1 — Localization resolver (Req 9), thuật toán thuần DB-free
- Status: Accepted (2026-07-04) — build + test verified (76 unit, 5 arch, 17 integration + 1 skip = 98 passed)
- Bám `18-localization.md` §2-5. Đây là **content localization** (nội dung DB theo LanguageCode) — KHÁC UI text (vue-i18n ở FE). Logic thuần (không I/O) → generic, tái dùng, test được.
- **`ITranslation`** (SharedKernel): `LanguageCode` + `HasContent` (entity tự quyết "rỗng" — điểm tinh vi "có row ≠ có nội dung").
- **`ITranslationResolver`** (Application port) + **`TranslationResolver`** (Infrastructure impl, ISingletonService):
  - `MatchSupported`: khớp chính xác → primary-subtag (`ko-KR`→`ko`, `zh-Hant`→`zh`) → default; không phân biệt hoa/thường. **Không dùng ToLowerInvariant** (tránh CA1308) — so trực tiếp `OrdinalIgnoreCase`, trả canonical code theo enabledCodes.
  - `Resolve<T>`: requested không rỗng → default không rỗng (IsFallback) → missing. "Row rỗng = thiếu" (Req 9.2/9.3/9.6).
  - `MissingLanguages<T>`: mã đã bật nhưng chưa có bản dịch không rỗng (Req 8.7, cho admin editor).
- **`Translated<T>`** (record) + static `Translated` factory (Found/Fallback/Missing) — tránh CA1000 (static trên generic type), cùng pattern Result.
- Giới hạn có chủ đích: primary-subtag gộp phồn/giản (`zh-Hant`/`zh-Hans`→`zh`); phân biệt cần bật mã riêng (TK-029, DEC-022).
- Storage (entity + *Translation, unique (ParentId,LanguageCode)) + resolve-set-1-query chống N+1 = EF/DB → hoãn cùng persistence. Base cung cấp *thuật toán*; resort ráp với entity + query.
- Test: MatchSupported (7 case), Resolve exact/fallback/empty-as-missing/missing, MissingLanguages.
- Refs: `Foundation.SharedKernel/Entities/ITranslation.cs`, `Foundation.Application/Localization/**`, `Foundation.Infrastructure/Localization/TranslationResolver.cs`; tests; Req 9; §18; DEC-022.
- Reversible?: có.

### DEC-047: Thiết kế tầng EF persistence cho Foundation (chưa code — chờ DB) + chiến lược test SQLite/Testcontainers
- Status: Design accepted (2026-07-04) — DB-free (soạn thiết kế, chưa triển khai). Doc: `foundation/docs/persistence-layer-design.md`.
- Bối cảnh: base generic DB-free đã xong (98 test); phần persistence bắt buộc cần Postgres nhưng user chưa cài DB. Theo triết lý design-first: soạn thiết kế rõ + kiểm chứng trước, code khi có DB.
- Quyết định thiết kế then chốt:
  1. **Generic vs app-specific:** Foundation cung cấp `FoundationDbContext` base (conventions) + `EfRepository<T>`/`EfUnitOfWork` + entity `RefreshToken` + `EfRefreshTokenStore` (refresh token là generic). App cung cấp entity nghiệp vụ + DbContext dẫn xuất + `IUserAuthStore` impl + migration.
  2. **xmin CÓ ĐIỀU KIỆN theo provider:** `UseXminAsConcurrencyToken()` là Npgsql-only → chỉ gọi khi `Database.IsNpgsql()`. Lý do gốc: cho phép **test provider-agnostic bằng SQLite (không Docker)** mà không vỡ; xmin thật kiểm bằng Testcontainers/Postgres. KHÔNG hardcode xmin vô điều kiện.
  3. **Atomic `TryConsumeAsync` = `ExecuteUpdateAsync` `WHERE Id=@id AND RevokedAt IS NULL` → rowcount** (một câu UPDATE nguyên tử, row-lock) — fix GỐC race refresh (khớp expert #1/DEC-043). Hỗ trợ Npgsql+SQLite (EF7+) → test rowcount bằng SQLite được; race đa-connection thật bằng Testcontainers.
  4. **Concurrency → 409 giữ Api KHÔNG phụ thuộc EF:** UoW bắt `DbUpdateConcurrencyException` → ném `ConcurrencyConflictException` (SharedKernel) → Api `MapException` map 409 concurrency_conflict. (Api chỉ phụ thuộc SharedKernel, không EF.)
  5. **Chiến lược test hòa giải DEC-021:** DEC-021 cấm **EF InMemory** (không enforce → false-green). **SQLite là DB quan hệ THẬT** → dùng cho hành vi provider-agnostic (audit/soft-delete/UoW-rollback/TryConsume-rowcount) KHÔNG cần Docker. Nhưng Postgres-specific (**xmin 409, partial unique index B3, timestamptz, race đa-connection**) VẪN BẮT BUỘC Testcontainers — không kết luận đúng từ SQLite. Mỗi test ghi nhãn nhóm.
- Ảnh hưởng khi implement: thêm `ConcurrencyConflictException` (SharedKernel) + case ở Api MapException; `/health/ready` (Npgsql health check tag "ready", 5s); `ConnectionStrings:Postgres` + validate-on-start.
- Refs: `foundation/docs/persistence-layer-design.md`; 03§1/§3, 04§3/§7/§8/§9, 05§2, 23§5/§7; DEC-005/006/020/021/043; TK-025/034/036.
- Reversible?: là thiết kế — điều chỉnh trước khi code.

### DEC-048: Triển khai tầng EF persistence (Wave 1.5) — DB-free bằng SQLite, verify version thật
- Status: Accepted (2026-07-04) — build 0W/0E + test verified **110 passed** (76 unit, 5 arch, **31 integration** + 1 skip). Hiện thực hóa DEC-047.
- Package PIN version THẬT (verify qua `dotnet add` + build, KHÔNG bịa): `Microsoft.EntityFrameworkCore` 10.0.9, `Npgsql.EntityFrameworkCore.PostgreSQL` **10.0.2**, `Microsoft.EntityFrameworkCore.Design` 10.0.9, `EFCore.NamingConventions` **10.0.1** (tương thích EF10 ✅), `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` 10.0.9; test: `Microsoft.EntityFrameworkCore.Sqlite` 10.0.9.
- Files tạo: `Foundation.SharedKernel/Results/ConcurrencyConflictException.cs`; `Foundation.Infrastructure/Persistence/{FoundationDbContext, RefreshTokenConfiguration, EfRepository, EfUnitOfWork, EfRefreshTokenStore, FoundationPersistenceExtensions}.cs`. Sửa: `ExceptionHandlingMiddleware` (map 409), `FoundationObservabilityExtensions` (`/health/ready`).
- Convention nền (test SQLite thật): audit set Created/Updated + chống ghi đè Created khi update; xóa mềm (query filter `!IsDeleted` + Delete→Modified + DeletedAt); snake_case (EFCore.NamingConventions verify chạy trên EF10 — cột `token_hash/user_id/...`).
- `EfUnitOfWork.ExecuteInTransactionAsync` dùng `CreateExecutionStrategy` (tương thích Npgsql retry) + `await using` transaction (rollback mọi exception, all-or-nothing) — test rollback + commit trên SQLite.
- Reversible?: có (thêm file + package; gỡ được).

### DEC-049: Persistence nền wire TƯỜNG MINH (opt-in) — loại khỏi Scrutor auto-scan
- Status: Accepted (2026-07-04) — test verified.
- Vấn đề GỐC: `IRefreshTokenStore : IScopedService` nên Scrutor sẽ tự đăng ký `EfRefreshTokenStore` (nằm trong assembly Infrastructure được quét). Mà `EfRefreshTokenStore`/`EfUnitOfWork` hard-require `FoundationDbContext` — thứ base KHÔNG cung cấp. Auto-đăng ký → thiếu DbContext sẽ lỗi lúc CHẠY (per-request), khó chẩn đoán, và có thể đăng ký TRÙNG với impl của app.
- Quyết định: (1) loại **namespace `Foundation.Infrastructure.Persistence`** khỏi auto-scan (`.NotInNamespaceOf<FoundationDbContext>()` ở cả 3 lifetime trong `DependencyInjectionExtensions`); (2) cung cấp `AddFoundationPersistence<TContext>()` đăng ký TƯỜNG MINH alias `FoundationDbContext`→TContext + `IUnitOfWork`→`EfUnitOfWork` + `IRefreshTokenStore`→`EfRefreshTokenStore` + health check DB tag "ready". Dùng `TryAdd*` để app override được.
- Lý do (fix tận gốc): phụ thuộc DbContext hiện rõ ở một chỗ; một đăng ký duy nhất, không mập mờ; app dùng store khác chỉ cần bỏ `AddFoundationPersistence`. Không ảnh hưởng auto-đăng ký impl của app (namespace khác).
- Verify: auth integration tests (in-memory stores) vẫn xanh — chứng minh không vỡ và không dính DbContext.
- Reversible?: có.

### DEC-050: Map `RefreshTokenRecord` TRỰC TIẾP làm EF entity (bỏ lớp `RefreshToken:Entity` riêng trong doc thiết kế)
- Status: Accepted (2026-07-04) — deviation so với `persistence-layer-design.md` §4 (xem DEV-021).
- `RefreshTokenRecord` (Application) đã tự mô tả "persistence-facing" + đúng đủ cột bảng. Tạo thêm `RefreshToken:Entity` + code map qua lại là trùng lặp (vi phạm DRY) không thêm giá trị (refresh token không cần audit/xmin/soft-delete). EF Core 10 map được `required`/`init` property.
- `EfRefreshTokenStore` thao tác thẳng `RefreshTokenRecord`; `IRepository<T> where T:Entity` KHÔNG áp cho refresh token (dùng store chuyên biệt, không generic repo) → không cần nó là `Entity`.
- Verify: test store (add/find/consume/revoke/logout) xanh trên SQLite.
- Reversible?: có (tách lại nếu sau cần tách DTO/entity).

### DEC-051: Pin transitive `SQLitePCLRaw.lib.e_sqlite3` 3.50.3 vá CVE-2025-6965 (fix tận gốc, không tắt audit)
- Status: Accepted (2026-07-04) — verify `dotnet restore` sạch, không còn NU1903.
- Vấn đề: `Microsoft.EntityFrameworkCore.Sqlite` 10.0.9 kéo transitive `SQLitePCLRaw.lib.e_sqlite3` **2.1.11** (native SQLite < 3.50.2) → **CVE-2025-6965 / GHSA-2m69-gcr7-jv3q (HIGH)**. `TreatWarningsAsErrors` + NuGet audit → lỗi build NU1903.
- Fix GỐC: `CentralPackageTransitivePinningEnabled=true` sẵn có → thêm `<PackageVersion Include="SQLitePCLRaw.lib.e_sqlite3" Version="3.50.3" />` (native SQLite ≥ 3.50.2 đã vá). KHÔNG dùng `NoWarn`/tắt audit (giữ audit sống để bắt lỗ hổng tương lai). Chỉ test project tham chiếu Sqlite → chỉ nó nạp native lib; production base không dính.
- Verify version thật: NuGet `sqlitepclraw.lib.e_sqlite3` mới nhất = 3.50.3.
- Reversible?: có (gỡ khi EF Sqlite chính thức nâng transitive).

### DEC-052: Fork base `foundation/` → `resort-qr/` (workspace app), giữ tên `Foundation.*`, base gốc SẠCH
- Status: Accepted (2026-07-04) — copy verify build 0W/0E + **112 test xanh** trên `ResortQr.slnx` (76 unit + 5 arch + 31 integration + 1 skip).
- Bối cảnh: base đã hoàn chỉnh ở trạng thái "clean base" (persistence DB-free xong, chỉ còn phần cần Docker). User yêu cầu copy ra thư mục mới cho Resort QR, giữ base sạch.
- Hành động: `robocopy foundation resort-qr /E /XD bin obj .vs` (copy SẠCH, loại artifact); đổi solution `Foundation.slnx`→`ResortQr.slnx`; `foundation/` giữ NGUYÊN VẸN.
- Quyết định tên: **GIỮ `Foundation.*`** trong bản copy (KHÔNG rename thành `ResortQr.*`). Lý do (bản chất, không fix ngọn):
  1. Foundation là *base/framework* của app — mô hình thương mại là "app build TRÊN framework", không đổi tên framework.
  2. Giữ đường **diff/merge** cập nhật base gốc `foundation/` về sau (rename sẽ chặn đứng khả năng đồng bộ vá lỗi/nâng cấp).
  3. Architecture tests đã enforce hướng phụ thuộc theo tên Foundation → không phải viết lại.
  Nghiệp vụ resort thêm dưới dạng project MỚI `ResortQr.{Domain,Application,Infrastructure,Api}` tham chiếu Foundation.*. Đã ghi lộ trình vào `resort-qr/README.md`.
  → Nếu user muốn rename toàn bộ namespace: làm một pass `semantic_rename`/đổi csproj + verify build+test (để mở, chưa làm vì đi ngược lợi ích diff/base).
- Không xung đột: hai bản `Foundation.*` (foundation/ và resort-qr/) ở hai solution/thư mục tách biệt, không bao giờ tham chiếu chung → không đụng assembly name.
- Refs: `resort-qr/ResortQr.slnx`, `resort-qr/README.md`; base `foundation/` (pristine).
- Reversible?: có (xóa `resort-qr/` là xong; base không đổi).

### DEC-052 UPDATE (2026-07-04): User CHỌN đổi tên toàn bộ `Foundation.*` → `ResortQr.*` trong bản copy
- Quyết định trước (giữ tên Foundation.* trong copy) bị **user override**: "có cứ copy ra ngoài thì đổi đi". Đã thực hiện rename TOÀN DIỆN trong `resort-qr/` (base gốc `foundation/` VẪN giữ nguyên `Foundation.*`).
- Cách làm (verify được, không sót): (1) dọn `bin/obj` (tránh sửa file sinh tự động); (2) thay token PascalCase `Foundation`→`ResortQr` trong TẤT CẢ file source (113 cs/csproj/slnx/props/json/editorconfig + 2 md) — an toàn vì KHÔNG package/type ngoài nào tên "Foundation"; (3) sửa giá trị test lowercase `foundation-it`→`resortqr-it` (2 file cs); (4) rename 8 file `.csproj` + 8 folder `Foundation.X`→`ResortQr.X`; (5) GIỮ lowercase `foundation/` trong README (đường dẫn base thật). Class/method đổi theo: `FoundationDbContext`→`ResortQrDbContext`, `AddFoundation*`→`AddResortQr*`, `UseFoundation`→`UseResortQr`, `MapFoundation*`→`MapResortQr*`.
- Verify: `ResortQr.slnx` build 0W/0E + **112 test xanh** (76 unit + 5 arch + 31 integration + 1 skip); architecture tests (kiểm namespace) xanh → rename nhất quán. Source còn 0 token `Foundation` (chỉ README còn tham chiếu cố ý tới base gốc).
- **HỆ QUẢ (ghi rõ để sau biết):** (1) sau rename, `resort-qr/` là cây ĐỘC LẬP — KHÔNG còn diff/merge trực tiếp với `foundation/`; lấy bản vá base về sau phải merge THỦ CÔNG theo layer. (2) Không còn tách "base vs app": nghiệp vụ resort thêm THẲNG vào project `ResortQr.{Domain,Application,Infrastructure,Api}` hiện có (ResortQr.Domain đang rỗng — nơi đặt entity resort). Lộ trình cập nhật trong `resort-qr/README.md`.
- Reversible?: có (xóa `resort-qr/` rồi copy lại từ base; base bất biến).

### DEC-053: Triển khai `ResortQr.Domain` — entity nền §3 (POCO thuần, design-first, KHÔNG cần DB)
- Status: Accepted (2026-07-04) — build 0W/0E; **112 test xanh** (76 unit + 5 arch + 31 integration + 1 skip). Architecture test xác nhận Domain chỉ phụ thuộc SharedKernel. Doc: `resort-qr/docs/domain-layer-design.md`.
- Phạm vi: 8 entity nền (`04` §3) + 4 enum. KHÔNG entity module (Rules/Faq/Messaging/Housekeeping — theo wave, TRD-002); KHÔNG RefreshToken (base sở hữu `RefreshTokenRecord`).
- Entity: `Resort`, `ResortSettings`, `ResortLanguage` (Resorts); `AppUser` (Identity); `Room`, `RoomQrToken` (Rooms); `GuestSession`, `GuestVisit` (Guests). Enum: `UserRole{Admin,Staff}`, `RoomStatus{Active,Inactive,Maintenance}`, `RoomQrTokenStatus{Active,Revoked}`, `GuestVisitStatus{Active,Closed,Expired}`.
- **Ánh xạ base-class (validate `04` §8):** tập cần concurrency `xmin` = {ResortSettings, Room, AppUser} **trùng khớp** tập cần audit → `AuditableEntity` (gộp audit+xmin) vừa vặn cả 3, KHÔNG cần tách base. Room thêm `ISoftDeletable`. Còn lại (Resort/ResortLanguage/RoomQrToken/GuestSession/GuestVisit) dùng `Entity`. Resort `CreatedAt` set khi seed (không convention). RoomQrToken lifecycle field set tường minh ở use case (`16`).
- **Nguyên tắc (fix gốc):** Domain POCO thuần — CHỈ phụ thuộc SharedKernel, KHÔNG EF/DataAnnotations/ASP.NET. Ràng buộc kỹ thuật (MaxLength/enum→string/xmin/partial-unique-index) đặt ở EF config (Infrastructure, bước sau) + validator (Application); Domain chỉ giữ bất biến kiểu (`required`/nullable) + guard `Guid.Empty` (từ Entity base). Anemic-có-kiểm-soát: thuật toán nghiệp vụ ở use case (`02`/`13`/`16`), entity là data-holder đóng gói (immutable dùng `required`+`init`, state đổi dùng `set`).
- **Reference-by-id:** FK là Guid scalar (`ResortId`/`RoomId`/`GuestSessionId`), CHƯA thêm navigation property (tránh lazy-loading + mô hình EF sớm + analyzer collection). Thêm nav khi use case/query thật cần.
- **Tenancy (`24`):** `ResortId` scalar trên entity tenant-scoped; `GuestSession` KHÔNG `ResortId` (thiết bị toàn cục/deployment — `24` §6); RoomQrToken scope qua Room. Instance-per-resort.
- `Role`/enum an toàn: `AppUser.Role` là `required` (chặn mặc định value-type ngoài ý muốn); status khác có default trạng thái tạo (Active).
- Verify analyzer: build 0W/0E dưới TreatWarningsAsErrors — KHÔNG dính CA1056 (LogoUrl/GuestWebBaseUrl string) hay CA1008 (enum zero-value) → giữ `string` cho URL (đúng spec, config-facing) hợp lệ với ruleset hiện tại.
- Refs: `resort-qr/docs/domain-layer-design.md`, `resort-qr/src/ResortQr.Domain/**`; `04` §3/§8/§9, `02` §1, `12`, `13`, `16`, `24`, `15`.
- Reversible?: có (POCO, chưa có EF mapping/DB).

### DEC-054: Triển khai `ResortQr.Infrastructure` EF mapping (AppDbContext + config + AppUserAuthStore + factory) — test SQLite Docker-free
- Status: Accepted (2026-07-04) — build 0W/0E; **126 test xanh** (76 unit + 5 arch + **45 integration** + 1 skip; +14 test EF mapping). Doc: `resort-qr/docs/persistence-mapping-design.md`.
- `AppDbContext : ResortQrDbContext` + 8 `IEntityTypeConfiguration<>` (base tự nạp qua `ApplyConfigurationsFromAssembly` cùng assembly) + `AppDbContextFactory` (design-time migration, đọc env, không thêm dependency config).
- Ràng buộc đặt ở EF config (KHÔNG ở Domain — DEC-053): enum→string (`HasConversion<string>`), MaxLength, FK `OnDelete(Restrict)` reference-by-id (không nav), unique/partial-unique index (`04` §5/§7): `ux_appuser_email`, `ux_qrtoken_token`, `ux_guestsession_key`, `ux_lang_code`, `ux_resort_settings_resort`(1-1), `ux_qr_active`(WHERE status='Active'), `ux_visit_active`, `ix_visit_sweep`, `ux_room_number`(WHERE is_deleted=false), `ux_lang_default`(WHERE is_default); CHECK constraint resort_settings (`04` §7.4).
- **Partial index filter provider-aware (fix gốc như xmin):** filter **enum-string** (`status='Active'`) provider-agnostic → đặt trong config; filter **BOOL** (`is_deleted`/`is_default`) khác literal (SQLite 0/1 vs Npgsql true/false) → sinh theo `Database.IsNpgsql()` ở `AppDbContext.OnModelCreating` (helper `ProviderPartialIndex.BoolEquals`).
- Conventions base (soft-delete filter + xmin-Npgsql) tự áp cho entity app vì DbSet đưa entity vào model TRƯỚC OnModelCreating → verify: test soft-delete+ux_room_number, model Npgsql xmin cho Room/AppUser/ResortSettings (offline).
- Test SQLite (DB quan hệ THẬT, bật `Foreign Keys=True`): enum round-trip, unique email/token, partial-unique ux_qr_active/ux_visit_active, ux_room_number + soft-delete tái tạo, **FK Restrict**, AppUserAuthStore find/update, model provider (xmin Npgsql vs SQLite). Postgres-specific (race đa-connection, xmin runtime, timestamptz) vẫn TK-036.
- **Fix DI (nhất quán DEC-049) — quan trọng:** `AppUserAuthStore` (impl `IUserAuthStore:IScopedService`) ban đầu ở namespace Identity → Scrutor TỰ đăng ký → nhưng cần DbContext chưa wire ở test container (auth in-memory + DI unit test) → **container validation FAIL** (đúng bản chất: service coupled-DbContext không nên auto-scan). Fix GỐC: chuyển `AppUserAuthStore` vào namespace `ResortQr.Infrastructure.Persistence` (loại khỏi auto-scan) + phụ thuộc `ResortQrDbContext` base (`Set<AppUser>()`) + đăng ký TƯỜNG MINH trong `AddResortQrPersistence`. Base tests giữ nguyên (Fake/in-memory), real app + real-DB test lấy store qua AddResortQrPersistence.
- Refs: `resort-qr/docs/persistence-mapping-design.md`, `resort-qr/src/ResortQr.Infrastructure/Persistence/**`; `04` §5/§7/§9, `06`, `23` §7, `12`, `16`, `24`; DEC-047/048/049/053, DEV-020/022.
- Reversible?: có (config/DbContext; chưa migration/DB thật).

### DEC-055: Host on-DB + migration InitialCreate + seeder idempotent (hoàn thiện "app chạy được")
- Status: Accepted (2026-07-04) — build 0W/0E; **130 test xanh** (76 unit + 5 arch + **49 integration** + 1 skip; +4 seeder). Migration Npgsql sinh + compile được (offline, không Docker).
- **Composition DB** (`ResortQrDatabaseExtensions`): `AddResortQrDatabase(connString)` = `AddDbContext<AppDbContext>(UseNpgsql + UseSnakeCaseNamingConvention)` + `AddResortQrPersistence<AppDbContext>()`. `MigrateAndSeedResortQrAsync(seedOptions)` = Migrate (Npgsql) + seed idempotent (mở kết nối THẬT → chỉ gọi khi có DB).
- **Program.cs**: `AddResortQr` + `AddResortQrDatabase(connString)`; connString **fail-fast ở Production** nếu rỗng (`15` §3), dev/test dùng placeholder (UseNpgsql KHÔNG mở kết nối lúc construct → validation qua). Migrate+seed **OPT-IN** qua `Database:MigrateOnStartup` (mặc định false) → **KHÔNG chạy trong integration test in-memory / môi trường chưa có Postgres**; deployment single-instance bật cờ.
- **Seeder** (`ResortSeeder`, `04` §6): idempotent (kiểm tồn tại trước khi thêm) — 1 Resort + ResortSettings (giá trị hợp lệ thoả CHECK) + ResortLanguage (en mặc định, vi/ko/zh) + admin (băm Argon2id, mật khẩu từ `Seed:AdminPassword` env — TK-004). Guard: có AdminEmail thì BẮT BUỘC AdminPassword (chống admin không mật khẩu); thiếu email → bỏ qua admin (dev).
- **Migration InitialCreate** (dotnet-ef 10.0.9 **local tool**, pin): dùng `AppDbContextFactory` (design-time Npgsql). Verify DDL offline: `xmin`→`xid` rowVersion (Room/AppUser/ResortSettings), CHECK `ck_portal_window`, partial index literal Npgsql ĐÚNG (`is_default = true`, `is_deleted = false`, `status = 'Active'`) → xác nhận helper `ProviderPartialIndex` chạy đúng cho design-time. Chứng minh mapping dịch sang Npgsql DDL đúng mà KHÔNG cần Docker.
- **Fix analyzer (fix gốc):** EF-generated migration dính CA1861 (mảng inline) dưới TreatWarningsAsErrors → `.editorconfig` `[**/Migrations/*.cs] generated_code = true` (migration là code SINH + review DDL, không giữ chuẩn viết tay). KHÔNG sửa file generated.
- **Test SQLite (Docker-free):** seeder tạo đúng baseline + idempotent (chạy 2 lần không nhân đôi) + admin hashed + guard no-password throws. (Migration Npgsql dùng EnsureCreated ở SQLite test — migration thật chỉ áp Postgres.)
- Refs: `resort-qr/src/ResortQr.Infrastructure/Persistence/{ResortQrDatabaseExtensions, Seeding/*}.cs`, `Migrations/*_InitialCreate.cs`, `ResortQr.Api/{Program.cs, appsettings.json}`, `dotnet-tools.json`; `04` §6, `15` §1/§3, `23` §7; DEC-047/048/053/054, TK-004.
- Reversible?: có (migration remove được; cờ opt-in).

### DEC-056: Vertical slice ResolveToken (xương sống guest — `13`) — use case + endpoint + test SQLite
- Status: Accepted (2026-07-04) — build 0W/0E; **141 test xanh** (76 unit + 5 arch + **60 integration** + 1 skip; +9 use-case +2 HTTP). Architecture test xanh → `ResolveTokenUseCase` (Application) KHÔNG chạm EF (chỉ port `IUnitOfWork`/`IRepository`). Doc: `resort-qr/docs/resolve-token-slice-design.md`.
- **`ResolveTokenUseCase`** (`IUseCase<ResolveTokenInput,ResolveTokenResult>`, HTTP-free): token→phòng→resort validate (không lộ phòng khác); GuestSession resolve/tạo (session key raw qua INPUT, trả `IssuedSessionKey` để Api set cookie — không đụng HttpContext); GuestVisit **lazy idle-expiry** (không phụ thuộc sweeper — `13` §2), **nối lại** Active còn hạn (sliding window), **tạo mới race DB-arbitrated** (bắt `UniqueConstraintViolationException`→re-query). Truy vấn chỉ qua `IRepository.FirstOrDefaultAsync`/`FindByIdAsync` (ux_visit_active đảm bảo ≤1 Active → không cần ORDER BY/EF operators trong Application).
- **Cơ chế nền thêm (fix gốc, tái dùng):** `UniqueConstraintViolationException` (SharedKernel, trung lập) — `EfUnitOfWork` bắt `DbUpdateException` unique-violation (Npgsql SqlState 23505; SQLite match type-name+message "UNIQUE" vì Infra không ref Sqlite) → ném; Api map 409 (nếu escape). `IGuestSessionKeyHasher` + `Sha256GuestSessionKeyHasher` (hash session key, tách khỏi refresh hasher cho rõ ngữ nghĩa). `GuestAccessErrors` (qr_invalid/qr_revoked/room_inactive/session_expired — mã ∈ catalog `14`).
- **Endpoint mỏng** `GET /api/guest/resolve/{token}` (AllowAnonymous): đọc cookie guest → use case → set cookie HttpOnly/Secure/**SameSite=Lax** (QR mở tab mới) nếu có session mới → 200 `ResolveResponse` (KHÔNG lộ session key) hoặc ProblemDetails. `GuestOptions` (CookieName/SessionCookieDays, validate-on-start).
- **Hoãn (có lý do):** `rule_ack_required`+ack (cần RulePublication wave Rules); EnforcePortalWindow + cascade EndVisit (cần entity Messaging/Housekeeping); danh sách ngôn ngữ đầy đủ (cần ToListAsync=EF → query service sau). Slice trả features + default language (lookup 1-entity).
- **Test SQLite (Docker-free):** 9 use-case theo ma trận `13` §7 (E1 qr_invalid, E2 qr_revoked, E3 inactive/maintenance/soft-deleted, E4 thiết bị mới, E5 nối lại+sliding, E6 lazy-expire+visit mới, E11 2 phòng); 2 HTTP end-to-end (200+Set-Cookie; 404 problem+json qr_invalid) qua `GuestApiFactory` (swap Npgsql→SQLite). Race đa-connection (E7a) đúng-bởi-xây-dựng → Testcontainers (TK-036).
- Refs: `resort-qr/docs/resolve-token-slice-design.md`, `ResortQr.Application/GuestAccess/**`, `ResortQr.Infrastructure/Security/Sha256GuestSessionKeyHasher.cs`, `ResortQr.Api/Endpoints/GuestAccessEndpoints.cs`; `13` §3/§7, `14` §2.1, `04` §7.1; DEC-047/048/054/055.
- Reversible?: có.

### TK-039: Swap EF provider (Npgsql→SQLite) trong WebApplicationFactory — phải gỡ TRIỆT ĐỂ
- Khi test HTTP trên SQLite mà Program đã `AddDbContext(UseNpgsql)`: chỉ `RemoveAll<DbContextOptions<T>>` KHÔNG đủ — EF (9+) đăng ký `IDbContextOptionsConfiguration<T>` TÍCH LUỸ config, nên UseNpgsql còn lại → options có 2 provider → `InvalidOperationException` "Only a single database provider...".
- Fix: gỡ MỌI descriptor có FullName chứa "DbContextOptions"/"Npgsql" + `typeof(AppDbContext)` rồi mới `AddDbContext(UseSqlite)`. Xem `GuestApiFactory`.

### DEC-057: Vertical slice Admin Rooms & QR (create+issue, rotate atomic) — hoàn thiện vòng admin→QR→resolve
- Status: Accepted (2026-07-04) — build 0W/0E; **155 test xanh** (76 unit + 5 arch + **74 integration** + 1 skip; +7 use-case +5 validator +2 authz). Doc: `resort-qr/docs/rooms-qr-slice-design.md`.
- **`CreateRoomUseCase`** (`16` §3): tạo Room(Active) + issue RoomQrToken(Active,V1) NGUYÊN TỬ (một SaveChanges). Trùng số phòng đang sống (ux_room_number) → `UniqueConstraintViolationException` → validation_error (Req 16.6). Input qua `CreateRoomValidator` (FluentValidation decorator: RoomNumber 1–20, Building ≤50, Floor −10..200).
- **`RotateRoomTokenUseCase`** (`16` §4, Property B2): revoke token Active cũ (giữ lịch sử, Status=Revoked) + issue mới (Version+1) trong MỘT SaveChanges → all-or-nothing (không bao giờ "0 token" hay "2 Active"). Race 2 admin rotate → `ux_qr_active` phân xử → unique-violation → qr_generation_failed. Phòng không tồn tại→not_found; Inactive→qr_generation_failed (Req 16.7).
- **`RoomTokenFactory.GenerateUniqueTokenAsync`** (`16` §5): app-retry 1..5 (best-effort) + `ux_qrtoken_token` chốt chặn thật; cạn→null→qr_generation_failed. `Preview` = 6 ký tự đầu + "…" (Req 11.6).
- **Nguyên tử bằng MỘT SaveChanges** (không ExecuteInTransaction): revoke+insert / room+token cùng transaction EF là đủ all-or-nothing; đơn giản hơn, không cần multi-save. (Ghi DEV-023.)
- **Endpoint admin** (RequireAdmin): `POST /api/admin/rooms` (201 + token), `POST /api/admin/rooms/{id}/revoke-token` (200). (GET list/PUT/DELETE + qr.png/qr-labels.pdf HOÃN — render output, QuestPDF vướng license TK-019 → increment riêng.)
- **Nguồn ResortId:** query resort DUY NHẤT (`FirstOrDefaultAsync(_=>true)`) — single-resort/deployment (`24`). TK-040: multi-tenant → lấy từ `ICurrentUser.ResortId` (claim).
- Test SQLite: create issue 1 Active V1; trùng số→validation_error; tái tạo sau soft-delete OK; 2 phòng token khác nhau; rotate revoke cũ+Active mới V2 (đúng 1 Active); rotate không tồn tại→not_found; rotate Inactive→qr_generation_failed; validator (rỗng/>20/floor ngoài range); authz 401 (RequireAdmin). Race 2 rotate thật → Testcontainers (TK-036).
- Refs: `resort-qr/docs/rooms-qr-slice-design.md`, `ResortQr.Application/Rooms/**`, `ResortQr.Api/Endpoints/AdminRoomEndpoints.cs`; `16` §2/§3/§4/§5/§8, `04` §7.1, `14`, Property B2/B3; DEC-053/054/056.
- Reversible?: có.

### TK-040: Nguồn ResortId ở use case nghiệp vụ — hiện query single-resort; multi-tenant cần ICurrentUser.ResortId
- Hiện tại (instance-per-resort — `24` §4): use case (CreateRoom...) lấy resort bằng `repo<Resort>.FirstOrDefault(_=>true)` vì mỗi deployment đúng 1 Resort.
- Khi chuyển shared-DB multi-tenant (`24` §6 checklist): resortId PHẢI lấy từ **`ICurrentUser.ResortId`** (claim JWT `resortId` — `12` §2). Cần: (a) thêm `ResortId` vào `ICurrentUser` + `HttpContextCurrentUser` (đọc claim); (b) thay các `FirstOrDefault(_=>true)` bằng scope theo currentUser.ResortId + global query filter theo ResortId. Chưa làm (YAGNI + tránh phức tạp rò chéo — `24` §4).

### DEC-058: Render QR PNG (QRCoder) — khép kín "admin sinh QR → dán → khách quét"
- Status: Accepted (2026-07-04) — build 0W/0E; **163 test xanh** (76 unit + 5 arch + **82 integration** + 1 skip; +8 QR). Doc: `resort-qr/docs/qr-render-slice-design.md`.
- **Thư viện QRCoder 1.8.0** (verify version thật + API `PngByteQRCode`/`QRCodeGenerator`/`ECCLevel` qua reflection/byte-scan; không lỗ hổng). Dùng **`PngByteQRCode`** — PNG THUẦN MANAGED, KHÔNG System.Drawing/SkiaSharp/native → chạy đa nền tảng (đúng lý do .NET Core). ECC level Q (~25% phục hồi, QR dán bền); 20 px/module.
- **Port `IQrService`** (`02` §5, Application) + **`QrCoderQrService`** (Infrastructure, namespace `Qr` → auto-scan). Sync (CPU-bound ≤5s — Req 16.2).
- **`RenderRoomQrPngUseCase`** (`16` §6): room Active (else not_found/qr_generation_failed Req16.7); `GuestWebBaseUrl` phải **HTTPS tuyệt đối hợp lệ** (Req 15.6, secure context camera) else invalid_configuration; URL `{baseUrl}/r/{token}` từ token Active (KHÔNG hardcode host Req14.3); QR chỉ chứa URL (Req 1.6, không nhúng số phòng).
- **Endpoint** `GET /api/admin/rooms/{id}/qr.png` (RequireAdmin): success → `image/png` + `Cache-Control: no-store` (token nhạy cảm không cache proxy); lỗi → ProblemDetails.
- Test SQLite: PNG hợp lệ (chữ ký ‰PNG) cho room Active + https; invalid_configuration (null/rỗng/http/not-a-url — 4 case); inactive→qr_generation_failed; not_found; Active nhưng không token Active→qr_generation_failed.
- **Hoãn**: PDF nhãn hàng loạt (QuestPDF — license doanh thu TK-019) → increment riêng.
- Refs: `resort-qr/docs/qr-render-slice-design.md`, `ResortQr.Application/{Abstractions/IQrService.cs, Rooms/RenderRoomQrPngUseCase.cs}`, `ResortQr.Infrastructure/Qr/QrCoderQrService.cs`, `AdminRoomEndpoints.cs`; `16` §6, `02` §5, `14` §2.2; DEC-057.
- Reversible?: có.

### DEC-059: Hoàn thiện Room CRUD (read query service + update/status/soft-delete) + phân quyền per-endpoint
- Status: Accepted (2026-07-04) — build 0W/0E; **173 test xanh** (76 unit + 5 arch + **92 integration** + 1 skip). Doc: `resort-qr/docs/rooms-crud-slice-design.md`.
- **Read query service (CQRS-lite `02` §6.1):** `IRoomQueries` (Application) + `EfRoomQueries` (Infrastructure) — EF projection `AsNoTracking` → DTO `RoomListItem` (KHÔNG xuyên IQueryable ra ngoài). Token preview qua **correlated subquery** (Set<RoomQrToken> Active). Query filter tự loại phòng soft-deleted. `EfRoomQueries` coupled DbContext → namespace Persistence (loại auto-scan) + wire tường minh `AddResortQrPersistence` (nhất quán DEC-049 với AppUserAuthStore).
- **`JsonStringEnumConverter`** (global, `ConfigureHttpJsonOptions`): enum→string trong JSON (hợp đồng API rõ ràng, ổn định — chuẩn commercial). Cho phép projection `r.Status` (enum) trực tiếp, tránh dịch `enum.ToString()` trong SQL.
- **Mutations:** `UpdateRoomUseCase` (+`UpdateRoomValidator`; trùng số→validation_error), `ChangeRoomStatusUseCase`, `DeleteRoomUseCase` (Remove→convention xóa MỀM, giữ token/visit/lịch sử — `04` §7.3). Đều `ICommandUseCase<>` (Result no-value), auto validation-decorated.
- **Endpoint phân quyền per-endpoint** (`16` §8): GET list/detail = **RequireStaff** (Staff|Admin — xem vận hành); POST/PUT/DELETE/qr.png/rotate = **RequireAdmin**. Bỏ auth mức group.
- **Verify end-to-end admin JWT:** test issue token (IJwtTokenService, role Admin) → Bearer → RequireAdmin (claim `role`=Admin khớp RoleClaimType="role") → CreateRoom → SQLite → 201. Xác nhận toàn stack auth+policy+use case+DB (không mock). GET list/create/rotate không token → 401.
- Test SQLite: list paged (Total, order, preview, loại soft-deleted); GetById (item/null); update (đổi field; trùng số→validation_error; unknown→not_found); change-status; delete (soft, token còn); + HTTP 401 + 201 admin.
- Refs: `resort-qr/docs/rooms-crud-slice-design.md`, `ResortQr.Application/Rooms/**`, `ResortQr.Infrastructure/Persistence/EfRoomQueries.cs`, `AdminRoomEndpoints.cs`, `ResortQrApiExtensions.cs`; `16` §2/§8, `02` §6.1, `04` §7.1/§7.3, `14`; DEC-049/053/057/058.
- Reversible?: có.

### DEC-060: Wave Rules — sub-slice A: domain + EF mapping + migration Rules_Init + test ràng buộc
- Status: Accepted (2026-07-04) — build 0W/0E; **179 test xanh** (76 unit + 5 arch + **98 integration** + 1 skip; +6 Rules schema). Migration Rules_Init sinh + no-pending-model-changes. Doc: `resort-qr/docs/rules-wave-design.md`.
- **Mô hình (draft mutable + snapshot bất biến + ack)** — 7 entity: `RuleSet`→`RuleSection`→`RuleSectionTranslation` (draft, `AuditableEntity` vì có concurrency `04` §8; Translation impl `ITranslation`); `RulePublication`→`RulePublicationSection`→`RulePublicationSectionTranslation` (snapshot bất biến, `Entity` append-only; Translation impl `ITranslation`); `RuleAcknowledgement` (`Entity` append-only). Fields theo master design §data-model.
- **Index (`04` §5):** `ux_pub_current` (partial `rule_publication(resort_id) WHERE is_current` — BOOL provider-aware qua `ProviderPartialIndex.BoolEquals` ở OnModelCreating, như ux_lang_default); `ux_ack` (guest_visit_id, rule_publication_id); `ux_tr_rule` + `ux_tr_rule_pub` (section+lang); `ux_rule_set_resort` (1 draft/resort); `ux_pub_version` (resort+version). **Cascade** cho aggregate sở hữu (set→section→translation, publication→section→translation); **Restrict** cho tham chiếu chéo lịch sử (→Resort/Room/GuestSession/GuestVisit/RulePublication).
- **Migration `Rules_Init` PER-WAVE (Req 8.7):** additive riêng (không dồn vào InitialCreate), verify DDL Npgsql offline (`is_current = true`, cascade/restrict đúng) + `has-pending-model-changes` = No changes.
- **`ITranslation.HasContent`** = Title (trim) khác rỗng ("có row nhưng rỗng" ⇒ thiếu → fallback, `18` §3) — dùng ở guest-read slice (D).
- Test SQLite: ux_pub_current (2 current/resort→chặn; 1 current+non-current→OK), ux_tr_rule (dup lang→chặn), cascade (xóa section→dịch biến mất), ux_ack (dup visit+pub→chặn), FK restrict (publication resort lạ→chặn).
- **Lộ trình wave còn lại (mỗi cái 1 increment):** B authoring draft (get/upsert + sanitize HTML + MissingLanguages `18` §5); C `PublishRulesUseCase` (snapshot version++ IsCurrent nguyên tử, `05` §2); D guest read (`GET /rules?lang` resolve fallback) + ack (`POST /rules/acknowledge` ux_ack idempotent) + ResolveResponse rules-state; E ack-gate (`EnforcePortalWindow` + `rule_ack_required`).
- Refs: `resort-qr/docs/rules-wave-design.md`, `ResortQr.Domain/Rules/**`, `Configurations/Rule*Configurations.cs`, `Migrations/*_Rules_Init.cs`; master design §data-model/§api-rules, `04` §2/§4/§5/§8, `18`; DEC-053/054/057/046.
- Reversible?: có (migration remove; per-wave nên gỡ độc lập).

### DEC-061: Wave Rules — sub-slice B: authoring draft (GET/PUT /api/admin/rules/draft)
- Status: Accepted (2026-07-05) — build 0W/0E; **185 test xanh** (76 unit + 5 arch + **104 integration** + 1 skip; +6 RuleDraft). Doc: `resort-qr/docs/rules-wave-design.md` §5-B.
- **Ngữ nghĩa PUT = FULL-REPLACE (AI tự chốt — spec chỉ nói "get/update draft"):** xóa toàn bộ `RuleSection` hiện có của RuleSet (DB cascade `ux_tr_rule` xóa translation) rồi chèn lại section+translation mới, TẤT CẢ trong MỘT `SaveChanges` (nguyên tử). Lý do bản chất: draft là MỘT tài liệu do admin biên tập tổng thể (Id section nội bộ, không phải khóa ổn định giữa các lần lưu — `Key` mới là khóa ổn định để đối chiếu khi publish). Full-replace đơn giản + đúng (không cần diff phức tạp add/update/delete từng section), và an toàn vì không có unique trên `RuleSection.Key` nên delete+insert cùng Key trong 1 transaction không xung đột. Verified test `Update_full_replaces_existing_sections_and_translations` + cascade translation.
- **Get-or-create `RuleSet`:** resort chưa có draft → tạo RuleSet mới (1 draft/resort — `ux_rule_set_resort`). GET trả DTO rỗng (`Sections=[]`, HTTP 200) khi chưa có draft (không 404) để editor mở form mới.
- **Sanitize HTML ở TẦNG GHI (use case), không ở validator/endpoint:** `BodyHtmlSanitized = IHtmlSanitizer.Sanitize(rawBodyHtml)` trước khi lưu → DB không bao giờ chứa HTML thô (chống XSS, defense tại nguồn ghi — DEC-018). Verified test script bị strip.
- **Phân quyền Staff|Admin** (`RequireStaffPolicy`) theo master design §admin rules ("Staff, Admin") — nội quy là nội dung vận hành, Staff soạn được (khác Rooms mutation = Admin-only).
- **Read-model `IRuleDraftQueries`/`EfRuleDraftQueries`:** trả `RuleDraftDto` kèm `MissingLanguages` per-section (tính bằng `ITranslationResolver.MissingLanguages` — thuật toán thuần in-memory, `18` §5) so với `ResortLanguage.IsEnabled`. Coupled DbContext → namespace `Infrastructure.Persistence` + wire tường minh `TryAddScoped` trong `AddResortQrPersistence` (DEC-049).
- **`IRepository.ListAsync(predicate)` — mở rộng contract base (xem DEV-024):** để use case nạp tập section tracked cho full-replace mà không lộ IQueryable/EF ra Application.
- Refs: `resort-qr/src/ResortQr.Application/Rules/{RuleDraftContracts,IRuleDraftQueries,UpdateRuleDraftUseCase,UpdateRuleDraftValidator}.cs`, `Infrastructure/Persistence/EfRuleDraftQueries.cs`, `Api/Endpoints/AdminRuleEndpoints.cs`; TRD-010, TK-041, DEV-024.
- Reversible?: có (full-replace có thể đổi sang diff-merge nếu cần lịch sử section-level; concurrency có thể bật lại — TRD-010).

### DEC-062: Wave Rules — sub-slice C: publish snapshot (POST /api/admin/rules/publish)
- Status: Accepted (2026-07-05) — build 0W/0E; **191 test xanh** (76 unit + 5 arch + **110 integration** + 1 skip; +6 RulePublish). Doc: `resort-qr/docs/rules-wave-design.md` §5-C.
- **Mô hình:** đóng băng draft (`RuleSet`→sections→translations) thành SNAPSHOT bất biến `RulePublication`(Version++)→`RulePublicationSection`→`RulePublicationSectionTranslation`; `IsCurrent=true` bản mới + hạ cờ bản cũ. Khách đọc từ publication IsCurrent (slice D). Bám pseudocode `02` §3 `PublishAsync` — NHƯNG ghi bằng 2 SaveChanges thay vì 1 (xem DEV-025).
- **Phân quyền = `RequireAdmin` (AI tự chốt — spec không nói rõ ai publish):** KHÁC draft (Staff|Admin). Lý do bản chất: publish là hành động "go-live" hệ trọng đổi nội dung khách thấy TOÀN CỤC + là hành động nhạy cảm cần audit (`19` §log-events, `10` §5 liệt kê "publish nội quy" là sensitive). Tách nhiệm vụ editorial: Staff soạn draft → Admin duyệt & publish (separation-of-duties, đồng nhất Rooms mutation=Admin). Reversible: đổi sang `RequireStaff` nếu user muốn Staff publish.
- **Thứ tự ghi nguyên tử (bản chất — DEV-025):** trong `ExecuteInTransactionAsync`: (1) hạ cờ current + **SaveChanges TRƯỚC** (flush giải phóng `ux_pub_current`); (2) insert snapshot + SaveChanges. Không gộp 1 SaveChanges vì EF không đảm bảo UPDATE-trước-INSERT trên cùng bảng → tránh trạng thái 2-current thoáng qua. Cả 2 save trong 1 transaction ⇒ all-or-nothing.
- **Guard ≥1 section (AI tự chốt):** publish khi chưa có `RuleSet` HOẶC 0 section → `validation_error` (`RulesErrors.NoDraftToPublish`). Publish rỗng gần như luôn là nhầm; muốn tắt rule-gate thì đổi ResortSettings, không publish rỗng. Reversible.
- **Race 2 admin publish:** DB phân xử — `ux_pub_version`(resort,version) + `ux_pub_current`(resort) WHERE is_current; lần commit sau ném unique-violation → bắt `UniqueConstraintViolationException` → `conflict` (`RulesErrors.PublishConflict`). Race thật đa-connection cần Testcontainers (TK-036); SQLite 1-connection kiểm luồng tuần tự.
- **Error code tái dùng catalog** (`validation_error`/`conflict`) — không thêm code mới (DEC-019). Copy translation nguyên văn (draft đã sanitize ở B — KHÔNG sanitize lại).
- Refs: `resort-qr/src/ResortQr.Application/Rules/{PublishRulesContracts,PublishRulesUseCase,PublishRulesValidator,RulesErrors}.cs`, `Api/Endpoints/AdminRuleEndpoints.cs`; `02` §3 PublishAsync, Req 5.4 (ux_pub_current), `19`/`10`; DEV-025, DEC-061/019/049.
- Reversible?: có (đổi authz; đổi guard; snapshot có thể xóa — bản thân publication bất biến append-only).

### DEC-063: Wave Rules — sub-slice D: guest read + acknowledge (GET/POST /api/guest/rules[/acknowledge])
- Status: Accepted (2026-07-05) — build 0W/0E; **200 test xanh** (76 unit + 5 arch + **119 integration** + 1 skip; +9 GuestRules). Doc: `resort-qr/docs/rules-wave-design.md` §5-D.
- **Bối cảnh QUAN TRỌNG:** tầng Application của D (GetGuestRulesUseCase, AcknowledgeRulesUseCase, GuestRulesContracts/Validators, IPortalWindowGuard/PortalWindowGuard, RuleAckStatusProvider, ResolveTokenResult+rules-state) đã do phiên TRƯỚC tạo nhưng **để lại trạng thái DỞ DANG/lỗi biên dịch**. Phiên này KIỂM CHỨNG bằng build (fail), fix tận gốc + hoàn thiện endpoint + viết test (xem TK-042, DEV-026).
- **Guest read `GET /api/guest/rules?visitId=&lang=`** (AllowAnonymous): đọc publication `IsCurrent` → resolve từng section theo ngôn ngữ (`ITranslationResolver.Resolve`, fallback default `18` §3, `IsFallback`/`LanguageCode` per-section). Chưa publish → `CurrentVersion=null, Sections=[]`. visitId qua QUERY, session key RAW qua cookie HttpOnly (use case tự hash — Api không xử lý hash).
- **Guest ack `POST /api/guest/rules/acknowledge`** (AllowAnonymous): ghi `RuleAcknowledgement` cho publication current — **server TỰ xác định publication+version** (KHÔNG tin client, chống giả version). **IDEMPOTENT**: check-then-act (đã ack → AlreadyAcknowledged=true) + bắt `UniqueConstraintViolationException` (ux_ack) khi 2 ack đua → cùng trả AlreadyAcknowledged=true. Chưa publish → `validation_error` (NoPublishedRules).
- **Bảo vệ bằng `IPortalWindowGuard`** (DEV-026): mọi endpoint guest interactive kiểm visit thuộc session (cookie) + Active + còn hạn (lazy idle-expiry + portal window). Thất bại → `session_expired` (không lộ nguyên nhân). **Sliding-window refresh cập-nhật-SAU** khi read/ack THÀNH CÔNG (DEC-030, B8) — ack refresh cùng lần ghi (nguyên tử với insert ack).
- **ResolveResponse rules-state:** `ResolveTokenUseCase` gọi PORT `IRuleAckStatusProvider` (không đọc thẳng entity Rules — ranh giới module master §2.3) → `RulesVersion`/`RulesAcknowledged`. Endpoint `ResolveAsync` nay truyền đủ 2 field (trước bị thiếu → lỗi, đã fix).
- Refs: `resort-qr/src/ResortQr.Application/Rules/{GetGuestRulesUseCase,AcknowledgeRulesUseCase,GuestRulesContracts,GuestRulesValidators,RuleAckStatusProvider}.cs`, `GuestAccess/{PortalWindowGuard,IPortalWindowGuard}.cs`, `Api/Endpoints/GuestAccessEndpoints.cs`; `13` §4, `18` §3, Req 9; DEC-030/062/046/049, DEV-026, TK-042.
- Reversible?: có (đổi visitId sang route/body; đổi nơi refresh window).

### DEC-064: Wave Rules — sub-slice E: ack-gate tái dùng (IRuleGate) — ĐÓNG WAVE RULES
- Status: Accepted (2026-07-05) — build 0W/0E; **205 test xanh** (76 unit + 5 arch + **124 integration** + 1 skip; +5 RuleGate). Doc: `resort-qr/docs/rules-wave-design.md` §5-E.
- **Cơ chế tái dùng (KHÔNG xây trên không):** FAQ/chat/housekeeping endpoints thuộc wave SAU chưa tồn tại → E chỉ xây **cổng `IRuleGate.CheckAsync(resortId, visitId, GuestFeature)`** + test. Wave sau chỉ việc gọi cổng SAU `IPortalWindowGuard` (một nguồn luật, không nhân bản). `GuestFeature` enum {Faq, Chat, Housekeeping} map tới cờ `ResortSettings.RequireRuleAckFor*`.
- **Luật cổng:** (1) settings không yêu cầu ack tính năng → MỞ; (2) yêu cầu ack + đã publish + lượt chưa ack publication IsCurrent → CHẶN `rule_ack_required` (Forbidden 403 — code ∈ catalog `14`, DEC-019); (3) đã ack → MỞ.
- **Fail-open khi CHƯA publish (AI tự chốt — quyết định bản chất):** yêu cầu ack NHƯNG resort chưa publish nội quy nào → cổng MỞ. Lý do: không có gì để ack; nếu chặn thì admin bật cờ mà quên publish sẽ **khóa khách khỏi mọi tính năng vĩnh viễn** (như sự cố ngừng dịch vụ) — tệ hơn nhiều so với việc tạm cho qua. Trách nhiệm publish thuộc admin. Reversible (đổi sang fail-closed nếu nghiệp vụ yêu cầu chặt).
- **Ranh giới trách nhiệm:** cổng CHỈ kiểm ack (không kiểm portal window — đó là việc của `IPortalWindowGuard`, DEV-026). Endpoint tương lai gọi lần lượt: portal-window-guard → rule-gate → xử lý. Phụ thuộc thuần IUnitOfWork → auto-scan DI an toàn.
- Refs: `resort-qr/src/ResortQr.Application/Rules/{IRuleGate,RuleGate,RulesErrors}.cs`; `13`/`14` (rule_ack_required), Req 8; DEC-030/063, DEV-026.
- Reversible?: có (fail-open→fail-closed; cổng có thể gộp thêm luật khác).

> **Wave Rules HOÀN TẤT (A→E):** schema → soạn draft → publish snapshot → khách đọc+ack → ack-gate. Tổng thể: draft mutable (Staff|Admin soạn) → publish bất biến (Admin, Version++, đúng 1 IsCurrent) → khách đọc publication current (resolve fallback) + ack idempotent (ux_ack) → cổng ack chặn tính năng khi cấu hình yêu cầu.

### DEC-065: Guest-web PROTOTYPE dạng 1 file HTML tự chứa (ưu tiên "lên hình" theo yêu cầu user)
- Status: Accepted (2026-07-05) — user yêu cầu "lên hình giao diện trước là làm".
- Bối cảnh: tới giờ chỉ có backend; user muốn THẤY giao diện. Thiết kế frontend (`design/frontend`) chốt Vue 3 + Vite + TS + pnpm monorepo (DEC-012/029) — nhưng dựng stack đó cần Node/npm install (tốn disk/mạng; máy user hạn chế tài nguyên — TK-034/043).
- **Quyết định:** dựng `resort-qr/guest-web/index.html` — **1 file HTML+CSS+JS tự chứa, KHÔNG build step**, mở trực tiếp bằng trình duyệt là thấy ngay. Mobile-first, 3 tab: Nội quy (đọc+đồng ý), FAQ, Dọn phòng (tạo ticket). i18n vi/en cho nhãn UI.
- **Lý do (bản chất):** mục tiêu tức thời của user = thấy giao diện/luồng NHANH, rủi ro thấp, chạy được ngay trên máy hạn chế. Prototype không-build đạt điều đó; Vue SPA chính thức để sau khi cần tương tác/scale thật.
- **Nối API thật:** đặt `API_BASE` + `?token=` → gọi `GET /api/guest/resolve/{token}`, `GET /api/guest/rules`, `POST /api/guest/rules/acknowledge` (wave Rules đã xong). Không đặt → dữ liệu mẫu. FAQ + Dọn phòng hiện là **dữ liệu mẫu** (backend 2 phần này chưa làm — wave kế).
- **Ranh giới:** đây là PROTOTYPE duyệt UI, KHÔNG thay Vue SPA trong thiết kế; render `bodyHtmlSanitized` (đã sanitize server-side B). Không đưa prototype vào luồng build/test backend.
- Refs: `resort-qr/web/guest/index.html` (đã chuyển từ `guest-web/` vào web root chung); DEC-012/029 (Vue SPA chính thức), DEC-063 (guest rules API), TK-043/044.
- Reversible?: có (prototype độc lập; thay bằng Vue SPA khi dựng frontend thật).

### DEC-066: Admin-web QR (test) client-side + phục vụ LAN bằng python (nginx chưa cài)
- Status: Accepted (2026-07-05) — user yêu cầu "nginx pub ra ngoài vào bằng đt + web admin lấy QR test".
- **Web root chung `resort-qr/web/`** (`index.html` landing + `guest/` + `admin/` + `nginx.conf`). Phục vụ ra LAN để điện thoại cùng WiFi truy cập.
- **nginx CHƯA cài trên máy** (verify `Get-Command nginx` rỗng) → dùng **`python -m http.server 8080 --bind 0.0.0.0`** (python có sẵn qua scoop) để chạy NGAY không cần cài. Vẫn để `web/nginx.conf` sẵn cho khi user muốn nginx (`scoop install nginx`). Lý do: mục tiêu tức thời = vào được từ đt nhanh; python zero-install đạt ngay, nginx là tùy chọn tương đương.
- **Admin QR = client-side** (`qrcode-generator` qua CDN) sinh QR trỏ `http://<LAN-IP>/guest/?token=`. KHÔNG phải QR thật của backend (backend đã có `GET /api/admin/rooms/{id}/qr.png` QRCoder — DEC-058) mà chỉ là **công cụ test giao diện** khi backend chưa chạy. Đánh dấu rõ là test tool.
- **LAN IP máy: 192.168.120.102** (card Ethernet; 172.x là vEthernet ảo Hyper-V/WSL — bỏ). Firewall mở cổng 8080 cần admin (New-NetFirewallRule bị "Access denied" khi không elevated) → user tự chạy elevated hoặc bấm Allow ở prompt Windows.
- Refs: `resort-qr/web/**`, DEC-058 (QR backend thật), DEC-065 (guest prototype), TK-044.
- Reversible?: có (đổi sang nginx; thay admin QR client-side bằng gọi endpoint qr.png thật khi backend chạy).

### DEC-067: Wave FAQ — sub-slice A HOÀN TẤT + FIX model-drift (migration Faq_Init bị THIẾU)
- Status: Accepted (2026-07-05) — build 0W/0E; **215 test xanh** (76 unit + 5 arch + **134 integration** + 1 skip; +4 FaqSchema). Doc: `resort-qr/docs/faq-wave-design.md`.
- **Nợ kỹ thuật phát hiện (audit BE "cực kỹ"):** FAQ domain (4 entity) + EF config (`FaqConfigurations.cs`) + DbSet đã có (phiên trước), NHƯNG **KHÔNG có migration** cho bảng faq_* → **model TRÔI khỏi snapshot**. Kiểm chứng bằng chứng: `dotnet ef migrations has-pending-model-changes` = "Changes have been made". Test SQLite dùng `EnsureCreated` (dựng schema từ MODEL, không từ migration) nên vẫn xanh → **che mất drift**; nhưng deploy Postgres (chạy migration) sẽ THIẾU bảng FAQ. Đây là fix tận gốc, không phải ngọn.
- **Fix:** sinh `dotnet ef migrations add Faq_Init` (per-wave additive — DEC-010/DEV-006) → 4 bảng faq_category/faq_category_translation/faq_item/faq_item_translation, unique ux_tr_faq_cat/ux_tr_faq, FK category→resort Restrict + translations/item→parent Cascade, xmin concurrency. Verify lại `has-pending-model-changes` = "No changes".
- **FAQ-A test SQLite (4):** dup category-translation lang → chặn (ux_tr_faq_cat); dup item-translation lang → chặn (ux_tr_faq); xóa category → cascade item + cả 2 loại translation; category resort lạ → FK Restrict.
- **Khác Rules:** FAQ là CMS đơn giản (sửa trực tiếp, hiển thị IsActive) — KHÔNG draft/publish/version.
- Refs: `Migrations/*_Faq_Init.cs`, `Configurations/FaqConfigurations.cs`, `tests/.../Faq/FaqSchemaTests.cs`; `04` §5/§7; DEC-060 (Rules-A pattern), TK-045.
- Reversible?: có (migration remove per-wave).

### DEC-068: Wave FAQ — sub-slice C: guest đọc FAQ (GET /api/guest/faq)
- Status: Accepted (2026-07-05) — trong 215 test xanh (+6 GuestFaqRead integration). Doc: `faq-wave-design.md` §3.
- **`GET /api/guest/faq?visitId=&lang=`** (AllowAnonymous): đọc cây category/item `IsActive` (OrderBy SortOrder) resolve theo ngôn ngữ (`ITranslationResolver.Resolve` fallback default, `IsFallback`/`LanguageCode` per-item). visitId qua query, session key RAW qua cookie.
- **Tái dùng luật cổng guest (nhất quán DEC-063):** `GetGuestFaqUseCase` mirror `GetGuestRulesUseCase` — `IPortalWindowGuard` (DEV-026) → nếu `!FaqEnabled` trả RỖNG (tính năng tắt, FE ẩn tab — không lỗi) → `IRuleGate.CheckAsync(Faq)` (DEC-064; chưa ack + settings yêu cầu → `rule_ack_required`) → nạp cây + resolve → refresh sliding-window (B8). Phụ thuộc thuần port → auto-scan DI an toàn.
- **Quyết định:** FaqEnabled=false → trả rỗng (không phải lỗi) vì bật/tắt là trạng thái hiển thị, FE dùng `ResolveResponse.FaqEnabled` ẩn tab; đọc trực tiếp API vẫn an toàn (nội dung không nhạy cảm). Reversible.
- Test SQLite (6): đọc cây theo lang (item ẩn IsActive=false bị loại); fallback khi thiếu bản dịch item; session sai → session_expired; FaqEnabled=false → rỗng; require-ack + chưa ack → rule_ack_required; refresh sliding-window.
- **Còn lại wave FAQ:** sub-slice B admin CRUD (tạo/sửa/ẩn category+item+upsert bản dịch, sanitize answer HTML, RequireStaff).
- Refs: `src/ResortQr.Application/Faq/{GuestFaqContracts,GetGuestFaqUseCase,GetGuestFaqValidator}.cs`, `Api/Endpoints/GuestAccessEndpoints.cs`; DEC-063/064/067, DEV-026, `18` §3.
- Reversible?: có.

### DEC-069: Wave FAQ — sub-slice B: admin CRUD → WAVE FAQ HOÀN TẤT (A+B+C)
- Status: Accepted (2026-07-05) — build 0W/0E; **224 test xanh** (76 unit + 5 arch + **143 integration** + 1 skip; +9 FaqAdmin). Doc: `faq-wave-design.md` §2-B.
- **Phân quyền `RequireStaff` (Staff|Admin):** theo master design §admin routes — `/faq` KHÔNG đánh dấu (Admin) như `/rooms`/`/settings` → FAQ là nội dung vận hành Staff soạn được (đồng nhất Rules draft).
- **CRUD cây (category → item) + bản dịch:** `GET /api/admin/faq` (cây đầy đủ kể cả inactive + MissingLanguages qua `IFaqAdminQueries`/`EfFaqAdminQueries` — Persistence namespace, wire tường minh DEC-049); `POST/PUT/DELETE /categories[/{id}]`; `POST/PUT/DELETE /items[/{id}]`. Create→201, Update/Delete→204. Answer HTML sanitize ở use case (Property B7). Xóa category → DB CASCADE item + mọi bản dịch.
- **MERGE bản dịch (quyết định bản chất — KHÁC Rules full-replace):** Update category/item cập nhật bản dịch bằng **upsert theo language_code** (có→sửa, thiếu→thêm, dư→xóa), KHÔNG delete-rồi-insert cùng key. Lý do: (1) tránh đúng lỗi thứ tự EF UPDATE-trước-INSERT vs unique `ux_tr_faq_cat`/`ux_tr_faq` (bài học DEV-025) → 1 SaveChanges an toàn; (2) giữ nguyên row (audit/xmin) khi chỉ sửa nội dung; (3) FAQ item/category có identity ỔN ĐỊNH (admin sửa từng cái), khác Rules draft là "tài liệu thay nguyên khối". Verified test `Update_category_merges_translations`/`Update_item_merges_translations` (đổi/thêm/xóa lang trong 1 save).
- Validators: ≥1 bản dịch; language_code duy nhất trong danh sách (chặn trước DB); LanguageCode ≤16, Name ≤200, Question ≤300.
- **→ WAVE FAQ HOÀN TẤT:** A (schema+migration, fix drift DEC-067) + B (admin CRUD, DEC-069) + C (guest read, DEC-068). Admin nhập → khách đọc chạy trọn vẹn.
- Refs: `src/ResortQr.Application/Faq/{FaqAdminContracts,FaqErrors,FaqAdminValidators,Create/Update/DeleteFaqCategory/ItemUseCase,IFaqAdminQueries}.cs`, `Infrastructure/Persistence/EfFaqAdminQueries.cs`, `Api/Endpoints/AdminFaqEndpoints.cs`; DEC-061 (Rules draft full-replace đối chiếu), DEV-025 (thứ tự EF), DEC-049/019.
- Reversible?: có.

### DEC-070: Wave Housekeeping — sub-slice A: domain + EF mapping + migration Housekeeping_Init + test ràng buộc
- Status: Accepted (2026-07-05) — build 0W/0E; **229 test xanh** (76 unit + 5 arch + **148 integration** + 1 skip; +5 HousekeepingSchema). Doc: `resort-qr/docs/housekeeping-wave-design.md`.
- **Mô hình:** `HousekeepingTicket` (AuditableEntity — audit ai tạo + xmin chặn 2 staff hoàn tất đè) trạng thái Requested→InProgress→Done|Cancelled; `HousekeepingEvent` (Entity append-only, nhật ký mỗi chuyển trạng thái). Ticket gắn visit/session (khách) HOẶC null (staff tạo cho phòng trống). Enum: `HousekeepingTicketStatus`, `HousekeepingCompletionMethod` (App/StaffScan) — lưu string per-property (`HasConversion<string>().HasMaxLength(16)`, DEC-006; không có global convention).
- **Bất biến sống còn (test-plan P10):** đúng **1 ticket "mở"/phòng** → partial unique `ux_hk_open` (room_id) WHERE `status IN ('Requested', 'InProgress')` — filter ENUM-STRING provider-agnostic đặt THẲNG config (như ux_qr_active/ux_visit_active; KHÔNG cần ProviderPartialIndex vì không có literal bool). Verified migration DDL + test SQLite (2 mở→chặn; đóng rồi mở lại→OK).
- **Delete behavior:** event ← ticket **Cascade** (nhật ký thuộc ticket); FK ticket → Resort/Room/GuestVisit?/GuestSession? = **Restrict** (visit/session nullable cho staff-ticket). Ticket KHÔNG xóa thường (Done/Cancelled giữ lịch sử).
- **Migration `Housekeeping_Init` per-wave** (additive, DEC-010) — verify `has-pending-model-changes` = No changes (không lặp lại drift FAQ — TK-045).
- **Lộ trình còn lại:** B guest create+read (`POST/GET /api/guest/housekeeping`, idempotent theo phòng + rule-gate + rate-limit); C staff (list/đổi trạng thái/complete-by-room|token/staff-create); tích hợp EndVisit hủy ticket mở (hoãn — qua port tránh coupling).
- Refs: `ResortQr.Domain/Housekeeping/**`, `Configurations/HousekeepingConfigurations.cs`, `Migrations/*_Housekeeping_Init.cs`, `tests/.../Housekeeping/HousekeepingSchemaTests.cs`; master §data model, test-plan P10/TC-HK; DEC-006/060/064/008, DEV-026.
- Reversible?: có (migration remove per-wave).

### DEC-071: Wave Housekeeping — sub-slice B: guest tạo + xem ticket (POST/GET /api/guest/housekeeping)
- Status: Accepted (2026-07-05) — build 0W/0E; **236 test xanh** (76 unit + 5 arch + **155 integration** + 1 skip; +7 GuestHousekeeping). Doc: `housekeeping-wave-design.md` §2-B.
- **`POST /api/guest/housekeeping`** (AllowAnonymous): guard portal-window → nếu `!HousekeepingEnabled` → **403 forbidden** (`HousekeepingErrors.FeatureDisabled`; ghi WRITE khi tắt tính năng phải chặn — khác READ trả rỗng) → `IRuleGate(Housekeeping)` (chưa ack + yêu cầu → rule_ack_required) → **IDEMPOTENT theo phòng**: đã có ticket "mở" (Requested|InProgress) cho phòng → trả lại (AlreadyOpen=true), KHÔNG tạo trùng (P10 chống double-tap); tạo mới → ticket Requested + `HousekeepingEvent(Requested, ByUserId=null)` + refresh window trong 1 SaveChanges; race 2 tạo → `ux_hk_open` chặn → bắt `UniqueConstraintViolationException` → trả ticket đang mở.
- **`GET /api/guest/housekeeping?visitId=`**: guard → CHỈ ticket `GuestVisitId = lượt hiện tại` (không lộ ticket lượt/khách khác — quyền riêng tư), OrderBy CreatedAt desc; tắt tính năng → rỗng; refresh window. Status enum serialize string (JsonStringEnumConverter DEC-059).
- **Quyết định phân biệt WRITE vs READ khi tắt tính năng:** create (WRITE) → 403 forbidden; read (GET) → rỗng (như FAQ). Lý do: cho ghi khi admin đã tắt là sai; đọc trạng thái ticket cũ thì vô hại.
- **Rate-limit: HOÃN có chủ đích (TK-046):** policy `guest-write` phân vùng theo session + `HousekeepingRateLimitPerHour` KHÔNG nhét vào use case này (fix ngọn) — thuộc task rate-limit cross-cutting #18 (áp đồng bộ mọi guest-write: messages + housekeeping). Chống spam TỨC THỜI đã có bằng bất biến domain "1 ticket mở/phòng" (ux_hk_open) + global IP limiter sẵn có.
- **Còn lại wave HK:** C staff (list/đổi trạng thái/complete-by-room|token/staff-create); tích hợp EndVisit hủy ticket mở.
- Refs: `src/ResortQr.Application/Housekeeping/{GuestHousekeepingContracts,HousekeepingErrors,CreateHousekeepingTicketUseCase,GetGuestHousekeepingUseCase,GuestHousekeepingValidators}.cs`, `Api/Endpoints/GuestAccessEndpoints.cs`; DEC-063/064/070, DEV-026, test-plan P10/TC-HK; TK-046.
- Reversible?: có.

### DEC-072: Wave Housekeeping — sub-slice C: staff xử lý ticket → WAVE HOUSEKEEPING HOÀN TẤT (A+B+C)
- Status: Accepted (2026-07-05) — build 0W/0E; **246 test xanh** (76 unit + 5 arch + **165 integration** + 1 skip; +10 HousekeepingStaff). Doc: `housekeeping-wave-design.md` §2-C.
- **Phân quyền `RequireStaff`** (docs task 3.2 — "Staff xử lý ticket"). Endpoints `/api/admin/housekeeping`: GET list (phân trang + lọc status), POST staff-create, PUT /{id}/status, POST /complete-by-room, POST /complete-by-token.
- **Máy trạng thái (ChangeHousekeepingStatusUseCase):** Requested→InProgress→Done, hoặc (Requested|InProgress)→Cancelled; Done/Cancelled = terminal → `conflict` (InvalidTransition). Mỗi lần đổi ghi đúng 1 `HousekeepingEvent` (P10). →Done set CompletedByUserId/At + Method=App.
- **complete-by-room (App) / complete-by-token (StaffScan):** dùng chung helper `HousekeepingCompletion.CompleteOpenTicketForRoomAsync` (tránh nhân bản — fix gốc) — đóng ticket ĐANG MỞ của ĐÚNG phòng (P10). Token map đúng phòng kể cả Revoked (ux_qrtoken_token toàn cục, không tái dùng). Không có ticket mở → not_found.
- **StaffCreate:** tạo cho phòng (visit null — TC-HK-05), idempotent theo phòng (race ux_hk_open → bắt UniqueConstraintViolationException).
- **Read-model `IHousekeepingQueries`/`EfHousekeepingQueries`** (Persistence namespace, DEC-049): list phân trang + lọc status, RoomNumber qua correlated subquery. **OrderBy Id (UUIDv7 time-ordered)** thay CreatedAt vì SQLite không ORDER BY DateTimeOffset (TK-047) — provider-agnostic + tất định.
- **→ WAVE HOUSEKEEPING HOÀN TẤT:** A (schema/migration/ux_hk_open) + B (guest create+read idempotent) + C (staff máy-trạng-thái/complete/list). Vòng đời: khách/staff tạo → staff nhận (InProgress) → hoàn tất (Done, App/StaffScan) hoặc Cancelled; mỗi bước 1 event.
- **Còn lại (cross-module, hoãn):** EndVisit/Sweeper hủy ticket mở khi kết thúc visit (TC-VIS-04) — nối qua port tránh coupling; rate-limit guest-write (TK-046).
- Refs: `src/ResortQr.Application/Housekeeping/**` (ChangeStatus/CompleteByRoom/CompleteByToken/StaffCreate/HousekeepingCompletion/IHousekeepingQueries/validators), `Infrastructure/Persistence/EfHousekeepingQueries.cs`, `Api/Endpoints/AdminHousekeepingEndpoints.cs`; DEC-070/071, test-plan P10/TC-HK-03/04/05; TK-047.
- Reversible?: có.

### DEC-073: Admin Settings — đọc/cập nhật ResortSettings (GET/PUT /api/admin/settings)
- Status: Accepted (2026-07-05) — build 0W/0E; **257 test xanh** (76 unit + 5 arch + **176 integration** + 1 skip; +11 SettingsAdmin). KHÔNG migration (không đổi schema — entity/config ResortSettings đã có).
- **Phân quyền `RequireAdmin`** (master design §admin routes: `/settings(Admin)` — khác `/faq`,`/rules`,`/housekeeping` là Staff|Admin). Cấu hình vận hành là quyền Admin.
- **`GET /api/admin/settings`** → `IResortSettingsReader` (thuần IUnitOfWork → auto-scan, KHÔNG cần query service coupled DbContext vì chỉ đọc 1 entity 1-1); chưa cấu hình → not_found. **`PUT`** → `UpdateResortSettingsUseCase` full-replace toàn bộ trường (cờ FAQ/Chat/Housekeeping + RequireRuleAck* + PortalWindowMinutes/VisitIdleExpiryHours + GuestWebBaseUrl + Max/rate) — 1 SaveChanges; xmin (AuditableEntity) tự kiểm concurrency trên Postgres.
- **Validator khớp CHECK constraint DB (defense-in-depth):** PortalWindowMinutes 1..1440 (ck_portal_window), VisitIdleExpiryHours 1..168 (ck_idle_expiry), MaxMessageLength 1..10000 (ck_msg_len), rate 1..1000; GuestWebBaseUrl null HOẶC URL tuyệt đối **HTTPS** ≤512 (Req 15.6 — secure context camera/QR). Chặn TRƯỚC bằng validation_error thay vì để DB ném lỗi thô.
- **Giá trị hệ thống:** đây là mảnh "mở khóa" — mọi cổng đã xây (RuleGate, FAQ/Housekeeping enabled, PortalWindow) đọc ResortSettings; giờ admin đổi được runtime (bật/tắt tính năng, yêu cầu ack, cửa sổ phiên) mà không cần redeploy.
- Refs: `src/ResortQr.Application/Settings/**`, `Api/Endpoints/AdminSettingsEndpoints.cs`; `04` §7.4 (CHECK), `15` §5, Req 15.3/15.6; DEC-020 (Options fail-fast — settings runtime khác secret startup).
- Reversible?: có.

### DEC-074: Rate-limit bề mặt guest (task #18, Req 13) — policy `resolve` + `guest-write` sliding-window
- Status: Accepted (2026-07-05) — build 0W/0E; **259 test xanh** (76 unit + 5 arch + **178 integration** + 1 skip; +2 GuestRateLimit HTTP). Đóng phần đã hoãn TK-046.
- **`resolve`** (GET /api/guest/resolve/{token}): sliding-window partition theo IP, mặc định 20/60s (Req 13.1). **`guest-write`** (POST /rules/acknowledge + POST /housekeeping): sliding-window partition theo SESSION (hash cookie), fallback IP, mặc định 10/60s (Req 13.2/13.3). Vượt → 429 ProblemDetails code=rate_limited + Retry-After (dùng lại `OnRejected` sẵn có), KHÔNG xử lý request (limiter chạy ở `UseRateLimiter` TRƯỚC auth/endpoint — pipeline order sẵn có).
- **Ngưỡng qua `RateLimitOptions` (appsettings) + default**, đọc LAZY mỗi request trong partition factory (không capture eager — DEC-040/042). SegmentsPerWindow cấu hình (mặc định 6).
- **GET đọc (rules/faq/housekeeping) KHÔNG gắn policy riêng** — chỉ đọc, không tạo tài nguyên; Req chỉ yêu cầu limit resolve (GET) + guest-write (POST). (Global limiter chưa bật GlobalLimiter → GET reads không bị chặn ở tầng này; chấp nhận cho MVP nội bộ.)
- **Partition guest-write theo hash(cookie) thay GuestSessionId — xem DEV-027.** **Req 13.5 (precedence ResortSettings→appsettings) phần ResortSettings HOÃN — xem TK-048.**
- Refs: `src/ResortQr.Api/RateLimiting/{RateLimitOptions,ResortQrRateLimitExtensions}.cs`, `Api/Endpoints/GuestAccessEndpoints.cs`; Req 13.1–13.5; DEV-027, TK-046/048.
- Reversible?: có (đổi ngưỡng/segments qua config; thêm policy GET nếu cần).

### DEC-075: EndVisit cascade — port `IVisitEndHandler` (cross-module) hủy ticket mở khi visit kết thúc
- Status: Accepted (2026-07-05) — build 0W/0E; **261 test xanh** (76 unit + 5 arch + **180 integration** + 1 skip; +2 cascade). Bám tasks #24 / TC-VIS-04 / P9.
- **Vấn đề (bản chất):** visit kết thúc (lazy-expiry) mà ticket dọn phòng đang MỞ không bị hủy → phòng "kẹt" 1 ticket mở mãi (vi phạm bất biến "1 ticket mở/phòng" về ngữ nghĩa + P9). Trước đây cascade bị HOÃN (comment trong ResolveTokenUseCase/PortalWindowGuard) vì chưa có entity Housekeeping.
- **Thiết kế cross-module (giữ ranh giới master §2.3):** port `IVisitEndHandler` do **GuestAccess SỞ HỮU**; module nghiệp vụ (Housekeeping) **implement** → không đảo chiều phụ thuộc (Housekeeping→GuestAccess, như IPortalWindowGuard). Nhiều impl tương lai (Messaging) → inject `IEnumerable<IVisitEndHandler>` (Scrutor AsImplementedInterfaces hỗ trợ). `HousekeepingVisitEndHandler` chỉ đụng entity Housekeeping (query theo GuestVisitId — entity của mình, KHÔNG đọc chéo GuestVisit).
- **Hợp đồng NGUYÊN TỬ (quan trọng):** handler chỉ **STAGE** (Add/mutate ChangeTracker qua IUnitOfWork scoped dùng chung), KHÔNG tự SaveChanges. Điểm kết thúc visit (caller) gọi MỘT SaveChanges → visit-status + hủy ticket + event commit CÙNG transaction (all-or-nothing). Idempotent (không ticket mở → no-op).
- **Đã wire:** lazy-expiry ở `ResolveTokenUseCase` (guest quay lại sau hết hạn) + `PortalWindowGuard` (endpoint tương tác gặp visit hết hạn) — cả hai stage handler trước SaveChanges sẵn có. Test: guard lazy-expiry → visit Expired + ticket Cancelled + event; handler no-op khi không có ticket mở.
- **CÒN LẠI (quan trọng):** `VisitIdleSweeper` (DEC-008, tasks #25) — proactive dọn visit idle mà khách KHÔNG quay lại (lazy-expiry chỉ fire khi có truy cập). Sweeper sẽ dùng CHÍNH `IVisitEndHandler` này. Ticket của visit hết hạn mà khách không quay lại HIỆN chỉ được hủy khi có sweeper → làm tiếp.
- **Churn test:** thêm param IEnumerable<IVisitEndHandler> vào ctor ResolveTokenUseCase + PortalWindowGuard → cập nhật 4 file test (truyền `[]` cho test không kiểm cascade). Chấp nhận vì đây là thiết kế đúng (mọi điểm kết thúc cascade đồng bộ, không stuck).
- Refs: `src/ResortQr.Application/GuestAccess/{IVisitEndHandler,ResolveTokenUseCase,PortalWindowGuard}.cs`, `Housekeeping/HousekeepingVisitEndHandler.cs`; tasks #24/#25, test-plan TC-VIS-04/P9, DEC-008/030/072, master §2.3.
- Reversible?: có (bỏ handler → về hành vi cũ).

### DEC-076: VisitIdleSweeper — hosted service dọn visit idle + cascade (DEC-008/tasks #25) → KHÉP KÍN vòng đời visit
- Status: Accepted (2026-07-05) — build 0W/0E; **264 test xanh** (76 unit + 5 arch + **183 integration** + 1 skip; +3 VisitEnder).
- **`IVisitEnder.EndExpiredVisitAsync(visitId, now)`** (Application/GuestAccess, testable): thao tác EndVisit dùng chung — idempotent (null / không Active / `now<=ExpiresAt` → no-op, chống race với lazy-expiry/sliding-window), ngược lại Expired + ClosedAt + cascade `IVisitEndHandler` (DEC-075) → 1 SaveChanges. Tái dùng cho staff-close sau này.
- **`VisitIdleSweeper : BackgroundService`** (Api/BackgroundServices — hosted service DUY NHẤT, DEC-008): loop **DELAY-TRƯỚC** (mặc định 300s — DEC-009) → scan visit Active quá hạn (1 scope) → **mỗi visit 1 DI scope riêng** gọi `IVisitEnder` (cách ly lỗi Req 14.8: 1 visit fail → log + tiếp tục, context sạch không lây trạng thái). Bắt lỗi cả-lượt (không sập host). TUYỆT ĐỐI không đụng RoomQrToken (Req 14.5).
- **Delay-trước để test an toàn:** integration test (GuestApiFactory chạy Program) có sweeper đăng ký nhưng delay 300s > thời gian test → KHÔNG kích hoạt (tránh nhiễu: sweeper vô tình expire visit seed giữa test). Cancellation lúc shutdown thoát sạch. Thêm `VisitSweeper:Enabled` (mặc định true) để tắt hẳn nếu cần.
- **CA1848:** logger dùng `[LoggerMessage]` source-gen (partial) thay `LogError` trực tiếp (TreatWarningsAsErrors bắt CA1848) — TK.
- **Vòng đời visit KHÉP KÍN:** lazy-expiry (resolve/guard, DEC-075) bắt khi khách quay lại; sweeper (proactive) bắt khi khách KHÔNG quay lại. Cả hai dùng chung `IVisitEndHandler` → ticket mở luôn được hủy, không "kẹt".
- **Test:** `VisitEnder` (kết thúc expired + cancel ticket; no-op khi chưa quá hạn; no-op khi đã Expired). Hosted-service timer (thin wrapper) không unit-test riêng — logic lõi ở VisitEnder đã phủ.
- Refs: `src/ResortQr.Application/GuestAccess/{IVisitEnder,VisitEnder}.cs`, `Api/BackgroundServices/{VisitIdleSweeper,VisitSweeperOptions}.cs`, `Program.cs`; DEC-008/009/075, Req 14.4/14.5/14.8, tasks #25.
- Reversible?: có (tắt qua config; gỡ hosted service).

### DEC-077: Admin Dashboard — read-model tổng quan vận hành (GET /api/admin/dashboard)
- Status: Accepted (2026-07-05) — build 0W/0E; **266 test xanh** (76 unit + 5 arch + **185 integration** + 1 skip; +2 Dashboard). KHÔNG migration (chỉ đọc, không entity mới).
- **Nội dung (AI tự chốt — spec chỉ ghi "Dashboard: stats aggregation", không liệt kê chỉ số):** phòng (tổng/active), ticket dọn phòng mở (tổng + Requested + InProgress), version nội quy hiện hành (null nếu chưa publish), FAQ (category/item active), visit đang Active. Lý do chọn các chỉ số: đều là số liệu VẬN HÀNH tức thời mà staff/admin cần nhìn nhanh, tái dùng dữ liệu sẵn có (không thêm entity/bảng). Không làm báo cáo lịch sử/biểu đồ (YAGNI cho MVP).
- **Phân quyền `RequireStaff`** (master routes `/dashboard` KHÔNG đánh dấu Admin như `/rooms`/`/settings` → Staff|Admin xem được tổng quan vận hành).
- **`IDashboardQueries`/`EfDashboardQueries`** (Persistence namespace, DEC-049): dùng `CountAsync` (AsNoTracking) per chỉ số. Room DbSet có query filter → TotalRooms là phòng còn sống. Open ticket = Requested+InProgress (nhất quán định nghĩa "mở" ux_hk_open). CurrentRulesVersion qua publication IsCurrent (null nếu chưa publish).
- Test: đủ số liệu (2 phòng/1 active, 2 ticket mở, version 3, 1 FAQ cat active + 2 item, 1 visit active); resort rỗng → toàn 0 + version null.
- Refs: `src/ResortQr.Application/Dashboard/{DashboardContracts,IDashboardQueries}.cs`, `Infrastructure/Persistence/EfDashboardQueries.cs`, `Api/Endpoints/AdminDashboardEndpoints.cs`; master §folder Dashboard, DEC-049.
- Reversible?: có (thêm/bớt chỉ số dễ).

### DEC-078: Wave Notes — ghi chú nội bộ staff (Req 9.4, P7) → module hoàn tất (schema + CRUD + query)
- Status: Accepted (2026-07-06) — build 0W/0E; **276 test xanh** (76 unit + 5 arch + **195 integration** + 1 skip; +10 Notes). Migration `Notes_Init` + `has-pending-model-changes` = No changes (TK-045).
- **Mô hình:** `InternalNote : AuditableEntity` — tác giả = `CreatedByUserId` (audit interceptor tự gán, KHÔNG nhận qua input/body → chống giả mạo tác giả); `CreatedAt/UpdatedAt` tự set; xmin chống 2 staff sửa cùng note ghi đè âm thầm. `Body` bắt buộc ≤ 2000. Gắn với **phòng HOẶC hội thoại** (Req 9.4) — validator bắt buộc ≥1 trong `RoomId`/`ConversationId`.
- **Quyết định AI tự ra (spec chỉ nêu entity + "P7: never shown to guest"):**
  1. **`ConversationId` = cột `Guid` nullable THUẦN, KHÔNG FK** — entity `Conversation` thuộc wave Messaging (CHƯA xây). Thêm FK ở migration wave Messaging sau. Lý do: không thể `HasOne<Conversation>` khi type chưa tồn tại; tạo cột trước để schema note ổn định, tránh migration phá vỡ note khi Messaging tới. `RoomId` thì CÓ FK Room (Restrict) vì Room đã tồn tại. `ResortId` FK Resort (Restrict).
  2. **P7 (không lộ cho khách) đảm bảo BẰNG KIẾN TRÚC, không bằng cờ runtime:** chỉ nhóm endpoint `/api/admin/notes` (`RequireStaff`) + `INoteQueries` chạm tới `InternalNote`; KHÔNG use case guest nào tham chiếu entity/interface này. Không có mặt phẳng tấn công runtime nên không có test phủ định "guest đọc note" — thay vào đó test tài liệu hóa nguyên tắc by-construction. Fix gốc: P7 là bất biến cấu trúc, mạnh hơn kiểm tra runtime dễ quên.
  3. **Delete = HARD delete** (InternalNote không `ISoftDeletable`): ghi chú nội bộ không phải tài nguyên nghiệp vụ cần vết lịch sử/khôi phục như Room; xóa là xóa hẳn. Gọi lại sau khi xóa → `not_found` (idempotent-friendly).
  4. **`RoomId`/`ConversationId` bất biến sau tạo (`init`):** update chỉ đổi `Body`, không đổi đối tượng gắn kèm (muốn đổi đối tượng → xóa + tạo lại). Giảm bề mặt lỗi + giữ ngữ nghĩa "note thuộc về X".
- **Endpoints (`RequireStaff`):** GET `/api/admin/notes?roomId=&conversationId=` (list, lọc), POST (create→201), PUT `/{id:guid}` (update Body→204), DELETE `/{id:guid}` (→204). `EfNoteQueries` (namespace `Infrastructure.Persistence`, loại auto-scan, wire tường minh — DEC-049); OrderByDescending(Id) (UUIDv7 time-ordered, SQLite không ORDER BY DateTimeOffset — TK-047). Index `ix_note_room`, `ix_note_conversation` (lọc theo đối tượng, không unique — 1 phòng/hội thoại nhiều note).
- Evidence/Refs: `docs/resort-qr-portal/design.md` §data model (InternalNote), Req 9.4, P7; DEC-049, DEC-006, TK-045, TK-047.
- Reversible?: có (migration remove per-wave; FK Conversation thêm sau ở wave Messaging).


### DEC-079: OpenAPI expose `/openapi/v1.json` (GAP-6/TK-022) — built-in .NET 10 + an-toàn-mặc-định + fix CVE transitive
- Status: Accepted (2026-07-06) — build 0W/0E; **278 test xanh** (76 unit + 5 arch + **197 integration** + 1 skip; +2 OpenApi). Doc: `resort-qr/docs/openapi-wave-design.md`.
- **Chọn `Microsoft.AspNetCore.OpenApi` 10.0.9 (built-in .NET 10), KHÔNG Swashbuckle/NSwag.** Lý do chính xác: gói CHÍNH THỨC cùng nhịp version/vá bảo mật với runtime (10.0.x) — Swashbuckle nhiều giai đoạn trễ nhịp .NET mới, NSwag nặng hơn nhu cầu; chỉ cần document JSON để FE sinh `shared-types` (DEC-029/032 contract-first), không cần UI nhúng.
- **CVE transitive — fix TẬN GỐC (không NoWarn):** `Microsoft.AspNetCore.OpenApi` 10.0.9 kéo `Microsoft.OpenApi` **2.0.0** dính **CVE-2026-49451** (GHSA-v5pm-xwqc-g5wc, HIGH — DoS stack-overflow parse circular `$ref`). NuGet audit + TreatWarningsAsErrors → NU1903. Advisory (đã verify web): affected `>=2.0.0-preview11,<=2.7.4`; patched dòng 2.x = **2.7.5**. → pin transitive `Microsoft.OpenApi=2.7.5` trong CPM (cùng major 2.x mà AspNetCore.OpenApi kỳ vọng → tránh breaking 3.x). Giống pattern SQLitePCLRaw trước đó. KHÔNG tắt audit.
- **An-toàn-mặc-định (secure-by-default) — QUYẾT ĐỊNH BẢO MẬT:** document OpenAPI lộ toàn bộ bề mặt API (reconnaissance). Cổng `OpenApi:Enabled` (bool?, null → BẬT khi `IsDevelopment()`, TẮT nơi khác); cờ tường minh THẮNG suy luận môi trường. Production giữ TẮT ở public; bật tường minh sau proxy nội bộ/CI nếu FE build pipeline cần.
- **Fix bẫy timing config (fix gốc, không vá ngọn):** ban đầu đọc `builder.Configuration.GetValue("OpenApi:Enabled")` EAGER ở top-level Program → chạy TRƯỚC khi `WebApplicationFactory` tiêm config test (áp lúc `builder.Build()`) → test "tắt" fail (luôn nhận 200 vì fallback IsDevelopment). Nguyên nhân bản chất: đọc eager không thấy config tiêm-sau; còn Options (JWT) bind LAZY sau build nên thấy. **Sửa:** `AddResortQrOpenApi()` LUÔN đăng ký generator (không phơi endpoint → không rủi ro); gate việc PHƠI ở `MapResortQrOpenApi()` đọc `app.Configuration`/`app.Environment` POST-build (đã gồm mọi override) → chính xác + test tất định. Thứ nhạy cảm bảo mật là *phơi endpoint*, không phải *đăng ký service*.
- **Nội dung document:** Info (title "Resort QR Portal API", version v1, description) + securityScheme `Bearer` (Http/bearer/JWT/Header) ở components + operation transformer gắn yêu cầu Bearer CHỈ cho operation có `IAuthorizeData` và KHÔNG `IAllowAnonymous` (admin=JWT; guest cookie/ẩn danh KHÔNG gắn) → contract phản ánh đúng mô hình auth.
- **API Microsoft.OpenApi 2.7.5 đã VERIFY bằng reflection** (file-based `dotnet run` + `#:sdk`/`#:package`, thư mục tạm chặn CPM/analyzer) trước khi code — không đoán chữ ký: `OpenApiInfo{Title,Version,Description}`, `OpenApiComponents.SecuritySchemes: IDictionary<string,IOpenApiSecurityScheme>`, `OpenApiSecurityScheme{Type:SecuritySchemeType?,Scheme,BearerFormat,In:ParameterLocation?}`, `OpenApiSecurityRequirement:Dictionary<IOpenApiSecurityScheme,IList<string>>`, `OpenApiSecuritySchemeReference(string,OpenApiDocument,string)`. (TK-053)
- **Phạm vi giữ hẹp:** wave này CHỈ document + Info + security. NỢ chú thích `.Produces<T>()`/`.Accepts<T>()` per-endpoint để schema response đầy đủ (khối lượng lớn, enrich dần khi FE cần — TK-054). Document vẫn hợp lệ + liệt kê path/verb/tham số/request body records.
- Evidence/Refs: `openapi-wave-design.md`; GHSA-v5pm-xwqc-g5wc; DEC-029/032; TK-022/052/053/054.
- Reversible?: có (gỡ package + 2 dòng wiring); cờ bật/tắt runtime.


### DEC-080: Enrich OpenAPI response schema — nhóm Auth (trả nợ TK-054, làm dần theo nhóm)
- Status: Accepted (2026-07-06) — build 0W/0E; test: **arch 5/5**, **integration OpenApi 3/3** (thêm 1 test mới), phần còn lại không đụng. Doc: `resort-qr/docs/openapi-enrich-wave-design.md`.
- **Nguyên nhân gốc (verify):** endpoint minimal-API trả `Task<IResult>` → kiểu bị xóa → ApiExplorer không suy được schema response → `/openapi/v1.json` rỗng phần responses. KHÔNG phải lỗi generator. Fix gốc = khai báo tường minh metadata `.Produces<T>()`/`.ProducesProblem()` (chỉ thêm metadata, KHÔNG đổi logic runtime).
- **Phương pháp khai status CHÍNH XÁC (không bịa):** success đọc thẳng endpoint; lỗi nghiệp vụ đọc `*Errors` + map `ErrorTypeToHttp`; lỗi pipeline theo authz/rate-limit/validator/exception. Chỉ khai status có căn cứ. Đã verify: `LoginCommand`/`RefreshCommand` KHÔNG có validator → login/refresh KHÔNG khai 400.
- **Đã làm (nhóm Auth):** helper `OpenApi/OpenApiConventions.ProducesProblems(params int[])` (application/problem+json). Enrich: `POST /auth/login`→`Produces<TokenResponse>()`+401/429/500; `POST /auth/refresh`→`Produces<TokenResponse>()`+401/429/500; `POST /auth/logout`→`Produces(204)`+429/500; `GET /auth/me`→ (200 MeResponse tự suy từ `Ok<MeResponse>`)+401/429/500.
- **Test tài liệu-hóa fix (không chỉ test code):** `OpenApiEndpointTests.Auth_login_operation_documents_success_and_error_responses` fetch `/openapi/v1.json` → assert `paths./auth/login.post.responses` có `200` (content `application/json`) + `401`. Fix hiệu lực ở tầng document thật.
- **Giới hạn đã biết (không ảo tưởng "đủ 100%"):** `.ProducesProblem` document ProblemDetails cơ bản, CHƯA mô tả extension `code`/`errors` trong schema (FE map theo `code`). Cân nhắc schema tùy biến ở wave sau nếu FE cần — chưa chặn type-gen success DTO.
- **Phạm vi giữ hẹp (làm dần):** wave này CHỈ nhóm Auth. Còn lại: Guest (resolve/rules/faq/housekeeping — có validator → thêm 400; resolve 404/409/429), Admin Rooms (+QR png image/png), Rules/FAQ/Housekeeping/Notes/Settings/Dashboard.
- Evidence/Refs: `openapi-enrich-wave-design.md`; TK-054; DEC-079/029/032; `ErrorTypeToHttp`, `AuthErrors`.
- Reversible?: có (gỡ metadata; helper độc lập).


### DEC-081: Enrich OpenAPI response schema — nhóm Guest (tiếp TK-054)
- Status: Accepted (2026-07-06) — build 0W/0E; test: **integration OpenApi 4/4** (thêm 1 test guest). Doc: `openapi-enrich-wave-design.md`.
- **Status khai theo CĂN CỨ đọc use case (verify, không đoán):**
  - `GET /resolve/{token}` (ResolveTokenUseCase, không guard/validator): `Produces<ResolveResponse>` + 404 (qr_invalid/qr_revoked) + 409 (room_inactive) + 429 (ResolvePolicy) + 500.
  - `GET /rules` (GetGuestRulesUseCase, PortalWindowGuard + validator): `GuestRulesResponse` + 400 + 403 (session_expired) + 500. (không rate-limit GET → không 429).
  - `GET /faq` (GetGuestFaqUseCase, guard + IRuleGate + validator): `GuestFaqResponse` + 400 + 403 (session_expired/rule_ack_required) + 500.
  - `GET /housekeeping` (guard + validator): `GuestHousekeepingResponse` + 400 + 403 + 500.
  - `POST /rules/acknowledge` (guard + validator; NoPublishedRules=400; GuestWrite rate-limit): `AcknowledgeRulesResult` + 400 + 403 + 429 + 500.
  - `POST /housekeeping` (guard + gate + feature + validator; rate-limit): `CreateHousekeepingTicketResult` + 400 + 403 + 429 + 500. (guest create trả 200 Ok, không 201).
- **Căn cứ 403 chắc chắn:** mọi endpoint guest interactive dùng `PortalWindowGuard` → luôn có thể `session_expired` (Forbidden 403) — verify tại `PortalWindowGuard.cs` (5 nhánh Fail SessionExpired). resolve KHÔNG guard (nó TẠO visit) → không 403.
- **Test:** `OpenApiEndpointTests.Guest_endpoints_document_success_and_error_responses` — resolve có 200(application/json)/404/409; faq có 400/403.
- Evidence/Refs: `GetGuestRulesUseCase`, `GetGuestFaqUseCase`, `AcknowledgeRulesUseCase`, `ResolveTokenUseCase`, `PortalWindowGuard`, `RuleGate`, `*Errors`; DEC-080; TK-054.
- Reversible?: có (metadata-only).


### DEC-082: Enrich OpenAPI response schema — nhóm Admin Rooms (+QR png) (tiếp TK-054)
- Status: Accepted (2026-07-06) — build 0W/0E; **281 test** (unit 76 + arch 5 + integration 199 + 1 skip; +1 test OpenApi admin). Doc: `openapi-enrich-wave-design.md`.
- **Status khai theo use case (verify grep `Application/Rooms/*`):** List→`Produces<PagedResult<RoomListItem>>`+401/403/500; GetById→`RoomListItem`+404+401/403/500; Create→`CreateRoomResult`(201)+400(validator/room_number_taken)+409(qr_generation_failed)+401/403/500; Update→204+400+404+401/403/500; ChangeStatus→204+400(enum)+404+401/403/500; Delete→204+404+401/403/500; RotateToken→`RotateRoomTokenResult`+404+409+401/403/500; RenderQr→**image/png**+400(invalid_configuration)+404+409+401/403/500. Admin KHÔNG rate-limit → KHÔNG 429.
- **BUG TÌM ĐƯỢC + FIX GỐC (ground truth, không đoán):** `.Produces(200, contentType:"image/png")` (responseType=null) → document 200 chỉ có `{"description":"OK"}` **THIẾU `content`** → FE không biết body kiểu gì. Nguyên nhân gốc: generator không phát content schema khi thiếu responseType. **Fix:** `.Produces<byte[]>(200, "image/png")` → 200 có `content.image/png.schema{type:string,format:byte}`. Verify bằng dump `/openapi/v1.json` thật (WebApplicationFactory in-process) TRƯỚC/SAU fix.
- **Concurrency 409:** chỉ khai 409 ở endpoint use case trả Conflict tường minh (Create/RotateToken/RenderQr = qr_generation_failed). 409 do optimistic-concurrency (`xmin`) là khả năng cross-cutting của MỌI mutation trên AuditableEntity — KHÔNG liệt kê per-endpoint (giữ đúng cách codebase xử lý ở middleware, tránh nhiễu). Ghi rõ trong wave-design để không hiểu nhầm là bỏ sót.
- **Test:** `OpenApiEndpointTests.Admin_room_qr_endpoint_documents_png_and_error_responses` — qr.png có security Bearer + 200(content image/png) + 404 + 409.
- Evidence/Refs: `AdminRoomEndpoints.cs`, `Application/Rooms/*UseCase.cs`, `RoomsErrors`; DEC-080/081; TK-054.
- Reversible?: có (metadata-only).


### DEC-083: Enrich OpenAPI — nốt nhóm Admin (Rules/FAQ/Housekeeping/Notes/Settings/Dashboard) → HOÀN TẤT TK-054
- Status: Accepted (2026-07-06) — build 0W/0E; **282 test** (unit 76 + arch 5 + integration 200 + 1 skip; +1 test OpenApi admin content). Doc: `openapi-enrich-wave-design.md`.
- **Helper mới:** `OpenApiConventions.ProducesAdminAuthProblems()` = 401+403+500 (mọi endpoint admin có RequireAuthorization) → giảm lặp; lỗi nghiệp vụ (400/404/409) khai riêng theo use case.
- **Status khai theo use case (verify grep *Errors + endpoint):**
  - Rules: GET draft→`RuleDraftDto`; PUT draft→204+400; POST publish→`PublishRulesResult`+400(no_draft)+409(publish_conflict/concurrency).
  - FAQ (Staff): GET tree→`FaqAdminTreeDto`; POST cat→`CreateFaqCategoryResult`(201)+400; PUT cat→204+400+404; DEL cat→204+404; POST item→`CreateFaqItemResult`(201)+400(category_required); PUT item→204+400+404; DEL item→204+404.
  - Housekeeping (Staff): GET list→`PagedResult<HousekeepingTicketListItem>`+400; POST staff-create→`StaffCreateHousekeepingTicketResult`(201)+400; PUT status→204+400+404(ticket_not_found)+409(invalid_transition); complete-by-room→204+400+404(no_open_ticket); complete-by-token→204+400+404(qr_invalid/no_open_ticket).
  - Notes (Staff): GET list→`IReadOnlyList<InternalNoteDto>`; POST→`CreateInternalNoteResult`(201)+400; PUT→204+400+404; DEL→204+400(validator NoteId)+404.
  - Settings (Admin): GET→`ResortSettingsDto`+404(not_configured); PUT→204+400+404 (UpdateResortSettings trả NotConfigured nếu thiếu — verify).
  - Dashboard (Staff): GET→`DashboardSummaryDto`.
- **Test:** `OpenApiEndpointTests.Admin_content_endpoints_document_success_and_error_responses` — settings GET 200(json)+404; publish 200+409; faq create category 201. (Path key `MapGroup(base)+MapGet("/")` → base KHÔNG trailing slash — xác nhận từ dump rooms trước đó.)
- **TK-054 DONE:** tất cả nhóm endpoint (auth/guest/admin) đã có success DTO + error responses trong `/openapi/v1.json`. Giới hạn còn lại (không chặn): ProblemDetails schema chưa mô tả extension `code`/`errors` (FE map theo `code`) — cân nhắc schema tùy biến nếu FE cần.
- Evidence/Refs: `Admin*Endpoints.cs`, query interfaces (IFaqAdminQueries/IHousekeepingQueries/INoteQueries/IDashboardQueries/IResortSettingsReader/IRuleDraftQueries), `*Errors`; DEC-080/081/082.
- Reversible?: có (metadata-only).


### DEC-084: Messaging/Chat — wave design (design-first, chia 3 slice) trước khi code
- Status: Design accepted (2026-07-06) — CHƯA code (design-first theo quy trình). Doc: `resort-qr/docs/messaging-wave-design.md`. Build/test không đổi (282 xanh — không đụng code).
- **Validate với authoritative:** `design/backend/21-realtime-signalr.md` (auth kép/authorize-on-join/EndVisit-evict/event contract), Req 5 (+5.9 isolation, +5.10 reopen), Req 10.8 (cascade), data model Conversation/Message, DEC-026/031, TK-050 (Notes FK nợ).
- **Cascade tái dùng pattern hiện có (verify code):** thêm `MessagingVisitEndHandler : IVisitEndHandler` (STAGE-only, đóng conversation Open của visit) — auto-wire Scrutor `IEnumerable<IVisitEndHandler>` giống `HousekeepingVisitEndHandler`; KHÔNG sửa GuestAccess/PortalWindowGuard/VisitEnder.
- **Bất biến chốt:** unique tổng `Conversation(GuestVisitId)` (1 conv/visit — reopen Req 5.10); UnreadForStaff đơn điệu (P11); isolation theo GuestVisit (P6/B9); cascade close on EndVisit (Req 10.8); wire FK `InternalNote.ConversationId→Conversation` (trả nợ TK-050).
- **Quyết định cần theo dõi:** `Conversation:AuditableEntity` (RowVersion xmin) — rủi ro 409 khi tăng unread đồng thời; MVP tăng thủ công (1 khách/visit tuần tự), nếu thành vấn đề chuyển sang recompute COUNT. `Message:Entity` (append-only, SenderUserId gán tường minh không qua audit).
- **Chia slice:** A = domain+EF+migration `AddMessaging`+cascade handler+Notes FK (không API/hub); B = use cases guest/admin + REST + OpenAPI enrich + test; C = SignalR hub + IRealtimeNotifier events + NotifyVisitEnded + test.
- **Test plan:** TC-MSG-A1..A3 (unique/cascade/note-FK), B1..B5 (unread/reopen/isolation/rule-gate/session), C1..C2 (join authorize/evict). Xem doc §9.
- Evidence/Refs: `IVisitEndHandler.cs`, `HousekeepingVisitEndHandler.cs`, `PortalWindowGuard.cs`, `VisitEnder.cs`, `21-realtime-signalr.md`; TK-050/024, DEC-026/031.
- Reversible?: N/A (chỉ design doc).


### DEC-085: Messaging/Chat — Slice A (domain + EF + migration + cascade + wire Notes FK) → HOÀN TẤT
- Status: Accepted (2026-07-06) — build 0W/0E; **286 test pass** (unit 76 + arch 5 + integration 205 + 1 skip), migration `has-pending-model-changes` = **No changes** (TK-045). Doc: `resort-qr/docs/messaging-wave-design.md` §8 Slice A, §9 TC-MSG-A1..A3.
- **Domain (verify code):** `ResortQr.Domain/Messaging/` — `Conversation : AuditableEntity` (RowVersion xmin cho unread), `Message : Entity` (append-only, `SenderUserId` gán tường minh — KHÔNG qua audit interceptor), enum `ConversationStatus{Open,Closed}` + `MessageSenderType{Guest,Staff,System}` (→string HasMaxLength(16)).
- **EF config (`MessagingConfigurations.cs`):** `ux_conversation_visit` unique tổng trên `conversation(guest_visit_id)` (1 conv/visit — reopen Req 5.10); index `ix_conversation_room`; FK `Conversation`→Resort/Room/GuestVisit/GuestSession = **Restrict** (lịch sử); `Message.ConversationId→Conversation` = **Cascade** (con của aggregate); `Message.Body` maxlen 2000; index `ix_message_conversation`. `DbSet<Conversation>/<Message>` thêm vào `AppDbContext`.
- **Cascade (`MessagingVisitEndHandler.cs`):** `IVisitEndHandler` STAGE-only, đóng conversation Open của visit (Status=Closed/ClosedAt/ClosedByUserId), idempotent (no-op nếu không còn Open) — auto-wire Scrutor giống Housekeeping. KHÔNG SaveChanges (caller commit chung transaction visit-status).
- **Trả nợ TK-050:** wire FK `InternalNote.ConversationId→Conversation` (Restrict, nullable) trong `InternalNoteConfiguration.cs`. NotesTests cập nhật: thêm `SeedConversationAsync` (seed session+visit+conversation thật) thay conversationId bịa → 2 test (`Create_with_conversation_only_persists`, `Query_filters_by_room_and_conversation`) pass dưới FK mới.
- **Test (`MessagingSchemaTests.cs`, 5 test):** TC-MSG-A1 hai conversation cùng visit → DbUpdateException (ux_conversation_visit); TC-MSG-A2 handler đóng conversation + gọi lại idempotent (không ghi đè ClosedAt/ClosedByUserId); TC-MSG-A3a xóa conversation cascade message; A3b message ConversationId lạ → FK; A3c conversation GuestVisitId lạ → FK. Tất cả pass trên SQLite bật FK.
- **Chưa làm (Slice B/C):** không API/hub/use case ở slice này (đúng phạm vi). Migration `AddMessaging` đã tạo trong session trước.
- Evidence/Refs: `Domain/Messaging/*`, `MessagingConfigurations.cs`, `AppDbContext.cs`, `InternalNoteConfiguration.cs`, `MessagingVisitEndHandler.cs`, `Notes/NotesTests.cs`, `Messaging/MessagingSchemaTests.cs`, migration `AddMessaging`; DEC-084; TK-050.
- Reversible?: có (module mới, chưa expose API; migration có thể revert).


### DEC-086: Messaging/Chat — Slice B-guest (use case gửi/xem tin + REST + test) → HOÀN TẤT
- Status: Accepted (2026-07-06) — build 0W/0E; **294 test pass** (unit 76 + arch 5 + integration 213 + 1 skip; +8 GuestMessaging). Chạy per-project (unit/arch/integration) đều xanh; subset `~Messaging` = 13 pass (5 schema A + 8 guest B). Doc: `resort-qr/docs/messaging-wave-design.md` §8 Slice B, §9 TC-MSG-B1..B5.
- **Bối cảnh:** code Slice B-guest (2 use case + contracts + validators + errors + endpoint) đã VIẾT ở session trước nhưng CHƯA có test/CHƯA chạy (rủi ro "xanh trên giấy"). Session này bổ sung test integration dùng SQLite thật + guard/gate THẬT (không mock) rồi verify build + toàn bộ test không regression.
- **Test (`tests/ResortQr.IntegrationTests/Messaging/GuestMessagingTests.cs`, 8 test):** dựng `SendGuestMessageUseCase`/`GetGuestConversationUseCase` với `PortalWindowGuard(uow, Clock, Sha256GuestSessionKeyHasher, [])` + `RuleGate(uow)` THẬT (mirror `GuestHousekeepingTests`); seed ResortSettings (`ChatEnabled`/`RequireRuleAckForChat`/`MaxMessageLength`), session (`SessionKeyHash = Hasher.Hash(rawKey)` để guard khớp), visit Active.
  - TC-MSG-B1: gửi 3 tin → 1 conversation, 3 message, `UnreadForStaff=3` (đơn điệu, P11).
  - TC-MSG-B2 (reopen Req 5.10): seed conversation **Closed** → guest gửi lại (visit Active) → reopen conversation CŨ (Open, ClosedAt/ClosedByUserId=null), KHÔNG tạo mới (conversation count=1).
  - TC-MSG-B3 (isolation P6/B9): lượt A có tin; lượt B (cùng phòng, **session riêng rawKey riêng**) GET → ConversationId=null + Messages rỗng.
  - TC-MSG-B4a: `RequireRuleAckForChat`+publication+chưa ack → 403 `rule_ack_required`. B4b: `ChatEnabled=false` → 403 `forbidden`.
  - TC-MSG-B5 (session_expired): đẩy Clock quá `PortalWindowMinutes`(30) nhưng trong `ExpiresAt`(24h) → nhánh (4) guard trả `session_expired` (check-before-update): `LastSeenAt` giữ nguyên, KHÔNG tạo conversation.
  - message_too_long: `MaxMessageLength=5`, body dài hơn → 400 `message_too_long`.
  - GET đánh dấu đã đọc: seed message Staff → guest GET → DTO `FromStaff=true` + `ReadByGuestAt` được set trong DB.
- **Verify cơ chế (đọc code, không đoán):** guard branch (3) `now>ExpiresAt` mới SaveChanges (expire+cascade); branch (4) cửa sổ đóng CHỈ trả lỗi, không ghi → dùng branch (4) cho B5 để chứng minh không đổi LastSeenAt. RuleGate fail-open khi chưa publish (DEC-064) → B4a phải seed `RulePublication{IsCurrent=true}` mới ra `rule_ack_required`.
- **Chưa làm (Slice B-admin — BƯỚC 2):** ListConversationsByRoom/GetConversationDetail/ReplyToConversation/MarkConversationRead/CloseConversation + `IConversationQueries` (TryAddScoped, DEC-049) + `AdminMessagingEndpoints` (RequireStaff) + OpenAPI enrich + test. Slice C (SignalR) sau đó.
- Evidence/Refs: `Application/Messaging/{SendGuestMessageUseCase,GetGuestConversationUseCase,GuestMessagingContracts,GuestMessagingValidators,MessagingErrors}.cs`, `Api/Endpoints/GuestAccessEndpoints.cs`, `Messaging/GuestMessagingTests.cs`; DEC-084/085; Req 5.9/5.10, P6/P11, B8.
- Reversible?: có (test bổ sung; code B-guest có thể chỉnh/revert độc lập).


### DEC-087: Messaging/Chat — Slice B-admin (use cases lễ tân + REST + read-model + OpenAPI + test) → HOÀN TẤT
- Status: Accepted (2026-07-06) — build 0W/0E; **307 test pass** (unit 76 + arch 5 + integration 226 + 1 skip; +12 AdminMessaging +1 OpenApi conversations). Subset `~Messaging` = 25 (5 schema A + 8 guest B + 12 admin B). Design-first: `messaging-wave-design.md` §11 (thiết kế chi tiết đã chốt) viết + tự-validate với code THẬT TRƯỚC khi code.
- **Phạm vi:** 3 use case (`ReplyToConversationUseCase`, `MarkConversationReadUseCase`, `CloseConversationUseCase`) + read-model `IConversationQueries`/`EfConversationQueries` (List/Detail) + contracts + validators + `AdminMessagingEndpoints` (`/api/admin/conversations`, RequireStaff) + wire `Program.cs`. **KHÔNG migration** (chỉ use case/REST/read; entity + index đã ở Slice A).
- **QUYẾT ĐỊNH AI TỰ RA (spec `design.md` im lặng — kiểm chứng: dòng 352 chỉ định reopen là hành động GUEST khi visit Active; dòng 160-161 chỉ liệt kê endpoint):**
  1. **Chính sách staff `reply` khi Closed = reopen NẾU visit Active; 409 `ConversationClosed` nếu visit đã kết thúc (Closed/Expired).** Bản chất: reopen chỉ có nghĩa khi guest CÒN nhận được (visit Active — nhất quán guard phía guest); reopen hội thoại của visit chết = Open-zombie guest không bao giờ đọc (session_expired) → ô nhiễm inbox. Vì staff KHÔNG đi qua `PortalWindowGuard`, phải kiểm `GuestVisit.Status==Active` tường minh. Khác gợi ý end.md ("reopen nếu Closed" chung chung) — chốt chặt hơn theo bản chất "guest có nhận được không". Staff muốn ghi khi visit đã kết thúc → dùng `InternalNote` (đúng công cụ, P7).
  2. **Admin reply/read/close KHÔNG gate `ChatEnabled`.** Bản chất: toggle `ChatEnabled` quản **truy cập của GUEST** (guest send/get bị chặn 403 khi tắt — DEC-086); staff là actor tin cậy quản DỮ LIỆU sẵn có (winding-down khi tắt tính năng vẫn cần trả lời/đóng hội thoại cũ). Gate admin theo toggle guest sẽ khóa nhầm nghiệp vụ vận hành.
  3. **`reply` KHÔNG tự mark-read và KHÔNG chạm `UnreadForStaff`.** Bản chất: `UnreadForStaff` là đếm guest→staff; reply là staff→guest. Luồng `design.md` dòng 348 tách "read" và "reply" thành 2 bước → giữ 2 use case orthogonal (single-responsibility), FE gọi read trước reply.
  4. **Inbox order theo `Id DESC` (UUIDv7 ≈ thời điểm tạo), KHÔNG `LastMessageAt DESC`** — do TK-047 (SQLite không ORDER BY DateTimeOffset). Đánh đổi recency có chủ đích — xem TRD-011 + TK-056.
- **Read-model (DEC-049):** `EfConversationQueries` inject base `ResortQrDbContext` (AppDbContext:ResortQrDbContext), `AsNoTracking`, RoomNumber qua correlated subquery (như `EfHousekeepingQueries`); wire `TryAddScoped` trong `ResortQrPersistenceExtensions` (impl ở namespace Persistence → scan tự loại — verify `DependencyInjectionExtensions.NotInNamespaceOf<ResortQrDbContext>`). Detail: message order `OrderBy(m=>m.Id)` — message append-only nên Id = thứ tự thời gian THẬT (không đánh đổi).
- **Test (`AdminMessagingTests.cs`, 12):** reply Open→append (LastStaffMessageAt/SenderUserId=staff, unread KHÔNG đổi); reply Closed+visit Active→reopen; reply Closed+visit Expired→409; reply 404; reply>MaxLen→400; markRead unread→0 + ReadByStaffAt cho tin guest (tin staff không đụng) + idempotent; markRead 404; close→Closed+ClosedByUserId=staff; close đã Closed→409; close 404; list filter room/status + RoomNumber; detail order theo Id (Id tất định `Guid.CreateVersion7(mốc)` — tránh flaky cùng-ms) + null khi id lạ. OpenApi: nhóm conversations có security Bearer + reply 404/409 + close 409 (verify document `/openapi/v1.json` THẬT).
- **CÒN LẠI:** Slice C (SignalR `ChatHub` + `IRealtimeNotifier` events post-commit + EndVisit evict) — BƯỚC 3. Notify realtime CHƯA gắn vào send/reply/read/close (sẽ thêm ở Slice C, post-commit).
- Evidence/Refs: `Application/Messaging/{ReplyToConversation,MarkConversationRead,CloseConversation}UseCase.cs`, `AdminMessagingContracts.cs`, `AdminMessagingValidators.cs`, `IConversationQueries.cs`, `Infrastructure/Persistence/EfConversationQueries.cs`, `ResortQrPersistenceExtensions.cs`, `Api/Endpoints/AdminMessagingEndpoints.cs`, `Program.cs`, `Messaging/AdminMessagingTests.cs`, `OpenApi/OpenApiEndpointTests.cs`; `messaging-wave-design.md` §11; DEC-049/084/085/086; `design.md` dòng 160-161/348-352.
- Reversible?: có (module mới; chính sách reopen/ordering chỉnh độc lập, không migration).


### DEC-088: Messaging/Chat — Slice C1 (port IRealtimeNotifier + notify POST-COMMIT, chưa có hub) → HOÀN TẤT
- Status: Accepted (2026-07-06) — build 0W/0E; **314 test pass** (unit 76 + arch 5 + integration 233 + 1 skip; +7: guest 2, admin 4, visitender 1). Design-first: `messaging-wave-design.md` §12 (chia C1/C2) viết + validate với code THẬT trước.
- **Bối cảnh (verify, không bịa):** grep xác nhận KHÔNG tồn tại `IRealtimeNotifier`/`ChatHub`/`IGuestContext`. Slice C to + có phần chưa kiểm chứng (test SignalR-over-TestServer) → **chia 2 increment**: C1 = port + wiring (verify Docker-free, KHÔNG transport); C2 = hub thật (TK-058).
- **Phạm vi C1:** port `ResortQr.Application.Realtime.IRealtimeNotifier` (`NotifyConversationAsync`/`NotifyStaffAsync`/`NotifyVisitEndedAsync`) + `RealtimeEvent(Name, Payload)` generic + `RealtimeEventNames` + payload records (Messaging) + `NullRealtimeNotifier` (no-op default). Gắn notify **POST-COMMIT** vào SendGuestMessage/ReplyToConversation/MarkConversationRead/CloseConversation/VisitEnder.
- **QUYẾT ĐỊNH AI TỰ RA + lý do bản chất:**
  1. **Notify SAU SaveChanges trong use case** (design §6 mandate; DEV-008): bắn event trước commit → rollback → client thấy tin "ma". Test chứng minh: thất bại (session_expired/409/message_too_long) → KHÔNG notify (spy `RecordingRealtimeNotifier` ghi 0 call).
  2. **Port generic `RealtimeEvent(string Name, object Payload)`** thay vì method-per-event: khớp adapter map §6 (`SendAsync(evt.Name, evt.Payload)`), giữ port realtime TRUNG LẬP (không coupled enum Messaging); payload records đặt ở `Application.Messaging` (module tự khai payload của mình — Housekeeping sau tự thêm). Tránh port phình theo từng module.
  3. **`NullRealtimeNotifier` mặc định + `TryAddSingleton` (KHÔNG marker auto-scan)**: chưa có hub thì realtime im lặng, polling fallback vẫn chạy (Req 17.7); DI luôn resolve được. KHÔNG mang `IScopedService`/`ISingletonService` để Scrutor không auto-scan → tránh 2 impl `IRealtimeNotifier` (Null + adapter C2) gây resolve mập mờ. C2 dùng `services.Replace(...)` để thay.
  4. **Ánh xạ event theo audience (giảm nhiễu):** MessageReceived→group hội thoại (staff đang mở + guest); ConversationUpdated→group staff (badge inbox). KHÔNG gửi MessageReceived tới cả staff-group (staff đang xem đã ở conversation-group; staff khác nhận ConversationUpdated) → tránh trùng. MarkRead: MessageRead chỉ bắn khi THỰC SỰ có tin được đánh dấu (không nhiễu khi gọi idempotent lần 2).
- **Churn (chấp nhận, như DEC-075):** thêm `IRealtimeNotifier` vào ctor 5 use case → cập nhật 3 helper test (`SendUc`/`ReplyUc`/`ReadUc`/`CloseUc`/`Ender`) nhận notifier optional (mặc định `NullRealtimeNotifier`). Test cũ không đổi hành vi.
- **DEFER sang C2 (ghi TK-058):** notify guest-read (`GetGuestConversation`); notify lazy-expiry qua `PortalWindowGuard`. **C2 còn lại:** hub + adapter + auth kép + JoinConversation authorize (tái dùng PortalWindowGuard) + EndVisit evict — verify harness SignalR-over-TestServer TRƯỚC.
- Evidence/Refs: `Application/Realtime/{IRealtimeNotifier,NullRealtimeNotifier}.cs`, `Application/Messaging/RealtimeNotifications.cs`, 5 use case (Send/Reply/MarkRead/Close/VisitEnder), `Infrastructure/DependencyInjection/DependencyInjectionExtensions.cs` (TryAddSingleton), `tests/.../Realtime/RecordingRealtimeNotifier.cs` + notify tests; `messaging-wave-design.md` §12; `21-realtime-signalr.md` §5/§6; DEC-026/031/084/085/086/087, DEV-008.
- Reversible?: có (port + no-op; C2 Replace bằng adapter — không migration, không đổi schema).


### DEC-089: Messaging/Chat — Slice C2 (SignalR hub transport: auth kép + authorize-on-join + evict + staff group) → HOÀN TẤT
- Status: Accepted (2026-07-06) — build 0W/0E; **324 test pass** (unit 76 + arch 5 + integration 243 + 1 skip; +10: harness 1, guest authorize/no-leak 2, evict 2, staff 2, HubAccessToken unit 3). Design-first: `messaging-wave-design.md` §12.2. **Harness verify TRƯỚC** (C2-0) rồi mới xây auth (đúng cam kết TK-058).
- **Cách làm từng-bước-chắc-chắn (verify từng lớp):** C2-0 chứng minh `HubConnection` nối được TestServer (LongPolling); C2-1 guest join authorize + adapter → guest nhận / B9 no-leak; C2-2 EndVisit evict (VisitEnded + rejoin denied); C2-3 staff group + JWT query + staff nhận / guest bị cô lập khỏi group staff.
- **QUYẾT ĐỊNH AI TỰ RA + lý do bản chất (kiểm chứng được):**
  1. **KHÔNG dựng `IGuestContext` — tái dùng `PortalWindowGuard.ValidateAsync(conv.GuestVisitId, cookie)` trong hub.** Bản chất: "guest có sở hữu hội thoại + còn hạn không" = ĐÚNG hợp đồng guard đã có (một nguồn luật, DEC-031). Tránh xây middleware IGuestContext (DEV-019 P0-1 chưa cần). Hub đọc cookie RAW từ `Context.GetHttpContext().Request.Cookies[GuestOptions.CookieName]`. Test B9 (guest lượt khác join → bị từ chối → không nhận) chứng minh chống nghe lén.
  2. **Staff group `resort-{resortId}-staff`: resortId lookup `AppUser.ResortId` theo `sub` claim lúc `OnConnectedAsync`** (JWT chỉ có sub/role/jti — verify JwtTokenService — KHÔNG mang resortId). Lý do KHÔNG thêm claim resortId vào JWT: sẽ đụng login/token + auth test; lookup 1 lần/connection (PK) rẻ + self-contained (chỉ hub). Đường mở: thêm claim nếu cần tối ưu.
  3. **JWT qua query `access_token` cho `/hubs`** (trình duyệt không set header Authorization trên WS handshake — `21` §2): thêm `OnMessageReceived` trong `ResortQrAuthExtensions` (resort-qr, KHÔNG đụng foundation). Tách logic đọc token ra `HubAccessToken.ReadFromHubRequest` static → **unit-test được** (path /hubs* mới đọc; endpoint khác bỏ qua) vì path WS thật khó test qua TestServer (LongPolling gửi token qua header).
  4. **Adapter `SignalRRealtimeNotifier` NUỐT lỗi transport (log Warning qua LoggerMessage)** — hợp đồng port: realtime lỗi KHÔNG được làm hỏng thao tác đã commit (use case gọi post-commit). Đăng ký `services.Replace(Singleton NullRealtimeNotifier → SignalRRealtimeNotifier)` (test C2-1/2/3 nhận event thật ⇒ chứng minh Replace hiệu lực, không còn Null).
  5. **SignalR JSON protocol enum→string** (`AddJsonProtocol` + `JsonStringEnumConverter`) đồng nhất REST (DEC enum-as-string) — hợp đồng FE nhất quán trên cả REST lẫn realtime.
  6. **Staff MVP single-resort join conv bất kỳ** (không check `conv.ResortId==user.ResortId` như `21` §4): DEC-028 instance-per-resort → 1 resort. Ghi để multi-resort tương lai thêm check.
- **RỦI RO đã xử lý (không bịa):** race `StartAsync` vs `OnConnectedAsync` (staff group join có DB lookup, chưa xong khi push) → **fence bằng await 1 hub invocation** (SignalR xử lý client-invocation SAU khi OnConnectedAsync hoàn tất — guarantee thực của HubConnectionHandler). Verify: staff test xanh ổn định 2 lần chạy.
- **access_token trong log:** VERIFY — request-logging dùng `{RequestPathMasked}`=`PathMasker.Mask(Request.Path.Value)` (chỉ PATH, KHÔNG query) → token KHÔNG bị log ở tầng app. Residual: proxy production nên tránh log query cho `/hubs` (TK-060).
- **DEFER (ghi TK-058):** notify guest-read (`GetGuestConversation`); notify lazy-expiry qua `PortalWindowGuard` (guest kích hoạt vốn đã bị reject); backplane scale-out đa-instance (TK-031, MVP single-instance).
- Evidence/Refs: `Api/Realtime/{ChatHub,SignalRRealtimeNotifier,HubAccessToken}.cs`, `Api/Security/ResortQrAuthExtensions.cs` (OnMessageReceived), `Api/ResortQrApiExtensions.cs` (AddSignalR+Replace), `Program.cs` (MapHub), `Application/Realtime/*`, tests `Realtime/{ChatHubHarness,ChatHubAuthorize,ChatHubStaff,HubAccessToken}Tests.cs`; `21-realtime-signalr.md`; `messaging-wave-design.md` §12; DEC-026/031/088, DEV-008/019.
- Reversible?: có (hub + adapter tách; port giữ nguyên; không migration/schema).


### DEC-090: Housekeeping — Wave D realtime (`HousekeepingUpdated` → board staff) → HOÀN TẤT
- Status: Accepted (2026-07-06) — build 0W/0E; **328 test pass** (unit 76 + arch 5 + integration 247 + 1 skip; +4: guest create-notify 1, staff change/complete/failure 3). Design-first: `housekeeping-wave-design.md` §4. Tái dùng hạ tầng realtime Messaging C (DEC-088/089) — KHÔNG đụng hub/adapter/schema.
- **Phạm vi:** gắn notify `HousekeepingUpdated` (→ `NotifyStaffAsync(resortId, …)`, group `resort-{id}-staff`) POST-COMMIT vào use case đổi ticket. Payload `HousekeepingUpdatedPayload(TicketId, RoomId, Status)` (Application.Housekeeping — module tự khai payload, port trung lập).
- **QUYẾT ĐỊNH AI TỰ RA + lý do bản chất:**
  1. **Chỉ notify khi CÓ THAY ĐỔI THẬT** (nguyên tắc no-noise, nhất quán Messaging): Create (guest + staff) notify CHỈ khi `AlreadyOpen=false` (tạo mới); idempotent trả ticket đang mở → KHÔNG notify. ChangeStatus notify sau commit (transition đã validate). Complete notify trong helper. Thất bại (feature disabled/rule gate/not_found/no_open_ticket/409) → KHÔNG notify (chỉ sau commit thành công). Test chứng minh cả nhánh có/không notify.
  2. **Notify trong `HousekeepingCompletion` helper** (một chỗ) thay vì lặp ở 2 use case complete-by-room/token — helper có `open` ticket (ticketId/roomId/resortId/Status=Done). Tránh nhân bản, đảm bảo post-commit đồng nhất.
  3. **Event chỉ tới GROUP STAFF** (board đồng bộ), guest KHÔNG nhận (khách xem ticket mình qua polling `GET /api/guest/housekeeping`) — đúng §5.
- **DEFER (ghi TK-058):** cascade-cancel ticket khi EndVisit (`HousekeepingVisitEndHandler` STAGE-only) → notify `HousekeepingUpdated(Cancelled)` cần handler báo ngược ticket đã hủy về `VisitEnder` (nơi post-commit) — phức tạp hơn giá trị (visit-end không hot-path board; VisitEnder đã notify VisitEnded). Board hơi cũ tới refresh/poll — chấp nhận MVP.
- **Churn (như DEC-075/088):** +IRealtimeNotifier vào ctor 5 use case (Create guest/staff, ChangeStatus, Complete by-room/by-token) + param helper. Cập nhật 5 helper test (mặc định Null).
- Evidence/Refs: `Application/Housekeeping/{HousekeepingRealtimeNotifications,HousekeepingCompletion,ChangeHousekeepingStatusUseCase,CreateHousekeepingTicketUseCase,StaffCreateHousekeepingTicketUseCase,CompleteHousekeeping*}.cs`, tests `Housekeeping/{GuestHousekeepingTests,HousekeepingStaffTests}.cs`; `housekeeping-wave-design.md` §4; `21` §5/§6; DEC-088/089.
- Reversible?: có (chỉ thêm notify; không migration/schema).


### DEC-091: Realtime defer-a (guest-read receipt) XONG; defer-b (cascade-cancel notify) GIỮ DEFER có phân tích
- Status: Accepted (2026-07-06) — build 0W/0E; **329 test pass** (unit 76 + arch 5 + integration 248 + 1 skip; +1 guest-read notify).
- **(a) — LÀM:** `GetGuestConversationUseCase` khi đánh dấu tin lễ tân là khách đã đọc (`ReadByGuestAt`) → notify `MessageRead(readBy=guest)` POST-COMMIT tới group hội thoại. **CHỈ khi thực sự có tin được đánh dấu** (poll không có gì mới → KHÔNG notify) — no-noise, đối xứng `MarkConversationRead(readBy=staff)`. Read-receipt để lễ tân đang mở hội thoại thấy khách đã đọc. Churn: +IRealtimeNotifier vào ctor + helper test `GetUc` (default Null). Test: đánh dấu → 1 MessageRead; poll lần 2 (không tin mới) → không notify.
- **(b) — GIỮ DEFER (quyết định kiến trúc có chủ đích, KHÔNG chắp vá):** cascade-cancel ticket / close conversation khi EndVisit chưa notify `HousekeepingUpdated(Cancelled)` / `ConversationUpdated(Closed)` tới board staff.
  - **Bản chất (verify code):** `IVisitEndHandler.StageOnVisitEndedAsync` là **STAGE-only, trả `Task` void, chạy TRƯỚC SaveChanges của caller** (`VisitEnder`/`PortalWindowGuard`/`ResolveTokenUseCase`). Notify post-commit (design §6) đòi biết "cái gì đã đổi" SAU commit. Handler biết (ticket/conversation của visit) nhưng chạy trước commit → không thể notify inline. Caller (`VisitEnder` ở GuestAccess) KHÔNG được query `HousekeepingTicket`/`Conversation` (vi phạm ranh giới module — chính lý do có pattern handler, master §2.3).
  - **Mọi phương án sạch đều tốn kém:** (i) đổi contract `IVisitEndHandler` để **trả về danh sách sự kiện realtime** (target+payload) rồi caller flush post-commit → đụng port cross-module + cả 2 handler + 3 caller; (ii) thêm **event-collector/outbox-lite** scoped (handler ghi, caller flush sau commit) → abstraction MỚI, tạo **pattern notify thứ 2** song song inline (design §6 chốt inline) — lệch kiến trúc đã thống nhất.
  - **Lý do defer:** lợi ích = board staff tươi hơn ở đường **visit-end** (sweeper nền / staff đóng thủ công — KHÔNG hot-path; board refresh/poll là thấy ticket biến mất khỏi filter "mở"). Chi phí = đổi contract cross-module HOẶC thêm cơ chế + pattern thứ 2. **Không tương xứng** → fix tận gốc = chờ khi realtime-on-visit-end thành yêu cầu thật rồi quyết cơ chế (contract-return vs outbox) như một DEC kiến trúc riêng. Chắp vá (VisitEnder query chéo module) là fix ngọn → TỪ CHỐI.
- Evidence/Refs: `Application/Messaging/GetGuestConversationUseCase.cs`, `GuestAccess/IVisitEndHandler.cs` + `VisitEnder.cs`, `Housekeeping/HousekeepingVisitEndHandler.cs`, `Messaging/MessagingVisitEndHandler.cs`; `21` §6; DEC-088/089/090; master §2.3.
- Reversible?: (a) có (chỉ thêm notify). (b) N/A (chưa làm).


### DEC-092: Frontend foundation — Vuexy CHỈ làm tham chiếu thị giác; dựng LẠI sạch trên stack hiện đại (không port); monorepo tại `resort-qr/frontend/`
- Status: Accepted (2026-07-06) — quyết định nền FE trước khi scaffold. Chưa build code FE ở entry này (kế tiếp mới scaffold + verify).
- **Verify reference (đọc code THẬT, không đoán):** `Reference/EPS.Vuexy/package.json` name = `vuexy-vuejs-react-html-laravel-admin-dashboard-template` v6.4.0 → **Vuexy của Pixinvent** (template admin THƯƠNG MẠI, ThemeForest). Stack: Vue **2.6.11 (EOL)**, Vue CLI 4.5/webpack, Vuex 3.6, Vue Router 3.4, Bootstrap-Vue 2.21 + Bootstrap 4.6 (đều EOL), JS (jsconfig, không tsconfig), `public/jquery-3.2.1.min.js`, `@vue/composition-api` shim, CASL. Bản "EPS" chế thêm grab-bag (leaflet/echarts/konva/fullcalendar/quill/hls.js/jsmpeg/ffmpeg-static — app giám sát/media). Key trùng `file-saver`/`uuid` (đúng FE-E14). `themeConfig.js`: layout vertical, skin light, navbar floating, footer static — đây là "base look".
- **QUYẾT ĐỊNH (bản chất + an toàn thương mại):** KHÔNG port code Vuexy. Dùng Vuexy làm **tham chiếu thị giác/layout**; dựng LẠI trên stack đã chốt (Vue 3.5 + TS strict + Vite + Pinia + Vue Router 4 + Element Plus admin / tối giản guest — DEC-012/013/015/029).
  - Lý do 1 (EOL/kỹ thuật): Vue 2 + Vue CLI/webpack + Bootstrap-Vue + jQuery đều dead-end → port = nợ kỹ thuật ngay từ nền, đi ngược "lâu dài/thương mại".
  - Lý do 2 (**bản quyền/an toàn**): Vuexy là template thương mại có license (Pixinvent/ThemeForest). Copy source/asset của nó vào sản phẩm StarHill có rủi ro pháp lý. Dựng lại bằng thành phần open-source (Element Plus MIT, Vue MIT) tránh rủi ro.
  - Lý do 3 (bloat): grab-bag khổng lồ (media/surveillance) không liên quan resort portal → chỉ mượn Ý TƯỞNG layout, bỏ phần thừa ("lược bỏ về starter-kit").
- **Vị trí monorepo = `resort-qr/frontend/`** (theo tên `/frontend` ở design `05`). GIỮ `resort-qr/web/` (prototype static guest/admin hiện có) làm **tham chiếu UX** (không xóa — guest/index.html khá chỉn chu). Không đụng.
- **Tooling (verify):** máy KHÔNG có Node trên PATH; nvm-windows có **v18.20.8 (EOL)** + **v25.2.1**. Dùng **Node 25.2.1** (prepend `C:\Users\toann\AppData\Local\nvm\v25.2.1` vào PATH; npm 11.6.2; registry npmjs PONG OK). Node 25 là Current (không LTS) — CHỈ là toolchain build (deploy phục vụ static, không chạy Node) → chấp nhận cho dev; production pin Node LTS theo DEC-015. Ghi TK-061.
- **Scope kế tiếp (incremental, verify từng bước):** FE-1 monorepo skeleton (`pnpm-workspace`, tsconfig.base strict, .nvmrc) + admin-web app tối thiểu build được (Vite+Vue3+TS) + layout Vuexy-inspired (sidebar+navbar+content, Element Plus); FE-2 packages (shared-types sinh từ OpenAPI + api-client + realtime + ui-kit); FE-3 guest-web skeleton; sau đó lát cắt màn nghiệp vụ. Theo design `02`/`05` + DoD §8.
- Evidence/Refs: `Reference/EPS.Vuexy/{package.json,themeConfig.js,src/*}`, `design/frontend/02-architecture.md` + `05-monorepo-skeleton.md`, DEC-012/013/015/029; TK-061.
- Reversible?: có (chưa viết code; hướng dựng-lại có thể điều chỉnh).
