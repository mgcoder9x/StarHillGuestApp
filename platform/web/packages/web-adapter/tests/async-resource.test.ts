import { describe, expect, it, vi } from 'vitest';
import { createAsyncResource } from '../src/index.js';

describe('createAsyncResource', () => {
  it('publishes loading and success snapshots', async () => {
    const resource = createAsyncResource(async () => 42);
    const listener = vi.fn();
    resource.subscribe(listener);
    const result = await resource.load();
    expect(result).toEqual({ status: 'success', data: 42 });
    expect(listener).toHaveBeenCalledTimes(2);
  });

  it('suppresses a stale completion after a newer load', async () => {
    const resolvers: Array<(value: number) => void> = [];
    const resource = createAsyncResource(() => new Promise<number>((resolve) => resolvers.push(resolve)));
    const first = resource.load();
    const second = resource.load();
    resolvers[1]?.(2);
    await second;
    resolvers[0]?.(1);
    await first;
    expect(resource.getSnapshot()).toEqual({ status: 'success', data: 2 });
  });

  it('cancels the active loader and returns to idle', async () => {
    let observedSignal: AbortSignal | undefined;
    const resource = createAsyncResource((signal) => {
      observedSignal = signal;
      return new Promise<never>(() => undefined);
    });
    void resource.load();
    resource.cancel();
    expect(observedSignal?.aborted).toBe(true);
    expect(resource.getSnapshot()).toEqual({ status: 'idle' });
  });
});
