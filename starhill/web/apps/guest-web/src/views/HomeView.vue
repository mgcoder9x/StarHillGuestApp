<script setup lang="ts">
import { computed, onMounted, ref, watch, nextTick } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useGuestSession } from '../stores/guestSession';
import { toastVisible, toastMessage, showToast } from '../stores/toast';

// Demo-only cờ nội quy cục bộ (mockup ở /demo). Real path dùng JourneyCore.ruleAck server-authoritative (QR-AD-057);
// stores/rules.ts (boolean drift) đã bị xoá — QR-DV-008/FE.5a.
const ruleConfirmed = ref(false);
function confirmRules(): void {
  ruleConfirmed.value = true;
}

const route = useRoute();
const router = useRouter();
const { t, locale } = useI18n();
const session = useGuestSession();

const resortName = computed(() => session.context.value?.resort.name ?? 'Star Hill Resort');
const roomNumber = computed(() => session.context.value?.room.number ?? 'A-1203');

// Active Tab computed synched to route path
const activeTab = computed<string>({
  get() {
    if (route.path === '/housekeeping') return 'service';
    if (route.path === '/faq') return 'faq';
    if (route.path === '/chat') return 'chat';
    return 'rules'; // default tab
  },
  set(val) {
    if (val === 'service') router.push('/housekeeping');
    else if (val === 'faq') router.push('/faq');
    else if (val === 'chat') router.push('/chat');
    else router.push('/rules');
  },
});

// Theme Management
const activeTheme = ref(localStorage.getItem('resort-theme') || 'emerald');

function applyTheme(theme: string) {
  activeTheme.value = theme;
  document.body.className = '';
  document.body.classList.add(`theme-${theme}`);
  localStorage.setItem('resort-theme', theme);
}

// Language Drawer States
const drawerOpen = ref(false);
const langSearchQuery = ref('');
const currentLangObj = computed(() => {
  const currentCode = locale.value.toLowerCase();
  return (
    languagesList.find((l) => l.code === currentCode) || {
      code: 'en',
      name: 'English',
      native: 'English',
      flag: '🇬🇧',
    }
  );
});

const languagesList = [
  { code: 'vi', name: 'Tiếng Việt', native: 'Tiếng Việt', flag: '🇻🇳' },
  { code: 'en', name: 'English (UK)', native: 'English', flag: '🇬🇧' },
  { code: 'us', name: 'English (US)', native: 'English', flag: '🇺🇸' },
  { code: 'zh', name: '中文 (简体)', native: '中文', flag: '🇨🇳' },
  { code: 'ja', name: '日本語', native: '日本語', flag: '🇯🇵' },
  { code: 'ko', name: '한국어', native: '한국어', flag: '🇰🇷' },
  { code: 'fr', name: 'Français', native: 'Français', flag: '🇫🇷' },
  { code: 'de', name: 'Deutsch', native: 'Deutsch', flag: '🇩🇪' },
  { code: 'es', name: 'Español', native: 'Español', flag: '🇪🇸' },
  { code: 'it', name: 'Italiano', native: 'Italiano', flag: '🇮🇹' },
  { code: 'ru', name: 'Русский', native: 'Русский', flag: '🇷🇺' },
  { code: 'th', name: 'ไทย', native: 'ไทย', flag: '🇹🇭' },
  { code: 'sg', name: 'English (SG)', native: 'English', flag: '🇸🇬' },
  { code: 'my', name: 'Melayu', native: 'Melayu', flag: '🇲🇾' },
  { code: 'id', name: 'Bahasa Indonesia', native: 'Bahasa Indonesia', flag: '🇮🇩' },
  { code: 'ph', name: 'Tagalog', native: 'Tagalog', flag: '🇵🇭' },
  { code: 'in', name: 'हिन्दी', native: 'हिन्दी', flag: '🇮🇳' },
  { code: 'au', name: 'English (AU)', native: 'English', flag: '🇦🇺' },
  { code: 'ca', name: 'English (CA)', native: 'English', flag: '🇨🇦' },
  { code: 'br', name: 'Português (BR)', native: 'Português', flag: '🇧🇷' },
  { code: 'za', name: 'English (ZA)', native: 'English', flag: '🇿🇦' },
  { code: 'se', name: 'Svenska', native: 'Svenska', flag: '🇸🇪' },
  { code: 'nl', name: 'Nederlands', native: 'Nederlands', flag: '🇳🇱' },
  { code: 'tr', name: 'Türkçe', native: 'Türkçe', flag: '🇹🇷' },
  { code: 'pl', name: 'Polski', native: 'Polski', flag: '🇵🇱' },
  { code: 'sa', name: 'العربية', native: 'العربية', flag: '🇸🇦' },
  { code: 'il', name: 'עברית', native: 'עברית', flag: '🇮🇱' },
  { code: 'gr', name: 'Ελληνικά', native: 'Ελληνικά', flag: '🇬🇷' },
  { code: 'pt', name: 'Português (PT)', native: 'Português', flag: '🇵🇹' },
  { code: 'ch', name: 'Deutsch (CH)', native: 'Deutsch', flag: '🇨🇭' },
  { code: 'at', name: 'Deutsch (AT)', native: 'Deutsch', flag: '🇦🇹' },
  { code: 'be', name: 'Français (BE)', native: 'Français', flag: '🇧🇪' },
  { code: 'no', name: 'Norsk', native: 'Norsk', flag: '🇳🇴' },
  { code: 'dk', name: 'Dansk', native: 'Dansk', flag: '🇩🇰' },
  { code: 'fi', name: 'Suomi', native: 'Suomi', flag: '🇫🇮' },
  { code: 'ie', name: 'Gaeilge', native: 'Gaeilge', flag: '🇮🇪' },
  { code: 'nz', name: 'English (NZ)', native: 'English', flag: '🇳🇿' },
  { code: 'mx', name: 'Español (MX)', native: 'Español', flag: '🇲🇽' },
  { code: 'ar', name: 'Español (AR)', native: 'Español', flag: '🇦🇷' },
  { code: 'cl', name: 'Español (CL)', native: 'Español', flag: '🇨🇱' },
  { code: 'co', name: 'Español (CO)', native: 'Español', flag: '🇨🇴' },
  { code: 'pe', name: 'Español (PE)', native: 'Español', flag: '🇵🇪' },
  { code: 'ua', name: 'Українська', native: 'Українська', flag: '🇺🇦' },
  { code: 'cz', name: 'Čeština', native: 'Čeština', flag: '🇨🇿' },
  { code: 'hu', name: 'Magyar', native: 'Magyar', flag: '🇭🇺' },
  { code: 'ro', name: 'Română', native: 'Română', flag: '🇷🇴' },
  { code: 'kh', name: 'Khmer', native: 'ភាសាខ្មែর', flag: '🇰🇭' },
  { code: 'la', name: 'Lao', native: 'ພາສາລາວ', flag: '🇱🇦' },
];

