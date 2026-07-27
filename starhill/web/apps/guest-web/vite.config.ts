import { readFileSync } from 'node:fs';
import { defineConfig, loadEnv } from 'vite';
import vue from '@vitejs/plugin-vue';
import tailwindcss from '@tailwindcss/vite';
import { assertMockNotBundled } from '../../tooling/assertMockNotBundled';

// Dev: same-origin giả lập bằng Vite proxy (KHÔNG bật CORS ở BE — QR-AD deploy same-origin).
// Prod: reverse proxy / Host-static phục vụ cùng origin (FE.6). /v1 = REST, /hubs = SignalR (ws).
const devHttpsPfx = process.env.STARHILL_DEV_HTTPS_PFX;
const devHttps = devHttpsPfx
  ? {
      pfx: readFileSync(devHttpsPfx),
      passphrase: process.env.STARHILL_DEV_HTTPS_PASSWORD,
    }
  : undefined;

export default defineConfig(({ command, mode }) => {
  // FAIL-CLOSED (QR-N-089): mock là năng lực CHỈ dev-server. `vite build` với cờ mock bật (qua .env* HOẶC biến
  // shell — Vite đọc CẢ process.env VITE_*) từng ship dữ liệu bịa vào bundle guest (đo được: mock-data lọt dist).
  // Chặn tận gốc ở đây thay vì dựa quy ước "đừng set biến đó".
  assertMockNotBundled({ command, mode, appName: 'guest-web', fileEnv: loadEnv(mode, process.cwd(), 'VITE_') });

  return {
    plugins: [vue(), tailwindcss()],
    server: {
      port: 5173,
      strictPort: true,
      https: devHttps,
      proxy: {
        '/v1': { target: 'http://localhost:18080', changeOrigin: true },
        '/hubs': { target: 'http://localhost:18080', changeOrigin: true, ws: true },
      },
    },
  };
});
