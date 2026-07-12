using Bedrock.Application.Behaviors;
using Bedrock.Application.Ports.Caching;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Xunit;

namespace Bedrock.UnitTests.Application;

public sealed class IdempotencyDecoratorTests
{
    private sealed record IdempotentInput(string IdempotencyKey, string Payload) : IIdempotentCommand;

    private sealed record OtherInput(string IdempotencyKey, string Payload) : IIdempotentCommand;

    private sealed record PlainInput(string Payload);

    private sealed class FailingUseCase<TIn> : IUseCase<TIn, string>
    {
        public int Runs { get; private set; }

        public Task<Result<string>> ExecuteAsync(TIn input, CancellationToken ct = default)
        {
            Runs++;
            return Task.FromResult(Result<string>.Failure(Error.Validation("boom", "inner failed")));
        }
    }

    private sealed class ThrowingUseCase<TIn> : IUseCase<TIn, string>
    {
        public int Runs { get; private set; }

        public Task<Result<string>> ExecuteAsync(TIn input, CancellationToken ct = default)
        {
            Runs++;
            throw new InvalidOperationException("inner threw");
        }
    }

    private sealed class CountingUseCase<TIn> : IUseCase<TIn, string>
    {
        public int Runs { get; private set; }

        public Task<Result<string>> ExecuteAsync(TIn input, CancellationToken ct = default)
        {
            Runs++;
            return Task.FromResult(Result<string>.Success("ok"));
        }
    }

    private sealed class CountingCommand<TIn> : ICommandUseCase<TIn>
    {
        public int Runs { get; private set; }

        public Task<Result> ExecuteAsync(TIn input, CancellationToken ct = default)
        {
            Runs++;
            return Task.FromResult(Result.Success());
        }
    }

    /// <summary>Test double ICurrentUser cấu hình được TenantId/UserId (kiểm scope idempotency — P1-03).</summary>
    private sealed class ScopedUser(Guid? tenantId, Guid? userId) : ICurrentUser
    {
        public Guid? UserId => userId;
        public bool IsAuthenticated => userId is not null;
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions => [];
        public Guid? TenantId => tenantId;
        public Guid? SessionId => null;
        public bool IsInRole(string role) => false;
        public bool HasPermission(string permission) => false;
    }

    [Fact]
    public async Task First_call_runs_inner_duplicate_returns_conflict()
    {
        var inner = new CountingUseCase<IdempotentInput>();
        var decorator = new IdempotencyUseCaseDecorator<IdempotentInput, string>(
            inner, new FakeIdempotencyStore(), new FakeCurrentUser([]));
        var input = new IdempotentInput("key-1", "p");

        var first = await decorator.ExecuteAsync(input);
        var second = await decorator.ExecuteAsync(input);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal("idempotency_conflict", second.Error.Code);
        Assert.Equal(ErrorType.Conflict, second.Error.Type);
        Assert.Equal(1, inner.Runs); // thân chỉ chạy đúng MỘT lần.
    }

    [Fact]
    public async Task Non_idempotent_input_passes_through_without_touching_store()
    {
        var store = new FakeIdempotencyStore();
        var inner = new CountingUseCase<PlainInput>();
        var decorator = new IdempotencyUseCaseDecorator<PlainInput, string>(
            inner, store, new FakeCurrentUser([]));

        await decorator.ExecuteAsync(new PlainInput("p"));
        await decorator.ExecuteAsync(new PlainInput("p"));

        Assert.Equal(2, inner.Runs);
        Assert.Equal(0, store.Calls); // không chạm store cho input không idempotent.
    }

    [Fact]
    public async Task Command_variant_duplicate_returns_conflict()
    {
        var inner = new CountingCommand<IdempotentInput>();
        var decorator = new IdempotencyCommandUseCaseDecorator<IdempotentInput>(
            inner, new FakeIdempotencyStore(), new FakeCurrentUser([]));
        var input = new IdempotentInput("key-2", "p");

        var first = await decorator.ExecuteAsync(input);
        var second = await decorator.ExecuteAsync(input);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal("idempotency_conflict", second.Error.Code);
        Assert.Equal(1, inner.Runs);
    }

    [Fact]
    public void Default_ttl_is_24_hours()
    {
        Assert.Equal(TimeSpan.FromHours(24), IdempotencyDefaults.Ttl);
    }

    [Fact]
    public async Task Failed_inner_releases_key_so_retry_is_allowed() // A-13: hết "claim rồi quên"
    {
        var store = new FakeIdempotencyStore();
        var inner = new FailingUseCase<IdempotentInput>();
        var decorator = new IdempotencyUseCaseDecorator<IdempotentInput, string>(inner, store, new FakeCurrentUser());
        var input = new IdempotentInput("key-fail", "p");

        var first = await decorator.ExecuteAsync(input);
        var second = await decorator.ExecuteAsync(input);

        Assert.True(first.IsFailure);
        Assert.True(second.IsFailure);
        Assert.Equal(2, inner.Runs); // Failure → Abort nhả key → retry chạy LẠI (không bị khoá đến hết TTL).
        Assert.NotEqual("idempotency_conflict", second.Error.Code); // KHÔNG phải conflict — key đã được nhả.
    }