const filteredLanguages = computed(() => {
  const query = langSearchQuery.value.toLowerCase().trim();
  if (!query) return languagesList;
  return languagesList.filter(
    (l) =>
      l.name.toLowerCase().includes(query) ||
      l.native.toLowerCase().includes(query) ||
      l.code.toLowerCase().includes(query),
  );
});

function openLanguageDrawer() {
  langSearchQuery.value = '';
  drawerOpen.value = true;
}

function selectLanguage(langCode: string) {
  drawerOpen.value = false;
  if (['vi', 'en', 'ko', 'zh'].includes(langCode)) {
    locale.value = langCode;
  } else {
    locale.value = 'en';
  }

  const selected = languagesList.find((l) => l.code === langCode);
  if (selected) {
    const welcomeMsgs: Record<string, string> = {
      vi: 'Chào mừng quý khách!',
      en: 'Welcome guests!',
      zh: '欢迎光临!',
      ja: 'ようこそ!',
      ko: '어서 오십시오!',
      fr: 'Bienvenue!',
      de: 'Willkommen!',
      es: '¡Bienvenido!',
    };
    const msg = welcomeMsgs[langCode] || welcomeMsgs['en'];
    showToast(`${selected.flag} ${selected.name}: ${msg}`);
  }
}

// Navigation Tab Selector with locks
function selectTab(tab: string) {
  if (['service', 'faq', 'chat'].includes(tab) && !ruleConfirmed.value) {
    const toastMsg =
      locale.value === 'vi'
        ? "Vui lòng đọc và nhấn 'Xác nhận & Đồng ý' nội quy resort trước!"
        : 'Please read and click "Confirm & Agree" to the resort rules first!';
    showToast(toastMsg);

    activeTab.value = 'rules';

    // Flash Rules Confirm Button
    setTimeout(() => {
      const confirmBtn = document.getElementById('rules-confirm-btn');
      if (confirmBtn) {
        confirmBtn.classList.add('flash-highlight');
        setTimeout(() => {
          confirmBtn.classList.remove('flash-highlight');
        }, 1000);
      }
    }, 150);
    return;
  }
  activeTab.value = tab;
}

// ================= TAB 1: RULES =================
const btnDisabled = ref(!ruleConfirmed.value);

function handleScroll(e: Event) {
  if (ruleConfirmed.value) return;
  const el = e.target as HTMLElement;
  const threshold = 50;
  const scrolledToBottom = el.scrollHeight - el.scrollTop - el.clientHeight <= threshold;
  if (scrolledToBottom && btnDisabled.value) {
    btnDisabled.value = false;
  }
}

function handleRulesConfirm() {
  confirmRules();
  showToast(t('rules.thanksAgree'));
  setTimeout(() => {
    selectTab('service');
  }, 1200);
}

// ================= TAB 2: HOUSEKEEPING =================
const selectedService = ref('');
const selectedTime = ref('');
const selectedHour = ref('');
const selectedMin = ref('');
const extraNotes = ref('');
const amenities = ref({
  toothbrush: 0,
  towel: 0,
  water: 0,
  soap: 0,
});
const isSubmitted = ref(false);
const ticketId = ref('');
const currentStep = ref(0);
const timelineTimeouts = ref<number[]>([]);

const hoursList = ['08', '09', '10', '11', '12', '13', '14', '15', '16', '17', '18', '19', '20', '21'];
const minutesList = ['00', '15', '30', '45'];

const selectedTimeDisplay = computed(() => {
  if (selectedTime.value === 'timeSpecific') {
    if (selectedHour.value && selectedMin.value) {
      return `${t('housekeeping.timeSpecific')}: ${selectedHour.value}:${selectedMin.value}`;
    }
    return '';
  }
  return selectedTime.value ? t(`housekeeping.${selectedTime.value}`) : '';
});

