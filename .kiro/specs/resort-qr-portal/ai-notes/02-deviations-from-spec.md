# 02 — Chỗ AI phải ĐỔI so với yêu cầu/docs ban đầu

> Những điểm base **khác** với `Reference/Backend` hoặc bổ sung ngoài `docs/resort-qr-portal/*`. Mỗi deviation nêu "bản chất vấn đề" (root cause) trước "thay đổi" — sửa gốc, không sửa ngọn.

### DEV-001: Repository KHÔNG còn tự `SaveChanges`
- Status: Accepted
- Date: 2026-07-03
- Root cause: `BaseRepository` gọi `SaveChangesAsync()` trong mỗi Add/Update/Delete (E1) ⇒ mất tính nguyên tử liên bảng (ví dụ publish nội quy đụng nhiều bảng), `IUnitOfWork` vô nghĩa (E3).
- Change: Repository chỉ thao tác ChangeTracker; điểm ghi DB duy nhất là `IUnitOfWork.SaveChangesAsync`; transaction tường minh `ExecuteInTransactionAsync`.
- Evidence/Refs: `../design/backend/09-reference-reconciliation.md` E1/E3; `../design/backend/02` §3.
- Impact: Mọi use case ghi dữ liệu. Đây là thay đổi nền quan trọng nhất.
- Reversible?: Không nên đảo (đảo lại là tái tạo bug).

### DEV-002: Bổ sung `CancellationToken` toàn tuyến
- Status: Accepted
- Date: 2026-07-03
- Root cause: Reference không có `CancellationToken` (E2) ⇒ không hủy được request dài, tốn tài nguyên.
- Change: Mọi method I/O nhận và truyền `CancellationToken` tới repo/EF.
- Evidence/Refs: E2; `../design/backend/06-conventions.md`.
- Impact: Chữ ký toàn bộ port + use case + controller.
- Reversible?: Không nên.

### DEV-003: Wire đầy đủ Authentication (JWT + guest cookie)
- Status: Accepted
- Date: 2026-07-03
- Root cause: `Program.cs` có `UseAuthorization` nhưng thiếu `UseAuthentication` + không đăng ký scheme (E5) ⇒ phân quyền không có danh tính.
- Change: Đăng ký 2 scheme (JWT Bearer cho admin, cookie guest), gọi `UseAuthentication` trước `UseAuthorization`, thêm policy `RequireAdmin/RequireStaff`.
- Evidence/Refs: E5; `../design/backend/03` §4.
- Impact: Bảo mật toàn hệ thống.
- Reversible?: Không nên.
- ⚠️ Ràng buộc: cần đọc `FresherDev.HMS.Auth` để tái dùng nếu đã có JWT — xem TK-001.

### DEV-004: Nâng target net8.0 → net10.0 LTS
- Status: Accepted
- Date: 2026-07-03
- Root cause: Reference dùng net8.0 (E7); docs đã chốt .NET 10 LTS.
- Change: `Directory.Build.props` đặt `net10.0`.
- Evidence/Refs: E7; docs README "Ngăn xếp công nghệ".
- Impact: Toàn solution + package versions.
- Reversible?: Có nhưng đi ngược quyết định đã chốt.

### DEV-005: Bỏ `ForceLoadAssembly` + `// TODO: Remove` UoW
- Status: Accepted
- Date: 2026-07-03
- Root cause: `ForceLoadAssemblies()` thủ công 8 assembly (E6) + đăng ký UoW kèm TODO (E8) ⇒ mùi code, dễ quên khi thêm project.
- Change: Đăng ký assembly qua `AssemblyMarker` tường minh + Scrutor; UoW đăng ký chuẩn không TODO.
- Evidence/Refs: E6, E8; `../design/backend/01` §5.
- Impact: Composition root.
- Reversible?: Có.

