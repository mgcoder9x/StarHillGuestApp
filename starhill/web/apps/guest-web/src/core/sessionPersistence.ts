// SessionPersistence — boundary sessionStorage (design-module 10 §2, DI-5). Lưu context resolve + ackedVersion
// THEO visit.id: đổi visit → dữ liệu cũ không áp dụng (INV4). Chỉ I/O thuần, KHÔNG logic gate (JourneyCore đọc/ghi).
import type { GuestResolveResponse } from './apiGateway';

const CONTEXT_KEY = 'starhill_guest_context_v2';
const ACK_KEY = 'starhill_guest_ack_v2'; // { visitId, ackedVersion }

interface AckRecord {
  visitId: string;
  ackedVersion: number;
}

function safeGet(key: string): string | null {
  try {
    return window.sessionStorage.getItem(key);
  } catch {
    return null;
  }
}
function safeSet(key: string, value: string): void {
  try {
    window.sessionStorage.setItem(key, value);
  } catch {
    // In-memory state của JourneyCore vẫn đủ cho phiên hiện tại.
  }
}
function safeRemove(key: string): void {
  try {
    window.sessionStorage.removeItem(key);
  } catch {
    // Bỏ qua — lần quét QR kế sẽ tạo lại.
  }
}

export const sessionPersistence = {
  loadContext(): GuestResolveResponse | null {
    const raw = safeGet(CONTEXT_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as GuestResolveResponse;
    } catch {
      return null;
    }
  },
  saveContext(ctx: GuestResolveResponse): void {
    safeSet(CONTEXT_KEY, JSON.stringify(ctx));
  },
  /** Ack đã lưu CHỈ hợp lệ nếu cùng visit hiện tại (INV4). */
  loadAckedVersion(visitId: string): number | null {
    const raw = safeGet(ACK_KEY);
    if (!raw) return null;
    try {
      const rec = JSON.parse(raw) as AckRecord;
      return rec.visitId === visitId ? rec.ackedVersion : null;
    } catch {
      return null;
    }
  },
  saveAckedVersion(visitId: string, ackedVersion: number): void {
    safeSet(ACK_KEY, JSON.stringify({ visitId, ackedVersion } satisfies AckRecord));
  },
  clear(): void {
    safeRemove(CONTEXT_KEY);
    safeRemove(ACK_KEY);
  },
};
