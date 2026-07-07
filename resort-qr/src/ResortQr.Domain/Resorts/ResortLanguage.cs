using ResortQr.SharedKernel.Entities;

namespace ResortQr.Domain.Resorts;

/// <summary>
/// Ngôn ngữ được bật cho resort. Đúng một bản ghi <see cref="IsDefault"/> mỗi resort
/// (partial unique index enforce). Nguồn mặc định fallback i18n.
/// </summary>
public sealed class ResortLanguage : Entity
{
    public required Guid ResortId { get; set; }

    /// <summary>Mã BCP-47 (en, vi, ko, zh).</summary>
    public required string Code { get; set; }

    public required string DisplayName { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool IsDefault { get; set; }

    public int SortOrder { get; set; }
}
