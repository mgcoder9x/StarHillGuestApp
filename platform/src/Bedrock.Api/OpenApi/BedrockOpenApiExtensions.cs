using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Api.OpenApi;

/// <summary>
/// OpenAPI document-gen của base (design §6/§9.1, R22.1, F32) — hoàn tất phần base của DV-015. Dùng NATIVE
/// <c>Microsoft.AspNetCore.OpenApi</c> (.NET 10) → bề mặt phụ thuộc tối thiểu (đúng lo ngại DV-015: không nhồi
/// stack lớn). <b>OPT-IN</b>: Host gọi <see cref="AddBedrockOpenApi"/> khi muốn phơi spec; <c>UseBedrockApi</c>
/// tự map <c>/openapi/{doc}.json</c> KHI đã opt-in (phát hiện qua <see cref="BedrockOpenApiMarker"/>) — giữ
/// một-dòng cho Host. Swagger UI là tầng TRÌNH BÀY → vẫn do Host chọn (giữ tinh thần DV-015).
/// <para>
/// Base hiện chỉ có <see cref="Versioning.BedrockApiVersioning.V1"/> → MỘT document mặc định <c>"v1"</c>
/// (<c>/openapi/v1.json</c>) là đủ + tối thiểu; tách doc-per-version chỉ khi có v2 (I10 — thêm khi cần).
/// Endpoint versioned mang URL <c>/v{n}</c> nên spec phản ánh version ngay trong path.
/// </para>
/// </summary>
public static class BedrockOpenApiExtensions
{
    /// <summary>Bật OpenAPI doc-gen (native, document mặc định "v1"). Host gọi ở <c>AddBedrockApi</c>-time để opt-in.</summary>
    public static IServiceCollection AddBedrockOpenApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOpenApi(); // document mặc định "v1" → phục vụ tại /openapi/v1.json qua MapOpenApi.
        services.AddSingleton<BedrockOpenApiMarker>(); // marker để UseBedrockApi biết đã opt-in → tự map.
        return services;
    }

    /// <summary>Map endpoint OpenAPI (<c>/openapi/{documentName}.json</c>). Gọi bởi <c>UseBedrockApi</c> khi đã opt-in.</summary>
    public static IEndpointRouteBuilder MapBedrockOpenApi(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapOpenApi();
        return endpoints;
    }
}

/// <summary>Marker nội bộ: có mặt trong DI ⇔ Host đã gọi <see cref="BedrockOpenApiExtensions.AddBedrockOpenApi"/>.</summary>
internal sealed class BedrockOpenApiMarker;
