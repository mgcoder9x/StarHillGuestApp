// api-client admin: fetch wrapper same-origin (/v1). JWT truyền Bearer (từ Pinia in-memory — không localStorage, an
// toàn XSS, QR-AD-047). Map lỗi ProblemDetails → ApiError có `code` để UI localize. Prod same-origin; dev Vite proxy.

/** Số liệu Dashboard (khớp DashboardStatsResponse của Host — task 11.1). */
export interface DashboardStats {
  unreadConversations: number;
  openConversations: number;
  openHousekeepingTickets: number;
  activeRooms: number;
  rulesAcksToday: number;
}

export interface LoginResult {
  accessToken: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}

/** Trạng thái phòng — khớp Rooms.Domain/RoomStatus (Host serialize string enum, QR-AD-021). */
export type RoomStatus = 'Active' | 'Inactive' | 'Maintenance';

/** Item read-model phòng — khớp Rooms.Application/IRoomQueries.RoomListItem (camelCase JSON). */
export interface RoomListItem {
  roomId: string;
  roomNumber: string;
  building: string | null;
  floor: number | null;
  status: RoomStatus;
  activeTokenPreview: string | null;
  activeTokenVersion: number;
  createdAt: string;
}

/** Kết quả phân trang — khớp Bedrock.Application/UseCases/Paging.PagedResult<T>. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface CreateRoomResult {
  roomId: string;
  tokenPreview: string;
}

export interface RotateRoomTokenResult {
  tokenPreview: string;
}

/** Section preview đã render từ Draft Rules; HTML trong body đã được backend sanitize trước khi trả. */
export interface RenderedRuleSection {
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

export interface RuleDraftPreview {
  language: string;
  sections: RenderedRuleSection[];
}

export interface RulePublicationHistoryItem {
  publicationId: string;
  version: number;
  publishedAt: string;
  publishedByUserId: string | null;
  changeNote: string | null;
  isCurrent: boolean;
}

export interface RulePublicationHistory {
  publications: RulePublicationHistoryItem[];
}

export interface RuleAdminTranslation {
  translationId: string;
  rowVersion: number;
  languageCode: string;
  title: string | null;
  bodyHtmlSanitized: string | null;
}

export interface RuleAdminSection {
  sectionId: string;
  rowVersion: number;
  key: string;
  sortOrder: number;
  isRequired: boolean;
  requireScrollEnd: boolean;
  minReadSeconds: number;
  missingLanguages: string[];
  translations: RuleAdminTranslation[];
}

export interface RuleAdminDraft {
  ruleSetId: string | null;
  rowVersion: number | null;
  enabledLanguageCodes: string[];
  defaultLanguageCode: string;
  sections: RuleAdminSection[];
}

export interface FaqAdminCategoryTranslation {
  translationId: string;
  rowVersion: number;
  languageCode: string;
  name: string | null;
}

export interface FaqAdminItemTranslation {
  translationId: string;
  rowVersion: number;
  languageCode: string;
  question: string | null;
  answerHtmlSanitized: string | null;
}

export interface FaqAdminItem {
  itemId: string;
  rowVersion: number;
  categoryId: string;
  parentId: string | null;
  sortOrder: number;
  isActive: boolean;
  missingLanguages: string[];
  translations: FaqAdminItemTranslation[];
  children: FaqAdminItem[];
}

export interface FaqAdminCategory {
  categoryId: string;
  rowVersion: number;
  key: string;
  sortOrder: number;
  isActive: boolean;
  missingLanguages: string[];
  translations: FaqAdminCategoryTranslation[];
  items: FaqAdminItem[];
}

export interface FaqAdminTree {
  enabledLanguageCodes: string[];
  defaultLanguageCode: string;
  categories: FaqAdminCategory[];
}

export class ApiError extends Error {
  public readonly status: number;
  public readonly code: string | undefined;

  public constructor(status: number, code: string | undefined, message: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
  }
}

async function parseError(response: Response): Promise<ApiError> {
  let code: string | undefined;
  let detail = response.statusText;
  try {
    const body = (await response.json()) as { code?: string; detail?: string; title?: string };
    // ProblemDetails Bedrock mang `code` (extension) + detail/title.
    code = body.code;
    detail = body.detail ?? body.title ?? detail;
  } catch {
    // body rỗng/không JSON — giữ statusText.
  }
  return new ApiError(response.status, code, detail);
}

async function request<T>(path: string, init: RequestInit, token?: string | null): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set('Accept', 'application/json');
  if (init.body !== undefined) {
    headers.set('Content-Type', 'application/json');
  }
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(path, { ...init, headers });
  if (!response.ok) {
    throw await parseError(response);
  }
  if (response.status === 204) {
    return undefined as T;
  }
  return (await response.json()) as T;
}

