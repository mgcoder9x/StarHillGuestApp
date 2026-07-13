using System.Xml.Linq;
using Xunit;

namespace Bedrock.ArchitectureTests;

/// <summary>
/// Project-graph guard discovered from disk. Unlike marker-type assembly tests, this automatically covers new
/// modules and adapters before someone remembers to add a compile-time reference to the architecture test project.
/// </summary>
public sealed class DiscoveredProjectBoundaryTests
{
    private static readonly string PlatformRoot = FindPlatformRoot();

    [Fact]
    public void Every_discovered_module_project_respects_layer_and_cross_module_boundaries()
    {
        var moduleRoot = Path.Combine(PlatformRoot, "src", "Modules");
        var violations = new List<string>();

        foreach (var project in Directory.EnumerateFiles(moduleRoot, "*.csproj", SearchOption.AllDirectories))
        {
            var module = new DirectoryInfo(project).Parent?.Parent?.Name
                ?? throw new InvalidOperationException($"Không xác định được module của '{project}'.");
            var projectName = Path.GetFileNameWithoutExtension(project);
            var expectedPrefix = module + ".";
            if (!projectName.StartsWith(expectedPrefix, StringComparison.Ordinal))
            {
                violations.Add($"{Relative(project)}: project phải bắt đầu bằng '{expectedPrefix}'.");
                continue;
            }

            var layer = projectName[expectedPrefix.Length..];
            if (layer is not ("Contracts" or "Domain" or "Application" or "Infrastructure" or "Api"))
            {
                violations.Add($"{Relative(project)}: layer '{layer}' không thuộc module layout chuẩn.");
                continue;
            }

            foreach (var reference in ReadProjectReferences(project))
            {
                ValidateModuleReference(module, layer, project, reference, violations);
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Every_discovered_adapter_only_references_neutral_bedrock_contracts()
    {
        var adapterRoot = Path.Combine(PlatformRoot, "src", "Adapters");
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "Bedrock.Application",
            "Bedrock.Domain",
            "Bedrock.Messaging.Contracts",
        };
        var violations = new List<string>();

        foreach (var project in Directory.EnumerateFiles(adapterRoot, "*.csproj", SearchOption.AllDirectories))
        {
            foreach (var reference in ReadProjectReferences(project))
            {
                var referencedName = Path.GetFileNameWithoutExtension(reference);
                if (!allowed.Contains(referencedName))
                {
                    violations.Add(
                        $"{Relative(project)} -> {Relative(reference)}: adapter chỉ được reference "
                        + "Bedrock.Application/Domain/Messaging.Contracts.");
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Every_discovered_adapter_forbids_technology_package_and_framework_references()
    {
        // Bổ sung CP3 ở tầng MANIFEST (bắt trước phân tích assembly, phủ adapter MỚI): adapter KHÔNG được kéo
        // SDK công nghệ hạ tầng (EF/Npgsql) hay ASP.NET framework — tech CHỈ sống trong adapter qua client SDK của
        // chính nó (vd RabbitMQ.Client), không phải EF/ASP.NET (I2/§17).
        var adapterRoot = Path.Combine(PlatformRoot, "src", "Adapters");
        var violations = new List<string>();

        foreach (var project in Directory.EnumerateFiles(adapterRoot, "*.csproj", SearchOption.AllDirectories))
        {
            foreach (var package in ReadIncludes(project, "PackageReference"))
            {
                if (package.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                    || package.StartsWith("Npgsql", StringComparison.Ordinal)
                    || package.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal))
                {
                    violations.Add($"{Relative(project)}: PackageReference '{package}' bị cấm trong adapter (EF/Npgsql/ASP.NET).");
                }
            }

            foreach (var framework in ReadIncludes(project, "FrameworkReference"))
            {
                violations.Add($"{Relative(project)}: FrameworkReference '{framework}' bị cấm trong adapter (không kéo ASP.NET/runtime framework).");
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Every_discovered_module_and_adapter_project_is_registered_in_solution()
    {
        // Module/adapter mới BỊ BỎ QUÊN khỏi Platform.slnx = không build/không CI = "xanh giả". Guard đọc slnx +
        // đối chiếu mọi project src dưới Modules/ và Adapters/ phải có mặt.
        var solutionProjects = ReadSolutionProjectPaths();
        var roots = new[]
        {
            Path.Combine(PlatformRoot, "src", "Modules"),
            Path.Combine(PlatformRoot, "src", "Adapters"),
        };
        var violations = new List<string>();

        foreach (var root in roots)
        {
            foreach (var project in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories))
            {
                if (!solutionProjects.Contains(Path.GetFullPath(project)))
                {
                    violations.Add($"{Relative(project)}: chưa được thêm vào Platform.slnx (sẽ không build/không được CI kiểm).");
                }
            }
        }

        Assert.Empty(violations);
    }

    private static void ValidateModuleReference(
        string module,
        string layer,
        string project,
        string reference,
        List<string> violations)
    {
        var referencedName = Path.GetFileNameWithoutExtension(reference);
        var referencedModule = ModuleName(reference);
        if (referencedModule is not null
            && !string.Equals(referencedModule, module, StringComparison.Ordinal)
            && !referencedName.EndsWith(".Contracts", StringComparison.Ordinal))
        {
            violations.Add(
                $"{Relative(project)} -> {Relative(reference)}: cross-module chỉ được đi qua *.Contracts.");
        }

        var forbidden = layer switch
        {
            "Contracts" => IsAnyLayer(referencedName, "Domain", "Application", "Infrastructure", "Api")
                || referencedName is "Bedrock.Application" or "Bedrock.Infrastructure" or "Bedrock.Api",
            "Domain" or "Application" => IsAnyLayer(referencedName, "Infrastructure", "Api")
                || referencedName is "Bedrock.Infrastructure" or "Bedrock.Api",
            "Infrastructure" => referencedName.EndsWith(".Api", StringComparison.Ordinal)
                || referencedName == "Bedrock.Api",
            "Api" => referencedName.EndsWith(".Infrastructure", StringComparison.Ordinal)
                || referencedName == "Bedrock.Infrastructure",
            _ => true,
        };

        if (forbidden)
        {
            violations.Add(
                $"{Relative(project)} -> {Relative(reference)}: reference không hợp lệ cho layer '{layer}'.");
        }
    }

    private static bool IsAnyLayer(string projectName, params string[] layers) =>
        layers.Any(layer => projectName.EndsWith("." + layer, StringComparison.Ordinal));

    private static string? ModuleName(string projectPath)
    {
        var modulesRoot = Path.GetFullPath(Path.Combine(PlatformRoot, "src", "Modules"));
        var fullPath = Path.GetFullPath(projectPath);
        if (!fullPath.StartsWith(modulesRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Path.GetRelativePath(modulesRoot, fullPath)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
    }

    private static IEnumerable<string> ReadProjectReferences(string projectPath)
    {
        var document = XDocument.Load(projectPath, LoadOptions.SetLineInfo);
        return document.Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath)!, include!)));
    }

    /// <summary>Đọc thuộc tính Include của một loại item (vd PackageReference/FrameworkReference) trong .csproj.</summary>
    private static IEnumerable<string> ReadIncludes(string projectPath, string elementName)
    {
        var document = XDocument.Load(projectPath);
        return document.Descendants(elementName)
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!);
    }

    /// <summary>Tập full-path mọi project khai trong Platform.slnx (định dạng slnx: &lt;Project Path="..." /&gt;).</summary>
    private static HashSet<string> ReadSolutionProjectPaths()
    {
        var solutionPath = Path.Combine(PlatformRoot, "Platform.slnx");
        var document = XDocument.Load(solutionPath);
        return document.Descendants("Project")
            .Select(element => element.Attribute("Path")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(Path.Combine(PlatformRoot, path!.Replace('\\', Path.DirectorySeparatorChar))))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

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

        throw new InvalidOperationException("Không tìm thấy platform root chứa Platform.slnx.");
    }
}
