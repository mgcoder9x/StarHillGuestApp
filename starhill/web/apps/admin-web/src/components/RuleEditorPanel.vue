<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import Button from 'primevue/button';
import Checkbox from 'primevue/checkbox';
import Dialog from 'primevue/dialog';
import InputNumber from 'primevue/inputnumber';
import InputText from 'primevue/inputtext';
import Message from 'primevue/message';
import ProgressSpinner from 'primevue/progressspinner';
import Textarea from 'primevue/textarea';
import Tag from 'primevue/tag';
import { ApiError, api, type RuleAdminDraft } from '../api/client';
import { useAuthStore } from '../stores/auth';

const props = defineProps<{ draft: RuleAdminDraft | null; loading: boolean }>();
const emit = defineEmits<{ changed: []; published: [] }>();
const { t } = useI18n();
const auth = useAuthStore();

const selectedSectionId = ref<string | null>(null);
const selectedLanguage = ref('');
const sortOrder = ref(0);
const isRequired = ref(false);
const requireScrollEnd = ref(false);
const minReadSeconds = ref(0);
const title = ref('');
const bodyHtml = ref('');
const error = ref<string | null>(null);
const saving = ref(false);
const createVisible = ref(false);
const deleteVisible = ref(false);
const publishVisible = ref(false);
const changeNote = ref('');
const newKey = ref('');
const newSortOrder = ref(10);
const newIsRequired = ref(false);
const newRequireScrollEnd = ref(false);
const newMinReadSeconds = ref(0);

const selectedSection = computed(() => props.draft?.sections.find((section) => section.sectionId === selectedSectionId.value) ?? null);
const languages = computed(() => props.draft?.enabledLanguageCodes ?? []);
const selectedTranslation = computed(() => selectedSection.value?.translations.find((translation) => translation.languageCode === selectedLanguage.value) ?? null);

function errorMessage(cause: unknown): string {
  if (cause instanceof ApiError && cause.code === 'concurrency_conflict') return t('rules.errors.conflict');
  if (cause instanceof ApiError && cause.code === 'configuration_unavailable') return t('rules.errors.configuration');
  return t('rules.errors.mutation');
}

function syncForm(): void {
  const section = selectedSection.value;
  if (!section) return;
  selectedLanguage.value = languages.value.includes(selectedLanguage.value) ? selectedLanguage.value : (props.draft?.defaultLanguageCode ?? languages.value[0] ?? '');
  sortOrder.value = section.sortOrder;
  isRequired.value = section.isRequired;
  requireScrollEnd.value = section.requireScrollEnd;
  minReadSeconds.value = section.minReadSeconds;
  title.value = selectedTranslation.value?.title ?? '';
  bodyHtml.value = selectedTranslation.value?.bodyHtmlSanitized ?? '';
}

watch(() => props.draft, (draft) => {
  if (!draft) return;
  if (!draft.sections.some((section) => section.sectionId === selectedSectionId.value)) {
    selectedSectionId.value = draft.sections[0]?.sectionId ?? null;
  }
  syncForm();
}, { immediate: true });
watch([selectedSectionId, selectedLanguage], syncForm);

function selectSection(sectionId: string): void {
  selectedSectionId.value = sectionId;
  error.value = null;
}

async function saveSection(): Promise<void> {
  const section = selectedSection.value;
  if (!section) return;
  saving.value = true;
  error.value = null;
  try {
    await api.updateRuleSection(section.sectionId, {
      sortOrder: sortOrder.value,
      isRequired: isRequired.value,
      requireScrollEnd: requireScrollEnd.value,
      minReadSeconds: minReadSeconds.value,
      expectedRowVersion: section.rowVersion,
    }, auth.accessToken);
    emit('changed');
  } catch (cause) {
    error.value = errorMessage(cause);
  } finally {
    saving.value = false;
  }
}

async function saveTranslation(): Promise<void> {
  const section = selectedSection.value;
  if (!section || !selectedLanguage.value) return;
  saving.value = true;
  error.value = null;
  try {
    await api.upsertRuleTranslation(section.sectionId, selectedLanguage.value, {
      title: title.value.trim() || null,
      bodyHtml: bodyHtml.value.trim() || null,
      expectedRowVersion: selectedTranslation.value?.rowVersion ?? null,
    }, auth.accessToken);
    emit('changed');
  } catch (cause) {
    error.value = errorMessage(cause);
  } finally {
    saving.value = false;
  }
}

