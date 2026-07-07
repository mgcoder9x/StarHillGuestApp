namespace ResortQr.SharedKernel.Entities;

/// <summary>Khả năng audit: thời điểm + actor tạo/sửa (set tự động ở SaveChanges).</summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    Guid? CreatedByUserId { get; set; }
    Guid? UpdatedByUserId { get; set; }
}

/// <summary>Khả năng xóa mềm (giữ bản ghi, đánh dấu IsDeleted).</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>Optimistic concurrency: map tới <c>xmin</c> của PostgreSQL (Npgsql).</summary>
public interface IConcurrencyAware
{
    uint RowVersion { get; set; }
}
