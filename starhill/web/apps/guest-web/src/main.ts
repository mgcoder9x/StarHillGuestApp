import { createApp } from 'vue';
import PrimeVue from 'primevue/config';
import Aura from '@primevue/themes/aura';
import App from './App.vue';
import './style.css';

// PrimeVue 4.x (MIT — pin <5 vì v5 đổi license, QR-AD FE). Styled mode + preset Aura; dark mode qua class `.dark`.
createApp(App)
  .use(PrimeVue, {
    theme: {
      preset: Aura,
      options: { darkModeSelector: '.dark' },
    },
  })
  .mount('#app');
