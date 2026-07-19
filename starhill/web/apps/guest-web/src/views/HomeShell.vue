<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useJourneyCore } from '../core/journeyCore';

// Trang phòng THẬT (design-module 10): thẻ capability đọc từ core derived (INV1). Khoá theo canFaq/canChat/
// canHousekeeping; nếu mustReadRules → nhắc đọc nội quy. Nội quy luôn vào được (đọc). Bấm thẻ khoá → /rules.
const { t } = useI18n();
const router = useRouter();
const core = useJourneyCore();

const resortName = computed(() => core.context.value?.resort.name ?? 'Star Hill Resort');
const roomNumber = computed(() => core.context.value?.room.number ?? '—');
const features = computed(() => core.context.value?.features ?? null);

interface Tile {
  key: 'rules' | 'faq' | 'chat' | 'housekeeping';
  icon: string;
  route: string;
  available: boolean;
  visible: boolean;
}
const tiles = computed<Tile[]>(() => {
  const f = features.value;
  return [
    { key: 'rules', icon: 'pi-book', route: '/rules', available: true, visible: true },
    { key: 'faq', icon: 'pi-question-circle', route: '/faq', available: core.canFaq.value, visible: !!f?.faqEnabled },
    { key: 'chat', icon: 'pi-comments', route: '/chat', available: core.canChat.value, visible: !!f?.chatEnabled },
    { key: 'housekeeping', icon: 'pi-sparkles', route: '/housekeeping', available: core.canHousekeeping.value, visible: !!f?.housekeepingEnabled },
  ];
});

function open(tile: Tile): void {
  if (tile.key === 'rules' || tile.available) {
    void router.push(tile.route);
  } else {
    // Khoá do chưa ack → dẫn về đọc nội quy (server cũng sẽ 403 nếu cố vào).
    void router.push('/rules');
  }
}
</script>

<template>
  <main class="shell">
    <header class="shell__head">
      <div class="shell__brand"><span class="shell__mark">✦</span><strong>{{ resortName }}</strong></div>
      <span class="shell__room">{{ t('homeShell.room', { room: roomNumber }) }}</span>
    </header>

    <h1 class="shell__welcome">{{ t('homeShell.welcome', { resort: resortName }) }}</h1>
    <p v-if="core.mustReadRules.value" class="shell__notice" data-testid="must-read-rules">
      {{ t('homeShell.mustReadRules') }}
    </p>

    <section class="shell__grid">
      <button
        v-for="tile in tiles.filter((x) => x.visible)"
        :key="tile.key"
        class="tile"
        :class="{ 'tile--locked': !tile.available && tile.key !== 'rules' }"
        :data-testid="`tile-${tile.key}`"
        @click="open(tile)"
      >
        <span class="tile__icon"><i :class="['pi', tile.icon]" aria-hidden="true"></i></span>
        <span class="tile__body">
          <span class="tile__title">{{ t(`homeShell.${tile.key}`) }}</span>
          <span class="tile__desc">{{ t(`homeShell.${tile.key}Desc`) }}</span>
        </span>
        <span v-if="!tile.available && tile.key !== 'rules'" class="tile__lock">
          <i class="pi pi-lock" aria-hidden="true"></i> {{ t('homeShell.locked') }}
        </span>
      </button>
    </section>

    <footer class="shell__footer">{{ t('homeShell.footer') }}</footer>
  </main>
</template>

<style scoped>
.shell {
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
.shell__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}
.shell__brand {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #0f766e;
  font-size: 1.1rem;
}
.shell__mark {
  display: grid;
  width: 1.8rem;
  height: 1.8rem;
  place-items: center;
  color: #fff;
  background: #0f766e;
  border-radius: 0.55rem;
}
.shell__room {
  font-weight: 700;
  color: #0f766e;
  background: #ccfbf1;
  border-radius: 999px;
  padding: 0.2rem 0.7rem;
  font-size: 0.85rem;
}
.shell__welcome {
  margin: 0;
  font-size: clamp(1.35rem, 1.1rem + 1.8vw, 1.9rem);
  font-weight: 800;
}
.shell__notice {
  margin: 0;
  padding: 0.6rem 0.9rem;
  color: #b45309;
  background: #fef3c7;
  border-radius: 0.6rem;
  font-size: 0.9rem;
}
.shell__grid {
  display: grid;
  gap: var(--sh-gap, 1rem);
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 14rem), 1fr));
}
.tile {
  display: flex;
  align-items: center;
  gap: 0.9rem;
  text-align: left;
  min-height: 44px;
  padding: 1rem 1.1rem;
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 0.9rem;
  cursor: pointer;
  font: inherit;
  position: relative;
}
.tile--locked {
  opacity: 0.72;
}
.tile__icon {
  display: inline-grid;
  place-items: center;
  width: 2.6rem;
  height: 2.6rem;
  flex: 0 0 auto;
  border-radius: 0.7rem;
  font-size: 1.25rem;
  color: #0f766e;
  background: #ccfbf1;
}
.tile__body {
  display: flex;
  flex-direction: column;
  min-width: 0;
}
.tile__title {
  font-weight: 700;
}
.tile__desc {
  color: #64748b;
  font-size: 0.85rem;
}
.tile__lock {
  position: absolute;
  top: 0.5rem;
  right: 0.6rem;
  font-size: 0.68rem;
  color: #b45309;
}
.shell__footer {
  margin-top: auto;
  text-align: center;
  color: #94a3b8;
  font-size: 0.8rem;
}
</style>
