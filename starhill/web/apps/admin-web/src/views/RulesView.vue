<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import Button from 'primevue/button';
import Message from 'primevue/message';
import ProgressSpinner from 'primevue/progressspinner';
import Select from 'primevue/select';
import Tag from 'primevue/tag';
import { ApiError, api, type RuleAdminDraft, type RuleDraftPreview, type RulePublicationHistoryItem } from '../api/client';
import { useAuthStore } from '../stores/auth';
import RuleEditorPanel from '../components/RuleEditorPanel.vue';

const { t } = useI18n();
const auth = useAuthStore();

const preview = ref<RuleDraftPreview | null>(null);
const adminDraft = ref<RuleAdminDraft | null>(null);
const publications = ref<RulePublicationHistoryItem[]>([]);
const language = ref('default');
const previewLoading = ref(true);
const historyLoading = ref(true);
const adminLoading = ref(true);
const error = ref<string | null>(null);
const success = ref<string | null>(null);

const languageOptions = [
  { label: t('rules.languageDefault'), value: 'default' },
  { label: 'Tiếng Việt (vi)', value: 'vi' },
  { label: 'English (en)', value: 'en' },
  { label: '한국어 (ko)', value: 'ko' },
  { label: '中文 (zh)', value: 'zh' },
];

function errorMessage(cause: unknown): string {
  if (cause instanceof ApiError && (cause.code === 'configuration_unavailable' || cause.code === 'not_found')) {
    return t('rules.errors.configuration');
  }
  return t('rules.errors.load');
}

async function loadPreview(): Promise<void> {
  previewLoading.value = true;
  error.value = null;
  try {
    preview.value = await api.getRuleDraftPreview(language.value === 'default' ? null : language.value, auth.accessToken);
  } catch (cause) {
    preview.value = null;
    error.value = errorMessage(cause);
  } finally {
    previewLoading.value = false;
  }
}

async function loadHistory(): Promise<void> {
  historyLoading.value = true;
  error.value = null;
  try {
    const result = await api.getRulePublicationHistory(auth.accessToken);
    publications.value = result.publications;
  } catch (cause) {
    publications.value = [];
    error.value = errorMessage(cause);
  } finally {
    historyLoading.value = false;
  }
}

async function loadAdminDraft(): Promise<void> {
  adminLoading.value = true;
  error.value = null;
  try {
    adminDraft.value = await api.getRuleAdminDraft(auth.accessToken);
  } catch (cause) {
    adminDraft.value = null;
    error.value = errorMessage(cause);
  } finally {
    adminLoading.value = false;
  }
}

async function refresh(): Promise<void> {
  await Promise.all([loadAdminDraft(), loadPreview(), loadHistory()]);
}

async function onEditorChanged(): Promise<void> {
  success.value = t('rules.editor.saved');
  await Promise.all([loadAdminDraft(), loadPreview()]);
}

async function onPublished(): Promise<void> {
  success.value = t('rules.editor.published');
  await refresh();
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value));
}

function actorPreview(value: string | null): string {
  if (!value) return t('rules.systemActor');
  return `${value.slice(0, 8)}…`;
}

onMounted(refresh);
</script>

