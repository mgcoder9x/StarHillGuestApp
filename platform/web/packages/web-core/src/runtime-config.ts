export interface WebRuntimeConfig {
  readonly apiBaseUrl: string;
  readonly requestTimeoutMs: number;
  readonly correlationHeader: string;
  readonly idempotencyHeader: string;
}

const headerName = /^[!#$%&'*+.^_`|~0-9A-Za-z-]+$/;

export function defineRuntimeConfig(input: Partial<WebRuntimeConfig> = {}): Readonly<WebRuntimeConfig> {
  const apiBaseUrl = normalizeBaseUrl(input.apiBaseUrl ?? '/');
  const requestTimeoutMs = input.requestTimeoutMs ?? 15_000;
  if (!Number.isSafeInteger(requestTimeoutMs) || requestTimeoutMs < 100 || requestTimeoutMs > 300_000) {
    throw new RangeError('requestTimeoutMs must be an integer between 100 and 300000.');
  }

  const correlationHeader = validateHeaderName(input.correlationHeader ?? 'X-Correlation-ID', 'correlationHeader');
  const idempotencyHeader = validateHeaderName(input.idempotencyHeader ?? 'Idempotency-Key', 'idempotencyHeader');

  return Object.freeze({ apiBaseUrl, requestTimeoutMs, correlationHeader, idempotencyHeader });
}

function normalizeBaseUrl(value: string): string {
  const trimmed = value.trim();
  if (trimmed.length === 0) {
    throw new TypeError('apiBaseUrl must not be empty.');
  }

  if (trimmed.startsWith('/')) {
    if (trimmed.startsWith('//') || trimmed.includes('?') || trimmed.includes('#')) {
      throw new TypeError('Relative apiBaseUrl must be an origin-relative path without query or fragment.');
    }

    return trimmed === '/' ? '/' : trimmed.replace(/\/+$/, '');
  }

  const parsed = new URL(trimmed);
  if (!['http:', 'https:'].includes(parsed.protocol) || parsed.username || parsed.password || parsed.search || parsed.hash) {
    throw new TypeError('Absolute apiBaseUrl must be HTTP(S), credential-free, and contain no query or fragment.');
  }

  return parsed.toString().replace(/\/+$/, '');
}

function validateHeaderName(value: string, optionName: string): string {
  const trimmed = value.trim();
  if (!headerName.test(trimmed)) {
    throw new TypeError(`${optionName} is not a valid HTTP header name.`);
  }

  return trimmed;
}
