<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import Button from 'primevue/button';
import Message from 'primevue/message';
import ProgressSpinner from 'primevue/progressspinner';
import { GuestApiError, resolveGuestToken } from '../api/guestApi';
import { SUPPORTED_LOCALES } from '../i18n';
import { useGuestSession } from '../stores/guestSession';

const TOKEN_PATTERN = /^[A-Za-z0-9_-]{43}$/;
const route = useRoute();
const router = useRouter();
const { t, locale } = useI18n();
const session = useGuestSession();
const loading = ref(true);
const errorCode = ref<string | undefined>();
const errorMessage = ref<string | undefined>();

function messageFor(error: unknown): string {
  if (error instanceof GuestApiError) {
    const known = ['qr_invalid', 'room_inactive', 'configuration_unavailable', 'session_expired'];
    if (error.code && known.includes(error.code)) {
      return t(`guestEntry.errors.${error.code}`);
    }
  }
  return t('guestEntry.errors.generic');
}

async function resolve(): Promise<void> {
  loading.value = true;
  errorCode.value = undefined;
  errorMessage.value = undefined;

  const token = String(route.params.token ?? '');
  if (!TOKEN_PATTERN.test(token)) {
    errorCode.value = 'qr_invalid';
    errorMessage.value = t('guestEntry.errors.qr_invalid');
    loading.value = false;
    return;
  }

  try {
    const resolved = await resolveGuestToken(token);
    session.setContext(resolved);
    if (SUPPORTED_LOCALES.includes(resolved.defaultLanguage as (typeof SUPPORTED_LOCALES)[number])) {
      locale.value = resolved.defaultLanguage;
    }
    await router.replace({ name: 'home' });
  } catch (error) {
    errorCode.value = error instanceof GuestApiError ? error.code : undefined;
    errorMessage.value = messageFor(error);
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  void resolve();
});
</script>

<template>
  <main class="guest-entry">
    <section class="guest-entry__card" aria-live="polite">
      <div class="guest-entry__brand"><span class="guest-entry__mark">✦</span><strong>Star Hill</strong></div>

      <template v-if="loading">
        <ProgressSpinner class="guest-entry__spinner" stroke-width="4" aria-label="Loading" />
        <h1>{{ t('guestEntry.loadingTitle') }}</h1>
        <p>{{ t('guestEntry.loadingBody') }}</p>
      </template>

      <template v-else-if="errorMessage">
        <Message severity="error" :closable="false">{{ errorMessage }}</Message>
        <h1>{{ t('guestEntry.errorTitle') }}</h1>
        <p>{{ t('guestEntry.errorBody') }}</p>
        <Button :label="t('guestEntry.retry')" icon="pi pi-refresh" @click="resolve" />
        <small v-if="errorCode" class="guest-entry__code">{{ errorCode }}</small>
      </template>
    </section>
  </main>
</template>

<style scoped>
.guest-entry {
  min-height: 100dvh;
  display: grid;
  place-items: center;
  padding: var(--sh-space-page);
  background:
    radial-gradient(circle at 12% 10%, rgba(14, 165, 164, 0.18), transparent 36%),
    linear-gradient(145deg, #f8fafc, #e6fffb);
}

.guest-entry__card {
  width: min(100%, 28rem);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.85rem;
  padding: clamp(1.5rem, 5vw, 3rem);
  text-align: center;
  background: rgba(255, 255, 255, 0.92);
  border: 1px solid rgba(15, 118, 110, 0.16);
  border-radius: 1.5rem;
  box-shadow: 0 1.5rem 4rem rgba(15, 23, 42, 0.12);
}

.guest-entry__brand {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  color: #0f766e;
  font-size: 1.25rem;
}

.guest-entry__mark {
  display: grid;
  width: 2rem;
  height: 2rem;
  place-items: center;
  color: white;
  background: #0f766e;
  border-radius: 0.65rem;
}

.guest-entry__spinner { width: 3rem; height: 3rem; }
.guest-entry h1 { margin: 0.5rem 0 0; font-size: clamp(1.35rem, 4vw, 1.8rem); }
.guest-entry p { margin: 0; color: #64748b; }
.guest-entry__code { color: #94a3b8; }
</style>
