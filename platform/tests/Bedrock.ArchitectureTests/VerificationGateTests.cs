using Xunit;

namespace Bedrock.ArchitectureTests;

/// <summary>Guards the local/CI verification entry points against false-green failures (AD-104).</summary>
public sealed class VerificationGateTests
{
    [Fact]
    public void AD104_verification_scripts_fail_closed_and_ci_runs_the_validator()
    {
        var root = FindRepositoryRoot();
        var verify = File.ReadAllText(Path.Combine(root, "platform", "tools", "verify.ps1"));
        var validator = File.ReadAllText(Path.Combine(root, "platform", "tests", "validate_ci.py"));
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));

        Assert.Contains("$ErrorActionPreference = 'Stop'", verify);
        Assert.Contains("[Console]::OutputEncoding", verify);
        Assert.Contains("Resolve-NativeCommand", verify);
        Assert.Contains("Add-BlockedStep", verify);
        Assert.Contains("validate_without_yaml", validator);
        Assert.Contains("run: python tests/validate_ci.py", workflow);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".github", "workflows", "ci.yml")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
