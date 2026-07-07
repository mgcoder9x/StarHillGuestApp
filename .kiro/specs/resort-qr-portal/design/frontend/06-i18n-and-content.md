# 06 — i18n & Content Rendering (UI text vs content, timezone, HTML an toàn, CJK)

> **File authoritative cho:** hai tầng ngôn ngữ ở FE, cấu trúc locale, render **content** từ API (isFallback, HTML đã sanitize), định dạng thời gian theo timezone resort, và lưu ý CJK. Đồng bộ backend `18` (localization) + `03` (guest flows).
>
> Thuật toán **resolve ngôn ngữ** đã ở `03` §3 (không lặp lại); file này tập trung **cấu trúc + render + định dạng** — nơi FE hay sai.

## 1. Hai tầng ngôn ngữ (phân định dứt khoát)

| Tầng | Nguồn | Ai quản | Ví dụ |
|---|---|---|---|
| **UI text** | vue-i18n JSON **trong app** | dev FE | nhãn nút "Tiếp tục", "Gửi", tiêu đề màn |
| **Content** | **API backend** theo `LanguageCode` (có `isFallback`) | Staff nhập (DB) | nội quy, câu hỏi/đáp FAQ |

- **Không trộn**: UI text không gọi API; content không nằm trong JSON app. (Req 2.4)
- guest-web: UI text **en/vi/ko/zh**; admin-web: UI text **vi** (kiến trúc cho thêm sau).

## 2. Cấu trúc locale (UI text)

```text
apps/guest-web/src/locales/
  en.json  vi.json  ko.json  zh.json     # cùng bộ key, khác giá trị
apps/admin-web/src/locales/
  vi.json                                # (+ thêm ngôn ngữ sau nếu cần)
```

- **Key namespacing theo màn/chức năng** (tránh key phẳng khó bảo trì):
  ```jsonc
  { "common": { "continue": "Continue", "send": "Send" },
    "ruleGate": { "progress": "{current}/{total}", "agree": "I have read and agree" },
    "errors": { "session_expired": "Session expired, please rescan the QR." } }
  ```
- **Tất cả locale phải cùng tập key** (thiếu key → cảnh báo build; dùng `vue-i18n` `missing` handler + lint). Interpolation `{current}`, pluralization qua vue-i18n.
- **`errors.*` map theo `ErrorCode`** (catalog `14`) → hiển thị thông điệp UI theo `code` ổn định, không đoán message backend.

## 3. Render CONTENT từ API (an toàn) — điểm dễ tạo lỗ XSS

- Content nội quy/FAQ là **HTML đã được backend sanitize** theo allowlist (`03` §9 backend / `18`). FE **chỉ** dùng `v-html` cho **nội dung đã sanitize từ API**; **TUYỆT ĐỐI KHÔNG** `v-html` cho input người dùng/guest hay chuỗi chưa qua sanitize.
- Defense-in-depth: dù backend đã sanitize, FE vẫn: không bind `v-html` với dữ liệu ngoài luồng content-API; CSP `script-src 'self'` (`20` §5) chặn inline script nếu lọt.
- **`isFallback`**: khi content trả về là bản dịch mặc định (thiếu ngôn ngữ yêu cầu), hiển thị **badge nhẹ** ("Hiển thị bản tiếng Anh") để khách hiểu — không im lặng, không chặn (Req 2.5).
- Content **missing** (thiếu cả ngôn ngữ yêu cầu lẫn default — `18` §4): FE ẩn mục/placeholder, không vỡ trang (Req 9.6).

## 4. Thời gian: API trả UTC → hiển thị theo timezone resort (điểm hay sai)

- **Bản chất:** backend lưu & trả **UTC** (`timestamptz`, `04` §9). Nếu FE hiển thị thẳng chuỗi UTC → khách/lễ tân thấy sai giờ.
- **Quy tắc:** FE format thời gian sang **timezone của resort** (`Resort.Timezone` từ `/resolve` response) bằng `Intl.DateTimeFormat(locale, { timeZone: resort.timezone })`. Một helper `formatDateTime(utcIso, locale, tz)` dùng chung (`packages/ui-kit` hoặc composable).
- Locale ảnh hưởng **định dạng** (thứ tự ngày/tháng, tên tháng) → dùng `locale` hiện tại; `timeZone` luôn theo resort (không theo trình duyệt — tránh lệch khi khách ở múi giờ khác).

## 5. CJK & hiển thị (ko/zh)

- **Font fallback CJK:** đảm bảo font stack có glyph cho Hàn/Trung (ví dụ system font `-apple-system, "Noto Sans", "Noto Sans KR", "Noto Sans SC", sans-serif`) để ko/zh không bị "tofu" (ô vuông). Guest mobile-first → ưu tiên system font (không tải web-font nặng).
- **RTL:** en/vi/ko/zh đều **LTR** → **không cần** RTL ở giai đoạn này. (Nếu sau thêm ar/he mới cần `dir="rtl"` — ghi nhận, không làm bây giờ.)
- Kiểm tra xuống dòng/độ dài: tiếng Đức/Hàn có thể dài hơn → layout co giãn, tránh cắt chữ (mobile-first).

## 6. Chuyển ngôn ngữ (switch) & lưu lựa chọn

- `LangSwitcher` đổi `locale` vue-i18n **và** kéo lại content theo lang mới; lưu `localStorage['guestLang']` (Req 2.3). Thuật toán chọn ban đầu ở `03` §3 (khớp backend `18` §2 primary-subtag).
- Đổi ngôn ngữ **giữ tiến độ RuleGate theo section id** (không reset) — docs.

## 7. Truy vết
- **Validates: Requirements 2.1–2.6, 9.5–9.6, 13**
- Align: backend `18` (fallback/normalize), `03` (flows/error map), `04` §9 (UTC), `20` §5 (CSP).
