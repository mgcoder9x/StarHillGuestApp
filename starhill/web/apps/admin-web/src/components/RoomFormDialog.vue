<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Button from 'primevue/button';
import Message from 'primevue/message';
import { ApiError, api, type RoomListItem } from '../api/client';
import { useAuthStore } from '../stores/auth';

const props = defineProps<{ visible: boolean; room: RoomListItem | null }>();
const emit = defineEmits<{
  'update:visible': [boolean];
  saved: ['created' | 'updated'];
}>();

const { t } = useI18n();
const auth = useAuthStore();
const roomNumber = ref('');
const building = ref('');
const floor = ref<number | null>(null);
const loading = ref(false);
const error = ref<string | null>(null);
const isEdit = computed(() => props.room !== null);

function reset(): void {
  roomNumber.value = props.room?.roomNumber ?? '';
  building.value = props.room?.building ?? '';
  floor.value = props.room?.floor ?? null;
  error.value = null;
}

watch(
  () => [props.visible, props.room?.roomId] as const,
  ([visible]) => {
    if (visible) reset();
  },
);

function errorMessage(cause: unknown): string {
  if (cause instanceof ApiError) {
    if (cause.code === 'validation_error') return t('rooms.errors.numberTaken');
    if (cause.code === 'not_found') return t('rooms.errors.notFound');
    if (cause.code === 'invalid_configuration' || cause.code === 'resort_not_found') return t('rooms.errors.configuration');
  }
  return t('rooms.errors.action');
}

async function submit(): Promise<void> {
  const number = roomNumber.value.trim();
  const normalizedBuilding = building.value.trim();
  if (!number || number.length > 20) {
    error.value = t('rooms.validation.number');
    return;
  }
  if (normalizedBuilding.length > 50) {
    error.value = t('rooms.validation.building');
    return;
  }
  if (floor.value !== null && (floor.value < -10 || floor.value > 200)) {
    error.value = t('rooms.validation.floor');
    return;
  }

  loading.value = true;
  error.value = null;
  const input = { roomNumber: number, building: normalizedBuilding || null, floor: floor.value };
  try {
    if (props.room) {
      await api.updateRoom(props.room.roomId, input, auth.accessToken);
      emit('saved', 'updated');
    } else {
      await api.createRoom(input, auth.accessToken);
      emit('saved', 'created');
    }
    emit('update:visible', false);
  } catch (cause) {
    error.value = errorMessage(cause);
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    :draggable="false"
    :dismissable-mask="!loading"
    :closable="!loading"
    :style="{ width: 'min(94vw, 30rem)' }"
    :header="isEdit ? t('rooms.form.editTitle', { room: room?.roomNumber }) : t('rooms.form.createTitle')"
    @update:visible="emit('update:visible', $event)"
  >
    <form id="room-form" class="room-form" @submit.prevent="submit">
      <div class="room-form__field">
        <label for="room-number">{{ t('rooms.number') }}</label>
        <InputText id="room-number" v-model="roomNumber" maxlength="20" autocomplete="off" fluid />
      </div>

      <div class="room-form__field">
        <label for="room-building">{{ t('rooms.form.building') }}</label>
        <InputText id="room-building" v-model="building" maxlength="50" autocomplete="off" fluid />
      </div>

      <div class="room-form__field">
        <label for="room-floor">{{ t('rooms.form.floor') }}</label>
        <InputNumber
          v-model="floor"
          input-id="room-floor"
          :min="-10"
          :max="200"
          :use-grouping="false"
          fluid
        />
        <small>{{ t('rooms.form.floorHint') }}</small>
      </div>

      <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>
    </form>

    <template #footer>
      <Button :label="t('common.cancel')" text :disabled="loading" @click="emit('update:visible', false)" />
      <Button form="room-form" type="submit" icon="pi pi-check" :label="t('common.save')" :loading="loading" />
    </template>
  </Dialog>
</template>

<style scoped>
.room-form {
  display: grid;
  gap: 1rem;
}
.room-form__field {
  display: grid;
  gap: 0.35rem;
}
.room-form__field label {
  color: var(--sh-text);
  font-size: 0.85rem;
  font-weight: 700;
}
.room-form__field small {
  color: var(--sh-text-muted);
  font-size: 0.76rem;
}
</style>
