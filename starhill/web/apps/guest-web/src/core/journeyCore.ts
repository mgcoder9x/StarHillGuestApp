// JourneyCore — nguồn sự thật DUY NHẤT cho "capability nào mở khoá" (design-module 10 §2/§4, kiến trúc B QR-AD-057).
// Reactive singleton module-level (khớp style guest-web sẵn có; KHÔNG thêm Pinia). DI-2: chỉ giữ session/visit +
// features + ruleAck + selectedLanguage; KHÔNG giữ dữ liệu FAQ/chat/ticket (view/slice giữ). DI-3: capability là
// derived getter (không bang trùng). DI-4: ruleAck từ SERVER (client-gate advisory, server 403 chốt — INV2).
import { computed, ref, type ComputedRef } from 'vue';
import { apiGateway, configureGatewayHooks, type GuestResolveResponse } from './apiGateway';
import { sessionPersistence } from './sessionPersistence';

const context = ref<GuestResolveResponse | null>(null);
const ackedVersion = ref<number | null>(null);
const currentRuleVersion = ref<number | null>(null);
const rescanNeeded = ref(false);
const selectedLanguage = ref<string | null>(null);

let loaded = false;
function ensureLoaded(): void {
  if (loaded || typeof window === 'undefined') return;
  loaded = true;
  const ctx = sessionPersistence.loadContext();
  if (ctx) {
    context.value = ctx;
    ackedVersion.value = sessionPersistence.loadAckedVersion(ctx.visit.id);
  }
}

// ---- Derived (INV1/INV3) ----
const roomId = computed(() => context.value?.room.id ?? null);
const features = computed(() => context.value?.features ?? null);
const isAckedCurrent = computed(
  () => ackedVersion.value !== null && currentRuleVersion.value !== null && ackedVersion.value === currentRuleVersion.value,
);
const mustRescan = computed(() => rescanNeeded.value || context.value === null);
const ackRequiredAny = computed(() => {
  const f = features.value;
  return !!f && (f.ruleAckRequiredForFaq || f.ruleAckRequiredForChat || f.ruleAckRequiredForHousekeeping);
});
const mustReadRules = computed(() => ackRequiredAny.value && !isAckedCurrent.value);
function capable(enabled: boolean | undefined, ackRequired: boolean | undefined): boolean {
  return !!enabled && (!ackRequired || isAckedCurrent.value);
}
const canFaq = computed(() => capable(features.value?.faqEnabled, features.value?.ruleAckRequiredForFaq));
const canChat = computed(() => capable(features.value?.chatEnabled, features.value?.ruleAckRequiredForChat));
const canHousekeeping = computed(() =>
  capable(features.value?.housekeepingEnabled, features.value?.ruleAckRequiredForHousekeeping),
);

// ---- Actions ----
function setContext(ctx: GuestResolveResponse): void {
  // Đổi visit → reset ack (INV4/DI-5).
  if (context.value && context.value.visit.id !== ctx.visit.id) {
    ackedVersion.value = null;
    currentRuleVersion.value = null;
    sessionPersistence.clear();
  }
  context.value = ctx;
  rescanNeeded.value = false;
  ackedVersion.value = sessionPersistence.loadAckedVersion(ctx.visit.id);
  sessionPersistence.saveContext(ctx);
}

/** Cập nhật version nội quy hiện hành (từ GET /guest/rules hoặc 403 refresh). */
function setCurrentRuleVersion(version: number): void {
  currentRuleVersion.value = version;
}

/** Ack thành công (server-authoritative). */
function setAcked(version: number): void {
  ackedVersion.value = version;
  currentRuleVersion.value = version;
  if (context.value) {
    sessionPersistence.saveAckedVersion(context.value.visit.id, version);
  }
}

function markRescanNeeded(): void {
  rescanNeeded.value = true;
}

/** 403 rule_ack_required: đảm bảo gate coi như CHƯA ack current (INV2). */
function markAckRequired(): void {
  // Nếu chưa biết currentVersion, đặt "khác ackedVersion" để mustReadRules=true.
  if (currentRuleVersion.value !== null && ackedVersion.value === currentRuleVersion.value) {
    ackedVersion.value = null;
  } else if (currentRuleVersion.value === null) {
    ackedVersion.value = null;
  }
}

function reset(): void {
  context.value = null;
  ackedVersion.value = null;
  currentRuleVersion.value = null;
  rescanNeeded.value = false;
  sessionPersistence.clear();
}

// Đăng ký hook cho ApiGateway (intercept cross-cutting) — MỘT lần.
let hooksWired = false;
function wireGatewayHooks(): void {
  if (hooksWired) return;
  hooksWired = true;
  configureGatewayHooks({
    onRescanNeeded: markRescanNeeded,
    onAckRequired: markAckRequired,
  });
}

export interface JourneyCore {
  context: ComputedRef<GuestResolveResponse | null>;
  roomId: ComputedRef<string | null>;
  features: ComputedRef<GuestResolveResponse['features'] | null>;
  selectedLanguage: typeof selectedLanguage;
  isAckedCurrent: ComputedRef<boolean>;
  mustRescan: ComputedRef<boolean>;
  mustReadRules: ComputedRef<boolean>;
  canFaq: ComputedRef<boolean>;
  canChat: ComputedRef<boolean>;
  canHousekeeping: ComputedRef<boolean>;
  setContext: (ctx: GuestResolveResponse) => void;
  setCurrentRuleVersion: (version: number) => void;
  setAcked: (version: number) => void;
  markRescanNeeded: () => void;
  reset: () => void;
}

export function useJourneyCore(): JourneyCore {
  ensureLoaded();
  wireGatewayHooks();
  return {
    context: computed(() => context.value),
    roomId,
    features,
    selectedLanguage,
    isAckedCurrent,
    mustRescan,
    mustReadRules,
    canFaq,
    canChat,
    canHousekeeping,
    setContext,
    setCurrentRuleVersion,
    setAcked,
    markRescanNeeded,
    reset,
  };
}

// Re-export cho tiện dùng ở view/router.
export { apiGateway };
