# AI Notes — Nhật ký kiểm chứng xuyên suốt

> **Mục đích:** đây là bộ nhớ xuyên suốt của AI cho spec `resort-qr-portal`, dùng để **kiểm chứng về sau**. Bất kỳ AI/người nào tiếp tục dự án PHẢI đọc thư mục này trước khi ra quyết định, và **cập nhật** khi có quyết định/thay đổi mới.

## Bốn việc thư mục này ghi lại (theo yêu cầu user)

| File | Việc | ID prefix |
|------|------|-----------|
| `01-autonomous-decisions.md` | Quyết định AI tự ra mà spec/docs KHÔNG nói | `DEC-` |
| `02-deviations-from-spec.md` | Chỗ AI phải ĐỔI so với yêu cầu/docs ban đầu | `DEV-` |
| `03-tradeoffs.md` | Các trade-off AI đã cân nhắc | `TRD-` |
| `04-things-to-know.md` | Bất kỳ điều gì cần biết (giả định, rủi ro, việc CHƯA xác minh) | `TK-` |

## Định dạng (tối ưu cho AI đọc lại)

Mỗi mục là một khối có ID ổn định + các trường cố định, để tra cứu/đối soát nhanh:

```
### <ID>: <tiêu đề ngắn>
- Status: Proposed | Accepted | Superseded | NeedsUserInput | Verified
- Date: YYYY-MM-DD
- Context: vì sao phát sinh
- Decision/Change: nội dung
- Rationale: lý do (sửa tận gốc, không sửa ngọn)
- Evidence/Refs: dẫn chứng (file code, docs, property) — nếu là fact
- Impact: ảnh hưởng tới đâu
- Reversible?: có/không + cách đảo ngược
```

## Quy tắc vàng (theo yêu cầu user)

1. **Không bịa, không suy đoán.** Điều gì chưa kiểm chứng → ghi vào `04-things-to-know.md` với `Status: NeedsUserInput` hoặc `Unverified`, KHÔNG viết như fact.
2. **Sửa tận gốc.** Mỗi thay đổi phải nêu "bản chất vấn đề" trước "giải pháp".
3. **Thiết kế rõ ràng → valid nhiều lần → mới triển khai.** Không viết code khi thiết kế chưa được user duyệt.
4. **Truy vết được.** Khẳng định về code tham chiếu phải có Evidence trỏ file cụ thể.

## Trạng thái tổng quát hiện tại

- Giai đoạn: **THIẾT KẾ** (Design-First, Phase 1). Chưa viết bất kỳ code triển khai nào.
- Đã kiểm chứng 8 khẳng định về `Reference/Backend` (xem `../design/backend/09-reference-reconciliation.md`).
- Đang chờ user: duyệt thiết kế backend + cung cấp input frontend.
