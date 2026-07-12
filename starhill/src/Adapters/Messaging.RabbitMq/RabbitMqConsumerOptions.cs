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

    /// <summary>Routing key/pattern bind queue vào topic exchange (vd <c>identity.#</c> hoặc <c>identity.user_token_refreshed</c>). Ít nhất một.</summary>
    public IList<string> RoutingKeys { get; } = [];

    /// <summary>QoS prefetch (số message chưa-ack tối đa mỗi consumer). Mỗi delivery xử lý trong SCOPE riêng → an toàn song song. Mặc định 10.</summary>
    public ushort PrefetchCount { get; set; } = 10;

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
    }
}
