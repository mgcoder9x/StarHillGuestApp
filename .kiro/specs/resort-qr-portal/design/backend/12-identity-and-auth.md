# 12 — Identity & Auth (đặc tả sâu, chuẩn thương mại)

> **File authoritative cho:** module Identity của base — hashing mật khẩu, JWT, refresh token rotation + reuse detection, luồng login/logout, lockout, và ranh giới với guest cookie.
>
> **Vì sao cần file riêng:** reference **rỗng hoàn toàn** phần auth (E9) và **lưu mật khẩu plaintext** (E10). Auth là nơi sai một ly đi một dặm cho sản phẩm thương mại → phải đặc tả chính xác, kiểm chứng được, trước khi code.
>
> Mọi tham số crypto cụ thể đánh dấu ⚠️ = **tune/verify theo OWASP + benchmark phần cứng khi implement**, KHÔNG hardcode "số thần thánh" trong tài liệu.

## 1. Hashing mật khẩu

### Lựa chọn: **Argon2id** (ưu tiên), fallback **PBKDF2-HMAC-SHA256** nếu ràng buộc hạ tầng.

**Lý do chính xác (vì sao Argon2id):**
- Argon2id là **memory-hard**: kẻ tấn công brute-force bằng GPU/ASIC bị chặn bởi chi phí RAM, không chỉ CPU → mạnh hơn PBKDF2/bcrypt trước tấn công phần cứng chuyên dụng.
- Biến thể **id** kết hợp kháng cả side-channel (Argon2i) lẫn GPU-cracking (Argon2d) → khuyến nghị mặc định của OWASP cho lưu mật khẩu.
- Fallback PBKDF2 chỉ khi môi trường bắt buộc thuật toán FIPS-approved; khi đó dùng iteration count cao theo OWASP.

**Tham số (⚠️ tune khi implement, theo OWASP Password Storage Cheat Sheet + benchmark để 1 lần hash ~ vài trăm ms trên phần cứng prod):**
- `memoryKiB`, `iterations`, `parallelism`, `saltLength` (≥16 byte, CSPRNG mỗi mật khẩu), `hashLength` (≥32 byte).
- Ghi TK-026: chốt con số sau benchmark, không lấy số cứng từ tài liệu này.

**Quy tắc bắt buộc:**
- Mỗi mật khẩu có **salt ngẫu nhiên riêng** (không salt dùng chung).
- Lưu **định danh thuật toán + tham số + salt + hash** trong `PasswordHash` (định dạng tự mô tả, ví dụ PHC string `$argon2id$v=19$m=...,t=...,p=...$salt$hash`) để **rehash khi nâng tham số** mà không phá dữ liệu cũ.
- **Rehash-on-login:** khi user đăng nhập thành công mà tham số hash cũ hơn cấu hình hiện tại → hash lại bằng tham số mới, cập nhật `PasswordHash`. Cho phép nâng độ khó theo thời gian không cần reset toàn bộ.
- **KHÔNG bao giờ** lưu/log mật khẩu hay hash dạng có thể đảo (sửa gốc lỗi E10). So khớp bằng hàm **constant-time** của thư viện.

## 2. JWT access token

**Đặc điểm:** ngắn hạn (⚠️ mặc định ~15', khoảng 5–60' — Req 11.1), client giữ **trong memory** (không localStorage — chống XSS trộm token, xem `../frontend/02` §8).

**Claims tối thiểu:** `sub` (userId), `role` (Admin/Staff), `resortId`, `jti` (id token), `iat`, `exp`, `iss`, `aud`.

