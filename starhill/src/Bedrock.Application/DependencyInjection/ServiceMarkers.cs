namespace Bedrock.Application.DependencyInjection;

/// <summary>Đánh dấu implementation đăng ký lifetime Scoped (DI convention qua auto-scan — F6/F18).</summary>
#pragma warning disable CA1040 // Marker interface là chủ đích cho DI convention.
public interface IScopedService;

/// <summary>Đánh dấu implementation đăng ký lifetime Singleton.</summary>
public interface ISingletonService;

/// <summary>Đánh dấu implementation đăng ký lifetime Transient.</summary>
public interface ITransientService;

/// <summary>
/// Marker TƯỜNG MINH (F6): implementation cần đăng ký thủ công (vd hard-require DbContext) → LOẠI khỏi
/// auto-scan. Thay cho việc dựa vào vị trí namespace (<c>NotInNamespaceOf</c>) vốn dễ vỡ.
/// </summary>
public interface IManualRegistration;
#pragma warning restore CA1040
