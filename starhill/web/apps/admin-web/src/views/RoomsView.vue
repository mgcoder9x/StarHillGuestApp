<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import DataTable, { type DataTablePageEvent } from 'primevue/datatable';
import Column from 'primevue/column';
import Select from 'primevue/select';
import Tag from 'primevue/tag';
import Button from 'primevue/button';
import Message from 'primevue/message';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import { ApiError, api, type RoomListItem, type RoomStatus } from '../api/client';
import { useAuthStore } from '../stores/auth';
import RoomQrDialog from '../components/RoomQrDialog.vue';
import RoomFormDialog from '../components/RoomFormDialog.vue';

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
const success = ref<string | null>(null);

const qrVisible = ref(false);
const qrRoom = ref<RoomListItem | null>(null);
const formVisible = ref(false);
const editingRoom = ref<RoomListItem | null>(null);
const statusVisible = ref(false);
const statusRoom = ref<RoomListItem | null>(null);
const statusValue = ref<RoomStatus>('Active');
const confirmKind = ref<'delete' | 'rotate' | null>(null);
const confirmRoom = ref<RoomListItem | null>(null);
const rotateReason = ref('');
const mutationLoading = ref(false);

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

function mutationError(cause: unknown): string {
  if (cause instanceof ApiError) {
    if (cause.code === 'validation_error') return t('rooms.errors.numberTaken');
    if (cause.code === 'not_found') return t('rooms.errors.notFound');
    if (cause.code === 'qr_generation_failed') return t('rooms.errors.qrGeneration');
    if (cause.code === 'invalid_configuration' || cause.code === 'resort_not_found') return t('rooms.errors.configuration');
  }
  return t('rooms.errors.action');
}

function openCreate(): void {
  editingRoom.value = null;
  formVisible.value = true;
}

function openEdit(room: RoomListItem): void {
  editingRoom.value = room;
  formVisible.value = true;
}

function openStatus(room: RoomListItem): void {
  statusRoom.value = room;
  statusValue.value = room.status;
  statusVisible.value = true;
}

async function saveStatus(): Promise<void> {
  if (!statusRoom.value) return;
  mutationLoading.value = true;
  error.value = null;
  try {
    await api.changeRoomStatus(statusRoom.value.roomId, statusValue.value, auth.accessToken);
    success.value = t('rooms.success.status');
    statusVisible.value = false;
    await load();
  } catch (cause) {
    error.value = mutationError(cause);
  } finally {
    mutationLoading.value = false;
  }
}

function openConfirm(kind: 'delete' | 'rotate', room: RoomListItem): void {
  confirmKind.value = kind;
  confirmRoom.value = room;
  rotateReason.value = '';
}

function closeConfirm(): void {
  if (!mutationLoading.value) {
    confirmKind.value = null;
    confirmRoom.value = null;
    rotateReason.value = '';
  }
}

async function confirmAction(): Promise<void> {
  if (!confirmKind.value || !confirmRoom.value) return;
  mutationLoading.value = true;
  error.value = null;
  let completed = false;
  try {
    if (confirmKind.value === 'delete') {
      await api.deleteRoom(confirmRoom.value.roomId, auth.accessToken);
      success.value = t('rooms.success.deleted');
    } else {
      await api.rotateRoomToken(confirmRoom.value.roomId, rotateReason.value.trim() || null, auth.accessToken);
      success.value = t('rooms.success.rotated');
    }
    await load();
    completed = true;
  } catch (cause) {
    error.value = mutationError(cause);
  } finally {
    mutationLoading.value = false;
    if (completed) closeConfirm();
  }
}

