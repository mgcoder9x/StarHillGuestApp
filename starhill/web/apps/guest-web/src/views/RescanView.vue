<script setup lang="ts">
import { useI18n } from 'vue-i18n';

// Màn "Quét lại QR" (INV3): hiện khi cửa sổ 30' hết hạn / cookie mất (session_expired/guest_context_missing).
// Không có nút "tiếp tục" giả — khách phải quét QR vật lý (mở /r/{token}) để nối lại visit.
const { t } = useI18n();
</script>

<template>
  <main class="rescan">
    <section class="rescan__card">
      <span class="rescan__icon"><i class="pi pi-qrcode" aria-hidden="true"></i></span>
      <h1>{{ t('rescan.title') }}</h1>
      <p>{{ t('rescan.body') }}</p>
    </section>
  </main>
</template>

<style scoped>
.rescan {
  min-height: 100vh;
  min-height: 100svh;
  min-height: 100dvh;
  display: grid;
  place-items: center;
  padding: var(--sh-space-page, 1rem);
}
.rescan__card {
  width: min(100%, 26rem);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  text-align: center;
  padding: clamp(1.5rem, 5vw, 2.5rem);
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 1.25rem;
  box-shadow: 0 1rem 3rem rgba(15, 23, 42, 0.1);
}
.rescan__icon {
  display: grid;
  place-items: center;
  width: 3.5rem;
  height: 3.5rem;
  font-size: 1.8rem;
  color: #0f766e;
  background: #ccfbf1;
  border-radius: 1rem;
}
.rescan h1 {
  margin: 0.25rem 0 0;
  font-size: clamp(1.25rem, 4vw, 1.6rem);
}
.rescan p {
  margin: 0;
  color: #64748b;
}
</style>
