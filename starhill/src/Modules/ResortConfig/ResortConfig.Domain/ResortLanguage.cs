using Bedrock.Domain.Entities;

namespace ResortConfig.Domain;

/// <summary>
/// Ngôn ngữ được bật cho resort. Bất biến: đúng MỘT bản ghi <see cref="IsDefault"/> mỗi resort
/// (partial unique index enforce — CP14). Là nguồn ngôn ngữ mặc định fallback i18n.
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
