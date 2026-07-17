import { createApp } from 'vue';
import PrimeVue from 'primevue/config';
import Aura from '@primevue/themes/aura';
import 'primeicons/primeicons.css';
import App from './App.vue';
import { router } from './router';
import { i18n } from './i18n';
import './style.css';

// PrimeVue 4.x (MIT — pin <5 vì v5 đổi license, QR-AD-047). Styled Aura; dark mode qua class `.dark`.
createApp(App)
  .use(router)
  .use(i18n)
  .use(PrimeVue, {
    theme: {
      preset: Aura,
      options: { darkModeSelector: '.dark' },
    },
  })
  .mount('#app');
