# Đánh giá kiến trúc vision-platform — chuẩn mission-critical (2026-07-26)

> Phạm vi: toàn bộ `vision-platform/` (69 file, ~6.000 LOC src). Phương pháp: 3 review độc lập
> theo mảng (kernel/IPC, runtime/application, adapters/tests/packaging), đối chiếu chéo với
> baseline test thật: **444 passed, 5 skipped, 1 flaky (Hypothesis PBT)**, `lint-imports` 5/5 kept.

## 1. Kết luận tổng quát

**Nền tảng kiến trúc ĐÚNG HƯỚNG — không cần đập đi xây lại.** Hexagonal 4 layer với import-linter
ép ranh giới (0 broken), DTO kernel thuần, backpressure đúng, bộ test hardening cross-process
(kill thật, lease, quarantine, PBT) thuộc loại hiếm gặp ở codebase Python. Quyết định đúng là
**giữ khung — vá lõi — nâng chuẩn**, đúng tinh thần chống-rebuild trong `.kiro/specs/scale-architecture`.

Tuy nhiên, để đạt chuẩn mission-critical (độ tin cậy cấp cao, latency xác định, chịu lỗi từng phần),
có **3 nhóm khoảng cách** phải xử lý theo thứ tự:

1. **Lỗi đúng-sai (correctness) trong lõi SHM/supervisor** — hệ có thể wedge vĩnh viễn, đọc frame
   rách, orphan process. Đây là P0, sửa ngay (xem §2).
2. **Trần hiệu năng cấu trúc** — executor tuyến tính đồng bộ, copy frame trên hot path, NMS thuần
   Python, logging đồng bộ. P1 (xem §3).
3. **Khoảng trống quy trình** — không CI, không lock deps, không soak/SLO test, codec không
   validate. P1–P2 (xem §4).

## 2. Lỗi nghiêm trọng đã xác nhận (P0 — đã lên kế hoạch sửa trong đợt này)

### Lõi SHM ring (`runtime/ipc/shm_frame_ring.py`, `ring_control_plane.py`)

| # | Lỗi | Hệ quả |
|---|-----|--------|
| CD-1 | Reader chết khi đang pin (state=READING) không bao giờ bị reap từ phía writer (:601) | Mỗi reader chết rò 1 slot vĩnh viễn → ring wedge, không có sự kiện quarantine nào |
| CD-2 | Slot READY không bao giờ được thu hồi khi consumer chết/tụt | Consumer restart > n_slots frame-time → `write()` trả None mãi mãi, capture tê liệt |
| CD-3 | Check epoch (lockless, :664) và pin (:674) không nguyên tử qua `reset_for_reuse`; generation reset về 0 khi tái dùng ring | Ref cũ pass cả 2 guard → đọc frame epoch sai như dữ liệu hợp lệ (ABA kép) |
| CD-4 | `reset_for_reuse` ghi header KHÔNG cầm lock khi acquire timeout (:525-534) | Torn header đè lên reader đang copy → frame rách được giao như hợp lệ |
| CD-5 | `read_current` không có seqlock — cặp (epoch, ring_name) có thể rách | Bind epoch N vào ring N+1, hoặc attach fail ngẫu nhiên |
| CD-6 | Lock chết (holder bị kill) hồi sinh thành slot FREE sau reset; FREE không quarantine được (:421) | Mỗi lượt quét trả 100 ms latency vĩnh viễn, không escalation |
| CD-7 | Writer cũ sau switchover vẫn ghi được (không check epoch/registry mỗi write) | Dual-writer — đúng invariant F-4 mà module tự cảnh báo |
| CD-9 | Giả định "store 4B aligned là atomic + ordered" chỉ đúng x86-TSO, CPython không cam kết | Trên ARM (Jetson/Orin — target rất khả dĩ) mọi protocol "ghi X trước, state sau" đều gãy |

### Supervisor / application

