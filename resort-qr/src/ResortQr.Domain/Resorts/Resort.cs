using ResortQr.SharedKernel.Entities;

namespace ResortQr.Domain.Resorts;

/// <summary>
/// Resort (tenant gốc). Tạo một lần khi seed (instance-per-resort — <c>24</c> §4).
/// POCO thuần: ràng buộc kỹ thuật (MaxLength/index) ở EF config.
/// </summary>
public sealed class Resort : Entity
{
    public required string Name { get; set; }

    /// <summary>Múi giờ IANA (vd "Asia/Ho_Chi_Minh").</summary>
    public required string Timezone { get; set; }

    public string? LogoUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
