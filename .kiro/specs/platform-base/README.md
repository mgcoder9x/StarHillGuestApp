# platform-base — Bản đồ định hướng (đọc file này TRƯỚC)

> Trang này để **một AI/người mới** hiểu đủ trong ~2 phút: ta đang xây gì, file nào là chuẩn, đang ở đâu, kiểm tra thế nào. Đọc xong mới đi vào chi tiết.

## 1. Ta đang xây gì (một câu)

Một **base/platform backend domain-agnostic, modular-monolith-ready** (.NET 10, C#): *lõi biết "cần gì" (port), adapter biết "làm bằng gì" (tech), module biết "nghiệp vụ gì", Host biết "bật cái nào" → thêm công nghệ/nghiệp vụ = thêm adapter/module, KHÔNG sửa lõi.*

Cấu trúc 4 tầng: **Bedrock** (lõi) → **Adapters** (công nghệ) → **Modules** (nghiệp vụ) → **Host** (composition root). Code hiện hành nằm ở thư mục `platform/` (solution `Platform.slnx`) — xem §4.

> ✅ **Quyết định đã chốt:** (a) prefix lõi = **`Bedrock.*`** (một từ); (b) `Result` = **`sealed class`** + factory `Success()/Failure()` (design §4.4); (c) dead-letter = cột `dead_lettered_at` (không bảng DLQ riêng). Thư mục giải pháp giữ `platform/`.

## 2. File nào là gì

**Thứ tự ĐỌC cho người mới:** README (file này) → `requirements.md` (WHAT/WHY) → `design.md` (HOW) → `tasks.md` (WHEN/checklist).
**Thứ tự THẨM QUYỀN khi mâu thuẫn:** `design.md` thắng (nguồn sự thật duy nhất về thiết kế); hai file `foundation/*.md` đóng băng làm lịch sử/nguồn rationale.

| File | Vai trò | Ghi chú |
|---|---|---|
| `design.md` | **THIẾT KẾ CHÍNH — nguồn sự thật duy nhất (HOW)** | Contract C#, thuật toán (outbox claim/backoff, inbox idempotency, rotation, domain-event dispatch), dependency matrix, DI, Correctness Properties CP1–CP15. Chỗ tinh chỉnh khác blueprint có nhãn `[Tinh chỉnh so với Blueprint]` kèm lý do. |
| `requirements.md` | **CÁI GÌ + TẠI SAO (EARS, R1–R34)** | Tiêu chí chấp nhận, truy vết F#/I#. |
| `tasks.md` | **KẾ HOẠCH + CHECKLIST (21 task, 10 wave, DoD)** | Nơi theo dõi tiến độ; mỗi task có dòng "Nghiệm thu". |
| `../../foundation/FOUNDATION-BLUEPRINT.md` | Nguồn gốc (thiết kế đích ban đầu, invariants I1–I10) | **Đã hấp thụ vào `design.md`** — chỉ tham chiếu, KHÔNG sửa. |
| `../../foundation/ARCHITECTURE-REVIEW.md` | Nguồn gốc (chẩn đoán F1–F35 — lý do) | Tra "vì sao có quyết định này". KHÔNG sửa. |

## 3. Các phase triển khai (bám build order design §15)

| Phase | Mục tiêu | Task | Findings chính |
|---|---|---|---|
| **Giai đoạn 0** | Khởi tạo solution + lõi Domain/Application + architecture-test làm lưới | 1–4 | nền tảng |
| **P0** | Api mechanism thuần (không rò nghiệp vụ, không ref Infrastructure) + masker + health | 5 | F1–F4, F14, F15 |
| **P1** | Infrastructure/EF (UoW/Repo/DomainEvents) + Outbox/Inbox + refresh store + crypto/JWT + DI/startup validation + HTTP hardening | 6–11 | F5–F11, F16–F19, F22 |
| **P1.5** | Đặt "ổ cắm" mở rộng: ports contract-first + extension architecture + adapter mẫu RabbitMQ | 12–14 | F24–F29, F33 |
| **P2** | Pipeline behaviors đầy đủ + Modules/Host + versioning/telemetry/secrets + contract tests + DoD | 15–21 | F12/F13, F20/F21, F23, F30–F35 |

Thứ tự & song song hóa chi tiết: xem **Task Dependency Graph** + khối `json` waves trong `tasks.md`.

## 4. Đang ở đâu (trạng thái hiện tại — 2026-07-18)

- ✅ `platform/Platform.slnx` + đầy đủ `Bedrock.Domain/Application/Infrastructure/Api`, messaging contracts, RabbitMQ adapter, module Identity mẫu, Host và hệ test đã tồn tại.
- ✅ Base đã qua nhiều vòng hardening: dependency guards, keyed persistence, domain-event atomicity/restore, outbox/inbox + retry/DLQ, security defaults, ProblemDetails, migrations, Testcontainers, journal consistency và CI.
- ✅ `starhill/` dùng trực tiếp base duy nhất qua `$(PlatformSrc)`; không copy Bedrock sang product tree.
- ⚠️ Audit 2026-07-18 phát hiện launcher local có thể **xanh giả khi thiếu `dotnet`**. AD-104 đã đổi gate sang fail-closed, chặn `--no-build` khi build fail, bỏ phụ thuộc PyYAML bắt buộc và đưa validator vào CI.
- ✅ SDK portable .NET 10.0.301 đã restore/build/test local: `platform/tools/verify.ps1 all` + `journal` PASS, 0 warning/0 failure; test Docker-backed skip mềm vì daemon không khả dụng. GitHub CI vẫn là gate bắt buộc có Docker.
- ℹ️ `foundation/` là lịch sử/rationale đã hấp thụ; không phải base thứ hai và không được sửa để phát triển tính năng mới. Báo cáo hiện trạng: `current-audit-2026-07-18.md`.

## 5. Kiểm tra / nghiệm thu như thế nào (3 lớp)

1. **Hành vi** → Acceptance Criteria trong `requirements.md` (R1–R34, EARS).
2. **Đúng đắn kỹ thuật** → **Correctness Properties CP1–CP15** trong `design.md` (mỗi CP có `Validates: Requirements` + test tương ứng: architecture test hoặc integration test). Ví dụ: CP1 no-business-in-core, CP2 Api⊥Infrastructure, CP6 Outbox atomicity, CP7 rotation atomicity, CP8 Inbox idempotency, CP14 domain-event atomic, CP15 outbox exclusive claim.
3. **Tiến độ & hoàn thành** → checklist `tasks.md` (mỗi task có dòng "Nghiệm thu") + **Definition of Done** (task 21, design §16).

**Cổng chất lượng bất biến (mọi lát):** chạy `platform\scripts\vp.cmd all`. Launcher phải build trước, validate CI, rồi chỉ chạy test `--no-build` khi build thành công; thiếu toolchain/build lỗi phải FAIL, không được dùng artifact cũ. CI có Docker để Testcontainers Postgres/RabbitMQ chạy thật.

## 6. Lệnh nhanh

```powershell
# Từ repo root: build + validate-ci + test, fail-closed
platform\scripts\vp.cmd all

# Chỉ validate workflow/anti-artifact, không cần .NET SDK
python platform\tests\validate_ci.py
```
