using Mono.Cecil;
using Xunit;

namespace Bedrock.ArchitectureTests;

/// <summary>
/// Discovers every module and adapter from disk, then validates the assembly references emitted by the compiler.
/// This complements project-file rules: a newly added project is covered without adding marker types or references
/// to this test project, and stale/unbuilt projects fail closed instead of silently escaping type-level checks.
/// </summary>
public sealed class CompiledAssemblyBoundaryTests
{
    private static readonly string PlatformRoot = FindPlatformRoot();

    [Fact]
    public void Every_discovered_module_and_adapter_compiled_assembly_respects_boundaries()
    {
        var modules = DiscoverModules();
        var violations = new List<string>();

        foreach (var module in modules)
        {
            using var assembly = ReadCompiledAssembly(module.ProjectPath, violations);
            if (assembly is null)
            {
                continue;
            }

            var references = assembly.MainModule.AssemblyReferences.Select(reference => reference.Name).ToArray();
            foreach (var reference in references)
            {
                ValidateModuleReference(module, reference, modules, violations);
            }
        }

        foreach (var project in DiscoverProjects(Path.Combine(PlatformRoot, "src", "Adapters")))
        {
            using var assembly = ReadCompiledAssembly(project, violations);
            if (assembly is null)
            {
                continue;
            }

            foreach (var reference in assembly.MainModule.AssemblyReferences.Select(item => item.Name))
            {
                if (reference is "Bedrock.Api" or "Bedrock.Infrastructure"
                    || modules.Any(module => reference.StartsWith(module.ModuleName + ".", StringComparison.Ordinal))
                    || reference.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                    || reference.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                    || reference.StartsWith("Npgsql", StringComparison.Ordinal))
                {
                    violations.Add($"{Relative(project)} -> {reference}: adapter compiled assembly crosses a forbidden boundary.");
                }
            }
        }

        Assert.Empty(violations);
    }

    private static void ValidateModuleReference(
        ModuleProject module,
        string reference,
        IReadOnlyCollection<ModuleProject> allModules,
        List<string> violations)
    {
        var otherModule = allModules
            .Select(item => item.ModuleName)
            .Distinct(StringComparer.Ordinal)
            .FirstOrDefault(name =>
                name != module.ModuleName && reference.StartsWith(name + ".", StringComparison.Ordinal));
        if (otherModule is not null && !reference.EndsWith(".Contracts", StringComparison.Ordinal))
        {
            violations.Add(
                $"{Relative(module.ProjectPath)} -> {reference}: compiled cross-module dependency must target *.Contracts.");
        }

        var forbidden = module.Layer switch
        {
            "Contracts" => IsAnyLayer(reference, "Domain", "Application", "Infrastructure", "Api")
                || reference is "Bedrock.Application" or "Bedrock.Infrastructure" or "Bedrock.Api",
            "Domain" or "Application" => IsAnyLayer(reference, "Infrastructure", "Api")
                || reference is "Bedrock.Infrastructure" or "Bedrock.Api",
            "Infrastructure" => reference.EndsWith(".Api", StringComparison.Ordinal)
                || reference == "Bedrock.Api",
            "Api" => reference.EndsWith(".Infrastructure", StringComparison.Ordinal)
                || reference == "Bedrock.Infrastructure",
            _ => true,
        };

        if (forbidden)
        {
            violations.Add(
                $"{Relative(module.ProjectPath)} -> {reference}: compiled reference is invalid for layer '{module.Layer}'.");
        }
    }

    private static AssemblyDefinition? ReadCompiledAssembly(string projectPath, List<string> violations)
    {
        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar));
        var targetFramework = baseDirectory.Name;
        var configuration = baseDirectory.Parent?.Name
            ?? throw new InvalidOperationException("Cannot determine the active build configuration.");
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        var assemblyPath = Path.Combine(
            Path.GetDirectoryName(projectPath)!,
            "bin",
            configuration,
            targetFramework,
            projectName + ".dll");

        if (!File.Exists(assemblyPath))
        {
            violations.Add(
                $"{Relative(projectPath)}: compiled assembly is missing for {configuration}/{targetFramework}; "
                + "run the solution build before architecture tests.");
            return null;
        }

        return AssemblyDefinition.ReadAssembly(new MemoryStream(File.ReadAllBytes(assemblyPath)));
    }

    private static ModuleProject[] DiscoverModules()
    {
        var root = Path.Combine(PlatformRoot, "src", "Modules");
        return DiscoverProjects(root)
            .Select(project =>
            {
                var moduleName = new DirectoryInfo(project).Parent?.Parent?.Name
                    ?? throw new InvalidOperationException($"Cannot determine module for '{project}'.");
                var projectName = Path.GetFileNameWithoutExtension(project);
                var prefix = moduleName + ".";
                var layer = projectName.StartsWith(prefix, StringComparison.Ordinal)
                    ? projectName[prefix.Length..]
                    : string.Empty;
                return new ModuleProject(project, moduleName, layer);
            })
            .ToArray();
    }

    private static IEnumerable<string> DiscoverProjects(string root) =>
        Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal);

    private static bool IsAnyLayer(string assemblyName, params string[] layers) =>
        layers.Any(layer => assemblyName.EndsWith("." + layer, StringComparison.Ordinal));

    private static string Relative(string path) => Path.GetRelativePath(PlatformRoot, path);

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

    private sealed record ModuleProject(string ProjectPath, string ModuleName, string Layer);
}
