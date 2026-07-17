import { ref, readonly } from 'vue';

// Dark/light theme cho admin. Nguồn quyết định (ưu tiên giảm dần): lựa chọn đã lưu (localStorage) →
// prefers-color-scheme của hệ điều hành → light. Áp bằng cách thêm/bỏ class `.dark` trên <html> — khớp
// PrimeVue darkModeSelector '.dark' (main.ts) nên PrimeVue + token --sh-* đảo màu đồng bộ MỘT nguồn.
// Lưu ý XSS/an toàn: chỉ đọc/ghi một khoá boolean, không eval, không nội dung người dùng.

const STORAGE_KEY = 'sh-admin-theme';
type ThemeMode = 'light' | 'dark';

function prefersDark(): boolean {
  return typeof window !== 'undefined' && window.matchMedia('(prefers-color-scheme: dark)').matches;
}

function readStored(): ThemeMode | null {
  try {
    const v = localStorage.getItem(STORAGE_KEY);
    return v === 'light' || v === 'dark' ? v : null;
  } catch {
    // localStorage có thể bị chặn (private mode) — bỏ qua, dùng mặc định hệ thống.
    return null;
  }
}

function resolveInitial(): ThemeMode {
  return readStored() ?? (prefersDark() ? 'dark' : 'light');
}

// State module-level (singleton) — mọi component chia sẻ cùng trạng thái theme.
const mode = ref<ThemeMode>(resolveInitial());

function apply(next: ThemeMode): void {
  const root = document.documentElement;
  root.classList.toggle('dark', next === 'dark');
  mode.value = next;
}

/** Áp theme hiện tại lên <html>. Gọi một lần khi khởi động app (trước khi render). */
export function initTheme(): void {
  apply(mode.value);
}

export function useTheme(): {
  mode: Readonly<typeof mode>;
  isDark: () => boolean;
  toggle: () => void;
} {
  function toggle(): void {
    const next: ThemeMode = mode.value === 'dark' ? 'light' : 'dark';
    apply(next);
    try {
      localStorage.setItem(STORAGE_KEY, next);
    } catch {
      // Không lưu được thì vẫn đổi trong phiên — không chặn UX.
    }
  }

  return { mode: readonly(mode), isDark: () => mode.value === 'dark', toggle };
}
