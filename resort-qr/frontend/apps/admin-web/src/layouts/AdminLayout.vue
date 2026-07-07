<template>
  <el-container class="admin-shell">
    <!-- Sidebar dọc (Vuexy-inspired: brand + vertical menu, skin light) -->
    <el-aside width="240px" class="sidebar">
      <div class="brand">
        <span class="brand-mark">SH</span>
        <span class="brand-text">Star Hill</span>
      </div>
      <el-menu :router="true" :default-active="route.path" class="side-menu">
        <el-menu-item index="/dashboard">
          <el-icon><Odometer /></el-icon>
          <span>Tổng quan</span>
        </el-menu-item>
        <el-menu-item index="/inbox" disabled>
          <el-icon><ChatDotRound /></el-icon>
          <span>Hộp thư</span>
        </el-menu-item>
        <el-menu-item index="/housekeeping" disabled>
          <el-icon><Brush /></el-icon>
          <span>Dọn phòng</span>
        </el-menu-item>
        <el-menu-item index="/rules" disabled>
          <el-icon><Document /></el-icon>
          <span>Nội quy</span>
        </el-menu-item>
        <el-menu-item index="/faq" disabled>
          <el-icon><QuestionFilled /></el-icon>
          <span>Hỏi đáp</span>
        </el-menu-item>
        <el-menu-item index="/rooms" disabled>
          <el-icon><House /></el-icon>
          <span>Phòng &amp; QR</span>
        </el-menu-item>
        <el-menu-item index="/settings" disabled>
          <el-icon><Setting /></el-icon>
          <span>Cấu hình</span>
        </el-menu-item>
      </el-menu>
    </el-aside>

    <el-container>
      <!-- Navbar trên -->
      <el-header class="navbar">
        <div class="page-title">{{ pageTitle }}</div>
        <div class="navbar-right">
          <el-tag type="info" size="small" effect="plain">Prototype</el-tag>
          <el-dropdown>
            <span class="user">
              <el-icon><User /></el-icon>
              <span>Lễ tân</span>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click="logout">Đăng xuất</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>

      <el-main class="content">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  Odometer,
  ChatDotRound,
  Brush,
  Document,
  QuestionFilled,
  House,
  Setting,
  User,
} from '@element-plus/icons-vue';

const route = useRoute();
const router = useRouter();

const pageTitle = computed(() => (route.name === 'dashboard' ? 'Tổng quan' : String(route.name ?? '')));

function logout(): void {
  void router.push('/login');
}
</script>

<style scoped>
.admin-shell {
  height: 100vh;
}
.sidebar {
  background: #ffffff;
  border-right: 1px solid #e6ebf1;
  display: flex;
  flex-direction: column;
}
.brand {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 18px 20px;
  font-weight: 700;
  font-size: 18px;
}
.brand-mark {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 34px;
  height: 34px;
  border-radius: 9px;
  background: linear-gradient(135deg, #0f766e, #0b5850);
  color: #fff;
  font-size: 14px;
}
.side-menu {
  border-right: 0;
  flex: 1;
}
.navbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: #ffffff;
  border-bottom: 1px solid #e6ebf1;
  box-shadow: 0 1px 4px rgba(20, 40, 60, 0.04);
}
.page-title {
  font-size: 18px;
  font-weight: 600;
}
.navbar-right {
  display: flex;
  align-items: center;
  gap: 14px;
}
.user {
  display: flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  outline: none;
}
.content {
  padding: 20px;
}
</style>
