<script setup lang="ts">
import { ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import Button from 'primevue/button';
import Drawer from 'primevue/drawer';
import { useAuthStore } from '../stores/auth';
import NavList from '../components/NavList.vue';

const { t } = useI18n();
const router = useRouter();
const auth = useAuthStore();
const drawerVisible = ref(false);

function logout(): void {
  auth.logout();
  void router.push({ name: 'login' });
}
</script>

<template>
  <div class="admin">
    <!-- Sidebar cố định (desktop ≥lg) — CSS ẩn <lg. -->
    <aside class="admin__sidebar">
      <div class="admin__brand">Star Hill</div>
      <NavList />
    </aside>

    <!-- Drawer (mobile <lg) — mở bằng hamburger. -->
    <Drawer v-model:visible="drawerVisible" :header="'Star Hill'">
      <NavList @navigate="drawerVisible = false" />
    </Drawer>

    <div class="admin__main">
      <header class="admin__topbar">
        <Button
          class="admin__hamburger"
          icon="pi pi-bars"
          text
          rounded
          :aria-label="'menu'"
          @click="drawerVisible = true"
        />
        <h1 class="admin__title">{{ t('app.title') }}</h1>
        <span class="admin__spacer"></span>
        <span v-if="auth.username" class="admin__user">{{ auth.username }}</span>
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
}
.admin__sidebar {
  display: none; /* mobile: dùng Drawer. */
}
.admin__brand {
  font-size: var(--sh-font-title);
  font-weight: 800;
  padding: 1rem 0.9rem;
}
.admin__main {
  display: flex;
  flex-direction: column;
  min-width: 0; /* cho phép content co, chống tràn ngang trong grid. */
}
.admin__topbar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem var(--sh-space-page);
  padding-block-start: max(0.5rem, env(safe-area-inset-top));
  border-bottom: 1px solid var(--p-content-border-color, #e2e8f0);
  background: var(--p-content-background, #fff);
}
.admin__title {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 700;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.admin__spacer {
  flex: 1 1 auto;
}
.admin__user {
  color: var(--p-text-muted-color, #64748b);
}
.admin__content {
  flex: 1 1 auto;
  padding: var(--sh-space-page);
  padding-inline: max(var(--sh-space-page), env(safe-area-inset-left)) max(var(--sh-space-page), env(safe-area-inset-right));
  container-type: inline-size;
}

/* Desktop ≥1024px: sidebar cố định 2 cột; ẩn hamburger. */
@media (min-width: 1024px) {
  .admin {
    grid-template-columns: var(--sh-sidebar-w) 1fr;
  }
  .admin__sidebar {
    display: block;
    border-right: 1px solid var(--p-content-border-color, #e2e8f0);
    background: var(--p-content-background, #fff);
  }
  .admin__hamburger {
    display: none;
  }
}
</style>
