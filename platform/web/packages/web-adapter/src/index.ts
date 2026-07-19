export type AsyncResourceSnapshot<T> =
  | { readonly status: 'idle' }
  | { readonly status: 'loading'; readonly previous?: T }
  | { readonly status: 'success'; readonly data: T }
  | { readonly status: 'error'; readonly error: unknown; readonly previous?: T };

export interface AsyncResource<T> {
  getSnapshot(): AsyncResourceSnapshot<T>;
  subscribe(listener: () => void): () => void;
  load(): Promise<AsyncResourceSnapshot<T>>;
  cancel(reason?: unknown): void;
  reset(): void;
}

/// Framework-neutral external store. React/Vue/Svelte adapters can bind this contract without duplicating stale-result
/// suppression, cancellation or state transitions in every product.
export function createAsyncResource<T>(loader: (signal: AbortSignal) => Promise<T>): AsyncResource<T> {
  let snapshot: AsyncResourceSnapshot<T> = { status: 'idle' };
  let controller: AbortController | undefined;
  let generation = 0;
  const listeners = new Set<() => void>();

  const publish = (next: AsyncResourceSnapshot<T>): void => {
    snapshot = next;
    for (const listener of listeners) listener();
  };

  return {
    getSnapshot: () => snapshot,
    subscribe(listener) {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    async load() {
      controller?.abort(new DOMException('Superseded by a newer load.', 'AbortError'));
      controller = new AbortController();
      const currentGeneration = ++generation;
      const previous = snapshot.status === 'success' ? snapshot.data : snapshot.status === 'loading' || snapshot.status === 'error' ? snapshot.previous : undefined;
      publish(previous === undefined ? { status: 'loading' } : { status: 'loading', previous });
      try {
        const data = await loader(controller.signal);
        if (currentGeneration === generation) publish({ status: 'success', data });
      } catch (error) {
        if (currentGeneration === generation) {
          publish(previous === undefined ? { status: 'error', error } : { status: 'error', error, previous });
        }
      }
      return snapshot;
    },
    cancel(reason) {
      generation++;
      controller?.abort(reason ?? new DOMException('Resource load cancelled.', 'AbortError'));
      controller = undefined;
      publish({ status: 'idle' });
    },
    reset() {
      generation++;
      controller?.abort(new DOMException('Resource reset.', 'AbortError'));
      controller = undefined;
      publish({ status: 'idle' });
    },
  };
}