### DEV-006: Migration incremental per-wave (ĐÃ CHỐT — thay cho "schema đầy đủ ngay")
- Status: ✅ Resolved (2026-07-03) → incremental
- Root cause (đã xét lại): nỗi lo "migration giữa chừng vỡ dữ liệu" là fix ngọn. Fix gốc = quy trình migration additive per-wave có review; EF migrations vốn an toàn khi làm đúng.
- Change: Migration nền chỉ chứa entity nền + index của chúng; mỗi module nghiệp vụ có migration riêng khi hiện thực. Không dựng bảng "chết".
- Evidence/Refs: `../design/backend/04` §4–5; requirements Req 8.6–8.7, 18.1–18.2; TRD-002, DEC-010.
- Impact: chuỗi migration + extension point.
- Reversible?: Có.

### DEV-007: Read model (CQRS-lite) cho truy vấn phức tạp
- Status: Accepted (2026-07-03, từ audit kiến trúc)
- Root cause: `IRepository.Query()` trả `IQueryable` là leaky và không tối ưu cho dashboard/inbox.
- Change: thêm read query service (`IInboxQueries`/`IDashboardQueries`...) trả DTO read-model, EF projection ở Infrastructure. Repo generic chỉ cho ghi + đọc đơn giản.
- Refs: `../design/backend/11-architecture-review-and-gaps.md` GAP-3; `02` §6.1.

### DEV-008: Realtime notify chỉ sau commit
- Status: Accepted (2026-07-03)
- Root cause: gọi realtime trong transaction → rủi ro thông báo rồi rollback.
- Change: `IRealtimeNotifier` gọi post-commit; Outbox để mở cho tương lai.
- Refs: GAP-4; `02` §6.2, `06`.

### DEV-009: Thay AutoMapper → Mapperly (license)
- Status: Accepted (2026-07-03)
- Root cause: AutoMapper chuyển thương mại (RPL/commercial) 7/2025.
- Change: dùng Mapperly (Apache-2.0 miễn phí, source-generator, đã verify file LICENSE) hoặc mapping thủ công.
- Refs: GAP-5; `../design/technology-stack.md` (audit license).

### DEV-010: `RoomQrToken.Token` UNIQUE toàn cục (lỗi thiết kế tìm được khi audit data model)
- Status: Accepted (2026-07-03)
- Root cause: partial unique `(room_id) WHERE status='Active'` chỉ đảm bảo 1 token Active/phòng, KHÔNG chặn hai phòng trùng chuỗi token → resolve `/r/{token}` có thể nhập nhằng (lỗi đúng đắn, không phải hiệu năng).
- Change: thêm `UNIQUE(token)` toàn cục (mọi Status). Cập nhật `04` §3 + §7.1, Property B3, requirements Req 8.
- Refs: `../design/backend/04` §7.1; `08` B3.

### DEV-011: Làm rõ phạm vi concurrency token + delete behavior + index nóng
- Status: Accepted (2026-07-03)
- Root cause: data model chưa nói entity nào có `xmin` token → nguy cơ gắn vào `GuestVisit` (LastSeenAt đổi mỗi request) gây 409 giả; chưa đặc tả delete behavior (nguy cơ cascade mất lịch sử); thiếu index hot-path.
- Change: `04` §7 (unique/index/delete/CHECK) + §8 (bảng entity có/không concurrency) + §9 (kiểu dữ liệu).
- Refs: `../design/backend/04` §7–9.

### DEV-012: RefreshToken schema mở rộng (rotation + reuse detection)
- Status: Accepted (2026-07-03)
- Root cause: refresh token dài hạn là mục tiêu trộm; rotation + reuse detection cần dữ liệu family/replaced.
- Change: `RefreshToken` thêm `FamilyId`, `ReplacedByTokenId?`, `RevokedReason?` (so với `04` §3 ban đầu). Lưu hash, không plaintext.
- Refs: `../design/backend/12-identity-and-auth.md` §3; `04` §3.

### DEV-013: `ux_visit_active` — đúng 1 GuestVisit Active/(session,room) (lỗi race tìm được khi audit)
- Status: Accepted (2026-07-03)
- Root cause: logic "tìm visit Active hoặc tạo" không có ràng buộc DB → 2 resolve đồng thời tạo 2 visit Active cho cùng (session, room), vi phạm Req 10.2/10.4, phân mảnh hội thoại/ack.
- Change: partial unique `guest_visit(guest_session_id, room_id) WHERE status='Active'`; use case xử lý unique-violation → re-query & reuse. Index này thay `ix_visit_lookup` (trùng chức năng).
- Refs: `../design/backend/04` §7.1–7.2; `08` B3; Req 10.2/10.4.

