<script setup lang="ts">
import { useI18n } from 'vue-i18n';

// Điều hướng IA admin (Req 9). FE.2a: chỉ 'dashboard' có route thật; các mục khác hiển thị (planned IA) nhưng CHƯA
// route (render mờ, không click) — trung thực, không tạo route giả. Slice sau bật dần.
// FE.2b polish: gom nhóm có nhãn section (bố cục học từ dashboard admin phổ biến, tự dựng bằng token).
defineEmits<{ navigate: [] }>();

const { t } = useI18n();

interface NavItem {
  key: string;
  icon: string;
  to: string | null;
}
interface NavGroup {
  label: string;
  items: NavItem[];
}

const groups: NavGroup[] = [
  {
    label: 'overview',
    items: [{ key: 'dashboard', icon: 'pi-chart-bar', to: '/' }],
  },
  {
    label: 'operations',
    items: [
      { key: 'rooms', icon: 'pi-qrcode', to: '/rooms' },
      { key: 'inbox', icon: 'pi-comments', to: null },
      { key: 'housekeeping', icon: 'pi-sparkles', to: null },
    ],
  },
  {
    label: 'content',
    items: [
      { key: 'rules', icon: 'pi-book', to: null },
      { key: 'faq', icon: 'pi-question-circle', to: null },
      { key: 'settings', icon: 'pi-cog', to: null },
    ],
  },
];
</script>

<template>
  <nav class="navlist" aria-label="admin">
    <div v-for="group in groups" :key="group.label" class="navlist__group">
      <p class="navlist__section">{{ t(`nav.section.${group.label}`) }}</p>
      <template v-for="item in group.items" :key="item.key">
        <RouterLink v-if="item.to" :to="item.to" class="navlist__item" @click="$emit('navigate')">
          <i :class="['pi', item.icon]" aria-hidden="true"></i>
          <span>{{ t(`nav.${item.key}`) }}</span>
        </RouterLink>
        <span v-else class="navlist__item navlist__item--disabled" :aria-disabled="true">
          <i :class="['pi', item.icon]" aria-hidden="true"></i>
          <span>{{ t(`nav.${item.key}`) }}</span>
          <span class="navlist__soon">{{ t('nav.soon') }}</span>
        </span>
      </template>
    </div>
  </nav>
</template>

<style scoped>
.navlist {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.navlist__group {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}
.navlist__section {
  margin: 0 0 0.25rem;
  padding: 0 0.9rem;
  font-size: 0.68rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--sh-text-muted);
}
.navlist__item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.6rem 0.9rem;
  min-height: 44px; /* touch target ≥44px */
  border-radius: var(--sh-radius-sm);
  color: var(--sh-text);
  text-decoration: none;
  transition: background-color 0.15s ease, color 0.15s ease;
}
.navlist__item .pi {
  font-size: 1.05rem;
  color: var(--sh-text-muted);
}
.navlist__item:hover {
  background: var(--sh-surface-hover);
}
.navlist__item.router-link-active {
  background: var(--sh-primary-soft);
  color: var(--sh-primary);
  font-weight: 600;
}
.navlist__item.router-link-active .pi {
  color: var(--sh-primary);
}
.navlist__item--disabled {
  color: var(--sh-text-muted);
  cursor: default;
}
.navlist__item--disabled:hover {
  background: transparent;
}
.navlist__soon {
  margin-left: auto;
  font-size: 0.6rem;
  font-weight: 700;
  letter-spacing: 0.03em;
  text-transform: uppercase;
  color: var(--sh-text-muted);
  background: var(--sh-surface-hover);
  border-radius: 999px;
  padding: 0.1rem 0.4rem;
}
</style>
