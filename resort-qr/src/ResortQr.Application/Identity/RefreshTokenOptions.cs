using System.ComponentModel.DataAnnotations;

namespace ResortQr.Application.Identity;

/// <summary>Chính sách refresh token. Default 30 ngày (khoảng 7–90 — Req 11.1).</summary>
public sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";

    [Range(7, 90)]
    public int RefreshTokenDays { get; set; } = 30;
}
