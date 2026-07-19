using Xunit;

namespace Bedrock.ArchitectureTests;

public sealed class OperationalReadinessTests
{
    private static readonly string PlatformRoot = FindPlatformRoot();

    [Fact]
    public void Slo_runbook_recovery_and_public_api_policy_cover_required_controls()
    {
        AssertContains("operations/SLO.md",
            "bedrock.outbox.oldest_pending.age",
            "bedrock.outbox.dead_letter.depth",
            "RTO",
            "RPO");
        AssertContains("operations/RUNBOOK.md",
            "DryRun=true",
            "/v1/operations/outbox/replay/preview",
            "OperationId",
            "actor",
            "reason",
            "outbox_replay_operation",
            "outbox_replay_audit",
            "Inbox");
        AssertContains("operations/RECOVERY_DRILL.md",
            "Broker stop/restart",
            "Broker partition/pause",
            "Consumer cancellation/restart",
            "Duplicate delivery",
            "Poison event");
        AssertContains("contracts/PUBLIC_API_POLICY.md",
            "Semantic Versioning",
            "BEDROCK_UPDATE_PUBLIC_API",
            "Obsolete");
        AssertContains("operations/prometheus-alerts.yml",
            "BedrockOutboxOldestPendingWarning",
            "BedrockOutboxDeadLettered",
            "for:");
        AssertContains("operations/grafana-dashboard.json",
            "bedrock-platform-operations",
            "bedrock_outbox_pending",
            "bedrock_outbox_dead_letter_depth");
        AssertContains("RELEASE.md",
            "Semantic Versioning",
            "PublicApiCompatibilityTests",
            "SPDX SBOM",
            "provenance");
    }

    private static void AssertContains(string relativePath, params string[] fragments)
    {
        var path = Path.Combine(PlatformRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"Required platform policy file is missing: {relativePath}");
        var content = File.ReadAllText(path);
        foreach (var fragment in fragments)
        {
            Assert.Contains(fragment, content, StringComparison.Ordinal);
        }
    }

    private static string FindPlatformRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Platform.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Cannot find the platform root containing Platform.slnx.");
    }
}
