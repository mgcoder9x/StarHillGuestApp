using System.Text;
using Foundation.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace Foundation.Api.Security;

/// <summary>
/// Validate cross-cutting cho <see cref="JwtOptions"/>: <see cref="JwtOptions.SigningKey"/> phải ≥ 32 byte
/// (256-bit) cho HS256 — DataAnnotations chỉ chặn rỗng, còn độ dài byte cần validator riêng. Fail-fast khi khởi động.
/// </summary>
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    private const int MinKeyBytes = 32;

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrEmpty(options.SigningKey))
        {
            return ValidateOptionsResult.Fail("Jwt:SigningKey là bắt buộc.");
        }

        var byteLength = Encoding.UTF8.GetByteCount(options.SigningKey);
        return byteLength < MinKeyBytes
            ? ValidateOptionsResult.Fail($"Jwt:SigningKey phải ≥ {MinKeyBytes} byte (256-bit) cho HS256; hiện có {byteLength} byte.")
            : ValidateOptionsResult.Success;
    }
}
