using System;
using ResortQr.SharedKernel.Results;
using Xunit;

namespace ResortQr.UnitTests.SharedKernel;

public sealed class ResultTests
{
    [Fact]
    public void Ok_should_be_success_and_carry_value()
    {
        var result = Result.Ok(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Fail_should_be_failure_and_carry_error()
    {
        var error = CommonErrors.NotFound();
        var result = Result.Fail<int>(error);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void Reading_value_on_failure_should_throw()
    {
        var result = Result.Fail<int>(CommonErrors.Validation());

        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Fail_with_null_error_should_throw()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Fail<int>(null!));
    }

    [Fact]
    public void NonGeneric_ok_and_fail_should_behave()
    {
        Assert.True(Result.Ok().IsSuccess);

        var fail = Result.Fail(CommonErrors.Conflict());
        Assert.True(fail.IsFailure);
        Assert.Equal("conflict", fail.Error!.Code);
        Assert.Equal(ErrorType.Conflict, fail.Error.Type);
    }
}
