import { describe, expect, it, vi } from 'vitest';
import { createHttpClient } from '../src/http-client.js';

describe('createHttpClient', () => {
  it('sends JSON with correlation and idempotency headers', async () => {
    const fetchMock: typeof fetch = async (input) => {
      const request = input as Request;
      expect(request.url).toBe('http://api.example/v1/orders');
      expect(request.headers.get('content-type')).toBe('application/json');
      expect(request.headers.get('x-correlation-id')).toBe('corr-1');
      expect(request.headers.get('idempotency-key')).toBe('idem-1');
      expect(await request.json()).toEqual({ amount: 10 });
      return Response.json({ id: 'order-1' }, { status: 201 });
    };
    const client = createHttpClient({ apiBaseUrl: 'http://api.example/v1' }, { fetch: fetchMock });

    const response = await client.request<{ id: string }, { amount: number }>('orders', {
      method: 'POST',
      json: { amount: 10 },
      correlationId: 'corr-1',
      idempotencyKey: 'idem-1',
    });

    expect(response.status).toBe(201);
    expect(response.data.id).toBe('order-1');
  });

  it('normalizes RFC Problem Details without losing correlation', async () => {
    const client = createHttpClient({ apiBaseUrl: 'http://api.example' }, {
      fetch: async () => Response.json(
        { title: 'Conflict', status: 409, code: 'orders.version_conflict' },
        { status: 409, headers: { 'X-Correlation-ID': 'server-corr' } },
      ),
    });

    await expect(client.request('/orders/1')).rejects.toMatchObject({
      kind: 'problem',
      status: 409,
      correlationId: 'server-corr',
      problem: { code: 'orders.version_conflict' },
    });
  });

  it('classifies timeout separately from caller cancellation', async () => {
    vi.useFakeTimers();
    try {
      const abortable: typeof fetch = (input): Promise<Response> => new Promise((_, reject) => {
        const request = input as Request;
        if (request.signal.aborted) {
          reject(request.signal.reason);
          return;
        }
        request.signal.addEventListener('abort', () => reject(request.signal.reason), { once: true });
      });
      const client = createHttpClient(
        { apiBaseUrl: 'http://api.example', requestTimeoutMs: 100 },
        { fetch: abortable },
      );

      const pending = client.request('/slow');
      const assertion = expect(pending).rejects.toMatchObject({ kind: 'timeout' });
      await vi.advanceTimersByTimeAsync(100);
      await assertion;
    } finally {
      vi.useRealTimers();
    }
  });

  it('classifies a pre-aborted signal as caller cancellation', async () => {
    const controller = new AbortController();
    controller.abort(new DOMException('Caller cancelled.', 'AbortError'));
    const client = createHttpClient(
      { apiBaseUrl: 'http://api.example' },
      { fetch: async (input) => {
        const request = input as Request;
        request.signal.throwIfAborted();
        return Response.json({});
      } },
    );

    await expect(client.request('/cancelled', { signal: controller.signal }))
      .rejects.toMatchObject({ kind: 'aborted' });
  });

  it('does not treat a caller TimeoutError reason as a client timeout', async () => {
    const controller = new AbortController();
    const abortable: typeof fetch = (input): Promise<Response> => new Promise((_, reject) => {
      const request = input as Request;
      if (request.signal.aborted) {
        reject(request.signal.reason);
        return;
      }
      request.signal.addEventListener('abort', () => reject(request.signal.reason), { once: true });
    });
    const client = createHttpClient({ apiBaseUrl: 'http://api.example' }, { fetch: abortable });
    const pending = client.request('/cancelled', { signal: controller.signal });
    controller.abort(new DOMException('Caller supplied a timeout reason.', 'TimeoutError'));

    await expect(pending).rejects.toMatchObject({ kind: 'aborted' });
  });

  it('accepts empty 205 responses and rejects non-JSON success media types', async () => {
    const fetchMock: typeof fetch = async (input) => {
      const request = input as Request;
      if (request.url.endsWith('/empty')) {
        return new Response(null, { status: 205 });
      }

      return new Response('{}', { status: 200, headers: { 'content-type': 'text/not-json' } });
    };
    const client = createHttpClient({ apiBaseUrl: 'http://api.example' }, { fetch: fetchMock });

    await expect(client.request('/empty')).resolves.toMatchObject({ data: undefined, status: 205 });
    await expect(client.request('/invalid-media')).rejects.toMatchObject({ kind: 'invalid-response' });
  });

  it('rejects absolute request URLs and ambiguous bodies', async () => {
    const client = createHttpClient(
      { apiBaseUrl: 'http://api.example' },
      { fetch: async () => Response.json({}) },
    );

    await expect(client.request('https://evil.example')).rejects.toThrow(TypeError);
    await expect(client.request('\\\\evil.example')).rejects.toThrow(TypeError);
    await expect(client.request('/x', { body: 'x', json: { x: 1 } })).rejects.toThrow(TypeError);
  });
});
