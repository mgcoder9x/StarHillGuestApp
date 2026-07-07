# Vertical slice: ResolveToken (guest access) — design-first

> Bám authoritative `13-guest-access-flows` (§3 thuật toán, §7 edge-case), `14` (mã lỗi), `04` (entity/index), `02` §2/§5 (Result/port). Test SQLite Docker-free.

## 0. Phạm vi (scope) — cắt gọn có chủ đích

**Trong slice này:** logic CỐT LÕI token→phòng→visit (phần rủi ro cao "sai edge-case = rò rỉ dữ liệu giữa khách" — `13` §2) dưới dạng use case testable + endpoint mỏng.
- `ResolveTokenUseCase`: token lookup → validate phòng/resort → resolve/tạo GuestSession → GuestVisit **lazy idle-expiry + nối lại Active + tạo mới có race DB-arbitrated** + sliding window.
- Endpoint mỏng `GET /api/guest/resolve/{token}`: đọc cookie guest (raw) → gọi use case → set cookie nếu có session mới → ProblemDetails khi lỗi.

**Hoãn (increment sau, có lý do):**
- `rule_ack_required` + ack state: cần `RulePublication` (wave Rules — chưa có entity). 
- `EnforcePortalWindow` (cho endpoint tương tác FAQ/chat/HK) + cascade EndVisit (đóng Conversation/hủy Ticket): cần entity wave Messaging/Housekeeping.
- Danh sách ngôn ngữ đầy đủ trong response: cần materialize list (ToListAsync = EF) → dùng query service (Infrastructure) sau; slice này chỉ trả **default language** + features (đều lookup 1-entity qua `IRepository.FirstOrDefaultAsync`, KHÔNG cần EF trong Application).

## 1. Contract use case (HTTP-free → testable)
```
public sealed record ResolveTokenInput(string RawToken, string? CurrentSessionKey);
public sealed record ResolveTokenResult(
    Guid RoomId, string RoomNumber, Guid ResortId, string ResortName,
    Guid VisitId, string? IssuedSessionKey,          // != null → endpoint set cookie
    bool FaqEnabled, bool ChatEnabled, bool HousekeepingEnabled,
    string? DefaultLanguageCode);
public interface IResolveTokenUseCase : IUseCase<ResolveTokenInput, ResolveTokenResult>, IScopedService;
```
- Session identity truyền qua `CurrentSessionKey` (raw cookie), KHÔNG phải HttpContext → use case thuần. Session mới → trả `IssuedSessionKey` (raw) cho Api set cookie (Application không đụng HTTP).

## 2. Thuật toán (bám `13` §3, đã điều chỉnh cho kiến trúc)
```
now = clock.UtcNow
// (1) token → phòng (không lộ phòng khác khi lỗi)
token = repo<RoomQrToken>.FirstOrDefault(t => t.Token == RawToken)
  null            → Fail(qr_invalid 404)
  Status=Revoked  → Fail(qr_revoked 404)
room = repo<Room>.FindById(token.RoomId)         // query filter loại soft-deleted → null nếu đã xóa
  null OR Status != Active → Fail(room_inactive 409)
resort = repo<Resort>.FindById(room.ResortId)
settings = repo<ResortSettings>.FirstOrDefault(s => s.ResortId == resort.Id)   // null → default an toàn
idleHours = settings?.VisitIdleExpiryHours ?? 24

// (2) GuestSession (định danh thiết bị) — SAVE session TRƯỚC (tách khỏi visit)
session = CurrentSessionKey!=null ? repo<GuestSession>.FirstOrDefault(s => s.SessionKeyHash == hash(CurrentSessionKey)) : null
issuedKey = null
IF session == null:
    rawKey = tokenGen.NewToken(); session = new GuestSession{ SessionKeyHash=hash(rawKey), FirstSeenAt=now, LastSeenAt=now }
    repo.Add(session); await uow.Save()      // new session id riêng → KHÔNG có race visit ở dưới
    issuedKey = rawKey
ELSE:
    session.LastSeenAt = now; await uow.Save()

// (3) GuestVisit — lazy idle-expiry + nối lại + tạo mới (race)
visit = repo<GuestVisit>.FirstOrDefault(v => v.GuestSessionId==session.Id && v.RoomId==room.Id && v.Status==Active)
        // ux_visit_active đảm bảo ≤1 Active/(session,room) → không cần ORDER BY (bất biến, không phải tiện)
IF visit != null AND now > visit.ExpiresAt:      // lazy expiry — KHÔNG phụ thuộc sweeper (13 §2)
    visit.Status=Expired; visit.ClosedAt=now; await uow.Save()   // cascade hoãn (chưa có Conversation/Ticket)
    visit = null
IF visit == null:
    visit = new GuestVisit{ Active, StartedAt=now, LastSeenAt=now, ExpiresAt=now+idleHours }
    repo.Add(visit)
    TRY await uow.Save()
    CATCH UniqueConstraintViolationException:     // race: request song song (session sẵn) vừa tạo → dùng lại
        detach(visit); visit = repo<GuestVisit>.FirstOrDefault(Active (session,room))
        IF visit == null: rethrow  // không phải race visit → lỗi thật
ELSE:
    visit.LastSeenAt=now; visit.ExpiresAt=now+idleHours; await uow.Save()   // sliding window (13 §3)

RETURN Ok(ResolveTokenResult{ room..., resort..., visit.Id, issuedKey, features(settings), settings?default lang })
POST: đúng 1 GuestVisit Active/(session,room); không nối lại visit quá hạn (Req 10.5); không lộ phòng khác khi lỗi.
```

