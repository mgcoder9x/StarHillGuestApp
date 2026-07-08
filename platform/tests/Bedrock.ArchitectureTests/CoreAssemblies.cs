using System.Reflection;
using Bedrock.Application.Ports.Time;
using Bedrock.Domain.Results;
using Bedrock.Infrastructure.Persistence;
using Bedrock.Messaging.Contracts;

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
}