### DEV-014: Lazy idle-expiry ở resolve/tương tác (không chỉ dựa sweeper)
- Status: Accepted (2026-07-03)
- Root cause: docs/design chỉ có background sweeper (Req 10.7) chuyển visit quá hạn → Expired. Nếu resolve tới sau ExpiresAt nhưng trước lần quét sweeper, logic "Status=Active → nối lại" sẽ nối nhầm visit đã idle > 24h → khách mới thấy dữ liệu lượt cũ (rò rỉ, vi phạm Req 10.5/10.8).
- Change: resolve và EnforcePortalWindow tự kiểm tra `now > ExpiresAt` (lazy-expire → EndVisit + tạo visit mới); sweeper chỉ là lưới an toàn/dọn cascade. EndVisit idempotent để 3 nguồn gọi (staff/lazy/sweeper) không xung đột.
- Refs: `../design/backend/13-guest-access-flows.md` §2, §3, §5; Req 10.5/10.7/10.8.

### DEV-015: Unique số phòng trong resort (partial WHERE is_deleted=false)
- Status: Accepted (2026-07-03)
- Root cause: Req 16.6 cấm trùng số phòng nhưng data model chưa có ràng buộc DB → dựa mỗi validation app (có thể bị bỏ qua/bug/race).
- Change: `CREATE UNIQUE INDEX ux_room_number ON room(resort_id, room_number) WHERE is_deleted=false` — cho phép tái dùng số phòng sau soft-delete, cấm hai phòng đang sống trùng số.
- Refs: `../design/backend/16-rooms-and-qr.md` §2; Req 16.6.

### DEV-016: Sửa 2 chỗ drift trong master design.md (consistency audit)
- Status: Accepted (2026-07-03)
- Root cause: master `design.md` (overview do subagent tạo ban đầu) còn 2 snippet cũ đối lập quyết định đã chốt: (C1) `IsRowVersion()` thay vì `UseXminAsConcurrencyToken()`; (C2) "schema đầy đủ ngay" thay vì migration incremental.
- Change: sửa master §4.3 → `UseXminAsConcurrencyToken()`; §5.4 → incremental + trỏ `04`. Chi tiết audit ở `../design/backend/22-consistency-audit.md`.
- Refs: `22` §1; `03`§3; `04`§4.
- **Lượt audit 2 (re-read 04+12):** thêm C3 (`04`§3 SessionKey → `(UNIQUE, indexed)` khớp §7.1), C4 (`12`§3 reword vì `04`§3 đã có FamilyId). Đã sửa. Refs `22`§1.

### DEV-017: Audit 3 (re-read 13/03/21) — sửa C5 (concurrency handling), C6 (ToResult compile)
- Status: Accepted (2026-07-03)
- C5 (quan trọng): `05`§2 UoW.SaveChanges "return error(concurrency_conflict)" mâu thuẫn chữ ký `Task<int>` (`02`§3). Fix gốc: exception `DbUpdateConcurrencyException` **propagate** lên ProblemDetails middleware; `03`§1 thêm `MapException` tường minh (DbUpdateConcurrencyException→409 concurrency_conflict, default→500). Một chỗ xử lý concurrency duy nhất.
- C6: `03`§1 `ToResult` dùng `Problem(extensions:)` không tồn tại overload → không compile. Fix: dựng `ProblemDetails` + `.Extensions["code"/"traceId"]` + `ObjectResult` (`application/problem+json`).
- Tự bắt: khi fix C5 lỡ thêm code `request_cancelled` (499) ngoài catalog `14` → gỡ ngay (giữ `14` là nguồn khép kín).
- C7 (minor, accepted): `03` mermaid/§6 gọi `/r` như path backend; thực ra là route SPA tĩnh (`20`§2). Ý đồ "guest surface" vẫn đúng → không sửa.
- Refs: `22`§1 (C5–C7); `13`/`21` re-read: sạch.

