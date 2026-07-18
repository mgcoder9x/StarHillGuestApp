<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import Button from 'primevue/button';
import Checkbox from 'primevue/checkbox';
import Dialog from 'primevue/dialog';
import InputNumber from 'primevue/inputnumber';
import InputText from 'primevue/inputtext';
import Message from 'primevue/message';
import ProgressSpinner from 'primevue/progressspinner';
import Select from 'primevue/select';
import Tag from 'primevue/tag';
import Textarea from 'primevue/textarea';
import {
  ApiError,
  api,
  type FaqAdminCategory,
  type FaqAdminItem,
  type FaqAdminTree,
} from '../api/client';
import { useAuthStore } from '../stores/auth';

type SelectionKind = 'category' | 'item';
type MoveDirection = -1 | 1;

interface FlatItem {
  item: FaqAdminItem;
  depth: number;
}

const { t } = useI18n();
const auth = useAuthStore();

const tree = ref<FaqAdminTree | null>(null);
const loading = ref(true);
const saving = ref(false);
const error = ref<string | null>(null);
const success = ref<string | null>(null);
const selectedKind = ref<SelectionKind>('category');
const selectedId = ref<string | null>(null);
const selectedLanguage = ref('');

const categorySortOrder = ref(0);
const categoryActive = ref(true);
const categoryName = ref('');
const itemSortOrder = ref(0);
const itemActive = ref(true);
const itemParentId = ref<string | null>(null);
const itemQuestion = ref('');
const itemAnswer = ref('');

const createCategoryVisible = ref(false);
const createCategoryKey = ref('');
const createCategorySortOrder = ref(10);
const createCategoryActive = ref(true);
const createItemVisible = ref(false);
const createItemCategoryId = ref<string | null>(null);
const createItemParentId = ref<string | null>(null);
const createItemSortOrder = ref(10);
const createItemActive = ref(true);
const deleteVisible = ref(false);

const orderedCategories = computed(() =>
  [...(tree.value?.categories ?? [])].sort((left, right) => left.sortOrder - right.sortOrder || left.key.localeCompare(right.key)),
);

function sortedItems(items: FaqAdminItem[]): FaqAdminItem[] {
  return [...items].sort((left, right) => left.sortOrder - right.sortOrder || left.itemId.localeCompare(right.itemId));
}

function flattenItems(items: FaqAdminItem[], depth = 0): FlatItem[] {
  return sortedItems(items).flatMap((item) => [
    { item, depth },
    ...flattenItems(item.children, depth + 1),
  ]);
}

function findItem(items: FaqAdminItem[], itemId: string): FaqAdminItem | null {
  for (const item of items) {
    if (item.itemId === itemId) return item;
    const child = findItem(item.children, itemId);
    if (child) return child;
  }
  return null;
}

function findItemContext(itemId: string | null): { category: FaqAdminCategory; item: FaqAdminItem } | null {
  if (!itemId) return null;
  for (const category of tree.value?.categories ?? []) {
    const item = findItem(category.items, itemId);
    if (item) return { category, item };
  }
  return null;
}

const selectedItemContext = computed(() =>
  selectedKind.value === 'item' ? findItemContext(selectedId.value) : null,
);
const selectedItem = computed(() => selectedItemContext.value?.item ?? null);
const selectedCategory = computed(() => {
  if (selectedKind.value === 'item') return selectedItemContext.value?.category ?? null;
  return tree.value?.categories.find((category) => category.categoryId === selectedId.value) ?? null;
});
const languages = computed(() => tree.value?.enabledLanguageCodes ?? []);
const selectedCategoryTranslation = computed(() =>
  selectedCategory.value?.translations.find((translation) => translation.languageCode === selectedLanguage.value) ?? null,
);
const selectedItemTranslation = computed(() =>
  selectedItem.value?.translations.find((translation) => translation.languageCode === selectedLanguage.value) ?? null,
);

function categoryLabel(category: FaqAdminCategory): string {
  const translation = category.translations.find((item) => item.languageCode === selectedLanguage.value)
    ?? category.translations.find((item) => item.languageCode === tree.value?.defaultLanguageCode)
    ?? category.translations[0];
  return translation?.name?.trim() || category.key;
}

