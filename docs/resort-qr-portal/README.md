# Resort QR Portal — Tài liệu thiết kế

Bộ tài liệu đặc tả cho hệ thống **Resort QR Portal** (Star Hill Guest App): khách quét QR tại phòng → mở web hiển thị nội quy (bắt buộc đọc hết), FAQ do lễ tân tự soạn, nhắn tin với lễ tân, và tạo yêu cầu dọn phòng; kèm dashboard quản trị (tạo phòng, sinh QR, quản lý nội quy Draft/Publish, FAQ, inbox theo phòng, ghi chú, housekeeping).

Hệ thống chạy trong **WiFi nội bộ resort, không public internet**. Tài liệu này là Markdown thuần để nhiều agent/developer cùng đọc và triển khai (không phụ thuộc cơ chế spec của Kiro).

## Mục lục

| Tài liệu | Nội dung |
|---|---|
| [requirements.md](./requirements.md) | 14 nhóm yêu cầu (EARS) + Glossary. Nguồn chân lý về "phải làm gì". |
| [design.md](./design.md) | Kiến trúc, data model, REST API, SignalR, luồng nghiệp vụ, IA + wireframe, deployment/HTTPS, DB constraints, bảo mật, i18n, testing, 15 Correctness Properties. Nguồn chân lý về "làm như thế nào". |
| [tasks.md](./tasks.md) | Kế hoạch triển khai 19 nhóm task + Task Dependency Graph (waves). Checklist theo dõi tiến độ. |
| [test-plan.md](./test-plan.md) | Chiến lược test + ma trận Property→Test + kịch bản Given/When/Then. Definition of done cho test. |
| [deep-solution-design.md](./deep-solution-design.md) | Phân tích sâu + **Decision Log (mục 21–22)** ghi các quyết định đã chốt. Tài liệu tham chiếu. |

## Tóm tắt phạm vi

- **Guest Web** (Vue 3, mobile-first, trong WiFi nội bộ): resolve QR → nhận diện phòng, auto-detect ngôn ngữ, force-read nội quy, FAQ dạng cây, chat, yêu cầu dọn phòng. Cửa sổ thao tác ~30' (quá thì quét lại, nối lại lượt lưu trú).
- **Admin Dashboard** (Vue 3, đăng nhập): CRUD phòng + sinh QR (PNG/PDF), nội quy Draft→Publish có version, FAQ cha-con, inbox gom theo phòng (badge chưa đọc), housekeeping board, StaffScan (quét QR đánh dấu đã dọn), ghi chú nội bộ, thống kê.
- **Backend** (ASP.NET Core .NET 10 + EF Core + PostgreSQL): REST API + SignalR Hub, auth JWT + guest session cookie, QRCoder + QuestPDF.

## Ngăn xếp công nghệ

- Backend: **ASP.NET Core .NET 10 LTS**, EF Core, **PostgreSQL** (Npgsql), SignalR, QRCoder, QuestPDF, FluentValidation, HtmlSanitizer.
- Frontend: Vue 3 + TypeScript + Vite + Pinia + Vue Router + vue-i18n (2 SPA riêng); guest mặc định tiếng Anh, admin tiếng Việt; admin dùng PrimeVue/Element Plus; StaffScan dùng html5-qrcode.
- Database: **PostgreSQL**.
- Realtime: SignalR (fallback polling).
- Triển khai: **HTTPS với cert hợp lệ** + DNS nội bộ trỏ về server resort (bắt buộc để camera/QR hoạt động trên điện thoại; tránh self-signed).

## Mô hình phiên khách (chốt)

- **GuestSession** = thiết bị (cookie dài hạn ~30–90 ngày), chỉ định danh máy.
- **GuestVisit** = lượt lưu trú/phòng; kết thúc khi lễ tân đóng (checkout) hoặc idle 24h; quét lại cùng thiết bị+phòng thì nối lại visit đang Active.
- **Portal window** = cửa sổ thao tác ~30'; quá hạn phải quét QR lại để tiếp tục (vẫn nối lại visit).
- Hội thoại + acknowledgement gắn theo **GuestVisit** (khách chỉ thấy dữ liệu lượt của mình).

## Quyết định đã chốt (Decision Log — xem chi tiết mục 21–22 của deep-solution-design.md)

- [x] **Stack**: .NET 10 LTS + PostgreSQL; guest web tiếng Anh mặc định, admin dashboard tiếng Việt.
- [x] **Ngôn ngữ khởi điểm**: en (default), vi, ko, zh — nguồn mặc định = `ResortLanguage.IsDefault`.
- [x] **Phạm vi mạng**: WiFi nội bộ, không public internet (là yêu cầu hạ tầng, app không tự enforce).
- [x] **Mô hình phiên**: GuestSession (thiết bị) / GuestVisit (lượt lưu trú, idle 24h/đóng thủ công) / Portal window 30'.
- [x] **QR token**: chỉ Active/Revoked, rotate thủ công — **KHÔNG auto-expire** mã đã in/dán.
- [x] **Nội quy**: Draft → Publish có version; lễ tân/admin soạn trên giao diện; acknowledge do server xác thực version.
- [x] **FAQ**: lễ tân tự soạn, dạng cây cha-con.
- [x] **Tin nhắn**: gom theo phòng, badge chưa đọc; hội thoại theo GuestVisit; inbox phân biệt lượt hiện tại vs lịch sử.
- [x] **Housekeeping**: khách/nhân viên tạo ticket; hoàn tất bằng chọn phòng+xác nhận (chính) hoặc quét QR (lối tắt); ghi `HousekeepingEvent`.
- [x] **Quyền**: CRUD phòng/QR/revoke/users/settings = Admin-only; Staff xem phòng + xử lý vận hành/nội dung.
- [x] **Pháp lý**: acknowledgement là bản ghi vận hành, không phải chứng cứ pháp lý.
- [x] **Vận hành**: không backup tự động; log có cấu trúc + health check.

## Quyết định còn mở (chốt trước Task 1)

- [ ] Deploy cùng domain hay tách subdomain (MVP nên cùng origin, ví dụ `portal.starhill.local`).
- [ ] Nguồn cert HTTPS: cert nội bộ (internal CA) hay cert công khai qua DNS nội bộ.

## Quy trình làm việc cho agent

1. Đọc `requirements.md` để hiểu mục tiêu và tiêu chí chấp nhận.
2. Đọc `design.md` để nắm kiến trúc, hợp đồng API và các bất biến (Correctness Properties).
3. Xem `deep-solution-design.md` mục 21–22 (Decision Log) để hiểu lý do các quyết định.
4. Thực thi theo `tasks.md` tuần tự hoặc theo `Task Dependency Graph` (các wave chạy song song được).
5. Đánh dấu `[x]` task đã xong trong `tasks.md` để đồng bộ tiến độ giữa các agent.
6. Khi thay đổi phạm vi: cập nhật `requirements.md`/`design.md` trước, rồi mới điều chỉnh `tasks.md`.

## Quy ước tham chiếu

- Mỗi task trong `tasks.md` gắn `_Requirements: X.Y_` trỏ tới acceptance criteria trong `requirements.md`.
- Mỗi Correctness Property trong `design.md` gắn `**Validates: Requirements X.Y**` để truy vết kiểm thử.
