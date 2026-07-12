using Bedrock.Domain.Entities;

namespace ResortConfig.Domain;

/// <summary>
/// Resort (tenant gốc) — tạo một lần khi seed (instance-per-resort). POCO thuần; ràng buộc kỹ thuật
/// (MaxLength/index) ở EF config. Port từ resort-qr <c>ResortQr.Domain.Resorts.Resort</c> (SharedKernel→Bedrock).
/// </summary>
public sealed class Resort : Entity
{
    public required string Name { get; set; }

    /// <summary>Múi giờ IANA (vd "Asia/Ho_Chi_Minh").</summary>
    public required string Timezone { get; set; }

    public string? LogoUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
