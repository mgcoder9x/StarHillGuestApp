# 22 — Cross-Consistency Audit (đối chiếu nhất quán toàn bộ design)

> **File authoritative cho:** kết quả rà soát **nhất quán chéo** giữa tất cả file design (backend 00→21, frontend, requirements, ai-notes). Mục tiêu: một claim xuất hiện ở nhiều nơi phải **giống nhau**; quyết định đã chốt không bị bản cũ mâu thuẫn. Đúng tinh thần "valid nhiều lần, chính xác kiểm chứng được".
>
> Ngày audit: 2026-07-03. Phương pháp: grep các pattern rủi ro (thuật ngữ/con số/quyết định xuất hiện đa-nơi) + đối chiếu thủ công từng cặp file liên quan.

## 1. Mâu thuẫn tìm được & đã sửa

| # | Chỗ lệch | Nguồn chân lý | Đã sửa |
|---|---|---|---|
| C1 | `design.md` (master) §4.3 còn `IsRowVersion()` (kiểu SQL Server) | `03` §3 dùng `UseXminAsConcurrencyToken()` (Npgsql) | ✅ sửa master → `UseXminAsConcurrencyToken()` |
| C2 | `design.md` (master) §5.4 còn "định nghĩa schema đầy đủ ngay" | `04` §4 = migration incremental per-wave (TRD-002) | ✅ sửa master → incremental + trỏ `04` |
| C3 | `04` §3 ghi `GuestSession.SessionKey (indexed)` trong khi §7.1 làm UNIQUE | §7.1 `ux_guestsession_key` UNIQUE | ✅ sửa §3 → `(UNIQUE, indexed)` (tránh implementer tạo non-unique) |
| C4 | `12` §3 câu "nên bổ sung FamilyId... so với 04 §3" đã lỗi thời (04 §3 đã có) | `04` §3 (đã có FamilyId/ReplacedByTokenId/RevokedReason) | ✅ sửa `12` §3 → "đã đồng bộ với 04 §3" |

| C5 | `05` §2 UoW.SaveChanges "return error(concurrency_conflict)" **mâu thuẫn kiểu** `Task<int>` (`02`§3) + trùng cơ chế với middleware (`03`§3) | `IUnitOfWork.SaveChangesAsync : Task<int>`; middleware là chỗ ánh xạ 409 | ✅ `05`§2: bỏ return-error, để exception propagate; `03`§1: thêm case tường minh `DbUpdateConcurrencyException→409` |
| C6 | `03` §1 `ToResult` dùng `Problem(..., extensions:...)` — overload không có `extensions` → **không compile** | `ControllerBase.Problem` không nhận extensions | ✅ dựng `ProblemDetails` + `.Extensions["code"]` + `ObjectResult` |
| C7 | `03` §4 mermaid/§6 liệt kê `/r` như path backend, nhưng `/r/{token}` là **route SPA tĩnh** (`20`§2); backend guest là `/api/guest` | `20` §2 | 🟡 Accepted-minor: ý đồ "guest surface" vẫn đúng; không sửa (tránh over-edit), ghi để minh bạch |

| C8 | `05` §2 chú thích "(C5, audit 2)" — C5 thuộc audit 3 | audit history | ✅ sửa nhãn → "audit 3" |
| C9 | `16` §5 trả `error("token_generation_exhausted")` — code **ngoài catalog 14** | `14` là tập code khép kín | ✅ đổi → `qr_generation_failed` (có trong `14`) |
| C10 | `05` §3 bản tóm tắt `EnforcePortalWindow` thiếu dòng lazy-expiry `now>ExpiresAt` (bản đầy đủ `13`§4 có) | `13` §4 | ✅ thêm dòng + comment trỏ `13`§4 (tránh copy thiếu) |
| C11 | `02` §5 `IQrService.RenderPng` trả `byte[]` vs Req 6.8 "service trả lỗi khi URL không hợp lệ" | Req 6.8 | 🟡 Accepted: use case (`16`§6) pre-validate & trả Result error trước khi gọi port; port là renderer thuần |