| # | Lỗi | Hệ quả |
|---|-----|--------|
| S-1 | Monitor loop không try/finally (:141-195) — exception khi respawn bỏ qua cascade shutdown; worker daemon bị kill cứng | Orphan process giữ SHM/port; finally trong worker không chạy |
| S-2 | exitcode==0 bị coi là crash (:159-161) | Worker xong việc hợp lệ bị restart lặp rồi bị "give up" ở mức ERROR |
| S-3 | Liveness dùng wall-clock `time.time()`; grace startup = heartbeat_timeout (2s) | NTP step giết toàn bộ worker cùng lúc; worker nặng (import torch) bị giết trước beat đầu → outage toàn phần |
| S-4 | `WriterEpochCoordinator._maybe_switch` mutate `_ring/_epoch` TRƯỚC khi tạo writer (:70-74) | Factory raise → ghi ring cũ dưới danh nghĩa epoch mới — vi phạm chính invariant lớp này tồn tại để giữ |
| S-5 | `InlineInferenceClient` coi stale-read là retryable=False, `InferenceServer` coi là True | Cùng 1 lỗi, dev và production hành xử ngược nhau |
| S-6 | Source lỗi liên tục → hot-spin 100% CPU không backoff (`pipeline_runner.py:83-85`) | RTSP đứt → 1 core cháy vô hạn, supervisor vẫn thấy worker "khỏe" |
| S-7 | Histogram metrics không giới hạn (`observability.py:120`) | ~10M float/ngày/metric ở 30fps → OOM khi chạy dài ngày |

### Adapters

| # | Lỗi | Hệ quả |
|---|-----|--------|
| A-1 | RTSP: timeout set SAU khi `cv2.VideoCapture` đã block mở (vô hiệu) | Camera mất mạng → hot path treo hàng chục giây/vô hạn |
| A-2 | `mask_rtsp` split ở `@` ĐẦU — password chứa `@` lộ mảnh vào log/source_id | Rò credential |
| A-3 | ZMQ client `_io_loop` không try/except — 1 reply hỏng giết thread IO im lặng | Một gói tin lỗi = mất inference vĩnh viễn, không tín hiệu |
| A-4 | `torch.load` bị monkey-patch toàn cục `weights_only=False`, không revert | Mở bề mặt RCE unpickle cho MỌI lần torch.load của cả process |
| A-5 | Đường ONNX không convert BGR→RGB | Độ chính xác detect suy giảm im lặng với weight YOLO thật (đúng config mà deploy đang ship) |
| A-6 | Codec wire không validate input, không version field | Payload đúng-msgpack-sai-kiểu đẩy garbage có kiểu vào đường đọc SHM; không thể rolling-upgrade |

## 3. Trần hiệu năng cấu trúc (P1 — thiết kế, làm sau đợt vá P0)

1. **Executor tuyến tính đồng bộ** (`SyncLinearExecutor`): capture → infer → sink không bao giờ
   overlap; detector 30ms nghĩa là trần ~30fps/camera và GPU idle lúc đọc/copy/sink.
   → Thiết kế **PipelinedExecutor**: bounded queue giữa stage-group (tận dụng `BoundedQueue` +
   4 policy backpressure sẵn có), capture ∥ inference ∥ sink. Giữ `SyncLinearExecutor` cho test/CLI.
2. **Copy frame toàn phần mỗi read** (`shm_frame_ring.py:699` + `pipeline_runner.py:92`): 1080p
   ≈ 186 MB/s memcpy + GC churn. → API đọc **zero-copy pinned view** (context manager giữ pin đến
   `__exit__`), copy chỉ là fallback khi vượt lease.
3. **NMS thuần Python O(n²)** trên dataclass — chục ms/frame với 10³ box, có thể vượt cả thời gian
   inference. → Vector hóa numpy (giữ nguyên API).
4. **Logging JSON đồng bộ ra stdout trên hot path** — stdout chậm là frame loop đứng. → Bounded
   non-blocking queue handler + rotation (K-018 đã ghi nhận, giờ là bắt buộc).
5. **Lock timeout 100ms trên hot path** (`LOCK_ACQUIRE_TIMEOUT_S`) — worst case write() = n_slots × 100ms.
   → Hot path acquire ≤2ms, probe 100ms chỉ ở đường maintenance/quarantine.
6. **Metrics**: `_key()` sort+format mỗi call + 1 lock toàn cục. → Bound-instrument pattern
   (resolve key 1 lần lúc tạo), lock per-metric.
7. **2·n_slots+1 segment SHM mỗi ring** (99 mapping với 16 slot × 3 ring). → Gộp 1 meta segment
   + 1 data segment per ring (stride layout) — attach 1 lần, ít handle.

## 4. Khoảng trống quy trình chuẩn production (P1–P2)

