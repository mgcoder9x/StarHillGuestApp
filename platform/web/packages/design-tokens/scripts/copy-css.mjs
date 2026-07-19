import { copyFile, mkdir } from 'node:fs/promises';

await mkdir(new URL('../dist/', import.meta.url), { recursive: true });
await copyFile(new URL('../src/theme.css', import.meta.url), new URL('../dist/theme.css', import.meta.url));
