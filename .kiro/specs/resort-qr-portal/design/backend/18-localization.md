# 18 — Localization & Translation (fallback, chuẩn hóa mã ngôn ngữ, phát hiện thiếu dịch)

> **File authoritative cho:** thuật toán `ITranslationResolver`, chuẩn hóa mã ngôn ngữ, quy tắc fallback + cờ `isFallback`, phát hiện thiếu bản dịch (cho admin editor). Nền cho Req 2, 8.7, 9.
>
> **Phân định (nguồn nhầm lẫn):** có **hai loại text**:
> - **UI text** (nút, nhãn) — nằm ở **frontend** (vue-i18n JSON), KHÔNG qua backend.
> - **Content** (nội quy/FAQ do Staff nhập) — nằm ở **DB theo `LanguageCode`**, resolve ở backend bằng file này.
> File này chỉ nói về **content**. UI text: `../frontend/`.

## 1. Nguồn sự thật ngôn ngữ mặc định

- Ngôn ngữ mặc định = bản ghi `ResortLanguage` có `IsDefault = true` (partial unique — `04` §7/§3) — **nguồn sự thật DUY NHẤT**. **KHÔNG** có field `Resort.DefaultLanguage` (tránh hai nguồn lệch nhau). Mặc định seed = `en`.
- Danh sách ngôn ngữ "được bật" = `ResortLanguage WHERE IsEnabled = true`.

## 2. Chuẩn hóa & so khớp mã ngôn ngữ (matchSupported)

Client gửi mã theo BCP-47 (`ko-KR`, `zh-Hant`, `en-US`) qua `?lang=` hoặc `Accept-Language`. Backend chuẩn hóa để so với mã đã bật:

```pascal
ALGORITHM MatchSupported(requested, enabledCodes[], defaultCode) -> string
BEGIN
  IF requested rỗng THEN RETURN defaultCode
  norm ← lowercase(requested)
  IF norm ∈ enabledCodes THEN RETURN norm                 // khớp chính xác ("vi", "en")
  primary ← substringBefore(norm, '-')                    // "ko-kr" -> "ko"; "zh-hant" -> "zh"
  IF primary ∈ enabledCodes THEN RETURN primary           // khớp primary subtag
  RETURN defaultCode                                       // fallback (Req 2.2)
END
```

- **So khớp không phân biệt hoa/thường**; lấy **primary subtag** (phần trước dấu `-`).
- ⚠️ **Giới hạn có chủ đích (ghi rõ):** base so theo **primary subtag** → `zh-Hant` và `zh-Hans` cùng map về `zh`. Nếu resort cần phân biệt phồn/giản thể, phải bật **mã riêng** (`zh-Hant`, `zh-Hans`) làm `ResortLanguage.Code` và client gửi đúng — khi đó khớp chính xác hoạt động. Ghi TK-029.

## 3. `ITranslationResolver` — thuật toán fallback (một mục)

```pascal
ALGORITHM Resolve<T>(translations[], requestedLang, defaultLang) -> Translated<T>
PRE: translations = danh sách bản dịch của MỘT mục (mỗi phần tử có LanguageCode + nội dung)
BEGIN
  exact ← translations.firstOrDefault(t => t.LanguageCode == requestedLang AND NotEmpty(t))
  IF exact != null THEN RETURN Translated(exact, requestedLang, IsFallback=false)

  fb ← translations.firstOrDefault(t => t.LanguageCode == defaultLang AND NotEmpty(t))
  IF fb != null THEN RETURN Translated(fb, defaultLang, IsFallback=true)

  RETURN TranslatedMissing()          // không có cả requested lẫn default không rỗng
END
```

- **`NotEmpty(t)`** = nội dung chính (Title/Body/Question/Answer/Name tùy entity) sau trim **khác rỗng**. Bản dịch **tồn tại nhưng rỗng** được coi như **thiếu** → fallback (Req 9.3). Đây là điểm tinh vi: "có row" ≠ "có nội dung".
- `requestedLang` truyền vào đã qua `MatchSupported` (nên nếu client gửi mã không bật, `requestedLang` = `defaultLang` → nhánh exact chính là default, `IsFallback=false`; hợp lý vì đó là ngôn ngữ hiển thị hợp lệ).

## 4. Resolve một TẬP mục (Req 9.5) — độc lập từng mục, chống N+1

```pascal
ALGORITHM ResolveSet<T>(items[], requestedLang, defaultLang) -> ResolvedItem<T>[]
BEGIN
  // Nạp TẤT CẢ translation của các item trong 1 query (tránh N+1)
  allTr ← db load translations WHERE ParentId IN items.ids     // 1 round-trip
  FOR item IN items
     yield Resolve(allTr[item.id], requestedLang, defaultLang) // cờ IsFallback riêng từng mục
END
```

- **Cờ `isFallback` gắn riêng từng mục** (Req 9.5): trong cùng một trang, mục A có `ko` → không fallback, mục B thiếu `ko` → fallback `en` + cờ true.
- Mục thiếu cả requested lẫn default → **đánh dấu missing**, **không** làm lỗi cả request (Req 9.6): trả placeholder/ẩn tùy UI, các mục khác vẫn resolve.
- **Bất biến (Req 9.4):** nếu tồn tại tối thiểu bản dịch default không rỗng → resolver luôn trả giá trị **không rỗng** (khách không gặp nội dung trống). PBT-2 (`17` §4) kiểm bất biến này.

## 5. Phát hiện thiếu bản dịch cho admin editor (Req 8.7)

```pascal
ALGORITHM MissingLanguages(item, enabledCodes[]) -> string[]
BEGIN
  present ← { t.LanguageCode : t ∈ item.translations AND NotEmpty(t) }
  RETURN enabledCodes \ present        // các mã đã bật nhưng chưa có bản dịch không rỗng
END
```

- Admin editor hiển thị "thiếu: ko, zh" cho mỗi mục; filter "show missing / show inactive" (docs i18n). Giúp Staff biết cần dịch gì trước khi publish.

## 6. Áp dụng ở đâu (module dùng lại)

- **Rules (wave)**: resolve `RulePublicationSectionTranslation` theo lang khách đọc; guest đọc từ publication `IsCurrent`.
- **FAQ (wave)**: resolve `FaqItemTranslation`/`FaqCategoryTranslation`; chỉ item `IsActive`.
- Cùng một `ITranslationResolver` (Application port, impl Infrastructure) — không lặp logic fallback mỗi module.

## 7. Truy vết
- **Validates: Requirements 2.1, 2.2, 2.5, 8.7, 9.1–9.6**
- PBT-2 (fallback luôn trả giá trị không rỗng) — `17` §4.
