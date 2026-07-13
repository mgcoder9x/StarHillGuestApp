namespace Adapters.Messaging.RabbitMq;

/// <summary>
/// Cấu hình SUBSCRIBER RabbitMQ (bind từ section <c>RabbitMqConsumer</c>). Kết nối + exchange dùng chung
/// <see cref="RabbitMqOptions"/> (publisher/consumer cùng broker + exchange). TOPOLOGY (queue/binding/prefetch/
/// nack) là quyết định APP/vận hành (N-063) — options này để Host khai tường minh, base KHÔNG áp mặc định nghiệp vụ.
/// </summary>
public sealed class RabbitMqConsumerOptions
{
    public const string SectionName = "RabbitMqConsumer";

    /// <summary>Tên queue (durable) subscriber tiêu thụ. Bắt buộc.</summary>
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// Tên consumer logic dùng cho khoá idempotency inbox <c>(message_id, consumer)</c>. Mặc định = <see cref="QueueName"/>
    /// nếu để trống. Ổn định qua các lần restart (đổi tên = mất dấu đã-xử-lý → xử lý lại).
    /// </summary>
    public string ConsumerName { get; set; } = string.Empty;

    /// <summary>
    /// DI key của integration-event dispatcher thuộc module sở hữu queue. Null/rỗng chỉ dành cho host
    /// single-context legacy; modular host phải đặt key để inbox/UoW/handler không resolve nhầm module.
    /// </summary>
    public string DispatcherServiceKey { get; set; } = string.Empty;

    /// <summary>Routing key/pattern bind queue vào topic exchange (vd <c>identity.#</c> hoặc <c>identity.user_token_refreshed</c>). Ít nhất một.</summary>
    public IList<string> RoutingKeys { get; } = [];

    /// <summary>QoS prefetch (số message chưa-ack tối đa mỗi consumer). Mỗi delivery xử lý trong SCOPE riêng → an toàn song song. Mặc định 10.</summary>
    public ushort PrefetchCount { get; set; } = 10;

    /// <summary>
    /// P0-02: số lần GIAO tối đa cho lỗi TRANSIENT (DB/network/timeout/handler tạm) trước khi vào final DLQ. Mỗi lần
    /// giao thất bại transient → retry có delay; đạt ngưỡng → quarantine. Mặc định 5. Lỗi PERMANENT (envelope/type/
    /// schema sai) KHÔNG retry (vào DLQ ngay). Phải >= 1.
    /// </summary>
    public int MaxDeliveryAttempts { get; set; } = 5;

    /// <summary>P0-02: delay giữa các lần retry transient (message chờ trong retry-queue TTL rồi quay lại). Mặc định 5s. Phải > 0.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Header mang số lần đã giao (app quản để đếm tin cậy, không phụ thuộc parse <c>x-death</c>).</summary>
    public const string AttemptHeader = "x-bedrock-attempt";

    /// <summary>Retry exchange custom. Rỗng = <c>{QueueName}.retry</c>, tránh cross-route giữa các consumer.</summary>
    public string RetryExchangeName { get; set; } = string.Empty;

    internal string EffectiveRetryExchangeName =>
        string.IsNullOrWhiteSpace(RetryExchangeName) ? QueueName + ".retry" : RetryExchangeName;

    /// <summary>Retry queue. Rỗng = <c>{QueueName}.retry</c>. Có <c>x-message-ttl</c>=RetryDelay + dead-letter về main exchange.</summary>
    public string RetryQueueName { get; set; } = string.Empty;

    internal string EffectiveRetryQueueName =>
        string.IsNullOrWhiteSpace(RetryQueueName) ? QueueName + ".retry" : RetryQueueName;

    /// <summary>Dead-letter exchange custom. Rỗng = <c>{QueueName}.dead-letter</c>, cô lập quarantine per queue.</summary>
    public string DeadLetterExchangeName { get; set; } = string.Empty;

    internal string EffectiveDeadLetterExchangeName =>
        string.IsNullOrWhiteSpace(DeadLetterExchangeName) ? QueueName + ".dead-letter" : DeadLetterExchangeName;

    /// <summary>Queue quarantine. Rỗng = tự dùng <c>{QueueName}.dead-letter</c>.</summary>
    public string DeadLetterQueueName { get; set; } = string.Empty;

    internal string EffectiveDeadLetterQueueName =>
        string.IsNullOrWhiteSpace(DeadLetterQueueName) ? QueueName + ".dead-letter" : DeadLetterQueueName;

    /// <summary>
    /// Đối số queue tuỳ chọn (vd <c>x-dead-letter-exchange</c> để poison đi vào DLX thay vì drop). Base KHÔNG áp
    /// DLX mặc định (topology app) — production NÊN cấu hình DLX vì khi handler lỗi subscriber NACK requeue=false.
    /// </summary>
    public IDictionary<string, object?> QueueArguments { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>Tên consumer hiệu lực (ConsumerName hoặc QueueName nếu trống).</summary>
    public string EffectiveConsumerName =>
        string.IsNullOrWhiteSpace(ConsumerName) ? QueueName : ConsumerName;

    /// <summary>Fail-fast (F35): cấu hình sai → chặn boot (validate-on-start ở AddRabbitMqConsumer).</summary>
    public static void Validate(RabbitMqConsumerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.QueueName))
        {
            throw new InvalidOperationException("RabbitMqConsumerOptions.QueueName rỗng.");
        }

        if (options.RoutingKeys.Count == 0)
        {
            throw new InvalidOperationException("RabbitMqConsumerOptions.RoutingKeys phải có ít nhất một binding.");
        }

        if (options.RoutingKeys.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("RabbitMqConsumerOptions.RoutingKeys chứa phần tử rỗng.");
        }

        if (options.PrefetchCount == 0)
        {
            throw new InvalidOperationException("RabbitMqConsumerOptions.PrefetchCount phải > 0.");
        }

        if (options.MaxDeliveryAttempts < 1)
        {
            throw new InvalidOperationException("RabbitMqConsumerOptions.MaxDeliveryAttempts phải >= 1.");
        }

        if (options.RetryDelay <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("RabbitMqConsumerOptions.RetryDelay phải > 0.");
        }

        ValidateOptionalName(options.RetryExchangeName, nameof(RetryExchangeName));
        ValidateOptionalName(options.DeadLetterExchangeName, nameof(DeadLetterExchangeName));
        ValidateOptionalName(options.RetryQueueName, nameof(RetryQueueName));
        ValidateOptionalName(options.DeadLetterQueueName, nameof(DeadLetterQueueName));
    }

    private static void ValidateOptionalName(string value, string propertyName)
    {
        // Empty means "derive from QueueName"; whitespace is almost always an accidental bad configuration value.
        if (value.Length > 0 && string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"RabbitMqConsumerOptions.{propertyName} chỉ chứa khoảng trắng.");
        }
    }
}
