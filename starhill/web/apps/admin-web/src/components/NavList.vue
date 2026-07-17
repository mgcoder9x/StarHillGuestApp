<script setup lang="ts">
import { useI18n } from 'vue-i18n';

// Điều hướng IA admin (Req 9). FE.2a: chỉ 'dashboard' có route thật; các mục khác hiển thị (planned IA) nhưng CHƯA
// route (render mờ, không click) — trung thực, không tạo route giả. Slice sau bật dần.
defineEmits<{ navigate: [] }>();

const { t } = useI18n();

const items = [
  { key: 'dashboard', icon: 'pi-chart-bar', to: '/' as string | null },
  { key: 'rooms', icon: 'pi-qrcode', to: null },
  { key: 'rules', icon: 'pi-book', to: null },
  { key: 'faq', icon: 'pi-question-circle', to: null },
  { key: 'inbox', icon: 'pi-comments', to: null },
  { key: 'housekeeping', icon: 'pi-sparkles', to: null },
  { key: 'settings', icon: 'pi-cog', to: null },
];
</script>

<template>
  <nav class="navlist" aria-label="admin">
    <template v-for="item in items" :key="item.key">
      <RouterLink v-if="item.to" :to="item.to" class="navlist__item" @click="$emit('navigate')">
        <i :class="['pi', item.icon]" aria-hidden="true"></i>
        <span>{{ t(`nav.${item.key}`) }}</span>
      </RouterLink>
      <span v-else class="navlist__item navlist__item--disabled" :aria-disabled="true">
        <i :class="['pi', item.icon]" aria-hidden="true"></i>
        <span>{{ t(`nav.${item.key}`) }}</span>
      </span>
    </template>
  </nav>
</template>

<style scoped>
.navlist {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}
.navlist__item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.7rem 0.9rem;
  min-height: 44px; /* touch target ≥44px */
  border-radius: 0.5rem;
  color: var(--p-text-color, #1e293b);
  text-decoration: none;
}
.navlist__item.router-link-active {
  background: var(--p-primary-100, #e0e7ff);
  color: var(--p-primary-color, #4f46e5);
  font-weight: 600;
}
.navlist__item--disabled {
  color: var(--p-text-muted-color, #94a3b8);
  cursor: default;
}
</style>
