// ApiGateway — ĐIỂM FETCH DUY NHẤT của guest-web (DI-1, design-module 10 §5). Mọi call guest đi qua đây:
// - credentials:'include' (cookie thiết bị __Host-), Accept json, tiêm roomId/lang.
// - map ProblemDetails → GuestApiError(code) (ProblemDetailsBuilder emit code nguyên văn — QR-N-076).
// - INTERCEPT tập trung (INV2/INV3): session_expired/guest_context_missing → onRescanNeeded();
//   403 rule_ack_required → onAckRequired(currentVersion?). View KHÔNG tự xử lý các cross-cutting này.
// Hợp đồng đọc TỪ CODE backend thật (RulesGuestEndpointModule/GuestAccessEndpointModule) — không bịa.

export interface GuestResolveResponse {
  room: { id: string; number: string; building: string | null; floor: number | null };
  resort: { id: string; name: string; logoUrl: string | null };
  languages: string[];
  defaultLanguage: string;
  visit: { id: string; portalWindowExpiresAt: string };
  features: {
    faqEnabled: boolean;
    chatEnabled: boolean;
    housekeepingEnabled: boolean;
    ruleAckRequiredForFaq: boolean;
    ruleAckRequiredForChat: boolean;
    ruleAckRequiredForHousekeeping: boolean;
  };
}

/** Section nội quy — khớp GuestRuleSectionResponse (BodyHtmlSanitized đã sanitize server — INV8). */
export interface GuestRuleSection {
  key: string;
  sortOrder: number;
  isRequired: boolean;
  requireScrollEnd: boolean;
  minReadSeconds: number;
  title: string | null;
  bodyHtmlSanitized: string | null;
  resolvedLanguage: string;
  isFallback: boolean;
  isMissing: boolean;
}

/** Khớp GuestRulesResponse. */
export interface GuestRulesResponse {
  publicationId: string;
  version: number;
  language: string;
  sections: GuestRuleSection[];
}

/** Khớp AcknowledgeRulesResponse. */
export interface AcknowledgeRulesResponse {
  rulePublicationId: string;
  version: number;
  alreadyAcknowledged: boolean;
}

/** Item FAQ đã render — khớp RenderedFaqItem (đệ quy children). answerHtmlSanitized sanitize server (INV8). */
export interface GuestFaqItem {
  id: string;
  sortOrder: number;
  question: string | null;
  answerHtmlSanitized: string | null;
  resolvedLanguage: string;
  isFallback: boolean;
  isMissing: boolean;
  children: GuestFaqItem[];
}

/** Category FAQ đã render — khớp RenderedFaqCategory. */
export interface GuestFaqCategory {
  id: string;
  key: string;
  sortOrder: number;
  name: string | null;
  resolvedLanguage: string;
  isFallback: boolean;
  isMissing: boolean;
  items: GuestFaqItem[];
}

/** Khớp GetGuestFaqTreeResult. */
export interface GuestFaqTree {
  language: string;
  categories: GuestFaqCategory[];
}

/** Tin nhắn guest-facing — khớp GuestMessageView. senderType string enum ('Guest'|'Staff'|'System'). body PLAIN TEXT (INV8: render textContent). */
export interface GuestMessage {
  messageId: string;
  senderType: 'Guest' | 'Staff' | 'System';
  body: string;
  createdAt: string;
  readByStaffAt: string | null;
}

/** Hội thoại guest-facing — khớp GuestConversationView. */
export interface GuestConversation {
  conversationId: string;
  status: 'Open' | 'Closed';
  lastMessageAt: string;
  messages: GuestMessage[];
}

/** Khớp GetGuestConversationResult (conversation null nếu visit chưa mở hội thoại). */
export interface GetGuestConversationResult {
  conversation: GuestConversation | null;
}

/** Khớp SendGuestMessageResponse. */
export interface SendGuestMessageResult {
  conversationId: string;
  messageId: string;
  status: 'Open' | 'Closed';
  reopened: boolean;
}

export class GuestApiError extends Error {
  public readonly status: number;
  public readonly code: string | undefined;

  public constructor(status: number, code: string | undefined, message: string) {
    super(message);
    this.name = 'GuestApiError';
    this.status = status;
    this.code = code;
  }
}

// Hook do JourneyCore/router đăng ký (tránh vòng phụ thuộc core↔gateway). Gateway chỉ "phát tín hiệu".
interface GatewayHooks {
  onRescanNeeded: () => void;
  onAckRequired: (currentVersion?: number) => void;
}
let hooks: GatewayHooks | null = null;
export function configureGatewayHooks(h: GatewayHooks): void {
  hooks = h;
}

const RESCAN_CODES = new Set(['session_expired', 'guest_context_missing']);

const MOCK = import.meta.env.VITE_STARHILL_MOCK === '1' && !(typeof window !== 'undefined' && window.navigator.webdriver);