<template>
  <section class="rules">
    <div class="rules__head">
      <div>
        <h2 class="rules__title">{{ t('rules.title') }}</h2>
        <p class="rules__subtitle">{{ t('rules.subtitle') }}</p>
      </div>
      <div class="rules__tools">
        <label class="rules__language">
          <span>{{ t('rules.language') }}</span>
          <Select
            v-model="language"
            input-id="rules-language"
            :options="languageOptions"
            option-label="label"
            option-value="value"
            :aria-label="t('rules.language')"
            @change="loadPreview"
          />
        </label>
        <Button icon="pi pi-refresh" :label="t('rules.refresh')" outlined :loading="previewLoading || historyLoading" @click="refresh" />
      </div>
    </div>

    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>
    <Message v-if="success" severity="success" closable @close="success = null">{{ success }}</Message>

    <RuleEditorPanel :draft="adminDraft" :loading="adminLoading" @changed="onEditorChanged" @published="onPublished" />

    <div class="rules__grid">
      <article class="rules__panel rules__preview" aria-labelledby="rules-preview-title">
        <div class="rules__panel-head">
          <div>
            <p class="rules__eyebrow">{{ t('rules.previewEyebrow') }}</p>
            <h3 id="rules-preview-title" class="rules__panel-title">{{ t('rules.previewTitle') }}</h3>
          </div>
          <Tag v-if="preview" :value="preview.language" severity="info" />
        </div>

        <div v-if="previewLoading" class="rules__loading" aria-live="polite">
          <ProgressSpinner stroke-width="4" aria-label="loading" />
        </div>
        <Message v-else-if="preview?.sections.length === 0" severity="info" :closable="false">
          {{ t('rules.previewEmpty') }}
        </Message>
        <div v-else class="rules__sections">
          <article v-for="section in preview?.sections" :key="section.key" class="rules__section" data-testid="rules-preview-section">
            <div class="rules__section-meta">
              <span class="rules__section-key">{{ section.key }}</span>
              <span v-if="section.isRequired" class="rules__required">{{ t('rules.required') }}</span>
              <Tag v-if="section.isMissing" :value="t('rules.missing')" severity="danger" />
              <Tag v-else-if="section.isFallback" :value="t('rules.fallback')" severity="warn" />
            </div>
            <h4 class="rules__section-title">{{ section.title || t('rules.missingTitle') }}</h4>
            <div v-if="section.bodyHtmlSanitized" class="rules__body" v-html="section.bodyHtmlSanitized"></div>
            <p v-else class="rules__missing-copy">{{ t('rules.missingBody') }}</p>
            <div class="rules__section-foot">
              <span>{{ t('rules.resolvedLanguage', { language: section.resolvedLanguage }) }}</span>
              <span>{{ t('rules.readTime', { seconds: section.minReadSeconds }) }}</span>
              <span v-if="section.requireScrollEnd">{{ t('rules.mustScroll') }}</span>
            </div>
          </article>
        </div>
      </article>

      <article class="rules__panel rules__history" aria-labelledby="rules-history-title">
        <div class="rules__panel-head">
          <div>
            <p class="rules__eyebrow">{{ t('rules.historyEyebrow') }}</p>
            <h3 id="rules-history-title" class="rules__panel-title">{{ t('rules.historyTitle') }}</h3>
          </div>
          <span class="rules__history-count">{{ publications.length }}</span>
        </div>

        <div v-if="historyLoading" class="rules__loading" aria-live="polite">
          <ProgressSpinner stroke-width="4" aria-label="loading" />
        </div>
        <Message v-else-if="publications.length === 0" severity="info" :closable="false">
          {{ t('rules.historyEmpty') }}
        </Message>
        <p v-else class="rules__swipe-hint"><i class="pi pi-arrows-h" aria-hidden="true"></i> {{ t('rules.swipeHistory') }}</p>
        <div v-if="!historyLoading && publications.length > 0" class="rules__history-table-wrap">
          <table class="rules__history-table">
            <thead>
              <tr>
                <th scope="col">{{ t('rules.version') }}</th>
                <th scope="col">{{ t('rules.publishedAt') }}</th>
                <th scope="col">{{ t('rules.publishedBy') }}</th>
                <th scope="col">{{ t('rules.changeNote') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in publications" :key="item.publicationId" data-testid="rules-history-row" :class="{ 'rules__history-current': item.isCurrent }">
                <td>
                  <span class="rules__version">v{{ item.version }}</span>
                  <Tag v-if="item.isCurrent" :value="t('rules.current')" severity="success" />
                </td>
                <td>{{ formatDate(item.publishedAt) }}</td>
                <td><code :title="item.publishedByUserId ?? t('rules.systemActor')">{{ actorPreview(item.publishedByUserId) }}</code></td>
                <td>{{ item.changeNote || '—' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </article>
    </div>
  </section>
</template>

<style scoped>
.rules {
  display: flex;
  flex-direction: column;
  gap: var(--sh-gap);
}
.rules__head,
.rules__panel-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--sh-gap);
}
.rules__head {
  flex-wrap: wrap;
}
.rules__title {
  margin: 0;
  font-size: var(--sh-font-title);
  font-weight: 800;
  letter-spacing: -0.01em;
  color: var(--sh-text);
}
.rules__subtitle {
  margin: 0.15rem 0 0;
  color: var(--sh-text-muted);
  font-size: 0.9rem;
}
.rules__tools {
  display: flex;
  align-items: flex-end;
  gap: 0.65rem;
  flex-wrap: wrap;
}
.rules__language {
  display: grid;
  gap: 0.25rem;
  color: var(--sh-text-muted);
  font-size: 0.75rem;
  font-weight: 700;
}
.rules__language :deep(.p-select) {
  min-width: 12rem;
}
.rules__grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 22rem), 1fr));
  gap: var(--sh-gap);
  align-items: start;
}
.rules__panel {
  min-width: 0;
  padding: clamp(1rem, 0.8rem + 1vw, 1.5rem);
  background: var(--sh-surface-card);
  border: 1px solid var(--sh-border);
  border-radius: var(--sh-radius);
  box-shadow: var(--sh-shadow-card);
}
.rules__eyebrow {
  margin: 0 0 0.15rem;
  color: var(--sh-primary);
  font-size: 0.7rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}
