using Bedrock.Api.ErrorHandling;
using Bedrock.Api.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bedrock.Api.Tests.ErrorHandling;

/// <summary>
/// A-27 guard: <see cref="ExceptionHandlingMiddleware"/> phân biệt CLIENT-ABORT với lỗi server:
/// request bị client huỷ (<c>OperationCanceledException</c> + <c>RequestAborted.IsCancellationRequested</c>) →
/// NUỐT ÊM (không log Error, không ghi 500 lên connection đã đóng); mọi lỗi khác (kể cả OCE KHÔNG do abort) → 500
/// ProblemDetails. Đây là fix bản chất: catch-all cũ coi cancellation là 500 → noise + status sai + ghi lên
/// connection đã đóng.
/// </summary>
public sealed class ExceptionHandlingMiddlewareTests
{
    private static ExceptionHandlingMiddleware Create(RequestDelegate next) =>
        new(next, NullLogger<ExceptionHandlingMiddleware>.Instance, new PathMasker(new ObservabilityOptions()));

    [Fact]
    public async Task Client_aborted_cancellation_is_swallowed_not_500()
    {
        var middleware = Create(_ => throw new OperationCanceledException());
        var context = new DefaultHttpContext { RequestAborted = new CancellationToken(canceled: true) };

        await middleware.InvokeAsync(context); // KHÔNG được ném

        Assert.NotEqual(StatusCodes.Status500InternalServerError, context.Response.StatusCode); // không ghi 500
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);                     // status mặc định giữ nguyên
    }

    [Fact]
    public async Task Cancellation_not_caused_by_abort_becomes_500() // `when` filter phân biệt đúng
    {
        var middleware = Create(_ => throw new OperationCanceledException());
        var context = new DefaultHttpContext();       // RequestAborted mặc định = None (KHÔNG cancelled)
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task Unhandled_exception_becomes_500_problem_details()
    {
        var middleware = Create(_ => throw new InvalidOperationException("boom"));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task Bad_http_request_becomes_400_not_500()
    {
        // Framework binding/parse (thiếu required route/query param, body JSON hỏng) ném BadHttpRequestException →
        // PHẢI là 400 (lỗi client), KHÔNG rơi vào catch(Exception) chung thành 500 (lỗi server giả).
        var middleware = Create(_ => throw new BadHttpRequestException(
            "Required parameter \"Guid roomId\" was not provided from query string."));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }
}
