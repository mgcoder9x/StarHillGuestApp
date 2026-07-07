# 20 — Deployment & Reverse Proxy (HTTPS, same-origin, WebSocket, security headers)

> **File authoritative cho:** cách triển khai base sau reverse proxy — định tuyến same-origin, TLS/HSTS, WebSocket cho SignalR, security headers, align với ForwardedHeaders (`03` §8), và quy trình áp migration khi deploy. Nền cho Req 12.5 + docs Deployment.
>
> **Bối cảnh (docs):** chạy WiFi nội bộ, **HTTPS cert hợp lệ** (bắt buộc cho camera/QR — secure context), DNS nội bộ trỏ domain về server. MVP **same-origin** (ví dụ `https://portal.starhill.local`).

## 1. Vì sao same-origin + reverse proxy (bản chất)

- **Same-origin** (`/` guest, `/admin` admin, `/api` API, `/hubs` SignalR cùng một origin) → **không cần CORS phức tạp**, cookie `Secure; SameSite` hoạt động tự nhiên, giảm bề mặt cấu hình sai. (Docs quyết định MVP same-origin.)
- **Reverse proxy** (Nginx/Caddy/IIS) terminate TLS + phục vụ static SPA + chuyển tiếp `/api`,`/hubs` cho Kestrel → tách hạ tầng khỏi app, dễ thay cert/scale.
- **Camera/QR chỉ chạy trong secure context** (`getUserMedia` cho html5-qrcode StaffScan) → **bắt buộc HTTPS cert hợp lệ**, tránh self-signed (điện thoại chặn/cảnh báo) — Req 12.5.

## 2. Bản đồ định tuyến (routing map)

```
https://portal.starhill.local
├─ /api/**      → proxy_pass Kestrel (backend REST)
├─ /hubs/**     → proxy_pass Kestrel (WebSocket upgrade — §4)
├─ /admin, /admin/**  → static admin-web (SPA base '/admin/'), fallback admin/index.html
├─ /r/**        → static guest-web index.html  (client route /r/:token do guest SPA xử lý)
└─ /**          → static guest-web, fallback guest/index.html (SPA)
```

- `/r/{token}` là **client route của guest-web** (không phải endpoint backend) → proxy trả `guest/index.html`, SPA đọc `:token` rồi gọi `GET /api/guest/resolve/{token}`. (Backend resolve nằm ở `/api/guest/resolve/{token}`, khác `/r/...`.)
- SPA fallback (`try_files ... /index.html`) để client-side routing hoạt động khi refresh sâu.

## 3. Cấu hình mẫu (đã align ForwardedHeaders + headers)

### 3.1 Caddy (khuyến nghị cho đơn giản — auto TLS, cấu hình ngắn)

```caddy
portal.starhill.local {
    encode zstd gzip
    @api    path /api/*
    @hubs   path /hubs/*
    @admin  path /admin /admin/*
    handle @api  { reverse_proxy 127.0.0.1:5000 }
    handle @hubs { reverse_proxy 127.0.0.1:5000 }   # Caddy tự xử lý WebSocket upgrade
    handle @admin { root * /var/www/admin; try_files {path} /admin/index.html; file_server }
    handle { root * /var/www/guest; try_files {path} /index.html; file_server }
    header {
        Strict-Transport-Security "max-age=31536000"      # bật khi cert ổn định (HSTS)
        X-Content-Type-Options "nosniff"
        X-Frame-Options "DENY"
        Referrer-Policy "no-referrer"
        # CSP: siết theo build FE (§5); đặt ở proxy HOẶC app, một nơi thống nhất
    }
}
```

> **Vì sao Caddy khuyến nghị:** tự động cấp/gia hạn cert (ACME) + WebSocket không cần cấu hình thủ công → giảm sai sót vận hành. ⚠️ Nếu dùng **internal CA** (không ACME công khai), cấu hình cert thủ công hoặc ACME nội bộ — phụ thuộc quyết định nguồn cert (TK-030).

### 3.2 Nginx (nếu hạ tầng dùng Nginx) — WebSocket cần khai báo tường minh

