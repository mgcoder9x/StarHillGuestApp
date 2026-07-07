# 05 — Catalog pattern/kiến trúc quan sát ở `Reference/Backend` (CHỈ tham khảo)

> Mục đích: **ghi lại các pattern & lựa chọn kiến trúc** mà reference (`FresherDev.HMS.*`) dùng, để tham khảo khi dựng base `Foundation`. **KHÔNG** đánh giá/không sao chép cách họ triển khai. Bằng chứng = cây file thật (đã liệt kê 2026-07-04). Chất lượng code đã đánh giá riêng ở `09-reference-reconciliation.md` (E1–E14) — file này chỉ liệt kê pattern.

## A. Phân tầng (layout project)
- Gom theo miền: `Application/` (Api, Api.Core, Api.Core.Shared), `Core/` (Auth, Common, EntityFramework, Infrastructure), `Domain/` (Core, Core.Shared, Domain). → layered/clean-ish nhưng ranh giới mờ, tên trùng (nhiều `Core`/`Shared`).

## B. Pattern quan sát được (theo bằng chứng file)

| # | Pattern | File chứng cứ | Ghi chú tham khảo |
|---|---|---|---|
| P1 | **Entity base tổ hợp qua interface** | `Entities/Abstractions/IEntity, IAuditEntity, IEnableEntity, ISoftDelete, IFullyEntity` + `Entity, AuditEntity, EnableEntity, SoftDeleteEntity, FullyEntity` | Tách khả năng (audit/enable/soft-delete) thành interface, ghép dần. Base `Foundation` cũng theo hướng interface (`IAuditable/ISoftDeletable/IConcurrencyAware`) nhưng gọn hơn. |
| P2 | **Repository + generic base** | `Repositories/IBaseRepository(.TEntity/.TKey), BaseRepository(.TKey)` + `UserRepository, PermissionRepository` | Repo generic theo `TEntity,TKey`. Foundation bỏ generic TKey (chốt `Guid`). |
| P3 | **Unit of Work** | `UoW/IUnitOfWork, UnitOfWork` | Có UoW nhưng repo tự SaveChanges (E1/E3) — Foundation sửa: chỉ UoW ghi. |
| P4 | **Use-case-per-operation** | `Users/UseCases/UserUseCase.Add/Delete/Query/Update` (partial) + `Interfaces/IAddUserUseCase...` | 1 interface / 1 thao tác. Foundation giữ ý tưởng này (dễ test/phân quyền). |
| P5 | **Criteria/Specification + PredicateBuilder** | `Common/BaseModels/Criteria/ICriteria`, `Helpers/Criteria/CriteriaBuilder, ICriteriaBuilder`, `Helpers/PredicateBuilder`, `Users/Criteria/UserCriteriaBuilder`, `Models/QueryUserCriteria` | Xây điều kiện truy vấn động (Specification-lite). Đáng tham khảo cho query phức tạp; Foundation cân nhắc read-model/queries thay vì leaky IQueryable. |
| P6 | **Pagination abstraction** | `Pagination/IPaginationInput/Output, PaginationInput/Output, OrderDirection` | Chuẩn hóa phân trang. Foundation có `PagedRequest/PagedResult`. |
| P7 | **Response/Result envelope** | `Response/IResponse, Response`, `Response/Http/IHttpResponse, HttpResponse(.Generic), HttpResponseExtensions` | Bọc kết quả API. ⚠️ Trộn HTTP vào tầng dưới (anti-pattern E-note) → Foundation thay bằng `Result<T>` trung lập + map ProblemDetails ở Api. |
| P8 | **Convention-based DI (AutoDependency)** | `AutoDependency/AutoDependencyExtensions, Attributes/AutoDependencyAttribute, IgnoreAutoDependencyAttribute, Models/DependencyType, Helpers/AssemblyHelper/Implement/Interface, Constants/AutoConstants` | Tự viết DI scan theo attribute. Foundation thay bằng **Scrutor** (an toàn hơn, không crash 0/nhiều impl). |
| P9 | **ForceLoadAssembly (nạp assembly cho reflection)** | `ForceLoadAssemblyBase` + `ForceLoadAssembly.cs` rải mỗi project | Ép nạp assembly để scan. Foundation bỏ, dùng `AssemblyMarker` tường minh. |
| P10 | **AutoMapper profile** | `Users/UserMapperProfile.cs` | Mapping qua profile. ⚠️ AutoMapper nay thương mại → Foundation dùng Mapperly/thủ công (license). |
| P11 | **BaseService layer** | `Core.Shared/BaseServices/BaseService, IBaseService` | Lớp service nền. Foundation nghiêng use-case trực tiếp thay vì BaseService phụ thuộc EF. |
| P12 | **Seeder + WebHostExtensions** | `SeedData/AddressSeeder, TokenSeeder, UserSeeder, WebHostExtensions` | Seed dữ liệu qua extension của host. Foundation giữ ý tưởng seeder idempotent. |
| P13 | **IEntityTypeConfiguration + design-time factory** | `DbContext/ApplicationDbContext, ApplicationContextFactory, Configurations/UserConfiguration` | Fluent config tách file + factory design-time cho migration. Foundation cũng dùng (design-time factory). |
| P14 | **Soft-delete global query filter** | `Extensions/SoftDeleteQueryExtension` | Lọc `IsDeleted` tự động. Foundation cũng có. |
| P15 | **Extension-method composition** | `Extensions/IConfigurationExtensions, IQueryableExtensions, IEnumerableExtensions, TypeExtensions, UseAppDbContextExtensions` | Wiring bằng extension method. |
| P16 | **Constants tập trung** | `Constants/EntityLength, AutoConstants` | Hằng số độ dài field / DI. |
| P17 | **Domain RBAC + Token** | `Entities/Core/User, Permission, Token, Address` | Miền mẫu: User–Permission (RBAC) + Token (auth). Auth logic RỖNG (E9) — chỉ có entity. |

## C. Điều rút ra cho `Foundation` (tham khảo, không sao chép code)
- **Giữ ý tưởng tốt:** entity base qua interface, Repository/UoW, use-case-per-operation, pagination, criteria/specification cho query động, IEntityTypeConfiguration + design-time factory, seeder idempotent, soft-delete filter.
- **Thay bằng lựa chọn tốt hơn (đã quyết ở ai-notes 01/02):** Result<T> thay IHttpResponse (P7); Scrutor thay AutoDependency (P8); bỏ ForceLoadAssembly (P9); Mapperly thay AutoMapper (P10); UoW là điểm ghi duy nhất (P3); bỏ generic TKey (P2).
- **Cần thận trọng:** P5 (Criteria) mạnh nhưng dễ leaky nếu trả IQueryable ra ngoài → Foundation dùng read-model/queries ở Infrastructure.
