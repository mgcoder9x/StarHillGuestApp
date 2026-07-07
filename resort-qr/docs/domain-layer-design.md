# ResortQr.Domain — Thiết kế tầng Domain (entity nền §3) — design-first

> **Trạng thái:** THIẾT KẾ để triển khai ngay (POCO thuần, KHÔNG cần DB). Bám authoritative: `04-data-model` §3/§8/§9, `02-core-abstractions` §1, `12-identity-and-auth`, `13-guest-access-flows`, `16-rooms-and-qr`, `24-tenancy-model`, `15-configuration`.
> **Phạm vi bước này:** CHỈ entity **nền** (§3) — KHÔNG làm entity module (Rules/Faq/Messaging/Housekeeping) vì chúng theo wave (TRD-002). KHÔNG làm `RefreshToken` (base đã sở hữu qua `RefreshTokenRecord`).

## 0. Nguyên tắc (fix gốc, không ngọn)

- **Domain thuần:** entity là POCO chỉ phụ thuộc `ResortQr.SharedKernel` (Entity/AuditableEntity + interface audit/soft-delete/concurrency). **KHÔNG** EF, **KHÔNG** DataAnnotations, **KHÔNG** ASP.NET. Ràng buộc kỹ thuật (MaxLength, index, enum→string, xmin) đặt ở EF config (Infrastructure) + validator (Application) — Domain không biết persistence.
- **Anemic-có-kiểm-soát:** theo spec (`02`/`13`/`16`) thuật toán nghiệp vụ nằm ở **use case** (Application) thao tác entity qua repository; entity là data-holder ĐÓNG GÓI (immutable field dùng `required`+`init`; state đổi được dùng `set`). Bất biến enforce ở **DB constraint** (partial unique index — `04` §5/§7) + **validator** + **use case**. KHÔNG nhét logic đa-bảng vào entity.
- **Tham chiếu bằng Id (reference-by-id):** FK là **Guid scalar** (`ResortId`, `RoomId`, `GuestSessionId`...), **CHƯA** thêm navigation property. Lý do: giữ Domain sạch/khỏi lazy-loading + tránh mô hình EF sớm + tránh analyzer collection (CA2227/CA1002). Navigation thêm sau **khi một use case/query thật cần** (cục bộ, có kiểm chứng).

## 1. Ánh xạ base-class (validate theo `04` §8)

`04` §8 liệt kê entity **CÓ** concurrency token (`xmin`/`IConcurrencyAware`): **ResortSettings, Room, AppUser** (+ RuleSection/FaqItem ở wave sau). Entity **KHÔNG** có: GuestSession, GuestVisit, Message, RefreshToken.

Base có: `Entity` (Id) và `AuditableEntity : Entity, IAuditable, IConcurrencyAware` (audit + xmin). **Phát hiện validate:** tập entity cần concurrency (§8) = {ResortSettings, Room, AppUser} **trùng khớp** tập entity cũng cần audit → `AuditableEntity` (gộp audit+concurrency) vừa vặn cho cả 3, KHÔNG cần tách base. Các entity còn lại dùng `Entity`.

| Entity | Base | Interface thêm | Concurrency (§8) | Ghi chú |
|---|---|---|---|---|
| `Resort` | `Entity` | — | Không | `CreatedAt` set khi seed (tạo 1 lần); không cần convention audit |
| `ResortSettings` | `AuditableEntity` | — | **Có** | 1-1 với Resort (xem D5); `UpdatedAt`+`RowVersion` từ base |
| `ResortLanguage` | `Entity` | — | Không | bảng cấu hình ngôn ngữ; không audit/concurrency |
| `AppUser` | `AuditableEntity` | — | **Có** | +`LastLoginAt`; `CreatedAt`/`RowVersion` từ base |
| `Room` | `AuditableEntity` | `ISoftDeletable` | **Có** | audit + xóa mềm + xmin |
| `RoomQrToken` | `Entity` | — | Không | field lifecycle riêng (Created/Revoked) set tường minh ở use case (`16`) |
| `GuestSession` | `Entity` | — | Không (§8) | FirstSeen/LastSeen |
| `GuestVisit` | `Entity` | — | Không (§8: update tần suất cao → tránh 409 giả) | Status/LastSeen/ExpiresAt |

## 2. Enum (lưu dạng string — DEC-006; giá trị ỔN ĐỊNH, không đổi tên)

- `UserRole { Admin, Staff }` (`12` §2 role).
- `RoomStatus { Active, Inactive, Maintenance }` (`16` §2).
- `RoomQrTokenStatus { Active, Revoked }` (`16`).
- `GuestVisitStatus { Active, Closed, Expired }` (`13` §5).

Giá trị string ('Active'...) là hợp đồng với **partial index predicate** (`WHERE status='Active'`) — đổi tên = vỡ index/dữ liệu (`04` §9). Convert `HasConversion<string>()` ở EF config (Infrastructure).

## 3. Field theo entity (nguồn: `04` §3 + file authoritative tương ứng)

