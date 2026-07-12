using Bedrock.Domain.Primitives;
using Xunit;

namespace Bedrock.UnitTests.Domain;

public sealed class ValueObjectTests
{
    private sealed class Money : ValueObject
    {
        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public decimal Amount { get; }

        public string Currency { get; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    [Fact]
    public void Same_components_should_be_equal()
    {
        var a = new Money(10m, "VND");
        var b = new Money(10m, "VND");

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Different_components_should_not_be_equal()
    {
        var a = new Money(10m, "VND");
        var b = new Money(10m, "USD");

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }
}
