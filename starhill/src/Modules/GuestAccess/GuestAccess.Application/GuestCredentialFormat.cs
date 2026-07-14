namespace GuestAccess.Application;

/// <summary>
/// Canonical wire format của capability QR và guest-session key do <c>ITokenGenerator.NewToken()</c> mặc định
/// sinh: 32 byte → base64url không padding → đúng 43 ký tự ASCII. Guard chạy trước resolver/hash/DB để malformed
/// input không tiêu tốn I/O và cookie lạ có thể được coi an toàn như thiết bị mới (QR-AD-025).
/// </summary>
internal static class GuestCredentialFormat
{
    private const int EncodedLength = 43;

    public static bool IsCanonical(string? value)
    {
        if (value is null || value.Length != EncodedLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!IsBase64UrlCharacter(character))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsBase64UrlCharacter(char character) =>
        character is >= 'A' and <= 'Z'
            or >= 'a' and <= 'z'
            or >= '0' and <= '9'
            or '-' or '_';
}