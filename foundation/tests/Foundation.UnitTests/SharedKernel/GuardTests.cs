using System;
using Foundation.SharedKernel.Guards;
using Xunit;

namespace Foundation.UnitTests.SharedKernel;

public sealed class GuardTests
{
    [Fact]
    public void AgainstNull_should_return_value_when_not_null()
    {
        var obj = new object();
        Assert.Same(obj, Guard.AgainstNull(obj));
    }

    [Fact]
    public void AgainstNull_should_throw_when_null()
    {
        object? obj = null;
        Assert.Throws<ArgumentNullException>(() => Guard.AgainstNull(obj));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AgainstNullOrWhiteSpace_should_throw(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => Guard.AgainstNullOrWhiteSpace(value));
    }

    [Fact]
    public void AgainstNullOrWhiteSpace_should_return_value_when_valid()
    {
        Assert.Equal("abc", Guard.AgainstNullOrWhiteSpace("abc"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AgainstNegativeOrZero_should_throw(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstNegativeOrZero(value));
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
