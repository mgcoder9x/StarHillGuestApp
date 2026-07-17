import { createI18n } from 'vue-i18n';

// Admin Dashboard cố định tiếng Việt (Req 2.6). Chuỗi UI ở JSON FE; nội dung DB trả từ BE.
const messages = {
  vi: {
    app: {
      title: 'Star Hill — Quản trị',
      logout: 'Đăng xuất',
      menu: 'Mở menu',
      theme: 'Đổi giao diện sáng/tối',
      env: 'Nội bộ',
    },
    nav: {
      section: {
        overview: 'Tổng quan',
        operations: 'Vận hành',
        content: 'Nội dung',
      },
      soon: 'Sắp có',
      dashboard: 'Tổng quan',
      rooms: 'Phòng & QR',
      rules: 'Nội quy',
      faq: 'FAQ',
      inbox: 'Tin nhắn',
      housekeeping: 'Dọn phòng',
      settings: 'Cài đặt',
    },
    login: {
      title: 'Đăng nhập quản trị',
      subtitle: 'Đăng nhập để quản lý phòng, nội quy và tin nhắn khách.',
      username: 'Tên đăng nhập',
      password: 'Mật khẩu',
      submit: 'Đăng nhập',
      error: 'Tên đăng nhập hoặc mật khẩu không đúng.',
      generic: 'Không đăng nhập được. Vui lòng thử lại.',
    },
    dashboard: {
      title: 'Tổng quan vận hành',
      welcome: 'Xin chào, {name}',
      unread: 'Hội thoại chưa đọc',
      openConversations: 'Hội thoại đang mở',
      openTickets: 'Ticket dọn phòng mở',
      activeRooms: 'Phòng đang hoạt động',
      acksToday: 'Xác nhận nội quy hôm nay',
      loadError: 'Không tải được số liệu.',
      refresh: 'Làm mới',
    },
  },
};

export const i18n = createI18n({
  legacy: false,
  locale: 'vi',
  fallbackLocale: 'vi',
  messages,
});
