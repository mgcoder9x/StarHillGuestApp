import { ApiError, isProblemDetails, type ProblemDetails } from './problem-details.js';
import { defineRuntimeConfig, type WebRuntimeConfig } from './runtime-config.js';

export interface ApiRequest<TBody = unknown> {
  readonly method?: string;
  readonly headers?: HeadersInit;
  readonly body?: BodyInit | null;
  readonly json?: TBody;
  readonly signal?: AbortSignal;
  readonly timeoutMs?: number;
  readonly correlationId?: string;
  readonly idempotencyKey?: string;
  readonly credentials?: RequestCredentials;
}

export interface ApiResponse<T> {
  readonly data: T;
  readonly status: number;
  readonly headers: Headers;
  readonly correlationId: string | undefined;
}

export interface HttpClientOptions {
  readonly fetch?: typeof fetch;
  readonly prepareRequest?: (request: Request) => Request | Promise<Request>;
}

export interface HttpClient {
  request<TResponse, TBody = unknown>(path: string, request?: ApiRequest<TBody>): Promise<ApiResponse<TResponse>>;
}

export function createHttpClient(
  configInput: Partial<WebRuntimeConfig> = {},
  options: HttpClientOptions = {},
): HttpClient {
  const config = defineRuntimeConfig(configInput);
  const fetchImplementation = options.fetch ?? globalThis.fetch;
  if (typeof fetchImplementation !== 'function') {
    throw new TypeError('A Fetch API implementation is required.');
  }

  return {
    async request<TResponse, TBody = unknown>(
      path: string,
      request: ApiRequest<TBody> = {},
    ): Promise<ApiResponse<TResponse>> {
      if (request.body !== undefined && request.json !== undefined) {
        throw new TypeError('Specify either body or json, not both.');
      }

      const timeoutMs = request.timeoutMs ?? config.requestTimeoutMs;
      if (!Number.isSafeInteger(timeoutMs) || timeoutMs < 100 || timeoutMs > 300_000) {
        throw new RangeError('timeoutMs must be an integer between 100 and 300000.');
      }

      const url = resolveUrl(config.apiBaseUrl, path);

      const correlationId = request.correlationId ?? crypto.randomUUID();
      const headers = new Headers(request.headers);
      headers.set('Accept', 'application/problem+json, application/json');
      headers.set(config.correlationHeader, correlationId);
      if (request.idempotencyKey) {
        headers.set(config.idempotencyHeader, request.idempotencyKey);
      }

      let body = request.body;
      if (request.json !== undefined) {
        headers.set('Content-Type', 'application/json');
        body = JSON.stringify(request.json);
      }

      const timeoutController = new AbortController();
      let timeoutTriggered = false;
      const relayAbort = (): void => timeoutController.abort(request.signal?.reason);
      if (request.signal?.aborted) {
        relayAbort();
      } else {
        request.signal?.addEventListener('abort', relayAbort, { once: true });
      }
      const timeoutHandle = setTimeout(
        () => {
          timeoutTriggered = true;
          timeoutController.abort(new DOMException('Request timed out.', 'TimeoutError'));
        },
        timeoutMs,
      );

      try {
        const requestInit: RequestInit = {
          method: request.method ?? 'GET',
          headers,
          signal: timeoutController.signal,
          credentials: request.credentials ?? 'same-origin',
          ...(body !== undefined ? { body } : {}),
        };
        const initial = new Request(url, requestInit);
        const prepared = options.prepareRequest ? await options.prepareRequest(initial) : initial;
        const response = await fetchImplementation(prepared);
        const responseCorrelation = response.headers.get(config.correlationHeader) ?? correlationId;

        if (!response.ok) {
          const problem = await readProblemDetails(response);
          throw new ApiError(
            problem?.detail ?? problem?.title ?? `HTTP request failed with status ${response.status}.`,
            {
              kind: problem ? 'problem' : 'http',
              status: response.status,
              ...(problem ? { problem } : {}),
              correlationId: responseCorrelation,
            },
          );
        }

        const data = await readSuccessBody<TResponse>(response, responseCorrelation);
        return { data, status: response.status, headers: response.headers, correlationId: responseCorrelation };
      } catch (error) {
        if (error instanceof ApiError) {
          throw error;
        }

        if (timeoutController.signal.aborted) {
          const timedOut = timeoutTriggered;
          throw new ApiError(timedOut ? 'Request timed out.' : 'Request was aborted.', {
            kind: timedOut ? 'timeout' : 'aborted',
            correlationId,
            cause: error,
          });
        }

        throw new ApiError('Network request failed.', { kind: 'network', correlationId, cause: error });
      } finally {
        clearTimeout(timeoutHandle);
        request.signal?.removeEventListener('abort', relayAbort);
      }
    },
  };
}

function resolveUrl(baseUrl: string, path: string): string {
  const trimmedPath = path.trim();
  if (trimmedPath.length === 0) {
    throw new TypeError('Request path must not be empty.');
  }

  if (/^https?:\/\//i.test(trimmedPath)) {
    throw new TypeError('Request path must be relative to the configured API base URL.');
  }

  // WHATWG URL parsing treats backslashes as slashes for special schemes. Reject them so an
  // origin-relative base cannot turn a caller-controlled path into a network-path reference.
  if (trimmedPath.includes('\\')) {
    throw new TypeError('Request path must not contain backslashes.');
  }

  const normalizedPath = trimmedPath.replace(/^\/+/, '');
  return baseUrl === '/' ? `/${normalizedPath}` : `${baseUrl}/${normalizedPath}`;
}

async function readProblemDetails(response: Response): Promise<ProblemDetails | undefined> {
  if (!isJson(response.headers.get('content-type'))) {
    return undefined;
  }

  try {
    const payload: unknown = await response.json();
    return isProblemDetails(payload) ? payload : undefined;
  } catch {
    return undefined;
  }
}

async function readSuccessBody<T>(response: Response, correlationId: string): Promise<T> {
  if (response.status === 204 || response.status === 205
      || response.body === null || response.headers.get('content-length') === '0') {
    return undefined as T;
  }

  if (!isJson(response.headers.get('content-type'))) {
    throw new ApiError('Successful response was not JSON.', {
      kind: 'invalid-response',
      status: response.status,
      correlationId,
    });
  }

  try {
    return await response.json() as T;
  } catch (error) {
    throw new ApiError('Successful response contained invalid JSON.', {
      kind: 'invalid-response',
      status: response.status,
      correlationId,
      cause: error,
    });
  }
}

function isJson(contentType: string | null): boolean {
  const mediaType = contentType?.split(';', 1)[0]?.trim().toLowerCase();
  return mediaType === 'application/json' || mediaType?.endsWith('+json') === true;
}