- **CI = 0**: `.github/` không có workflow nào. Cần ngay: pytest (+ `pytest-timeout`), `lint-imports`,
  ruff + mypy, matrix **Windows + Linux** — vì toàn bộ hardening suite hiện `skipif != win32`
  trong khi deploy target là Linux/Docker (mâu thuẫn nặng nhất về QA).
- **Deps**: lower-bound-only, không lockfile → build không tái lập. Cần constraints/lock + extras
  tách bạch (`onnx`, `torch`, `rtsp`, `zmq`).
- **Test gap chuẩn defense-grade** (theo thứ tự): fuzz codec wire (decode garbage), client sống sót
  reply hỏng, soak 30–60 phút + assert không rò memory/FD, latency SLO gate p99 từ benchmark,
  corrupt-SHM fuzz (lật byte header → reader phải fail-closed), server kill giữa chừng + reconnect,
  clock-skew liveness, POSIX run toàn bộ hardening suite.
- **Bảo mật**: secrets không được vào log (A-2), scrub data segment khi tái dùng ring (frame cũ
  tồn lưu trong SHM), non-root + HEALTHCHECK trong Dockerfile, xóa monkey-patch torch (A-4).
- **Config**: knob vận hành nhạy nhất (lease, lock timeout, pool size, TTL) đang hardcode trong
  module — phải vào `AppConfig` (triết lý declarative config của chính dự án).

## 5. Kiến trúc đích (giữ khung hiện tại, mở rộng theo spec scale-architecture)

```
CONTROL PLANE                    DATA PLANE (node = base hiện tại, đã vá P0)          OBSERVABILITY
config/scheduler/registry  ──▶   Ingest (1 cam = 1 writer/ring, NVDEC sub-stream)     metrics push
  budget + shed policy            → motion-gate (CPU, rẻ) → SHM ring (P0-hardened)     (Prometheus/OTel)
  ◀── stats/heartbeat             → batch-mux inference (GPU pool)                     non-blocking log
                                  → stateful analytics (camera-affinity)               latency histograms
                                  → ISink (event/DB/queue)
```

Nguyên tắc giữ nguyên từ spec: capacity model đo được (C_inf/C_dec/VRAM), shed có kiểm soát và
quan sát được, 1 cam = 1 writer, camera-affinity cho analytics có trạng thái. Bổ sung từ review:

- **Liveness 2 tầng**: heartbeat counter (clock-step immune) + progress counter (frames/interval)
  trong ctrl segment — bắt được cả hang, không chỉ crash.
- **Fail-closed SHM**: CRC32C per-frame ở commit, verify ở read; geometry validate khi attach;
  epoch ghi trong slot header — mọi đường mơ hồ trả về trạng thái tường minh (`ReadResult`),
  không phải `None` 6 nghĩa.
- **Ràng buộc nền tảng tường minh**: assert x86-64 khi import lớp SHM (CD-9) cho đến khi có
  atomics thật; ARM là mục tiêu riêng có thiết kế riêng (fence/atomic extension).
- **Orphan prevention**: Windows Job Object (`KILL_ON_JOB_CLOSE`) / Linux `PR_SET_PDEATHSIG`.

## 6. Lộ trình

| Đợt | Nội dung | Trạng thái |
|-----|----------|-----------|
| **P0 – vá lõi** | CD-1..7, S-1..7, A-1..6 + regression test từng lỗi | **Đang thực hiện (đợt này)** |
| **P1a – quy trình** | CI Windows+Linux, ruff+mypy, lock deps, pytest-timeout | Kế tiếp |
| **P1b – hiệu năng** | PipelinedExecutor, zero-copy read, non-blocking logging, lock ≤2ms, NMS numpy (đã kéo vào P0) | Sau P1a |
| **P2 – scale** | Batch-mux GPU, motion-gate, budget scheduler + shed, metric push — theo spec scale-architecture | Theo spec |
| **P2+ – nền tảng** | POSIX/ARM verification, CRC32C, Job Object/PDEATHSIG, soak + SLO gate | Theo spec |

## 7. Số liệu kiểm chứng

- Baseline trước sửa: `pytest` → 444 passed, 5 skipped, 1 flaky (`test_config_pbt::test_roundtrip_reflects_structure`
  — không tái hiện với cùng seed; cần điều tra riêng, khả năng phụ thuộc thứ tự sinh dữ liệu).
- `lint-imports`: 5 contracts kept, 0 broken.
- Môi trường verify: Windows 10, Python 3.14.6 (README nói 3.13 — đã chạy được trên 3.14).
