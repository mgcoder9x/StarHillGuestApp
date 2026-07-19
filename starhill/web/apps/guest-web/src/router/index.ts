import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import GuestResolveView from '../views/GuestResolveView.vue';
import HomeShell from '../views/HomeShell.vue';
import RulesView from '../views/RulesView.vue';
import RescanView from '../views/RescanView.vue';
import FaqView from '../views/FaqView.vue';
import ChatView from '../views/ChatView.vue';
import HousekeepingView from '../views/HousekeepingView.vue';
import HomeView from '../views/HomeView.vue'; // MOCKUP tĩnh — chỉ dùng ở /demo (tham chiếu thị giác, QR-DV-008).
import { useJourneyCore } from '../core/journeyCore';

const routes: RouteRecordRaw[] = [
  { path: '/r/:token', name: 'guest-resolve', component: GuestResolveView, meta: { entry: true } },
  { path: '/rescan', name: 'rescan', component: RescanView, meta: { entry: true } },
  { path: '/', name: 'home', component: HomeShell },
  { path: '/rules', name: 'rules', component: RulesView },
  { path: '/faq', name: 'faq', component: FaqView, meta: { capability: 'faq' } },
  { path: '/chat', name: 'chat', component: ChatView, meta: { capability: 'chat' } },
  { path: '/housekeeping', name: 'housekeeping', component: HousekeepingView, meta: { capability: 'housekeeping' } },
  { path: '/demo', name: 'demo', component: HomeView, meta: { entry: true } },
];

export const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0 }),
});

// Guard đọc JourneyCore (design-module 10 §2). entry routes (resolve/rescan/demo) luôn cho qua.
// mustRescan (chưa scan / cửa sổ hết hạn) → /rescan. Capability chưa mở khoá (chưa ack) → /rules.
router.beforeEach((to) => {
  if (to.meta.entry === true) {
    return true;
  }
  const core = useJourneyCore();
  if (core.mustRescan.value) {
    return { name: 'rescan' };
  }
  const capability = to.meta.capability as 'faq' | 'chat' | 'housekeeping' | undefined;
  if (capability) {
    const allowed =
      capability === 'faq' ? core.canFaq.value : capability === 'chat' ? core.canChat.value : core.canHousekeeping.value;
    if (!allowed) {
      return { name: 'rules' };
    }
  }
  return true;
});
