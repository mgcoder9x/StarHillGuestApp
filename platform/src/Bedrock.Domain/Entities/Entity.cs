using Bedrock.Domain.Events;

namespace Bedrock.Domain.Entities;

/// <summary>
/// Base entity: khóa chính <c>uuid</c> sinh client-side bằng UUIDv7 (time-ordered → thân thiện B-tree).
/// Bất biến: <see cref="Id"/> KHÔNG bao giờ là <see cref="Guid.Empty"/> — chặn ngay ở init.
/// So sánh định danh (identity equality) theo <see cref="Id"/> + kiểu thực. Thu thập domain events (R33).
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    private readonly List<IDomainEvent> _domainEvents = [];
    private readonly Guid _id = Guid.CreateVersion7();

    protected Entity()
    {
    }

    protected Entity(Guid id) => Id = id;

    public Guid Id
    {
        get => _id;
        protected init => _id = value == Guid.Empty
            ? throw new ArgumentException("Id của entity không được là Guid.Empty.", nameof(value))
            : value;
    }

    /// <summary>Domain events đã raise, chờ dispatch trong SaveChanges. Chỉ đọc từ ngoài.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Khôi phục event về đầu hàng đợi khi dispatch thất bại trước commit. Public để persistence mechanism có
    /// thể giữ retry semantics; application code thông thường chỉ raise qua protected method.
    /// </summary>
    public void RestoreDomainEvents(IReadOnlyCollection<IDomainEvent> domainEvents)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);
        _domainEvents.InsertRange(0, domainEvents);
    }

    public bool Equals(Entity? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Id luôn khác Guid.Empty (bất biến enforce ở init) → chỉ cần so kiểu thực + Id.
        return GetType() == other.GetType() && Id == other.Id;
    }

    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