function itemLabel(item: FaqAdminItem): string {
  const translation = item.translations.find((entry) => entry.languageCode === selectedLanguage.value)
    ?? item.translations.find((entry) => entry.languageCode === tree.value?.defaultLanguageCode)
    ?? item.translations[0];
  return translation?.question?.trim() || t('faq.untitledQuestion');
}

function selectionExists(): boolean {
  if (!selectedId.value) return false;
  if (selectedKind.value === 'category') {
    return Boolean(tree.value?.categories.some((category) => category.categoryId === selectedId.value));
  }
  return Boolean(findItemContext(selectedId.value));
}

function syncForm(): void {
  selectedLanguage.value = languages.value.includes(selectedLanguage.value)
    ? selectedLanguage.value
    : (tree.value?.defaultLanguageCode ?? languages.value[0] ?? '');

  if (selectedKind.value === 'category' && selectedCategory.value) {
    categorySortOrder.value = selectedCategory.value.sortOrder;
    categoryActive.value = selectedCategory.value.isActive;
    categoryName.value = selectedCategoryTranslation.value?.name ?? '';
    return;
  }
  if (selectedItem.value) {
    itemSortOrder.value = selectedItem.value.sortOrder;
    itemActive.value = selectedItem.value.isActive;
    itemParentId.value = selectedItem.value.parentId;
    itemQuestion.value = selectedItemTranslation.value?.question ?? '';
    itemAnswer.value = selectedItemTranslation.value?.answerHtmlSanitized ?? '';
  }
}

watch(
  () => [tree.value, selectedKind.value, selectedId.value, selectedLanguage.value] as const,
  syncForm,
  { immediate: true },
);

function mutationError(cause: unknown): string {
  if (cause instanceof ApiError) {
    if (cause.code === 'concurrency_conflict') return t('faq.errors.conflict');
    if (cause.code === 'configuration_unavailable') return t('faq.errors.configuration');
    if (cause.code === 'faq_category_not_empty') return t('faq.errors.categoryNotEmpty');
    if (cause.code === 'faq_item_has_children') return t('faq.errors.itemHasChildren');
  }
  return t('faq.errors.mutation');
}

async function load(): Promise<void> {
  loading.value = true;
  error.value = null;
  try {
    tree.value = await api.getFaqAdminTree(auth.accessToken);
    if (!selectionExists()) {
      selectedKind.value = 'category';
      selectedId.value = orderedCategories.value[0]?.categoryId ?? null;
    }
  } catch (cause) {
    tree.value = null;
    error.value = cause instanceof ApiError && cause.code === 'configuration_unavailable'
      ? t('faq.errors.configuration')
      : t('faq.errors.load');
  } finally {
    loading.value = false;
  }
}

function selectCategory(categoryId: string): void {
  selectedKind.value = 'category';
  selectedId.value = categoryId;
  error.value = null;
}

function selectItem(itemId: string): void {
  selectedKind.value = 'item';
  selectedId.value = itemId;
  error.value = null;
}

async function runMutation(action: () => Promise<void>, message: string): Promise<void> {
  saving.value = true;
  error.value = null;
  try {
    await action();
    success.value = message;
    await load();
  } catch (cause) {
    error.value = mutationError(cause);
  } finally {
    saving.value = false;
  }
}

async function saveCategory(): Promise<void> {
  const category = selectedCategory.value;
  if (!category || selectedKind.value !== 'category') return;
  await runMutation(
    () => api.updateFaqCategory(category.categoryId, {
      sortOrder: categorySortOrder.value,
      isActive: categoryActive.value,
      expectedRowVersion: category.rowVersion,
    }, auth.accessToken),
    t('faq.saved'),
  );
}