function changeCounter(key: 'toothbrush' | 'towel' | 'water' | 'soap', delta: number) {
  const newVal = amenities.value[key] + delta;
  if (newVal >= 0 && newVal <= 5) {
    amenities.value[key] = newVal;
  }
}

function clearTimelineTimeouts() {
  timelineTimeouts.value.forEach((tId) => clearTimeout(tId));
  timelineTimeouts.value = [];
}

function startTimelineSimulation() {
  clearTimelineTimeouts();
  currentStep.value = 0;

  const t1 = window.setTimeout(() => {
    currentStep.value = 1;
  }, 5000);

  const t2 = window.setTimeout(() => {
    currentStep.value = 2;
  }, 10000);

  const t3 = window.setTimeout(() => {
    currentStep.value = 3;
    showToast(t('housekeeping.statusCompleted'));
  }, 15000);

  timelineTimeouts.value = [t1, t2, t3];
}

function handleHousekeepingSubmit() {
  if (!selectedService.value) {
    showToast(locale.value === 'vi' ? 'Vui lòng chọn loại dịch vụ!' : 'Please select a service type!');
    return;
  }
  if (!selectedTime.value) {
    showToast(locale.value === 'vi' ? 'Vui lòng chọn thời gian phục vụ!' : 'Please select preferred time!');
    return;
  }
  if (selectedTime.value === 'timeSpecific' && (!selectedHour.value || !selectedMin.value)) {
    showToast(locale.value === 'vi' ? 'Vui lòng chọn cả giờ và phút mong muốn!' : 'Please select both preferred hour and minute!');
    return;
  }

  const randomHex = Math.floor(100 + Math.random() * 900);
  ticketId.value = `HK-${roomNumber.value}-${randomHex}`;
  isSubmitted.value = true;
  showToast(t('housekeeping.toastSubmitSuccess'));
  startTimelineSimulation();
}

function handleCancelTicket() {
  if (confirm(t('housekeeping.cancelConfirm'))) {
    clearTimelineTimeouts();
    isSubmitted.value = false;
    currentStep.value = 0;

    selectedService.value = '';
    selectedTime.value = '';
    selectedHour.value = '';
    selectedMin.value = '';
    extraNotes.value = '';
    amenities.value = {
      toothbrush: 0,
      towel: 0,
      water: 0,
      soap: 0,
    };

    showToast(t('housekeeping.toastTicketCancelled'));
  }
}

// ================= TAB 3: FAQ =================
const faqSearchQuery = ref('');
const faqOpenIndex = ref<number | null>(null);

const faqs = computed(() => [
  { q: t('faq.q1'), a: t('faq.a1') },
  { q: t('faq.q2'), a: t('faq.a2') },
  { q: t('faq.q3'), a: t('faq.a3') },
  { q: t('faq.q4'), a: t('faq.a4') },
  { q: t('faq.q5'), a: t('faq.a5') },
  { q: t('faq.q6'), a: t('faq.a6') },
]);

const filteredFaqs = computed(() => {
  const query = faqSearchQuery.value.toLowerCase().trim();
  if (!query) return faqs.value;
  return faqs.value.filter(
    (item) =>
      item.q.toLowerCase().includes(query) ||
      item.a.toLowerCase().includes(query),
  );
});

// ================= TAB 4: CHAT =================
interface ChatMessage {
  id: number;
  sender: 'receptionist' | 'guest';
  text: string;
  time: string;
}

const chatMessages = ref<ChatMessage[]>([]);
const chatInputText = ref('');
const chatContainerRef = ref<HTMLElement | null>(null);

function formatTime() {
  const now = new Date();
  const hours = String(now.getHours()).padStart(2, '0');
  const minutes = String(now.getMinutes()).padStart(2, '0');
  return `${hours}:${minutes}`;
}

function scrollChatToBottom() {
  nextTick(() => {
    if (chatContainerRef.value) {
      chatContainerRef.value.scrollTop = chatContainerRef.value.scrollHeight;
    }
  });
}

function handleChatSend() {
  const text = chatInputText.value.trim();
  if (!text) return;

  chatMessages.value.push({
    id: Date.now(),
    sender: 'guest',
    text,
    time: formatTime(),
  });
  chatInputText.value = '';
  scrollChatToBottom();

  setTimeout(() => {
    chatMessages.value.push({
      id: Date.now() + 1,
      sender: 'receptionist',
      text: t('chat.autoResponse'),
      time: formatTime(),
    });
    scrollChatToBottom();
  }, 1500);
}

// Watch active tab to scroll things or handle initial loads
watch(activeTab, (newTab) => {
  nextTick(() => {
    const contentArea = document.getElementById('app-content-area');
    if (contentArea) {
      contentArea.scrollTop = 0;
    }
  });

  if (newTab === 'chat') {
    if (chatMessages.value.length === 0) {
      chatMessages.value.push({
        id: 1,
        sender: 'receptionist',
        text: t('chat.welcomeMsg'),
        time: formatTime(),
      });
    }
    scrollChatToBottom();
  }
});

onMounted(() => {
  applyTheme(activeTheme.value);

  // Check if scrollable
  const contentArea = document.getElementById('app-content-area');
  if (contentArea) {
    setTimeout(() => {
      if (contentArea.scrollHeight <= contentArea.clientHeight + 10) {
        btnDisabled.value = false;
      }
    }, 100);
  }
});
</script>

