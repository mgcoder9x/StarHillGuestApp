# 09 — Đối chiếu với `Reference/Backend` (ĐÃ KIỂM CHỨNG)

> **File authoritative cho:** "giữ gì / sửa gì" so với code tham chiếu `FresherDev.HMS.*`.
>
> **Nguyên tắc:** mỗi khẳng định dưới đây được **kiểm chứng trực tiếp trong code thật** (đọc file cụ thể), không suy đoán. Cột "Bằng chứng" ghi rõ nơi kiểm chứng. Ngày kiểm chứng: xem `../../ai-notes/04-things-to-know.md`.

## 1. Bằng chứng đã đọc (evidence log)

| # | Khẳng định | Kết quả | File đã đọc | Bằng chứng cụ thể |
|---|-----------|---------|-------------|-------------------|
| E1 | Repository gọi `SaveChanges` mỗi thao tác | ✅ Đúng | `Core/FresherDev.HMS.EntityFramework/Common/Repositories/BaseRepository.cs` | `AddAsync`, `AddRangeAsync`, `UpdateAsync`, `UpdateRangeAsync`, `ExecuteUpdateAsync`, `DeleteAsync`, `DeleteRangeAsync`, `ExecuteDeleteAsync` — tất cả gọi `await dbContext.SaveChangesAsync()` bên trong |
| E2 | Không có `CancellationToken` trong repo | ✅ Đúng | `BaseRepository.cs` | Không method nào nhận `CancellationToken` |
| E3 | `UnitOfWork` không có `SaveChanges` | ✅ Đúng | `Core/FresherDev.HMS.EntityFramework/Common/UoW/UnitOfWork.cs` | Chỉ có `CreateTransaction`, `GetRepository`, `GetGenericRepository`, `Dispose` — không có phương thức save |
| E4 | `AutoDependency` ném lỗi khi 0 hoặc >1 impl | ✅ Đúng | `Core/FresherDev.HMS.Common/AutoDependency/AutoDependencyExtensions.cs` | `if (!implementTypes.Any() \|\| implementTypes.Count > 1) throw new Exception(...)` |
| E5 | Thiếu authentication scheme | ✅ Đúng | `Application/FresherDev.HMS.Api/Program.cs` | Có `app.UseAuthorization()` nhưng **không** có `app.UseAuthentication()` và không đăng ký scheme (`AddAuthentication`) |
| E6 | `ForceLoadAssembly` rải rác | ✅ Đúng | `Program.cs` | `ForceLoadAssemblies()` gọi thủ công 8 assembly |
| E7 | Target framework net8.0 | ✅ Đúng | `Application/FresherDev.HMS.Api/FresherDev.HMS.Api.csproj` | `<TargetFramework>net8.0</TargetFramework>` |
| E8 | Còn `// TODO: Remove` cho đăng ký UoW | ✅ Đúng | `Program.cs` | `// TODO: Remove this line` ngay trên `AddScoped<IUnitOfWork, UnitOfWork>()` |
| E9 | `FresherDev.HMS.Auth` RỖNG (không có code auth) | ✅ Đúng | `Core/FresherDev.HMS.Auth/` | Chỉ có `ForceLoadAssembly.cs` + `.csproj` (net8.0, chỉ reference `Common`). Không có JWT/login/hasher nào |
| E10 | Mật khẩu lưu PLAINTEXT | ✅ Đúng | `Entities/Core/User.cs`, `Users/UseCases/UserUseCase.Add.cs` | `User.Password` là string thường; use case set `Password = "password"` — không hash |
| E11 | Không có cấu hình JWT/auth trong appsettings | ✅ Đúng | `Application/FresherDev.HMS.Api/appsettings.json` | Không có section Jwt/Authentication; chỉ có ConnectionStrings + Logging |
| E12 | Domain/Core.Shared phụ thuộc EF (leaky layering) | ✅ Đúng | `Domain/FresherDev.HMS.Core.Shared/BaseServices/BaseService.cs` | `using FresherDev.HMS.EntityFramework;` + phụ thuộc `IUnitOfWork`/`IBaseRepository` trong tầng "domain service" |
| E13 | DbContext dùng `DateTimeOffset.UtcNow` trực tiếp, không set actor, không concurrency | ✅ Đúng | `Common/DbContext/ApplicationDbContext.cs` | `BeforeSaveChange` set `CreatedTime/UpdatedTime = DateTimeOffset.UtcNow`; KHÔNG set `CreatedBy/UpdatedBy`; không có RowVersion; vòng đầu lọc `IAuditEntity<Guid>` còn vòng hai lọc `IAuditEntity` (không nhất quán) |
| E14 | Controller trả string demo, input `[FromQuery] username` | ✅ Đúng | `Controllers/UserController.cs` | `return Ok("Add user success: " + username)`; endpoint tạo user nhận mỗi `username`, hardcode password |

## 2. Bảng "giữ gì / sửa gì"

