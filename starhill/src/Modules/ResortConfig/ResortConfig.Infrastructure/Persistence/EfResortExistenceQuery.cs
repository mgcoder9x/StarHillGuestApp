using Microsoft.EntityFrameworkCore;
using ResortConfig.Contracts.Queries;

namespace ResortConfig.Infrastructure.Persistence;

/// <summary>
/// Impl EF của <see cref="IResortExistenceQuery"/> — read-only (<c>AsNoTracking</c> + <c>AnyAsync</c>, chỉ EXISTS ở
/// SQL, không load entity). Đăng ký scoped ở <c>AddResortConfigInfrastructure</c> (dùng chung scope/DbContext).
/// </summary>
public sealed class EfResortExistenceQuery(ResortConfigDbContext db) : IResortExistenceQuery
{
    public Task<bool> ExistsAsync(Guid resortId, CancellationToken ct = default) =>
        db.Resorts.AsNoTracking().AnyAsync(r => r.Id == resortId, ct);
}
