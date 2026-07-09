using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Bedrock.ArchitectureTests;

/// <summary>
/// CP1 (I1/F2/F3/F4) — phần HOÀN TẤT ở task 20: quét CHUỖI LITERAL trong IL của toàn bộ 5 assembly
/// <c>Bedrock.*</c> (<c>ldstr</c> trong thân method + hằng chuỗi <c>const</c>). Đây là phần CP1 mà reflection/
/// NetArchTest KHÔNG bắt được (chỉ bắt tên type/namespace — xem <see cref="NoBusinessInCoreTests"/>). Dùng
/// Mono.Cecil (decode IL đúng chuẩn) đọc DLL từ bytes (không khoá file). Lệch = lõi rò khái niệm nghiệp vụ = FAIL BUILD.
/// </summary>
public sealed class NoBusinessInCoreLiteralTests
{
    private static readonly string[] ForbiddenTokens = ["guest", "room", "resort", "admin", "staff"];

    [Fact]
    public void Bedrock_core_should_not_contain_business_string_literals()
    {
        var violations = new List<string>();
        foreach (var assembly in CoreAssemblies.AllBedrock)
        {
            violations.AddRange(FindForbiddenLiterals(assembly));
        }

        Assert.True(
            violations.Count == 0,
            $"Rò khái niệm nghiệp vụ trong chuỗi literal của Bedrock.*:{Environment.NewLine}"
            + string.Join(Environment.NewLine, violations));
    }

    // === NEGATIVE CONTROL: scanner PHẢI bắt được literal nghiệp vụ. Assembly test này CHỨA literal cấm
    // (chính mảng ForbiddenTokens + hạt giống dưới) → scan nó phải ra kết quả khác rỗng.
    private static string SeededBusinessLiteral() => "seeded-guest-room-resort-admin-staff-leak";

    [Fact]
    public void Scanner_should_detect_business_literals_in_dirty_assembly()
    {
        _ = SeededBusinessLiteral(); // giữ ldstr không bị tối ưu bỏ.

        var violations = FindForbiddenLiterals(typeof(NoBusinessInCoreLiteralTests).Assembly);

        Assert.Contains(
            violations,
            v => v.Contains("seeded-guest-room-resort", StringComparison.Ordinal));
    }

    private static List<string> FindForbiddenLiterals(Assembly assembly)
    {
        var violations = new List<string>();

        // Đọc DLL từ bytes → không khoá file DLL đang được process test nạp (an toàn chạy song song).
        var bytes = File.ReadAllBytes(assembly.Location);
        using var stream = new MemoryStream(bytes, writable: false);
        using var module = ModuleDefinition.ReadModule(stream);

        foreach (var type in module.GetTypes())
        {
            foreach (var field in type.Fields)
            {
                if (field.HasConstant && field.Constant is string constant)
                {
                    AddIfForbidden(violations, constant, $"{type.FullName}.{field.Name} (const)");
                }
            }

            foreach (var method in type.Methods)
            {
                if (!method.HasBody)
                {
                    continue;
                }

                foreach (var instruction in method.Body.Instructions)
                {
                    if (instruction.OpCode == OpCodes.Ldstr && instruction.Operand is string literal)
                    {
                        AddIfForbidden(violations, literal, $"{type.FullName}.{method.Name} (ldstr)");
                    }
                }
            }
        }

        return violations;
    }

    private static void AddIfForbidden(List<string> accumulator, string value, string where)
    {
        var hit = ForbiddenTokens.FirstOrDefault(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
        if (hit is not null)
        {
            accumulator.Add($"'{value}' @ {where} (token '{hit}')");
        }
    }
}