    [Fact]
    public async Task Different_input_types_with_same_raw_key_do_not_collide() // A-13: key namespace theo TInput
    {
        var store = new FakeIdempotencyStore();
        var user = new FakeCurrentUser();
        var a = new IdempotencyUseCaseDecorator<IdempotentInput, string>(
            new CountingUseCase<IdempotentInput>(), store, user);
        var b = new IdempotencyUseCaseDecorator<OtherInput, string>(
            new CountingUseCase<OtherInput>(), store, user);

        var ra = await a.ExecuteAsync(new IdempotentInput("same-key", "p"));
        var rb = await b.ExecuteAsync(new OtherInput("same-key", "p"));

        Assert.True(ra.IsSuccess);
        Assert.True(rb.IsSuccess); // cùng raw key nhưng KHÁC TInput → key namespaced khác → KHÔNG va chạm/conflict.
    }

    [Fact]
    public async Task Exception_in_inner_releases_key_so_retry_is_allowed() // A-13: nhánh exception CŨNG nhả claim
    {
        var store = new FakeIdempotencyStore();
        var inner = new ThrowingUseCase<IdempotentInput>();
        var decorator = new IdempotencyUseCaseDecorator<IdempotentInput, string>(inner, store, new FakeCurrentUser());
        var input = new IdempotentInput("key-throw", "p");

        await Assert.ThrowsAsync<InvalidOperationException>(() => decorator.ExecuteAsync(input));
        // Exception giữa chừng (vd crash trước commit) → catch Abort NHẢ key → retry KHÔNG bị khoá conflict tới hết TTL.
        await Assert.ThrowsAsync<InvalidOperationException>(() => decorator.ExecuteAsync(input));
        Assert.Equal(2, inner.Runs);
    }

    [Fact]
    public async Task Same_key_same_tenant_different_users_do_not_collide() // P1-03: chống đụng key XUYÊN USER cùng tenant
    {
        var store = new FakeIdempotencyStore();
        var tenant = Guid.CreateVersion7();
        var userA = new ScopedUser(tenant, Guid.CreateVersion7());
        var userB = new ScopedUser(tenant, Guid.CreateVersion7()); // CÙNG tenant, KHÁC user.
        var a = new IdempotencyUseCaseDecorator<IdempotentInput, string>(new CountingUseCase<IdempotentInput>(), store, userA);
        var b = new IdempotencyUseCaseDecorator<IdempotentInput, string>(new CountingUseCase<IdempotentInput>(), store, userB);

        var ra = await a.ExecuteAsync(new IdempotentInput("same-key", "p"));
        var rb = await b.ExecuteAsync(new IdempotentInput("same-key", "p"));

        Assert.True(ra.IsSuccess);
        Assert.True(rb.IsSuccess); // user B KHÔNG bị chặn bởi key của user A (fallback cũ Tenant??User sẽ làm đụng).
    }

    [Fact]
    public async Task Same_key_same_identity_still_conflicts() // P1-03: cùng tenant+user+key VẪN idempotent (không phá)
    {
        var store = new FakeIdempotencyStore();
        var user = new ScopedUser(Guid.CreateVersion7(), Guid.CreateVersion7());
        var a = new IdempotencyUseCaseDecorator<IdempotentInput, string>(new CountingUseCase<IdempotentInput>(), store, user);
        var b = new IdempotencyUseCaseDecorator<IdempotentInput, string>(new CountingUseCase<IdempotentInput>(), store, user);

        var first = await a.ExecuteAsync(new IdempotentInput("k", "p"));
        var second = await b.ExecuteAsync(new IdempotentInput("k", "p")); // cùng identity + key + store.

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal("idempotency_conflict", second.Error.Code);
    }
}

/// <summary>Test double cho <see cref="IIdempotencyStore"/>: lần đầu với key → true, sau đó false; đếm số lần gọi.</summary>
internal sealed class FakeIdempotencyStore : IIdempotencyStore
{
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);

    public int Calls { get; private set; }

    public Task<bool> TryBeginAsync(string idempotencyKey, TimeSpan ttl, CancellationToken ct = default)
    {
        Calls++;
        return Task.FromResult(_seen.Add(idempotencyKey));
    }

    public Task CompleteAsync(string idempotencyKey, TimeSpan ttl, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task AbortAsync(string idempotencyKey, CancellationToken ct = default)
    {
        _seen.Remove(idempotencyKey);
        return Task.CompletedTask;
    }
}
