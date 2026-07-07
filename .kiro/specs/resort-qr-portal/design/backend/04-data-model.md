# 04 — Nền Data Model

> **File authoritative cho:** quy ước entity, translation pattern, entity nền + module, DB partial unique index, seed.

## 1. Quy ước chung (áp cho mọi entity nghiệp vụ)

- Khóa chính kiểu `uuid`, sinh client-side bằng `Guid.CreateVersion7()` (lý do + rủi ro kiềm chế: xem `02-core-abstractions.md` §1). Bảng/cột **snake_case** (Npgsql convention).
- Entity có audit → kế thừa `AuditableEntity` (tự set `CreatedAt/UpdatedAt/actor` + `RowVersion`).
- Xóa "mềm" cho dữ liệu có tham chiếu lịch sử (Room, ...) qua `ISoftDeletable`.
- Nội dung do nội bộ nhập (rich text) **luôn qua `IHtmlSanitizer`** trước khi lưu.
- Bất biến quan trọng enforce ở **DB constraint** (partial unique index), không chỉ ở code.

## 2. Mẫu đa ngôn ngữ (Translation pattern) — nền dùng lại

Mọi nội dung đa ngôn ngữ theo cùng một khuôn: entity gốc + bảng `*Translation` (unique theo `(ParentId, LanguageCode)`), resolve có fallback + cờ `isFallback`.

```csharp
public interface ITranslation
{
    string LanguageCode { get; }
}

// Resolver dùng chung cho Rule/Faq/... — nền cho Property 5 & 14 của docs nghiệp vụ
public interface ITranslationResolver : IScopedService
{
    // Trả bản dịch theo lang; nếu thiếu → default language của resort + isFallback=true
    Translated<T> Resolve<T>(IEnumerable<T> translations, string requestedLang, string defaultLang)
        where T : ITranslation;
}

public sealed record Translated<T>(T Value, string ResolvedLanguage, bool IsFallback);
```

## 3. Entity nền (base hiện thực đầy đủ ngay)

Các entity **nền** mà mọi module khác dựa vào; base implement + migration ngay từ đầu:

```text
Resort            Id, Name, Timezone, LogoUrl, CreatedAt
ResortSettings    ResortId(PK/FK 1-1),
                  RequireRuleAckForFaq/Chat/Housekeeping (bool),
                  FaqEnabled/ChatEnabled/HousekeepingEnabled (bool),
                  PortalWindowMinutes (=30), VisitIdleExpiryHours (=24),
                  GuestWebBaseUrl, MaxMessageLength,
                  MessageRateLimitPerMinute, HousekeepingRateLimitPerHour,
                  RowVersion, UpdatedAt
ResortLanguage    Id, ResortId, Code, DisplayName, IsEnabled, IsDefault, SortOrder
                  -- partial unique: (ResortId) WHERE IsDefault → đúng 1 default (=en)
                  -- unique: (ResortId, Code)
AppUser           Id, ResortId, Email(unique), DisplayName, PasswordHash,
                  Role(Admin/Staff), IsActive, LastLoginAt?, CreatedAt, RowVersion
RefreshToken      Id, UserId, FamilyId, TokenHash(indexed),
                  ExpiresAt, CreatedAt, RevokedAt?, ReplacedByTokenId?, RevokedReason?
                  -- rotation + reuse detection: xem 12-identity-and-auth.md §3
Room              Id, ResortId, RoomNumber, Building, Floor,
                  Status(Active/Inactive/Maintenance), audit, IsDeleted, DeletedAt?, RowVersion
RoomQrToken       Id, RoomId, Token(**UNIQUE toàn cục**, indexed), TokenPreview,
                  Status(Active/Revoked), Version,
                  CreatedAt, CreatedByUserId, RevokedAt?, RevokedByUserId?, RevocationReason?
                  -- UNIQUE(Token): resolve theo token phải map đúng 1 dòng (kể cả token đã Revoked vẫn giữ, không trùng)
                  -- partial unique: (RoomId) WHERE Status='Active' → 1 token active/phòng
GuestSession      Id, SessionKeyHash(**UNIQUE**, indexed), PreferredLanguage, FirstSeenAt, LastSeenAt
                  -- Cookie chứa secret RAW (CSPRNG); DB lưu HASH (SessionKeyHash) — KHÔNG lưu raw (như RefreshToken).
                  -- Tra thiết bị: hash(cookie) → lookup SessionKeyHash (xem §7.1 ux_guestsession_key; 03 §4).
GuestVisit        Id, ResortId, RoomId, GuestSessionId,
                  Status(Active/Closed/Expired),
                  StartedAt, LastSeenAt, ExpiresAt, ClosedAt?, ClosedByUserId?
```

