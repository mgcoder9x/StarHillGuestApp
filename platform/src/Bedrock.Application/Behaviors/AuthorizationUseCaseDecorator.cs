using Bedrock.Application.Authorization;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;

namespace Bedrock.Application.Behaviors;

/// <summary>
/// Behavior Authorization (§8) — chạy TRƯỚC Validation (đảo so Blueprint §14, DV/AD): không lộ chi tiết validation
/// cho caller thiếu quyền + permission-check rẻ hơn validate payload. Đọc permission khai báo trên
/// <c>typeof(TInput)</c> (qua <see cref="RequirePermissionAttribute"/>); yêu cầu <see cref="ICurrentUser.HasPermission"/>
/// đúng CHO TỪNG permission (AND). Không khai permission → pass-through. Authorization theo TRẠNG THÁI resource
/// (cần load dữ liệu) vẫn nằm trong use case (design §8).
/// </summary>
public sealed class AuthorizationUseCaseDecorator<TInput, TOutput> : IUseCase<TInput, TOutput>
{
    private static readonly string[] RequiredPermissions = PermissionMetadata.For(typeof(TInput));
    private readonly IUseCase<TInput, TOutput> _inner;
    private readonly ICurrentUser _currentUser;

    public AuthorizationUseCaseDecorator(IUseCase<TInput, TOutput> inner, ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(currentUser);
        _inner = inner;
        _currentUser = currentUser;
    }

    public Task<Result<TOutput>> ExecuteAsync(TInput input, CancellationToken ct = default)
    {
        foreach (var permission in RequiredPermissions)
        {
            if (!_currentUser.HasPermission(permission))
            {
                return Task.FromResult(Result<TOutput>.Failure(CommonErrors.Forbidden()));
            }
        }

        return _inner.ExecuteAsync(input, ct);
    }
}
