using System.Reflection;
using Bedrock.Domain.Results;
using GuestAccess.Application;
using Identity.Domain;
using Rooms.Application;
using Rules.Application;

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
        "configuration_unavailable",     // GuestAccess (C-GA.2b)
        "conflict",
        "forbidden",
        "guest_context_missing",         // GuestAccess (C-GA.4: current-guest-context)
        "identity.invalid_refresh_token",
        "invalid_configuration",         // Rooms (P1-11: nay được gác)
        "not_found",
        "qr_generation_failed",          // Rooms (P1-11)
        "qr_invalid",                    // GuestAccess (C-GA.2b)
        "rate_limited",
        "resort_not_found",              // Rooms (P1-11)
        "room_inactive",                 // GuestAccess (C-GA.2b)
        "rules_conflict",                // Rules (D-Rules.2b: DraftConflict — nay được gác, đóng gap QR-AD-018)
        "rules_unavailable",             // Rules (D-Rules.4a: guest read chưa publish)
        "session_expired",               // GuestAccess (C-GA.4: portal-window)
        "unauthorized",
        "unexpected",
        "validation_error",
    ];

    [Fact]
    public void Error_code_registry_matches_snapshot()
    {
        Assembly[] catalogAssemblies =
        [
            typeof(Error).Assembly,              // Bedrock.Domain (CommonErrors + Error)
            typeof(AuthErrors).Assembly,         // Identity.Domain (module error catalog)
            typeof(RoomsErrors).Assembly,        // Rooms.Application (P1-11: error catalog module QR — trước đây KHÔNG gác)
            typeof(GuestAccessErrors).Assembly,  // GuestAccess.Application (C-GA.2b + C-GA.4)
            typeof(RulesErrors).Assembly,        // Rules.Application (D-Rules.2b/3: rules_conflict — đóng gap QR-AD-018)
        ];

        var actual = CollectStableErrorCodes(catalogAssemblies);

        Assert.Equal(ExpectedCodes, actual);
    }

    /// <summary>
    /// Thu MỌI code ổn định: static <see cref="Error"/> qua (a) FIELD, (b) PROPERTY get-only (P1-11 — catalog kiểu
    /// property như <c>RoomsErrors</c> trước đây LỌT lưới), (c) METHOD với MỌI tham số optional (invoke bằng default →
    /// code cố định). Bỏ code rỗng (<see cref="Error.None"/>). Trả danh sách sắp Ordinal, distinct. Code template
    /// (method cần tham số, vd <c>{entity}.not_found</c>) KHÔNG tính (không phải hằng ổn định).
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

                // P1-11: catalog dạng static get-only PROPERTY trả Error (RoomsErrors) — hằng ổn định, KHÔNG tham số.
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    if (property.PropertyType == typeof(Error)
                        && property.GetMethod is not null
                        && property.GetValue(null) is Error propertyError
                        && propertyError.Code.Length > 0)
                    {
                        codes.Add(propertyError.Code);
                    }
                }

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (method.ReturnType != typeof(Error))
                    {
                        continue;
                    }

                    // Bỏ getter của property (đã xử lý ở trên) — GetMethods trả cả accessor get_X.
                    if (method.IsSpecialName)
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
