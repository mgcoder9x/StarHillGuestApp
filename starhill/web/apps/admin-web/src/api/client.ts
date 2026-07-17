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
        accessToken: 'mock.dev.token',
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
      const all = status ? buildMockRooms().filter((r) => r.status === status) : buildMockRooms();
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
};