## 4. Entity module (migration TĂNG DẦN theo wave — đã chốt)

**Quyết định (TRD-002, đã chốt hướng incremental):** base **KHÔNG** dựng sẵn bảng cho module chưa có code. Mỗi module nghiệp vụ được thêm bằng **migration riêng theo wave** khi hiện thực module đó. Base chỉ tạo **khung thư mục + interface UseCase rỗng + khung controller** (extension point). Các entity dưới đây được thiết kế trước (để nhìn toàn cảnh) nhưng **chỉ tạo bảng khi tới wave tương ứng**:

```text
[Wave Rules]        RuleSet, RuleSection, RuleSectionTranslation,
                    RulePublication, RulePublicationSection, RulePublicationSectionTranslation,
                    RuleAcknowledgement
[Wave Faq]          FaqCategory, FaqCategoryTranslation, FaqItem, FaqItemTranslation
[Wave Messaging]    Conversation, Message
[Wave Housekeeping] HousekeepingTicket, HousekeepingEvent
[Wave Notes]        InternalNote
```

**Nguyên tắc migration (commercial):** mỗi migration additive, review được độc lập, áp tuần tự lên DB có dữ liệu mà không mất dữ liệu; ràng buộc unique/partial index của một module nằm trong chính migration của module đó (ví dụ `ux_pub_current`, `ux_conv_visit` thuộc wave Rules/Messaging).

> Lý do đổi hướng (fix gốc, không fix ngọn): migration là lịch sử phiên bản schema; tạo bảng "chết" cho module chưa code vi phạm YAGNI và tạo cam kết sớm. Xem `../../ai-notes/03-tradeoffs.md` TRD-002.

## 5. DB constraints (partial/filtered unique index)

Mỗi index được tạo trong migration của wave sở hữu bảng tương ứng (không dồn hết vào migration đầu):

```sql
-- [Migration nền]
-- 1 token Active/phòng
CREATE UNIQUE INDEX ux_qr_active ON room_qr_token(room_id) WHERE status = 'Active';
-- đúng 1 ngôn ngữ mặc định/resort
CREATE UNIQUE INDEX ux_lang_default ON resort_language(resort_id) WHERE is_default;

-- [Wave Rules]
CREATE UNIQUE INDEX ux_pub_current ON rule_publication(resort_id) WHERE is_current;
CREATE UNIQUE INDEX ux_ack ON rule_acknowledgement(guest_visit_id, rule_publication_id);
CREATE UNIQUE INDEX ux_tr_rule ON rule_section_translation(rule_section_id, language_code);

-- [Wave Faq]
CREATE UNIQUE INDEX ux_tr_faq  ON faq_item_translation(faq_item_id, language_code);

-- [Wave Messaging]
CREATE UNIQUE INDEX ux_conv_visit ON conversation(guest_visit_id);

-- [Wave Housekeeping]
CREATE UNIQUE INDEX ux_hk_open ON housekeeping_ticket(room_id) WHERE status IN ('Requested','InProgress');
```

> ⚠️ **Cần xác minh:** enum lưu dạng string ('Active'...) hay int ảnh hưởng điều kiện `WHERE status = 'Active'`. Base chọn lưu enum dạng **string** để partial index đọc được và dễ debug. Xem `../../ai-notes/01-autonomous-decisions.md` mục DEC-006.

## 6. Seed data nền (Req 12.4)

Migration + seeder tạo: 1 `Resort` (Star Hill), `ResortSettings` mặc định an toàn, `ResortLanguage` (`en` default, `vi`, `ko`, `zh`), 1 tài khoản `Admin`. Seeder **idempotent** (chạy lại không nhân đôi), kế thừa pattern `WebHostExtensions.SeedData` của reference nhưng gọn và an toàn hơn (kiểm tra tồn tại trước khi thêm).

