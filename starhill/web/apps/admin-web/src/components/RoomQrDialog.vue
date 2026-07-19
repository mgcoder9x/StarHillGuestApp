<script setup lang="ts">
import { computed, ref, watch, onBeforeUnmount } from 'vue';
import { useI18n } from 'vue-i18n';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Message from 'primevue/message';
import { api, type RoomListItem } from '../api/client';
import { useAuthStore } from '../stores/auth';
import BrandMark from './BrandMark.vue';

// Dialog xem QR của phòng. QR endpoint (qr.png) RequireStaff → cần Bearer → fetch blob→objectURL (không <img src>).
// Revoke objectURL khi đóng/unmount (chống rò bộ nhớ). Mock mode trả data-URL SVG (không cần revoke).
const props = defineProps<{ visible: boolean; room: RoomListItem | null }>();
const emit = defineEmits<{ 'update:visible': [boolean] }>();

const { t } = useI18n();
const auth = useAuthStore();

const src = ref<string | null>(null);
const loading = ref(false);
const error = ref<string | null>(null);
const roomLocation = computed(() => {
  if (!props.room) return '';
  return [props.room.building, props.room.floor === null ? null : `T${props.room.floor}`].filter(Boolean).join(' · ');
});

function revoke(): void {
  // Chỉ objectURL (blob:) mới cần revoke; data-URL (mock) thì không.
  if (src.value && src.value.startsWith('blob:')) {
    URL.revokeObjectURL(src.value);
  }
  src.value = null;
}

async function loadQr(room: RoomListItem): Promise<void> {
  revoke();
  error.value = null;
  // Phòng không có token Active → không có QR để render (BE cũng trả lỗi). Báo rõ, không gọi mạng thừa.
  if (!room.activeTokenPreview) {
    error.value = t('rooms.noToken');
    return;
  }
  loading.value = true;
  try {
    src.value = await api.getRoomQrObjectUrl(room.roomId, auth.accessToken);
  } catch {
    error.value = t('rooms.qrError');
  } finally {
    loading.value = false;
  }
}

// Nạp QR khi dialog mở với một phòng; dọn khi đóng.
watch(
  () => [props.visible, props.room?.roomId] as const,
  ([visible]) => {
    if (visible && props.room) {
      void loadQr(props.room);
    } else if (!visible) {
      revoke();
      error.value = null;
    }
  },
);

onBeforeUnmount(revoke);

