namespace Bedrock.Application.Ports.Email;

/// <summary>Email gửi đi (contract-first, F28). Body HTML bắt buộc; text-body tuỳ chọn (multipart).</summary>
public sealed record EmailMessage(string To, string Subject, string HtmlBody, string? TextBody = null);

/// <summary>
/// Gửi email (adapter Gmail/SMTP/SendGrid ở <c>Adapters.*</c>). Default khi chưa cắm adapter = fail-loud
/// (ném <c>InvalidOperationException</c>) — mất email âm thầm nguy hiểm hơn crash (§5.5/R16.4).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
