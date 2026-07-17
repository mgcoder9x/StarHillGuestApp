<script setup lang="ts">
import { ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import InputText from 'primevue/inputtext';
import Password from 'primevue/password';
import Button from 'primevue/button';
import Message from 'primevue/message';
import { useAuthStore } from '../stores/auth';
import { ApiError } from '../api/client';
import BrandMark from '../components/BrandMark.vue';

const { t } = useI18n();
const router = useRouter();
const auth = useAuthStore();

const username = ref('');
const password = ref('');
const error = ref<string | null>(null);
const loading = ref(false);

async function submit(): Promise<void> {
  error.value = null;
  loading.value = true;
  try {
    await auth.login(username.value, password.value);
    await router.push({ name: 'dashboard' });
  } catch (e) {
    // Mã ổn định `identity.invalid_credentials` (generic chống user-enumeration) → thông báo sai thông tin.
    error.value = e instanceof ApiError && e.code === 'identity.invalid_credentials'
      ? t('login.error')
      : t('login.generic');
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="login">
    <div class="login__card">
      <div class="login__brand">
        <BrandMark />
        <span class="login__brand-name">Star Hill</span>
      </div>
      <h1 class="login__title">{{ t('login.title') }}</h1>
      <p class="login__subtitle">{{ t('login.subtitle') }}</p>

      <form class="login__form" @submit.prevent="submit">
        <div class="login__field">
          <label for="username">{{ t('login.username') }}</label>
          <InputText id="username" v-model="username" autocomplete="username" fluid />
        </div>
        <div class="login__field">
          <label for="password">{{ t('login.password') }}</label>
          <Password input-id="password" v-model="password" :feedback="false" toggle-mask fluid />
        </div>
        <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>
        <Button type="submit" :label="t('login.submit')" :loading="loading" fluid />
      </form>
    </div>
  </div>
</template>

<style scoped>
.login {
  min-height: 100vh;
  min-height: 100svh;
  min-height: 100dvh;
  display: grid;
  place-items: center;
  padding: var(--sh-space-page);
  padding-block: max(var(--sh-space-page), env(safe-area-inset-top)) max(var(--sh-space-page), env(safe-area-inset-bottom));
  background:
    radial-gradient(1200px 600px at 100% 0%, var(--sh-primary-soft), transparent 60%),
    radial-gradient(900px 500px at 0% 100%, var(--sh-primary-soft), transparent 55%),
    var(--sh-surface-ground);
}
.login__card {
  width: 100%;
  max-width: 24rem;
  padding: clamp(1.5rem, 1rem + 3vw, 2.25rem);
  background: var(--sh-surface-card);
  border: 1px solid var(--sh-border);
  border-radius: var(--sh-radius);
  box-shadow: var(--sh-shadow-pop);
}
.login__brand {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  margin-bottom: 1.25rem;
}
.login__brand-name {
  font-size: 1.15rem;
  font-weight: 800;
  color: var(--sh-text);
}
.login__title {
  margin: 0;
  font-size: 1.35rem;
  font-weight: 800;
  color: var(--sh-text);
}
.login__subtitle {
  margin: 0.25rem 0 1.25rem;
  color: var(--sh-text-muted);
  font-size: 0.9rem;
}
.login__form {
  display: flex;
  flex-direction: column;
  gap: var(--sh-gap);
}
.login__field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}
.login__field label {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--sh-text);
}
</style>
