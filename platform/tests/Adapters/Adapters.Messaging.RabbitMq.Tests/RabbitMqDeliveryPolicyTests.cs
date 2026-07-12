using System.Text.Json;
using Adapters.Messaging.RabbitMq;
using Bedrock.Application.Messaging.Dispatch;
using Xunit;

namespace Adapters.Messaging.RabbitMq.Tests;

/// <summary>
/// Guard THUẦN (không broker) cho <see cref="RabbitMqDeliveryPolicy"/> — P0-02 retry tier. Đây là phần rủi ro nhất
/// (quyết định "lỗi transient có bị coi poison vĩnh viễn không"): tách hàm thuần cho phép kiểm chứng đầy đủ ngưỡng
/// retry + phân loại permanent/transient KHÔNG cần RabbitMQ. Hành vi topology (TTL → dead-letter về main, publish
/// confirm) chứng minh trên broker thật ở e2e (Testcontainers/CI).
/// </summary>
public sealed class RabbitMqDeliveryPolicyTests
{
    // ---- ForOutcome: exhaustive trên toàn enum InboxDispatchOutcome ----

    [Theory]
    [InlineData(InboxDispatchOutcome.Handled)]
    [InlineData(InboxDispatchOutcome.Duplicate)]
    public void Handled_and_duplicate_acknowledge(InboxDispatchOutcome outcome)
    {
        Assert.Equal(DeliveryAction.Acknowledge, RabbitMqDeliveryPolicy.ForOutcome(outcome));
    }

    [Fact]
    public void Dead_lettered_outcome_dead_letters_immediately()
    {
        // Envelope/type/schema sai (permanent) → DLQ ngay, KHÔNG retry (retry cũng lỗi y hệt).
        Assert.Equal(DeliveryAction.DeadLetter, RabbitMqDeliveryPolicy.ForOutcome(InboxDispatchOutcome.DeadLettered));
    }

    // ---- IsPermanent ----

    [Fact]
    public void Json_exception_is_permanent()
    {
        Assert.True(RabbitMqDeliveryPolicy.IsPermanent(new JsonException("bad payload")));
    }

    [Theory]
    [MemberData(nameof(TransientExceptions))]
    public void Non_json_exceptions_are_transient(Exception ex)
    {
        Assert.False(RabbitMqDeliveryPolicy.IsPermanent(ex));
    }

    public static TheoryData<Exception> TransientExceptions() => new()
    {
        new TimeoutException(),
        new InvalidOperationException("db unavailable"),
        new HttpRequestException("network"),
        new TaskCanceledException(),
    };

    // ---- ForException: permanent → DLQ ngay bất kể còn lượt ----

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void Permanent_exception_dead_letters_regardless_of_attempts(int priorAttempts)
    {
        Assert.Equal(
            DeliveryAction.DeadLetter,
            RabbitMqDeliveryPolicy.ForException(new JsonException("x"), priorAttempts, maxAttempts: 5));
    }

    // ---- ForException: transient còn lượt → Retry; hết lượt → DeadLetter ----

    [Theory]
    // maxAttempts = 5: tổng giao = priorAttempts + 1. Còn lượt khi (prior+1) < 5.
    [InlineData(0, 5, DeliveryAction.Retry)]   // giao lần 1/5 → còn
    [InlineData(1, 5, DeliveryAction.Retry)]   // 2/5
    [InlineData(2, 5, DeliveryAction.Retry)]   // 3/5
    [InlineData(3, 5, DeliveryAction.Retry)]   // 4/5
    [InlineData(4, 5, DeliveryAction.DeadLetter)] // 5/5 = lần cuối → hết lượt
    [InlineData(5, 5, DeliveryAction.DeadLetter)] // quá ngưỡng (phòng thủ)
    public void Transient_exception_respects_attempt_ceiling(int priorAttempts, int maxAttempts, DeliveryAction expected)
    {
        Assert.Equal(
            expected,
            RabbitMqDeliveryPolicy.ForException(new TimeoutException(), priorAttempts, maxAttempts));
    }

    [Fact]
    public void Transient_with_max_one_dead_letters_on_first_delivery()
    {
        // maxAttempts = 1: chỉ giao đúng 1 lần, không retry → transient rơi thẳng DLQ.
        Assert.Equal(
            DeliveryAction.DeadLetter,
            RabbitMqDeliveryPolicy.ForException(new TimeoutException(), priorAttempts: 0, maxAttempts: 1));
    }

    [Fact]
    public void For_exception_null_throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => RabbitMqDeliveryPolicy.ForException(null!, priorAttempts: 0, maxAttempts: 5));
    }
}