async function createSection(): Promise<void> {
  const key = newKey.value.trim();
  if (!key) {
    error.value = t('rules.errors.keyRequired');
    return;
  }
  saving.value = true;
  error.value = null;
  try {
    const result = await api.createRuleSection({
      key,
      sortOrder: newSortOrder.value,
      isRequired: newIsRequired.value,
      requireScrollEnd: newRequireScrollEnd.value,
      minReadSeconds: newMinReadSeconds.value,
    }, auth.accessToken);
    selectedSectionId.value = result.sectionId;
    createVisible.value = false;
    newKey.value = '';
    emit('changed');
  } catch (cause) {
    error.value = errorMessage(cause);
  } finally {
    saving.value = false;
  }
}

async function deleteSection(): Promise<void> {
  const section = selectedSection.value;
  if (!section) return;
  saving.value = true;
  error.value = null;
  try {
    await api.deleteRuleSection(section.sectionId, section.rowVersion, auth.accessToken);
    selectedSectionId.value = null;
    deleteVisible.value = false;
    emit('changed');
  } catch (cause) {
    error.value = errorMessage(cause);
  } finally {
    saving.value = false;
  }
}

async function publish(): Promise<void> {
  saving.value = true;
  error.value = null;
  try {
    await api.publishRules(changeNote.value.trim() || null, auth.accessToken);
    publishVisible.value = false;
    changeNote.value = '';
    emit('published');
  } catch (cause) {
    error.value = errorMessage(cause);
  } finally {
    saving.value = false;
  }
}

function openPublish(): void {
  changeNote.value = '';
  publishVisible.value = true;
}
</script>

