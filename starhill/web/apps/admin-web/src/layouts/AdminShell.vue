<script setup lang="ts">
import { ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import Button from 'primevue/button';
import Drawer from 'primevue/drawer';
import { useAuthStore } from '../stores/auth';
import { useTheme } from '../composables/useTheme';
import NavList from '../components/NavList.vue';
import BrandMark from '../components/BrandMark.vue';

const { t } = useI18n();
const router = useRouter();
const auth = useAuthStore();
const { isDark, toggle } = useTheme();
const drawerVisible = ref(false);

function logout(): void {
  auth.logout();
  void router.push({ name: 'login' });
}

function initials(name: string | null): string {
  if (!name) {
    return '?';
  }
  return name.trim().slice(0, 2).toUpperCase();
}
</script>

<template>
  <div class="admin">
    <!-- Sidebar cố định (desktop ≥lg) — CSS ẩn <lg. -->
    <aside class="admin__sidebar">
      <div class="admin__brand">
        <BrandMark />
        <span class="admin__brand-name">Star Hill</span>
      </div>
      <NavList />
      <div class="admin__sidebar-foot">
        <span class="admin__badge">{{ t('app.env') }}</span>
      </div>
    </aside>

    <!-- Drawer (mobile <lg) — mở bằng hamburger. -->
    <Drawer v-model:visible="drawerVisible" class="admin__drawer">
      <template #header>
        <div class="admin__brand">
          <BrandMark />
          <span class="admin__brand-name">Star Hill</span>
        </div>
      </template>
      <NavList @navigate="drawerVisible = false" />
    </Drawer>

    <div class="admin__main">
      <header class="admin__topbar">
        <Button
          class="admin__hamburger"
          icon="pi pi-bars"
          text
          rounded
          :aria-label="t('app.menu')"
          @click="drawerVisible = true"
        />
        <h1 class="admin__title">{{ t('app.title') }}</h1>
        <span class="admin__spacer"></span>
        <Button
          class="admin__theme"
          :icon="isDark() ? 'pi pi-sun' : 'pi pi-moon'"
          text
          rounded
          :aria-label="t('app.theme')"
          @click="toggle"
        />
        <div v-if="auth.username" class="admin__user" :title="auth.username">
          <span class="admin__avatar" aria-hidden="true">{{ initials(auth.username) }}</span>
          <span class="admin__user-name">{{ auth.username }}</span>
        </div>
        <Button icon="pi pi-sign-out" :label="t('app.logout')" text @click="logout" />
      </header>
      <main class="admin__content">
        <RouterView />
      </main>
    </div>
  </div>
</template>

<style scoped>
.admin {
  min-height: 100vh;
  min-height: 100svh;
  min-height: 100dvh;
  display: grid;
  grid-template-columns: 1fr;
  background: var(--sh-surface-ground);
}
.admin__sidebar {
  display: none; /* mobile: dùng Drawer. */
}
.admin__brand {
  display: flex;
  align-items: center;
  gap: 0.6rem;
}
.admin__brand-name {
  font-size: 1.15rem;
  font-weight: 800;
  letter-spacing: -0.01em;
  color: var(--sh-text);
}
.admin__sidebar-foot {
  margin-top: auto;
  padding-top: 1rem;
}
.admin__badge {
  display: inline-flex;
  align-items: center;
  padding: 0.2rem 0.55rem;
  font-size: 0.7rem;
  font-weight: 600;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  color: var(--sh-text-muted);
  background: var(--sh-surface-hover);
  border: 1px solid var(--sh-border);
  border-radius: 999px;
}
.admin__main {
  display: flex;
  flex-direction: column;
  min-width: 0; /* cho phép content co, chống tràn ngang trong grid. */
}
.admin__topbar {
  position: sticky;
  top: 0;
  z-index: 10;
  display: flex;
  align-items: center;
  gap: 0.4rem;
  min-height: var(--sh-topbar-h);
  padding: 0.4rem var(--sh-space-page);
  padding-block-start: max(0.4rem, env(safe-area-inset-top));
  border-bottom: 1px solid var(--sh-border);
  background: var(--sh-surface-topbar);
  backdrop-filter: saturate(1.2) blur(8px);
}
.admin__title {
  margin: 0;
  font-size: 1.02rem;
  font-weight: 700;
  color: var(--sh-text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.admin__spacer {
  flex: 1 1 auto;
}
.admin__user {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  min-width: 0;
  padding: 0.2rem 0.2rem 0.2rem 0.35rem;
}
.admin__avatar {
  display: inline-grid;
  place-items: center;
  width: 2rem;
  height: 2rem;
  flex: 0 0 auto;
  border-radius: 50%;
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--sh-on-primary);
  background: linear-gradient(135deg, var(--sh-primary), var(--sh-primary-strong));
}
.admin__user-name {
  color: var(--sh-text-muted);
  font-size: 0.9rem;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 10rem;
}
.admin__content {
  flex: 1 1 auto;
  padding: var(--sh-space-page);
  padding-inline: max(var(--sh-space-page), env(safe-area-inset-left)) max(var(--sh-space-page), env(safe-area-inset-right));
  container-type: inline-size;
}

/* <lg: ẩn tên user (giữ avatar) để topbar không chật trên phone. */
@media (max-width: 559px) {
  .admin__user-name {
    display: none;
  }
}

/* Desktop ≥1024px: sidebar cố định 2 cột; ẩn hamburger. */
@media (min-width: 1024px) {
  .admin {
    grid-template-columns: var(--sh-sidebar-w) 1fr;
  }
  .admin__sidebar {
    display: flex;
    flex-direction: column;
    padding: 1.1rem 0.85rem;
    border-right: 1px solid var(--sh-border);
    background: var(--sh-surface-sidebar);
  }
  .admin__brand {
    padding: 0.25rem 0.4rem 1rem;
  }
  .admin__hamburger {
    display: none;
  }
}
</style>