> **Lượt audit 2 (2026-07-03):** re-read `04` + `12` → C3, C4. **Lượt audit 3:** re-read `13` + `03` + `21` → `13`/`21` sạch; C5 (mâu thuẫn kiểu), C6 (không compile), C7 (minor). **Lượt audit 4:** re-read `02` + `05` + `16` → `02` sạch; C8 (nhãn sai), C9 (code ngoài catalog — cùng lớp lỗi self-catch với `request_cancelled`), C10 (pseudocode tóm tắt thiếu lazy-expiry), C11 (minor, accepted). Đã sửa C8/C9/C10.

> Cả hai nằm ở master overview (đã có banner "design/backend là nguồn chân lý"), nhưng vẫn sửa để master **không phát biểu ngược** quyết định đã chốt (tránh người đọc master bị lạc).

## 2. Ma trận bất biến chéo (đã đối chiếu = nhất quán)

| Bất biến/quyết định | Xuất hiện ở | Trạng thái |
|---|---|---|
| Concurrency = `xmin` via `UseXminAsConcurrencyToken()` (KHÔNG IsRowVersion) | `02`§1, `03`§3, `04`§9, master§4.3, ai-notes TK-003 | ✅ nhất quán |
| Khóa chính `uuid`, sinh client-side UUIDv7 (fallback PG18 `uuidv7()`, không đổi schema) | `02`§1, `04`§1, master§3.1/§5.1, req 3.1, ai-notes DEC-003/TRD-003/TK-002 | ✅ nhất quán |
| Migration incremental per-wave | `04`§4-5, master§5.4, req 8.6-8.7/18.1-18.2, ai-notes TRD-002/DEC-010/DEV-006 | ✅ nhất quán |
| Mapping = Mapperly (Apache-2.0), KHÔNG AutoMapper | `06`, `07`, master§9/§10, `technology-stack`, `11`GAP-5, ai-notes DEV-009 | ✅ nhất quán (đã sửa "MIT"→Apache-2.0) |
| Admin UI = Element Plus (loại PrimeVue do archive) | FE README/`01`/`02`, `technology-stack`, ai-notes TRD-007/DEC-013 | ✅ nhất quán |
| Error codes: `14` là catalog authoritative; `02`§2 chỉ ví dụ | `02`§2 (có pointer), `14`, FE `03`/`04`, ai-notes DEC-019 | ✅ nhất quán |
| Repository không SaveChanges; UoW là điểm ghi duy nhất | `02`§3, master§3.3, req 5, `09`E1/E3, Property B2 | ✅ nhất quán |
| Realtime notify POST-COMMIT | `02`§6.2, `06`, `21`§6, ai-notes DEV-008 | ✅ nhất quán |
| Lazy idle-expiry (không chỉ sweeper) | `13`§2-5, `05`§3, ai-notes DEV-014 | ✅ nhất quán |
| 1 token Active/phòng + Token UNIQUE toàn cục | `04`§3/§7.1, `16`§3-4, Property B3, ai-notes DEV-010 | ✅ nhất quán |
| 1 GuestVisit Active/(session,room) `ux_visit_active` | `04`§7.1, `13`§3, Property B3, ai-notes DEV-013 | ✅ nhất quán |
| Auth: Argon2id + JWT HS256 + refresh rotation/reuse-detection | `12`, `04`§3 (RefreshToken schema), FE `04`, ai-notes DEC-016/DEV-012 | ✅ nhất quán |
| ForwardedHeaders ⇒ rate-limit theo IP đúng | `03`§5/§8, `20`§4, ai-notes DEC-018/TK-027 | ✅ nhất quán |
| Guest SignalR cookie + authorize join server-side (GAP-1) | `21`, `11`GAP-1(resolved), FE `03`, ai-notes DEC-026/TK-024 | ✅ nhất quán |

## 3. Đối chiếu CON SỐ (đa-nguồn phải khớp)

