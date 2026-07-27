// DEMO-ONLY store (chỉ route `/demo` → HomeView mockup dùng). Đường guest THẬT dùng JourneyCore
// (`core/journeyCore.ts`) làm nguồn context duy nhất — QR-AD-057. Type lấy từ `core/apiGateway`
// (MỘT nguồn khai báo; module `api/guestApi.ts` cũ đã bị xoá vì nhân đôi GuestApiError → bẫy `instanceof`).
import { computed, ref, type ComputedRef } from 'vue';
import type { GuestResolveResponse } from '../core/apiGateway';

const STORAGE_KEY = 'starhill_guest_context_v1';
const context = ref<GuestResolveResponse | null>(null);
const publicContext = computed<GuestResolveResponse | null>(() => context.value);
let loaded = false;

function loadStoredContext(): void {
  if (loaded || typeof window === 'undefined') return;
  loaded = true;
  try {
    const raw = window.sessionStorage.getItem(STORAGE_KEY);
    context.value = raw ? (JSON.parse(raw) as GuestResolveResponse) : null;
  } catch {
    context.value = null;
  }
}

function setContext(value: GuestResolveResponse): void {
  context.value = value;
  try {
    window.sessionStorage.setItem(STORAGE_KEY, JSON.stringify(value));
  } catch {
    // The in-memory context is still enough for the current navigation.
  }
}

function clearContext(): void {
  context.value = null;
  try {
    window.sessionStorage.removeItem(STORAGE_KEY);
  } catch {
    // Ignore storage failures; the next QR scan can establish a new context.
  }
}

export function useGuestSession(): {
  context: ComputedRef<GuestResolveResponse | null>;
  setContext: (value: GuestResolveResponse) => void;
  clearContext: () => void;
} {
  loadStoredContext();
  return { context: publicContext, setContext, clearContext };
}
