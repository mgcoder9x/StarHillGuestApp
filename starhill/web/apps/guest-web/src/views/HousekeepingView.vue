<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import Button from 'primevue/button';
import Message from 'primevue/message';
import { useJourneyCore, apiGateway } from '../core/journeyCore';
import { GuestApiError, type HousekeepingStatus, type HousekeepingTicket } from '../core/apiGateway';

// HousekeepingView (FE.5d, Req 6): tạo yêu cầu dọn phòng (idempotent 1-mở/phòng) + xem trạng thái (poll ~5s).
// Rule-gate + HousekeepingEnabled enforce server; session_expired→rescan, 403→rules (ApiGateway + view).
const POLL_MS = 5000;
const { t } = useI18n();
const router = useRouter();
const core = useJourneyCore();

const ticket = ref<HousekeepingTicket | null>(null);
const loading = ref(true);
const submitting = ref(false);
const error = ref<string | null>(null);
const notice = ref<string | null>(null);
let pollTimer: number | null = null;

const OPEN: HousekeepingStatus[] = ['Requested', 'InProgress'];
const isOpen = computed(() => !!ticket.value && OPEN.includes(ticket.value.status));
// Timeline hiển thị: Requested → InProgress → Done.
const steps: HousekeepingStatus[] = ['Requested', 'InProgress', 'Done'];
const activeStepIndex = computed(() => {
  if (!ticket.value) return -1;
  if (ticket.value.status === 'Cancelled') return -1;
  return steps.indexOf(ticket.value.status);
});

async function load(silent = false): Promise<void> {
  const roomId = core.roomId.value;
  if (!roomId) {
    await router.replace({ name: 'rescan' });
    return;
  }
  if (!silent) loading.value = true;
  try {
    const res = await apiGateway.getHousekeepingStatus(roomId);
    ticket.value = res.ticket;
  } catch (e) {
    if (e instanceof GuestApiError && (e.code === 'session_expired' || e.code === 'guest_context_missing')) {
      await router.replace({ name: 'rescan' });
      return;
    }
    if (!silent) error.value = t('housekeepingFlow.loadError');
  } finally {
    loading.value = false;
  }
}

async function request(): Promise<void> {
  const roomId = core.roomId.value;
  if (!roomId || submitting.value) return;
  submitting.value = true;
  error.value = null;
  notice.value = null;
  try {
    const res = await apiGateway.requestHousekeeping(roomId);
    notice.value = res.alreadyOpen ? t('housekeepingFlow.alreadyOpen') : t('housekeepingFlow.requested');
    await load(true);
  } catch (e) {
    if (e instanceof GuestApiError && (e.code === 'session_expired' || e.code === 'guest_context_missing')) {
      await router.replace({ name: 'rescan' });
      return;
    }
    if (e instanceof GuestApiError && e.code === 'rule_ack_required') {
      await router.replace({ name: 'rules' });
      return;
    }
    error.value = t('housekeepingFlow.requestError');
  } finally {
    submitting.value = false;
  }
}

function backHome(): void {
  void router.replace({ name: 'home' });
}
function statusLabel(s: HousekeepingStatus): string {
  return t(`housekeepingFlow.status.${s}`);
}

watch(
  () => core.roomId.value,
  (id) => {
    if (!id) void router.replace({ name: 'rescan' });
  },
);
onMounted(() => {
  void load();
  pollTimer = window.setInterval(() => void load(true), POLL_MS);
});
onBeforeUnmount(() => {
  if (pollTimer !== null) window.clearInterval(pollTimer);
});
</script>

<template>
  <main class="hk">
    <header class="hk__bar">
      <Button icon="pi pi-arrow-left" text rounded :aria-label="t('housekeepingFlow.back')" @click="backHome" />
      <h1 class="hk__title">{{ t('housekeepingFlow.title') }}</h1>
    </header>

    <p class="hk__desc">{{ t('housekeepingFlow.desc') }}</p>

    <Message v-if="notice" severity="success" :closable="true" @close="notice = null">{{ notice }}</Message>
    <Message v-if="error" severity="error" :closable="true" @close="error = null">{{ error }}</Message>

    <!-- Trạng thái ticket hiện hành. -->
    <section v-if="ticket && ticket.status !== 'Cancelled'" class="hk__status" data-testid="hk-status">
      <span class="hk__badge" :data-status="ticket.status">{{ statusLabel(ticket.status) }}</span>
      <ol class="hk__timeline">
        <li
          v-for="(s, i) in steps"
          :key="s"
          class="hk__step"
          :class="{ 'hk__step--done': activeStepIndex >= 0 && i <= activeStepIndex }"
        >
          <span class="hk__dot"></span>
          <span class="hk__step-label">{{ statusLabel(s) }}</span>
        </li>
      </ol>
    </section>

    <section v-else class="hk__empty" data-testid="hk-empty">
      <p>{{ ticket?.status === 'Cancelled' ? t('housekeepingFlow.cancelled') : t('housekeepingFlow.none') }}</p>
    </section>

    <!-- Yêu cầu dọn phòng: ẩn/disable khi đã có ticket mở (idempotent). -->
    <Button
      v-if="!isOpen"
      :label="t('housekeepingFlow.request')"
      icon="pi pi-sparkles"
      :loading="submitting"
      data-testid="hk-request"
      @click="request"
    />
    <p v-else class="hk__hint" data-testid="hk-open-hint">{{ t('housekeepingFlow.openHint') }}</p>
  </main>
</template>

<style scoped>
.hk {
  min-height: 100vh;
  min-height: 100svh;
  min-height: 100dvh;
  display: flex;
  flex-direction: column;
  gap: var(--sh-gap, 1rem);
  padding: var(--sh-space-page, 1rem);
  padding-block-start: max(var(--sh-space-page, 1rem), env(safe-area-inset-top));
  padding-block-end: max(var(--sh-space-page, 1rem), env(safe-area-inset-bottom));
  max-width: 42rem;
  margin-inline: auto;
}
.hk__bar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
.hk__title {
  margin: 0;
  font-size: clamp(1.25rem, 1.05rem + 1.6vw, 1.6rem);
  font-weight: 800;
}
.hk__desc {
  margin: 0;
  color: #64748b;
}
.hk__status,
.hk__empty {
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 0.9rem;
  padding: 1rem 1.15rem;
}
.hk__badge {
  display: inline-block;
  font-weight: 700;
  padding: 0.2rem 0.7rem;
  border-radius: 999px;
  color: #0f766e;
  background: #ccfbf1;
}
.hk__badge[data-status='Done'] {
  color: #166534;
  background: #dcfce7;
}
.hk__timeline {
  list-style: none;
  margin: 1rem 0 0;
  padding: 0;
  display: flex;
  gap: 0.5rem;
}
.hk__step {
  flex: 1 1 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.35rem;
  color: #94a3b8;
  font-size: 0.8rem;
  text-align: center;
}
.hk__dot {
  width: 0.9rem;
  height: 0.9rem;
  border-radius: 50%;
  background: #e2e8f0;
}
.hk__step--done {
  color: #0f766e;
  font-weight: 600;
}
.hk__step--done .hk__dot {
  background: #0f766e;
}
.hk__hint {
  margin: 0;
  color: #64748b;
}
</style>