<template>
  <article class="rule-editor" aria-labelledby="rules-editor-title">
    <div class="rule-editor__head">
      <div>
        <p class="rule-editor__eyebrow">{{ t('rules.editor.eyebrow') }}</p>
        <h3 id="rules-editor-title" class="rule-editor__title">{{ t('rules.editor.title') }}</h3>
        <p class="rule-editor__hint">{{ t('rules.editor.subtitle') }}</p>
      </div>
      <div class="rule-editor__actions">
        <Button icon="pi pi-plus" :label="t('rules.editor.addSection')" outlined :disabled="loading || saving" @click="createVisible = true" />
        <Button icon="pi pi-send" :label="t('rules.editor.publish')" :disabled="loading || saving || !draft?.sections.length" @click="openPublish" />
      </div>
    </div>

    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>
    <div v-if="loading" class="rule-editor__loading" aria-live="polite">
      <ProgressSpinner stroke-width="4" aria-label="loading" />
    </div>
    <Message v-else-if="draft && draft.sections.length === 0" severity="info" :closable="false">{{ t('rules.editor.empty') }}</Message>

    <div v-else-if="draft" class="rule-editor__grid">
      <div class="rule-editor__list" role="listbox" :aria-label="t('rules.editor.sections')">
        <button
          v-for="section in draft?.sections"
          :key="section.sectionId"
          type="button"
          class="rule-editor__section-button"
          :class="{ 'rule-editor__section-button--active': section.sectionId === selectedSectionId }"
          role="option"
          :data-testid="`rule-section-${section.key}`"
          :aria-selected="section.sectionId === selectedSectionId"
          @click="selectSection(section.sectionId)"
        >
          <span>
            <strong>{{ section.key }}</strong>
            <small>{{ t('rules.editor.sort', { value: section.sortOrder }) }}</small>
          </span>
          <Tag v-if="section.missingLanguages.length" :value="String(section.missingLanguages.length)" severity="warn" />
        </button>
      </div>

      <div v-if="selectedSection" class="rule-editor__form">
        <div class="rule-editor__form-head">
          <div>
            <span class="rule-editor__key">{{ selectedSection.key }}</span>
            <p class="rule-editor__version">{{ t('rules.editor.version', { value: selectedSection.rowVersion }) }}</p>
          </div>
          <Button icon="pi pi-trash" severity="danger" text :aria-label="t('rules.editor.delete')" :disabled="saving" @click="deleteVisible = true" />
        </div>

        <div class="rule-editor__fields">
          <label for="rule-section-sort">{{ t('rules.editor.sortOrder') }}<InputNumber v-model="sortOrder" input-id="rule-section-sort" :min="0" :use-grouping="false" fluid /></label>
          <label for="rule-section-read-time">{{ t('rules.editor.minReadSeconds') }}<InputNumber v-model="minReadSeconds" input-id="rule-section-read-time" :min="0" :max="3600" :use-grouping="false" fluid /></label>
          <label class="rule-editor__check"><Checkbox v-model="isRequired" binary /> {{ t('rules.required') }}</label>
          <label class="rule-editor__check"><Checkbox v-model="requireScrollEnd" binary /> {{ t('rules.mustScroll') }}</label>
        </div>
        <Button data-testid="rule-save-settings" icon="pi pi-save" :label="t('rules.editor.saveSettings')" :loading="saving" @click="saveSection" />

        <div class="rule-editor__translation">
          <div class="rule-editor__translation-head">
            <div>
              <p class="rule-editor__eyebrow">{{ t('rules.editor.translationEyebrow') }}</p>
              <h4>{{ t('rules.editor.translationTitle') }}</h4>
            </div>
            <div class="rule-editor__languages" role="tablist">
              <button v-for="language in languages" :key="language" type="button" role="tab" :data-testid="`rule-language-${language}`" :aria-selected="language === selectedLanguage" :class="{ active: language === selectedLanguage }" @click="selectedLanguage = language">{{ language }}</button>
            </div>
          </div>
          <p v-if="!selectedTranslation" class="rule-editor__new-translation">{{ t('rules.editor.newTranslation') }}</p>
          <label for="rule-translation-title">{{ t('rules.editor.translationTitleLabel') }}<InputText id="rule-translation-title" v-model="title" maxlength="300" fluid /></label>
          <label for="rule-translation-body">{{ t('rules.editor.bodyLabel') }}<Textarea id="rule-translation-body" v-model="bodyHtml" rows="8" auto-resize fluid /></label>
          <small class="rule-editor__sanitize-note">{{ t('rules.editor.sanitizeNote') }}</small>
          <Button data-testid="rule-save-translation" icon="pi pi-save" :label="t('rules.editor.saveTranslation')" :loading="saving" @click="saveTranslation" />
        </div>
      </div>
    </div>

    <Dialog v-model:visible="createVisible" modal :draggable="false" :style="{ width: 'min(94vw, 30rem)' }" :header="t('rules.editor.addSection')">
      <div class="rule-editor__dialog-form">
        <label>{{ t('rules.editor.key') }}<InputText v-model="newKey" maxlength="100" fluid /></label>
        <label>{{ t('rules.editor.sortOrder') }}<InputNumber v-model="newSortOrder" :min="0" :use-grouping="false" fluid /></label>
        <label>{{ t('rules.editor.minReadSeconds') }}<InputNumber v-model="newMinReadSeconds" :min="0" :max="3600" :use-grouping="false" fluid /></label>
        <label class="rule-editor__check"><Checkbox v-model="newIsRequired" binary /> {{ t('rules.required') }}</label>
        <label class="rule-editor__check"><Checkbox v-model="newRequireScrollEnd" binary /> {{ t('rules.mustScroll') }}</label>
      </div>
      <template #footer>
        <Button :label="t('common.cancel')" text :disabled="saving" @click="createVisible = false" />
        <Button icon="pi pi-plus" :label="t('rules.editor.addSection')" :loading="saving" @click="createSection" />
      </template>
    </Dialog>

    <Dialog
      v-model:visible="deleteVisible"
      modal
      :draggable="false"
      :closable="!saving"
      :dismissable-mask="!saving"
      :style="{ width: 'min(94vw, 28rem)' }"
      :header="t('rules.editor.deleteTitle')"
    >
      <p class="rule-editor__dialog-copy">{{ t('rules.editor.deleteConfirm', { key: selectedSection?.key }) }}</p>
      <template #footer>
        <Button :label="t('common.cancel')" text :disabled="saving" @click="deleteVisible = false" />
        <Button icon="pi pi-trash" severity="danger" :label="t('rules.editor.delete')" :loading="saving" @click="deleteSection" />
      </template>
    </Dialog>

    <Dialog
      v-model:visible="publishVisible"
      modal
      :draggable="false"
      :closable="!saving"
      :dismissable-mask="!saving"
      :style="{ width: 'min(94vw, 30rem)' }"
      :header="t('rules.editor.publishTitle')"
    >
      <div class="rule-editor__dialog-form">
        <label>{{ t('rules.editor.changeNote') }}<Textarea v-model="changeNote" rows="4" maxlength="500" auto-resize fluid /></label>
        <small class="rule-editor__sanitize-note">{{ t('rules.editor.publishHint') }}</small>
      </div>
      <template #footer>
        <Button :label="t('common.cancel')" text :disabled="saving" @click="publishVisible = false" />
        <Button icon="pi pi-send" :label="t('rules.editor.publish')" :loading="saving" @click="publish" />
      </template>
    </Dialog>
  </article>
