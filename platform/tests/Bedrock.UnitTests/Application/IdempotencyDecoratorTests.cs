using Bedrock.Application.Behaviors;
using Bedrock.Application.Ports.Caching;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Xunit;

namespace Bedrock.UnitTests.Application;

public sealed class IdempotencyDecoratorTests
{
    private sealed record IdempotentInput(string IdempotencyKey, string Payload) : IIdempotentCommand;

    private sealed record PlainInput(string Payload);

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

    [Fact]
    public async Task First_call_runs_inner_duplicate_returns_conflict()
    {
        var inner = new CountingUseCase<IdempotentInput>();
        var decorator = new IdempotencyUseCaseDecorator<IdempotentInput, string>(inner, new FakeIdempotencyStore());
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
        var decorator = new IdempotencyUseCaseDecorator<PlainInput, string>(inner, store);

        await decorator.ExecuteAsync(new PlainInput("p"));
        await decorator.ExecuteAsync(new PlainInput("p"));

        Assert.Equal(2, inner.Runs);
        Assert.Equal(0, store.Calls); // không chạm store cho input không idempotent.
    }

    [Fact]
    public async Task Command_variant_duplicate_returns_conflict()
    {
        var inner = new CountingCommand<IdempotentInput>();
        var decorator = new IdempotencyCommandUseCaseDecorator<IdempotentInput>(inner, new FakeIdempotencyStore());
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
}