.rules__panel-title {
  margin: 0;
  color: var(--sh-text);
  font-size: 1.1rem;
  font-weight: 800;
}
.rules__history-count {
  display: inline-grid;
  place-items: center;
  min-width: 2rem;
  min-height: 2rem;
  padding: 0 0.55rem;
  color: var(--sh-primary);
  background: var(--sh-primary-soft);
  border-radius: 999px;
  font-weight: 800;
}
.rules__loading {
  display: grid;
  place-items: center;
  min-height: 12rem;
}
.rules__loading :deep(.p-progressspinner) {
  width: 2.5rem;
  height: 2.5rem;
}
.rules__sections {
  display: grid;
  gap: 0.75rem;
  margin-top: 1.15rem;
}
.rules__section {
  min-width: 0;
  padding: 1rem;
  border: 1px solid var(--sh-border);
  border-radius: var(--sh-radius-sm);
  background: color-mix(in srgb, var(--sh-surface-card) 90%, var(--sh-primary-soft));
}
.rules__section-meta,
.rules__section-foot {
  display: flex;
  align-items: center;
  gap: 0.45rem;
  flex-wrap: wrap;
}
.rules__section-meta {
  color: var(--sh-text-muted);
  font-size: 0.74rem;
}
.rules__section-key {
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-weight: 700;
}
.rules__required {
  color: var(--sh-primary);
  font-weight: 700;
}
.rules__section-title {
  margin: 0.55rem 0 0.4rem;
  color: var(--sh-text);
  font-size: 1.05rem;
}
.rules__body {
  color: var(--sh-text);
  line-height: 1.7;
}
.rules__body :deep(p) {
  margin: 0.35rem 0;
}
.rules__body :deep(pre),
.rules__body :deep(table) {
  max-width: 100%;
  overflow-x: auto;
}
.rules__missing-copy {
  margin: 0;
  color: var(--sh-text-muted);
  font-style: italic;
}
.rules__section-foot {
  margin-top: 0.8rem;
  color: var(--sh-text-muted);
  font-size: 0.75rem;
}
.rules__section-foot span + span::before {
  content: '·';
  margin-right: 0.45rem;
}
.rules__history-table-wrap {
  margin-top: 1.15rem;
  overflow-x: auto;
  border: 1px solid var(--sh-border);
  border-radius: var(--sh-radius-sm);
}
.rules__swipe-hint {
  display: none;
  align-items: center;
  gap: 0.4rem;
  margin: 1rem 0 -0.45rem;
  color: var(--sh-text-muted);
  font-size: 0.75rem;
}
.rules__history-table {
  width: 100%;
  min-width: 36rem;
  border-collapse: collapse;
  color: var(--sh-text);
  font-size: 0.82rem;
}
.rules__history-table th,
.rules__history-table td {
  padding: 0.75rem 0.8rem;
  text-align: left;
  vertical-align: top;
  border-bottom: 1px solid var(--sh-border);
}
.rules__history-table th {
  color: var(--sh-text-muted);
  font-size: 0.7rem;
  font-weight: 800;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  background: var(--sh-surface-hover);
}
.rules__history-table tbody tr:last-child td {
  border-bottom: 0;
}
.rules__history-current td {
  background: color-mix(in srgb, var(--sh-primary-soft) 55%, transparent);
}
.rules__version {
  display: inline-block;
  margin-right: 0.4rem;
  font-weight: 800;
}
.rules__history-table code {
  color: var(--sh-text-muted);
  font-size: 0.74rem;
}
@media (max-width: 559px) {
  .rules__tools {
    width: 100%;
    align-items: stretch;
  }
  .rules__language,
  .rules__language :deep(.p-select),
  .rules__tools > .p-button {
    width: 100%;
  }
  .rules__swipe-hint {
    display: flex;
  }
}
</style>