### DEV-018: Audit 4 (re-read 02/05/16) — C8/C9/C10 (C11 accepted)
- Status: Accepted (2026-07-03)
- C8: `05`§2 nhãn "(C5, audit 2)" → sửa "audit 3".
- C9 (thật): `16`§5 trả code `token_generation_exhausted` ngoài catalog `14` → đổi `qr_generation_failed`. Cùng lớp lỗi với `request_cancelled` (self-catch): **mọi code trả ra phải thuộc catalog 14**. Bài học: khi viết pseudocode có "return error(...)", kiểm code đó có trong `14` không.
- C10: `05`§3 tóm tắt `EnforcePortalWindow` thiếu dòng `now>ExpiresAt` (lazy-expiry) → thêm + comment trỏ `13`§4.
- C11 (accepted): `02`§5 `IQrService.RenderPng` trả `byte[]` — Req 6.8 "service trả lỗi" được thỏa ở tầng use case (`16`§6 pre-validate → Result error), port là renderer thuần. Không đổi.
- `02` sạch. Refs `22`§1 (C8–C11).
- **Quy tắc rút ra (ghi để tự kiểm sau):** bất kỳ `error("...")` trong pseudocode → code PHẢI có trong `14-error-catalog.md`. Đã tự bắt 2 lần (request_cancelled, token_generation_exhausted).

### DEV-019: Expert review (external) — 8 findings đã verify + sửa tận gốc (2026-07-03)
- Status: Accepted — mỗi điểm đã đọc đúng dòng được cite, xác nhận đúng, sửa gốc.
- **P0-1 (pipeline/rate-limit):** `UseRateLimiter` chạy trước khi có GuestSessionId → guest-write không partition theo session được. Fix: tách middleware **GuestCookieRead (đọc cookie, chạy TRƯỚC UseRateLimiter)** khỏi **tạo session (chỉ trong resolve use case, sau khi token hợp lệ)**. Cập nhật `03`§4, §8; `13`§3. Không cookie → rate-limit fallback IP (Req 13.3).
- **P0-2 (portal window semantics mâu thuẫn):** `13`§3 nói "chỉ /resolve refresh" nhưng §4 refresh ở endpoint tương tác. Chốt = **SLIDING WINDOW** (hoạt động thành công trong hạn đẩy LastSeenAt; quá hạn mới phải quét lại) — khớp Req 10.5/10.6 + docs. Cập nhật `13`§1, §3.
- **P0-3 (SignalR bypass expiry):** authorize chỉ lúc join → connection cũ vẫn nghe realtime sau khi visit hết hạn. Fix: join enforce (Active+lazy-expiry+portal window); guest join group `visit-{id}-guest`; **EndVisit → NotifyVisitEndedAsync evict + chặn rejoin** (`21`§4, §4.1, §5, §6; `02`§5 thêm port; `13`§5 gọi post-commit).
- **P1-1 (migration drift ở roadmap):** `00`§Lộ trình bước 3 + `design.md`§11 bước 3 còn "toàn bộ schema" (grep audit trước sót vì tôi grep cụm khác). Fix → foundation + incremental; bước 6 → khung + entity/migration per-wave.
- **P1-2 (traceability 19→20):** Req 20 (CORS/security/HTTPS) có thật (requirements:303). Fix `11`§A (header + thêm dòng Req 20→`03`§7-8/`20`), `22`§4, README.
- **P1-3 (GuestSession key hash):** "DB lưu key hoặc hash" mơ hồ. Fix: lưu **SessionKeyHash** (unique), cookie chứa secret raw, so bằng hash (như RefreshToken). Cập nhật `03`§4, `04`§3/§7.1, `13`§3.
- **P2-1 (ProblemDetails thiếu type):** Req 4.2 cần type/title/status/code/traceId. Fix `03`§1: `Type=/problems/{code}` + traceId=Activity.Current.Id.
- **P2-2 (E7 overclaim):** tách E7a (cùng session → 1 visit) vs E7b (chưa cookie → có thể 2 session). Cập nhật `13`§7.
- Refs: `22` §9 (bảng P0–P2). Đánh giá nền kiến trúc của reviewer: 8/10 — đồng ý; các fix trên gỡ "nợ chân" trước khi code.

