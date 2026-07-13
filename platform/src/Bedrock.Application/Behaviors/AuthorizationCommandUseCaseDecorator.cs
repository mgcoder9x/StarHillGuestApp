using Bedrock.Application.Authorization;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;

namespace Bedrock.Application.Behaviors;

/// <summary>
/// Như <see cref="AuthorizationUseCaseDecorator{TInput,TOutput}"/> nhưng cho <see cref="ICommandUseCase{TInput}"/>.
/// Permission thiếu → trả <c>forbidden</c>, KHÔNG chạy thân (không đổi trạng thái).
/// </summary>
public sealed class AuthorizationCommandUseCaseDecorator<TInput> : ICommandUseCase<TInput>
{
    private static readonly string[] RequiredPermissions = PermissionMetadata.For(typeof(TInput));
    private readonly ICommandUseCase<TInput> _inner;
    private readonly ICurrentUser _currentUser;

    public AuthorizationCommandUseCaseDecorator(ICommandUseCase<TInput> inner, ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(currentUser);
        _inner = inner;
        _currentUser = currentUser;
    }

    public Task<Result> ExecuteAsync(TInput input, CancellationToken ct = default)
    {
        foreach (var permission in RequiredPermissions)
        {
            if (!_currentUser.HasPermission(permission))
            {
                return Task.FromResult(Result.Failure(CommonErrors.Forbidden()));
            }
        }

        return _inner.ExecuteAsync(input, ct);
    }

    public string? PersistenceKey => _inner.PersistenceKey;
}
