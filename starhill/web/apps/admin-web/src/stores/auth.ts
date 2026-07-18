import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { api } from '../api/client';

export type UserRole = 'admin' | 'staff';

// Decode only for presentation. The API remains the authorization authority; this value only hides controls
// that a Staff user cannot use and must never be used as a security boundary.
function roleFromAccessToken(token: string): UserRole | null {
  const payload = token.split('.')[1];
  if (!payload) {
    return null;
  }

  try {
    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(payload.length / 4) * 4, '=');
    const claims = JSON.parse(atob(normalized)) as { role?: unknown };
    return claims.role === 'admin' || claims.role === 'staff' ? claims.role : null;
  } catch {
    return null;
  }
}

// Auth store: access-token GIỮ TRONG BỘ NHỚ (không localStorage — an toàn XSS, QR-AD-047). Refresh qua cookie
// httpOnly là bước sau (FE.2b). Mất token khi reload = quay lại login (chấp nhận cho MVP nội bộ).
export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(null);
  const username = ref<string | null>(null);
  const role = ref<UserRole | null>(null);

  const isAuthenticated = computed(() => accessToken.value !== null);
  const isAdmin = computed(() => role.value === 'admin');

  async function login(user: string, password: string): Promise<void> {
    const result = await api.login(user, password);
    accessToken.value = result.accessToken;
    username.value = user;
    role.value = roleFromAccessToken(result.accessToken);
  }

  function logout(): void {
    accessToken.value = null;
    username.value = null;
    role.value = null;
  }

  return { accessToken, username, role, isAdmin, isAuthenticated, login, logout };
});
