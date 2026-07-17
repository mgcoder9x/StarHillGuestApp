import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { api } from '../api/client';

// Auth store: access-token GIỮ TRONG BỘ NHỚ (không localStorage — an toàn XSS, QR-AD-047). Refresh qua cookie
// httpOnly là bước sau (FE.2b). Mất token khi reload = quay lại login (chấp nhận cho MVP nội bộ).
export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(null);
  const username = ref<string | null>(null);

  const isAuthenticated = computed(() => accessToken.value !== null);

  async function login(user: string, password: string): Promise<void> {
    const result = await api.login(user, password);
    accessToken.value = result.accessToken;
    username.value = user;
  }

  function logout(): void {
    accessToken.value = null;
    username.value = null;
  }

  return { accessToken, username, isAuthenticated, login, logout };
});
