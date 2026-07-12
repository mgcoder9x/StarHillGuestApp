namespace Bedrock.Domain.Entities;

/// <summary>Khả năng audit: thời điểm + actor tạo/sửa (set tự động ở SaveChanges qua interceptor).</summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    Guid? CreatedByUserId { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    Guid? UpdatedByUserId { get; set; }
}

/// <summary>Khả năng xóa mềm (giữ bản ghi, đánh dấu <see cref="IsDeleted"/> + query filter loại bỏ).</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>
/// Optimistic concurrency token. F8: đổi tên khỏi <c>IConcurrencyAware</c>; wording TRUNG LẬP —
/// kernel KHÔNG nhắc <c>xmin</c>/Npgsql. Kiểu <c>uint</c> là tradeoff có ý thức (design §4.3):
/// khớp system column 32-bit của provider đích, EF map thẳng; provider khác là breaking change chấp nhận được.
/// </summary>
public interface IHasConcurrencyToken
{
    uint RowVersion { get; set; }
}