## 3. Cơ chế nền cần thêm (fix gốc, tái dùng)
- **`UniqueConstraintViolationException`** (SharedKernel, trung lập): `EfUnitOfWork.SaveChangesAsync` bắt `DbUpdateException` do **unique violation** → ném exception này (để use case bắt + re-query, KHÔNG rò EF lên Application). Phát hiện provider: Npgsql `PostgresException.SqlState=="23505"`; SQLite (test) match theo type-name + message "UNIQUE" (Infrastructure KHÔNG tham chiếu Sqlite). Api map → 409 (an toàn nếu escape).
- **`IGuestSessionKeyHasher`** (Application, ISingletonService) + `Sha256GuestSessionKeyHasher` (Infrastructure): hash session key (SHA-256 hex) — DB lưu HASH, cookie giữ raw (`04` §7.1). Tách khỏi `IRefreshTokenHasher` cho rõ ngữ nghĩa.
- **`GuestAccessErrors`** (Application): `QrInvalid`(404), `QrRevoked`(404), `RoomInactive`(409), `SessionExpired`(403) — mã ∈ catalog `14`.

## 4. Endpoint mỏng (`GuestAccessEndpoints`)
`GET /api/guest/resolve/{token}` (AllowAnonymous, rate-limit sau):
- đọc `Request.Cookies[GuestOptions.CookieName]` → `CurrentSessionKey`.
- gọi use case; nếu `IssuedSessionKey != null` → set cookie **HttpOnly, Secure, SameSite=Lax, Path=/** hạn `SessionCookieDays` (Lax vì QR mở tab mới — `13`/`03` §4).
- Result → 200 JSON (map DTO) hoặc ProblemDetails (dùng `ResultExtensions` base). 
- `GuestOptions` (Api): CookieName="shq_guest", SessionCookieDays=60 (`15`).

## 5. Test (SQLite Docker-free) — ma trận `13` §7
- E1 token không tồn tại → qr_invalid, không tạo session/visit.
- E2 token Revoked → qr_revoked.
- E3 phòng Inactive/Maintenance/soft-deleted → room_inactive.
- E4 thiết bị mới (CurrentSessionKey=null) → tạo GuestSession + GuestVisit Active + trả IssuedSessionKey.
- E5 cùng session+phòng, visit Active còn hạn, gọi lại → **nối lại đúng visit** (cùng VisitId), refresh LastSeenAt/ExpiresAt, KHÔNG issue key mới.
- E6 visit Active nhưng now>ExpiresAt (idle) → **lazy-expire** cũ (Status=Expired) + tạo visit MỚI (khác VisitId).
- E11 cùng thiết bị quét phòng A rồi B → 2 visit Active (mỗi phòng 1).
- (race đa-connection E7a: logic đúng-bởi-xây-dựng + ux_visit_active; race thật → Testcontainers TK-036.)
- Endpoint HTTP (SQLite factory): resolve thiết bị mới → 200 + Set-Cookie; token sai → 404 problem+json.

## 6. Truy vết
- `13` §3/§7, `14` §2.1, `04` §7.1 (ux_visit_active/ux_guestsession_key), `02` §2/§5; Req 1.3/1.4/1.5, 10.1–10.6. Base: DEC-047/048/054.
