import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import { useAuthStore } from '../stores/auth';

const routes: RouteRecordRaw[] = [
  { path: '/login', name: 'login', component: () => import('../views/LoginView.vue'), meta: { public: true } },
  {
    path: '/',
    component: () => import('../layouts/AdminShell.vue'),
    children: [
      { path: '', name: 'dashboard', component: () => import('../views/DashboardView.vue') },
      { path: 'rooms', name: 'rooms', component: () => import('../views/RoomsView.vue') },
      { path: 'rules', name: 'rules', component: () => import('../views/RulesView.vue') },
      { path: 'faq', name: 'faq', component: () => import('../views/FaqView.vue') },
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