| Khía cạnh | Reference (FresherDev.HMS) | Base mới | Lý do (sửa tận gốc) |
|---|---|---|---|
| Số project | ~10 project nhỏ, ranh giới mờ | 5 project rõ vai trò + module thư mục | Dễ định vị code, build nhanh, vẫn giữ dependency rule |
| DI convention | AutoDependency tự viết, **crash** khi 0/nhiều impl (E4), cần ForceLoadAssembly (E6) | Scrutor + marker interface, assembly marker tường minh | An toàn, bỏ ForceLoadAssembly, hỗ trợ nhiều impl |
| Repository/UoW | **SaveChanges mỗi thao tác** (E1), UoW không có save (E3) | Repo chỉ đổi ChangeTracker; **1 điểm SaveChanges**; transaction tường minh | Đảm bảo publish/cascade nguyên tử |
| CancellationToken | Không có (E2) | Propagate toàn tuyến | Hủy request đúng cách |
| Khóa chính | `TKey` generic | Cố định `uuid`, sinh client-side UUIDv7 (fallback: PG18 `uuidv7()` DEFAULT — cùng kiểu cột) | Gọn, đồng nhất; Id có sẵn lúc tạo; locality kỳ vọng tuần tự (verify 1 lần) |
| Concurrency token | Không (chỉ vài chỗ) | `IConcurrencyAware`=xmin cho mọi entity nội dung | Property optimistic concurrency |
| Response | `IHttpResponse` trộn HTTP vào domain | `Result<T>` trung lập + map ở Api | Tách tầng, test dễ |
| Error handling | Rải rác | 1 middleware → ProblemDetails + `code` | Hợp đồng lỗi ổn định |
| Validation | Chưa có pipeline | FluentValidation behavior | Chặn input xấu sớm |
| Auth | `UseAuthorization` nhưng **thiếu** scheme (E5) | JWT + guest cookie đầy đủ, policy | Bảo mật thực sự |
| Target | net8.0 (E7) | **net10.0 LTS** | LTS, đã chốt |
| Logging/Health | Không | Serilog + health check + mask token | Vận hành/quan sát |
| Thời gian/ngẫu nhiên | `DateTimeOffset.UtcNow` trực tiếp | `IDateTimeProvider`/`ITokenGenerator` | Test tất định |

## 3. Những gì reference làm TỐT — giữ lại

- Ý tưởng phân tầng Domain / EntityFramework / Application / Api (chỉ gom gọn lại).
- Base entity `Entity/AuditEntity/SoftDeleteEntity` (giữ ý tưởng, thêm concurrency).
- Use-case/service-per-operation (interface nhỏ, dễ test).
- Seed data qua `WebHostExtensions` (giữ, làm idempotent an toàn hơn).
- Tinh thần "khai báo DI tại chỗ" của AutoDependency (giữ, đổi engine sang Scrutor).

## 4. Kết luận đánh giá (sau khi đã đọc sâu)

**Bản chất `Reference/Backend` = scaffold học tập (fresher template), KHÔNG phải base production.** Bằng chứng: controller trả chuỗi demo (E14), use case hardcode `Password = "password"` (E10), dữ liệu mẫu `luuquangict`, không có auth thật (E9, E11), không hash mật khẩu (E10).

Phán quyết theo câu hỏi của user ("đã ok chưa?"):

| Phần reference | Đánh giá | Hành động cho base mới |
|---|---|---|
| `FresherDev.HMS.Auth` | ❌ **Rỗng** (E9) — không có gì để tái dùng | **Thiết kế + viết mới hoàn toàn** (JWT + guest cookie + Argon2/PBKDF2 hasher + refresh rotation) |
| Bảo mật mật khẩu | ❌ **Plaintext** (E10) — lỗi nghiêm trọng | Bắt buộc hash; không bao giờ lưu/ghi log plaintext |
| Phân tầng | ⚠️ **Leaky** (E12) — domain service phụ thuộc EF | Đảo ngược: Application định nghĩa port, Infrastructure implement (đã thiết kế) |
| DbContext audit | ⚠️ Thiếu actor + concurrency, dùng UtcNow trực tiếp (E13) | Bổ sung `IDateTimeProvider`, actor từ `ICurrentUser`, `xmin` concurrency |
| Repository/UoW | ❌ SaveChanges mỗi thao tác (E1), UoW vô nghĩa (E3) | Repo chỉ ChangeTracker; một điểm SaveChanges |
| DI | ⚠️ Crash khi 0/nhiều impl (E4) + ForceLoadAssembly (E6) | Scrutor + marker interface |
| Base entity | 🟡 Ý tưởng ok, generic TKey (Entity/Audit/Fully) | Giữ **ý tưởng**, cố định `Guid`, thêm concurrency |
| Controllers | ❌ Demo, trả string (E14) | Viết mới theo Result→ProblemDetails |
| Response<T> | 🟡 Cơ bản, trộn vào Common | Thay bằng `Result<T>` trung lập + ProblemDetails ở Api |

**Kết luận chung:** Base mới **thiết kế lại (design new)**, chỉ kế thừa **ý tưởng/pattern** (layering có chủ đích, entity base concept, use-case-per-operation, seed approach) — **không tái dùng code as-is**. Đặc biệt **Auth phải viết mới từ đầu** vì không có gì tồn tại.

## 5. Phạm vi vẫn chưa đọc (trung thực)

- `FresherDev.HMS.Infrastructure` (chỉ có ForceLoadAssembly — gần như rỗng, chưa xác nhận từng dòng).
- Nội dung migration `InitDb` chi tiết + provider thực thi (SQL Server vs Postgres — appsettings có cả hai chuỗi kết nối).
- `Permission.cs`, `Token.cs`, `Address.cs`, các file Criteria/Pagination/PredicateBuilder (tiện ích, chưa ảnh hưởng quyết định nền).