- **Resort** (`Entity`): `Name`(req), `Timezone`(req, IANA), `LogoUrl?`, `CreatedAt`.
- **ResortSettings** (`AuditableEntity`): `ResortId`(req, unique FK 1-1), cờ bool `RequireRuleAckForFaq/Chat/Housekeeping`, `FaqEnabled/ChatEnabled/HousekeepingEnabled`, `PortalWindowMinutes`(=30), `VisitIdleExpiryHours`(=24), `GuestWebBaseUrl?`, `MaxMessageLength`, `MessageRateLimitPerMinute`, `HousekeepingRateLimitPerHour`. (`UpdatedAt`/`RowVersion` từ base.)
- **ResortLanguage** (`Entity`): `ResortId`(req), `Code`(req, BCP-47 vd en/vi/ko/zh), `DisplayName`(req), `IsEnabled`, `IsDefault`, `SortOrder`.
- **AppUser** (`AuditableEntity`): `ResortId`(req), `Email`(req, unique), `DisplayName`(req), `PasswordHash`(req, PHC string), `Role`(UserRole), `IsActive`, `LastLoginAt?`. (`CreatedAt`/`RowVersion` từ base.)
- **Room** (`AuditableEntity`,`ISoftDeletable`): `ResortId`(req), `RoomNumber`(req, 1–20), `Building?`(≤50), `Floor?`(−10..200), `Status`(RoomStatus). (audit+`RowVersion`+`IsDeleted`/`DeletedAt` từ base/interface.)
- **RoomQrToken** (`Entity`): `RoomId`(req), `Token`(req, unique toàn cục — capability token plaintext), `TokenPreview`(req, dạng che), `Status`(RoomQrTokenStatus), `Version`(int), `CreatedAt`, `CreatedByUserId?`, `RevokedAt?`, `RevokedByUserId?`, `RevocationReason?`.
- **GuestSession** (`Entity`): `SessionKeyHash`(req, unique — lưu HASH cookie, không raw), `PreferredLanguage?`, `FirstSeenAt`, `LastSeenAt`. (KHÔNG `ResortId` — `24` §6: thiết bị toàn cục/deployment.)
- **GuestVisit** (`Entity`): `ResortId`(req), `RoomId`(req), `GuestSessionId`(req), `Status`(GuestVisitStatus), `StartedAt`, `LastSeenAt`, `ExpiresAt`(=LastSeenAt+idle, vật hoá), `ClosedAt?`, `ClosedByUserId?`.

## 4. Quyết định thiết kế (ghi để kiểm chứng)

- **D5 — ResortSettings 1-1:** dùng `Id` riêng (UUIDv7 từ Entity) + `ResortId` **unique FK**, KHÔNG dùng ResortId làm PK. Lý do: đồng nhất với base `Entity` (mọi entity có `Id` UUIDv7, FK/equality/guard nhất quán, `EfRepository<T>` generic dùng được); 1-1 vẫn enforce bằng **unique index** trên `ResortId` (EF config). *Lệch spec §3 ("ResortId PK/FK 1-1") ở mức implementation — ghi DEV.*
- **D-tenancy:** `ResortId` là **Guid scalar** trên entity tenant-scoped (ResortSettings, ResortLanguage, AppUser, Room, GuestVisit). RoomQrToken scope qua Room (không ResortId trực tiếp). GuestSession KHÔNG ResortId (`24` §6). Instance-per-resort (`24` §4) — mỗi deployment 1 resort; `ResortId` là "bảo hiểm rẻ" cho khả năng shared-DB tương lai.
- **D-nav:** chưa thêm navigation property (reference-by-id). Thêm khi use case cần (vd resolve cần Room→Resort) — cục bộ, có kiểm chứng.
- **D-constraints-ở-Infra:** MaxLength/enum-string/xmin/index/partial-unique KHÔNG đặt ở Domain (POCO sạch) mà ở `IEntityTypeConfiguration<>` (Infrastructure, bước sau) + validator (Application). Domain chỉ giữ bất biến "kiểu" (required/nullable) + `Guid.Empty` guard (từ Entity base).

## 5. Không dùng / hoãn
- RefreshToken: base sở hữu (`RefreshTokenRecord`) — không lặp.
- Translation entity (RuleSectionTranslation, FaqItemTranslation...): thuộc module wave sau (`04` §4). `ITranslation` đã có ở SharedKernel để dùng khi tới wave.
- Entity module Rules/Faq/Messaging/Housekeeping/Notes: wave sau (TRD-002).

## 6. Kiểm thử bước này
- Domain là POCO → **build 0W/0E** (analyzer strict) là tiêu chí chính. Unit test hành vi entity ở bước có use case (Application). Có thể thêm test nhẹ: `Entity` guard Guid.Empty đã test ở SharedKernel; enum values tồn tại.

## 7. Truy vết
- `04` §3 (field), §8 (concurrency), §9 (type/enum-string); `02` §1 (Entity base); `12` §2 (Role); `13` §5 (GuestVisitStatus); `16` §2 (RoomStatus/RoomQrTokenStatus); `24` §4/§6 (tenancy, GuestSession không ResortId); `15` §5 (ResortSettings vận hành). Base: DEC-006 (enum string), `04` §7 (partial unique — Infra sau).
