using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Foundation.Application.Common;
using Foundation.SharedKernel.Results;
using Xunit;

namespace Foundation.UnitTests.Application;

public sealed class ValidationUseCaseDecoratorTests
{
    private sealed record SampleInput(string Name);

    private sealed class SampleValidator : AbstractValidator<SampleInput>
    {
        public SampleValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    private sealed class SpyUseCase : IUseCase<SampleInput, string>
    {
        public bool Executed { get; private set; }

        public Task<Result<string>> ExecuteAsync(SampleInput input, CancellationToken cancellationToken = default)
        {
            Executed = true;
            return Task.FromResult(Result.Ok("done"));
        }
    }

    [Fact]
    public async Task Valid_input_should_run_inner()
    {
        var inner = new SpyUseCase();
        var sut = new ValidationUseCaseDecorator<SampleInput, string>(inner, [new SampleValidator()]);

        var result = await sut.ExecuteAsync(new SampleInput("ok"));

        Assert.True(result.IsSuccess);
        Assert.Equal("done", result.Value);
        Assert.True(inner.Executed);
    }

    [Fact]
    public async Task Invalid_input_should_fail_with_field_errors_and_not_run_inner()
    {
        var inner = new SpyUseCase();
        var sut = new ValidationUseCaseDecorator<SampleInput, string>(inner, [new SampleValidator()]);

        var result = await sut.ExecuteAsync(new SampleInput(string.Empty));

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
        Assert.NotNull(result.Error.Details);
        Assert.True(result.Error.Details!.ContainsKey("Name"));
        Assert.False(inner.Executed);
    }

    [Fact]
    public async Task No_validators_should_pass_through()
    {
        var inner = new SpyUseCase();
        var sut = new ValidationUseCaseDecorator<SampleInput, string>(inner, []);

        var result = await sut.ExecuteAsync(new SampleInput(string.Empty));

        Assert.True(result.IsSuccess);
        Assert.True(inner.Executed);
    }
}
