# Design — D-Rules.4c(api): Rules.Api endpoints + Host wiring + RequirePort (đóng QR-AD-030/031)

> Chi tiết hóa `design-modules/04-rules.md` §9/§10/§12 cho lát cắt cuối của D-Rules.4. Mọi khẳng định đã kiểm code
> thật (đường dẫn ghi kèm). Slice này flip **QR-AD-030** (snapshot+ack+gate) và **QR-AD-031** (sanitize+RequirePort)
> từ Proposed → **Implemented + Guard-Tests (INV-6)**.

## 0. Ground-truth đã verify
- **Host CHƯA wire Rules** (`starhill/src/Host/StarHill.Api/Program.cs`): không `AddRulesInfrastructure`/`AddRulesApi`,
  không connection string `Rules`, không migrate Rules. Rules mới chạy trong test tự-dựng ServiceProvider. ⇒ slice này
  wire Rules vào Host lần đầu.
- **Rules Draft use case phụ thuộc `IHtmlSanitizer`** (`UpsertRuleSectionTranslationUseCase`). Host CHƯA gọi
  `AddStarHillHtml` ⇒ nếu wire Rules mà không cấp sanitizer → DI `ValidateOnBuild` FAIL. ⇒ slice phải `AddStarHillHtml`
  + `RequirePort<IHtmlSanitizer>()` (boot fail-fast tường minh — QR-AD-031).
- **RequirePort API** (verify `BedrockSecurityExtensions.cs`/`BedrockRegistrationExtensions.cs`): `services.RequirePort<TPort>()`
  (= `services.BedrockStartupValidation().RequirePort(typeof(TPort))`). `RequiredPortsValidator` chặn boot nếu thiếu.
- **Use case Rules đã có + đăng ký** (Rules.Infrastructure): Create/Update/Delete section, Upsert translation, Publish,
  GetCurrentRules (guest read), Acknowledge (4b), IRuleGate (4c-gate). Endpoint chỉ là seam HTTP (F14).
- **Guest endpoint pattern** (`GuestAccessEndpointModule`, verify): `MapVersionedGroup("/guest",V1)` + `AllowAnonymous`
  + đọc cookie `http.Request.Cookies[name]` + `Cache-Control: no-store` + `Set-Cookie __Host-` chỉ khi phát session mới
  + `ProblemDetailsBuilder`. `ICurrentGuestContextResolver.ResolveAsync(sessionKey, roomId)` (C-GA.4) + `TouchAsync` sau thành công.
- **Admin endpoint pattern** (`RoomsEndpointModule`, verify): `MapVersionedGroup` + `RequireAuthorization(StarHillPolicies.*)`
  + map Result→HTTP. `StarHillPolicies.RequireStaff`/`RequireAdmin`.

## 1. Quyết định (kèm lý do)

### D-A Role endpoint admin (Req 8/11.3)
- Soạn/sửa/xoá Draft section + translation + **preview** + **publish** + **history**: `RequireStaff` (Req 8: "lễ tân/admin"
  soạn nội quy; Publish là hành vi nội dung, không phải cấu hình hệ thống). Admin superset qua RequireStaff. Lý do: Req
  11.3 xếp nội quy/FAQ vào quyền Staff (khác phòng/QR/settings = Admin). ⇒ mọi endpoint Rules admin = **RequireStaff**.

### D-B Guest endpoint (Req 1/3/11.2 — AllowAnonymous, cookie thiết bị)
- `GET /v1/guest/rules?roomId={guid}&lang={code}` + `POST /v1/guest/rules/acknowledge` body `{ roomId }`.
- **roomId BẮT BUỘC là tham số client** (không suy từ cookie): cookie GuestSession định danh THIẾT BỊ (toàn deployment,
  không gắn phòng — design GuestAccess §3). Một thiết bị có thể có visit ở nhiều phòng ⇒ phải biết đang xem phòng nào.
  Client lấy roomId từ response `resolve` trước đó.