### DEV-020: Npgsql EF Core 10 ĐÃ BỎ helper `UseXminAsConcurrencyToken()` → map xmin THỦ CÔNG
- Bối cảnh: thiết kế `persistence-layer-design.md` §2 + DEC-047 giả định dùng `modelBuilder.Entity(...).UseXminAsConcurrencyToken()` (API kinh điển của Npgsql cho optimistic concurrency qua cột hệ thống `xmin`).
- Phát hiện (verify từ ASSEMBLY THẬT, không suy đoán): trong `Npgsql.EntityFrameworkCore.PostgreSQL` **10.0.2**, method `UseXminAsConcurrencyToken` (và cả chuỗi "xmin") KHÔNG còn tồn tại; build lỗi CS1061. Chuỗi "xid" thì vẫn còn (type hỗ trợ nguyên vẹn). Đã kiểm bằng quét byte assembly + so sánh với API đã biết (HasPostgresExtension/IsRowVersion vẫn có).
- Xử lý (fix GỐC theo API thật): map THỦ CÔNG trong `FoundationDbContext.ApplyConventions` (nhánh `Database.IsNpgsql()`):
  `Entity(clrType).Property("RowVersion").HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken()` — đúng những gì helper cũ sinh ra bên dưới.
- Verify OFFLINE (không cần Docker): test `ProviderConditionalModelTests` build model Npgsql (không mở kết nối) → xác nhận `RowVersion` là concurrency token, column `xmin`, type `xid`, ValueGenerated OnAddOrUpdate; và nhánh SQLite KHÔNG gắn token.
- Ảnh hưởng: chỉ nội bộ convention; hợp đồng `IConcurrencyAware` không đổi. `xmin` runtime thật vẫn cần Testcontainers (TK-036).

### DEV-021: Map `RefreshTokenRecord` trực tiếp thay vì tạo entity `RefreshToken:Entity` riêng (so với doc §4)
- `persistence-layer-design.md` §4 vẽ một `RefreshToken : Entity` riêng + map. Khi triển khai, map thẳng `RefreshTokenRecord` (Application, đã "persistence-facing") vào bảng `refresh_token` — DRY hơn, không cần lớp trung gian (refresh token không audit/xmin/soft-delete; không đi qua generic `IRepository<Entity>`). Chi tiết lý do: DEC-050. Verify: test store xanh trên SQLite.

### DEV-022: `ResortSettings` 1-1 dùng Id riêng + unique FK `ResortId` (thay vì ResortId làm PK như `04` §3)
- `04` §3 ghi `ResortSettings ResortId(PK/FK 1-1)`. Khi triển khai Domain, dùng `ResortSettings : AuditableEntity` (có `Id` UUIDv7 riêng từ base) + `ResortId` là **unique FK**.
- Lý do (bản chất, không ngọn): đồng nhất với base `Entity` — MỌI entity có `Id` UUIDv7 (equality/guard/`EfRepository<T>` generic nhất quán). 1-1 vẫn được enforce chặt bằng **unique index** trên `ResortId` (EF config, bước Infrastructure). Chọn shared-PK (ResortId=PK) sẽ tạo ngoại lệ cho quy ước "mọi entity có Id", làm phức tạp generic repo/convention.
- Ảnh hưởng: 1-1 vẫn đúng đắn (unique index); chỉ khác "khóa chính là surrogate Id, không phải ResortId". Không ảnh hưởng nghiệp vụ.
- Ghi ở `resort-qr/docs/domain-layer-design.md` D5. Sẽ hiện thực unique index ở EF config (Infrastructure).