async function saveCategoryTranslation(): Promise<void> {
  const category = selectedCategory.value;
  if (!category || selectedKind.value !== 'category' || !selectedLanguage.value) return;
  await runMutation(
    async () => {
      await api.upsertFaqCategoryTranslation(category.categoryId, selectedLanguage.value, {
        name: categoryName.value.trim() || null,
        expectedRowVersion: selectedCategoryTranslation.value?.rowVersion ?? null,
      }, auth.accessToken);
    },
    t('faq.saved'),
  );
}

async function saveItem(): Promise<void> {
  const item = selectedItem.value;
  if (!item) return;
  await runMutation(
    () => api.updateFaqItem(item.itemId, {
      parentId: itemParentId.value,
      sortOrder: itemSortOrder.value,
      isActive: itemActive.value,
      expectedRowVersion: item.rowVersion,
    }, auth.accessToken),
    t('faq.saved'),
  );
}

async function saveItemTranslation(): Promise<void> {
  const item = selectedItem.value;
  if (!item || !selectedLanguage.value) return;
  await runMutation(
    async () => {
      await api.upsertFaqItemTranslation(item.itemId, selectedLanguage.value, {
        question: itemQuestion.value.trim() || null,
        answerHtml: itemAnswer.value.trim() || null,
        expectedRowVersion: selectedItemTranslation.value?.rowVersion ?? null,
      }, auth.accessToken);
    },
    t('faq.saved'),
  );
}

function openCreateCategory(): void {
  createCategoryKey.value = '';
  createCategorySortOrder.value = (orderedCategories.value.at(-1)?.sortOrder ?? 0) + 10;
  createCategoryActive.value = true;
  createCategoryVisible.value = true;
}

async function createCategory(): Promise<void> {
  const key = createCategoryKey.value.trim();
  if (!key) {
    error.value = t('faq.errors.keyRequired');
    return;
  }
  saving.value = true;
  error.value = null;
  try {
    const result = await api.createFaqCategory({
      key,
      sortOrder: createCategorySortOrder.value,
      isActive: createCategoryActive.value,
    }, auth.accessToken);
    selectedKind.value = 'category';
    selectedId.value = result.categoryId;
    createCategoryVisible.value = false;
    success.value = t('faq.created');
    await load();
  } catch (cause) {
    error.value = mutationError(cause);
  } finally {
    saving.value = false;
  }
}

function openCreateItem(category: FaqAdminCategory, parentId: string | null = null): void {
  createItemCategoryId.value = category.categoryId;
  createItemParentId.value = parentId;
  createItemSortOrder.value = 10;
  createItemActive.value = true;
  createItemVisible.value = true;
}

async function createItem(): Promise<void> {
  if (!createItemCategoryId.value) {
    error.value = t('faq.errors.categoryRequired');
    return;
  }
  saving.value = true;
  error.value = null;
  try {
    const result = await api.createFaqItem({
      categoryId: createItemCategoryId.value,
      parentId: createItemParentId.value,
      sortOrder: createItemSortOrder.value,
      isActive: createItemActive.value,
    }, auth.accessToken);
    selectedKind.value = 'item';
    selectedId.value = result.itemId;
    createItemVisible.value = false;
    success.value = t('faq.created');
    await load();
  } catch (cause) {
    error.value = mutationError(cause);
  } finally {
    saving.value = false;
  }
}

async function deleteSelected(): Promise<void> {
  const category = selectedCategory.value;
  const item = selectedItem.value;
  saving.value = true;
  error.value = null;
  try {
    if (selectedKind.value === 'category' && category) {
      await api.deleteFaqCategory(category.categoryId, category.rowVersion, auth.accessToken);
    } else if (item) {
      await api.deleteFaqItem(item.itemId, item.rowVersion, auth.accessToken);
    } else {
      return;
    }
    selectedId.value = null;
    deleteVisible.value = false;
    success.value = t('faq.deleted');
    await load();
  } catch (cause) {
    error.value = mutationError(cause);
  } finally {
    saving.value = false;
  }
}

function swap<T>(items: T[], index: number, direction: MoveDirection): T[] {
  const target = index + direction;
  if (target < 0 || target >= items.length) return items;
  const reordered = [...items];
  [reordered[index], reordered[target]] = [reordered[target], reordered[index]];
  return reordered;
}

