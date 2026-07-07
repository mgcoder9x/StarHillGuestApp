using System.Linq.Expressions;
using ResortQr.Application.Abstractions.Persistence;
using ResortQr.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace ResortQr.Infrastructure.Persistence;

/// <summary>
/// Repository generic EF: CHỈ thao tác ChangeTracker (Add/Update/Remove/Query) — KHÔNG tự ghi DB.
/// Ghi thật đi qua <see cref="EfUnitOfWork.SaveChangesAsync"/> (điểm ghi nguyên tử duy nhất — DEV-001).
/// Xóa mềm: <see cref="Remove"/> gọi EF Remove; <see cref="ResortQrDbContext"/> chuyển Delete→Modified khi
/// entity là <see cref="ISoftDeletable"/> lúc SaveChanges.
/// </summary>
public sealed class EfRepository<TEntity> : IRepository<TEntity>
    where TEntity : Entity
{
    private readonly ResortQrDbContext _context;

    public EfRepository(ResortQrDbContext context) => _context = context;

    public IQueryable<TEntity> Query() => _context.Set<TEntity>();

    public ValueTask<TEntity?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Set<TEntity>().FindAsync([id], cancellationToken);

    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        _context.Set<TEntity>().FirstOrDefaultAsync(predicate, cancellationToken);

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        _context.Set<TEntity>().AnyAsync(predicate, cancellationToken);

    public void Add(TEntity entity) => _context.Set<TEntity>().Add(entity);

    public void Update(TEntity entity) => _context.Set<TEntity>().Update(entity);

    public void Remove(TEntity entity) => _context.Set<TEntity>().Remove(entity);
}
