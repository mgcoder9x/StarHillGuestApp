using Bedrock.Application.Ports.Security;

namespace Bedrock.Infrastructure.Tokens;

/// <summary>
/// Validate-on-start cho <see cref="JwtKeyRingOptions"/> (F35 — fail-fast mọi môi trường). Tách riêng (internal)
/// để test thuần không cần dựng config. Task 10 (RequiredPortsValidator) sẽ gộp vào validator khởi động thống nhất.
/// </summary>
internal static class JwtKeyRingValidation
{
    private const int MinSecretBytes = 32; // HS256 cần khóa ≥ 256-bit.

    /// <summary>Ném <see cref="InvalidOperationException"/> với thông điệp rõ nếu key-ring không hợp lệ.</summary>
    public static void Validate(JwtKeyRingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Keys.Count == 0)
        {
            throw new InvalidOperationException("JwtKeyRingOptions.Keys rỗng — cần ít nhất một khóa ký (F22/F35).");
        }

        if (string.IsNullOrWhiteSpace(options.ActiveKid))
        {
            throw new InvalidOperationException("JwtKeyRingOptions.ActiveKid rỗng — phải chỉ định khóa đang ký.");
        }

        if (!options.Keys.Any(k => string.Equals(k.Kid, options.ActiveKid, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"JwtKeyRingOptions.ActiveKid '{options.ActiveKid}' không có trong Keys.");
        }


        var duplicateKids = options.Keys
            .GroupBy(key => key.Kid, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateKids.Length > 0)
        {
            throw new InvalidOperationException(
                "JwtKeyRingOptions có Kid trùng: " + string.Join(", ", duplicateKids));
        }

        if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("JwtKeyRingOptions cần Issuer và Audience khác rỗng.");
        }

        if (options.AccessTokenLifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("JwtKeyRingOptions.AccessTokenLifetimeMinutes phải > 0.");
        }

        foreach (var key in options.Keys)
        {
            if (string.IsNullOrWhiteSpace(key.Kid))
            {
                throw new InvalidOperationException("Mỗi JwtSigningKey phải có Kid khác rỗng.");
            }

            byte[] secret;
            try
            {
                secret = Convert.FromBase64String(key.Secret);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException($"JwtSigningKey '{key.Kid}': Secret không phải base64 hợp lệ.");
            }

            if (secret.Length < MinSecretBytes)
            {
                throw new InvalidOperationException(
                    $"JwtSigningKey '{key.Kid}': Secret chỉ {secret.Length} byte — HS256 cần ≥ {MinSecretBytes} byte (256-bit).");
            }
        }
    }
}
