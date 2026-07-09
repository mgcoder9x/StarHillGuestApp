using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Api.Versioning;

/// <summary>
/// Cơ chế API versioning của base (design §9.1/§6, F32/R22.1): URL-segment <c>/v{version}</c>, mặc định v1,
/// báo version hỗ trợ qua header <c>api-supported-versions</c>. Base cấp CƠ CHẾ; module tự KHAI version cho
/// endpoint của mình qua <see cref="MapVersionedGroup"/> (đúng "per-endpoint versioning do module tự khai").
/// <para>
/// OpenAPI document-per-version (Swagger UI) là mối quan tâm TRÌNH BÀY ở Host — base hiện chưa dựng hạ tầng
/// OpenAPI; endpoint đã mang metadata <see cref="ApiVersion"/> để Host nhóm theo version khi bật OpenAPI (DV).
/// </para>
/// </summary>
public static class BedrockApiVersioning
{
    /// <summary>Version mặc định v1.0 (điểm tham chiếu chung, tránh mỗi module tự chế số).</summary>
    public static readonly ApiVersion V1 = new(1, 0);

    public static IServiceCollection AddBedrockApiVersioning(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = V1;
            options.AssumeDefaultVersionWhenUnspecified = true; // không có version trong reader → coi là v1
            options.ReportApiVersions = true;                   // trả header api-supported/deprecated-versions
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

        return services;
    }

    /// <summary>
    /// Tạo route group versioned với tiền tố <c>/v{version:apiVersion}{prefix}</c> + version set khai
    /// <paramref name="versions"/> (mặc định <see cref="V1"/>). Module gọi để versioning nhất quán một chỗ.
    /// </summary>
    public static RouteGroupBuilder MapVersionedGroup(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        params ApiVersion[] versions)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        var declared = versions is { Length: > 0 } ? versions : [V1];

        var setBuilder = endpoints.NewApiVersionSet();
        foreach (var version in declared)
        {
            setBuilder.HasApiVersion(version);
        }

        var versionSet = setBuilder.Build();

        return endpoints.MapGroup($"/v{{version:apiVersion}}{prefix}").WithApiVersionSet(versionSet);
    }
}