// Chế độ MOCK DEV-only (bật khi VITE_STARHILL_MOCK=1 — chỉ qua `vite --mode mock`, KHÔNG có trong prod/normal-dev).
// Mục đích: xem/tương tác UI KHÔNG cần backend (máy không Docker). Prod build + Playwright real-fetch KHÔNG đụng nhánh này.
const MOCK = import.meta.env.VITE_STARHILL_MOCK === '1';

const mockDelay = (): Promise<void> => new Promise((resolve) => setTimeout(resolve, 200));

// --- MOCK DEV-only data (tree-shake khỏi prod: chỉ gọi trong nhánh `if (MOCK)`; MOCK=false ở prod). ---
// Sinh danh sách phòng tất định (42 phòng ~ khớp dashboard activeRooms, đủ 3 trạng thái + đủ paging).
function buildMockRooms(): RoomListItem[] {
  const rooms: RoomListItem[] = [];
  for (let i = 1; i <= 42; i += 1) {
    const status: RoomStatus = i % 13 === 0 ? 'Maintenance' : i % 7 === 0 ? 'Inactive' : 'Active';
    rooms.push({
      roomId: `00000000-0000-0000-0000-${String(i).padStart(12, '0')}`,
      roomNumber: String(100 + i),
      building: i % 2 === 0 ? 'A' : 'B',
      floor: Math.ceil(i / 10),
      status,
      activeTokenPreview: status === 'Active' ? `qr_${String(i).padStart(3, '0')}…` : null,
      activeTokenVersion: status === 'Active' ? 1 : 0,
      createdAt: new Date(Date.now() - i * 86_400_000).toISOString(),
    });
  }
  return rooms;
}

const mockRooms = buildMockRooms();
let mockRoomSequence = 1000;
let mockTokenSequence = 1000;
let mockContentSequence = 1000;
let mockRowVersion = 100;

function nextContentId(prefix: string): string {
  // Keep mock IDs valid GUIDs even when the type prefix is shorter than 8 chars.
  return `${prefix.padEnd(8, '0').slice(0, 8)}-0000-0000-0000-${String(mockContentSequence++).padStart(12, '0')}`;
}

function nextRowVersion(): number {
  mockRowVersion += 1;
  return mockRowVersion;
}

function clone<T>(value: T): T {
  return structuredClone(value);
}

const mockRulePublications: RulePublicationHistoryItem[] = [
  {
    publicationId: '10000000-0000-0000-0000-000000000003',
    version: 3,
    publishedAt: '2026-07-17T03:15:00Z',
    publishedByUserId: '20000000-0000-0000-0000-000000000001',
    changeNote: 'Cập nhật giờ yên tĩnh và hướng dẫn an toàn.',
    isCurrent: true,
  },
  {
    publicationId: '10000000-0000-0000-0000-000000000002',
    version: 2,
    publishedAt: '2026-06-10T08:30:00Z',
    publishedByUserId: '20000000-0000-0000-0000-000000000001',
    changeNote: 'Bổ sung quy định hồ bơi.',
    isCurrent: false,
  },
];

const mockRuleDraft: RuleAdminDraft = {
  ruleSetId: '11000000-0000-0000-0000-000000000001',
  rowVersion: 91,
  enabledLanguageCodes: ['vi', 'en', 'ko'],
  defaultLanguageCode: 'vi',
  sections: [
    {
      sectionId: '12000000-0000-0000-0000-000000000001',
      rowVersion: 92,
      key: 'welcome',
      sortOrder: 10,
      isRequired: true,
      requireScrollEnd: false,
      minReadSeconds: 5,
      missingLanguages: ['ko'],
      translations: [
        { translationId: '13000000-0000-0000-0000-000000000001', rowVersion: 93, languageCode: 'vi', title: 'Chao mung den Star Hill', bodyHtmlSanitized: '<p>Vui long giu chia khoa phong trong suot ky nghi.</p>' },
        { translationId: '13000000-0000-0000-0000-000000000002', rowVersion: 94, languageCode: 'en', title: 'Welcome to Star Hill', bodyHtmlSanitized: '<p>Please keep your room key with you.</p>' },
      ],
    },
    {
      sectionId: '12000000-0000-0000-0000-000000000002',
      rowVersion: 95,
      key: 'quiet-hours',
      sortOrder: 20,
      isRequired: true,
      requireScrollEnd: true,
      minReadSeconds: 12,
      missingLanguages: ['en', 'ko'],
      translations: [
        { translationId: '13000000-0000-0000-0000-000000000003', rowVersion: 96, languageCode: 'vi', title: 'Gio yen tinh', bodyHtmlSanitized: '<p>Giu yen tinh tu <strong>22:00 den 07:00</strong>.</p>' },
      ],
    },
  ],
};

