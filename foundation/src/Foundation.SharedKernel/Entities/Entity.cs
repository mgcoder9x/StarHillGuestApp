namespace Foundation.SharedKernel.Entities;

/// <summary>
/// Base entity: khóa chính <c>uuid</c> sinh client-side bằng UUIDv7 (time-ordered → thân thiện B-tree).
/// Bất biến: <see cref="Id"/> KHÔNG bao giờ là <see cref="Guid.Empty"/> — chặn ngay ở init (kể cả object initializer).
/// So sánh định danh (identity equality) theo <see cref="Id"/> + kiểu thực.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    private readonly Guid _id = Guid.CreateVersion7();

    protected Entity()
    {
    }

    protected Entity(Guid id) => Id = id;

    public Guid Id
    {
        get => _id;
        init => _id = value == Guid.Empty
            ? throw new ArgumentException("Id của entity không được là Guid.Empty.", nameof(value))
            : value;
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

        // Id luôn khác Guid.Empty (bất biến enforce ở init) → chỉ cần so kiểu + Id.
        return GetType() == other.GetType() && Id == other.Id;
    }

    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
