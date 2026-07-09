using System.Reflection;
using Bedrock.Domain.Results;
using Identity.Domain;

namespace Bedrock.ContractTests;

/// <summary>
/// Task 20 (CP12/F20, R29.1/R32.4) — SNAPSHOT registry <see cref="Error.Code"/>. Bản chất: <c>Error.Code</c> là
/// HỢP ĐỒNG máy-đọc với client (UI localize theo code). Đổi/xoá một code là BREAKING cho client. Test reflect
/// mọi code ỔN ĐỊNH (static <see cref="Error"/> field + static method trả <see cref="Error"/> với MỌI tham số
/// optional — code cố định, không template) trong Domain lõi + Domain module, so với snapshot đã duyệt → mọi
/// khác biệt FAIL BUILD, buộc quyết định CÓ Ý THỨC (đổi code = phối hợp client). Code template (vd
/// <c>{entity}.not_found</c> — cần tham số) KHÔNG nằm trong registry (không phải hằng ổn định).
/// </summary>
public sealed class ErrorCodeSnapshotTests
{
    // SNAPSHOT ĐÃ DUYỆT (sắp Ordinal). Cập nhật CÓ Ý THỨC khi thêm/đổi code (kèm review breaking-change F20).
    private static readonly string[] ExpectedCodes =
    [
        "concurrency_conflict",
        "conflict",
        "forbidden",
        "identity.invalid_refresh_token",
        "not_found",
        "rate_limited",
        "unauthorized",
        "unexpected",
        "validation_error",
    ];

    [Fact]
    public void Error_code_registry_matches_snapshot()
    {
        Assembly[] catalogAssemblies =
        [
            typeof(Error).Assembly,        // Bedrock.Domain (CommonErrors + Error)
            typeof(AuthErrors).Assembly,   // Identity.Domain (module error catalog)
        ];

        var actual = CollectStableErrorCodes(catalogAssemblies);

        Assert.Equal(ExpectedCodes, actual);
    }

    /// <summary>
    /// Thu MỌI code ổn định: static field kiểu <see cref="Error"/> + static method trả <see cref="Error"/> mà
    /// TẤT CẢ tham số có default (invoke bằng default → code cố định). Bỏ code rỗng (<see cref="Error.None"/>).
    /// Trả danh sách sắp Ordinal, distinct.
    /// </summary>
    private static string[] CollectStableErrorCodes(params Assembly[] assemblies)
    {
        var codes = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (field.FieldType == typeof(Error) && field.GetValue(null) is Error fieldError && fieldError.Code.Length > 0)
                    {
                        codes.Add(fieldError.Code);
                    }
                }

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (method.ReturnType != typeof(Error))
                    {
                        continue;
                    }

                    var parameters = method.GetParameters();
                    if (!parameters.All(p => p.HasDefaultValue))
                    {
                        continue; // code template (cần tham số) — không phải hằng ổn định.
                    }

                    var args = parameters.Select(p => p.DefaultValue).ToArray();
                    if (method.Invoke(null, args) is Error methodError && methodError.Code.Length > 0)
                    {
                        codes.Add(methodError.Code);
                    }
                }
            }
        }

        return [.. codes];
    }
}