- Luồng guest (mirror check-before-touch — QR-AD-032):
  1. Đọc cookie `__Host-starhill_guest` (dùng CÙNG `GuestAccessOptions.CookieName` — không hardcode).
  2. `ICurrentGuestContextResolver.ResolveAsync(sessionKey, roomId)` → context (GuestVisitId, ResortId, ...) hoặc
     Failure(session_expired/guest_context_missing/configuration_unavailable) → map ProblemDetails, DỪNG.
  3. **GET rules**: `GetCurrentRulesUseCase(context.ResortId, lang)`. Đọc thuần → KHÔNG touch (đọc nội quy nhiều lần
     không nên tự gia hạn cửa sổ; touch chỉ cho hành vi nghiệp vụ thực — giữ đúng semantics; nội quy là đọc tham khảo).
     Cân nhắc: có touch khi đọc rules không? → KHÔNG (đọc rules là thao tác phụ trợ; portal-window đại diện "đang thao
     tác" — đọc-lại-nội-quy không phải thao tác nghiệp vụ). Xem Open-Q1.
  4. **POST acknowledge**: `AcknowledgeRulesUseCase(context.ResortId, context.RoomId, context.GuestSessionId,
     context.GuestVisitId, lang)` → nếu Success → `ICurrentGuestContextResolver.TouchAsync(context.GuestVisitId)` (ack
     LÀ hành vi nghiệp vụ hợp lệ → trượt cửa sổ sau thành công). Map Result→HTTP. `no-store`.
- Endpoint guest `AllowAnonymous`; rate-limit biên = Bedrock global IP limiter (đã áp #9 UseBedrockApi — QR-TO-007).

### D-C Host wiring (thứ tự + RequirePort)
- Thêm connection string `Rules` (cùng DB `starhill`, schema `rules`) + `AddRulesInfrastructure(UseNpgsql(cs, MigrationsHistoryTable("__EFMigrationsHistory","rules")))` (QR-AD-028) + `AddRulesApi()` + migrate gated.
- `services.AddStarHillHtml()` (adapter Ganss) + `services.RequirePort<IHtmlSanitizer>()` — đặt ở Host SAU AddBedrockStartupValidation, TRƯỚC Build. Lý do RequirePort: sanitize là port bảo mật KHÔNG default (thiếu = XSS lọt) → boot fail-fast tường minh thay vì lỗi runtime khi admin nhập nội dung.
- appsettings.json + docker-compose override `ConnectionStrings__Rules` + `.github/workflows/starhill-ci.yml` bundle `rules`.

### D-D DTO Api tách khỏi Application (mirror Rooms/GuestAccess)
- Request/response records ở Rules.Api; guest ack response `{ rulePublicationId, version, alreadyAcknowledged }`;
  guest rules response = sections đã render (Key/SortOrder/cờ đọc/Title/Body/ResolvedLanguage/IsFallback/IsMissing) +
  publicationId + version + language. Admin: section CRUD trả id; publish trả `{ publicationId, version }`.

## 2. Cấu trúc code (MỚI)
```
starhill/src/Modules/Rules/Rules.Api/
  Rules.Api.csproj            (ref: Rules.Application + Rules.Contracts + GuestAccess.Contracts + ResortConfig.Contracts?
                               + StarHill.Authorization + Bedrock.Api)  # GuestAccess.Contracts cho ICurrentGuestContextResolver
  RulesAdminEndpointModule.cs (IEndpointModule — /v1/rules/* RequireStaff)
  RulesGuestEndpointModule.cs (IEndpointModule — /v1/guest/rules[/acknowledge] AllowAnonymous)
  DependencyInjection/RulesApiExtensions.cs  (AddRulesApi → 2 IEndpointModule singleton)
```
- Rules.Api ref `GuestAccess.Contracts` (dùng `ICurrentGuestContextResolver`) — hợp lệ (cross-module qua Contracts,
  QR-AD-024). KHÔNG ref Infrastructure module nào (I7).

## 3. Verify plan (tối đa KHÔNG Docker)
- **Admin auth guard** `RulesAdminEndpointAuthTests` (TestServer + fake use cases): Staff→2xx; no-token→401; (Admin cũng
  qua — superset). Route thật `/v1/rules/...`.
- **Guest endpoint** `RulesGuestEndpointTests` (TestServer + fake `ICurrentGuestContextResolver` + fake use cases):
  GET rules context OK→200; POST ack OK→200 + `TouchAsync` được gọi (fake ghi nhận); resolver Failure(session_expired)→
  map ProblemDetails đúng; no cookie→guest_context_missing; `no-store` header.
- **Host boot** `StarHill.Api.Tests` (WebApplicationFactory<Program>, ValidateOnBuild): wire Rules + RequirePort không phá
  boot (IHtmlSanitizer có mặt qua AddStarHillHtml). Thêm smoke: thiếu sanitizer → boot fail (guard RequirePort) — nếu khả thi test riêng.
- **Publish/ack/gate** đã có guard (D-Rules.3a/4a/4b/4c-gate). Postgres bundle/migration → CI.
- `vp all` + `vp journal` INV-1..6. Máy phiên này KHÔNG Docker → integration Postgres skip mềm; endpoint/auth verify qua TestServer.

## 4. Sub-slice (từng bước chắc chắn — mỗi cái build+test+commit)
1. **4c-api-1 (Host wiring + RequirePort + admin endpoints):** Rules.Api project + `RulesAdminEndpointModule` (sections
   CRUD + translations upsert + publish + preview + history) + `AddRulesApi` + Host wire (infra+api+AddStarHillHtml+
   RequirePort+conn string+migrate) + compose + CI bundle + `RulesAdminEndpointAuthTests`. Cổng: `vp all` xanh + Host boot.
2. **4c-api-2 (guest endpoints):** `RulesGuestEndpointModule` (GET rules + POST acknowledge, resolver+touch) +
   `RulesGuestEndpointTests`. Cổng: `vp all` xanh. → flip **QR-AD-030/031 Implemented + Guard-Tests** (INV-6).
3. **D-Rules.3b (preview/history use cases)** nếu chưa đủ cho admin preview/history endpoint — kiểm code thật trước
   (có thể GetDraftPreview/GetPublicationHistory chưa tồn tại → thêm read-model, hoặc defer endpoint history).

## 5. Open questions (chốt khi implement, không chặn design)
- **Q1:** GET /v1/guest/rules có touch cửa sổ không? Khuyến nghị KHÔNG (đọc nội quy = phụ trợ, không phải thao tác
  nghiệp vụ; ack MỚI touch). Nếu sản phẩm muốn "đang đọc = còn hoạt động" thì touch — nhưng dễ khiến cửa sổ không bao
  giờ hết khi để mở tab. Giữ KHÔNG touch (an toàn, đúng check-before-touch).
- **Q2:** Admin preview/history — 3b use case (`GetDraftPreview`/`GetPublicationHistory`) đã tồn tại chưa? Verify code
  thật ở đầu 4c-api-1; nếu chưa, đưa vào slice này hay defer endpoint (giữ CRUD+publish trước).

## 6. Anti-drift
- QR-AD-030/031 chỉ chuyển Implemented KHI 4c-api-2 xong + Guard-Tests khai đủ (INV-6 cưỡng chế). Ghi QR-N cho mỗi sub-slice.
- RequirePort(IHtmlSanitizer) = guard boot; thêm test boot-fail-khi-thiếu-sanitizer nếu khả thi (biến claim thành test).
