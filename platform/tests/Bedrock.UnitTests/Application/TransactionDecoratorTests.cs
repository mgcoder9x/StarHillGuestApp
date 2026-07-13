using Bedrock.Application.Behaviors;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Xunit;

namespace Bedrock.UnitTests.Application;

public sealed class TransactionDecoratorTests
{
    private sealed record Input(string Name);

    private sealed class RecordingCommand : ICommandUseCase<Input>, ITransactionalUseCase
    {
        public string? PersistenceKey { get; init; }

        public bool RanInsideTransaction { get; private set; }
        public FakeUnitOfWork? Uow { get; init; }

        public Task<Result> ExecuteAsync(Input input, CancellationToken ct = default)
        {
            // Khi thân chạy, transaction phải đã mở (reentrancy: nested join, không nổ).
            RanInsideTransaction = Uow?.InTransaction == true;
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class FailingCommand : ICommandUseCase<Input>
    {
        public string? PersistenceKey => null;

        public Task<Result> ExecuteAsync(Input input, CancellationToken ct = default) =>
            Task.FromResult(Result.Failure(CommonErrors.Conflict()));
    }

    [Fact]
    public async Task Command_body_runs_inside_transaction()
    {
        var uow = new FakeUnitOfWork();
        var inner = new RecordingCommand { Uow = uow };
        var resolver = new FakeUnitOfWorkResolver((null, uow));
        var decorator = new TransactionCommandUseCaseDecorator<Input>(inner, resolver);

        var result = await decorator.ExecuteAsync(new Input("x"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, uow.TransactionCalls);
        Assert.True(inner.RanInsideTransaction);
    }

    [Fact]
    public async Task Failing_command_result_propagates_through_transaction()
    {
        var uow = new FakeUnitOfWork();
        var decorator = new TransactionCommandUseCaseDecorator<Input>(
            new FailingCommand(),
            new FakeUnitOfWorkResolver((null, uow)));

        var result = await decorator.ExecuteAsync(new Input("x"));

        Assert.True(result.IsFailure);
        Assert.Equal("conflict", result.Error.Code);
        Assert.Equal(1, uow.TransactionCalls);
    }

    [Fact]
    public async Task Keyed_command_resolves_only_its_module_unit_of_work()
    {
        var moduleA = new FakeUnitOfWork();
        var moduleB = new FakeUnitOfWork();
        var inner = new RecordingCommand { Uow = moduleB, PersistenceKey = "module-b" };
        var resolver = new FakeUnitOfWorkResolver(("module-a", moduleA), ("module-b", moduleB));

        var result = await new TransactionCommandUseCaseDecorator<Input>(inner, resolver)
            .ExecuteAsync(new Input("x"));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, moduleA.TransactionCalls);
        Assert.Equal(1, moduleB.TransactionCalls);
        Assert.Equal("module-b", Assert.Single(resolver.RequestedKeys));
    }

    [Fact]
    public async Task Value_returning_transactional_use_case_uses_keyed_unit_of_work_while_query_does_not()
    {
        var uow = new FakeUnitOfWork();
        var resolver = new FakeUnitOfWorkResolver(("identity", uow));
        var command = new TransactionUseCaseDecorator<Input, string>(new ValueCommand(), resolver);
        var query = new TransactionUseCaseDecorator<Input, string>(new ValueQuery(), resolver);

        Assert.True((await command.ExecuteAsync(new Input("write"))).IsSuccess);
        Assert.True((await query.ExecuteAsync(new Input("read"))).IsSuccess);

        Assert.Equal(1, uow.TransactionCalls);
        Assert.Equal(["identity"], resolver.RequestedKeys);
    }

    private sealed class ValueCommand : ICommandUseCase<Input, string>
    {
        public string PersistenceKey => "identity";

        public Task<Result<string>> ExecuteAsync(Input input, CancellationToken ct = default) =>
            Task.FromResult(Result.Success(input.Name));
    }

    private sealed class ValueQuery : IQueryUseCase<Input, string>
    {
        public Task<Result<string>> ExecuteAsync(Input input, CancellationToken ct = default) =>
            Task.FromResult(Result.Success(input.Name));
    }
}

/// <summary>Test double cho <see cref="IUnitOfWork"/>: ghi nhận số lần mở transaction + cờ đang-trong-transaction.</summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int TransactionCalls { get; private set; }

    public bool InTransaction { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        TransactionCalls++;
        InTransaction = true;
        try
        {
            return await action(ct).ConfigureAwait(false);
        }
        finally
        {
            InTransaction = false;
        }
    }
}

internal sealed class FakeUnitOfWorkResolver(params (string? Key, IUnitOfWork UnitOfWork)[] registrations)
    : IUnitOfWorkResolver
{
    private readonly Dictionary<string, IUnitOfWork> _keyed = registrations
        .Where(registration => registration.Key is not null)
        .ToDictionary(registration => registration.Key!, registration => registration.UnitOfWork, StringComparer.Ordinal);
    private readonly IUnitOfWork? _unkeyed = registrations
        .FirstOrDefault(registration => registration.Key is null).UnitOfWork;

    public List<string?> RequestedKeys { get; } = [];

    public IUnitOfWork Resolve(string? persistenceKey)
    {
        RequestedKeys.Add(persistenceKey);
        if (persistenceKey is null)
        {
            return _unkeyed ?? throw new InvalidOperationException("Missing unkeyed Unit of Work.");
        }

        return _keyed.TryGetValue(persistenceKey, out var unitOfWork)
            ? unitOfWork
            : throw new InvalidOperationException($"Missing Unit of Work for '{persistenceKey}'.");
    }
}
