using Bedrock.Api.ErrorHandling;
using NetArchTest.Rules;
using Xunit;

namespace Bedrock.Api.Tests.Architecture;

/// <summary>
/// CP2 / luật 2 (F14/I7): <c>Bedrock.Api</c> KHÔNG được phụ thuộc <c>*.Infrastructure</c> — Api chỉ là cơ chế
/// HTTP, composition với Infrastructure chỉ ở Host. ANTI-DRIFT: nếu ai đó lỡ thêm ref Infrastructure vào Api
/// (kéo EF/Npgsql theo package Api), test này FAIL BUILD. Hiện Infrastructure chưa tồn tại → luật đúng, nhưng
/// guard sẵn sàng bắt ngay khi Infrastructure ra đời.
/// </summary>
public sealed class ApiBoundaryTests
{
    private static System.Reflection.Assembly ApiAssembly => typeof(ErrorTypeToHttp).Assembly;

    [Fact]
    public void Api_should_not_depend_on_infrastructure()
    {
        var result = Types.InAssembly(ApiAssembly)
            .Should()
            .NotHaveDependencyOn("Bedrock.Infrastructure")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Bedrock.Api KHÔNG được ref Infrastructure (F14). Type vi phạm: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Api_should_not_depend_on_ef_or_npgsql()
    {
        var result = Types.InAssembly(ApiAssembly)
            .Should()
            .NotHaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Npgsql")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    // NEGATIVE CONTROL: engine phải phát hiện dependency có thật của Api (Api ref Bedrock.Application).
    [Fact]
    public void Control_engine_detects_real_dependency_api_on_application()
    {
        var result = Types.InAssembly(ApiAssembly)
            .Should()
            .NotHaveDependencyOn("Bedrock.Application")
            .GetResult();

        Assert.False(result.IsSuccessful);
    }
}
