using NetArchTest.Rules;
using Xunit;

namespace ResortQr.ArchitectureTests;

/// <summary>
/// Kiểm chứng dependency rule của base (Clean Architecture / modular monolith).
/// Tầng runtime (reflection) bổ trợ enforce tĩnh ở csproj — bắt vi phạm tinh vi (using rò rỉ).
/// </summary>
public sealed class DependencyRuleTests
{
    private static readonly System.Reflection.Assembly SharedKernel = typeof(ResortQr.SharedKernel.AssemblyMarker).Assembly;
    private static readonly System.Reflection.Assembly Domain = typeof(ResortQr.Domain.AssemblyMarker).Assembly;
    private static readonly System.Reflection.Assembly Application = typeof(ResortQr.Application.AssemblyMarker).Assembly;

    private const string DomainNs = "ResortQr.Domain";
    private const string ApplicationNs = "ResortQr.Application";
    private const string InfrastructureNs = "ResortQr.Infrastructure";
    private const string ApiNs = "ResortQr.Api";
    private const string EfCoreNs = "Microsoft.EntityFrameworkCore";
    private const string AspNetCoreNs = "Microsoft.AspNetCore";

    private static void AssertOk(TestResult result)
    {
        var failing = result.FailingTypeNames is null ? "" : string.Join(", ", result.FailingTypeNames);
        Assert.True(result.IsSuccessful, $"Vi phạm dependency rule ở các type: {failing}");
    }

    [Fact] // Domain KHÔNG phụ thuộc EF/ASP.NET/Infrastructure/Api
    public void Domain_should_not_depend_on_outer_layers()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot().HaveDependencyOnAny(InfrastructureNs, ApiNs, EfCoreNs, AspNetCoreNs)
            .GetResult();
        AssertOk(result);
    }

    [Fact] // Application KHÔNG phụ thuộc Api hoặc Infrastructure (chỉ khai port)
    public void Application_should_not_depend_on_api_or_infrastructure()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot().HaveDependencyOnAny(ApiNs, InfrastructureNs)
            .GetResult();
        AssertOk(result);
    }

    [Fact] // Application không phụ thuộc EF/ASP.NET (giữ tầng Application thuần, chỉ interface)
    public void Application_should_not_depend_on_efcore_or_aspnetcore()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot().HaveDependencyOnAny(EfCoreNs, AspNetCoreNs)
            .GetResult();
        AssertOk(result);
    }

    [Fact] // SharedKernel sạch tuyệt đối — không phụ thuộc project khác/EF/ASP.NET
    public void SharedKernel_should_be_clean()
    {
        var result = Types.InAssembly(SharedKernel)
            .ShouldNot().HaveDependencyOnAny(DomainNs, ApplicationNs, InfrastructureNs, ApiNs, EfCoreNs, AspNetCoreNs)
            .GetResult();
        AssertOk(result);
    }

    [Fact] // META: chứng minh harness KHÔNG "luôn xanh" (chống false-green).
    public void Harness_detects_violations_negative_control()
    {
        // Sự thật: SharedKernel KHÔNG phụ thuộc Domain. Yêu cầu luật SAI "phải phụ thuộc Domain"
        // => rule PHẢI thất bại => chứng minh NetArchTest thực sự inspect dependency thật.
        var result = Types.InAssembly(SharedKernel)
            .Should().HaveDependencyOn(DomainNs)
            .GetResult();
        Assert.False(result.IsSuccessful);
    }
}
