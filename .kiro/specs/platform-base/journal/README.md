# platform-base — Decision Journal (nhật ký kiểm chứng xuyên suốt)

> **Mục đích:** ghi lại **có thể kiểm chứng** mọi quyết định/độ lệch/trade-off/lưu ý phát sinh khi xây `platform-base`, để bất kỳ AI hoặc người nào sau này cũng **kiểm chứng lại được** vì sao hệ thống thành ra như vậy. Đây là bộ nhớ dài hạn của spec.
>
> **Nguyên tắc tối thượng (bắt buộc mọi lần cập nhật):**
> 1. **KHÔNG bịa, KHÔNG suy đoán.** Mỗi bản ghi phải có trường **Provenance/Evidence** trỏ về nguồn thật (file + mục, hoặc thao tác đã verify). Nếu chưa kiểm chứng được → đánh dấu `⚠️ UNVERIFIED` chứ không viết như thật.
> 2. **Sửa tận gốc, không sửa ngọn.** Khi ghi một fix, ghi **nguyên nhân gốc (root cause)**, không chỉ triệu chứng.
> 3. **Chỉ append/patch, không viết lại lịch sử.** Bản ghi cũ sai → đặt `Status: Superseded by <ID>` + tạo bản ghi mới. KHÔNG xoá dấu vết.
> 4. **Mọi thay đổi thiết kế phải đồng bộ** với `design.md` (nguồn sự thật) + truy vết `R#`/`CP#`/`F#`.

## Cấu trúc thư mục

| File | Mục (theo yêu cầu) | ID prefix |
|---|---|---|
| `01-decisions.md` | (1) Quyết định AI tự ra mà spec KHÔNG nói | `AD-###` (Autonomous Decision) |
| `02-deviations.md` | (2) Chỗ AI phải ĐỔI so với yêu cầu/blueprint ban đầu | `DV-###` (Deviation) |
| `03-tradeoffs.md` | (3) Trade-off AI phải cân nhắc | `TO-###` (Trade-Off) |
| `04-notes.md` | (4) Bất kỳ điều gì AI nên biết (giả định/cạm bẫy/trạng thái) | `N-###` (Note) |
| `05-anti-drift.md` | Cơ chế chống drift (guard test tự động) + bản đồ CP/AD → test + vòng lặp bắt buộc | — |

## Trạng thái nguồn sự thật (đọc trước khi tin bất cứ gì)

- **`design.md`** = nguồn thiết kế CHUẨN DUY NHẤT (HOW). Lệch với nó → sửa theo nó.
- **`requirements.md`** = CÁI GÌ + TẠI SAO (EARS R1–R34).
- **`tasks.md`** = kế hoạch + checklist (21 task).
- **`README.md`** (thư mục spec) = bản đồ định hướng.
- **`foundation/ARCHITECTURE-REVIEW.md`** (F1–F35) + **`foundation/FOUNDATION-BLUEPRINT.md`** (I1–I10) = nguồn *rationale* ĐÓNG BĂNG (không sửa).
- **`review.md`** = báo cáo critique của AI review (D1–D14, R1–R7, T1–T5) — ĐÓNG BĂNG, cố ý còn dùng tên cũ `BuildingBlocks` (bằng chứng lịch sử trước rename).

## Schema mỗi bản ghi (giữ nhất quán)

```
### <ID> — <Tiêu đề ngắn>
- Status: Proposed | Confirmed | Superseded by <ID> | Reverted
- Date: YYYY-MM-DD
- Decider: user | AI(review) | AI(Kiro) | AI(<tên/model nếu biết>)
- Provenance/Evidence: <file §, review D#, hoặc "verified in-session via <tool>">
- Context: <bối cảnh, vấn đề gốc>
- Decision/Change: <quyết định cụ thể>
- Rationale (verifiable): <lý do chính xác, gắn bằng chứng>
- Alternatives: <phương án khác + vì sao loại>
- Consequences: <hệ quả xuôi dòng, kể cả bất lợi>
- Reversibility: Low | Medium | High (+ chi phí đảo ngược)
- Traceability: R#, CP#, F#, design §
```

## Legend

- `[Confirmed]` = đã được user chốt hoặc đã hiện diện trong `design.md` (đã verify).
- `[Proposed]` = AI đề xuất, chưa chốt.
- `⚠️ UNVERIFIED` = chưa kiểm chứng được — cấm dùng làm căn cứ triển khai.

## Giao thức cập nhật (cho AI đời sau)

1. Trước khi thêm bản ghi: **đọc lại** file liên quan để xác nhận sự thật hiện tại (đừng tin trí nhớ).
2. Thêm ID kế tiếp (không tái dùng ID cũ).
3. Điền đủ mọi trường schema; thiếu bằng chứng → `⚠️ UNVERIFIED`.
4. Nếu quyết định mới lật quyết định cũ → set bản cũ `Superseded by <ID>` và giải thích.
5. Nếu thay đổi chạm code/spec → cập nhật `design.md`/`requirements.md`/`tasks.md` cho khớp, rồi chạy `getDiagnostics` xác nhận 0 lỗi định dạng.
6. **Cổng tự động (L4/AD-030):** sau khi thêm/sửa bản ghi, chạy `dotnet test Platform.slnx` — `JournalConsistencyTests` (trong `Bedrock.ArchitectureTests`) enforce: ID mỗi loại **duy nhất + liên tục 1..N**; **mọi `AD-###` phải có mặt trong bảng guard `05-anti-drift.md`** (KEYSTONE tự động); ref `AD/DV/TO/N-###` không dangling; AD & DV đủ `Status:`+`Provenance/Evidence:`; `CP##` trong 1..15. Đỏ = journal đã lệch → sửa trước khi tiếp. Đây là lá chắn chống drift MẠNH NHẤT (không dựa trí nhớ).
7. Ghi ngày theo ngày hệ thống.

> **Ngày khởi tạo journal:** 2026-07-07. Toàn bộ bản ghi khởi tạo dưới đây được rút từ `design.md`, `review.md`, và các thao tác đã verify trong phiên làm việc tạo ra chúng.