**Ký & xác thực:**
- Base MVP: **khóa đối xứng HMAC-SHA256** (HS256), `SigningKey` ≥ 256-bit lấy từ secret store (§`10` §1). Đủ vì chỉ backend tự phát & tự verify (không bên thứ ba verify).
- Đường mở: chuyển **bất đối xứng (RS256/ES256)** nếu sau này có bên thứ ba cần verify mà không giữ secret — chỉ đổi cấu hình, không đổi luồng.
- Validation params bật đầy đủ: verify **signature, issuer, audience, lifetime**; `ClockSkew` đặt nhỏ (⚠️ vài phút, không để mặc định 5' quá rộng nếu muốn chặt).

> **Lý do khóa đối xứng cho MVP (chính xác, không theo trend):** chỉ có một bên (backend) vừa ký vừa verify → HS256 đơn giản, nhanh, an toàn tương đương khi `SigningKey` được bảo vệ. Bất đối xứng chỉ cần khi tách bên verify khỏi bên ký — chưa phát sinh ở giai đoạn này. Chọn phức tạp sớm là over-engineering.

## 3. Refresh token (rotation + reuse detection)

**Bản chất:** access token ngắn hạn cần refresh token dài hạn để duy trì đăng nhập; refresh token là mục tiêu trộm cắp giá trị cao → phải thiết kế chống tái sử dụng.

**Thiết kế:**
- Refresh token là **chuỗi ngẫu nhiên CSPRNG** (opaque, không phải JWT), lưu **hash** trong DB (`RefreshToken.TokenHash`) — không lưu plaintext (nếu DB lộ, token không dùng được).
- Đặt trong **cookie HttpOnly, Secure, SameSite=Strict** (admin surface), path giới hạn endpoint refresh.
- **Rotation:** mỗi lần dùng refresh token để lấy access mới → **cấp refresh token mới, thu hồi cái cũ** (one-time use). Giảm cửa sổ giá trị của một token bị lộ.
- **Reuse detection (token family):** mỗi chuỗi refresh gắn một `FamilyId`. Nếu một refresh token **đã bị xoay (đã dùng)** lại được trình lần nữa → dấu hiệu **token bị đánh cắp** → **thu hồi toàn bộ family** (buộc đăng nhập lại). Đây là cơ chế phát hiện trộm refresh token tiêu chuẩn.
- **Expiry:** ⚠️ mặc định ~30 ngày (khoảng 7–90 — Req 11.1). Hết hạn/không hợp lệ → xóa cookie, buộc login lại (Req 11.8).
- **Logout:** thu hồi refresh token hiện tại (và tùy chọn cả family/tất cả thiết bị).

> **Schema (đã đồng bộ với `04` §3):** `RefreshToken` có `FamilyId (uuid)`, `ReplacedByTokenId (uuid?)`, `RevokedReason?` để hỗ trợ rotation + reuse detection — `04` §3 đã phản ánh đúng schema này (không còn lệch). Ghi DEV-012.

**Schema RefreshToken (tham chiếu — nguồn chân lý `04` §3):**
```text
RefreshToken  Id, UserId, FamilyId, TokenHash(indexed),
              ExpiresAt, CreatedAt, RevokedAt?, ReplacedByTokenId?, RevokedReason?
              -- index: (TokenHash) để tra khi refresh; (UserId) để thu hồi theo user/family
```

## 4. Luồng login & chống brute-force

1. `POST /api/auth/login { email, password }` → tìm user active theo email; so khớp Argon2id constant-time.
2. Thành công → phát access (JWT) + refresh (cookie), cập nhật `LastLoginAt`, rehash-on-login nếu cần.
3. Thất bại → trả lỗi **mơ hồ đồng nhất** ("email hoặc mật khẩu không đúng") — **không** tiết lộ email tồn tại hay không (chống user enumeration).
4. **Lockout (⚠️ ngưỡng chốt với user — TK-008):** khóa tạm tài khoản/IP sau N lần sai liên tiếp trong cửa sổ thời gian; đếm bằng bộ đếm có TTL. Bối cảnh nội bộ có thể nới, nhưng commercial vẫn nên có.
5. Response thời gian gần như hằng định để giảm timing oracle (so khớp hash luôn chạy kể cả khi user không tồn tại — dùng hash giả).

## 5. Ranh giới guest vs admin (nhắc lại, tránh nhầm)

- **Admin/Staff:** JWT (header) + refresh cookie — như trên. CSRF thấp vì access token đi qua **header Authorization**, không tự gửi cross-site.
- **Guest:** cookie `GuestSession` HttpOnly định danh thiết bị, **không** phải auth đăng nhập; state-changing guest endpoints cần chống CSRF (GAP-2, `../frontend/02` §9). Hai surface tách bạch (Property B9).

## 6. Không dùng lại gì từ reference

Reference: `Auth` rỗng (E9), `User.Password` plaintext (E10), không JWT scheme (E5). ⇒ toàn bộ module Identity **viết mới** theo file này (DEC-011). Chỉ giữ **ý tưởng** use-case-per-operation cho `LoginUseCase`/`RefreshTokenUseCase`/`LogoutUseCase`.

## 7. Test bắt buộc (Identity)

- Unit: hashing round-trip + rehash-on-login; constant-time compare (không test timing tuyệt đối, test logic).
- Unit: refresh rotation cấp token mới + thu hồi cũ; **reuse detection thu hồi cả family**.
- Integration: login sai N lần → lockout; refresh hết hạn → 401 + xóa cookie; `/api/admin/*` không JWT → 401/403 (Property B9).
- Property-based: token sinh (access `jti`, refresh) không trùng trong tập sinh.
