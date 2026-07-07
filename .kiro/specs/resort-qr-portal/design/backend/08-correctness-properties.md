# 08 — Correctness Properties của Base

> **File authoritative cho:** 10 property xác thực **nền móng**, bổ sung cho 15 property nghiệp vụ trong `docs/resort-qr-portal/design.md`. Mỗi property truy vết tới requirement gốc.

### Property B1: Dependency rule không bị vi phạm
Domain không tham chiếu EF Core/Api; Application không tham chiếu Api/Infrastructure implementation; mọi phụ thuộc chỉ đi vào trong. (ArchitectureTests bắt buộc xanh.)
**Validates: Requirements 8.1, 8.5**

### Property B2: Ghi DB nguyên tử qua UnitOfWork
Repository không tự `SaveChanges`; một luồng use case chỉ ghi DB ở `SaveChangesAsync`/`ExecuteInTransactionAsync`. Khi transaction lỗi giữa chừng, **không** thay đổi nào được ghi.
**Validates: Requirements 8.3, 10.8**

### Property B3: Bất biến enforce ở DB, không chỉ ở code
Cố tạo: 2 `RoomQrToken` Active cùng phòng / **2 `RoomQrToken` cùng chuỗi `Token` (khác phòng)** / **2 `GuestVisit` Active cùng (session, room)** / 2 `ResortLanguage` default / 2 `RulePublication` IsCurrent / 2 hội thoại cùng visit — đều bị DB từ chối (unique / partial-unique index), kể cả khi bỏ qua tầng service. Resolve theo `token` luôn map **đúng 1** dòng; resolve đồng thời cùng (session, room) chỉ tạo **đúng 1** visit Active.
**Validates: Requirements 1.3, 1.5, 2.1, 7.5, 8.2, 8.3, 5.2, 10.2, 10.4**

### Property B4: Thời gian & ngẫu nhiên tất định trong test
Mọi use case phụ thuộc thời gian/ngẫu nhiên qua `IDateTimeProvider`/`ITokenGenerator`; test giả lập được, không có `DateTime.UtcNow`/`Random` ẩn.
**Validates: Requirements 10.3, 10.4**

### Property B5: Lỗi nghiệp vụ ra hợp đồng ổn định
Mọi lỗi nghiệp vụ trả `ProblemDetails` có `code` thuộc tập `AppErrors` cố định; không lộ stack trace; frontend `ApiError.code` khớp 1-1.
**Validates: Requirements 1.4, 3.11**

### Property B6: Ngữ cảnh actor luôn được gắn
Mọi thay đổi entity `IAuditable` có `CreatedAt/UpdatedAt` set tự động; hành động của admin/staff gắn `actorUserId` từ `ICurrentUser`; guest không bao giờ ghi được `actorUserId` của staff.
**Validates: Requirements 6.8, 9.4**

### Property B7: Sanitize là bắt buộc trên đường ghi
Không có đường ghi nội dung rich text nào (nội quy/FAQ) bỏ qua `IHtmlSanitizer`; nội dung lưu xuống DB không còn script thực thi được.
**Validates: Requirements 8.6, 11.4**

### Property B8: Cửa sổ thao tác luôn có thể hết hạn
Với chuỗi request tương tác liên tiếp quá `PortalWindowMinutes` kể từ `LastSeenAt`, hệ thống trả `session_expired` và **không** tự gia hạn; chỉ `/resolve` mới refresh.
**Validates: Requirements 10.3**

### Property B9: Tách bạch guest/admin surface
Endpoint `/api/admin/*` từ chối guest (không JWT hợp lệ); endpoint `/api/guest/*` không yêu cầu JWT nhưng phát/đọc `GuestSession`. Không route nào phục vụ chéo.
**Validates: Requirements 11.1, 11.2, 11.3**

### Property B10: Concurrency an toàn
Hai lần cập nhật đồng thời cùng bản ghi (`IConcurrencyAware`): lần lưu sau nhận `409 concurrency_conflict`, không ghi đè âm thầm.
**Validates: Requirements 8.1, 8.5**
