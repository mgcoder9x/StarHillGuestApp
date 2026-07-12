namespace Bedrock.Domain.Primitives;

/// <summary>
/// Base Value Object: bình đẳng theo GIÁ TRỊ (structural equality) qua <see cref="GetEqualityComponents"/>,
/// không có định danh. Bất biến khuyến nghị. Dùng cho khái niệm như Money, Email, DateRange...
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>Các thành phần tham gia so sánh bình đẳng (theo thứ tự ổn định).</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return GetType() == other.GetType()
            && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !Equals(left, right);
}
