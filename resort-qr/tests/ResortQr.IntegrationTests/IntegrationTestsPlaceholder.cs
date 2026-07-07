using Xunit;

namespace ResortQr.IntegrationTests;

/// <summary>
/// Integration test của base dùng Testcontainers (PostgreSQL 18) — hoãn tới khi máy có Docker
/// (xem ai-notes TK-034/035). Placeholder skipped để `dotnet test` không báo "No test available".
/// </summary>
public sealed class IntegrationTestsPlaceholder
{
    [Fact(Skip = "Integration tests cần Docker/Testcontainers (PostgreSQL) — hoãn tới khi có Docker.")]
    public void Deferred_until_docker_available()
    {
    }
}
