<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import DataTable, { type DataTablePageEvent } from 'primevue/datatable';
import Column from 'primevue/column';
import Select from 'primevue/select';
import Tag from 'primevue/tag';
import Button from 'primevue/button';
import Message from 'primevue/message';
import { api, type RoomListItem, type RoomStatus } from '../api/client';
import { useAuthStore } from '../stores/auth';
import RoomQrDialog from '../components/RoomQrDialog.vue';

const { t } = useI18n();
const auth = useAuthStore();

const items = ref<RoomListItem[]>([]);
const total = ref(0);
const page = ref(1);
const pageSize = ref(20);
const first = ref(0);
const statusFilter = ref<RoomStatus | null>(null);
const loading = ref(true);
const error = ref<string | null>(null);

const qrVisible = ref(false);
const qrRoom = ref<RoomListItem | null>(null);

interface StatusOption {
  label: string;
  value: RoomStatus;
}
const statusOptions: StatusOption[] = [
  { label: t('rooms.statusActive'), value: 'Active' },
  { label: t('rooms.statusInactive'), value: 'Inactive' },
  { label: t('rooms.statusMaintenance'), value: 'Maintenance' },
];

// Severity Tag PrimeVue: success|secondary|warn theo trạng thái phòng.
function statusSeverity(s: RoomStatus): 'success' | 'secondary' | 'warn' {
  return s === 'Active' ? 'success' : s === 'Maintenance' ? 'warn' : 'secondary';
}
function statusLabel(s: RoomStatus): string {
  return t(`rooms.status${s}`);
}

async function load(): Promise<void> {
  loading.value = true;
  error.value = null;
  try {
    const result = await api.listRooms(statusFilter.value, page.value, pageSize.value, auth.accessToken);
    items.value = result.items;
    total.value = result.total;
  } catch {
    error.value = t('rooms.loadError');
    items.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
  }
}

function onPage(event: DataTablePageEvent): void {
  first.value = event.first;
  page.value = event.page + 1;
  pageSize.value = event.rows;
  void load();
}

function onFilterChange(): void {
  page.value = 1;
  first.value = 0;
  void load();
}

function openQr(room: RoomListItem): void {
  qrRoom.value = room;
  qrVisible.value = true;
}

onMounted(load);
</script>

<template>
  <section class="rooms">
    <div class="rooms__head">
      <div>
        <h2 class="rooms__title">{{ t('rooms.title') }}</h2>
        <p class="rooms__subtitle">{{ t('rooms.subtitle') }}</p>
      </div>
      <Select
        v-model="statusFilter"
        :options="statusOptions"
        option-label="label"
        option-value="value"
        :placeholder="t('rooms.filterAll')"
        show-clear
        class="rooms__filter"
        @change="onFilterChange"
      />
    </div>

    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>

    <div class="rooms__table">
      <DataTable
        :value="items"
        :lazy="true"
        :paginator="true"
        :rows="pageSize"
        :total-records="total"
        :first="first"
        :loading="loading"
        data-key="roomId"
        paginator-position="bottom"
        @page="onPage"
      >
        <template #empty>
          <span class="rooms__empty">{{ t('rooms.empty') }}</span>
        </template>

        <Column field="roomNumber" :header="t('rooms.number')" />

        <Column :header="t('rooms.location')">
          <template #body="{ data }">
            <span>{{ [data.building, data.floor != null ? `T${data.floor}` : null].filter(Boolean).join(' · ') || '—' }}</span>
          </template>
        </Column>

        <Column :header="t('rooms.status')">
          <template #body="{ data }">
            <Tag :value="statusLabel(data.status)" :severity="statusSeverity(data.status)" />
          </template>
        </Column>

        <Column :header="t('rooms.qr')">
          <template #body="{ data }">
            <span v-if="data.activeTokenPreview" class="rooms__token">
              <code>{{ data.activeTokenPreview }}</code> · v{{ data.activeTokenVersion }}
            </span>
            <span v-else class="rooms__muted">—</span>
          </template>
        </Column>

        <Column :header="t('rooms.actions')">
          <template #body="{ data }">
            <Button
              icon="pi pi-qrcode"
              :label="t('rooms.viewQr')"
              text
              size="small"
              :disabled="!data.activeTokenPreview"
              @click="openQr(data)"
            />
          </template>
        </Column>
      </DataTable>
    </div>

    <RoomQrDialog v-model:visible="qrVisible" :room="qrRoom" />
  </section>
</template>

<style scoped>
.rooms {
  display: flex;
  flex-direction: column;
  gap: var(--sh-gap);
}
.rooms__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--sh-gap);
  flex-wrap: wrap;
}
.rooms__title {
  margin: 0;
  font-size: var(--sh-font-title);
  font-weight: 800;
  letter-spacing: -0.01em;
  color: var(--sh-text);
}
.rooms__subtitle {
  margin: 0.15rem 0 0;
  color: var(--sh-text-muted);
  font-size: 0.9rem;
}
.rooms__filter {
  min-width: 12rem;
}
/* Bảng data-dense: cuộn ngang CỤC BỘ trong wrapper (min-width:0 chống đẩy tràn trang — §3.5). */
.rooms__table {
  min-width: 0;
  overflow-x: auto;
  background: var(--sh-surface-card);
  border: 1px solid var(--sh-border);
  border-radius: var(--sh-radius);
  box-shadow: var(--sh-shadow-card);
}
.rooms__token code {
  font-size: 0.85rem;
}
.rooms__muted,
.rooms__empty {
  color: var(--sh-text-muted);
}
</style>
