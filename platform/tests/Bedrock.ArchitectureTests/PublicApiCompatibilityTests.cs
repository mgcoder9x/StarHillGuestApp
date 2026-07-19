using System.Reflection;
using Bedrock.Api.Versioning;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Bedrock.Infrastructure.Persistence;
using Bedrock.Messaging.Contracts;
using Xunit;

namespace Bedrock.ArchitectureTests;

/// <summary>
/// Public API contract for reusable Bedrock assemblies. The checked-in shipped snapshot is intentionally strict:
/// additions and removals require an explicit baseline update in the same change, which makes versioning/deprecation
/// review visible instead of allowing accidental source-level API drift.
/// </summary>
public sealed class PublicApiCompatibilityTests
{
    private static readonly Assembly[] SupportedAssemblies =
    [
        typeof(Result).Assembly,
        typeof(IntegrationEvent).Assembly,
        typeof(PagedRequest).Assembly,
        typeof(BedrockApiVersioning).Assembly,
        typeof(PlatformDbContext).Assembly,
        typeof(Adapters.Messaging.RabbitMq.RabbitMqOptions).Assembly,
    ];

    [Fact]
    public void Supported_assembly_public_surfaces_match_shipped_snapshots()
    {
        var root = FindPlatformRoot();
        var baselineDirectory = Path.Combine(root, "contracts", "public-api");
        var update = string.Equals(
            Environment.GetEnvironmentVariable("BEDROCK_UPDATE_PUBLIC_API"),
            "1",
            StringComparison.Ordinal);
        var failures = new List<string>();

        foreach (var assembly in SupportedAssemblies.OrderBy(item => item.GetName().Name, StringComparer.Ordinal))
        {
            var assemblyName = assembly.GetName().Name
                ?? throw new InvalidOperationException("Supported assembly has no name.");
            var expectedPath = Path.Combine(baselineDirectory, assemblyName + ".txt");
            var actual = PublicApiSurfaceRenderer.RenderAssembly(assembly);

            if (update)
            {
                Directory.CreateDirectory(baselineDirectory);
                File.WriteAllLines(expectedPath, actual);
                continue;
            }

            if (!File.Exists(expectedPath))
            {
                failures.Add($"{Relative(expectedPath, root)} is missing; run with BEDROCK_UPDATE_PUBLIC_API=1.");
                continue;
            }

            var expected = File.ReadAllLines(expectedPath);
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
            {
                failures.Add($"{Relative(expectedPath, root)} does not match the compiled public surface.");
            }
        }

        Assert.Empty(failures);
    }

    private static string Relative(string path, string root) => Path.GetRelativePath(root, path);

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
