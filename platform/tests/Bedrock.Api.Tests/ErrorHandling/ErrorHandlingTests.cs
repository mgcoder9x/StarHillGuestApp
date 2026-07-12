using Bedrock.Api.ErrorHandling;
using Bedrock.Domain.Results;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Bedrock.Api.Tests.ErrorHandling;

public sealed class ErrorHandlingTests
{
    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.RateLimited, StatusCodes.Status429TooManyRequests)]
    [InlineData(ErrorType.Failure, StatusCodes.Status500InternalServerError)]
    public void ToStatusCode_should_map_each_error_type(ErrorType type, int expected)
    {
        Assert.Equal(expected, ErrorTypeToHttp.ToStatusCode(type));
    }

    [Fact]
    public void Build_should_set_status_title_code_and_traceId()
    {
        var error = CommonErrors.NotFound("Room");

        var problem = ProblemDetailsBuilder.Build(error, "trace-123");

        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal(error.Message, problem.Title);
        Assert.Equal("Room.not_found", problem.Extensions["code"]);
        Assert.Equal("trace-123", problem.Extensions["traceId"]);
        Assert.False(problem.Extensions.ContainsKey("errors"));
    }

    [Fact]
    public void Build_should_attach_field_errors_for_validation()
    {
        var details = new Dictionary<string, string[]> { ["Name"] = ["required"] };
        var error = CommonErrors.Validation().WithDetails(details);

        var problem = ProblemDetailsBuilder.Build(error, "trace-xyz");

        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("validation_error", problem.Extensions["code"]);
        Assert.True(problem.Extensions.ContainsKey("errors"));
    }

    [Fact]
    public void Build_should_reject_null_error()
    {
        Assert.Throws<ArgumentNullException>(() => ProblemDetailsBuilder.Build(null!, "t"));
    }
}