async function moveCategory(category: FaqAdminCategory, direction: MoveDirection): Promise<void> {
  const index = orderedCategories.value.findIndex((item) => item.categoryId === category.categoryId);
  const reordered = swap(orderedCategories.value, index, direction);
  if (reordered === orderedCategories.value) return;
  await runMutation(
    () => api.reorderFaqCategories(
      reordered.map((item, order) => ({ id: item.categoryId, sortOrder: (order + 1) * 10, expectedRowVersion: item.rowVersion })),
      auth.accessToken,
    ),
    t('faq.reordered'),
  );
}

function siblingItems(category: FaqAdminCategory, item: FaqAdminItem): FaqAdminItem[] {
  if (!item.parentId) return sortedItems(category.items);
  return sortedItems(findItem(category.items, item.parentId)?.children ?? []);
}

function canMoveItem(category: FaqAdminCategory, item: FaqAdminItem, direction: MoveDirection): boolean {
  const siblings = siblingItems(category, item);
  const index = siblings.findIndex((entry) => entry.itemId === item.itemId);
  return index + direction >= 0 && index + direction < siblings.length;
}

async function moveItem(category: FaqAdminCategory, item: FaqAdminItem, direction: MoveDirection): Promise<void> {
  const siblings = siblingItems(category, item);
  const index = siblings.findIndex((entry) => entry.itemId === item.itemId);
  const reordered = swap(siblings, index, direction);
  if (reordered === siblings) return;
  await runMutation(
    () => api.reorderFaqItems(
      category.categoryId,
      reordered.map((entry, order) => ({ id: entry.itemId, sortOrder: (order + 1) * 10, expectedRowVersion: entry.rowVersion })),
      auth.accessToken,
    ),
    t('faq.reordered'),
  );
}

function descendantIds(item: FaqAdminItem): Set<string> {
  const ids = new Set<string>([item.itemId]);
  const visit = (children: FaqAdminItem[]): void => {
    for (const child of children) {
      ids.add(child.itemId);
      visit(child.children);
    }
  };
  visit(item.children);
  return ids;
}

const itemParentOptions = computed(() => {
  const category = selectedCategory.value;
  const item = selectedItem.value;
  if (!category || !item) return [];
  const excluded = descendantIds(item);
  return flattenItems(category.items)
    .filter((entry) => !excluded.has(entry.item.itemId))
    .map((entry) => ({ label: '—'.repeat(entry.depth) + (entry.depth ? ' ' : '') + itemLabel(entry.item), value: entry.item.itemId }));
});

const createItemParentOptions = computed(() => {
  const category = tree.value?.categories.find((entry) => entry.categoryId === createItemCategoryId.value);
  return category
    ? flattenItems(category.items).map((entry) => ({ label: '—'.repeat(entry.depth) + (entry.depth ? ' ' : '') + itemLabel(entry.item), value: entry.item.itemId }))
    : [];
});

onMounted(load);
</script>

