using Bedrock.Application.Authorization;
using Bedrock.Application.DependencyInjection;
using Bedrock.Application.Ports.Caching;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bedrock.UnitTests.Application;

/// <summary>
/// GUARD AD-037/AD-040 (anti-drift L3): dựng pipeline THẬT qua DI + Scrutor <c>AddBedrockCore</c> và kiểm chứng
/// THỨ TỰ decorator (§8): Logging → Authorization → Validation → Idempotency → Transaction → UseCase. Phép thử
/// mấu chốt: caller THIẾU QUYỀN + input KHÔNG hợp lệ → phải trả <c>forbidden</c> (không phải <c>validation_error</c>)
/// → chứng minh Authorization chạy TRƯỚC Validation (đảo Blueprint §14 — không lộ chi tiết validation cho caller
/// không có quyền).
/// </summary>
public sealed class PipelineOrderTests
{
    [RequirePermission("do.it")]
    private sealed record PipelineInput(string Name) : IIdempotentCommand
    {
        public string IdempotencyKey => Name;
    }

    private sealed class PipelineInputValidator : AbstractValidator<PipelineInput>
    {
        public PipelineInputValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    private sealed class QueryUseCase : IQueryUseCase<PipelineInput, string>
    {
        public Task<Result<string>> ExecuteAsync(PipelineInput input, CancellationToken ct = default) =>
            Task.FromResult(Result<string>.Success("ran"));
    }

    private sealed class CommandUseCase : ICommandUseCase<PipelineInput>
    {
        public string? PersistenceKey => null;

        public Task<Result> ExecuteAsync(PipelineInput input, CancellationToken ct = default) =>
            Task.FromResult(Result.Success());
    }

    private static ServiceProvider BuildProvider(
        string[] permissions,
        FakeUnitOfWork? uow = null,
        IIdempotencyStore? store = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ICurrentUser>(new FakeCurrentUser(permissions));
        services.AddSingleton(store ?? new FakeIdempotencyStore());
        var transactionUnitOfWork = uow ?? new FakeUnitOfWork();
        services.AddSingleton<IUnitOfWork>(transactionUnitOfWork);
        services.AddSingleton<IUnitOfWorkResolver>(new FakeUnitOfWorkResolver((null, transactionUnitOfWork)));
        services.AddTransient<IValidator<PipelineInput>, PipelineInputValidator>();
        services.AddTransient<IUseCase<PipelineInput, string>, QueryUseCase>();
        services.AddTransient<ICommandUseCase<PipelineInput>, CommandUseCase>();

        services.AddBedrockCore();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Unauthorized_and_invalid_input_returns_forbidden_proving_authz_before_validation()
    {
        using var provider = BuildProvider([]); // không có quyền.
        var useCase = provider.GetRequiredService<IUseCase<PipelineInput, string>>();

        var result = await useCase.ExecuteAsync(new PipelineInput("")); // Name rỗng = invalid.

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code); // Authz chặn TRƯỚC khi Validation kịp chạy.
    }

    [Fact]
    public async Task Authorized_but_invalid_input_returns_validation_error()
    {
        using var provider = BuildProvider(["do.it"]);
        var useCase = provider.GetRequiredService<IUseCase<PipelineInput, string>>();

        var result = await useCase.ExecuteAsync(new PipelineInput(""));

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error.Code); // qua Authz → Validation bắt.
    }

    [Fact]
    public async Task Authorized_valid_input_runs_use_case_then_duplicate_hits_idempotency()
    {
        using var provider = BuildProvider(["do.it"]);
        var useCase = provider.GetRequiredService<IUseCase<PipelineInput, string>>();
        var input = new PipelineInput("req-1");

        var first = await useCase.ExecuteAsync(input);
        var second = await useCase.ExecuteAsync(input);

        Assert.True(first.IsSuccess);
        Assert.Equal("ran", first.Value);
        Assert.True(second.IsFailure);
        Assert.Equal("idempotency_conflict", second.Error.Code);
    }

    [Fact]
    public async Task Command_family_wraps_transaction_after_passing_authz_and_validation()
    {
        var uow = new FakeUnitOfWork();
        using var provider = BuildProvider(["do.it"], uow);
        var command = provider.GetRequiredService<ICommandUseCase<PipelineInput>>();

        var result = await command.ExecuteAsync(new PipelineInput("req-2"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, uow.TransactionCalls); // Transaction behavior đã bọc command (AD-040).
    }

    [Fact]
    public async Task Command_family_unauthorized_short_circuits_before_transaction()
    {
        var uow = new FakeUnitOfWork();
        using var provider = BuildProvider([], uow);
        var command = provider.GetRequiredService<ICommandUseCase<PipelineInput>>();

        var result = await command.ExecuteAsync(new PipelineInput("req-3"));

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
        Assert.Equal(0, uow.TransactionCalls); // Authz (ngoài) chặn trước Transaction (trong) → không mở transaction.
    }
}
