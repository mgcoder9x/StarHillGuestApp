import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import tailwindcss from '@tailwindcss/vite';

// Dev: same-origin giả lập bằng Vite proxy (KHÔNG bật CORS ở BE — QR-AD deploy same-origin).
// Prod: reverse proxy / Host-static phục vụ cùng origin (FE.6). /v1 = REST, /hubs = SignalR (ws).
export default defineConfig({
  plugins: [vue(), tailwindcss()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/v1': { target: 'http://localhost:18080', changeOrigin: true },
      '/hubs': { target: 'http://localhost:18080', changeOrigin: true, ws: true },
    },
  },
});
