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
    private readonly HashSet<Type> _outboxProducerContexts = [];
    private readonly HashSet<Type> _outboxDrainerContexts = [];

    /// <summary>Các port single-impl bắt buộc phải có implementation lúc boot.</summary>
    public IReadOnlyCollection<Type> RequiredPorts => _requiredPorts;

    /// <summary>Các port CỐ Ý đa-implementation (IEnumerable) — duplicate-guard bỏ qua.</summary>
    public IReadOnlyCollection<Type> MultiImplementationPorts => _multiImplementationPorts;

    /// <summary>
    /// P1-15: các DbContext đã bật OUTBOX PRODUCER (AddBedrockOutbox) — tức module CÓ ghi integration-event vào outbox.
    /// </summary>
    public IReadOnlyCollection<Type> OutboxProducerContexts => _outboxProducerContexts;

    /// <summary>
    /// P1-15: các DbContext đã đăng ký DISPATCHER WORKER (AddOutboxDispatcherWorker) — tức có tiến trình DRAIN outbox phát đi.
    /// </summary>
    public IReadOnlyCollection<Type> OutboxDrainerContexts => _outboxDrainerContexts;

    /// <summary>
    /// P1-15: TƯỜNG MINH cho phép có outbox producer mà KHÔNG có dispatcher worker (chế độ offline/dev/smoke).
    /// Mặc định <c>false</c> → producer-không-drainer chặn boot (chống tích lũy event im lặng). Đặt <c>true</c> chỉ
    /// khi đã CÓ Ý THỨC chấp nhận event không được phát (vd smoke test không cần broker).
    /// </summary>
    public bool OutboxWithoutDispatcherAllowed { get; private set; }

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

    /// <summary>P1-15: đánh dấu <paramref name="contextType"/> có outbox producer (AddBedrockOutbox).</summary>
    public StartupValidationOptions RegisterOutboxProducer(Type contextType)
    {
        ArgumentNullException.ThrowIfNull(contextType);
        _outboxProducerContexts.Add(contextType);
        return this;
    }

    /// <summary>P1-15: đánh dấu <paramref name="contextType"/> có dispatcher worker (AddOutboxDispatcherWorker).</summary>
    public StartupValidationOptions RegisterOutboxDrainer(Type contextType)
    {
        ArgumentNullException.ThrowIfNull(contextType);
        _outboxDrainerContexts.Add(contextType);
        return this;
    }

    /// <summary>P1-15: TƯỜNG MINH chấp nhận outbox producer không có drainer (offline/dev/smoke). Không thể đảo lại.</summary>
    public StartupValidationOptions AllowOutboxWithoutDispatcher()
    {
        OutboxWithoutDispatcherAllowed = true;
        return this;
    }
}
