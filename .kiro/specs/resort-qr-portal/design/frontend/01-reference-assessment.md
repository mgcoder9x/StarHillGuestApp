# 01 — Khảo sát `Reference/EPS.Vuexy` (ĐÃ KIỂM CHỨNG)

> **File authoritative cho:** đánh giá reference frontend. Mỗi khẳng định dẫn chứng file cụ thể. Ngày kiểm chứng: 2026-07-03.

## 1. Bằng chứng đã đọc (evidence log)

| # | Khẳng định | Kết quả | File | Bằng chứng |
|---|-----------|---------|------|-----------|
| FE-E1 | Là template **Vuexy** thương mại, không phải code viết riêng | ✅ | `package.json` | `"name": "vuexy-vuejs-react-html-laravel-admin-dashboard-template"`, `"version": "6.4.0"` |
| FE-E2 | **Vue 2.6.11** (EOL từ 12/2023) | ✅ | `package.json`, `src/main.js` | `"vue": "2.6.11"`, `vue-template-compiler`, `import Vue from 'vue'` + `new Vue({...}).$mount('#app')` |
| FE-E3 | **JavaScript**, không TypeScript | ✅ | `jsconfig.json`, `src/main.js` | Toàn bộ `.js`, `babel.config.js`, không có `tsconfig` cho src |
| FE-E4 | **Vue CLI 4.5.9 (webpack)**, không Vite | ✅ | `package.json`, `vue.config.js` | `@vue/cli-service ~4.5.9`, script `vue-cli-service serve/build` |
| FE-E5 | **Vuex 3** (không Pinia) | ✅ | `package.json` | `"vuex": "3.6.0"` |
| FE-E6 | UI kit **Bootstrap 4 + bootstrap-vue 2.21** | ✅ | `package.json`, `src/main.js` | `bootstrap-vue 2.21.1`, `Vue.use(BootstrapVue)` |
| FE-E7 | Đăng ký component **toàn cục hàng loạt** (hại tree-shaking/bundle) | ✅ | `src/main.js` | Chuỗi dài `Vue.component('BasicTable', ...)`, `Vue.prototype.$services = useJwt` |
| FE-E8 | Grab-bag nặng, trộn nhiều thư viện trùng chức năng | ✅ | `package.json` | 3 lib chart (`apexcharts`,`chart.js`,`echarts`), video (`jsmpeg`,`hls.js`,`flvPlayer`), map (`leaflet`), `express`+`ffmpeg-static` trong FE |
| FE-E9 | Chất lượng cấu hình cẩu thả | ✅ | `package.json` | Key trùng lặp: `file-saver` khai báo 2 lần, `uuid` 2 lần |
| FE-E10 | Dynamic iframe routes nạp từ `config.json` runtime | ✅ | `src/main.js` | fetch `config.json` → `router.addRoutes(dynamicRoutes)` render `IFrameSystemInfo.vue` |
| FE-E11 | Có sẵn `@microsoft/signalr` và `socket.io-client` (realtime) | ✅ | `package.json` | `@microsoft/signalr ^8.0.7`, `socket.io-client ^4.8.1` |
| FE-E12 | Phân quyền bằng **CASL** | ✅ | `package.json`, `src/main.js` | `@casl/ability`, `@casl/vue`, `import '@/libs/acl'` |
| FE-E13 | i18n bằng **vue-i18n 8** (bản Vue 2) | ✅ | `package.json`, `src/main.js` | `vue-i18n 8.22.2`, `import i18n from '@/libs/i18n'` |
| FE-E14 | Được host bởi ASP.NET (không phải SPA thuần tách rời) | ✅ | thư mục gốc | `Program.cs`, `Startup.cs`, `Vuexy.csproj`, `web.config` |

## 2. Bản chất reference

Đây là **template Vuexy (Vue 2) mua sẵn**, bị nhồi thêm rất nhiều thư viện cho một ứng dụng giám sát/IoT (video player, map, serial port, telerik report), **host trong ASP.NET**, viết bằng **JavaScript trên Vue CLI/webpack**. Đây là **thế hệ công nghệ cũ và đã EOL** (Vue 2), khác hẳn mục tiêu base (Vue 3 + TS + Vite + Pinia).

## 3. Phán quyết: giữ Ý TƯỞNG gì / bỏ gì

| Khía cạnh | Reference | Base mới | Lý do |
|---|---|---|---|
| Vue version | Vue 2.6 (EOL) | **Vue 3** | Vue 2 hết hỗ trợ; commercial lâu dài phải Vue 3 |
| Ngôn ngữ | JavaScript | **TypeScript** | An toàn kiểu, đồng bộ hợp đồng backend, bảo trì lâu dài |
| Build | Vue CLI/webpack | **Vite** | Nhanh, hiện đại, HMR tốt, chuẩn Vue 3 |
| State | Vuex 3 | **Pinia** | Chuẩn mới, TS-first, gọn |
| UI | Bootstrap-Vue 2 (EOL) | **Element Plus** (admin) / tối giản (guest) | Bootstrap-Vue 2 EOL; guest cần bundle nhẹ (PrimeVue bị loại do repo archive — xem `../technology-stack.md`) |
| Kiến trúc SPA | 1 app khổng lồ host trong .NET | **2 SPA tách rời** (guest/admin) | Tách bundle + bề mặt; guest nhẹ |
| Đăng ký component | Toàn cục hàng loạt (FE-E7) | Import cục bộ + auto-import có kiểm soát | Tree-shaking, bundle nhỏ |
| Phân quyền | CASL | **Giữ Ý TƯỞNG** permission-based; base dùng role guard đơn giản, CASL tùy chọn sau | Đủ cho Admin/Staff; tránh phức tạp sớm |
| Realtime | signalr + socket.io (thừa) | Chỉ **@microsoft/signalr** (khớp backend Hub) | Backend dùng SignalR; bỏ socket.io thừa |
| Routing động | iframe từ config.json (FE-E10) | KHÔNG | Anti-pattern bảo mật/bảo trì |
| Host | ASP.NET host SPA | Static build sau reverse proxy | Tách biệt, đúng docs deployment |

**Kết luận:** **KHÔNG port code** từ `EPS.Vuexy`. Chỉ kế thừa vài **ý tưởng**: cấu trúc layout admin (navigation/breadcrumb), khái niệm permission-based access (CASL → role guard), tách `@core` vs app code, dùng SignalR cho realtime, i18n tách UI-text vs content.

## 4. Chưa kiểm chứng (trung thực)

- Chi tiết `src/views/*`, `src/store/*`, `src/services/*`, `src/@core/*` chưa đọc từng file — nhưng đủ cơ sở kết luận về stack/thế hệ để quyết định "không port". Nếu cần mượn một pattern layout cụ thể sẽ đọc file tương ứng và ghi bằng chứng khi đó.
