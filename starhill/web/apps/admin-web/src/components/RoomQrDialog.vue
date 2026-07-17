<script setup lang="ts">
import { ref, watch, onBeforeUnmount } from 'vue';
import { useI18n } from 'vue-i18n';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Message from 'primevue/message';
import { api, type RoomListItem } from '../api/client';
import { useAuthStore } from '../stores/auth';

// Dialog xem QR của phòng. QR endpoint (qr.png) RequireStaff → cần Bearer → fetch blob→objectURL (không <img src>).
// Revoke objectURL khi đóng/unmount (chống rò bộ nhớ). Mock mode trả data-URL SVG (không cần revoke).
const props = defineProps<{ visible: boolean; room: RoomListItem | null }>();
const emit = defineEmits<{ 'update:visible': [boolean] }>();

const { t } = useI18n();
const auth = useAuthStore();

const src = ref<string | null>(null);
const loading = ref(false);
const error = ref<string | null>(null);

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
    :style="{ width: 'min(92vw, 22rem)' }"
    :header="room ? t('rooms.qrDialogTitle', { room: room.roomNumber }) : ''"
    @update:visible="emit('update:visible', $event)"
  >
    <div class="qr">
      <Message v-if="error" severity="warn" :closable="false">{{ error }}</Message>
      <div v-else class="qr__frame">
        <img
          v-if="src"
          :src="src"
          :alt="room ? t('rooms.qrDialogTitle', { room: room.roomNumber }) : 'QR'"
          class="qr__img"
          data-testid="room-qr-img"
        />
        <span v-else class="qr__loading">{{ t('rooms.qrLoading') }}</span>
      </div>

      <p v-if="room?.activeTokenPreview" class="qr__preview">
        {{ t('rooms.qr') }}: <code>{{ room.activeTokenPreview }}</code> · v{{ room.activeTokenVersion }}
      </p>
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
  gap: 0.75rem;
  align-items: center;
}
.qr__frame {
  display: grid;
  place-items: center;
  width: 100%;
  min-height: 12rem;
  padding: 0.75rem;
  background: #ffffff;
  border: 1px solid var(--sh-border);
  border-radius: var(--sh-radius-sm);
}
.qr__img {
  width: 100%;
  max-width: 15rem;
  height: auto;
  image-rendering: pixelated; /* QR nét khi phóng (khớp thiết kế guest §3.7). */
}
.qr__loading {
  color: var(--sh-text-muted);
  font-size: 0.9rem;
}
.qr__preview {
  margin: 0;
  color: var(--sh-text-muted);
  font-size: 0.85rem;
  text-align: center;
}
.qr__dl {
  text-decoration: none;
}
</style>
