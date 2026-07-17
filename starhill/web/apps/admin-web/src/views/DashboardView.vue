<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import Card from 'primevue/card';
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
}

const kpis: Kpi[] = [
  { key: 'unread', icon: 'pi-inbox', value: (s) => s.unreadConversations, accent: '#ef4444' },
  { key: 'openConversations', icon: 'pi-comments', value: (s) => s.openConversations, accent: '#6366f1' },
  { key: 'openTickets', icon: 'pi-sparkles', value: (s) => s.openHousekeepingTickets, accent: '#f59e0b' },
  { key: 'activeRooms', icon: 'pi-qrcode', value: (s) => s.activeRooms, accent: '#10b981' },
  { key: 'acksToday', icon: 'pi-verified', value: (s) => s.rulesAcksToday, accent: '#0ea5e9' },
];
</script>

<template>
  <section class="dash">
    <div class="dash__head">
      <h2 class="dash__title">{{ t('dashboard.title') }}</h2>
      <Button icon="pi pi-refresh" :label="t('dashboard.refresh')" text :loading="loading" @click="load" />
    </div>

    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>

    <div class="dash__grid">
      <Card v-for="k in kpis" :key="k.key" class="kpi">
        <template #content>
          <div class="kpi__row">
            <span class="kpi__icon" :style="{ color: k.accent }"><i :class="['pi', k.icon]" aria-hidden="true"></i></span>
            <div class="kpi__body">
              <div class="kpi__value" :data-testid="`kpi-${k.key}`">{{ stats ? k.value(stats) : '—' }}</div>
              <div class="kpi__label">{{ t(`dashboard.${k.key}`) }}</div>
            </div>
          </div>
        </template>
      </Card>
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
  align-items: center;
  justify-content: space-between;
  gap: var(--sh-gap);
  flex-wrap: wrap;
}
.dash__title {
  margin: 0;
  font-size: var(--sh-font-title);
  font-weight: 700;
}
/* auto-fit: 1 cột phone → nhiều cột tablet/desktop, KHÔNG media query. */
.dash__grid {
  display: grid;
  gap: var(--sh-gap);
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 13rem), 1fr));
}
.kpi__row {
  display: flex;
  align-items: center;
  gap: 1rem;
}
.kpi__icon {
  font-size: 2rem;
}
.kpi__value {
  font-size: 1.9rem;
  font-weight: 800;
  line-height: 1.1;
}
.kpi__label {
  color: var(--p-text-muted-color, #64748b);
  font-size: 0.85rem;
}
</style>
