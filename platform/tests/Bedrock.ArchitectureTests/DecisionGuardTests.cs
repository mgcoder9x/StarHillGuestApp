using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Html;
using Bedrock.Domain.Results;
using Bedrock.Messaging.Contracts;
using Xunit;

namespace Bedrock.ArchitectureTests;

/// <summary>
/// ANTI-DRIFT (layer 2): biến các QUYẾT ĐỊNH đã chốt trong journal thành guard test — code lệch quyết định
/// sẽ FAIL BUILD, không dựa review bằng tay. Mỗi test map tới một AD trong journal/01-decisions.md.
/// </summary>
public sealed class DecisionGuardTests
{
    // AD-002: Result/Result<T> PHẢI là sealed class (KHÔNG readonly struct) — tránh bẫy default(struct).
    [Fact]
    public void AD002_Result_must_be_sealed_class_not_struct()
    {
        Assert.True(typeof(Result).IsClass, "Result phải là class (AD-002).");
        Assert.True(typeof(Result).IsSealed, "Result phải sealed (AD-002).");
        Assert.False(typeof(Result).IsValueType, "Result KHÔNG được là struct (AD-002).");

        var resultOfT = typeof(Result<>);
        Assert.True(resultOfT.IsClass, "Result<T> phải là class (AD-002).");
        Assert.True(resultOfT.IsSealed, "Result<T> phải sealed (AD-002).");
        Assert.False(resultOfT.IsValueType, "Result<T> KHÔNG được là struct (AD-002).");
    }

    // AD-017: IntegrationEvent PHẢI ở assembly/namespace trung tính Bedrock.Messaging.Contracts,
    // KHÔNG ở Bedrock.Application → giữ *.Contracts không trỏ lên Application.
    [Fact]
    public void AD017_IntegrationEvent_must_live_in_neutral_contracts_assembly()
    {
        var type = typeof(IntegrationEvent);

        Assert.Equal("Bedrock.Messaging.Contracts", type.Namespace);
        Assert.Same(CoreAssemblies.MessagingContracts, type.Assembly);
        Assert.NotSame(CoreAssemblies.Application, type.Assembly);
    }

    // AD-021: IHtmlSanitizer PHẢI ở Ports.Html (tách khỏi Ports.Security auth/crypto).
    [Fact]
    public void AD021_HtmlSanitizer_must_live_in_ports_html_namespace()
    {
        Assert.Equal("Bedrock.Application.Ports.Html", typeof(IHtmlSanitizer).Namespace);
    }

    // AD-081 (A-20): port transport IEventBusPublisher CHỈ nhận envelope BẤT BIẾN OutgoingIntegrationMessage,
    // KHÔNG lộ shape persistence/retry của OutboxMessage cho adapter bus (pluggable F29).
    [Fact]
    public void AD081_EventBusPublisher_port_takes_immutable_outgoing_envelope_not_outbox_record()
    {
        // (1) Tham số PublishAsync PHẢI là OutgoingIntegrationMessage (không phải OutboxMessage).
        var publishMethod = typeof(IEventBusPublisher).GetMethod(nameof(IEventBusPublisher.PublishAsync))!;
        var messageParam = publishMethod.GetParameters()[0];
        Assert.Same(typeof(OutgoingIntegrationMessage), messageParam.ParameterType);
        Assert.NotSame(typeof(OutboxMessage), messageParam.ParameterType);

        // (2) Envelope KHÔNG được chứa cột retry/persistence (rò rỉ trạng thái outbox nội bộ ra transport).
        var leakingMembers = new[]
        {
            "ProcessedAt", "ErrorCount", "NextAttemptAt", "DeadLetteredAt", "ClaimId", "ClaimedUntil",
        };
        foreach (var member in leakingMembers)
        {
            Assert.Null(typeof(OutgoingIntegrationMessage).GetProperty(member));
        }

        // (3) Envelope PHẢI bất biến: mọi property công khai là init-only (không setter mutable).
        var externalInit = typeof(System.Runtime.CompilerServices.IsExternalInit);
        foreach (var prop in typeof(OutgoingIntegrationMessage).GetProperties())
        {
            var setter = prop.SetMethod;
            Assert.True(setter is null || setter.ReturnParameter.GetRequiredCustomModifiers().Contains(externalInit),
                $"Property {prop.Name} của OutgoingIntegrationMessage phải init-only (AD-081).");
        }
    }
}
