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
}