<template>
  <section class="faq">
    <div class="faq__head">
      <div>
        <p class="faq__eyebrow">{{ t('faq.eyebrow') }}</p>
        <h2 class="faq__title">{{ t('faq.title') }}</h2>
        <p class="faq__subtitle">{{ t('faq.subtitle') }}</p>
      </div>
      <div class="faq__tools">
        <Button icon="pi pi-refresh" :label="t('faq.refresh')" outlined :loading="loading" @click="load" />
        <Button icon="pi pi-plus" :label="t('faq.addCategory')" :disabled="loading || saving" @click="openCreateCategory" />
        <Button
          icon="pi pi-question-circle"
          :label="t('faq.addItem')"
          :disabled="loading || saving || !selectedCategory"
          @click="selectedCategory && openCreateItem(selectedCategory, selectedItem?.itemId ?? null)"
        />
      </div>
    </div>

    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>
    <Message v-if="success" severity="success" closable @close="success = null">{{ success }}</Message>

    <div v-if="loading" class="faq__loading" aria-live="polite">
      <ProgressSpinner stroke-width="4" aria-label="loading" />
    </div>
    <Message v-else-if="tree?.categories.length === 0" severity="info" :closable="false">{{ t('faq.empty') }}</Message>

    <div v-else-if="tree" class="faq__workspace">
      <aside class="faq-tree" :aria-label="t('faq.categories')">
        <div v-for="(category, categoryIndex) in orderedCategories" :key="category.categoryId" class="faq-tree__category">
          <div
            class="faq-tree__row faq-tree__row--category"
            :class="{ 'faq-tree__row--selected': selectedKind === 'category' && selectedId === category.categoryId }"
            role="treeitem"
            :aria-selected="selectedKind === 'category' && selectedId === category.categoryId"
          >
            <button
              type="button"
              class="faq-tree__select"
              :data-testid="'faq-category-' + category.key"
              @click="selectCategory(category.categoryId)"
            >
              <span class="faq-tree__copy">
                <strong>{{ categoryLabel(category) }}</strong>
                <small>{{ category.key }}</small>
              </span>
              <span class="faq-tree__tags">
                <Tag v-if="!category.isActive" :value="t('faq.inactive')" severity="secondary" />
                <Tag v-if="category.missingLanguages.length" :value="t('faq.missing', { count: category.missingLanguages.length })" severity="warn" />
              </span>
            </button>
            <div class="faq-tree__reorder">
              <Button icon="pi pi-angle-up" text rounded size="small" :aria-label="t('faq.reorderUp')" :disabled="saving || categoryIndex === 0" @click="moveCategory(category, -1)" />
              <Button icon="pi pi-angle-down" text rounded size="small" :aria-label="t('faq.reorderDown')" :disabled="saving || categoryIndex === orderedCategories.length - 1" @click="moveCategory(category, 1)" />
            </div>
          </div>

          <p v-if="category.items.length === 0" class="faq-tree__empty">{{ t('faq.emptyItems') }}</p>
          <div
            v-for="row in flattenItems(category.items)"
            :key="row.item.itemId"
            class="faq-tree__row faq-tree__row--item"
            :class="{ 'faq-tree__row--selected': selectedKind === 'item' && selectedId === row.item.itemId }"
            role="treeitem"
            :aria-level="row.depth + 2"
            :aria-selected="selectedKind === 'item' && selectedId === row.item.itemId"
          >
            <button
              type="button"
              class="faq-tree__select"
              :style="{ paddingLeft: (0.65 + row.depth) + 'rem' }"
              :data-testid="'faq-item-' + row.item.itemId"
              @click="selectItem(row.item.itemId)"
            >
              <i class="pi pi-angle-right faq-tree__branch" aria-hidden="true"></i>
              <span class="faq-tree__copy">
                <strong>{{ itemLabel(row.item) }}</strong>
                <small>{{ t('faq.sortOrder') }} {{ row.item.sortOrder }}</small>
              </span>
              <span class="faq-tree__tags">
                <Tag v-if="!row.item.isActive" :value="t('faq.inactive')" severity="secondary" />
                <Tag v-if="row.item.missingLanguages.length" :value="String(row.item.missingLanguages.length)" severity="warn" />
              </span>
            </button>
            <div class="faq-tree__reorder">
              <Button icon="pi pi-angle-up" text rounded size="small" :aria-label="t('faq.reorderUp')" :disabled="saving || !canMoveItem(category, row.item, -1)" @click="moveItem(category, row.item, -1)" />
              <Button icon="pi pi-angle-down" text rounded size="small" :aria-label="t('faq.reorderDown')" :disabled="saving || !canMoveItem(category, row.item, 1)" @click="moveItem(category, row.item, 1)" />
            </div>
          </div>
        </div>
      </aside>

      <article class="faq-editor">
        <p v-if="!selectedCategory" class="faq-editor__hint">{{ t('faq.selectHint') }}</p>

        <template v-else-if="selectedKind === 'category'">
          <div class="faq-editor__head">
            <div>
              <p class="faq__eyebrow">{{ t('faq.categorySettings') }}</p>
              <h3>{{ selectedCategory.key }}</h3>
              <small>{{ t('faq.version', { value: selectedCategory.rowVersion }) }}</small>
            </div>
            <Button icon="pi pi-trash" severity="danger" text :aria-label="t('faq.deleteCategoryTitle')" :disabled="saving" @click="deleteVisible = true" />
          </div>
          <div class="faq-editor__fields">
            <label for="faq-category-sort">{{ t('faq.sortOrder') }}<InputNumber v-model="categorySortOrder" input-id="faq-category-sort" :min="0" :use-grouping="false" fluid /></label>
            <label class="faq-editor__check"><Checkbox v-model="categoryActive" binary /> {{ t('faq.active') }}</label>
          </div>
          <Button data-testid="faq-save-category" icon="pi pi-save" :label="t('faq.saveCategory')" :loading="saving" @click="saveCategory" />

          <div class="faq-editor__translation">
            <div class="faq-editor__translation-head">
              <div>
                <p class="faq__eyebrow">{{ t('faq.translationEyebrow') }}</p>
                <h4>{{ t('faq.translationCategoryTitle') }}</h4>
              </div>
              <div class="faq-editor__languages" role="tablist">
                <button v-for="language in languages" :key="language" type="button" role="tab" :data-testid="'faq-language-' + language" :aria-selected="language === selectedLanguage" :class="{ active: language === selectedLanguage }" @click="selectedLanguage = language">{{ language }}</button>
              </div>
            </div>
            <label for="faq-category-name">{{ t('faq.categoryName') }}<InputText id="faq-category-name" v-model="categoryName" maxlength="300" fluid /></label>
            <Button data-testid="faq-save-category-translation" icon="pi pi-save" :label="t('faq.saveTranslation')" :loading="saving" @click="saveCategoryTranslation" />
          </div>
        </template>

        <template v-else-if="selectedItem">
          <div class="faq-editor__head">
            <div>
              <p class="faq__eyebrow">{{ t('faq.itemSettings') }}</p>
              <h3>{{ itemLabel(selectedItem) }}</h3>
              <small>{{ t('faq.version', { value: selectedItem.rowVersion }) }}</small>
            </div>
            <Button icon="pi pi-trash" severity="danger" text :aria-label="t('faq.deleteItemTitle')" :disabled="saving" @click="deleteVisible = true" />
          </div>
          <div class="faq-editor__fields">
            <label for="faq-item-sort">{{ t('faq.sortOrder') }}<InputNumber v-model="itemSortOrder" input-id="faq-item-sort" :min="0" :use-grouping="false" fluid /></label>
            <label for="faq-item-parent">{{ t('faq.parent') }}<Select v-model="itemParentId" input-id="faq-item-parent" :options="itemParentOptions" option-label="label" option-value="value" show-clear :placeholder="t('faq.noParent')" fluid /></label>
            <label class="faq-editor__check"><Checkbox v-model="itemActive" binary /> {{ t('faq.active') }}</label>
          </div>
          <Button data-testid="faq-save-item" icon="pi pi-save" :label="t('faq.saveItem')" :loading="saving" @click="saveItem" />

          <div class="faq-editor__translation">
            <div class="faq-editor__translation-head">
              <div>
                <p class="faq__eyebrow">{{ t('faq.translationEyebrow') }}</p>
                <h4>{{ t('faq.translationItemTitle') }}</h4>
              </div>
              <div class="faq-editor__languages" role="tablist">
                <button v-for="language in languages" :key="language" type="button" role="tab" :data-testid="'faq-language-' + language" :aria-selected="language === selectedLanguage" :class="{ active: language === selectedLanguage }" @click="selectedLanguage = language">{{ language }}</button>
              </div>
            </div>
            <label for="faq-item-question">{{ t('faq.question') }}<InputText id="faq-item-question" v-model="itemQuestion" maxlength="500" fluid /></label>
            <label for="faq-item-answer">{{ t('faq.answer') }}<Textarea id="faq-item-answer" v-model="itemAnswer" rows="9" auto-resize fluid /></label>
            <small class="faq-editor__hint">{{ t('faq.publishHint') }}</small>
            <Button data-testid="faq-save-item-translation" icon="pi pi-save" :label="t('faq.saveTranslation')" :loading="saving" @click="saveItemTranslation" />
          </div>
        </template>
      </article>
    </div>

    <Dialog v-model:visible="createCategoryVisible" modal :draggable="false" :closable="!saving" :dismissable-mask="!saving" :style="{ width: 'min(94vw, 30rem)' }" :header="t('faq.createCategoryTitle')">
      <div class="faq-dialog__form">
        <label for="faq-new-category-key">{{ t('faq.key') }}<InputText id="faq-new-category-key" v-model="createCategoryKey" maxlength="100" fluid /></label>
        <label for="faq-new-category-sort">{{ t('faq.sortOrder') }}<InputNumber v-model="createCategorySortOrder" input-id="faq-new-category-sort" :min="0" :use-grouping="false" fluid /></label>
        <label class="faq-editor__check"><Checkbox v-model="createCategoryActive" binary /> {{ t('faq.active') }}</label>
      </div>
      <template #footer>
        <Button :label="t('common.cancel')" text :disabled="saving" @click="createCategoryVisible = false" />
        <Button icon="pi pi-plus" :label="t('faq.addCategory')" :loading="saving" @click="createCategory" />
      </template>
    </Dialog>

    <Dialog v-model:visible="createItemVisible" modal :draggable="false" :closable="!saving" :dismissable-mask="!saving" :style="{ width: 'min(94vw, 30rem)' }" :header="t('faq.createItemTitle')">
      <div class="faq-dialog__form">
        <label for="faq-new-item-parent">{{ t('faq.parent') }}<Select v-model="createItemParentId" input-id="faq-new-item-parent" :options="createItemParentOptions" option-label="label" option-value="value" show-clear :placeholder="t('faq.noParent')" fluid /></label>
        <label for="faq-new-item-sort">{{ t('faq.sortOrder') }}<InputNumber v-model="createItemSortOrder" input-id="faq-new-item-sort" :min="0" :use-grouping="false" fluid /></label>
        <label class="faq-editor__check"><Checkbox v-model="createItemActive" binary /> {{ t('faq.active') }}</label>
      </div>
      <template #footer>
        <Button :label="t('common.cancel')" text :disabled="saving" @click="createItemVisible = false" />
        <Button icon="pi pi-plus" :label="t('faq.addItem')" :loading="saving" @click="createItem" />
      </template>
    </Dialog>

    <Dialog v-model:visible="deleteVisible" modal :draggable="false" :closable="!saving" :dismissable-mask="!saving" :style="{ width: 'min(94vw, 28rem)' }" :header="selectedKind === 'category' ? t('faq.deleteCategoryTitle') : t('faq.deleteItemTitle')">
      <p class="faq-dialog__copy">
        {{ selectedKind === 'category' ? t('faq.deleteCategoryConfirm', { key: selectedCategory?.key }) : t('faq.deleteItemConfirm') }}
      </p>
      <template #footer>
        <Button :label="t('common.cancel')" text :disabled="saving" @click="deleteVisible = false" />
        <Button icon="pi pi-trash" severity="danger" :label="selectedKind === 'category' ? t('faq.deleteCategoryTitle') : t('faq.deleteItemTitle')" :loading="saving" @click="deleteSelected" />
      </template>
    </Dialog>
  </section>