> ⚠️ **Cần user cung cấp:** mật khẩu admin khởi tạo phải lấy từ cấu hình/biến môi trường, **không** hardcode. Xem `../../ai-notes/04-things-to-know.md` mục TK-004.

## 7. Ràng buộc toàn vẹn & index chi tiết (deep — bổ sung từ audit)

### 7.1 Unique bắt buộc cho tính đúng đắn (không chỉ hiệu năng)

```sql
-- Token resolve phải map đúng 1 dòng: UNIQUE toàn cục (mọi Status)
CREATE UNIQUE INDEX ux_qrtoken_token ON room_qr_token(token);
-- Cookie guest tra theo HASH của session key (không lưu raw): UNIQUE
CREATE UNIQUE INDEX ux_guestsession_key ON guest_session(session_key_hash);
-- Email đăng nhập: UNIQUE (single-resort → global; multi-resort tương lai → (resort_id,email))
CREATE UNIQUE INDEX ux_appuser_email ON app_user(email);
-- ĐÚNG MỘT GuestVisit Active cho mỗi (thiết bị, phòng) — enforce "nối lại 1 visit" (Req 10.2/10.4)
CREATE UNIQUE INDEX ux_visit_active ON guest_visit(guest_session_id, room_id) WHERE status = 'Active';
-- Số phòng không trùng trong resort (trừ phòng đã soft-delete) — Req 16.6
CREATE UNIQUE INDEX ux_room_number ON room(resort_id, room_number) WHERE is_deleted = false;
```

> **Vì sao `ux_visit_active` là ĐÚNG ĐẮN, không chỉ tiện:** resolve logic là "tìm visit Active của (session, room) → nếu chưa có thì tạo". Hai request đồng thời (khách mở 2 tab, thiết bị chưa có visit) đều đọc "chưa có" → **tạo 2 visit Active** → vi phạm bất biến "mỗi lượt lưu trú = một visit" (Req 10.2/10.4), làm hội thoại/ack phân mảnh. Partial unique index chặn ở DB. **Xử lý race ở use case:** khi `INSERT` visit đụng unique violation → nghĩa là request song song vừa tạo → **re-query và dùng lại** visit Active đó (không lỗi ra khách).
> Index này **cũng phục vụ truy vấn lookup** `(guest_session_id, room_id) WHERE Active` → thay thế `ix_visit_lookup` ở §7.2 (không cần index rời).

> **Vì sao (bản chất):** `ux_qr_active` (partial theo room) chỉ đảm bảo *1 token Active/phòng*, KHÔNG chặn hai phòng có cùng chuỗi token. Resolve `/r/{token}` tra theo `token` → nếu trùng sẽ sai/nhập nhằng. Do đó `token` phải **UNIQUE toàn cục**. Đây là ràng buộc **đúng đắn**, tách khỏi `ux_qr_active`.

### 7.2 Index hiệu năng cho hot-path (đọc nhiều)

```sql
-- Tra visit Active của (session, room) khi resolve: ĐÃ được ux_visit_active (§7.1) phục vụ → không tạo index rời.
-- VisitIdleSweeper quét visit hết hạn theo chu kỳ
CREATE INDEX ix_visit_sweep ON guest_visit(expires_at) WHERE status = 'Active';
-- RefreshToken tra theo hash khi refresh
CREATE INDEX ix_refresh_hash ON refresh_token(token_hash);
```

EF Core tự tạo index cho mọi cột FK; các index trên là **bổ sung có chủ đích** (filtered/partial) cho truy vấn nóng. Lưu ý: unique partial index cũng dùng được như index tra cứu, nên tránh tạo index trùng chức năng (ví dụ `ux_visit_active` đã bao phủ truy vấn lookup visit Active).

### 7.3 Hành vi xóa (delete behavior) — chống mất lịch sử

- FK mặc định **`ON DELETE RESTRICT`** (EF: `DeleteBehavior.Restrict`) cho mọi quan hệ tham chiếu lịch sử (RoomQrToken→Room, GuestVisit→Room/GuestSession, Conversation/HousekeepingTicket→GuestVisit...). **Không** dùng cascade delete cho dữ liệu vận hành/lịch sử.
- `Room` dùng **soft-delete** (`ISoftDeletable`): "xóa" phòng chỉ set `IsDeleted`; token/visit/lịch sử vẫn còn để tra cứu và không vỡ FK.
- Kết thúc `GuestVisit` (checkout/idle) là **chuyển trạng thái** (Closed/Expired) + đóng conversation/hủy ticket ở tầng ứng dụng — **không phải DELETE** (giữ lịch sử, Req 10.8).

