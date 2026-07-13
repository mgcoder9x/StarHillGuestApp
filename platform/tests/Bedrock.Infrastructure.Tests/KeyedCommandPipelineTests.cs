using Bedrock.Application.DependencyInjection;
using Bedrock.Application.Ports.Caching;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Bedrock.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// FULL-DI guard cho gap "command void" (A-... / review): dựng pipeline THẬT qua <c>AddBedrockCore</c> + resolver
/// THẬT <see cref="ServiceProviderUnitOfWorkResolver"/>, chứng minh:
/// <list type="bullet">
///   <item>Command void KEYED resolve ĐÚNG Unit of Work của module (không đụng module khác, không cần unkeyed).</item>
///   <item>Command void PersistenceKey=null (host một DbContext legacy) resolve Unit of Work UNKEYED.</item>
/// </list>
/// Vì <see cref="ICommandUseCase{TInput}"/> nay kế thừa <see cref="ITransactionalUseCase"/>, mọi command void
/// buộc khai key ở compile-time → không còn nhánh "quên marker" để test runtime.
/// </summary>
public sealed class KeyedCommandPipelineTests
{
    private sealed record Input(string Name);

    private sealed class KeyedCommand(string? persistenceKey) : ICommandUseCase<Input>
    {
        public string? PersistenceKey => persistenceKey;

        public Task<Result> ExecuteAsync(Input input, CancellationToken ct = default) =>
            Task.FromResult(Result.Success());
    }

    private static ServiceProvider BuildProvider(
        KeyedCommand command,
        (string? Key, RecordingUnitOfWork Uow)[] unitsOfWork)
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IIdempotencyStore>(new StubIdempotencyStore());
        services.AddScoped<IUnitOfWorkResolver, ServiceProviderUnitOfWorkResolver>();

        foreach (var (key, uow) in unitsOfWork)
        {
            if (key is null)
            {
                services.AddScoped<IUnitOfWork>(_ => uow);
            }
            else
            {
                services.AddKeyedScoped<IUnitOfWork>(key, (_, _) => uow);
            }
        }

        services.AddScoped<ICommandUseCase<Input>>(_ => command);
        services.AddBedrockCore();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
    }

    [Fact]
    public async Task Keyed_void_command_opens_transaction_on_its_module_unit_of_work_only()
    {
        var moduleA = new RecordingUnitOfWork();
        var moduleB = new RecordingUnitOfWork();
        await using var provider = BuildProvider(
            new KeyedCommand("module-b"),
            [("module-a", moduleA), ("module-b", moduleB)]);

        await using var scope = provider.CreateAsyncScope();
        var command = scope.ServiceProvider.GetRequiredService<ICommandUseCase<Input>>();

        var result = await command.ExecuteAsync(new Input("write"));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, moduleA.TransactionCalls); // KHÔNG đụng module khác.
        Assert.Equal(1, moduleB.TransactionCalls); // đúng module của command.
    }

    [Fact]
    public async Task Explicit_null_key_void_command_uses_unkeyed_unit_of_work_legacy()
    {
        var unkeyed = new RecordingUnitOfWork();
        await using var provider = BuildProvider(new KeyedCommand(null), [(null, unkeyed)]);

        await using var scope = provider.CreateAsyncScope();
        var command = scope.ServiceProvider.GetRequiredService<ICommandUseCase<Input>>();

        var result = await command.ExecuteAsync(new Input("write"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unkeyed.TransactionCalls); // host một DbContext legacy: key null → unkeyed.
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int TransactionCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(action);
            TransactionCalls++;
            return await action(ct).ConfigureAwait(false);
        }
    }

    private sealed class StubCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
        public bool IsAuthenticated => false;
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions => [];
        public Guid? TenantId => null;
        public Guid? SessionId => null;
        public bool IsInRole(string role) => false;
        public bool HasPermission(string permission) => false;
    }

    /// <summary>No-op store: input test KHÔNG là IIdempotentCommand nên không bị chạm; chỉ cần resolve lúc construct.</summary>
    private sealed class StubIdempotencyStore : IIdempotencyStore
    {
        public Task<bool> TryBeginAsync(string idempotencyKey, TimeSpan ttl, CancellationToken ct = default) =>
            Task.FromResult(true);

        public Task CompleteAsync(string idempotencyKey, TimeSpan ttl, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task AbortAsync(string idempotencyKey, CancellationToken ct = default) => Task.CompletedTask;
    }
}