</template>

<style scoped>
.faq { display: flex; flex-direction: column; gap: var(--sh-gap); }
.faq__head, .faq-editor__head, .faq-editor__translation-head { display: flex; align-items: flex-start; justify-content: space-between; gap: var(--sh-gap); }
.faq__head { flex-wrap: wrap; }
.faq__eyebrow { margin: 0 0 0.15rem; color: var(--sh-primary); font-size: 0.7rem; font-weight: 800; letter-spacing: 0.08em; text-transform: uppercase; }
.faq__title { margin: 0; color: var(--sh-text); font-size: var(--sh-font-title); font-weight: 800; letter-spacing: -0.01em; }
.faq__subtitle { margin: 0.15rem 0 0; color: var(--sh-text-muted); font-size: 0.9rem; }
.faq__tools { display: flex; flex-wrap: wrap; gap: 0.55rem; }
.faq__loading { display: grid; place-items: center; min-height: 16rem; }
.faq__loading :deep(.p-progressspinner) { width: 2.75rem; height: 2.75rem; }
.faq__workspace { display: grid; grid-template-columns: minmax(18rem, 0.42fr) minmax(0, 1fr); gap: var(--sh-gap); align-items: start; }
.faq-tree, .faq-editor { min-width: 0; padding: 1rem; background: var(--sh-surface-card); border: 1px solid var(--sh-border); border-radius: var(--sh-radius); box-shadow: var(--sh-shadow-card); }
.faq-tree { display: grid; gap: 0.8rem; }
.faq-tree__category { display: grid; gap: 0.25rem; }
.faq-tree__row { display: flex; align-items: stretch; min-width: 0; border: 1px solid transparent; border-radius: var(--sh-radius-sm); background: var(--sh-surface-hover); }
.faq-tree__row--category { background: color-mix(in srgb, var(--sh-primary-soft) 60%, var(--sh-surface-card)); }
.faq-tree__row--selected { border-color: var(--sh-primary); box-shadow: inset 3px 0 0 var(--sh-primary); }
.faq-tree__select { display: flex; align-items: center; gap: 0.55rem; min-width: 0; flex: 1 1 auto; padding: 0.65rem; color: var(--sh-text); text-align: left; background: transparent; border: 0; cursor: pointer; }
.faq-tree__copy { display: grid; gap: 0.12rem; min-width: 0; flex: 1 1 auto; }
.faq-tree__copy strong { overflow: hidden; text-overflow: ellipsis; font-size: 0.86rem; }
.faq-tree__copy small, .faq-tree__empty { color: var(--sh-text-muted); font-size: 0.7rem; }
.faq-tree__tags { display: flex; justify-content: flex-end; flex-wrap: wrap; gap: 0.25rem; }
.faq-tree__branch { color: var(--sh-text-muted); font-size: 0.65rem; }
.faq-tree__reorder { display: flex; align-items: center; padding-right: 0.15rem; }
.faq-tree__empty { margin: 0.2rem 0 0.2rem 0.75rem; }
.faq-editor { display: grid; gap: 1rem; }
.faq-editor__head h3, .faq-editor__translation h4 { margin: 0; color: var(--sh-text); }
.faq-editor__head small, .faq-editor__hint { color: var(--sh-text-muted); font-size: 0.78rem; }
.faq-editor__fields { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 0.8rem; }
.faq-editor__fields label, .faq-editor__translation label, .faq-dialog__form label { display: grid; gap: 0.35rem; color: var(--sh-text); font-size: 0.82rem; font-weight: 700; }
.faq-editor__check { display: flex !important; align-items: center; gap: 0.45rem; min-height: 2.6rem; }
.faq-editor__translation { display: grid; gap: 0.75rem; padding-top: 1rem; border-top: 1px solid var(--sh-border); }
.faq-editor__languages { display: flex; flex-wrap: wrap; gap: 0.3rem; }
.faq-editor__languages button { min-width: 2.5rem; padding: 0.35rem 0.55rem; color: var(--sh-text-muted); background: transparent; border: 1px solid var(--sh-border); border-radius: 999px; cursor: pointer; }
.faq-editor__languages button.active { color: var(--sh-primary); border-color: var(--sh-primary); background: var(--sh-primary-soft); font-weight: 800; }
.faq-dialog__form { display: grid; gap: 0.8rem; }
.faq-dialog__copy { margin: 0; color: var(--sh-text-muted); line-height: 1.6; }
@media (max-width: 880px) { .faq__workspace { grid-template-columns: 1fr; } }
@media (max-width: 600px) { .faq__tools, .faq__tools .p-button { width: 100%; } .faq-editor__fields { grid-template-columns: 1fr; } .faq-editor__translation-head { flex-direction: column; } .faq-tree__tags { display: none; } }
</style>
