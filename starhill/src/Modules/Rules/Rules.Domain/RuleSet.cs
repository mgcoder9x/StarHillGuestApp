using Bedrock.Domain.Entities;

namespace Rules.Domain;

/// <summary>
/// Bản NHÁP (Draft) nội quy của một resort — admin/staff sửa ở đây, KHÔNG phải nguồn khách đọc (khách đọc từ
/// <see cref="RulePublication"/> IsCurrent — CP4). <see cref="ResortId"/> là Guid TRẦN (không FK chéo-schema
/// resort_config — QR-AD-002). Concurrency token (xmin) chống hai người sửa đè âm thầm (CP15).
/// </summary>
public sealed class RuleSet : Entity, IHasConcurrencyToken
{
    public required Guid ResortId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public uint RowVersion { get; set; }
}
