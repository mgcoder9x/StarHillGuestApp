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
};
