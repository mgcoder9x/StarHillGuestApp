import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import HomeView from '../views/HomeView.vue';
import SectionPlaceholderView from '../views/SectionPlaceholderView.vue';

// Guest Web routes (Req 3-6). FE.1a: home showcase + placeholder các mục (nối API thật ở slice sau).
const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: HomeView },
  { path: '/rules', name: 'rules', component: SectionPlaceholderView, props: { section: 'rules', icon: 'pi-book' } },
  { path: '/faq', name: 'faq', component: SectionPlaceholderView, props: { section: 'faq', icon: 'pi-question-circle' } },
  { path: '/chat', name: 'chat', component: SectionPlaceholderView, props: { section: 'chat', icon: 'pi-comments' } },
  {
    path: '/housekeeping',
    name: 'housekeeping',
    component: SectionPlaceholderView,
    props: { section: 'housekeeping', icon: 'pi-sparkles' },
  },
];

export const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0 }),
});
