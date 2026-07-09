using Bedrock.Application.Ports.Security;
using Microsoft.Extensions.Options;

namespace Bedrock.Infrastructure.Tokens;

/// <summary>
/// Bọc <see cref="JwtKeyRingValidation"/> vào options-pattern để chạy VALIDATE-ON-START (design §9.4/F35):
/// đăng ký qua <c>AddOptions&lt;JwtKeyRingOptions&gt;().ValidateOnStart()</c> → validate lúc host START (SAU
/// Build), KHÔNG phải lúc đăng ký service. Nhờ vậy config nạp muộn (User-Secrets/env/test-config) vẫn được
/// thấy, mà vẫn FAIL-FAST (boot chặn) khi key-ring sai — giữ nguyên thông điệp chi tiết của validation.
/// </summary>
internal sealed class JwtKeyRingOptionsValidator : IValidateOptions<JwtKeyRingOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtKeyRingOptions options)
    {
        try
        {
            JwtKeyRingValidation.Validate(options);
            return ValidateOptionsResult.Success;
        }
        catch (InvalidOperationException ex)
        {
            return ValidateOptionsResult.Fail(ex.Message);
        }
    }
}