function close(): void {
  emit('update:visible', false);
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    :draggable="false"
    :dismissable-mask="true"
    :style="{ width: 'min(94vw, 29rem)' }"
    :header="room ? t('rooms.qrDialogTitle', { room: room.roomNumber }) : ''"
    @update:visible="emit('update:visible', $event)"
  >
    <div class="qr">
      <Message v-if="error" severity="warn" :closable="false">{{ error }}</Message>
      <article v-else class="qr__card">
        <header class="qr__brand-row">
          <div class="qr__brand">
            <BrandMark />
            <div>
              <strong>Star Hill</strong>
              <span>{{ t('rooms.qrPortal') }}</span>
            </div>
          </div>
          <span class="qr__badge">{{ t('rooms.qrDemo') }}</span>
        </header>

        <div class="qr__room-row">
          <div>
            <span class="qr__eyebrow">ROOM</span>
            <strong>{{ room?.roomNumber }}</strong>
          </div>
          <span v-if="roomLocation" class="qr__location">{{ roomLocation }}</span>
        </div>

        <div class="qr__frame">
          <img
            v-if="src"
            :src="src"
            :alt="room ? t('rooms.qrDialogTitle', { room: room.roomNumber }) : 'QR'"
            class="qr__img"
            data-testid="room-qr-img"
          />
          <span v-else class="qr__loading">{{ t('rooms.qrLoading') }}</span>
        </div>

        <div class="qr__copy">
          <strong>{{ t('rooms.qrScanTitle') }}</strong>
          <span>{{ t('rooms.qrScanHint') }}</span>
        </div>

        <footer class="qr__card-footer">
          <span><i class="pi pi-lock" aria-hidden="true"></i>{{ t('rooms.qrPrivate') }}</span>
          <code v-if="room?.activeTokenPreview">{{ room.activeTokenPreview }} · v{{ room.activeTokenVersion }}</code>
        </footer>
      </article>
    </div>

    <template #footer>
      <a v-if="src && !error" :href="src" :download="`qr-${room?.roomNumber ?? 'room'}.png`" class="qr__dl">
        <Button icon="pi pi-download" :label="t('rooms.downloadPng')" outlined />
      </a>
      <Button :label="t('common.close')" text @click="close" />
    </template>
  </Dialog>
</template>

<style scoped>
.qr {
  display: flex;
  flex-direction: column;
  align-items: stretch;
}
.qr__card {
  position: relative;
  overflow: hidden;
  padding: clamp(1rem, 4vw, 1.4rem);
  color: #10231d;
  background:
    radial-gradient(circle at 100% 0%, rgba(213, 171, 95, 0.2), transparent 30%),
    linear-gradient(145deg, #fffdf7 0%, #f4f0e4 100%);
  border: 1px solid rgba(16, 35, 29, 0.12);
  border-radius: 1.35rem;
  box-shadow: 0 1.25rem 3rem rgba(15, 23, 42, 0.12);
}
.qr__card::after {
  content: '';
  position: absolute;
  inset: 0.55rem;
  pointer-events: none;
  border: 1px solid rgba(16, 35, 29, 0.07);
  border-radius: 1rem;
}
.qr__brand-row,
.qr__room-row,
.qr__card-footer {
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}
.qr__brand {
  display: flex;
  align-items: center;
  gap: 0.65rem;
}
.qr__brand div {
  display: flex;
  flex-direction: column;
  line-height: 1.15;
}
.qr__brand strong {
  font-size: 1rem;
  letter-spacing: -0.01em;
}
.qr__brand span {
  margin-top: 0.18rem;
  color: #66736e;
  font-size: 0.7rem;
}
.qr__badge {
  padding: 0.35rem 0.55rem;
  color: #6c4d18;
  background: rgba(213, 171, 95, 0.2);
  border: 1px solid rgba(177, 129, 44, 0.22);
  border-radius: 999px;
  font-size: 0.64rem;
  font-weight: 800;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}
.qr__room-row {
  margin: 1.2rem 0 0.85rem;
  padding: 0 0.2rem;
}
.qr__room-row > div {
  display: flex;
  align-items: baseline;
  gap: 0.45rem;
}
.qr__eyebrow {
  color: #6f7c76;
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.18em;
}
.qr__room-row strong {
  font-family: Georgia, 'Times New Roman', serif;
  font-size: 2.25rem;
  font-weight: 600;
  line-height: 1;
}
.qr__location {
  color: #53635c;
  font-size: 0.78rem;
  font-weight: 700;
}
.qr__frame {
  position: relative;
  z-index: 1;
  display: grid;
  place-items: center;
  width: min(100%, 17rem);
  min-height: 17rem;
  margin-inline: auto;
  padding: 0.7rem;
  background: #ffffff;
  border: 1px solid rgba(16, 35, 29, 0.12);
  border-radius: 1rem;
  box-shadow: 0 0.65rem 1.6rem rgba(16, 35, 29, 0.1);
}
.qr__img {
  width: 100%;
  max-width: 15.5rem;
  height: auto;
  border-radius: 0.35rem;
  image-rendering: pixelated;
}
.qr__loading {
  color: var(--sh-text-muted);
  font-size: 0.9rem;
}
.qr__copy {
  position: relative;
  z-index: 1;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  margin-top: 1rem;
  text-align: center;
}
.qr__copy strong {
  font-family: Georgia, 'Times New Roman', serif;
  font-size: 1.05rem;
}
.qr__copy span {
  color: #66736e;
  font-size: 0.76rem;
  line-height: 1.45;
}
.qr__card-footer {
  align-items: flex-end;
  margin-top: 1rem;
  padding-top: 0.85rem;
  color: #66736e;
  border-top: 1px solid rgba(16, 35, 29, 0.1);
  font-size: 0.66rem;
}
.qr__card-footer span {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
}
.qr__card-footer code {
  max-width: 48%;
  overflow: hidden;
  color: #53635c;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.qr__dl {
  text-decoration: none;
}

@media (max-width: 430px) {
  .qr__frame {
    width: min(100%, 15.5rem);
    min-height: 15.5rem;
  }
  .qr__card-footer {
    align-items: flex-start;
    flex-direction: column;
  }
  .qr__card-footer code {
    max-width: 100%;
  }
}
</style>
