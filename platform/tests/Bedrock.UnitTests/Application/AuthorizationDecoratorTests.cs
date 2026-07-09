using Bedrock.Application.Authorization;
using Bedrock.Application.Behaviors;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Xunit;

namespace Bedrock.UnitTests.Application;

public sealed class AuthorizationDecoratorTests
{
    [RequirePermission("orders.create")]
    private sealed record SecuredInput(string Name);

    private sealed record OpenInput(string Name);

    [RequirePermission("a")]
    [RequirePermission("b")]
    private sealed record TwoPermInput;

    private sealed class RecordingUseCase<TIn> : IUseCase<TIn, string>
    {
        public bool Ran { get; private set; }

        public Task<Result<string>> ExecuteAsync(TIn input, CancellationToken ct = default)
        {
            Ran = true;
            return Task.FromResult(Result<string>.Success("ok"));
        }
    }

    private sealed class RecordingCommand<TIn> : ICommandUseCase<TIn>
    {
        public bool Ran { get; private set; }

        public Task<Result> ExecuteAsync(TIn input, CancellationToken ct = default)
        {
            Ran = true;
            return Task.FromResult(Result.Success());
        }
    }

    [Fact]
    public async Task Missing_permission_should_return_forbidden_and_not_run_inner()
    {
        var inner = new RecordingUseCase<SecuredInput>();
        var decorator = new AuthorizationUseCaseDecorator<SecuredInput, string>(inner, new FakeCurrentUser());

        var result = await decorator.ExecuteAsync(new SecuredInput("x"));

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.False(inner.Ran);
    }

    [Fact]
    public async Task Granted_permission_should_run_inner()
    {
        var inner = new RecordingUseCase<SecuredInput>();
        var decorator = new AuthorizationUseCaseDecorator<SecuredInput, string>(
            inner, new FakeCurrentUser("orders.create"));

        var result = await decorator.ExecuteAsync(new SecuredInput("x"));

        Assert.True(result.IsSuccess);
        Assert.True(inner.Ran);
    }

    [Fact]
    public async Task No_permission_attribute_should_pass_through_even_when_user_has_none()
    {
        var inner = new RecordingUseCase<OpenInput>();
        var decorator = new AuthorizationUseCaseDecorator<OpenInput, string>(inner, new FakeCurrentUser());

        var result = await decorator.ExecuteAsync(new OpenInput("x"));

        Assert.True(result.IsSuccess);
        Assert.True(inner.Ran);
    }

    [Fact]
    public async Task All_declared_permissions_required_and_semantics()
    {
        // Chỉ có "a", thiếu "b" → phải bị chặn (AND — least privilege, AD-038).
        var inner = new RecordingUseCase<TwoPermInput>();
        var decorator = new AuthorizationUseCaseDecorator<TwoPermInput, string>(inner, new FakeCurrentUser("a"));

        var result = await decorator.ExecuteAsync(new TwoPermInput());

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
        Assert.False(inner.Ran);
    }

    [Fact]
    public async Task Command_variant_missing_permission_should_fail()
    {
        var inner = new RecordingCommand<SecuredInput>();
        var decorator = new AuthorizationCommandUseCaseDecorator<SecuredInput>(inner, new FakeCurrentUser());

        var result = await decorator.ExecuteAsync(new SecuredInput("x"));

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
        Assert.False(inner.Ran);
    }

    [Fact]
    public void PermissionMetadata_reads_declared_permissions_and_empty_when_none()
    {
        Assert.Equal(["orders.create"], PermissionMetadata.For(typeof(SecuredInput)));
        Assert.Equal(["a", "b"], PermissionMetadata.For(typeof(TwoPermInput)));
        Assert.Empty(PermissionMetadata.For(typeof(OpenInput)));
    }
}

/// <summary>Test double cho <see cref="ICurrentUser"/> — cấu hình tập permission qua ctor.</summary>
internal sealed class FakeCurrentUser : ICurrentUser
{
    private readonly HashSet<string> _permissions;

    public FakeCurrentUser(params string[] permissions) =>
        _permissions = new HashSet<string>(permissions, StringComparer.Ordinal);

    public Guid? UserId => Guid.Empty;

    public bool IsAuthenticated => true;

    public IReadOnlyCollection<string> Roles => [];

    public IReadOnlyCollection<string> Permissions => _permissions;

    public Guid? TenantId => null;

    public Guid? SessionId => null;

    public bool IsInRole(string role) => false;

    public bool HasPermission(string permission) => _permissions.Contains(permission);
}
