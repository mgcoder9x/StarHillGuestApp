<script setup lang="ts">
// Logo-mark tự vẽ (inline SVG, KHÔNG asset ngoài — license-clean): ngôi sao trên nền gradient bo góc.
// "Star Hill" → ngôi sao. Dùng token brand để hoà theme sáng/tối.
</script>

<template>
  <span class="brandmark" aria-hidden="true">
    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" xmlns="http://www.w3.org/2000/svg">
      <path
        d="M12 3.2l2.35 4.76 5.25.76-3.8 3.7.9 5.23L12 15.9l-4.7 2.47.9-5.23-3.8-3.7 5.25-.76L12 3.2z"
        fill="currentColor"
      />
    </svg>
  </span>
</template>

<style scoped>
.brandmark {
  display: inline-grid;
  place-items: center;
  width: 2.1rem;
  height: 2.1rem;
  flex: 0 0 auto;
  border-radius: 0.6rem;
  color: var(--sh-on-primary);
  background: linear-gradient(135deg, var(--sh-primary), var(--sh-primary-strong));
  box-shadow: var(--sh-shadow-card);
}
</style>
