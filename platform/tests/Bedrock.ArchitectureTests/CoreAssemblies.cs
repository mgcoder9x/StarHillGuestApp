using System.Reflection;
using Bedrock.Api.Endpoints;
using Bedrock.Application.Ports.Time;
using Bedrock.Domain.Results;
using Bedrock.Infrastructure.Persistence;
using Bedrock.Messaging.Contracts;
using Identity.Api;
using Identity.Application.RefreshToken;
using Identity.Contracts.Events;
using Identity.Domain;
using Identity.Infrastructure.Persistence;

namespace Bedrock.ArchitectureTests;

/// <summary>
/// Điểm truy cập các assembly lõi qua marker type (không hardcode tên assembly — refactor-safe).
/// </summary>
internal static class CoreAssemblies
{
    public static Assembly Domain => typeof(Result).Assembly;

    public static Assembly MessagingContracts => typeof(IntegrationEvent).Assembly;

    public static Assembly Application => typeof(IClock).Assembly;

    public static Assembly Infrastructure => typeof(PlatformDbContext).Assembly;

    public static Assembly Api => typeof(IEndpointModule).Assembly;

    /// <summary>Toàn bộ 5 assembly lõi <c>Bedrock.*</c> — để quét no-business-in-core (CP1) toàn diện.</summary>
    public static IReadOnlyList<Assembly> AllBedrock => [Domain, MessagingContracts, Application, Infrastructure, Api];
}

/// <summary>
/// Điểm truy cập assembly của module mẫu <c>Identity</c> (qua marker type public mỗi tầng) — để kiểm
/// CP4 (module boundary) + CP5 (single composition root) + CP11 trên module thật.
/// </summary>
internal static class ModuleAssemblies
{
    public static Assembly IdentityContracts => typeof(UserTokenRefreshedIntegrationEvent).Assembly;

    public static Assembly IdentityDomain => typeof(AuthErrors).Assembly;

    public static Assembly IdentityApplication => typeof(RefreshAccessTokenUseCase).Assembly;

    public static Assembly IdentityInfrastructure => typeof(IdentityDbContext).Assembly;

    public static Assembly IdentityApi => typeof(IdentityEndpointModule).Assembly;
}
