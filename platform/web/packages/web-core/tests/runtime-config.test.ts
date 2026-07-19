import { describe, expect, it } from 'vitest';
import { defineRuntimeConfig } from '../src/runtime-config.js';

describe('defineRuntimeConfig', () => {
  it('normalizes safe defaults and base paths', () => {
    expect(defineRuntimeConfig()).toMatchObject({ apiBaseUrl: '/', requestTimeoutMs: 15_000 });
    expect(defineRuntimeConfig({ apiBaseUrl: '/v1/' }).apiBaseUrl).toBe('/v1');
  });

  it.each(['', '//evil.example', '/v1?token=x', 'ftp://example.com'])('rejects unsafe base URL %s', (apiBaseUrl) => {
    expect(() => defineRuntimeConfig({ apiBaseUrl })).toThrow();
  });

  it('rejects invalid timeout and header names', () => {
    expect(() => defineRuntimeConfig({ requestTimeoutMs: 0 })).toThrow(RangeError);
    expect(() => defineRuntimeConfig({ correlationHeader: 'bad header' })).toThrow(TypeError);
  });
});