<template>
  <div class="phone-container">
    <!-- App Header -->
    <header class="app-header">
      <div class="header-top">
        <div class="brand-section">
          <div class="brand-logo">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" style="width: 20px; height: 20px;">
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 3v18m0-18a9 9 0 019 9m-9-9a9 9 0 00-9 9m9-9c1.657 0 3 3.582 3 8s-1.343 8-3 8m0-16c-1.657 0-3 3.582-3 8s1.343 8 3 8" />
            </svg>
          </div>
          <div>
            <span class="brand-name">{{ resortName }}</span>
            <span class="brand-sub">{{ t('common.brandSub', 'LUXURY & RETREAT') }}</span>
          </div>
        </div>

        <div class="header-actions">
          <!-- Language selector trigger pill -->
          <div class="lang-selector-trigger" id="lang-selector-trigger" @click="openLanguageDrawer" style="cursor: pointer; background: var(--accent-bg); border: 1px solid rgba(197, 168, 128, 0.25); border-radius: 20px; padding: 4px 10px; display: flex; align-items: center; gap: 5px; font-size: 11px; font-weight: 600; color: var(--text-muted); user-select: none;">
            <span>{{ currentLangObj.flag }}</span>
            <span>{{ currentLangObj.code.toUpperCase() }}</span>
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2.5" stroke="currentColor" style="width: 10px; height: 10px; color: var(--text-light);">
              <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
            </svg>
          </div>

          <!-- Room Info Badge -->
          <div class="room-badge">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" style="width: 14px; height: 14px;">
              <path stroke-linecap="round" stroke-linejoin="round" d="M2.25 12l8.954-8.955c.44-.439 1.152-.439 1.591 0L21.75 12M4.5 9.75v10.125c0 .621.504 1.125 1.125 1.125H9.75v-4.875c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21h4.125c.621 0 1.125-.504 1.125-1.125V9.75M8.25 21h8.25" />
            </svg>
            <span>{{ roomNumber }}</span>
          </div>
        </div>
      </div>

      <!-- Theme Switcher Row -->
      <div class="header-sub-row" style="display: flex; justify-content: space-between; align-items: center; border-top: 1px dashed var(--border-color); padding-top: 8px; margin-top: 2px;">
        <span style="font-size: 11px; font-weight: 600; color: var(--text-light); text-transform: uppercase; letter-spacing: 0.5px;">{{ t('common.themeLabel', 'Giao diện') }}</span>
        <div class="theme-dots-container">
          <div class="theme-dot emerald" :class="{ active: activeTheme === 'emerald' }" @click="applyTheme('emerald')" title="Eco Emerald"></div>
          <div class="theme-dot ocean" :class="{ active: activeTheme === 'ocean' }" @click="applyTheme('ocean')" title="Ocean Breeze"></div>
          <div class="theme-dot sunset" :class="{ active: activeTheme === 'sunset' }" @click="applyTheme('sunset')" title="Sunset Gold"></div>
          <div class="theme-dot white" :class="{ active: activeTheme === 'white' }" @click="applyTheme('white')" title="Minimalist White"></div>
          <div class="theme-dot royal" :class="{ active: activeTheme === 'royal' }" @click="applyTheme('royal')" title="Royal Purple"></div>
        </div>
      </div>
    </header>

    <!-- Main Content Area -->
    <main class="app-content" id="app-content-area" @scroll="handleScroll">

      <!-- ================= TAB 1: RULES & REGULATIONS ================= -->
      <section id="rules-tab" class="tab-content" :class="{ active: activeTab === 'rules' }">
        <!-- Welcome Banner -->
        <div class="welcome-card">
          <h2 class="welcome-title">{{ t('home.welcome', { resort: resortName }) }}</h2>
          <p class="welcome-desc">{{ t('home.subtitle') }} ({{ t('home.room', { room: roomNumber }) }})</p>
        </div>

        <div>
          <h3 class="section-title">{{ t('rules.title') }}</h3>
          <p class="section-desc">{{ t('rules.desc') }}</p>
        </div>

        <div class="rules-container">
          <!-- Rule Card 1 -->
          <div class="rule-card">
            <div class="rule-header">
              <div class="rule-icon">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" style="width: 18px; height: 18px;">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
              </div>
              <span class="rule-title">{{ t('rules.rule1Title') }}</span>
            </div>
            <ul class="rule-list">
              <li>{{ t('rules.rule1_1') }}</li>
              <li>{{ t('rules.rule1_2') }}</li>
              <li>{{ t('rules.rule1_3') }}</li>
            </ul>
          </div>

          <!-- Rule Card 2 -->
          <div class="rule-card">
            <div class="rule-header">
              <div class="rule-icon">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" style="width: 18px; height: 18px;">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01" />
                </svg>
              </div>
              <span class="rule-title">{{ t('rules.rule2Title') }}</span>
            </div>
            <ul class="rule-list">
              <li>{{ t('rules.rule2_1') }}</li>
              <li>{{ t('rules.rule2_2') }}</li>
              <li>{{ t('rules.rule2_3') }}</li>
            </ul>
          </div>

          <!-- Rule Card 3 -->
          <div class="rule-card">
            <div class="rule-header">
              <div class="rule-icon">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" style="width: 18px; height: 18px;">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.75c0 5.592 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.57-.598-3.75h-.152c-3.196 0-6.1-1.249-8.25-3.286zm0 13.036h.008v.008H12v-.008z" />
                </svg>
              </div>
              <span class="rule-title">{{ t('rules.rule3Title') }}</span>
            </div>
            <ul class="rule-list">
              <li>{{ t('rules.rule3_1') }}</li>
              <li>{{ t('rules.rule3_2') }}</li>
              <li>{{ t('rules.rule3_3') }}</li>
            </ul>
          </div>
        </div>

        <!-- Agreement Box -->
        <div class="agree-box" id="agree-box-wrapper">
          <div v-if="ruleConfirmed" style="color: var(--success); font-weight: 600; display: flex; align-items: center; gap: 8px; justify-content: center; padding: 6px 0;">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2.5" stroke="currentColor" style="width: 20px; height: 20px;">
              <path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span>{{ t('rules.thanksAgree') }}</span>
          </div>

          <div v-else style="display: flex; flex-direction: column; gap: 12px; align-items: center;">
            <label class="agree-label" style="text-align: center;">
              <span>{{ t('rules.agreeText') }}</span>
            </label>
            <button class="btn-confirm" id="rules-confirm-btn" :disabled="btnDisabled" :class="{ 'pulse-glow': !btnDisabled }" @click="handleRulesConfirm">
              {{ t('rules.btnConfirm') }}
            </button>
          </div>
        </div>
      </section>

      <!-- ================= TAB 2: HOUSEKEEPING TICKET ================= -->
      <section id="service-tab" class="tab-content" :class="{ active: activeTab === 'service' }">
        <div>
          <h3 class="section-title">{{ t('housekeeping.title') }}</h3>
          <p class="section-desc">{{ t('housekeeping.desc') }}</p>
        </div>

        <!-- Request Form Card -->
        <div v-if="!isSubmitted" class="ticket-card" id="housekeeping-form-card">
          <form @submit.prevent="handleHousekeepingSubmit">
            <!-- Room Number -->
            <div class="form-group">
              <label class="form-label">{{ t('housekeeping.roomLabel') }}</label>
              <input type="text" class="text-input" :value="roomNumber" readonly style="font-weight: 600; color: var(--primary);">
            </div>

            <!-- Service Grid Selection -->
            <div class="form-group">
              <label class="form-label">{{ t('housekeeping.serviceLabel') }}</label>
              <div class="service-grid">
                <div class="service-option" :class="{ selected: selectedService === 'serviceFull' }" @click="selectedService = 'serviceFull'">
                  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M9.813 15.904L9 21m0 0l-.813-5.096m.813 5.096a17.25 17.25 0 010-10.874m0 10.874a17.25 17.25 0 000-10.874m0 0L9 3m0 0l-.813 5.096m.813-5.096a17.25 17.25 0 010 10.874m0-10.874a17.25 17.25 0 000 10.874M9 21h3m-3 0H6" />
                  </svg>
                  <span>{{ t('housekeeping.serviceFull') }}</span>
                </div>

                <div class="service-option" :class="{ selected: selectedService === 'serviceTowel' }" @click="selectedService = 'serviceTowel'">
                  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M8.25 21v-4.875c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21m0 0h4.5V3.545M12.75 7.5h1.5m-1.5 3h1.5m-1.5 3h1.5m-7.5-3h7.5" />
                  </svg>
                  <span>{{ t('housekeeping.serviceTowel') }}</span>
                </div>

                <div class="service-option" :class="{ selected: selectedService === 'serviceTrash' }" @click="selectedService = 'serviceTrash'">
                  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                  </svg>
                  <span>{{ t('housekeeping.serviceTrash') }}</span>
                </div>

                <div class="service-option" :class="{ selected: selectedService === 'serviceRefill' }" @click="selectedService = 'serviceRefill'">
                  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z" />
                  </svg>
                  <span>{{ t('housekeeping.serviceRefill') }}</span>
                </div>
              </div>
            </div>

            <!-- Time Slots -->
            <div class="form-group">
              <label class="form-label">{{ t('housekeeping.timeLabel') }}</label>
              <div class="time-grid">
                <div class="time-option" :class="{ selected: selectedTime === 'timeNow' }" @click="selectedTime = 'timeNow'; selectedHour = ''; selectedMin = ''">
                  {{ t('housekeeping.timeNow') }}
                </div>
                <div class="time-option" :class="{ selected: selectedTime === 'time1h' }" @click="selectedTime = 'time1h'; selectedHour = ''; selectedMin = ''">
                  {{ t('housekeeping.time1h') }}
                </div>
                <div class="time-option full-width" :class="{ selected: selectedTime === 'timeSpecific' }" @click="selectedTime = 'timeSpecific'">
                  {{ t('housekeeping.timeSpecific') }}
                </div>
              </div>

              <!-- Specific Time Picker -->
              <div v-if="selectedTime === 'timeSpecific'" class="specific-time-wrapper">
                <div class="custom-time-picker">
                  <div>
                    <div class="picker-section-title">{{ t('housekeeping.hourLabel') }}</div>
                    <div class="picker-grid">
                      <div v-for="h in hoursList" :key="h" class="picker-pill" :class="{ selected: selectedHour === h }" @click="selectedHour = h">
                        {{ h }}
                      </div>
                    </div>
                  </div>
                  <div style="margin-top: 8px;">
                    <div class="picker-section-title">{{ t('housekeeping.minuteLabel') }}</div>
                    <div class="picker-grid" style="grid-template-columns: repeat(4, 1fr);">
                      <div v-for="m in minutesList" :key="m" class="picker-pill" :class="{ selected: selectedMin === m }" @click="selectedMin = m">
                        {{ m }}
                      </div>
                    </div>
                  </div>

                  <div v-if="selectedHour && selectedMin" class="time-preview-badge">
                    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2.5" stroke="currentColor" style="width: 14px; height: 14px;">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    <span>{{ t('common.confirm') }}: {{ selectedHour }}:{{ selectedMin }}</span>
                  </div>
                </div>
              </div>
            </div>

            <!-- Amenities Counters -->
            <div class="form-group">
              <label class="form-label">{{ t('housekeeping.amenitiesLabel') }}</label>
              <div class="amenities-list">
                <!-- Toothbrush -->
                <div class="amenity-item">
                  <div class="amenity-info">
                    <div class="amenity-icon">
                      <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M19 5L5 19M16.5 2.5l5 5M18 4l-2.5 2.5m3.5.5L16.5 4.5M6 18l-2 2" />
                      </svg>
                    </div>
                    <span class="amenity-name">{{ t('housekeeping.amenityToothbrush') }}</span>
                  </div>
                  <div class="counter-control">
                    <button type="button" class="counter-btn" :disabled="amenities.toothbrush === 0" @click="changeCounter('toothbrush', -1)">-</button>
                    <span class="counter-val">{{ amenities.toothbrush }}</span>
                    <button type="button" class="counter-btn" :disabled="amenities.toothbrush === 5" @click="changeCounter('toothbrush', 1)">+</button>
                  </div>
                </div>

                <!-- Towel -->
                <div class="amenity-item">
                  <div class="amenity-info">
                    <div class="amenity-icon">
                      <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 10h16M4 14h16M4 18h16" />
                      </svg>
                    </div>
                    <span class="amenity-name">{{ t('housekeeping.amenityTowel') }}</span>
                  </div>
                  <div class="counter-control">
                    <button type="button" class="counter-btn" :disabled="amenities.towel === 0" @click="changeCounter('towel', -1)">-</button>
                    <span class="counter-val">{{ amenities.towel }}</span>
                    <button type="button" class="counter-btn" :disabled="amenities.towel === 5" @click="changeCounter('towel', 1)">+</button>
                  </div>
                </div>

                <!-- Water -->
                <div class="amenity-item">
                  <div class="amenity-info">
                    <div class="amenity-icon">
                      <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M12 21.5c-4.14 0-7.5-3.36-7.5-7.5 0-5.25 7.5-12 7.5-12s7.5 6.75 7.5 12c0 4.14-3.36 7.5-7.5 7.5z" />
                      </svg>
                    </div>
                    <span class="amenity-name">{{ t('housekeeping.amenityWater') }}</span>
                  </div>
                  <div class="counter-control">
                    <button type="button" class="counter-btn" :disabled="amenities.water === 0" @click="changeCounter('water', -1)">-</button>
                    <span class="counter-val">{{ amenities.water }}</span>
                    <button type="button" class="counter-btn" :disabled="amenities.water === 5" @click="changeCounter('water', 1)">+</button>
                  </div>
                </div>

                <!-- Soap -->
                <div class="amenity-item">
                  <div class="amenity-info">
                    <div class="amenity-icon">
                      <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M9 5h6M9 8h6m-7 3h8v9a2 2 0 01-2 2H10a2 2 0 01-2-2v-9zM12 5V2m0 0l-2 1m2-1l2 1" />
                      </svg>
                    </div>
                    <span class="amenity-name">{{ t('housekeeping.amenitySoap') }}</span>
                  </div>
                  <div class="counter-control">
                    <button type="button" class="counter-btn" :disabled="amenities.soap === 0" @click="changeCounter('soap', -1)">-</button>
                    <span class="counter-val">{{ amenities.soap }}</span>
                    <button type="button" class="counter-btn" :disabled="amenities.soap === 5" @click="changeCounter('soap', 1)">+</button>
                  </div>
                </div>
              </div>
            </div>

            <!-- Notes -->
            <div class="form-group">
              <label class="form-label">{{ t('housekeeping.noteLabel') }}</label>
              <textarea class="textarea-input" v-model="extraNotes" :placeholder="t('housekeeping.notePlaceholder')"></textarea>
            </div>

            <button type="submit" class="btn-submit">
              <span>{{ t('housekeeping.btnSubmitTicket') }}</span>
            </button>
          </form>
        </div>

        <!-- Simulation Tracker Timeline -->
        <div v-else class="ticket-status-container" id="housekeeping-status-card">
          <div class="ticket-status-header">
            <div>
              <span class="status-ref">{{ t('housekeeping.ticketRef') }}</span>
              <div style="font-weight: 700; color: var(--primary); font-size: 15px; margin-top: 2px;">{{ ticketId }}</div>
            </div>
            <div class="status-badge" :class="currentStep === 3 ? 'completed' : 'in-progress'">
              {{ currentStep === 3 ? t('housekeeping.statusCompleted') : t('housekeeping.statusSubmitted') }}
            </div>
          </div>

          <!-- Summary Box -->
          <div style="background-color: var(--bg-input); padding: 12px 16px; border-radius: var(--radius-md); font-size: 13px;">
            <div style="font-weight: 600; margin-bottom: 4px; color: var(--primary);">{{ t(`housekeeping.${selectedService}`) }}</div>
            <div style="color: var(--text-muted); margin-bottom: 4px;">⏱ {{ selectedTimeDisplay }}</div>
            <div v-if="amenities.toothbrush > 0 || amenities.towel > 0 || amenities.water > 0 || amenities.soap > 0" style="color: var(--text-muted); margin-bottom: 4px;">
              📦
              <span v-if="amenities.toothbrush > 0">{{ t('housekeeping.amenityToothbrush') }} (x{{ amenities.toothbrush }}), </span>
              <span v-if="amenities.towel > 0">{{ t('housekeeping.amenityTowel') }} (x{{ amenities.towel }}), </span>
              <span v-if="amenities.water > 0">{{ t('housekeeping.amenityWater') }} (x{{ amenities.water }}), </span>
              <span v-if="amenities.soap > 0">{{ t('housekeeping.amenitySoap') }} (x{{ amenities.soap }})</span>
            </div>
            <div v-if="extraNotes.trim()" style="font-style: italic; color: var(--text-muted); border-top: 1px dashed rgba(31, 78, 61, 0.1); margin-top: 6px; padding-top: 4px;">
              ✍️ "{{ extraNotes }}"
            </div>
          </div>

          <!-- Timeline -->
          <div class="timeline">
            <div class="timeline-step" :class="{ completed: currentStep > 0, active: currentStep === 0 }">
              <div class="timeline-dot"></div>
              <div class="timeline-label">{{ t('housekeeping.statusSubmitted') }}</div>
              <div class="timeline-desc">{{ t('housekeeping.statusSubmittedDesc') }}</div>
            </div>
            <div class="timeline-step" :class="{ completed: currentStep > 1, active: currentStep === 1 }">
              <div class="timeline-dot"></div>
              <div class="timeline-label">{{ t('housekeeping.statusAssigned') }}</div>
              <div class="timeline-desc">{{ t('housekeeping.statusAssignedDesc') }}</div>
            </div>
            <div class="timeline-step" :class="{ completed: currentStep > 2, active: currentStep === 2 }">
              <div class="timeline-dot"></div>
              <div class="timeline-label">{{ t('housekeeping.statusCleaning') }}</div>
              <div class="timeline-desc">{{ t('housekeeping.statusCleaningDesc') }}</div>
            </div>
            <div class="timeline-step" :class="{ completed: currentStep === 3, active: currentStep === 3 }">
              <div class="timeline-dot"></div>
              <div class="timeline-label">{{ t('housekeeping.statusCompleted') }}</div>
              <div class="timeline-desc">{{ t('housekeeping.statusCompletedDesc') }}</div>
            </div>
          </div>

          <button class="btn-cancel" @click="handleCancelTicket">
            {{ t('housekeeping.btnCancelTicket') }}
          </button>
        </div>
      </section>

      <!-- ================= TAB 3: FAQ / SUPPORT ================= -->
      <section id="faq-tab" class="tab-content" :class="{ active: activeTab === 'faq' }">
        <div>
          <h3 class="section-title">{{ t('faq.title') }}</h3>
          <p class="section-desc">{{ t('faq.desc') }}</p>
        </div>

        <!-- Search box -->
        <div class="faq-search-wrapper" style="margin-bottom: 16px;">
          <div class="faq-search-icon">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" style="width: 18px; height: 18px;">
              <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z" />
            </svg>
          </div>
          <input type="text" class="text-input faq-search" v-model="faqSearchQuery" :placeholder="t('faq.searchPlaceholder')">
        </div>

        <!-- Accordion FAQ list -->
        <div class="faq-list">
          <div v-for="(item, idx) in filteredFaqs" :key="idx" class="faq-item" :class="{ open: faqOpenIndex === idx }">
            <div class="faq-question" @click="faqOpenIndex = faqOpenIndex === idx ? null : idx">
              <span>{{ item.q }}</span>
              <svg class="faq-arrow" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2.5" stroke="currentColor" style="width: 14px; height: 14px;">
                <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
              </svg>
            </div>
            <div class="faq-answer" :style="{ maxHeight: faqOpenIndex === idx ? '200px' : '0', paddingBottom: faqOpenIndex === idx ? '16px' : '0', paddingTop: faqOpenIndex === idx ? '0' : '0' }">
              {{ item.a }}
            </div>
          </div>
        </div>
      </section>

      <!-- ================= TAB 4: CHAT / MESSAGE RECEPTION ================= -->
      <section id="chat-tab" class="tab-content" :class="{ active: activeTab === 'chat' }">
        <div class="chat-container">
          <!-- Chat Header -->
          <div style="background-color: var(--bg-card); border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 12px 16px; display: flex; align-items: center; gap: 12px; margin-bottom: 12px; box-shadow: var(--shadow-soft);">
            <div style="width: 38px; height: 38px; background-color: var(--primary-bg); border-radius: 50%; display: flex; align-items: center; justify-content: center; color: var(--primary); font-weight: 700; font-size: 14px;">
              🛎️
            </div>
            <div>
              <div style="font-weight: 700; font-size: 14px; color: var(--text-main);">{{ t('chat.receptionist') }}</div>
              <div style="font-size: 11px; color: var(--success); font-weight: 600; display: flex; align-items: center; gap: 4px;">
                <span style="display: inline-block; width: 6px; height: 6px; background-color: var(--success); border-radius: 50%;"></span>
                {{ t('chat.online') }}
              </div>
            </div>
          </div>

          <!-- Message log -->
          <div class="chat-messages" ref="chatContainerRef">
            <div v-for="msg in chatMessages" :key="msg.id" class="chat-bubble" :class="msg.sender">
              <div>{{ msg.text }}</div>
              <span class="chat-time">{{ msg.time }}</span>
            </div>
          </div>

          <!-- Message input -->
          <div class="chat-input-area">
            <input type="text" class="text-input" v-model="chatInputText" :placeholder="t('chat.inputPlaceholder')" @keydown.enter="handleChatSend" style="flex: 1;">
            <button class="btn-send" @click="handleChatSend">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2.5" stroke="currentColor" style="width: 18px; height: 18px;">
                <path stroke-linecap="round" stroke-linejoin="round" d="M6 12L3.269 3.126A59.768 59.768 0 0121.485 12 59.77 59.77 0 013.27 20.876L5.999 12zm0 0h7.5" />
              </svg>
            </button>
          </div>
        </div>
      </section>
    </main>

    <!-- Bottom Navigation Bar -->
    <nav class="bottom-nav">
      <!-- Nội quy -->
      <button class="nav-item" :class="{ active: activeTab === 'rules' }" @click="selectTab('rules')">
        <svg class="nav-icon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 6.042A8.967 8.967 0 006 3.75c-1.052 0-2.062.18-3 .512v14.25A8.987 8.987 0 016 18c2.305 0 4.408.867 6 2.292m0-14.25a8.966 8.966 0 016-2.292c1.052 0 2.062.18 3 .512v14.25A8.987 8.987 0 0018 18a8.967 8.967 0 00-6 2.292m0-14.25v14.25" />
        </svg>
        <span class="nav-label">{{ t('home.rules.title') }}</span>
      </button>

      <!-- Dọn phòng -->
      <button class="nav-item" :class="{ active: activeTab === 'service', locked: !ruleConfirmed }" @click="selectTab('service')">
        <svg class="nav-icon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M9.813 15.904L9 21m0 0l-.813-5.096m.813 5.096a17.25 17.25 0 010-10.874m0 10.874a17.25 17.25 0 000-10.874m0 0L9 3m0 0l-.813 5.096m.813-5.096a17.25 17.25 0 010 10.874m0-10.874a17.25 17.25 0 000 10.874M9 21h3m-3 0H6" />
        </svg>
        <span class="nav-label">{{ t('home.housekeeping.title') }}</span>
      </button>

      <!-- Hỏi đáp -->
      <button class="nav-item" :class="{ active: activeTab === 'faq', locked: !ruleConfirmed }" @click="selectTab('faq')">
        <svg class="nav-icon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M9.879 7.519c1.171-1.025 3.071-1.025 4.242 0 1.172 1.025 1.172 2.687 0 3.712-.203.179-.43.326-.67.442-.745.361-1.45.999-1.45 1.827v.75M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9 5.25h.008v.008H12v-.008z" />
        </svg>
        <span class="nav-label">{{ t('home.faq.title') }}</span>
      </button>

      <!-- Nhắn tin -->
      <button class="nav-item" :class="{ active: activeTab === 'chat', locked: !ruleConfirmed }" @click="selectTab('chat')">
        <svg class="nav-icon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M8.625 9.75a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0H8.25m4.125 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0H12m4.125 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0h-.375M21 12c0 4.556-4.03 8.25-9 8.25a9.764 9.764 0 01-2.555-.337A5.972 5.972 0 015.41 18.09a5.967 5.967 0 01-.707-1.754 5.969 5.969 0 013.65-4.58 8.09 8.09 0 01-.06-.827c0-4.556 4.03-8.25 9-8.25s9 3.694 9 8.25z" />
        </svg>
        <span class="nav-label">{{ t('home.chat.title') }}</span>
      </button>
    </nav>

    <!-- Toast Messages -->
    <div class="toast-container">
      <div class="toast" :class="{ show: toastVisible }">
        {{ toastMessage }}
      </div>
    </div>

    <!-- Language Selection Drawer Sheet -->
    <div class="drawer-overlay" :class="{ open: drawerOpen }" @click="drawerOpen = false"></div>
    <div class="drawer-sheet" :class="{ open: drawerOpen }">
      <div class="drawer-header">
        <h4>{{ t('common.selectLanguage', 'Chọn Ngôn Ngữ / Select Language') }}</h4>
        <button class="drawer-close-btn" @click="drawerOpen = false">&times;</button>
      </div>
      <div class="drawer-search-wrapper">
        <input type="text" class="text-input drawer-search" v-model="langSearchQuery" :placeholder="t('common.searchLanguage', 'Tìm kiếm ngôn ngữ...')">
      </div>
      <div class="drawer-list">
        <div v-for="lang in filteredLanguages" :key="lang.code" class="drawer-item" :class="{ selected: locale === lang.code }" @click="selectLanguage(lang.code)">
          <span class="drawer-flag">{{ lang.flag }}</span>
          <span class="drawer-lang-name">{{ lang.name }} ({{ lang.native }})</span>
        </div>
      </div>
    </div>
  </div>
</template>
