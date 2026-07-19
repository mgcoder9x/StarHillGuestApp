<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import Button from 'primevue/button';
import Message from 'primevue/message';
import ProgressSpinner from 'primevue/progressspinner';
import { useJourneyCore, apiGateway } from '../core/journeyCore';
import { GuestApiError, type GuestFaqCategory } from '../core/apiGateway';
import FaqItem from '../components/FaqItem.vue';

// FaqView (FE.5b, Req 4): cây FAQ active theo ngôn ngữ (GET /guest/faq). Rule-gate + FaqEnabled enforce server;
// session_expired/403 do ApiGateway intercept. Nội dung answerHtmlSanitized sanitize server (INV8). Đa ngôn ngữ INV5.
const { t, locale } = useI18n();
const router = useRouter();
const core = useJourneyCore();

const loading = ref(true);
const error = ref<string | null>(null);
const categories = ref<GuestFaqCategory[]>([]);

async function load(): Promise<void> {
  loading.value = true;
  error.value = null;
  const roomId = core.roomId.value;
  if (!roomId) {
    await router.replace({ name: 'rescan' });
    return;
  }
  try {
    const tree = await apiGateway.getFaq(roomId, locale.value);
    categories.value = [...tree.categories].sort((a, b) => a.sortOrder - b.sortOrder);
  } catch (e) {
    if (e instanceof GuestApiError && (e.code === 'session_expired' || e.code === 'guest_context_missing')) {
      await router.replace({ name: 'rescan' });
      return;
    }
    if (e instanceof GuestApiError && e.code === 'rule_ack_required') {
      await router.replace({ name: 'rules' });
      return;
    }
    error.value = t('faqFlow.loadError');
  } finally {
    loading.value = false;
  }
}

function backHome(): void {
  void router.replace({ name: 'home' });
}

watch(locale, () => {
  void load();
});
onMounted(load);
</script>

<template>
  <main class="faq">
    <header class="faq__bar">
      <Button icon="pi pi-arrow-left" text rounded :aria-label="t('faqFlow.back')" @click="backHome" />
      <h1 class="faq__title">{{ t('faqFlow.title') }}</h1>
    </header>

    <div v-if="loading" class="faq__center"><ProgressSpinner stroke-width="4" aria-label="Loading" /></div>
    <Message v-else-if="error" severity="error" :closable="false">{{ error }}</Message>
    <p v-else-if="categories.length === 0" class="faq__empty">{{ t('faqFlow.empty') }}</p>

    <section v-else class="faq__list" data-testid="faq-list">
      <article v-for="cat in categories" :key="cat.id" class="faq__cat">
        <h2 class="faq__cat-name">
          {{ cat.name }}
          <span v-if="cat.isFallback" class="faq__fallback">{{ t('faqFlow.fallback', { lang: cat.resolvedLanguage }) }}</span>
        </h2>
        <FaqItem v-for="item in cat.items" :key="item.id" :item="item" :depth="0" />
      </article>
    </section>
  </main>
</template>

<style scoped>
.faq {
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
.faq__bar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
.faq__title {
  margin: 0;
  font-size: clamp(1.25rem, 1.05rem + 1.6vw, 1.6rem);
  font-weight: 800;
}
.faq__center {
  display: grid;
  place-items: center;
  flex: 1;
}
.faq__empty {
  color: #64748b;
}
.faq__list {
  display: flex;
  flex-direction: column;
  gap: var(--sh-gap, 1rem);
}
.faq__cat {
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 0.9rem;
  padding: 0.75rem 0.9rem;
}
.faq__cat-name {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin: 0 0 0.35rem;
  font-size: 1.05rem;
}
.faq__fallback {
  font-size: 0.7rem;
  font-weight: 500;
  color: #b45309;
  background: #fef3c7;
  border-radius: 999px;
  padding: 0.05rem 0.4rem;
}
</style>
