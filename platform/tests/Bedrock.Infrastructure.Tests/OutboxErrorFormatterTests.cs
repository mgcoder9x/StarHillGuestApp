using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Infrastructure.Persistence.Messaging;
using Xunit;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// P1-07 guard: <see cref="OutboxErrorFormatter.Redact"/> KHÔNG chỉ truncate mà THỰC SỰ redact credential/token
/// trong <c>Exception.Message</c> trước khi lưu vào cột chẩn đoán <see cref="OutboxMessage.LastError"/> — giảm rủi
/// ro rò secret/PII vào bảng outbox + công cụ admin/telemetry. Giữ classification (tên kiểu exception) + bound.
/// </summary>
public sealed class OutboxErrorFormatterTests
{
    [Fact]
    public void Prefixes_exception_type_as_stable_classification()
    {
        var result = OutboxErrorFormatter.Redact(new InvalidOperationException("boom"));
        Assert.StartsWith("InvalidOperationException: ", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redacts_credentials_in_uri()
    {
        var result = OutboxErrorFormatter.Redact(
            new InvalidOperationException("connect amqp://guest:s3cretPass@broker:5672/vh failed"));
        Assert.DoesNotContain("s3cretPass", result, StringComparison.Ordinal);
        Assert.DoesNotContain("guest:s3cretPass", result, StringComparison.Ordinal);
        Assert.Contains("amqp://***:***@broker:5672", result, StringComparison.Ordinal); // scheme/host giữ để chẩn đoán.
    }

    [Theory]
    [InlineData("Password=SuperSecret1;Host=db", "SuperSecret1")]
    [InlineData("pwd=abc123;", "abc123")]
    [InlineData("token=eyJhbGciOiJ; rest", "eyJhbGciOiJ")]
    [InlineData("api_key=AKIA1234567890", "AKIA1234567890")]
    [InlineData("access_key: TOPSECRETVALUE", "TOPSECRETVALUE")]
    [InlineData("secret=hunter2", "hunter2")]
    public void Redacts_sensitive_key_value_pairs(string message, string secret)
    {
        var result = OutboxErrorFormatter.Redact(new InvalidOperationException(message));
        Assert.DoesNotContain(secret, result, StringComparison.Ordinal);
        Assert.Contains("=***", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redacts_bearer_token()
    {
        var result = OutboxErrorFormatter.Redact(
            new InvalidOperationException("header Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.payload.sig"));
        // Thuộc tính bảo mật cốt lõi: token KHÔNG còn trong chuỗi lưu; có dấu redaction.
        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiJ9.payload.sig", result, StringComparison.Ordinal);
        Assert.Contains("***", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redacts_bare_bearer_token_without_authorization_key()
    {
        // Không có key "Authorization" đứng trước → rule Bearer giữ tiền tố "Bearer ***".
        var result = OutboxErrorFormatter.Redact(
            new InvalidOperationException("sent Bearer eyJhbGciOiJIUzI1NiJ9.payload.sig upstream"));
        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiJ9.payload.sig", result, StringComparison.Ordinal);
        Assert.Contains("Bearer ***", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Leaves_non_sensitive_message_intact()
    {
        var result = OutboxErrorFormatter.Redact(new TimeoutException("operation timed out after 30s"));
        Assert.Equal("TimeoutException: operation timed out after 30s", result);
    }

    [Fact]
    public void Bounds_length_to_max()
    {
        var longMessage = new string('x', OutboxMessage.MaxLastErrorLength * 2);
        var result = OutboxErrorFormatter.Redact(new InvalidOperationException(longMessage));
        Assert.True(result.Length <= OutboxMessage.MaxLastErrorLength);
    }

    [Fact]
    public void Null_exception_throws()
    {
        Assert.Throws<ArgumentNullException>(() => OutboxErrorFormatter.Redact(null!));
    }
}