async function parseError(response: Response): Promise<GuestApiError> {
  let code: string | undefined;
  let detail = response.statusText;
  try {
    const body = (await response.json()) as { code?: string; detail?: string; title?: string };
    code = body.code;
    detail = body.detail ?? body.title ?? detail;
  } catch {
    // Keep HTTP status when server did not return ProblemDetails.
  }
  return new GuestApiError(response.status, code, detail);
}

// Áp intercept cross-cutting MỘT chỗ (DI-1). Trả lại error để caller vẫn biết (throw tiếp).
function applyIntercept(error: GuestApiError): void {
  if (error.code && RESCAN_CODES.has(error.code)) {
    hooks?.onRescanNeeded();
  } else if (error.status === 403 && error.code === 'rule_ack_required') {
    hooks?.onAckRequired();
  }
}

async function guestFetch<T>(
  method: 'GET' | 'POST',
  path: string,
  opts: { query?: Record<string, string | undefined>; body?: unknown } = {},
): Promise<T> {
  const url = new URL(path, window.location.origin);
  if (opts.query) {
    for (const [k, v] of Object.entries(opts.query)) {
      if (v !== undefined && v !== '') {
        url.searchParams.set(k, v);
      }
    }
  }
  const init: RequestInit = {
    method,
    credentials: 'include',
    headers: { Accept: 'application/json', ...(opts.body !== undefined ? { 'Content-Type': 'application/json' } : {}) },
    ...(opts.body !== undefined ? { body: JSON.stringify(opts.body) } : {}),
  };
  const response = await fetch(url.pathname + url.search, init);
  if (!response.ok) {
    const error = await parseError(response);
    applyIntercept(error);
    throw error;
  }
  if (response.status === 204) {
    return undefined as T;
  }
  return (await response.json()) as T;
}

// ---- MOCK DEV-only (xem offline; prod/e2e-real KHÔNG đụng) ----
const mockDelay = (): Promise<void> => new Promise((r) => setTimeout(r, 200));

function buildMockResolve(token: string): GuestResolveResponse {
  const base: GuestResolveResponse = {
    room: { id: '00000000-0000-0000-0000-000000000101', number: '101', building: 'A', floor: 1 },
    resort: { id: '00000000-0000-0000-0000-000000000001', name: 'Star Hill Resort', logoUrl: null },
    languages: ['vi', 'en', 'ko', 'zh'],
    defaultLanguage: 'vi',
    visit: { id: '00000000-0000-0000-0000-000000000001', portalWindowExpiresAt: '2099-12-31T23:59:59Z' },
    features: {
      faqEnabled: true, chatEnabled: true, housekeepingEnabled: true,
      ruleAckRequiredForFaq: true, ruleAckRequiredForChat: true, ruleAckRequiredForHousekeeping: false,
    },
  };
  const m = /^D(\d{4})/.exec(token);
  if (m) {
    const seq = Number.parseInt(m[1], 10);
    base.room.id = `00000000-0000-0000-0000-${String(seq).padStart(12, '0')}`;
    base.room.number = String(seq);
    base.room.building = seq % 2 === 0 ? 'A' : 'B';
    base.room.floor = Math.max(1, Math.ceil((seq - 100) / 10));
  }
  return base;
}

const MOCK_RULES: GuestRulesResponse = {
  publicationId: '10000000-0000-0000-0000-000000000003',
  version: 3,
  language: 'vi',
  sections: [
    { key: 'welcome', sortOrder: 10, isRequired: true, requireScrollEnd: false, minReadSeconds: 3, title: 'Chào mừng đến Star Hill', bodyHtmlSanitized: '<p>Vui lòng giữ chìa khoá phòng và thẻ QR trong suốt kỳ nghỉ. Cảm ơn quý khách đã lựa chọn Star Hill Resort.</p>', resolvedLanguage: 'vi', isFallback: false, isMissing: false },
    { key: 'quiet-hours', sortOrder: 20, isRequired: true, requireScrollEnd: true, minReadSeconds: 5, title: 'Giờ yên tĩnh & An toàn', bodyHtmlSanitized: '<p>Giữ yên tĩnh từ <strong>22:00 đến 07:00</strong>. Khoá cửa cẩn thận khi rời phòng. Không mang chất dễ cháy nổ vào khuôn viên.</p><p>Bể bơi mở 06:00–22:00; trẻ dưới 12 tuổi cần người lớn giám sát.</p>', resolvedLanguage: 'vi', isFallback: false, isMissing: false },
  ],
};
let mockAckedVersion = 0;

