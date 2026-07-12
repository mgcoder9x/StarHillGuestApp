using Bedrock.Domain.Results;
using Xunit;

namespace Bedrock.UnitTests.Domain;

public sealed class ResultTests
{
    [Fact]
    public void Success_should_have_no_error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_should_carry_error()
    {
        var error = Error.Validation("code", "message");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Failure_should_reject_null_error()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
    }

    [Fact]
    public void Failure_should_reject_error_none()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));
    }

    [Fact]
    public void SuccessT_should_carry_value()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void FailureT_value_access_should_throw()
    {
        var result = Result<int>.Failure(CommonErrors.NotFound("Room"));

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Implicit_conversion_from_value_should_succeed()
    {
        Result<string> result = "hello";

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Implicit_conversion_from_error_should_fail()
    {
        Result<string> result = CommonErrors.Conflict();

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Match_should_dispatch_to_correct_branch()
    {
        Result<int> success = 1;
        Result<int> failure = CommonErrors.Unexpected();

        Assert.Equal("ok", success.Match(_ => "ok", _ => "fail"));
        Assert.Equal("fail", failure.Match(_ => "ok", _ => "fail"));
    }

    [Fact]
    public void Error_WithDetails_should_attach_details_immutably()
    {
        var error = Error.Validation("v", "invalid");
        var details = new Dictionary<string, string[]> { ["name"] = ["required"] };

        var withDetails = error.WithDetails(details);

        Assert.Null(error.Details);
        Assert.NotNull(withDetails.Details);
        Assert.Equal(ErrorType.Validation, withDetails.Type);
    }
}
