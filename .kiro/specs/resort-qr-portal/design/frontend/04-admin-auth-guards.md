# 04 — admin-web: auth store, silent refresh (single-flight), route guards

> **File authoritative cho:** xác thực phía admin-web — lưu access token an toàn, silent refresh có chống refresh-storm, xử lý reuse-detection, route guard theo Role. Đồng bộ backend `12` (Identity) + `14` (error catalog).
>
> Phạm vi base: dựng **auth store + api-client interceptor refresh + route guard + error boundary + Login skeleton**; màn nghiệp vụ khác điền wave sau.

## 1. Lưu access token — trong MEMORY, không localStorage

- Access token (JWT) giữ **trong Pinia state (memory)**; refresh token là **cookie HttpOnly** (JS không đọc được).
- **Vì sao (chính xác):** localStorage đọc được bằng JS → nếu có lỗ XSS, token bị trộm; để token trong memory + refresh trong cookie HttpOnly giảm mạnh bề mặt trộm token. Đánh đổi: **reload trang mất access token trong memory** → dùng silent refresh (§2) để lấy lại từ cookie khi khởi động app → trải nghiệm liền mạch mà vẫn an toàn.

```ts
interface AuthState {
  accessToken: string | null;     // memory only
  user: { id: string; role: 'Admin'|'Staff'; displayName: string } | null;
  refreshing: Promise<string> | null;   // single-flight (§2)
}
```

## 2. Silent refresh — single-flight (chống refresh-storm)

```ts
// api-client interceptor: khi một request nhận 401 unauthorized
async function onUnauthorized(originalReq): Promise<Response> {
  auth.refreshing ??= doRefresh();       // single-flight: nhiều 401 song song CHIA SẺ 1 promise
  try {
    const newToken = await auth.refreshing;   // gọi POST /api/auth/refresh (cookie đi kèm)
    return retryOnce(originalReq, newToken);  // thử lại ĐÚNG 1 lần
  } catch {
    auth.hardLogout();                    // refresh thất bại → về /login
    throw new ApiError('unauthorized', 401, '...');
  } finally {
    auth.refreshing = null;
  }
}
```

- **Vì sao single-flight (bản chất):** khi access token hết hạn, nhiều request song song cùng nhận 401. Nếu mỗi cái tự gọi `/refresh` → **refresh-storm** + rotation đá nhau (mỗi refresh xoay token, cái sau dùng token đã bị xoay → **reuse detection thu hồi family** → đăng xuất oan). Single-flight: tất cả 401 chờ **một** lần refresh, rồi retry với token mới.
- **Retry đúng 1 lần**: nếu vẫn 401 sau refresh → hard logout (tránh vòng lặp vô hạn).
- **Reuse detection (khớp `12` §3):** nếu `/refresh` trả 401 vì family bị thu hồi (token bị trộm/xoay sai) → hard logout ngay → `/login`. Không cố refresh lại.
- (Tùy chọn nâng cao) refresh **chủ động** trước khi `exp` tới (đọc exp từ JWT) để tránh 401 giữa thao tác — không bắt buộc ở base.

## 3. Route guards theo Role

```ts
// meta: { requiresAuth: true, roles?: ['Admin'] }
router.beforeEach(async (to) => {
  if (!to.meta.requiresAuth) return true;
  if (!auth.accessToken) { try { await auth.tryRestore(); } catch { /* ignore */ } }  // silent refresh khi mở app
  if (!auth.user) return { name: 'login', query: { redirect: to.fullPath } };          // chưa đăng nhập
  if (to.meta.roles && !to.meta.roles.includes(auth.user.role))
     return { name: 'forbidden' };                                                     // đủ đăng nhập nhưng sai quyền
  return true;
});
```

- **`requiresAuth`** → chưa đăng nhập redirect `/login` (giữ `redirect` để quay lại) — khớp Req 17.8.
- **`roles: ['Admin']`** cho Rooms&QR/Users/Settings; Staff bị chặn với màn "không đủ quyền" (Req 17.9). Khớp phân quyền backend (`RequireAdmin`/`RequireStaff`).
- Guard là **UX**; **backend vẫn là trọng tài** (policy trên endpoint) — FE guard không thay thế server authorization (Property B9).

## 4. Ánh xạ ErrorCode (admin) — khớp `14`

| code / status | Hành vi admin-web |
|---|---|
| `401 unauthorized` | silent refresh (§2) → thất bại thì `/login` |
| `403 forbidden` | màn "không đủ quyền" |
| `validation_error` (400) | hiển thị lỗi theo field (từ `errors`) |
| `concurrency_conflict` (409) | "Nội dung đã đổi, tải lại" + nút reload |
| `invalid_configuration`/`qr_generation_failed`/`pdf_limit_exceeded` | thông báo nghiệp vụ tương ứng (Settings/QR) |
| `rate_limited` (429) | toast + tôn trọng Retry-After |
| `unexpected` (500) | toast lỗi chung, không lộ chi tiết |

## 5. CSRF (admin)
- Access token đi qua **header `Authorization: Bearer`** (không tự gửi cross-site) → CSRF thấp cho các request nghiệp vụ.
- Refresh cookie đặt **`SameSite=Strict`** + path giới hạn endpoint refresh → giảm CSRF cho refresh.
- (Guest surface có xử lý CSRF riêng — `03`/GAP-2.)

## 6. Error boundary & khởi động
- App bootstrap: `tryRestore()` (silent refresh từ cookie) → nếu có phiên hợp lệ, nạp user; ngược lại tới `/login`.
- Error boundary toàn cục: lỗi render/unhandled → trang lỗi thân thiện + nút về dashboard; không lộ chi tiết kỹ thuật.

## 7. Truy vết
- **Validates: Requirements 11.1, 11.7, 11.8, 17.8, 17.9**
- Đồng bộ: backend `12` (refresh rotation + reuse detection), `14` (error codes), Property B9.
