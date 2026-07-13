# Bedrock Platform Base

Reusable .NET modular-monolith foundation. The base owns mechanisms and neutral contracts; business policy stays
inside modules. `src/Host` is the only composition root allowed to connect API, Infrastructure, modules, and adapters.

## Dependency Rules

- `Bedrock.Domain` has no dependency on other Bedrock layers.
- `Bedrock.Application` owns ports, use-case contracts, and decorators; it does not reference EF, ASP.NET, or adapters.
- `Bedrock.Infrastructure` implements Application ports and does not reference `Bedrock.Api`.
- Module-to-module references may target only `*.Contracts`.
- Module `Api` projects cannot reference Infrastructure; module `Infrastructure` projects cannot reference API.
- Adapters may reference only `Bedrock.Application`, `Bedrock.Domain`, and `Bedrock.Messaging.Contracts`.
- `DiscoveredProjectBoundaryTests` scans every module and adapter project from disk, including newly added projects.

## Use Cases And Transactions

Use the semantic interface that matches the operation:

- `IQueryUseCase<TInput,TOutput>` for read-only operations.
- `ICommandUseCase<TInput,TOutput>` for writes returning a value.
- `ICommandUseCase<TInput>` for writes returning only success/failure.

Value-returning commands expose an `ITransactionalUseCase.PersistenceKey`. The key belongs to the implementation,
not the request DTO, so callers cannot select another module's DbContext. The transaction decorator resolves a keyed
`IUnitOfWork`; queries pass through without opening a transaction. A null key is supported only by legacy single-
DbContext hosts. Empty or whitespace keys fail fast.

Register all module use cases before calling `AddBedrockCore`. The pipeline order is:

```text
Logging -> Authorization -> Validation -> Idempotency -> Transaction -> UseCase
```

## Persistence Capabilities

`AddBedrockPersistence<TContext>` registers only the persistence foundation: DbContext, clock, domain-event dispatcher,
Unit of Work, resolver, repository foundation, and database readiness. Schema-dependent capabilities are explicit:

```csharp
services.AddBedrockPersistence<IdentityDbContext>(IdentityModule.PersistenceKey, configureDbContext);
services.AddBedrockOutbox<IdentityDbContext>(IdentityModule.PersistenceKey);
services.AddBedrockInbox<IdentityDbContext>(IdentityModule.PersistenceKey);
services.AddBedrockRefreshTokens<IdentityDbContext>(IdentityModule.PersistenceKey);
```

Only call a capability when the same DbContext maps its tables through `AddOutboxInbox` or `AddRefreshTokens`.
Capability registration validates the context/key pair and is idempotent for an identical registration.

## Messaging Guarantees

Outbox publishing is at-least-once. A dispatcher renews ownership before each publish and finalizes only while its
claim id still matches. Lost ownership records `bedrock.outbox.lease_lost`; consumers must retain Inbox deduplication.

RabbitMQ retry and dead-letter exchanges default to queue-specific names:

```text
{queue}.retry
{queue}.dead-letter
```

Each registered consumer gets a `rabbitmq-consumer:{queue}` readiness check. It is healthy only after topology setup
and broker consumer registration have produced a consumer tag. Recovery, startup, fault, and shutdown states are not
ready. Shutdown cancels broker consumption and waits for in-flight handlers before disposing channels.

## Startup Safety

JWT signing and verify-only hosts use the same key-ring validator from `Bedrock.Application`. Both paths register
`ValidateOnStart`, so an empty ring, unknown active key, duplicate key id, malformed Base64 secret, weak HS256 secret,
or missing issuer/audience prevents startup.

Run the complete verification gate from the platform directory:

```powershell
.\tools\verify.ps1 all
```

The CI validator selects a working `python` command or Windows `py -3` launcher automatically. Docker-backed tests
skip locally when Docker is unavailable and run as required in CI.
