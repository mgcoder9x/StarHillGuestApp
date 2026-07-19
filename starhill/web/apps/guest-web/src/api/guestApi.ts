export interface GuestResolveResponse {
  room: {
    id: string;
    number: string;
    building: string | null;
    floor: number | null;
  };
  resort: {
    id: string;
    name: string;
    logoUrl: string | null;
  };
  languages: string[];
  defaultLanguage: string;
  visit: {
    id: string;
    portalWindowExpiresAt: string;
  };
  features: {
    faqEnabled: boolean;
    chatEnabled: boolean;
    housekeepingEnabled: boolean;
    ruleAckRequiredForFaq: boolean;
    ruleAckRequiredForChat: boolean;
    ruleAckRequiredForHousekeeping: boolean;
  };
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

const MOCK = import.meta.env.VITE_STARHILL_MOCK === '1' && !(typeof window !== 'undefined' && window.navigator.webdriver);

const MOCK_RESOLVE_RESPONSE: GuestResolveResponse = {
  room: {
    id: '00000000-0000-0000-0000-000000000101',
    number: '101',
    building: 'A',
    floor: 1,
  },
  resort: {
    id: '00000000-0000-0000-0000-000000000001',
    name: 'Star Hill Resort',
    logoUrl: null,
  },
  languages: ['vi', 'en', 'ko', 'zh'],
  defaultLanguage: 'vi',
  visit: {
    id: '00000000-0000-0000-0000-000000000001',
    portalWindowExpiresAt: '2099-12-31T23:59:59Z',
  },
  features: {
    faqEnabled: true,
    chatEnabled: true,
    housekeepingEnabled: true,
    ruleAckRequiredForFaq: false,
    ruleAckRequiredForChat: false,
    ruleAckRequiredForHousekeeping: false,
  },
};

function buildMockResolveResponse(token: string): GuestResolveResponse {
  const response = structuredClone(MOCK_RESOLVE_RESPONSE);
  const match = /^D(\d{4})/.exec(token);
  if (!match) return response;

  const sequence = Number.parseInt(match[1], 10);
  response.room.id = `00000000-0000-0000-0000-${String(sequence).padStart(12, '0')}`;
  response.room.number = String(sequence);
  response.room.building = sequence % 2 === 0 ? 'A' : 'B';
  response.room.floor = Math.max(1, Math.ceil((sequence - 100) / 10));
  return response;
}

async function parseError(response: Response): Promise<GuestApiError> {
  let code: string | undefined;
  let detail = response.statusText;
  try {
    const body = (await response.json()) as { code?: string; detail?: string; title?: string };
    code = body.code;
    detail = body.detail ?? body.title ?? detail;
  } catch {
    // Keep the HTTP status when the server did not return ProblemDetails.
  }
  return new GuestApiError(response.status, code, detail);
}

export async function resolveGuestToken(token: string): Promise<GuestResolveResponse> {
  if (MOCK) {
    await new Promise((resolve) => setTimeout(resolve, 250));
    return buildMockResolveResponse(token);
  }

  const response = await fetch('/v1/guest/resolve', {
    method: 'POST',
    credentials: 'include',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ token }),
  });

  if (!response.ok) {
    throw await parseError(response);
  }

  return (await response.json()) as GuestResolveResponse;
}