function refreshRuleMissingLanguages(section: RuleAdminSection): void {
  const populated = new Set(
    section.translations
      .filter((translation) => Boolean(translation.title?.trim() || translation.bodyHtmlSanitized?.trim()))
      .map((translation) => translation.languageCode.toLowerCase()),
  );
  section.missingLanguages = mockRuleDraft.enabledLanguageCodes.filter((language) => !populated.has(language.toLowerCase()));
}

const mockFaqTree: FaqAdminTree = {
  enabledLanguageCodes: ['vi', 'en', 'ko'],
  defaultLanguageCode: 'vi',
  categories: [
    {
      categoryId: '21000000-0000-0000-0000-000000000001',
      rowVersion: 101,
      key: 'arrival',
      sortOrder: 10,
      isActive: true,
      missingLanguages: ['ko'],
      translations: [
        { translationId: '22000000-0000-0000-0000-000000000001', rowVersion: 102, languageCode: 'vi', name: 'Nhan phong' },
        { translationId: '22000000-0000-0000-0000-000000000002', rowVersion: 103, languageCode: 'en', name: 'Arrival' },
      ],
      items: [
        {
          itemId: '23000000-0000-0000-0000-000000000001',
          rowVersion: 104,
          categoryId: '21000000-0000-0000-0000-000000000001',
          parentId: null,
          sortOrder: 10,
          isActive: true,
          missingLanguages: ['ko'],
          translations: [{ translationId: '24000000-0000-0000-0000-000000000001', rowVersion: 105, languageCode: 'vi', question: 'Nhan phong luc may gio?', answerHtmlSanitized: '<p>Sau 14:00.</p>' }],
          children: [],
        },
      ],
    },
  ],
};

function refreshFaqMissingLanguages(category: FaqAdminCategory): void {
  const languages = mockFaqTree.enabledLanguageCodes;
  const categoryPresent = new Set(category.translations.filter((item) => item.name?.trim()).map((item) => item.languageCode.toLowerCase()));
  category.missingLanguages = languages.filter((language) => !categoryPresent.has(language.toLowerCase()));
  const visit = (item: FaqAdminItem): void => {
    const present = new Set(item.translations.filter((translation) => translation.question?.trim() || translation.answerHtmlSanitized?.trim()).map((translation) => translation.languageCode.toLowerCase()));
    item.missingLanguages = languages.filter((language) => !present.has(language.toLowerCase()));
    item.children.forEach(visit);
  };
  category.items.forEach(visit);
}

function findFaqItem(itemId: string): FaqAdminItem | undefined {
  const search = (items: FaqAdminItem[]): FaqAdminItem | undefined => {
    for (const item of items) {
      if (item.itemId === itemId) return item;
      const child = search(item.children);
      if (child) return child;
    }
    return undefined;
  };
  for (const category of mockFaqTree.categories) {
    const item = search(category.items);
    if (item) return item;
  }
  return undefined;
}

function removeFaqItem(items: FaqAdminItem[], itemId: string): boolean {
  const index = items.findIndex((item) => item.itemId === itemId);
  if (index >= 0) {
    items.splice(index, 1);
    return true;
  }
  return items.some((item) => removeFaqItem(item.children, itemId));
}

function buildMockRulePreview(requestedLanguage: string | null): RuleDraftPreview {
  const language = requestedLanguage || 'vi';
  const isEnglish = language === 'en';
  return {
    language,
    sections: [
      {
        key: 'welcome',
        sortOrder: 10,
        isRequired: true,
        requireScrollEnd: false,
        minReadSeconds: 5,
        title: isEnglish ? 'Welcome to Star Hill' : 'Chào mừng đến Star Hill',
        bodyHtmlSanitized: isEnglish
          ? '<p>Please keep your room key and QR card with you.</p>'
          : '<p>Vui lòng giữ chìa khoá phòng và thẻ QR trong suốt kỳ nghỉ.</p>',
        resolvedLanguage: language,
        isFallback: false,
        isMissing: false,
      },
      {
        key: 'quiet-hours',
        sortOrder: 20,
        isRequired: true,
        requireScrollEnd: true,
        minReadSeconds: 12,
        title: 'Giờ yên tĩnh',
        bodyHtmlSanitized: '<p>Giữ yên tĩnh từ <strong>22:00 đến 07:00</strong>.</p>',
        resolvedLanguage: 'vi',
        isFallback: language !== 'vi',
        isMissing: false,
      },
    ],
  };
}

