using System.Security.Claims;
using Bedrock.Application.DependencyInjection;
using Bedrock.Application.Messaging;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Bedrock.Messaging.Contracts;
using Bedrock.Infrastructure.DependencyInjection;
using Identity.Application.RefreshToken;
using Identity.Infrastructure.DependencyInjection;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Identity.IntegrationTests;

public sealed class IdentityTransactionPipelineTests
{
    [Fact]
    public async Task Refresh_rotation_runs_inside_identity_keyed_transaction_through_full_pipeline()
    {
        var uow = new RecordingUnitOfWork();
        var store = new RecordingRefreshTokenStore(uow);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser>(new AnonymousCurrentUser());
        services.AddIdentityInfrastructure(options => options.UseNpgsql("Host=unused;Database=unused"));

        // Test overrides are keyed exactly like production. Last registration is the active keyed service.
        services.AddKeyedSingleton<IUnitOfWork>(IdentityInfrastructureExtensions.PersistenceKey, uow);
        services.AddKeyedSingleton<IRefreshTokenStore>(IdentityInfrastructureExtensions.PersistenceKey, store);
        services.AddKeyedSingleton<IOutboxWriter>(
            IdentityInfrastructureExtensions.PersistenceKey,
            new RecordingOutboxWriter(uow));
        services.AddSingleton<ITokenGenerator>(new FixedTokenGenerator());
        services.AddSingleton<IJwtTokenService>(new FixedJwtTokenService());
        services.AddCacheCore();
        services.AddBedrockCore();

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        await using var scope = provider.CreateAsyncScope();
        var useCase = scope.ServiceProvider
            .GetRequiredService<IUseCase<RefreshTokenCommand, RefreshTokenResult>>();

        var result = await useCase.ExecuteAsync(new RefreshTokenCommand("known-token"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, uow.TransactionCalls);
        Assert.Equal(1, uow.SaveChangesCalls);
        Assert.True(store.AllCallsWereTransactional);
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public bool IsInsideTransaction { get; private set; }
        public int TransactionCalls { get; private set; }
        public int SaveChangesCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            Assert.True(IsInsideTransaction);
            SaveChangesCalls++;
            return Task.FromResult(1);
        }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> action,
            CancellationToken ct = default)
        {
            TransactionCalls++;
            Assert.False(IsInsideTransaction);
            IsInsideTransaction = true;
            try
            {
                return await action(ct);
            }
            finally
            {
                IsInsideTransaction = false;
            }
        }
    }

    private sealed class RecordingRefreshTokenStore(RecordingUnitOfWork uow) : IRefreshTokenStore
    {
        private readonly Guid _userId = Guid.CreateVersion7();

        public bool AllCallsWereTransactional { get; private set; } = true;

        public Task<RefreshTokenSnapshot?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        {
            RecordTransactionState();
            return Task.FromResult<RefreshTokenSnapshot?>(new RefreshTokenSnapshot(
                Guid.CreateVersion7(),
                _userId,
                Guid.CreateVersion7(),
                tokenHash,
                DateTimeOffset.UtcNow.AddDays(1),
                RevokedAt: null));
        }

        public Task<bool> TryConsumeAsync(
            Guid tokenId,
            DateTimeOffset now,
            string reason,
            Guid replacedByTokenId,
            CancellationToken ct = default)
        {
            RecordTransactionState();
            return Task.FromResult(true);
        }

        public Task AddAsync(RefreshTokenSnapshot newToken, CancellationToken ct = default)
        {
            RecordTransactionState();
            return Task.CompletedTask;
        }

        public Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default)
        {
            RecordTransactionState();
            return Task.CompletedTask;
        }

        private void RecordTransactionState() => AllCallsWereTransactional &= uow.IsInsideTransaction;
    }

    private sealed class RecordingOutboxWriter(RecordingUnitOfWork uow) : IOutboxWriter
    {
        public Task EnqueueAsync(IntegrationEvent integrationEvent, CancellationToken ct = default)
        {
            Assert.True(uow.IsInsideTransaction);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTokenGenerator : ITokenGenerator
    {
        public string NewToken(int byteLength = 32) => "new-token";
    }

    private sealed class FixedJwtTokenService : IJwtTokenService
    {
        public string Issue(ClaimsIdentity identity) => "access-token";
    }

    private sealed class AnonymousCurrentUser : ICurrentUser
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
}
