import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { api, configureAuthSession, type LoginResult } from '../api/client';

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

// Tokens stay in memory (never localStorage). A 401 rotates the refresh token and retries once; concurrent 401s
// share one refresh request so the backend's single-use refresh-token semantics remain intact.
export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(null);
  const refreshToken = ref<string | null>(null);
  const refreshTokenExpiresAt = ref<string | null>(null);
  const username = ref<string | null>(null);
  const role = ref<UserRole | null>(null);

  const isAuthenticated = computed(() => accessToken.value !== null);
  const isAdmin = computed(() => role.value === 'admin');

  function applyTokens(result: LoginResult): void {
    accessToken.value = result.accessToken;
    refreshToken.value = result.refreshToken;
    refreshTokenExpiresAt.value = result.refreshTokenExpiresAt;
    role.value = roleFromAccessToken(result.accessToken);
  }

  async function login(user: string, password: string): Promise<void> {
    const result = await api.login(user, password);
    applyTokens(result);
    username.value = user;
  }

  function logout(): void {
    accessToken.value = null;
    refreshToken.value = null;
    refreshTokenExpiresAt.value = null;
    username.value = null;
    role.value = null;
  }

  configureAuthSession({
    getRefreshToken: () => refreshToken.value,
    onTokensRefreshed: applyTokens,
    onSessionInvalid: logout,
  });

  return { accessToken, username, role, isAdmin, isAuthenticated, login, logout };
});
