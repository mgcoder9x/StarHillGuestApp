namespace Bedrock.Application.DependencyInjection;

/// <summary>
/// Sổ đăng ký (accumulate lúc compose) cho startup validation (R13.1): các port BẮT BUỘC single-impl (thiếu →
/// chặn boot) + danh sách port ĐA-IMPL được phép (miễn duplicate-guard). Mỗi <c>AddXxxCore</c> tự đóng góp phần
/// của mình → thêm capability KHÔNG phải sửa validator (open/closed, sửa F7 gốc).
/// <para>
/// Đăng ký như MỘT singleton instance (KHÔNG <c>IOptions&lt;&gt;</c>) → chỉ cần
/// <c>Microsoft.Extensions.DependencyInjection.Abstractions</c> (đúng lớp abstraction đã whitelist AD-018), giữ
/// Application tối thiểu dependency (DV-012). Accumulate được vì mọi <c>AddXxxCore</c> chạy trước khi Build.
/// </para>
/// </summary>
public sealed class StartupValidationOptions
{
    private readonly HashSet<Type> _requiredPorts = [];
    private readonly HashSet<Type> _multiImplementationPorts = [];

    /// <summary>Các port single-impl bắt buộc phải có implementation lúc boot.</summary>
    public IReadOnlyCollection<Type> RequiredPorts => _requiredPorts;

    /// <summary>Các port CỐ Ý đa-implementation (IEnumerable) — duplicate-guard bỏ qua.</summary>
    public IReadOnlyCollection<Type> MultiImplementationPorts => _multiImplementationPorts;

    public StartupValidationOptions RequirePort(Type portType)
    {
        ArgumentNullException.ThrowIfNull(portType);
        _requiredPorts.Add(portType);
        return this;
    }

    public StartupValidationOptions AllowMultipleImplementations(Type portType)
    {
        ArgumentNullException.ThrowIfNull(portType);
        _multiImplementationPorts.Add(portType);
        return this;
    }
}