// QR mock = SVG placeholder TRUNG THỰC ("QR demo") — KHÔNG giả mã quét được. Real mode trả PNG thật từ BE.
const MOCK_QR_DATA_URL =
  'data:image/svg+xml;utf8,' +
  encodeURIComponent(
    '<svg xmlns="http://www.w3.org/2000/svg" width="240" height="260" viewBox="0 0 240 260">' +
      '<rect width="240" height="260" fill="#ffffff"/>' +
      '<rect x="20" y="20" width="200" height="200" fill="none" stroke="#0f172a" stroke-width="4" rx="12"/>' +
      '<g fill="#0f172a">' +
      '<rect x="36" y="36" width="48" height="48"/><rect x="156" y="36" width="48" height="48"/>' +
      '<rect x="36" y="156" width="48" height="48"/><rect x="104" y="104" width="32" height="32"/>' +
      '<rect x="156" y="120" width="16" height="16"/><rect x="188" y="156" width="16" height="16"/>' +
      '<rect x="120" y="188" width="16" height="16"/><rect x="156" y="188" width="48" height="16"/>' +
      '</g>' +
      '<text x="120" y="245" text-anchor="middle" font-family="system-ui,sans-serif" font-size="16" fill="#64748b">QR demo</text>' +
      '</svg>',
  );