</template>

<style scoped>
.rule-editor { padding: clamp(1rem, 0.8rem + 1vw, 1.5rem); background: var(--sh-surface-card); border: 1px solid var(--sh-border); border-radius: var(--sh-radius); box-shadow: var(--sh-shadow-card); }
.rule-editor__head, .rule-editor__form-head, .rule-editor__translation-head { display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; }
.rule-editor__actions { display: flex; flex-wrap: wrap; gap: 0.55rem; }
.rule-editor__eyebrow { margin: 0 0 0.15rem; color: var(--sh-primary); font-size: 0.7rem; font-weight: 800; letter-spacing: 0.08em; text-transform: uppercase; }
.rule-editor__title { margin: 0; color: var(--sh-text); font-size: 1.1rem; font-weight: 800; }
.rule-editor__hint, .rule-editor__version, .rule-editor__new-translation, .rule-editor__sanitize-note { margin: 0.25rem 0 0; color: var(--sh-text-muted); font-size: 0.8rem; }
.rule-editor__loading { display: grid; place-items: center; min-height: 10rem; }
.rule-editor__loading :deep(.p-progressspinner) { width: 2.5rem; height: 2.5rem; }
.rule-editor__grid { display: grid; grid-template-columns: minmax(11rem, 0.34fr) minmax(0, 1fr); gap: 1rem; margin-top: 1rem; }
.rule-editor__list { display: grid; align-content: start; gap: 0.4rem; }
.rule-editor__section-button { display: flex; align-items: center; justify-content: space-between; gap: 0.5rem; width: 100%; padding: 0.75rem; color: var(--sh-text); text-align: left; background: var(--sh-surface-hover); border: 1px solid var(--sh-border); border-radius: var(--sh-radius-sm); cursor: pointer; }
.rule-editor__section-button--active { color: var(--sh-primary); border-color: var(--sh-primary); background: var(--sh-primary-soft); }
.rule-editor__section-button span { display: grid; gap: 0.2rem; min-width: 0; }
.rule-editor__section-button small { color: var(--sh-text-muted); font-size: 0.72rem; }
.rule-editor__form { display: grid; gap: 1rem; min-width: 0; }
.rule-editor__key { font-family: ui-monospace, SFMono-Regular, Consolas, monospace; font-weight: 800; color: var(--sh-text); }
.rule-editor__fields, .rule-editor__dialog-form { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 0.8rem; }
.rule-editor__fields label, .rule-editor__dialog-form label, .rule-editor__translation label { display: grid; gap: 0.35rem; color: var(--sh-text); font-size: 0.82rem; font-weight: 700; }
.rule-editor__check { display: flex !important; align-items: center; gap: 0.45rem; min-height: 2.6rem; }
.rule-editor__translation { display: grid; gap: 0.7rem; padding-top: 1rem; border-top: 1px solid var(--sh-border); }
.rule-editor__translation h4 { margin: 0; color: var(--sh-text); }
.rule-editor__languages { display: flex; flex-wrap: wrap; gap: 0.3rem; }
.rule-editor__languages button { min-width: 2.5rem; padding: 0.35rem 0.55rem; color: var(--sh-text-muted); background: transparent; border: 1px solid var(--sh-border); border-radius: 999px; cursor: pointer; }
.rule-editor__languages button.active { color: var(--sh-primary); border-color: var(--sh-primary); background: var(--sh-primary-soft); font-weight: 800; }
.rule-editor__dialog-form { grid-template-columns: 1fr; }
.rule-editor__dialog-copy { margin: 0; color: var(--sh-text-muted); line-height: 1.6; }
@media (max-width: 700px) { .rule-editor__grid { grid-template-columns: 1fr; } .rule-editor__head, .rule-editor__translation-head { flex-direction: column; } }
@media (max-width: 460px) { .rule-editor__fields { grid-template-columns: 1fr; } .rule-editor__actions, .rule-editor__actions .p-button { width: 100%; } }
</style>
