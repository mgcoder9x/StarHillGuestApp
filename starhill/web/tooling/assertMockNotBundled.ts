/**
 * Chốt FAIL-CLOSED dùng chung cho MỌI SPA StarHill (QR-N-089).
 *
 * BẢN CHẤT VẤN ĐỀ (đo được, không suy đoán): chế độ mock (`VITE_STARHILL_MOCK=1`) tồn tại để xem/tương tác UI
 * khi máy không có backend. Nó bật qua `.env.mock` + `vite --mode mock`. NHƯNG Vite còn nạp biến `VITE_*` từ
 * `process.env`, nên `VITE_STARHILL_MOCK=1 pnpm build` (mode=production) VẪN nhúng dữ liệu bịa vào bundle:
 * đã verify bằng cách grep marker mock trong `dist/assets/*.js` → có mặt. Hệ quả thương mại: khách quét QR thấy
 * phòng/nội quy/feature-flag GIẢ, không gọi backend, không có lỗi nào nổi lên → sai im lặng, tệ hơn cả sập.
 *
 * QUYẾT ĐỊNH: mock là năng lực CHỈ dev-server. Vì vậy MỌI `vite build` có cờ mock bật đều phải THẤT BẠI ngay,
 * bất kể cờ đến từ `.env*` hay biến shell, bất kể `--mode`. Không có nhu cầu hợp lệ nào cho "artifact mock":
 * bản mockup demo là ROUTE (`/demo`) trong bundle thật, không phải một chế độ build.
 */
export interface MockGateInput {
  /** Vite truyền 'build' | 'serve'. */
  command: string;
  /** `--mode` (production/development/mock/...). */
  mode: string;
  /** Tên app để thông báo lỗi chỉ đúng chỗ. */
  appName: string;
  /** Kết quả `loadEnv(mode, cwd, 'VITE_')` — biến từ file .env*. */
  fileEnv: Record<string, string>;
}

export const MOCK_ENV_KEY = 'VITE_STARHILL_MOCK';

/** Ném lỗi (chặn build) nếu cờ mock bật trong lúc build. Không tác dụng phụ khi `vite serve`. */
export function assertMockNotBundled(input: MockGateInput): void {
  if (input.command !== 'build') {
    return;
  }

  const fromFile = input.fileEnv[MOCK_ENV_KEY];
  const fromShell = process.env[MOCK_ENV_KEY];
  const enabled = fromFile === '1' || fromShell === '1';
  if (!enabled) {
    return;
  }

  const source = fromShell === '1' ? `biến môi trường shell (${MOCK_ENV_KEY}=1)` : `.env của mode "${input.mode}"`;
  throw new Error(
    `[${input.appName}] CHẶN BUILD: chế độ mock đang bật qua ${source}. ` +
      'Mock CHỈ dùng cho dev-server (`vite --mode mock`); nếu build tiếp, bundle production sẽ phục vụ dữ liệu bịa ' +
      'cho người dùng thật mà không phát sinh lỗi nào. Bỏ biến/không dùng --mode mock rồi build lại.',
  );
}
