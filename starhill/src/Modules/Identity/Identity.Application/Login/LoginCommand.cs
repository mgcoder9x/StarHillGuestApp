namespace Identity.Application.Login;

/// <summary>Input đăng nhập: username + password THÔ (chỉ dùng để verify, KHÔNG log — F15/Req 11.6).</summary>
public sealed record LoginCommand(string Username, string Password);

/// <summary>
/// Kết quả đăng nhập: access-token JWT (mang <c>sub</c>+<c>role</c>) + refresh-token RAW mới (gia đình mới) + hạn
/// refresh. Client lưu refresh để rotation về sau qua <c>/token/refresh</c>.
/// </summary>
public sealed record LoginResult(string AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);
