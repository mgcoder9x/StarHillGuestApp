using Xunit;

namespace StarHill.ArchitectureTests;

/// <summary>
/// C1.4 + C3.5 (R1.10, R3.6) — Guard TĨNH cho hardening hạ tầng build của Host: đọc TEXT hai artifact ở
/// repo (Dockerfile + docker-compose.yml) và khoá bốn bất biến, KHÔNG cần Docker, KHÔNG ProjectReference:
///   1) Dockerfile khai <c>HEALTHCHECK</c> đủ 4 tham số (interval/timeout/retries/start-period).
///   2) MỌI dòng <c>FROM</c> trong Dockerfile ghim digest <c>@sha256:</c> (R3.6, build tái lập).
///   3) Stage <c>runtime</c> (từ <c>FROM ... AS runtime</c> tới hết file) KHÔNG cài package OS (R1.5).
///   4) Service <c>host</c> trong compose khai <c>healthcheck</c> cùng bộ tham số (R1.9).
/// Guard-first: ở trạng thái hiện tại các artifact CHƯA hardening nên các [Fact] này FAIL — đỏ chính là
/// bằng chứng cổng bắt được vi phạm. Task 1.3 sẽ sửa artifact cho xanh sau khi có Docker tra digest thật.
/// </summary>
public sealed class DockerfileHardeningGuardTests
{
    private static readonly string[] DockerfilePathSegments =
        { "starhill", "src", "Host", "StarHill.Api", "Dockerfile" };

    private static readonly string[] ComposePathSegments =
        { "starhill", "docker-compose.yml" };

    [Fact]
    public void Dockerfile_declares_healthcheck_with_required_params()
    {
        var dockerfile = ReadRepositoryFile(DockerfilePathSegments);

        Assert.Contains("HEALTHCHECK", dockerfile);
        Assert.Contains("--interval=10s", dockerfile);
        Assert.Contains("--timeout=3s", dockerfile);
        Assert.Contains("--retries=3", dockerfile);
        Assert.Contains("--start-period=20s", dockerfile);
    }

    [Fact]
    public void Dockerfile_pins_every_base_image_by_digest()
    {
        var dockerfile = ReadRepositoryFile(DockerfilePathSegments);

        var fromLines = dockerfile
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("FROM ", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(fromLines);
        Assert.All(fromLines, line => Assert.Contains("@sha256:", line));
    }

    [Fact]
    public void Dockerfile_runtime_stage_installs_no_os_packages()
    {
        var dockerfile = ReadRepositoryFile(DockerfilePathSegments);
        var runtimeStage = ExtractStageFromMarkerToEnd(dockerfile, " AS runtime");

        Assert.DoesNotContain("apt-get install", runtimeStage);
        Assert.DoesNotContain("apk add", runtimeStage);
        Assert.DoesNotContain("yum install", runtimeStage);
    }

    [Fact]
    public void Compose_host_service_declares_healthcheck()
    {
        var compose = ReadRepositoryFile(ComposePathSegments);
        // `host` là service cuối trong file → khối của nó chạy từ `  host:` (thụt 2 cấp) tới hết file.
        // Cô lập để không vô tình khớp `healthcheck` của service `postgres` phía trên.
        var hostService = ExtractStageFromMarkerToEnd(compose, "\n  host:");

        Assert.Contains("healthcheck:", hostService);
        Assert.Contains("interval: 10s", hostService);
        Assert.Contains("timeout: 3s", hostService);
        Assert.Contains("retries: 3", hostService);
        Assert.Contains("start_period: 20s", hostService);
    }

    private static string ReadRepositoryFile(string[] segments)
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(new[] { root }.Concat(segments).ToArray());
        return File.ReadAllText(path);
    }

    /// <summary>Trả về đoạn text từ lần xuất hiện đầu tiên của <paramref name="marker"/> tới hết chuỗi.</summary>
    private static string ExtractStageFromMarkerToEnd(string content, string marker)
    {
        var index = content.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Không tìm thấy mốc '{marker}' — artifact chưa hardening đúng kỳ vọng.");
        return content[index..];
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".github", "workflows", "starhill-ci.yml")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