### 7.4 CHECK constraint (defense-in-depth, tùy chọn nhưng khuyến nghị)

Ngoài validation ở tầng app (FluentValidation, Req 15.3), thêm CHECK ở DB cho bất biến số học quan trọng:

```sql
ALTER TABLE resort_settings ADD CONSTRAINT ck_portal_window CHECK (portal_window_minutes BETWEEN 1 AND 1440);
ALTER TABLE resort_settings ADD CONSTRAINT ck_idle_expiry   CHECK (visit_idle_expiry_hours BETWEEN 1 AND 168);
ALTER TABLE resort_settings ADD CONSTRAINT ck_msg_len       CHECK (max_message_length BETWEEN 1 AND 10000);
```

> Lý do: app validation có thể bị bỏ qua (script, migration data, bug); CHECK ở DB là lớp cuối cùng bảo vệ bất biến — đúng tinh thần "correctness-by-construction".

## 8. Phạm vi Optimistic Concurrency (rõ ràng entity nào có / không có)

`xmin` concurrency token qua `UseXminAsConcurrencyToken()` chỉ áp cho **entity nội dung ít cập nhật, cần chống ghi đè âm thầm**; **KHÔNG** áp cho entity cập nhật tần suất cao (sẽ gây `409` sai trên hot-path).

| Có concurrency token (`IConcurrencyAware`) | KHÔNG có (cập nhật thường xuyên / append-only) |
|---|---|
| `ResortSettings`, `Room`, `AppUser` | `GuestSession`, `GuestVisit` (LastSeenAt đổi mỗi request) |
| `RuleSection`/`RuleSectionTranslation` (wave Rules) | `Message`, `HousekeepingEvent` (append-only) |
| `FaqItem`/`FaqCategory`(+translation) (wave Faq) | `RefreshToken` (tạo/thu hồi, không sửa) |

> **Bản chất:** concurrency token để hai người **cùng sửa một nội dung** thì người sau nhận 409 (Property B10). `GuestVisit.LastSeenAt` bị ghi liên tục bởi chính guest → gắn token sẽ tạo xung đột giả. Vì vậy tách rõ hai nhóm.

## 9. Kiểu dữ liệu & quy ước cột (PostgreSQL/Npgsql)

- **Thời gian:** mọi mốc thời gian là **UTC**, sinh qua `IDateTimeProvider.UtcNow`, map sang **`timestamptz`** (`timestamp with time zone`).
  - ⚠️ **Cần confirm khi implement (chưa verify lại được ở bước thiết kế):** Npgsql (từ 6.0) xử lý `timestamptz` nghiêm ngặt — `DateTimeOffset` map `timestamptz` yêu cầu **offset = 0 (UTC)**, offset khác 0 sẽ ném lỗi lúc chạy. Vì ta luôn dùng UTC nên an toàn, nhưng phải kiểm chứng cấu hình. Xem `../../ai-notes/04-things-to-know.md` TK-025.
- **Enum:** lưu dạng **string** qua `HasConversion<string>()` (DEC-006), cột `text`/`varchar`. **Giá trị string phải ổn định** (đừng đổi tên: 'Active'/'Revoked'/'Requested'...) vì partial index predicate (`WHERE status = 'Active'`) và dữ liệu phụ thuộc chuỗi này. (Phương án khác: Npgsql native enum type — an toàn kiểu hơn nhưng migration phức tạp hơn; base chọn string cho đơn giản + debug dễ.)
- **`RowVersion` (uint):** map cột hệ thống `xmin` qua `UseXminAsConcurrencyToken()` — **không** tạo cột thật (xem `03` §3).
- **Chuỗi:** đặt `MaxLength` hợp lý cho mọi cột string (RoomNumber ≤ 20, Building ≤ 50, Email ≤ 256...) để có kiểu `varchar(n)` thay vì `text` không giới hạn.
