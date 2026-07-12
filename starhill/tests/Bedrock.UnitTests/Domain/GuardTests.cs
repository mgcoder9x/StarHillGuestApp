using Bedrock.Domain.Guards;
using Xunit;

namespace Bedrock.UnitTests.Domain;

public sealed class GuardTests
{
    [Fact]
    public void AgainstNull_should_return_value_when_not_null()
    {
        var value = new object();

        Assert.Same(value, Guard.AgainstNull(value));
    }

    [Fact]
    public void AgainstNull_should_throw_when_null()
    {
        object? value = null;

        Assert.Throws<ArgumentNullException>(() => Guard.AgainstNull(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AgainstNullOrWhiteSpace_should_throw_for_blank_input(string? value)
    {
        // ThrowIfNullOrWhiteSpace ném ArgumentNullException cho null, ArgumentException cho rỗng/whitespace
        // — cả hai đều thuộc họ ArgumentException (đúng hợp đồng docstring của Guard). ThrowsAny chấp nhận
        // kiểu dẫn xuất, khác Throws<T> của xUnit yêu cầu khớp type chính xác.
        Assert.ThrowsAny<ArgumentException>(() => Guard.AgainstNullOrWhiteSpace(value));
    }

    [Fact]
    public void AgainstNegativeOrZero_should_throw_for_zero_or_negative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstNegativeOrZero(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstNegativeOrZero(-1));
    }

    [Fact]
    public void AgainstEmpty_should_throw_for_empty_guid()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstEmpty(Guid.Empty));
    }

    [Fact]
    public void AgainstEmpty_should_return_value_for_nonempty_guid()
    {
        var id = Guid.CreateVersion7();

        Assert.Equal(id, Guard.AgainstEmpty(id));
    }
}
