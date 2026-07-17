/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Bật mock DEV-only (chỉ set trong .env.mock qua `vite --mode mock`). Prod/normal-dev: undefined. */
  readonly VITE_STARHILL_MOCK?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
