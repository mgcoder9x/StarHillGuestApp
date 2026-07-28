using System.Globalization;
using System.Net;

namespace StarHill.Api;

/// <summary>
/// Chế độ health-probe cấp container (R1.1–R1.5). Docker HEALTHCHECK gọi <c>dotnet StarHill.Api.dll --healthcheck</c>
/// → chạy đúng nhánh này bằng runtime .NET có sẵn, KHÔNG cần cài package OS (curl/wget) vào runtime image (R1.5).
/// </summary>
internal static class HealthProbe
{
    private const int DefaultPort = 8080;

    /// <summary>
    /// GET <c>/health/live</c> qua HTTP localhost với timeout 3 giây (R1.2/R1.3). Trả <c>0</c> nếu HTTP 200,
    /// ngược lại <c>1</c>. Mọi exception (kể cả timeout <see cref="TaskCanceledException"/>) đều được nuốt và
    /// trả <c>1</c> để probe fail-closed — tuyệt đối không ném ra ngoài làm hỏng health check của container.
    /// </summary>
    /// <param name="args">Đối số dòng lệnh của process probe (đã khớp <c>--healthcheck</c> trước khi gọi).</param>
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var port = ResolvePort(Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS"));

            // HTTP (không HTTPS): Kestrel trong container nghe HTTP cổng ASPNETCORE_HTTP_PORTS (Dockerfile đặt 8080).
            var probeUri = new Uri(FormattableString.Invariant($"http://localhost:{port}/health/live"));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            using var response = await client.GetAsync(probeUri).ConfigureAwait(false);

            return response.StatusCode == HttpStatusCode.OK ? 0 : 1;
        }
        catch (Exception)
        {
            // Fail-closed: kết nối bị từ chối, timeout (TaskCanceledException), DNS lỗi... đều = unhealthy → exit 1.
            // Bắt Exception tổng quát là CHỦ ĐÍCH: probe chạy trong container, không được để bất kỳ lỗi nào thoát ra.
            return 1;
        }
    }

    /// <summary>
    /// Đọc cổng từ giá trị <c>ASPNETCORE_HTTP_PORTS</c> (có thể dạng <c>8080</c> hoặc <c>8080;8081</c> → lấy phần tử
    /// đầu). Trả <see cref="DefaultPort"/> nếu vắng, rỗng, không phải số, hoặc ngoài dải cổng hợp lệ.
    /// </summary>
    private static int ResolvePort(string? httpPortsValue)
    {
        if (string.IsNullOrWhiteSpace(httpPortsValue))
        {
            return DefaultPort;
        }

        var ports = httpPortsValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (ports.Length == 0
            || !int.TryParse(ports[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var port)
            || port is < 1 or > 65535)
        {
            return DefaultPort;
        }

        return port;
    }
}