### DEV-023: Rotate/Create dùng MỘT SaveChanges thay vì ExecuteInTransactionAsync (như `16` §4 vẽ)
- `16` §3/§4 minh hoạ rotate/issue bọc trong `IUnitOfWork.ExecuteInTransactionAsync`. Khi triển khai: dùng **một `SaveChangesAsync`** (revoke cũ + insert mới / room + token staged cùng lúc) — EF gói mọi thay đổi của một SaveChanges vào MỘT transaction → all-or-nothing tương đương, đơn giản hơn (không cần execution strategy/transaction thủ công khi chỉ có 1 lần ghi).
- ExecuteInTransactionAsync vẫn cần cho thao tác **nhiều lần SaveChanges** (vd publish nội quy đa bảng — `02` §3). Không xoá; chỉ không dùng ở nơi 1-save là đủ.
- Bất biến/atomicity không đổi; race vẫn do unique index (ux_qr_active) phân xử + `UniqueConstraintViolationException`. Verify: test rotate (đúng 1 Active, cũ Revoked).


### DEV-024: Mở rộng contract base `IRepository<T>` — thêm `ListAsync(predicate)` (fix tại gốc, không leak IQueryable)
- Status: Accepted (2026-07-05)
- Root cause: sub-slice B (draft full-replace) cần nạp MỘT TẬP `RuleSection` (tracked) theo `RuleSetId` để `Remove` từng cái. `IRepository` gốc chỉ có `FindByIdAsync` (1 entity), `FirstOrDefaultAsync`, `AnyAsync`, và `Query()` trả `IQueryable`. Dùng `Query()` trong use case = LEAK `IQueryable`/EF ra tầng Application (phá dependency rule + khó test), đúng thứ DEV-007 chủ đích tránh.
- Thay đổi: thêm `Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity,bool>> predicate, CancellationToken)` vào `IRepository<TEntity>` (Application) + impl EF `=> Set<T>().Where(predicate).ToListAsync()` (materialize NGAY, trả `IReadOnlyList`, không trả IQueryable). Fix tại GỐC (base contract) thay vì "hack" bằng `Query().Where().ToListAsync()` rải rác trong từng use case.
- Ranh giới: đây là thao tác GHI cần load-rồi-sửa/xóa tập nhỏ (draft). Read-model PHỨC TẠP (dashboard/list phân trang/projection) VẪN đi qua query service riêng (`IRuleDraftQueries`/`IRoomQueries` — DEV-007), KHÔNG dùng `ListAsync` cho read-model.
- Verify: build 0W/0E; toàn bộ 185 test xanh (không hồi quy các repo test cũ). Refs: DEC-061, DEV-007.
- Reversible?: có (là bổ sung thuần — không phá API cũ).


### DEV-025: Publish dùng 2 SaveChanges (hạ-cờ-rồi-flush TRƯỚC insert) thay vì 1 SaveChanges như pseudocode `02` §3
- Status: Accepted (2026-07-05)
- Root cause (bản chất): pseudocode `PublishAsync` trong `02-core-abstractions.md` §3 minh hoạ `current.IsCurrent=false` + `Add(next)` rồi **một** `SaveChangesAsync`. Nhưng `ux_pub_current` = partial unique `rule_publication(resort_id) WHERE is_current` là index KHÔNG deferrable (Postgres/SQLite kiểm ngay mỗi statement). EF Core **KHÔNG đảm bảo thứ tự UPDATE-trước-INSERT** cho các dòng cùng bảng không có FK ràng buộc giữa chúng → nếu EF phát INSERT (bản mới is_current=true) TRƯỚC khi UPDATE bản cũ về false, sẽ tồn tại thoáng qua 2 dòng is_current=true cho cùng resort → **unique-violation giả** ngay cả khi logic đúng.
- Thay đổi: trong `PublishRulesUseCase` (bọc `ExecuteInTransactionAsync`): hạ cờ current → **SaveChanges #1** (flush, giải phóng ux_pub_current), rồi Add snapshot → **SaveChanges #2**. Hai save trong CÙNG transaction ⇒ vẫn all-or-nothing (đúng ý đồ DEV-023: "ExecuteInTransactionAsync cần cho thao tác nhiều lần SaveChanges — vd publish"). Fix tại GỐC (thứ tự ghi xác định) thay vì phó mặc thứ tự batch của EF.
- Kiểm chứng: test `Publish_again_supersedes_previous_keeping_single_current` xanh trên SQLite (v1→non-current, v2 current, đúng 1 current). ⚠️ Race đa-connection thật (2 tx song song) vẫn cần Testcontainers/Postgres (TK-036) — SQLite 1-connection tuần tự không mô phỏng được.
- Refs: DEC-062, DEV-023, `02` §3, Req 5.4/8.7.
- Reversible?: không nên đảo (gộp 1 save tái tạo rủi ro unique-violation giả theo thứ tự batch EF).


