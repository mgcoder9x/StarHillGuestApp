import { createI18n } from 'vue-i18n';

// Guest Web đa ngôn ngữ (Req 2): mặc định en, hỗ trợ vi/ko/zh, auto-detect ngôn ngữ điện thoại.
// Chuỗi UI ở đây (Req 2.4 — i18n JSON FE); nội dung do lễ tân nhập trả từ BE theo ngôn ngữ (isFallback).
export const SUPPORTED_LOCALES = ['en', 'vi', 'ko', 'zh'] as const;
export type Locale = (typeof SUPPORTED_LOCALES)[number];
export const DEFAULT_LOCALE: Locale = 'en';

const messages = {
  en: {
    common: { confirm: 'Confirm', open: 'Open', language: 'Language' },
    home: {
      room: 'Room {room}',
      welcome: 'Welcome to {resort}',
      subtitle: 'Everything you need during your stay, in one place.',
      rules: { title: 'Resort rules', desc: 'Please read the house rules before using other features.' },
      faq: { title: 'FAQ', desc: 'Answers to the most common questions.' },
      chat: { title: 'Message reception', desc: 'Chat directly with our front desk.' },
      housekeeping: { title: 'Housekeeping', desc: 'Request room cleaning in one tap.' },
      footer: 'Connected to the resort Wi-Fi · Room-specific portal',
    },
  },
  vi: {
    common: { confirm: 'Xác nhận', open: 'Mở', language: 'Ngôn ngữ' },
    home: {
      room: 'Phòng {room}',
      welcome: 'Chào mừng đến {resort}',
      subtitle: 'Mọi thứ bạn cần trong kỳ nghỉ, gói gọn một nơi.',
      rules: { title: 'Nội quy', desc: 'Vui lòng đọc nội quy trước khi dùng các tính năng khác.' },
      faq: { title: 'Câu hỏi thường gặp', desc: 'Giải đáp những thắc mắc phổ biến nhất.' },
      chat: { title: 'Nhắn tin lễ tân', desc: 'Trò chuyện trực tiếp với lễ tân.' },
      housekeeping: { title: 'Dọn phòng', desc: 'Yêu cầu dọn phòng chỉ với một chạm.' },
      footer: 'Đã kết nối Wi-Fi resort · Cổng riêng theo phòng',
    },
  },
  ko: {
    common: { confirm: '확인', open: '열기', language: '언어' },
    home: {
      room: '객실 {room}',
      welcome: '{resort}에 오신 것을 환영합니다',
      subtitle: '숙박 중 필요한 모든 것을 한 곳에서.',
      rules: { title: '이용 수칙', desc: '다른 기능을 사용하기 전에 이용 수칙을 읽어 주세요.' },
      faq: { title: '자주 묻는 질문', desc: '가장 흔한 질문에 대한 답변.' },
      chat: { title: '프런트 메시지', desc: '프런트 데스크와 직접 채팅하세요.' },
      housekeeping: { title: '客실 청소', desc: '한 번의 탭으로 청소를 요청하세요.' },
      footer: '리조트 Wi-Fi 연결됨 · 객실 전용 포털',
    },
  },
  zh: {
    common: { confirm: '确认', open: '打开', language: '语言' },
    home: {
      room: '房间 {room}',
      welcome: '欢迎入住 {resort}',
      subtitle: '入住期间所需的一切，尽在此处。',
      rules: { title: '酒店守则', desc: '使用其他功能前请先阅读守则。' },
      faq: { title: '常见问题', desc: '最常见问题的解答。' },
      chat: { title: '前台留言', desc: '与前台直接沟通。' },
      housekeeping: { title: '客房清洁', desc: '一键请求客房清洁。' },
      footer: '已连接酒店 Wi-Fi · 房间专属门户',
    },
  },
};

function detectLocale(): Locale {
  const nav = (navigator.languages?.[0] ?? navigator.language ?? DEFAULT_LOCALE).toLowerCase();
  const base = nav.split('-')[0];
  return (SUPPORTED_LOCALES as readonly string[]).includes(base) ? (base as Locale) : DEFAULT_LOCALE;
}

export const i18n = createI18n({
  legacy: false,
  locale: detectLocale(),
  fallbackLocale: DEFAULT_LOCALE,
  messages,
});
