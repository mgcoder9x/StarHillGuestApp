<script setup lang="ts">
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import Button from 'primevue/button';

// Placeholder mục Guest Web (FE.1a) — điều hướng chạy được; nội dung thật (force-read nội quy / FAQ cây / chat /
// housekeeping) nối API ở các slice FE.1 sau. Giữ responsive + safe-area nhất quán home.
const props = defineProps<{ section: string; icon: string }>();
const { t } = useI18n();
const router = useRouter();
</script>

<template>
  <div class="section-page">
    <header class="section-page__bar">
      <Button icon="pi pi-arrow-left" text rounded :aria-label="'back'" @click="router.push('/')" />
      <h1 class="section-page__title">{{ t(`home.${props.section}.title`) }}</h1>
    </header>
    <main class="section-page__body">
      <i :class="['pi', props.icon, 'section-page__icon']" aria-hidden="true"></i>
      <p class="section-page__desc">{{ t(`home.${props.section}.desc`) }}</p>
    </main>
  </div>
</template>

<style scoped>
.section-page {
  min-height: 100vh;
  min-height: 100svh;
  min-height: 100dvh;
  display: flex;
  flex-direction: column;
}
.section-page__bar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: var(--sh-space-page);
  padding-block-start: max(var(--sh-space-page), env(safe-area-inset-top));
  padding-inline: max(var(--sh-space-page), env(safe-area-inset-left)) max(var(--sh-space-page), env(safe-area-inset-right));
}
.section-page__title {
  margin: 0;
  font-size: var(--sh-font-title);
  font-weight: 700;
}
.section-page__body {
  flex: 1 1 auto;
  display: grid;
  place-content: center;
  justify-items: center;
  gap: 1rem;
  padding: var(--sh-space-page);
  text-align: center;
}
.section-page__icon {
  font-size: 3rem;
  color: var(--p-primary-color, #6366f1);
}
.section-page__desc {
  margin: 0;
  max-width: 28rem;
  color: var(--p-text-muted-color, #64748b);
}
</style>
