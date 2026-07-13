using Bedrock.Application.Behaviors;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using FluentValidation;
using Xunit;

namespace Bedrock.UnitTests.Application;

public sealed class ValidationDecoratorTests
{
    private sealed record Input(string Name);

    private sealed class InputValidator : AbstractValidator<Input>
    {
        public InputValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    private sealed class PassThroughUseCase : IUseCase<Input, string>
    {
        public Task<Result<string>> ExecuteAsync(Input input, CancellationToken ct = default) =>
            Task.FromResult(Result<string>.Success(input.Name));
    }

    private sealed class PassThroughCommand : ICommandUseCase<Input>
    {
        public string? PersistenceKey => null;

        public Task<Result> ExecuteAsync(Input input, CancellationToken ct = default) =>
            Task.FromResult(Result.Success());
    }

    [Fact]
    public async Task Invalid_input_should_short_circuit_with_validation_error()
    {
        var decorator = new ValidationUseCaseDecorator<Input, string>(
            new PassThroughUseCase(), [new InputValidator()]);

        var result = await decorator.ExecuteAsync(new Input(""));

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error.Code);
        Assert.NotNull(result.Error.Details);
        Assert.True(result.Error.Details!.ContainsKey("Name"));
    }

    [Fact]
    public async Task Valid_input_should_call_inner()
    {
        var decorator = new ValidationUseCaseDecorator<Input, string>(
            new PassThroughUseCase(), [new InputValidator()]);

        var result = await decorator.ExecuteAsync(new Input("ok"));

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Value);
    }

    [Fact]
    public async Task No_validators_should_pass_through()
    {
        var decorator = new ValidationUseCaseDecorator<Input, string>(new PassThroughUseCase(), []);

        var result = await decorator.ExecuteAsync(new Input(""));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Command_invalid_input_should_fail()
    {
        var decorator = new ValidationCommandUseCaseDecorator<Input>(
            new PassThroughCommand(), [new InputValidator()]);

        var result = await decorator.ExecuteAsync(new Input(""));

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error.Code);
    }
}
