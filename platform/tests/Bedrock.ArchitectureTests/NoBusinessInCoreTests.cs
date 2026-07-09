using System.Reflection;
using Xunit;

namespace Bedrock.ArchitectureTests;

/// <summary>
/// CP1 (I1/F2/F3/F4): lõi <c>Bedrock.*</c> KHÔNG chứa khái niệm nghiệp vụ.
/// PHẠM VI TEST NÀY: tên type + namespace (metadata qua reflection) trên TOÀN BỘ 5 assembly <c>Bedrock.*</c>.
/// Phần quét CHUỖI LITERAL trong IL (ldstr + const) do <see cref="NoBusinessInCoreLiteralTests"/> phủ (task 20).
/// Hai test bổ trợ nhau → CP1 phủ cả tên lẫn literal.
/// </summary>
public sealed class NoBusinessInCoreTests
{
    // Token nghiệp vụ bị cấm trong lõi (design CP1). Case-insensitive.
    private static readonly string[] ForbiddenTokens = ["guest", "room", "resort", "admin", "staff"];

    private static IReadOnlyList<string> FindForbiddenTypeNames(IEnumerable<Type> types) =>
        [.. types
            .Select(t => t.FullName ?? t.Name)
            .Where(name => ForbiddenTokens.Any(tok => name.Contains(tok, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.Ordinal)];

    private static Type[] SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return [.. ex.Types.Where(t => t is not null).Cast<Type>()];
        }
    }

    [Fact]
    public void Bedrock_core_type_names_should_be_business_agnostic()
    {
        var violations = new List<string>();
        foreach (var assembly in CoreAssemblies.AllBedrock)
        {
            violations.AddRange(FindForbiddenTypeNames(SafeGetTypes(assembly)));
        }

        Assert.True(
            violations.Count == 0,
            $"Rò khái niệm nghiệp vụ trong tên type Bedrock.*: {string.Join(", ", violations)}");
    }

    // === NEGATIVE CONTROL: chứng minh scanner THẬT SỰ bắt được vi phạm (không phải luôn trả rỗng) ===
    private sealed class GuestRoomLeakSample;

    private sealed class ResortAdminStaffSample;

    [Fact]
    public void Scanner_should_detect_seeded_business_leak_names()
    {
        var seeded = new[] { typeof(GuestRoomLeakSample), typeof(ResortAdminStaffSample) };

        var violations = FindForbiddenTypeNames(seeded);

        Assert.Equal(2, violations.Count);
    }

    [Fact]
    public void Scanner_should_return_empty_for_clean_names()
    {
        var clean = new[] { typeof(NoBusinessInCoreTests), typeof(string) };

        var violations = FindForbiddenTypeNames(clean);

        Assert.Empty(violations);
    }
}
