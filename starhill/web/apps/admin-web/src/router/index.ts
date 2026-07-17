import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import AdminShell from '../layouts/AdminShell.vue';
import LoginView from '../views/LoginView.vue';
import DashboardView from '../views/DashboardView.vue';
import RoomsView from '../views/RoomsView.vue';

const routes: RouteRecordRaw[] = [
  { path: '/login', name: 'login', component: LoginView, meta: { public: true } },
  {
    path: '/',
    component: AdminShell,
    children: [
      { path: '', name: 'dashboard', component: DashboardView },
      { path: 'rooms', name: 'rooms', component: RoomsView },
    ],
  },
];

export const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0 }),
});

// Route guard theo trạng thái đăng nhập (QR-AD-005: trang quản trị cần auth). Token in-memory → reload mất → về login.
router.beforeEach((to) => {
  const auth = useAuthStore();
  if (to.meta.public !== true && !auth.isAuthenticated) {
    return { name: 'login' };
  }
  if (to.name === 'login' && auth.isAuthenticated) {
    return { name: 'dashboard' };
  }
  return true;
});
