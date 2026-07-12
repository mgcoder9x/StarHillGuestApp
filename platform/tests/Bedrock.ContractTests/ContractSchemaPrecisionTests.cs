using System.Collections.Generic;
using Xunit;

namespace Bedrock.ContractTests;

/// <summary>
/// A-22 — chứng minh <see cref="ContractSchema"/> mô tả CHÍNH XÁC để snapshot bắt được breaking-change tinh vi mà
/// <c>Type.Name</c> thuần BỎ SÓT: đổi generic argument, đổi element mảng, thêm/bớt nullable (value + reference).
/// Đây là guard cho chính cơ chế chống-drift (snapshot chỉ mạnh khi descriptor phân biệt được các khác biệt này).
/// </summary>
public sealed class ContractSchemaPrecisionTests
{
    private sealed record Sample(
        List<string> Strings,
        List<int> Ints,
        string[] Array,
        Dictionary<string, int> Map,
        Guid Id,
        Guid? MaybeId,
        string Name,
        string? MaybeName);

    private static string Describe(string propertyName) =>
        ContractSchema.DescribeProperty(typeof(Sample).GetProperty(propertyName)!);

    [Fact]
    public void Generic_arguments_are_rendered_fully_and_distinguish_element_type()
    {
        Assert.Equal("List<String>", Describe(nameof(Sample.Strings)));
        Assert.Equal("List<Int32>", Describe(nameof(Sample.Ints)));
        Assert.NotEqual(Describe(nameof(Sample.Strings)), Describe(nameof(Sample.Ints))); // List<string> ≠ List<int>.
        Assert.Equal("Dictionary<String, Int32>", Describe(nameof(Sample.Map)));
    }

    [Fact]
    public void Arrays_render_element_type()
    {
        Assert.Equal("String[]", Describe(nameof(Sample.Array)));
    }

    [Fact]
    public void Value_type_nullability_is_distinguished()
    {
        Assert.Equal("Guid", Describe(nameof(Sample.Id)));
        Assert.Equal("Guid?", Describe(nameof(Sample.MaybeId)));
        Assert.NotEqual(Describe(nameof(Sample.Id)), Describe(nameof(Sample.MaybeId)));
    }

    [Fact]
    public void Reference_type_nullability_is_distinguished()
    {
        Assert.Equal("String", Describe(nameof(Sample.Name)));
        Assert.Equal("String?", Describe(nameof(Sample.MaybeName)));
        Assert.NotEqual(Describe(nameof(Sample.Name)), Describe(nameof(Sample.MaybeName)));
    }
}
