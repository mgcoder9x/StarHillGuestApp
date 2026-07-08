using System.Reflection;
using Xunit;

namespace Bedrock.ArchitectureTests;

/// <summary>
/// CP1 (I1/F2/F3/F4): lõi <c>Bedrock.*</c> KHÔNG chứa khái niệm nghiệp vụ.
/// PHẠM VI KIỂM CỦA TEST NÀY: tên type + namespace (metadata qua reflection). Đây là phần CP1 mà công cụ
/// tĩnh bắt được. Việc quét CHUỖI LITERAL trong thân method (vd hằng "/api/guest/...") KHÔNG do reflection/
/// NetArchTest bắt được — sẽ phủ bằng cơ chế khác (source/IL scan) ở task 20 (xem N-021). KHÔNG tuyên bố
/// test này phủ literal — nói đúng giới hạn.
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
    public void Domain_type_names_should_be_business_agnostic()
    {
        var violations = FindForbiddenTypeNames(SafeGetTypes(CoreAssemblies.Domain));

        Assert.True(violations.Count == 0, $"Rò nghiệp vụ trong Bedrock.Domain: {string.Join(", ", violations)}");
    }

    [Fact]
    public void MessagingContracts_type_names_should_be_business_agnostic()
    {
        var violations = FindForbiddenTypeNames(SafeGetTypes(CoreAssemblies.MessagingContracts));

        Assert.True(violations.Count == 0, $"Rò nghiệp vụ trong Bedrock.Messaging.Contracts: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Application_type_names_should_be_business_agnostic()
    {
        var violations = FindForbiddenTypeNames(SafeGetTypes(CoreAssemblies.Application));

        Assert.True(violations.Count == 0, $"Rò nghiệp vụ trong Bedrock.Application: {string.Join(", ", violations)}");
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