### DEV-026: Port `IPortalWindowGuard` — trừu tượng "cổng" guest interactive dùng CHUNG cho mọi endpoint guest ghi/đọc bảo vệ
- Status: Accepted (2026-07-05) — code do phiên trước tạo, phiên này kiểm chứng + hoàn thiện + ghi note (code đã forward-reference "DEV-026").
- Root cause (bản chất): `13` §4 mô tả `EnforcePortalWindow` như bước lặp ở mọi endpoint guest tương tác (kiểm visit thuộc session + Active + còn hạn + lazy-expiry + sliding window). Nếu mỗi use case tự lặp logic này → nhân bản luật bảo mật, dễ lệch (một chỗ quên kiểm → lỗ hổng truy cập chéo session/visit hết hạn). 
- Thay đổi: tách thành port `IPortalWindowGuard.ValidateAsync(visitId, rawSessionKey)` trả `Result<PortalWindowState>` (Visit/Session/IdleHours). MỘT nguồn luật: hash cookie→session, visit thuộc session (chống truy cập chéo `13` E11), lazy idle-expiry (`now>ExpiresAt`→Expired, không nối lại — nhất quán ResolveTokenUseCase/DEV-014), portal window check-before. Thất bại → `session_expired` mờ (không lộ nguyên nhân). Dùng ở GetGuestRules/AcknowledgeRules (và mọi endpoint guest interactive wave sau: FAQ/chat/housekeeping). Phụ thuộc thuần port → auto-scan DI an toàn (không coupled DbContext).
- Sliding window: guard CHECK-BEFORE (không cập nhật); use case gọi RefreshWindow (LastSeenAt/ExpiresAt = now + idle) SAU khi xử lý thành công (B8, DEC-030) — không refresh cho request lỗi.
- Refs: `../design/backend/13-guest-access-flows.md` §4; DEC-030/063, DEV-014/019.
- Reversible?: không nên (đảo lại = nhân bản luật cổng ở từng use case → rủi ro lệch bảo mật).


### DEV-027: guest-write rate-limit partition theo hash(cookie) thay vì GuestSessionId (Req 13.2)
- Status: Accepted (2026-07-05)
- Root cause (bản chất): Req 13.2 nói partition theo `GuestSessionId`. Nhưng GuestSessionId là khóa DB; cookie guest chứa RAW session key (secret), KHÔNG phải id. Muốn có GuestSessionId phải hash cookie → tra DB `GuestSession`. Làm việc đó TRONG partition factory của rate-limiter = **DB read mỗi request ở tầng pipeline** (trước cả auth) → anti-pattern (chậm, coupling pipeline↔DB, chưa có middleware IGuestContext nào populate sẵn — DEV-019 P0-1 mới là ý định thiết kế, chưa hiện thực).
- Thay đổi: partition `guest-write` theo **`hash(cookie)`** (dùng chính `IGuestSessionKeyHasher` SHA-256). Cookie ↔ GuestSession là **1:1** (SessionKeyHash unique — ux_guestsession_key) nên partition theo hash(cookie) **tương đương** partition theo GuestSessionId về mặt cách ly, mà KHÔNG cần DB. Không cookie → fallback IP (Req 13.3). Không giữ secret thô làm key (hash trước).
- Kiểm chứng: test HTTP `GuestWrite_exceeding_limit_returns_429_problem` (2 request → thứ 3 chặn).
- Refs: DEC-074; Req 13.2/13.3; DEV-019 (P0-1 pipeline order); `IGuestSessionKeyHasher`.
- Reversible?: có (nếu sau này có middleware IGuestContext cache GuestSessionId, đổi key sang id — cùng hiệu quả).
