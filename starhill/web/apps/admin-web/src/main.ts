import { createApp } from 'vue';
import { createPinia } from 'pinia';
import PrimeVue from 'primevue/config';
import Aura from '@primevue/themes/aura';
import 'primeicons/primeicons.css';
import App from './App.vue';
import { router } from './router';
import { i18n } from './i18n';
import './style.css';

// Pinia TRƯỚC router (guard beforeEach dùng useAuthStore). PrimeVue 4.x MIT (pin <5) styled Aura.
createApp(App)
  .use(createPinia())
  .use(router)
  .use(i18n)
  .use(PrimeVue, {
    theme: {
      preset: Aura,
      options: { darkModeSelector: '.dark' },
    },
  })
  .mount('#app');
