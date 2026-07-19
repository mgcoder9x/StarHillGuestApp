import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import HomeView from '../views/HomeView.vue';
import GuestResolveView from '../views/GuestResolveView.vue';

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: HomeView },
  { path: '/rules', name: 'rules', component: HomeView },
  { path: '/faq', name: 'faq', component: HomeView },
  { path: '/chat', name: 'chat', component: HomeView },
  { path: '/housekeeping', name: 'housekeeping', component: HomeView },
  { path: '/r/:token', name: 'guest-resolve', component: GuestResolveView },
];

export const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0 }),
});
