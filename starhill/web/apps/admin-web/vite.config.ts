import { defineConfig, loadEnv } from 'vite';
import vue from '@vitejs/plugin-vue';
import tailwindcss from '@tailwindcss/vite';
import { assertMockNotBundled } from '../../tooling/assertMockNotBundled';

// Admin Dashboard (SPA thứ 2). Dev: Vite proxy same-origin-giả tới Host (/v1 REST, /hubs SignalR ws). Prod: reverse
// proxy /admin (FE.6). Port 5174 (guest-web dùng 5173) để chạy song song khi dev.
export default defineConfig(({ command, mode }) => {
  // FAIL-CLOSED (QR-N-089) — xem `web/build/assertMockNotBundled.ts`: mock chỉ dành cho dev-server; build với cờ
  // mock bật (từ .env* HOẶC biến shell) sẽ ship dữ liệu bịa → chặn cứng tại đây.
  assertMockNotBundled({ command, mode, appName: 'admin-web', fileEnv: loadEnv(mode, process.cwd(), 'VITE_') });

  return {
    plugins: [vue(), tailwindcss()],
    server: {
      port: 5174,
      strictPort: true,
      proxy: {
        '/v1': { target: 'http://localhost:18080', changeOrigin: true },
        '/hubs': { target: 'http://localhost:18080', changeOrigin: true, ws: true },
      },
    },
  };
});