| Tham số | Giá trị | Nguồn khớp |
|---|---|---|
| PortalWindowMinutes | 30 (khoảng 1–1440) | req 10.3/15.3, `04`§3, `13`§1, `15`, docs |
| VisitIdleExpiryHours | 24 (1–168) | req 10.4/15.3, `04`§3, `13`§1, `15`, docs |
| JWT access token | 15' (5–60) | req 11.1, `12`§2, `15` |
| Refresh token | 30 ngày (7–90) | req 11.1, `12`§3, `15` |
| Rate limit resolve | 20 req/60s/IP | req 13.1, `03`§5, `15` |
| Rate limit guest-write | 10 req/60s/session | req 13.2, `03`§5, `15` |
| Token entropy / độ dài | ≥32 byte / base64url ≥43 ký tự | req 7.1-7.2, `05`§1, `16`§5 |
| PDF tối đa | 500 phòng/lần | req 16.8, `16`§7 |
| Sweeper chu kỳ | 5' | req 14.4, `13`§6, `15` |
| MaxMessageLength | 2000 (1–10000) | req 15.3, `03`§2, `15` |
| Health ready DB timeout | 5s | req 14.6, `19`§5, `15` |

→ Tất cả **khớp**. (Con số là default AI đề xuất — DEC-009; vẫn tune được, nhưng hiện đồng bộ mọi nơi.)

## 4. Truy vết Property → Requirements (không property "mồ côi")

- B1..B10 (`08`) đều có `Validates: Requirements X.Y`; test tương ứng ở `17` §2. Traceability **20 requirements** (đã sửa từ 19 — Req 20 CORS/security/HTTPS) → design ở `11` §A (mọi req có chỗ phủ; Req 20 → `03` §7-8 + `20`).

## 5. Điểm "chưa đầy đủ nhưng KHÔNG mâu thuẫn" (ghi để minh bạch)

- Master `design.md` §5 (data models kiểu docs) ghi `RoomQrToken Token (random, indexed)` — **thiếu** chữ "UNIQUE toàn cục" mà `04`§7.1 bổ sung. Đây là **thiếu chi tiết**, không mâu thuẫn (indexed ⊂ unique-indexed). Nguồn chân lý là `04`. Không sửa master (đã có banner) để tránh chỉnh sửa lan man; ai đọc chi tiết luôn theo `design/backend/`.
- Một số con số ở master overview mang tính minh hoạ; nguồn chân lý con số là `15`/requirements.

## 6. Kết luận
- **Không còn mâu thuẫn logic** giữa các file sau khi sửa C1, C2.
- Các bất biến/quyết định/con số đa-nơi **đã khớp**.
- Nguồn chân lý rõ ràng: chi tiết theo `design/backend/**` và `design/frontend/**`; master `design.md` là overview có banner.
- Audit này nên **chạy lại** (grep các pattern ở §1–3) sau mỗi lần sửa design lớn — xem `../../ai-notes/04-things-to-know.md` TK-032.

## 7. Kiểm tra hệ thống: mọi mã lỗi phải thuộc catalog 14 (audit 4)

Sau khi tự bắt 2 lần code ngoài catalog (`request_cancelled`, `token_generation_exhausted`), đã **grep toàn bộ `design/**` cho `error(...)`** và đối chiếu với `14`:
- Tất cả usage hiện dùng code ∈ `14`: `qr_invalid, qr_revoked, room_inactive, session_expired, qr_generation_failed, invalid_configuration, pdf_limit_exceeded` (BE); `unauthorized, unexpected` (FE api-client). ✅ **Không còn code ngoài catalog.**
- **Quy tắc tự kiểm về sau (TK):** mỗi khi thêm `error("...")`/`throw new ApiError('...')` → xác nhận code có trong `14` (hoặc bổ sung `14` + `AppErrors` + FE `ErrorCode` đồng thời).

## 8. File đã re-read sâu (từng dòng)

`04, 12` (audit 2) · `13, 03, 21` (audit 3) · `02, 05, 16, 24` (audit 4). Kết quả: `13, 21, 02, 24` sạch; các file khác đã sửa C1–C10 (C7/C11 accepted-minor). Các file còn lại (`00,01,06,07,08,09,10,11,15,17,18,19,20,23` + frontend) là narrative/config/convention, rủi ro thấp, đã phủ bởi audit-1 (grep diện rộng) + đối chiếu chéo.

