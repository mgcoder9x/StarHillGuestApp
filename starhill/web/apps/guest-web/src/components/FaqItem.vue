<script setup lang="ts">
import { ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useJourneyCore } from '../core/journeyCore';
import type { GuestFaqItem } from '../core/apiGateway';

// Item FAQ đệ quy (flow cha-con, Req 4.1/4.3). Câu hỏi = nút mở/đóng → hiện đáp án (sanitize server, INV8) + con.
// CTA "gửi tin về vấn đề này" (Req 4.6) → /chat với prefill (FE.5c tiêu thụ query.prefill).
const props = defineProps<{ item: GuestFaqItem; depth: number }>();
const { t } = useI18n();
const router = useRouter();
const core = useJourneyCore();
const open = ref(props.depth === 0 ? false : true);

function toggle(): void {
  open.value = !open.value;
}
function askAboutThis(): void {
  void router.push({ name: 'chat', query: { prefill: props.item.question ?? '' } });
}
</script>

<template>
  <div class="faq-item" :style="{ marginInlineStart: depth > 0 ? '0.75rem' : '0' }">
    <button class="faq-item__q" :aria-expanded="open" @click="toggle">
      <i :class="['pi', open ? 'pi-chevron-down' : 'pi-chevron-right']" aria-hidden="true"></i>
      <span>{{ item.question }}</span>
      <span v-if="item.isFallback" class="faq-item__fallback">{{ t('faqFlow.fallback', { lang: item.resolvedLanguage }) }}</span>
    </button>

    <div v-if="open" class="faq-item__body">
      <!-- eslint-disable-next-line vue/no-v-html — answerHtmlSanitized đã sanitize server (INV8) -->
      <div v-if="item.answerHtmlSanitized" class="faq-item__answer" v-html="item.answerHtmlSanitized"></div>

      <button v-if="core.canChat.value" class="faq-item__cta" @click="askAboutThis">
        <i class="pi pi-comments" aria-hidden="true"></i> {{ t('faqFlow.askAboutThis') }}
      </button>

      <FaqItem v-for="child in item.children" :key="child.id" :item="child" :depth="depth + 1" />
    </div>
  </div>
</template>

<style scoped>
.faq-item {
  border-left: 2px solid transparent;
}
.faq-item__q {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  width: 100%;
  text-align: left;
  min-height: 44px;
  padding: 0.6rem 0.4rem;
  background: none;
  border: none;
  border-bottom: 1px solid #f1f5f9;
  font: inherit;
  font-weight: 600;
  color: #0f172a;
  cursor: pointer;
}
.faq-item__q .pi {
  color: #0f766e;
  flex: 0 0 auto;
}
.faq-item__fallback {
  margin-left: auto;
  font-size: 0.7rem;
  font-weight: 500;
  color: #b45309;
  background: #fef3c7;
  border-radius: 999px;
  padding: 0.05rem 0.4rem;
}
.faq-item__body {
  padding: 0.25rem 0.4rem 0.5rem 1.6rem;
}
.faq-item__answer :where(p) {
  margin: 0 0 0.5rem;
  line-height: 1.6;
  color: #334155;
  overflow-wrap: anywhere;
}
.faq-item__cta {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  min-height: 44px;
  padding: 0.35rem 0.7rem;
  margin: 0.25rem 0 0.5rem;
  color: #0f766e;
  background: #ccfbf1;
  border: none;
  border-radius: 999px;
  font: inherit;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
}
</style>
