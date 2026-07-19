using Bedrock.Api.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace ModuleName.Api;

/// <summary>Endpoint discovery seam; add versioned module routes here when the first use case exists.</summary>
public sealed class ModuleNameEndpointModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
    }
}
