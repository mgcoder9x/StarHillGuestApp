using Bedrock.Application.UseCases;
using Xunit;

namespace Bedrock.UnitTests.Application;

public sealed class PagingTests
{
    [Theory]
    [InlineData(0, 20, 1, 20, 0)]
    [InlineData(-5, 20, 1, 20, 0)]
    [InlineData(3, 10, 3, 10, 20)]
    [InlineData(2, 0, 2, 20, 20)]
    [InlineData(2, 1000, 2, 20, 20)]
    public void SafePage_SafePageSize_Skip_should_normalize(
        int page, int pageSize, int expectedPage, int expectedSize, int expectedSkip)
    {
        var req = new PagedRequest(page, pageSize);

        Assert.Equal(expectedPage, req.SafePage);
        Assert.Equal(expectedSize, req.SafePageSize);
        Assert.Equal(expectedSkip, req.Skip);
    }

    [Fact]
    public void PagedResult_should_compute_navigation_flags()
    {
        var result = new PagedResult<int>([1, 2, 3], Page: 2, PageSize: 3, Total: 10);

        Assert.Equal(4, result.TotalPages);
        Assert.True(result.HasNext);
        Assert.True(result.HasPrevious);
    }
}
