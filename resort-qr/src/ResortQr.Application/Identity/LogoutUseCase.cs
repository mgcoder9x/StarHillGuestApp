using ResortQr.Application.Abstractions;
using ResortQr.Application.Abstractions.Persistence;
using ResortQr.Application.Common;
using ResortQr.SharedKernel.DependencyInjection;
using ResortQr.SharedKernel.Results;

namespace ResortQr.Application.Identity;

/// <summary>
/// Đăng xuất: thu hồi refresh token hiện tại (idempotent — không lỗi nếu token không tồn tại/đã thu hồi).
/// </summary>
public sealed class LogoutUseCase : ICommandUseCase<LogoutCommand>, IScopedService
{
    private readonly IRefreshTokenStore _refreshTokens;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutUseCase(
        IRefreshTokenStore refreshTokens,
        IRefreshTokenHasher refreshTokenHasher,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork)
    {
        _refreshTokens = refreshTokens;
        _refreshTokenHasher = refreshTokenHasher;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(LogoutCommand input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var hash = _refreshTokenHasher.Hash(input.RefreshToken);
        var record = await _refreshTokens.FindByHashAsync(hash, cancellationToken);

        if (record is not null && record.RevokedAt is null)
        {
            record.RevokedAt = _clock.UtcNow;
            record.RevokedReason = "logout";
            await _refreshTokens.UpdateAsync(record, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Ok();
    }
}