```nginx
server {
  listen 443 ssl http2;
  server_name portal.starhill.local;
  ssl_certificate     /etc/ssl/portal.crt;
  ssl_certificate_key /etc/ssl/portal.key;
  add_header Strict-Transport-Security "max-age=31536000" always;
  add_header X-Content-Type-Options nosniff always;
  add_header X-Frame-Options DENY always;

  location /api/  { proxy_pass http://127.0.0.1:5000; include proxy_common; }
  location /hubs/ {                       # WebSocket cho SignalR — BẮT BUỘC upgrade headers
    proxy_pass http://127.0.0.1:5000;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_read_timeout 100s;              # WS dài hơi hơn request thường
    include proxy_common;
  }
  location /admin/ { root /var/www; try_files $uri /admin/index.html; }
  location /       { root /var/www/guest; try_files $uri /index.html; }
}
# proxy_common: proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
#               proxy_set_header X-Forwarded-Proto $scheme; Host $host;
```

> **Nginx WebSocket (bản chất):** SignalR dùng WebSocket → proxy **phải** chuyển `Upgrade`/`Connection: upgrade` + `proxy_http_version 1.1`, nếu không handshake WS thất bại → SignalR rớt về fallback polling liên tục (chậm). Đây là lỗi cấu hình rất phổ biến.

## 4. Align với backend (ForwardedHeaders + IP proxy)

- Proxy set `X-Forwarded-For`/`X-Forwarded-Proto`; backend bật `ForwardedHeadersMiddleware` **chỉ tin IP proxy** (`03` §8, TK-027). Sai → rate-limit theo IP hỏng + log IP sai + `Request.IsHttps` sai (cookie Secure lỗi).
- Backend Kestrel **chỉ nghe loopback/nội bộ** (127.0.0.1:5000), không expose trực tiếp ra mạng — mọi traffic qua proxy.

## 5. Security headers & CSP (một nơi thống nhất)
- Đặt security headers **ở proxy HOẶC app, không cả hai** (tránh header trùng/mâu thuẫn). Khuyến nghị: HSTS/X-Frame/X-Content-Type ở proxy; **CSP** align nội dung FE (`03`/`10` §2): `default-src 'self'; script-src 'self'; connect-src 'self' wss:; frame-ancestors 'none'`.
- `connect-src ... wss:` để WebSocket SignalR không bị CSP chặn.
- HSTS chỉ bật **khi cert đã ổn định** (bật sớm lúc cert self-signed/đổi liên tục sẽ khóa nhầm trình duyệt).

## 6. Artifact & quy trình deploy
- Backend: `dotnet publish -c Release` → chạy sau proxy (Kestrel loopback), cấu hình/secret qua env (`15`).
- FE: `vite build` (Node 24) → static (`guest`, `admin`) do **proxy phục vụ** (tách khỏi ASP.NET static — decouple, cache tốt). admin build với `base: '/admin/'`.
- **Migration (TK-011):** áp bằng **bước deploy riêng** (`dotnet ef database update` hoặc **migration bundle**) — **KHÔNG** auto-migrate lúc app khởi động ở prod (tránh nhiều instance cùng migrate/nửa chừng). Seed idempotent chạy sau migration.
- Static asset: cache-control dài cho file có hash (Vite), `no-cache` cho `index.html`.

## 7. Scale-out (đường mở, MVP single-instance)
- MVP **một instance** đủ cho tải resort. Nếu scale nhiều instance sau này:
  - SignalR cần **backplane** (Redis) hoặc sticky sessions — `21` §7.
  - Rate limiter cần **store phân tán** (hiện in-memory theo instance — `03` §5).
  - `VisitIdleSweeper` cần chống chạy trùng (leader election/lock) — hiện an toàn vì single-instance.
- Ghi TK-031: các thay đổi khi scale-out (không làm ở base, nhưng thiết kế không cản).

## 8. Truy vết
- **Validates: Requirements 12.5, 13.3** + docs Deployment (same-origin, HTTPS, DNS nội bộ).
- Align: `03` §7–8 (headers/proxy IP), `15` (config/secret), `21` (WebSocket/SignalR).
