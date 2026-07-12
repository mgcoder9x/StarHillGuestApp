using Bedrock.Application.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bedrock.UnitTests.Application;

/// <summary>
/// A-12/AD-085 — <c>AddBedrockCore</c> là IDEMPOTENCY-GUARDED: chỉ được gọi MỘT lần ở composition root. Gọi lần 2
/// NÉM (fail-loud) vì sẽ bọc pipeline behaviors HAI lớp (validation/authorization/idempotency/transaction chạy 2
/// lần = sai nghiêm trọng) + đăng ký validator trùng. Khoá hành vi này để chống hồi quy (KEYSTONE anti-drift).
/// </summary>
public sealed class BedrockCoreIdempotencyTests
{
    [Fact]
    public void AddBedrockCore_is_callable_once()
    {
        var services = new ServiceCollection();

        var returned = services.AddBedrockCore();

        Assert.Same(services, returned); // trả về chính collection (fluent), không ném.
    }

    [Fact]
    public void AddBedrockCore_called_twice_throws_fail_loud()
    {
        var services = new ServiceCollection();
        services.AddBedrockCore();

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddBedrockCore());

        Assert.Contains("AddBedrockCore đã được gọi", ex.Message, StringComparison.Ordinal);
    }
}
