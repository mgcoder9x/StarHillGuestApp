<script setup lang="ts">
import { ref } from 'vue';
import Card from 'primevue/card';
import Button from 'primevue/button';

// Khung mẫu FE.0 — chứng minh chiến lược responsive §3 (dvh app-shell + safe-area + fluid + auto-fit grid +
// container-query) chạy thật trong browser (Playwright gate). Dữ liệu tĩnh; slice FE.1 nối API guest thật.
const rooms = ref([
  { id: 'A-101', status: 'Active' },
  { id: 'A-102', status: 'Active' },
  { id: 'B-201', status: 'Inactive' },
]);
</script>

<template>
  <div class="app-shell">
    <header class="app-header">
      <h1 class="app-title">Star Hill</h1>
    </header>

    <main class="app-main">
      <section class="card-grid">
        <Card v-for="r in rooms" :key="r.id">
          <template #title>{{ r.id }}</template>
          <template #content>
            <p class="room-status">{{ r.status }}</p>
          </template>
        </Card>
      </section>
    </main>

    <footer class="app-footer">
      <Button label="Xác nhận" fluid />
    </footer>
  </div>
</template>

<style scoped>
/* §3.1 app-shell full-height chống bug 100vh mobile: fallback tầng vh → svh → dvh (trình cũ nhận vh, mới nhận dvh). */
.app-shell {
  min-height: 100vh;
  min-height: 100svh;
  min-height: 100dvh;
  display: flex;
  flex-direction: column;
}

/* §3.3 safe-area: header/footer cộng env(safe-area-inset-*) để không bị notch/thanh gạt che. */
.app-header {
  padding: var(--sh-space-page);
  padding-block-start: max(var(--sh-space-page), env(safe-area-inset-top));
  padding-inline: max(var(--sh-space-page), env(safe-area-inset-left))
    max(var(--sh-space-page), env(safe-area-inset-right));
}

.app-title {
  margin: 0;
  font-size: var(--sh-font-title);
  font-weight: 700;
}

/* §3.4 container-type: component con thích ứng theo khung chứa (bền mọi bề rộng, không chỉ viewport). */
.app-main {
  flex: 1 1 auto;
  padding-inline: max(var(--sh-space-page), env(safe-area-inset-left))
    max(var(--sh-space-page), env(safe-area-inset-right));
  container-type: inline-size;
}

/* §3.5 auto-fit grid: tự rớt cột khi hẹp, KHÔNG media query. min(100%, …) chống tràn ở 320px. */
.card-grid {
  display: grid;
  gap: var(--sh-gap);
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 16rem), 1fr));
}

.room-status {
  margin: 0;
}

.app-footer {
  padding: var(--sh-space-page);
  padding-block-end: max(var(--sh-space-page), env(safe-area-inset-bottom));
  padding-inline: max(var(--sh-space-page), env(safe-area-inset-left))
    max(var(--sh-space-page), env(safe-area-inset-right));
}
</style>