const MOCK_FAQ: GuestFaqTree = {
  language: 'vi',
  categories: [
    {
      id: '21000000-0000-0000-0000-000000000001', key: 'arrival', sortOrder: 10, name: 'Nhận phòng & Tiện ích',
      resolvedLanguage: 'vi', isFallback: false, isMissing: false,
      items: [
        {
          id: '23000000-0000-0000-0000-000000000001', sortOrder: 10, question: 'Mật khẩu Wi-Fi là gì?',
          answerHtmlSanitized: '<p>Wi-Fi: <strong>StarHill-Guest</strong>, mật khẩu <strong>welcome2026</strong>.</p>',
          resolvedLanguage: 'vi', isFallback: false, isMissing: false, children: [],
        },
        {
          id: '23000000-0000-0000-0000-000000000002', sortOrder: 20, question: 'Giờ ăn sáng?',
          answerHtmlSanitized: '<p>Buffet sáng 06:30–10:00 tại nhà hàng tầng 1.</p>',
          resolvedLanguage: 'vi', isFallback: false, isMissing: false,
          children: [
            {
              id: '23000000-0000-0000-0000-000000000003', sortOrder: 10, question: 'Có phục vụ ăn chay không?',
              answerHtmlSanitized: '<p>Có, vui lòng báo lễ tân trước 21:00 hôm trước.</p>',
              resolvedLanguage: 'vi', isFallback: false, isMissing: false, children: [],
            },
          ],
        },
      ],
    },
    {
      id: '21000000-0000-0000-0000-000000000002', key: 'services', sortOrder: 20, name: 'Dịch vụ',
      resolvedLanguage: 'vi', isFallback: false, isMissing: false,
      items: [
        {
          id: '23000000-0000-0000-0000-000000000004', sortOrder: 10, question: 'Spa mở cửa mấy giờ?',
          answerHtmlSanitized: '<p>Spa mở 09:00–21:00. Đặt lịch qua lễ tân.</p>',
          resolvedLanguage: 'vi', isFallback: false, isMissing: false, children: [],
        },
      ],
    },
  ],
};

// MOCK conversation STATEFUL (DEV offline). KHÔNG seed tin STAFF giả (không bịa nhân viên) — bắt đầu rỗng,
// guest gửi thì hiện. Real staff reply đến từ backend thật.
let mockConversation: GuestConversation | null = null;
let mockMsgSeq = 1;

export const apiGateway = {
  resolve: async (token: string): Promise<GuestResolveResponse> => {
    if (MOCK) {
      await mockDelay();
      return buildMockResolve(token);
    }
    return guestFetch<GuestResolveResponse>('POST', '/v1/guest/resolve', { body: { token } });
  },

  getRules: async (roomId: string, lang: string | null): Promise<GuestRulesResponse> => {
    if (MOCK) {
      await mockDelay();
      return MOCK_RULES;
    }
    return guestFetch<GuestRulesResponse>('GET', '/v1/guest/rules', { query: { roomId, lang: lang ?? undefined } });
  },

  acknowledgeRules: async (roomId: string, lang: string | null): Promise<AcknowledgeRulesResponse> => {
    if (MOCK) {
      await mockDelay();
      const already = mockAckedVersion === MOCK_RULES.version;
      mockAckedVersion = MOCK_RULES.version;
      return { rulePublicationId: MOCK_RULES.publicationId, version: MOCK_RULES.version, alreadyAcknowledged: already };
    }
    return guestFetch<AcknowledgeRulesResponse>('POST', '/v1/guest/rules/acknowledge', { body: { roomId, lang } });
  },

  getFaq: async (roomId: string, lang: string | null): Promise<GuestFaqTree> => {
    if (MOCK) {
      await mockDelay();
      return MOCK_FAQ;
    }
    return guestFetch<GuestFaqTree>('GET', '/v1/guest/faq', { query: { roomId, lang: lang ?? undefined } });
  },

  getConversation: async (roomId: string): Promise<GetGuestConversationResult> => {
    if (MOCK) {
      await mockDelay();
      return { conversation: mockConversation };
    }
    return guestFetch<GetGuestConversationResult>('GET', '/v1/guest/conversation', { query: { roomId } });
  },

  sendMessage: async (roomId: string, body: string): Promise<SendGuestMessageResult> => {
    if (MOCK) {
      await mockDelay();
      const now = new Date().toISOString();
      if (!mockConversation) {
        mockConversation = { conversationId: '30000000-0000-0000-0000-000000000001', status: 'Open', lastMessageAt: now, messages: [] };
      }
      const messageId = `40000000-0000-0000-0000-${String(mockMsgSeq++).padStart(12, '0')}`;
      mockConversation.messages.push({ messageId, senderType: 'Guest', body, createdAt: now, readByStaffAt: null });
      mockConversation.status = 'Open';
      mockConversation.lastMessageAt = now;
      return { conversationId: mockConversation.conversationId, messageId, status: 'Open', reopened: false };
    }
    return guestFetch<SendGuestMessageResult>('POST', '/v1/guest/messages', { body: { roomId, body } });
  },
};
