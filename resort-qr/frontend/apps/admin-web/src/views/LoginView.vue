<template>
  <div class="login-page">
    <el-card class="login-card" shadow="always">
      <div class="brand">
        <span class="brand-mark">SH</span>
        <span>Star Hill Admin</span>
      </div>
      <el-form label-position="top" @submit.prevent="submit">
        <el-form-item label="Email">
          <el-input v-model="email" type="email" placeholder="staff@resort" />
        </el-form-item>
        <el-form-item label="Mật khẩu">
          <el-input v-model="password" type="password" show-password placeholder="••••••••" />
        </el-form-item>
        <el-button type="primary" native-type="submit" class="submit" :loading="false">Đăng nhập</el-button>
      </el-form>
      <p class="hint">Skeleton — luồng đăng nhập thật (POST /auth/login + refresh in-memory) nối ở lát cắt sau.</p>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();
const email = ref('');
const password = ref('');

function submit(): void {
  // Placeholder: chưa gọi API. Điều hướng tạm vào dashboard để xem khung.
  void router.push('/dashboard');
}
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #0f766e, #0b5850);
  padding: 24px;
}
.login-card {
  width: 360px;
  max-width: 100%;
}
.brand {
  display: flex;
  align-items: center;
  gap: 10px;
  font-weight: 700;
  font-size: 20px;
  margin-bottom: 18px;
}
.brand-mark {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: 9px;
  background: linear-gradient(135deg, #0f766e, #0b5850);
  color: #fff;
  font-size: 15px;
}
.submit {
  width: 100%;
}
.hint {
  color: #6b7a8d;
  font-size: 12px;
  margin: 14px 0 0;
}
</style>
