<script setup lang="ts">
import { ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import Card from 'primevue/card';
import InputText from 'primevue/inputtext';
import Password from 'primevue/password';
import Button from 'primevue/button';
import Message from 'primevue/message';
import { useAuthStore } from '../stores/auth';
import { ApiError } from '../api/client';

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
    <Card class="login__card">
      <template #title>{{ t('login.title') }}</template>
      <template #content>
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
      </template>
    </Card>
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
}
.login__card {
  width: 100%;
  max-width: 24rem;
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
</style>
