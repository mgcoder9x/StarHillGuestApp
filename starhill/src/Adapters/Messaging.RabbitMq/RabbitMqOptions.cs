namespace Adapters.Messaging.RabbitMq;

/// <summary>
/// Cấu hình adapter RabbitMQ (bind từ section <c>RabbitMq</c>). Secret (Password) nạp từ user-secrets/env/Key Vault
/// ở prod (F35) — KHÔNG commit. Resilience per-adapter (design §9.2).
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    /// <summary>Exchange (topic) publish tới; routing key = <c>EventType</c>.</summary>
    public string ExchangeName { get; set; } = "bedrock.events";

    public RabbitMqResilienceOptions Resilience { get; set; } = new();

    /// <summary>Fail-fast (F35): cấu hình sai → chặn boot với thông điệp rõ (validate-on-start ở AddRabbitMqMessaging).</summary>
    public static void Validate(RabbitMqOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.HostName))
        {
            throw new InvalidOperationException("RabbitMqOptions.HostName rỗng.");
        }

        if (options.Port is <= 0 or > 65535)
        {
            throw new InvalidOperationException($"RabbitMqOptions.Port không hợp lệ: {options.Port}.");
        }

        if (string.IsNullOrWhiteSpace(options.ExchangeName))
        {
            throw new InvalidOperationException("RabbitMqOptions.ExchangeName rỗng.");
        }

        RabbitMqResilienceOptions.Validate(options.Resilience);
    }
}

/// <summary>Tham số resilience pipeline biên adapter (design §9.2: timeout → retry(exp+jitter) → circuit-breaker).</summary>
public sealed class RabbitMqResilienceOptions
{
    public double TimeoutSeconds { get; set; } = 10;

    public int RetryAttempts { get; set; } = 3;

    public double RetryBaseDelayMs { get; set; } = 200;

    public double CircuitFailureRatio { get; set; } = 0.5;

    public int CircuitMinimumThroughput { get; set; } = 10;

    public double CircuitSamplingSeconds { get; set; } = 30;

    public double CircuitBreakSeconds { get; set; } = 15;

    public static void Validate(RabbitMqResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("RabbitMq:Resilience.TimeoutSeconds phải > 0.");
        }

        if (options.RetryAttempts < 0)
        {
            throw new InvalidOperationException("RabbitMq:Resilience.RetryAttempts phải >= 0.");
        }

        if (options.RetryBaseDelayMs <= 0)
        {
            throw new InvalidOperationException("RabbitMq:Resilience.RetryBaseDelayMs phải > 0.");
        }

        // Polly circuit-breaker: FailureRatio ∈ (0,1], MinimumThroughput >= 2, SamplingDuration >= 0.5s.
        if (options.CircuitFailureRatio is <= 0 or > 1)
        {
            throw new InvalidOperationException("RabbitMq:Resilience.CircuitFailureRatio phải trong (0,1].");
        }

        if (options.CircuitMinimumThroughput < 2)
        {
            throw new InvalidOperationException("RabbitMq:Resilience.CircuitMinimumThroughput phải >= 2 (ràng buộc Polly).");
        }

        if (options.CircuitSamplingSeconds < 0.5)
        {
            throw new InvalidOperationException("RabbitMq:Resilience.CircuitSamplingSeconds phải >= 0.5 (ràng buộc Polly).");
        }

        if (options.CircuitBreakSeconds <= 0)
        {
            throw new InvalidOperationException("RabbitMq:Resilience.CircuitBreakSeconds phải > 0.");
        }
    }
}
