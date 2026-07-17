<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import Card from 'primevue/card';
import Button from 'primevue/button';
import Select from 'primevue/select';
import Tag from 'primevue/tag';
import { SUPPORTED_LOCALES, type Locale } from '../i18n';

// Dữ liệu resort/phòng: FE.1 sẽ lấy từ /v1/guest/resolve (đã có BE). Ở FE.1a demo tĩnh để trình bày giao diện.
const resortName = 'Star Hill Resort';
const roomNumber = 'A-1203';

const { t, locale } = useI18n();
const router = useRouter();

const languageOptions = computed(() =>
  SUPPORTED_LOCALES.map((code) => ({
    code,
    label: { en: 'English', vi: 'Tiếng Việt', ko: '한국어', zh: '中文' }[code],
  })),
);

const selectedLocale = computed<Locale>({
  get: () => locale.value as Locale,
  set: (v) => (locale.value = v),
});

const sections = computed(() => [
  { key: 'rules', icon: 'pi-book', to: '/rules', accent: 'accent-rules' },
  { key: 'faq', icon: 'pi-question-circle', to: '/faq', accent: 'accent-faq' },
  { key: 'chat', icon: 'pi-comments', to: '/chat', accent: 'accent-chat' },
  { key: 'housekeeping', icon: 'pi-sparkles', to: '/housekeeping', accent: 'accent-hk' },
]);
</script>

<template>
  <div class="home">
    <header class="home-header">
      <div class="home-header__top">
        <Tag :value="t('home.room', { room: roomNumber })" severity="contrast" rounded />
        <Select
          v-model="selectedLocale"
          :options="languageOptions"
          option-label="label"
          option-value="code"
          :aria-label="t('common.language')"
          class="lang-select"
        />
      </div>
      <h1 class="home-title">{{ t('home.welcome', { resort: resortName }) }}</h1>
      <p class="home-subtitle">{{ t('home.subtitle') }}</p>
    </header>

    <main class="home-main">
      <section class="section-grid" aria-label="sections">
        <Card v-for="s in sections" :key="s.key" class="section-card" :class="s.accent">
          <template #header>
            <div class="section-icon"><i :class="['pi', s.icon]" aria-hidden="true"></i></div>
          </template>
          <template #title>{{ t(`home.${s.key}.title`) }}</template>
          <template #content>
            <p class="section-desc">{{ t(`home.${s.key}.desc`) }}</p>
          </template>
          <template #footer>
            <Button :label="t('common.open')" fluid @click="router.push(s.to)" />
          </template>
        </Card>
      </section>
    </main>

    <footer class="home-footer">
      <i class="pi pi-wifi" aria-hidden="true"></i>
      <span>{{ t('home.footer') }}</span>
    </footer>
  </div>
</template>

<style scoped>
.home {
  min-height: 100vh;
  min-height: 100svh;
  min-height: 100dvh;
  display: flex;
  flex-direction: column;
  background: linear-gradient(180deg, var(--p-primary-50, #eef2ff) 0%, transparent 30%);
}

.home-header {
  padding: var(--sh-space-page);
  padding-block-start: max(var(--sh-space-page), env(safe-area-inset-top));
  padding-inline: max(var(--sh-space-page), env(safe-area-inset-left)) max(var(--sh-space-page), env(safe-area-inset-right));
}
.home-header__top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--sh-gap);
  flex-wrap: wrap;
}
.lang-select {
  min-width: 8.5rem;
}
.home-title {
  margin: 1rem 0 0.25rem;
  font-size: var(--sh-font-title);
  font-weight: 800;
  line-height: 1.15;
}
.home-subtitle {
  margin: 0;
  color: var(--p-text-muted-color, #64748b);
}

.home-main {
  flex: 1 1 auto;
  padding-inline: max(var(--sh-space-page), env(safe-area-inset-left)) max(var(--sh-space-page), env(safe-area-inset-right));
  padding-block: var(--sh-space-page);
  container-type: inline-size;
}

/* auto-fit: 1 cột trên phone → 2 cột phablet → 4 cột tablet/desktop, KHÔNG media query. */
.section-grid {
  display: grid;
  gap: var(--sh-gap);
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 15rem), 1fr));
}
.section-card {
  height: 100%;
}
.section-icon {
  display: grid;
  place-items: center;
  font-size: 2rem;
  padding-block-start: 1.25rem;
}
.section-desc {
  margin: 0;
  min-height: 3rem;
  color: var(--p-text-muted-color, #64748b);
}

.home-footer {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  justify-content: center;
  padding: var(--sh-space-page);
  padding-block-end: max(var(--sh-space-page), env(safe-area-inset-bottom));
  color: var(--p-text-muted-color, #64748b);
  font-size: 0.85rem;
}
</style>
