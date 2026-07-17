<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import Button from 'primevue/button';
import Message from 'primevue/message';
import { api, type DashboardStats } from '../api/client';
import { useAuthStore } from '../stores/auth';

const { t } = useI18n();
const auth = useAuthStore();

const stats = ref<DashboardStats | null>(null);
const error = ref<string | null>(null);
const loading = ref(true);

async function load(): Promise<void> {
  loading.value = true;
  error.value = null;
  try {
    stats.value = await api.getDashboardStats(auth.accessToken);
  } catch {
    error.value = t('dashboard.loadError');
  } finally {
    loading.value = false;
  }
}

onMounted(load);

interface Kpi {
  key: string;
  icon: string;
  value: (s: DashboardStats) => number;
  accent: string;
  tint: string;
}

// accent = màu icon; tint = nền tròn icon (mờ). Bố cục KPI học từ dashboard admin phổ biến, tự dựng bằng PrimeVue-less
// card thuần token (không phụ thuộc theming ngoài) để kiểm soát dark/light hoàn toàn.
const kpis: Kpi[] = [
  { key: 'unread', icon: 'pi-inbox', value: (s) => s.unreadConversations, accent: '#ef4444', tint: 'rgba(239,68,68,0.12)' },
  { key: 'openConversations', icon: 'pi-comments', value: (s) => s.openConversations, accent: '#6366f1', tint: 'rgba(99,102,241,0.12)' },
  { key: 'openTickets', icon: 'pi-sparkles', value: (s) => s.openHousekeepingTickets, accent: '#f59e0b', tint: 'rgba(245,158,11,0.14)' },
  { key: 'activeRooms', icon: 'pi-qrcode', value: (s) => s.activeRooms, accent: '#10b981', tint: 'rgba(16,185,129,0.13)' },
  { key: 'acksToday', icon: 'pi-verified', value: (s) => s.rulesAcksToday, accent: '#0ea5e9', tint: 'rgba(14,165,233,0.13)' },
];

const greeting = computed(() => t('dashboard.welcome', { name: auth.username ?? '' }).trim());
</script>

<template>
  <section class="dash">
    <div class="dash__head">
      <div>
        <h2 class="dash__title">{{ t('dashboard.title') }}</h2>
        <p class="dash__subtitle">{{ greeting }}</p>
      </div>
      <Button icon="pi pi-refresh" :label="t('dashboard.refresh')" outlined :loading="loading" @click="load" />
    </div>

    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>

    <div class="dash__grid">
      <article v-for="k in kpis" :key="k.key" class="kpi">
        <span class="kpi__icon" :style="{ color: k.accent, background: k.tint }">
          <i :class="['pi', k.icon]" aria-hidden="true"></i>
        </span>
        <div class="kpi__body">
          <div class="kpi__value" :data-testid="`kpi-${k.key}`">{{ stats ? k.value(stats) : '—' }}</div>
          <div class="kpi__label">{{ t(`dashboard.${k.key}`) }}</div>
        </div>
      </article>
    </div>
  </section>
</template>

<style scoped>
.dash {
  display: flex;
  flex-direction: column;
  gap: var(--sh-gap);
}
.dash__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--sh-gap);
  flex-wrap: wrap;
}
.dash__title {
  margin: 0;
  font-size: var(--sh-font-title);
  font-weight: 800;
  letter-spacing: -0.01em;
  color: var(--sh-text);
}
.dash__subtitle {
  margin: 0.15rem 0 0;
  color: var(--sh-text-muted);
  font-size: 0.9rem;
}
/* auto-fit: 1 cột phone → nhiều cột tablet/desktop, KHÔNG media query. */
.dash__grid {
  display: grid;
  gap: var(--sh-gap);
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 14rem), 1fr));
}
.kpi {
  display: flex;
  align-items: center;
  gap: 1rem;
  min-width: 0;
  padding: 1.15rem 1.25rem;
  background: var(--sh-surface-card);
  border: 1px solid var(--sh-border);
  border-radius: var(--sh-radius);
  box-shadow: var(--sh-shadow-card);
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}
.kpi:hover {
  transform: translateY(-2px);
  box-shadow: var(--sh-shadow-pop);
}
.kpi__icon {
  display: inline-grid;
  place-items: center;
  width: 3rem;
  height: 3rem;
  flex: 0 0 auto;
  border-radius: 0.8rem;
  font-size: 1.4rem;
}
.kpi__body {
  min-width: 0;
}
.kpi__value {
  font-size: 2rem;
  font-weight: 800;
  line-height: 1.05;
  color: var(--sh-text);
}
.kpi__label {
  margin-top: 0.15rem;
  color: var(--sh-text-muted);
  font-size: 0.85rem;
}
</style>
