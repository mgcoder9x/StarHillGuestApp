<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import Button from 'primevue/button';
import Message from 'primevue/message';
import { useJourneyCore, apiGateway } from '../core/journeyCore';
import { GuestApiError, type GuestConversation } from '../core/apiGateway';

// ChatView (FE.5c, Req 5): POLLING là nguồn sự thật (GET /guest/conversation ~4s + refresh sau gửi). Gửi tin
// POST /guest/messages (plain text — render {{}} = textContent, INV8; KHÔNG v-html). SignalR realtime = FE.5c-ii.
// session_expired/403 do ApiGateway intercept. Prefill từ FAQ CTA (query.prefill, Req 4.6).
const POLL_MS = 4000;
const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const core = useJourneyCore();

const conversation = ref<GuestConversation | null>(null);
const input = ref('');
const loading = ref(true);
const sending = ref(false);
const error = ref<string | null>(null);
const listRef = ref<HTMLElement | null>(null);
let pollTimer: number | null = null;

const messages = computed(() => conversation.value?.messages ?? []);

function scrollToBottom(): void {
  void nextTick(() => {
    if (listRef.value) {
      listRef.value.scrollTop = listRef.value.scrollHeight;
    }
  });
}

async function load(silent = false): Promise<void> {
  const roomId = core.roomId.value;
  if (!roomId) {
    await router.replace({ name: 'rescan' });
    return;
  }
  if (!silent) {
    loading.value = true;
  }
  try {
    const res = await apiGateway.getConversation(roomId);
    const hadCount = messages.value.length;
    conversation.value = res.conversation;
    if ((conversation.value?.messages.length ?? 0) !== hadCount) {
      scrollToBottom();
    }
  } catch (e) {
    if (e instanceof GuestApiError && (e.code === 'session_expired' || e.code === 'guest_context_missing')) {
      await router.replace({ name: 'rescan' });
      return;
    }
    if (!silent) {
      error.value = t('chatFlow.loadError');
    }
  } finally {
    loading.value = false;
  }
}

async function send(): Promise<void> {
  const body = input.value.trim();
  if (!body || sending.value) {
    return;
  }
  const roomId = core.roomId.value;
  if (!roomId) {
    await router.replace({ name: 'rescan' });
    return;
  }
  sending.value = true;
  error.value = null;
  try {
    await apiGateway.sendMessage(roomId, body);
    input.value = '';
    await load(true); // refresh ngay (không đợi poll) — polling nguồn sự thật.
  } catch (e) {
    if (e instanceof GuestApiError && (e.code === 'session_expired' || e.code === 'guest_context_missing')) {
      await router.replace({ name: 'rescan' });
      return;
    }
    if (e instanceof GuestApiError && e.code === 'rule_ack_required') {
      await router.replace({ name: 'rules' });
      return;
    }
    error.value = e instanceof GuestApiError ? e.message || t('chatFlow.sendError') : t('chatFlow.sendError');
  } finally {
    sending.value = false;
  }
}

function backHome(): void {
  void router.replace({ name: 'home' });
}

watch(
  () => core.roomId.value,
  (id) => {
    if (!id) {
      void router.replace({ name: 'rescan' });
    }
  },
);

onMounted(() => {
  const prefill = route.query.prefill;
  if (typeof prefill === 'string' && prefill.trim()) {
    input.value = prefill.trim();
  }
  void load();
  pollTimer = window.setInterval(() => void load(true), POLL_MS);
});
onBeforeUnmount(() => {
  if (pollTimer !== null) {
    window.clearInterval(pollTimer);
  }
});
</script>

<template>
  <main class="chat">
    <header class="chat__bar">
      <Button icon="pi pi-arrow-left" text rounded :aria-label="t('chatFlow.back')" @click="backHome" />
      <h1 class="chat__title">{{ t('chatFlow.title') }}</h1>
    </header>

    <div ref="listRef" class="chat__list" data-testid="chat-list">
      <p v-if="!loading && messages.length === 0" class="chat__empty">{{ t('chatFlow.empty') }}</p>
      <div
        v-for="m in messages"
        :key="m.messageId"
        class="chat__msg"
        :class="m.senderType === 'Guest' ? 'chat__msg--me' : 'chat__msg--them'"
      >
        <!-- Plain text: interpolation {{}} render textContent (KHÔNG v-html) — INV8. -->
        <span class="chat__bubble">{{ m.body }}</span>
      </div>
    </div>

    <Message v-if="error" severity="error" :closable="true" @close="error = null">{{ error }}</Message>

    <form class="chat__compose" @submit.prevent="send">
      <input
        v-model="input"
        class="chat__input"
        :placeholder="t('chatFlow.placeholder')"
        :aria-label="t('chatFlow.placeholder')"
        data-testid="chat-input"
        autocomplete="off"
      />
      <Button
        type="submit"
        icon="pi pi-send"
        :aria-label="t('chatFlow.send')"
        :disabled="!input.trim() || sending"
        :loading="sending"
        data-testid="chat-send"
      />
    </form>
  </main>
</template>

<style scoped>
.chat {
  height: 100vh;
  height: 100svh;
  height: 100dvh;
  display: flex;
  flex-direction: column;
  max-width: 42rem;
  margin-inline: auto;
  padding: var(--sh-space-page, 1rem);
  padding-block-start: max(var(--sh-space-page, 1rem), env(safe-area-inset-top));
  padding-block-end: max(0.6rem, env(safe-area-inset-bottom));
  gap: 0.6rem;
}
.chat__bar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex: 0 0 auto;
}
.chat__title {
  margin: 0;
  font-size: 1.2rem;
  font-weight: 800;
}
.chat__list {
  flex: 1 1 auto;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding: 0.25rem;
  min-height: 0;
}
.chat__empty {
  margin: auto;
  color: #94a3b8;
}
.chat__msg {
  display: flex;
  max-width: 85%;
}
.chat__msg--me {
  align-self: flex-end;
}
.chat__msg--them {
  align-self: flex-start;
}
.chat__bubble {
  padding: 0.55rem 0.8rem;
  border-radius: 1rem;
  line-height: 1.45;
  overflow-wrap: anywhere;
  white-space: pre-wrap;
}
.chat__msg--me .chat__bubble {
  background: #0f766e;
  color: #fff;
  border-bottom-right-radius: 0.3rem;
}
.chat__msg--them .chat__bubble {
  background: #f1f5f9;
  color: #0f172a;
  border-bottom-left-radius: 0.3rem;
}
.chat__compose {
  flex: 0 0 auto;
  display: flex;
  gap: 0.5rem;
  align-items: center;
}
.chat__input {
  flex: 1 1 auto;
  min-width: 0;
  min-height: 44px;
  padding: 0.6rem 0.9rem;
  border: 1px solid #cbd5e1;
  border-radius: 999px;
  font: inherit;
}
</style>
