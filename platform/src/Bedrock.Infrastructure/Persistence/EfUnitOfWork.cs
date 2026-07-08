using Bedrock.Application.Ports.Persistence;
using Bedrock.Domain.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bedrock.Infrastructure.Persistence;

/// <summary>
/// Unit of Work EF (design §5.1). Điểm ghi DB DUY NHẤT + giao dịch tường minh.
/// <list type="bullet">
///   <item><see cref="SaveChangesAsync"/>: gọi <c>DbContext.SaveChangesAsync</c>; map
///     <see cref="DbUpdateConcurrencyException"/> → <see cref="ConcurrencyConflictException"/> (kernel) để
///     Api map 409 mà KHÔNG phụ thuộc EF (persistence-layer-design §3/§7).</item>
///   <item><see cref="ExecuteInTransactionAsync"/>: all-or-nothing qua <c>ExecutionStrategy</c> (tương thích
///     retry của Npgsql). <b>REENTRANCY (R7.4):</b> đã có transaction đang mở → THAM GIA (không BEGIN lồng,
///     không commit sớm); cần vì Transaction behavior (§8) bọc command trong khi use case cũng có thể gọi
///     tường minh.</item>
/// </list>
/// </summary>
public sealed class EfUnitOfWork(PlatformDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Giữ nguyên inner để chẩn đoán; thông điệp trung lập cho tầng trên (Api map 409).
            throw new ConcurrencyConflictException(
                "The record was modified by another operation. Reload and try again.", ex);
        }
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        // Reentrancy: đã có transaction do UoW này mở → tham gia, không mở lồng.
        if (context.Database.CurrentTransaction is not null)
        {
            return await action(ct).ConfigureAwait(false);
        }

        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            IDbContextTransaction transaction =
                await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                try
                {
                    var result = await action(ct).ConfigureAwait(false);
                    await transaction.CommitAsync(ct).ConfigureAwait(false);
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(ct).ConfigureAwait(false);
                    throw;
                }
            }
        }).ConfigureAwait(false);
    }
}
