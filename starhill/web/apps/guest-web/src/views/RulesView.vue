<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch, nextTick } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import Button from 'primevue/button';
import Message from 'primevue/message';
import ProgressSpinner from 'primevue/progressspinner';
import { useJourneyCore, apiGateway } from '../core/journeyCore';
import { GuestApiError, type GuestRuleSection } from '../core/apiGateway';

// Force-read state machine (design-module 10 §3, INV6). Từng section: RequireScrollEnd (IntersectionObserver) +
// MinReadSeconds (đếm ngược) → "Tiếp tục" chỉ bật khi section thoả. Hết section bắt buộc → checkbox → acknowledge
// (server-authoritative — INV2). Chế độ xem lại nếu đã ack current (Req 3.10). bodyHtmlSanitized đã sanitize server (INV8).
const { t, locale } = useI18n();
const router = useRouter();
const core = useJourneyCore();

const loading = ref(true);
const error = ref<string | null>(null);
const sections = ref<GuestRuleSection[]>([]);
const currentIndex = ref(0);
const viewed = ref(false);
const secondsLeft = ref(0);
const checkboxChecked = ref(false);
const acknowledging = ref(false);
let countdownTimer: number | null = null;
let observer: IntersectionObserver | null = null;
const sentinelRef = ref<HTMLElement | null>(null);

const reviewMode = computed(() => core.isAckedCurrent.value);
const current = computed<GuestRuleSection | null>(() => sections.value[currentIndex.value] ?? null);
const total = computed(() => sections.value.length);
const isLast = computed(() => currentIndex.value === total.value - 1);

const currentSatisfied = computed(() => {
  const s = current.value;
  if (!s) return false;
  if (!s.isRequired) return true;
  return viewed.value && secondsLeft.value <= 0;
});

// Hết section cuối + đã thoả → cho phép xác nhận (checkbox).
const atConfirmStep = computed(() => isLast.value && currentSatisfied.value);

function clearCountdown(): void {
  if (countdownTimer !== null) {
    window.clearInterval(countdownTimer);
    countdownTimer = null;
  }
}
function clearObserver(): void {
  if (observer) {
    observer.disconnect();
    observer = null;
  }
}

// Khởi tạo trạng thái cho section hiện tại (viewed + countdown + observer).
async function enterSection(): Promise<void> {
  clearCountdown();
  clearObserver();
  const s = current.value;
  if (!s) return;
  // viewed: nếu không RequireScrollEnd thì true ngay; nếu có thì chờ IntersectionObserver.
  viewed.value = !s.requireScrollEnd || reviewMode.value;
  secondsLeft.value = reviewMode.value ? 0 : s.minReadSeconds;
  if (secondsLeft.value > 0) {
    countdownTimer = window.setInterval(() => {
      if (secondsLeft.value > 0) {
        secondsLeft.value -= 1;
      }
      if (secondsLeft.value <= 0) {
        clearCountdown();
      }
    }, 1000);
  }
  if (s.requireScrollEnd && !reviewMode.value) {
    await nextTick();
    if (sentinelRef.value) {
      observer = new IntersectionObserver(
        (entries) => {
          if (entries.some((e) => e.isIntersecting)) {
            viewed.value = true;
            clearObserver();
          }
        },
        { threshold: 0.9 },
      );
      observer.observe(sentinelRef.value);
    } else {
      viewed.value = true; // không có sentinel (an toàn) → coi như viewed
    }
  }
}

async function load(): Promise<void> {
  loading.value = true;
  error.value = null;
  const roomId = core.roomId.value;
  if (!roomId) {
    core.markRescanNeeded();
    await router.replace({ name: 'rescan' });
    return;
  }
  try {
    const res = await apiGateway.getRules(roomId, locale.value);
    sections.value = [...res.sections].sort((a, b) => a.sortOrder - b.sortOrder);
    core.setCurrentRuleVersion(res.version);
    currentIndex.value = 0;
    await enterSection();
  } catch (e) {
    // session_expired/guest_context_missing đã được ApiGateway intercept → rescan; ở đây chỉ báo lỗi generic khác.
    if (e instanceof GuestApiError && (e.code === 'session_expired' || e.code === 'guest_context_missing')) {
      await router.replace({ name: 'rescan' });
      return;
    }
    error.value = t('rulesFlow.loadError');
  } finally {
    loading.value = false;
  }
}

function next(): void {
  if (!currentSatisfied.value || isLast.value) return;
  currentIndex.value += 1;
  void enterSection();
}

async function confirm(): Promise<void> {
  if (!checkboxChecked.value) return;
  const roomId = core.roomId.value;
  if (!roomId) {
    await router.replace({ name: 'rescan' });
    return;
  }
  acknowledging.value = true;
  error.value = null;
  try {
    const res = await apiGateway.acknowledgeRules(roomId, locale.value);
    core.setAcked(res.version);
    await router.replace({ name: 'home' });
  } catch (e) {
    if (e instanceof GuestApiError && (e.code === 'session_expired' || e.code === 'guest_context_missing')) {
      await router.replace({ name: 'rescan' });
      return;
    }
    error.value = t('rulesFlow.ackError');
  } finally {
    acknowledging.value = false;
  }
}

