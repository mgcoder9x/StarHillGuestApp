using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Rooms.Domain;

namespace Rooms.Application;

/// <summary>
/// Sinh token QR duy nhất (thiết kế §5): app-retry giảm va chạm + <c>ux_qrtoken_token</c> là chốt chặn thật.
/// Token 256-bit CSPRNG base64url (<see cref="ITokenGenerator.NewToken"/> mặc định 32 byte) → xác suất trùng ~0;
/// 5 lần retry là quá đủ. Port từ resort-qr (SharedKernel→Bedrock: <c>ITokenGenerator.NewToken()</c>).
/// </summary>
internal static class RoomTokenFactory
{
    private const int MaxAttempts = 5;

    /// <summary>Trả token chưa tồn tại (best-effort), hoặc null nếu hết retry (cực hiếm → qr_generation_failed).</summary>
    public static async Task<string?> GenerateUniqueTokenAsync(
        IRepository<RoomQrToken> tokens,
        ITokenGenerator tokenGenerator,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var candidate = tokenGenerator.NewToken();
            if (!await tokens.AnyAsync(t => t.Token == candidate, cancellationToken).ConfigureAwait(false))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>Dạng che để hiển thị/đối soát mà KHÔNG lộ token đầy đủ (Req 11.6): 6 ký tự đầu + "…".</summary>
    public static string Mask(string token) => token.Length <= 6 ? token : string.Concat(token.AsSpan(0, 6), "…");
}
