import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import tailwindcss from '@tailwindcss/vite';

// Admin Dashboard (SPA thứ 2). Dev: Vite proxy same-origin-giả tới Host (/v1 REST, /hubs SignalR ws). Prod: reverse
// proxy /admin (FE.6). Port 5174 (guest-web dùng 5173) để chạy song song khi dev.
export default defineConfig({
  plugins: [vue(), tailwindcss()],
  server: {
    port: 5174,
    strictPort: true,
    proxy: {
      '/v1': { target: 'http://localhost:18080', changeOrigin: true },
      '/hubs': { target: 'http://localhost:18080', changeOrigin: true, ws: true },
    },
  },
});