function backHome(): void {
  void router.replace({ name: 'home' });
}

// Đổi ngôn ngữ → tải lại nội dung server theo lang (INV5).
watch(locale, () => {
  void load();
});

onMounted(load);
onBeforeUnmount(() => {
  clearCountdown();
  clearObserver();
});
</script>

<template>
  <main class="rules">
    <header class="rules__bar">
      <h1 class="rules__title">{{ t('rulesFlow.title') }}</h1>
      <span v-if="!reviewMode && total > 0" class="rules__progress" data-testid="rules-progress">
        {{ currentIndex + 1 }}/{{ total }}
      </span>
    </header>

    <div v-if="loading" class="rules__center">
      <ProgressSpinner stroke-width="4" aria-label="Loading" />
    </div>

    <Message v-else-if="error" severity="error" :closable="false">{{ error }}</Message>

    <template v-else>
      <!-- Chế độ xem lại: hiện tất cả section, không checkbox/confirm (Req 3.10). -->
      <div v-if="reviewMode" class="rules__review">
        <article v-for="s in sections" :key="s.key" class="rules__section">
          <h2>{{ s.title }}</h2>
          <span v-if="s.isFallback" class="rules__fallback">{{ t('rulesFlow.fallback', { lang: s.resolvedLanguage }) }}</span>
          <!-- eslint-disable-next-line vue/no-v-html — bodyHtmlSanitized đã sanitize server (INV8) -->
          <div class="rules__body" v-html="s.bodyHtmlSanitized ?? ''"></div>
        </article>
        <Button :label="t('rulesFlow.back')" icon="pi pi-arrow-left" outlined @click="backHome" />
      </div>

      <!-- Force-read stepper. -->
      <div v-else-if="current" class="rules__step">
        <article class="rules__section">
          <h2>{{ current.title }}</h2>
          <span v-if="current.isFallback" class="rules__fallback">{{ t('rulesFlow.fallback', { lang: current.resolvedLanguage }) }}</span>
          <!-- eslint-disable-next-line vue/no-v-html — sanitized server (INV8) -->
          <div class="rules__body" v-html="current.bodyHtmlSanitized ?? ''"></div>
          <div ref="sentinelRef" class="rules__sentinel" aria-hidden="true"></div>
        </article>

        <div class="rules__actions">
          <p v-if="!currentSatisfied && secondsLeft > 0" class="rules__hint" data-testid="rules-countdown">
            {{ t('rulesFlow.waitSeconds', { n: secondsLeft }) }}
          </p>
          <p v-else-if="!currentSatisfied && current.requireScrollEnd" class="rules__hint">
            {{ t('rulesFlow.scrollToEnd') }}
          </p>

          <template v-if="!atConfirmStep">
            <Button
              :label="t('rulesFlow.next')"
              icon="pi pi-arrow-right"
              icon-pos="right"
              :disabled="!currentSatisfied"
              data-testid="rules-next"
              @click="next"
            />
          </template>

          <template v-else>
            <label class="rules__agree">
              <input v-model="checkboxChecked" type="checkbox" data-testid="rules-agree" />
              <span>{{ t('rulesFlow.agree') }}</span>
            </label>
            <Button
              :label="t('rulesFlow.confirm')"
              icon="pi pi-check"
              :disabled="!checkboxChecked"
              :loading="acknowledging"
              data-testid="rules-confirm"
              @click="confirm"
            />
          </template>
        </div>
      </div>
    </template>
  </main>
</template>

<style scoped>
.rules {
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
.rules__bar {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 0.75rem;
}
.rules__title {
  margin: 0;
  font-size: clamp(1.25rem, 1.05rem + 1.6vw, 1.6rem);
  font-weight: 800;
}
.rules__progress {
  font-weight: 700;
  color: #0f766e;
}
.rules__center {
  display: grid;
  place-items: center;
  flex: 1;
}
.rules__review,
.rules__step {
  display: flex;
  flex-direction: column;
  gap: var(--sh-gap, 1rem);
}
.rules__section {
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 0.9rem;
  padding: 1rem 1.15rem;
}
.rules__section h2 {
  margin: 0 0 0.5rem;
  font-size: 1.1rem;
}
.rules__body :where(p) {
  margin: 0 0 0.6rem;
  line-height: 1.6;
  overflow-wrap: anywhere;
}
.rules__fallback {
  display: inline-block;
  margin-bottom: 0.5rem;
  font-size: 0.75rem;
  color: #b45309;
  background: #fef3c7;
  border-radius: 999px;
  padding: 0.1rem 0.5rem;
}
.rules__sentinel {
  height: 1px;
}
.rules__actions {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  align-items: stretch;
  position: sticky;
  bottom: 0;
  padding-top: 0.5rem;
}
.rules__hint {
  margin: 0;
  color: #64748b;
  font-size: 0.9rem;
  text-align: center;
}
.rules__agree {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  font-size: 0.95rem;
}
.rules__agree input {
  width: 1.15rem;
  height: 1.15rem;
  margin-top: 0.1rem;
  flex: 0 0 auto;
}
</style>
