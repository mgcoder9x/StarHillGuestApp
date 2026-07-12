namespace Identity.Application.RefreshToken;

/// <summary>Input rotation: raw refresh token client gửi lên (chưa hash).</summary>
public sealed record RefreshTokenCommand(string RawRefreshToken);

/// <summary>
/// Kết quả rotation: access token JWT mới + refresh token RAW mới (client thay thế token cũ) + hạn của refresh token.
/// <b>DV-014:</b> §7.4 pseudocode chỉ trả <c>Issue(newRecord)</c> (access token); nhưng rotation bản chất sinh
/// token mới (hash mới ⇒ raw mới) — client PHẢI nhận raw mới để dùng lần sau, nếu không rotation vô nghĩa.
/// </summary>
public sealed record RefreshTokenResult(string AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);