async function onFormSaved(mode: 'created' | 'updated'): Promise<void> {
  success.value = t(`rooms.success.${mode}`);
  await load();
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
  if (room.status !== 'Active' || !room.activeTokenPreview) return;
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
      <div class="rooms__tools">
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
        <Button v-if="auth.isAdmin" icon="pi pi-plus" :label="t('rooms.create')" @click="openCreate" />
      </div>
    </div>

    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>
    <Message v-if="success" severity="success" :closable="true" @close="success = null">{{ success }}</Message>

    <p class="rooms__swipe-hint"><i class="pi pi-arrows-h" aria-hidden="true"></i> {{ t('rooms.swipeHint') }}</p>
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
            <div class="rooms__actions">
              <Button
                icon="pi pi-qrcode"
                text
                rounded
                size="small"
                :aria-label="t('rooms.viewQr')"
                :disabled="data.status !== 'Active' || !data.activeTokenPreview"
                @click="openQr(data)"
              />
              <template v-if="auth.isAdmin">
                <Button icon="pi pi-pencil" text rounded size="small" :aria-label="t('rooms.edit')" @click="openEdit(data)" />
                <Button icon="pi pi-sliders-h" text rounded size="small" :aria-label="t('rooms.changeStatus')" @click="openStatus(data)" />
                <Button
                  icon="pi pi-refresh"
                  text
                  rounded
                  size="small"
                  :aria-label="t('rooms.rotate')"
                  :disabled="data.status !== 'Active'"
                  @click="openConfirm('rotate', data)"
                />
                <Button icon="pi pi-trash" text rounded severity="danger" size="small" :aria-label="t('rooms.delete')" @click="openConfirm('delete', data)" />
              </template>
            </div>
          </template>
        </Column>
      </DataTable>
    </div>

    <RoomQrDialog v-model:visible="qrVisible" :room="qrRoom" />
    <RoomFormDialog v-model:visible="formVisible" :room="editingRoom" @saved="onFormSaved" />

    <Dialog
      v-model:visible="statusVisible"
      modal
      :draggable="false"
      :style="{ width: 'min(94vw, 24rem)' }"
      :header="statusRoom ? t('rooms.statusDialogTitle', { room: statusRoom.roomNumber }) : ''"
    >
      <div class="rooms__status-form">
        <label for="room-status">{{ t('rooms.status') }}</label>
        <Select input-id="room-status" v-model="statusValue" :options="statusOptions" option-label="label" option-value="value" fluid />
      </div>
      <template #footer>
        <Button :label="t('common.cancel')" text :disabled="mutationLoading" @click="statusVisible = false" />
        <Button icon="pi pi-check" :label="t('common.save')" :loading="mutationLoading" @click="saveStatus" />
      </template>
    </Dialog>

    <Dialog
      :visible="confirmKind !== null"
      modal
      :draggable="false"
      :closable="!mutationLoading"
      :dismissable-mask="!mutationLoading"
      :style="{ width: 'min(94vw, 28rem)' }"
      :header="confirmKind === 'delete' ? t('rooms.confirm.deleteTitle') : t('rooms.confirm.rotateTitle')"
      @update:visible="(visible) => !visible && closeConfirm()"
    >
      <p class="rooms__confirm-copy">
        {{ confirmKind === 'delete' ? t('rooms.confirm.deleteBody', { room: confirmRoom?.roomNumber }) : t('rooms.confirm.rotateBody', { room: confirmRoom?.roomNumber }) }}
      </p>
      <div v-if="confirmKind === 'rotate'" class="rooms__status-form">
        <label for="rotate-reason">{{ t('rooms.confirm.reason') }}</label>
        <InputText id="rotate-reason" v-model="rotateReason" maxlength="200" :placeholder="t('rooms.confirm.reasonPlaceholder')" fluid />
      </div>
      <template #footer>
        <Button :label="t('common.cancel')" text :disabled="mutationLoading" @click="closeConfirm" />
        <Button
          :icon="confirmKind === 'delete' ? 'pi pi-trash' : 'pi pi-refresh'"
          :severity="confirmKind === 'delete' ? 'danger' : undefined"
          :label="confirmKind === 'delete' ? t('rooms.confirm.deleteAction') : t('rooms.confirm.rotateAction')"
          :loading="mutationLoading"
          @click="confirmAction"
        />
      </template>
    </Dialog>
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
.rooms__tools {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  flex-wrap: wrap;
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
.rooms__table :deep(.p-datatable-table) {
  min-width: 52rem;
}
.rooms__table :deep(.p-datatable-thead > tr > th),
.rooms__table :deep(.p-datatable-tbody > tr > td) {
  white-space: nowrap;
}
.rooms__swipe-hint {
  display: none;
  align-items: center;
  gap: 0.4rem;
  margin: 0;
  color: var(--sh-text-muted);
  font-size: 0.78rem;
}
.rooms__token code {
  font-size: 0.85rem;
}
.rooms__muted,
.rooms__empty {
  color: var(--sh-text-muted);
}
.rooms__actions {
  display: flex;
  align-items: center;
  gap: 0.1rem;
  min-width: max-content;
}
.rooms__status-form {
  display: grid;
  gap: 0.4rem;
}
.rooms__status-form label {
  color: var(--sh-text);
  font-size: 0.85rem;
  font-weight: 700;
}
.rooms__confirm-copy {
  margin: 0 0 1rem;
  color: var(--sh-text-muted);
}
@container (max-width: 600px) {
  .rooms__swipe-hint {
    display: flex;
  }
}
</style>
