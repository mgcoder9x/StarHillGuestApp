using Bedrock.Application.Behaviors;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Xunit;

namespace Bedrock.UnitTests.Application;

public sealed class TransactionDecoratorTests
{
    private sealed record Input(string Name);

    private sealed class RecordingCommand : ICommandUseCase<Input>
    {
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
        public Task<Result> ExecuteAsync(Input input, CancellationToken ct = default) =>
            Task.FromResult(Result.Failure(CommonErrors.Conflict()));
    }

    [Fact]
    public async Task Command_body_runs_inside_transaction()
    {
        var uow = new FakeUnitOfWork();
        var inner = new RecordingCommand { Uow = uow };
        var decorator = new TransactionCommandUseCaseDecorator<Input>(inner, uow);

        var result = await decorator.ExecuteAsync(new Input("x"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, uow.TransactionCalls);
        Assert.True(inner.RanInsideTransaction);
    }

    [Fact]
    public async Task Failing_command_result_propagates_through_transaction()
    {
        var uow = new FakeUnitOfWork();
        var decorator = new TransactionCommandUseCaseDecorator<Input>(new FailingCommand(), uow);

        var result = await decorator.ExecuteAsync(new Input("x"));

        Assert.True(result.IsFailure);
        Assert.Equal("conflict", result.Error.Code);
        Assert.Equal(1, uow.TransactionCalls);
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