export const api = {
  login: async (username: string, password: string): Promise<LoginResult> => {
    if (MOCK) {
      await mockDelay();
      return {
        accessToken: 'eyJhbGciOiJub25lIn0.eyJyb2xlIjoiYWRtaW4ifQ.mock',
        refreshToken: 'mock.dev.refresh',
        refreshTokenExpiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      };
    }
    return request<LoginResult>('/v1/token/login', { method: 'POST', body: JSON.stringify({ username, password }) });
  },

  getDashboardStats: async (token: string | null): Promise<DashboardStats> => {
    if (MOCK) {
      await mockDelay();
      return { unreadConversations: 5, openConversations: 3, openHousekeepingTickets: 7, activeRooms: 42, rulesAcksToday: 11 };
    }
    return request<DashboardStats>('/v1/dashboard/stats', { method: 'GET' }, token);
  },

  // GET /v1/rooms (RequireStaff) — danh sách phòng phân trang, lọc trạng thái. QR-AD-002: đọc read-model qua Api.
  listRooms: async (
    status: RoomStatus | null,
    page: number,
    pageSize: number,
    token: string | null,
  ): Promise<PagedResult<RoomListItem>> => {
    if (MOCK) {
      await mockDelay();
      const all = status ? mockRooms.filter((r) => r.status === status) : mockRooms;
      const start = (page - 1) * pageSize;
      return { items: all.slice(start, start + pageSize), page, pageSize, total: all.length };
    }
    const q = new URLSearchParams();
    if (status) {
      q.set('status', status);
    }
    q.set('page', String(page));
    q.set('pageSize', String(pageSize));
    return request<PagedResult<RoomListItem>>(`/v1/rooms?${q.toString()}`, { method: 'GET' }, token);
  },

  // GET /v1/rooms/{id}/qr.png (RequireStaff) — trả PNG binary CẦN Bearer → KHÔNG dùng <img src> trực tiếp được;
  // fetch blob rồi tạo objectURL (caller PHẢI revoke khi xong để tránh rò bộ nhớ).
  getRoomQrObjectUrl: async (roomId: string, token: string | null): Promise<string> => {
    if (MOCK) {
      await mockDelay();
      return MOCK_QR_DATA_URL;
    }
    const headers = new Headers({ Accept: 'image/png' });
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }
    const response = await fetch(`/v1/rooms/${roomId}/qr.png`, { method: 'GET', headers });
    if (!response.ok) {
      throw await parseError(response);
    }
    const blob = await response.blob();
    return URL.createObjectURL(blob);
  },

  // Admin-only mutations. The mock branch keeps one in-memory collection so the dev UI exercises the same
  // reload-after-command flow as the real API instead of pretending every request succeeded independently.
  createRoom: async (input: { roomNumber: string; building: string | null; floor: number | null }, token: string | null): Promise<CreateRoomResult> => {
    if (MOCK) {
      await mockDelay();
      if (mockRooms.some((room) => room.roomNumber === input.roomNumber)) {
        throw new ApiError(400, 'validation_error', 'Số phòng đã tồn tại.');
      }
      const roomId = `00000000-0000-0000-0000-${String(mockRoomSequence++).padStart(12, '0')}`;
      const tokenPreview = `qr_new_${String(mockTokenSequence++).padStart(4, '0')}…`;
      mockRooms.push({ roomId, roomNumber: input.roomNumber, building: input.building, floor: input.floor, status: 'Active', activeTokenPreview: tokenPreview, activeTokenVersion: 1, createdAt: new Date().toISOString() });
      return { roomId, tokenPreview };
    }
    return request<CreateRoomResult>('/v1/rooms', { method: 'POST', body: JSON.stringify(input) }, token);
  },

  updateRoom: async (roomId: string, input: { roomNumber: string; building: string | null; floor: number | null }, token: string | null): Promise<void> => {
    if (MOCK) {
      await mockDelay();
      const room = mockRooms.find((item) => item.roomId === roomId);
      if (!room) throw new ApiError(404, 'not_found', 'Không tìm thấy phòng.');
      if (mockRooms.some((item) => item.roomId !== roomId && item.roomNumber === input.roomNumber)) {
        throw new ApiError(400, 'validation_error', 'Số phòng đã tồn tại.');
      }
      Object.assign(room, input);
      return;
    }
    await request<void>(`/v1/rooms/${roomId}`, { method: 'PUT', body: JSON.stringify(input) }, token);
  },

  changeRoomStatus: async (roomId: string, status: RoomStatus, token: string | null): Promise<void> => {
    if (MOCK) {
      await mockDelay();
      const room = mockRooms.find((item) => item.roomId === roomId);
      if (!room) throw new ApiError(404, 'not_found', 'Không tìm thấy phòng.');
      room.status = status;
      return;
    }
    await request<void>(`/v1/rooms/${roomId}/status`, { method: 'PATCH', body: JSON.stringify({ status }) }, token);
  },

  deleteRoom: async (roomId: string, token: string | null): Promise<void> => {
    if (MOCK) {
      await mockDelay();
      const index = mockRooms.findIndex((item) => item.roomId === roomId);
      if (index < 0) throw new ApiError(404, 'not_found', 'Không tìm thấy phòng.');
      mockRooms.splice(index, 1);
      return;
    }
    await request<void>(`/v1/rooms/${roomId}`, { method: 'DELETE' }, token);
  },

  rotateRoomToken: async (roomId: string, reason: string | null, token: string | null): Promise<RotateRoomTokenResult> => {
    if (MOCK) {
      await mockDelay();
      const room = mockRooms.find((item) => item.roomId === roomId);
      if (!room) throw new ApiError(404, 'not_found', 'Không tìm thấy phòng.');
      if (room.status !== 'Active') throw new ApiError(409, 'qr_generation_failed', 'Không thể sinh mã QR.');
      const tokenPreview = `qr_new_${String(mockTokenSequence++).padStart(4, '0')}…`;
      room.activeTokenPreview = tokenPreview;
      room.activeTokenVersion += 1;
      return { tokenPreview };
    }
    return request<RotateRoomTokenResult>(`/v1/rooms/${roomId}/rotate-token`, { method: 'POST', body: JSON.stringify({ reason }) }, token);
  },

  getRuleAdminDraft: async (token: string | null): Promise<RuleAdminDraft> => {
    if (MOCK) {
      await mockDelay();
      return clone(mockRuleDraft);
    }
    return request<RuleAdminDraft>('/v1/rules/admin', { method: 'GET' }, token);
  },

  createRuleSection: async (input: { key: string; sortOrder: number; isRequired: boolean; requireScrollEnd: boolean; minReadSeconds: number }, token: string | null): Promise<{ sectionId: string }> => {
    if (MOCK) {
      await mockDelay();
      const section: RuleAdminSection = {
        sectionId: nextContentId('12'),
        rowVersion: nextRowVersion(),
        ...input,
        missingLanguages: [...mockRuleDraft.enabledLanguageCodes],
        translations: [],
      };
      mockRuleDraft.sections.push(section);
      mockRuleDraft.rowVersion = nextRowVersion();
      return { sectionId: section.sectionId };
    }
    return request<{ sectionId: string }>('/v1/rules/sections', { method: 'POST', body: JSON.stringify(input) }, token);
  },

  updateRuleSection: async (sectionId: string, input: { sortOrder: number; isRequired: boolean; requireScrollEnd: boolean; minReadSeconds: number; expectedRowVersion: number }, token: string | null): Promise<void> => {
    if (MOCK) {
      await mockDelay();
      const section = mockRuleDraft.sections.find((item) => item.sectionId === sectionId);
      if (!section) throw new ApiError(404, 'not_found', 'Rule section not found.');
      if (section.rowVersion !== input.expectedRowVersion) throw new ApiError(409, 'concurrency_conflict', 'Draft changed.');
      Object.assign(section, input, { rowVersion: nextRowVersion() });
      return;
    }
    await request<void>(`/v1/rules/sections/${sectionId}`, { method: 'PUT', body: JSON.stringify(input) }, token);
  },

  deleteRuleSection: async (sectionId: string, expectedRowVersion: number, token: string | null): Promise<void> => {
    if (MOCK) {
      await mockDelay();
      const index = mockRuleDraft.sections.findIndex((item) => item.sectionId === sectionId);
      if (index < 0) throw new ApiError(404, 'not_found', 'Rule section not found.');
      if (mockRuleDraft.sections[index].rowVersion !== expectedRowVersion) throw new ApiError(409, 'concurrency_conflict', 'Draft changed.');
      mockRuleDraft.sections.splice(index, 1);
      mockRuleDraft.rowVersion = nextRowVersion();
      return;
    }
    await request<void>(`/v1/rules/sections/${sectionId}?expectedRowVersion=${expectedRowVersion}`, { method: 'DELETE' }, token);
  },

  upsertRuleTranslation: async (sectionId: string, languageCode: string, input: { title: string | null; bodyHtml: string | null; expectedRowVersion: number | null }, token: string | null): Promise<{ translationId: string }> => {
    if (MOCK) {
      await mockDelay();
      const section = mockRuleDraft.sections.find((item) => item.sectionId === sectionId);
      if (!section) throw new ApiError(404, 'not_found', 'Rule section not found.');
      const existing = section.translations.find((item) => item.languageCode === languageCode);
      if ((existing?.rowVersion ?? null) !== input.expectedRowVersion) throw new ApiError(409, 'concurrency_conflict', 'Draft changed.');
      const translation = existing ?? { translationId: nextContentId('13'), rowVersion: 0, languageCode, title: null, bodyHtmlSanitized: null };
      Object.assign(translation, { title: input.title, bodyHtmlSanitized: input.bodyHtml, rowVersion: nextRowVersion() });
      if (!existing) section.translations.push(translation);
      refreshRuleMissingLanguages(section);
      return { translationId: translation.translationId };
    }
    return request<{ translationId: string }>(`/v1/rules/sections/${sectionId}/translations/${encodeURIComponent(languageCode)}`, { method: 'PUT', body: JSON.stringify(input) }, token);
  },

  publishRules: async (changeNote: string | null, token: string | null): Promise<{ publicationId: string; version: number }> => {
    if (MOCK) {
      await mockDelay();
      const version = (mockRulePublications[0]?.version ?? 0) + 1;
      const publication = { publicationId: nextContentId('14'), version, publishedAt: new Date().toISOString(), publishedByUserId: null, changeNote, isCurrent: true };
      mockRulePublications.forEach((item) => { item.isCurrent = false; });
      mockRulePublications.unshift(publication);
      return { publicationId: publication.publicationId, version };
    }
    return request<{ publicationId: string; version: number }>('/v1/rules/publish', { method: 'POST', body: JSON.stringify({ changeNote }) }, token);
  },

  getRuleDraftPreview: async (language: string | null, token: string | null): Promise<RuleDraftPreview> => {
    if (MOCK) {
      await mockDelay();
      return buildMockRulePreview(language);
    }
    const query = new URLSearchParams();
    if (language) query.set('lang', language);
    const queryString = query.toString();
    const suffix = queryString ? `?${queryString}` : '';
    return request<RuleDraftPreview>(`/v1/rules/preview${suffix}`, { method: 'GET' }, token);
  },

  getRulePublicationHistory: async (token: string | null): Promise<RulePublicationHistory> => {
    if (MOCK) {
      await mockDelay();
      return { publications: mockRulePublications.map((item) => ({ ...item })) };
    }
    return request<RulePublicationHistory>('/v1/rules/publications', { method: 'GET' }, token);
  },

  getFaqAdminTree: async (token: string | null): Promise<FaqAdminTree> => {
    if (MOCK) {
      await mockDelay();
      return clone(mockFaqTree);
    }
    return request<FaqAdminTree>('/v1/faq/admin', { method: 'GET' }, token);
  },

  createFaqCategory: async (input: { key: string; sortOrder: number; isActive: boolean }, token: string | null): Promise<{ categoryId: string }> => {
    if (MOCK) {
      await mockDelay();
      const category: FaqAdminCategory = {
        categoryId: nextContentId('21'), rowVersion: nextRowVersion(), ...input,
        missingLanguages: [...mockFaqTree.enabledLanguageCodes], translations: [], items: [],
      };
      mockFaqTree.categories.push(category);
      return { categoryId: category.categoryId };
    }
    return request<{ categoryId: string }>('/v1/faq/categories', { method: 'POST', body: JSON.stringify(input) }, token);
  },

  updateFaqCategory: async (categoryId: string, input: { sortOrder: number; isActive: boolean; expectedRowVersion: number }, token: string | null): Promise<void> => {
    if (MOCK) {
      await mockDelay();
      const category = mockFaqTree.categories.find((item) => item.categoryId === categoryId);
      if (!category) throw new ApiError(404, 'faq_category_not_found', 'FAQ category not found.');
      if (category.rowVersion !== input.expectedRowVersion) throw new ApiError(409, 'concurrency_conflict', 'FAQ changed.');
      Object.assign(category, input, { rowVersion: nextRowVersion() });
      return;
    }
    await request<void>(`/v1/faq/categories/${categoryId}`, { method: 'PUT', body: JSON.stringify(input) }, token);
  },

  deleteFaqCategory: async (categoryId: string, expectedRowVersion: number, token: string | null): Promise<void> => {
    if (MOCK) {
      await mockDelay();
      const index = mockFaqTree.categories.findIndex((item) => item.categoryId === categoryId);
      if (index < 0) throw new ApiError(404, 'faq_category_not_found', 'FAQ category not found.');
      const category = mockFaqTree.categories[index];
      if (category.rowVersion !== expectedRowVersion) throw new ApiError(409, 'concurrency_conflict', 'FAQ changed.');
      if (category.items.length) throw new ApiError(409, 'faq_category_not_empty', 'FAQ category is not empty.');
      mockFaqTree.categories.splice(index, 1);
      return;
    }
    await request<void>(`/v1/faq/categories/${categoryId}?expectedRowVersion=${expectedRowVersion}`, { method: 'DELETE' }, token);
  },

  upsertFaqCategoryTranslation: async (categoryId: string, languageCode: string, input: { name: string | null; expectedRowVersion: number | null }, token: string | null): Promise<{ translationId: string }> => {
    if (MOCK) {
      await mockDelay();
      const category = mockFaqTree.categories.find((item) => item.categoryId === categoryId);
      if (!category) throw new ApiError(404, 'faq_category_not_found', 'FAQ category not found.');
      const existing = category.translations.find((item) => item.languageCode === languageCode);
      if ((existing?.rowVersion ?? null) !== input.expectedRowVersion) throw new ApiError(409, 'concurrency_conflict', 'FAQ changed.');
      const translation = existing ?? { translationId: nextContentId('22'), rowVersion: 0, languageCode, name: null };
      Object.assign(translation, { name: input.name, rowVersion: nextRowVersion() });
      if (!existing) category.translations.push(translation);
      refreshFaqMissingLanguages(category);
      return { translationId: translation.translationId };
    }
    return request<{ translationId: string }>(`/v1/faq/categories/${categoryId}/translations/${encodeURIComponent(languageCode)}`, { method: 'PUT', body: JSON.stringify(input) }, token);
  },

  createFaqItem: async (input: { categoryId: string; parentId: string | null; sortOrder: number; isActive: boolean }, token: string | null): Promise<{ itemId: string }> => {
    if (MOCK) {
      await mockDelay();
      const category = mockFaqTree.categories.find((item) => item.categoryId === input.categoryId);
      if (!category) throw new ApiError(404, 'faq_category_not_found', 'FAQ category not found.');
      const item: FaqAdminItem = {
        itemId: nextContentId('23'), rowVersion: nextRowVersion(), ...input,
        missingLanguages: [...mockFaqTree.enabledLanguageCodes], translations: [], children: [],
      };
      const parent = input.parentId ? findFaqItem(input.parentId) : undefined;
      (parent?.children ?? category.items).push(item);
      return { itemId: item.itemId };
    }
    return request<{ itemId: string }>('/v1/faq/items', { method: 'POST', body: JSON.stringify(input) }, token);
  },

  updateFaqItem: async (itemId: string, input: { parentId: string | null; sortOrder: number; isActive: boolean; expectedRowVersion: number }, token: string | null): Promise<void> => {
    if (MOCK) {
      await mockDelay();
      const item = findFaqItem(itemId);
      if (!item) throw new ApiError(404, 'faq_item_not_found', 'FAQ item not found.');
      if (item.rowVersion !== input.expectedRowVersion) throw new ApiError(409, 'concurrency_conflict', 'FAQ changed.');
      if (item.parentId !== input.parentId) {
        const category = mockFaqTree.categories.find((entry) => entry.categoryId === item.categoryId);
        const parent = input.parentId ? findFaqItem(input.parentId) : undefined;
        if (!category || (parent && parent.categoryId !== item.categoryId)) {
          throw new ApiError(422, 'faq_invalid_parent', 'FAQ parent is invalid.');
        }
        for (const entry of mockFaqTree.categories) {
          if (removeFaqItem(entry.items, itemId)) break;
        }
        (parent?.children ?? category.items).push(item);
      }
      item.parentId = input.parentId;
      item.sortOrder = input.sortOrder;
      item.isActive = input.isActive;
      item.rowVersion = nextRowVersion();
      return;
    }
    await request<void>(`/v1/faq/items/${itemId}`, { method: 'PUT', body: JSON.stringify(input) }, token);
  },

  deleteFaqItem: async (itemId: string, expectedRowVersion: number, token: string | null): Promise<void> => {
    if (MOCK) {
      await mockDelay();
      const item = findFaqItem(itemId);
      if (!item) throw new ApiError(404, 'faq_item_not_found', 'FAQ item not found.');
      if (item.rowVersion !== expectedRowVersion) throw new ApiError(409, 'concurrency_conflict', 'FAQ changed.');
      if (item.children.length) throw new ApiError(409, 'faq_item_has_children', 'FAQ item has children.');
      for (const category of mockFaqTree.categories) {
        if (removeFaqItem(category.items, itemId)) break;
      }
      return;
    }
    await request<void>(`/v1/faq/items/${itemId}?expectedRowVersion=${expectedRowVersion}`, { method: 'DELETE' }, token);
  },

  upsertFaqItemTranslation: async (itemId: string, languageCode: string, input: { question: string | null; answerHtml: string | null; expectedRowVersion: number | null }, token: string | null): Promise<{ translationId: string }> => {
    if (MOCK) {
      await mockDelay();
      const item = findFaqItem(itemId);
      if (!item) throw new ApiError(404, 'faq_item_not_found', 'FAQ item not found.');
      const existing = item.translations.find((translation) => translation.languageCode === languageCode);
      if ((existing?.rowVersion ?? null) !== input.expectedRowVersion) throw new ApiError(409, 'concurrency_conflict', 'FAQ changed.');
      const translation = existing ?? { translationId: nextContentId('24'), rowVersion: 0, languageCode, question: null, answerHtmlSanitized: null };
      Object.assign(translation, { question: input.question, answerHtmlSanitized: input.answerHtml, rowVersion: nextRowVersion() });
      if (!existing) item.translations.push(translation);
      const category = mockFaqTree.categories.find((entry) => entry.categoryId === item.categoryId);
      if (category) refreshFaqMissingLanguages(category);
      return { translationId: translation.translationId };
    }
    return request<{ translationId: string }>(`/v1/faq/items/${itemId}/translations/${encodeURIComponent(languageCode)}`, { method: 'PUT', body: JSON.stringify(input) }, token);
  },

  reorderFaqCategories: async (entries: Array<{ id: string; sortOrder: number; expectedRowVersion: number }>, token: string | null): Promise<void> => {
    if (MOCK) {
      await mockDelay();
      for (const entry of entries) {
        const category = mockFaqTree.categories.find((item) => item.categoryId === entry.id);
        if (!category || category.rowVersion !== entry.expectedRowVersion) throw new ApiError(409, 'concurrency_conflict', 'FAQ changed.');
        category.sortOrder = entry.sortOrder;
        category.rowVersion = nextRowVersion();
      }
      return;
    }
    await request<void>('/v1/faq/reorder/categories', { method: 'POST', body: JSON.stringify({ entries }) }, token);
  },

  reorderFaqItems: async (categoryId: string, entries: Array<{ id: string; sortOrder: number; expectedRowVersion: number }>, token: string | null): Promise<void> => {
    if (MOCK) {
      await mockDelay();
      for (const entry of entries) {
        const item = findFaqItem(entry.id);
        if (!item || item.categoryId !== categoryId || item.rowVersion !== entry.expectedRowVersion) throw new ApiError(409, 'concurrency_conflict', 'FAQ changed.');
        item.sortOrder = entry.sortOrder;
        item.rowVersion = nextRowVersion();
      }
      return;
    }
    await request<void>(`/v1/faq/reorder/items/${categoryId}`, { method: 'POST', body: JSON.stringify({ entries }) }, token);
  },
};