## 9. Expert review (external) — audit 5 (2026-07-03)

Reviewer chấm nền kiến trúc 8/10; tìm 8 điểm (đã verify từng dòng cite → đúng → sửa gốc). Chi tiết fix: `../../ai-notes/02-deviations-from-spec.md` DEV-019.

| # | Sev | Vấn đề | Fix |
|---|---|---|---|
| P0-1 | P0 | Rate-limit guest-write partition theo GuestSessionId nhưng `UseRateLimiter` chạy trước khi nạp session | Tách GuestCookieRead (trước rate limiter) vs tạo-session (trong resolve); `03`§4/§8, `13`§3 |
| P0-2 | P0 | Portal window 2 semantics trái nhau (`13`§3 vs §4) | Chốt **sliding window**; `13`§1/§3 |
| P0-3 | P0 | SignalR chỉ authorize lúc join → nghe lén sau expiry | Enforce ở join + **EndVisit evict** (VisitEnded); `21`§4/§4.1/§5/§6, `02`§5, `13`§5 |
| P1-1 | P1 | Migration drift ở roadmap (`00`, `design.md`§11) | Sửa → foundation + incremental per-wave |
| P1-2 | P1 | Traceability ghi 19 nhưng có 20 requirements | `11`§A + Req20 row, `22`§4, README |
| P1-3 | P1 | GuestSession "key hoặc hash" mơ hồ | Lưu **SessionKeyHash**; `03`§4, `04`§3/§7.1, `13`§3 |
| P2-1 | P2 | ProblemDetails thiếu `type` | `Type=/problems/{code}`; `03`§1 |
| P2-2 | P2 | E7 overclaim (2 tab chưa cookie) | Tách E7a/E7b; `13`§7 |

**Bài học:** grep audit trước sót P1-1 vì tìm cụm cụ thể ("định nghĩa schema đầy đủ ngay") thay vì cả họ cụm ("toàn bộ schema"). → khi audit drift, grep **nhiều biến thể** của cùng ý.

## 10. Verify pass cuối — audit 6 (2026-07-04): quét residual của audit 5 ở master `design.md`

Sau khi sửa 8 điểm P0/P1/P2 ở `design/backend/**`, chạy grep-nhiều-biến-thể trên **master `design.md`** (bài học §9) và bắt được **3 residual** — cùng lớp lỗi "master phát biểu ngược quyết định đã chốt", đều đã sửa gốc:

| # | Residual (master design.md) | Nguồn chân lý | Đã sửa |
|---|---|---|---|
| R1 (của P1-3) | §Security guest còn `"random key; DB lưu key hoặc hash"` | `03`§4, `04`§3/§7.1 = lưu **SessionKeyHash** | ✅ đổi → cookie giữ raw secret ≥256-bit, DB chỉ lưu `SessionKeyHash` (unique), đối xứng RefreshToken |
| R2 (của P1-1) | §5.5 heading `"DB constraints nền (enforce ở migration đầu tiên)"` ⇒ ngược incremental per-wave | `04`§5 = mỗi index ở migration của wave sở hữu bảng | ✅ đổi heading + thêm câu "mỗi index tạo trong migration của wave sở hữu bảng", trỏ `04`§5 |
| R3 (của P0-2) | `13`§3 comment inline `"refresh cửa sổ (chỉ /resolve được phép)"` ⇒ ngược sliding window | `13`§3 POST + §4 = endpoint tương tác cũng refresh trong hạn | ✅ sửa comment → nêu rõ sliding window; điểm riêng của /resolve chỉ là mở khóa khi ĐÃ quá portal window |

**Kết luận audit 6:** master `design.md` không còn phát biểu ngược các quyết định P0-2/P1-1/P1-3. `get_diagnostics` trên `design.md`, `requirements.md`, `13`, `ai-notes/01` = 0 lỗi format. Toàn bộ 8 điểm expert-review + 3 residual đã đóng.
