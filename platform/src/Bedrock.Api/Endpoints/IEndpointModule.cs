using Microsoft.AspNetCore.Routing;

namespace Bedrock.Api.Endpoints;

/// <summary>
/// Hợp đồng discovery endpoint (design §6, thay "endpoint/module discovery" mơ hồ): mỗi module Api khai một
/// class implement interface này và đăng ký instance vào DI (trong <c>AddXModule</c>). Host gọi
/// <c>MapBedrockApi</c> → resolve <c>IEnumerable&lt;IEndpointModule&gt;</c> và gọi <see cref="MapEndpoints"/> —
/// KHÔNG reflection-scan mờ ám, "thêm module = 1 dòng" có cơ chế cụ thể + testable (F1/F13/F30).
/// </summary>
public interface IEndpointModule
{
    /// <summary>Map endpoint của module; per-endpoint policy/versioning do module tự khai.</summary>
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
