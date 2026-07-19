using System.Collections.Concurrent;
using System.Reflection;
using Bedrock.Application.Behaviors;

namespace Bedrock.Application.Authorization;

/// <summary>
/// Khai báo (declarative) một permission BẮT BUỘC để chạy use case, gắn trên KIỂU input (command/query).
/// <see cref="AuthorizationUseCaseDecorator{TInput,TOutput}"/> đọc mọi attribute trên <c>typeof(TInput)</c> và
/// yêu cầu <see cref="Bedrock.Application.Ports.Users.ICurrentUser.HasPermission(string)"/> đúng CHO TỪNG permission
/// (AND — least privilege, AD-038). Đặt trên kiểu (không phải instance) vì permission là hằng của loại thao tác →
/// đọc được từ metadata, cache được, không phụ thuộc giá trị runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : Attribute
{
    public RequirePermissionAttribute(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission phải khác rỗng.", nameof(permission));
        }

        Permission = permission;
    }

    /// <summary>Mã permission (chuỗi ổn định do app/module định nghĩa — lõi không hardcode).</summary>
    public string Permission { get; }
}

/// <summary>
/// Đọc + cache tập permission khai báo trên một kiểu input. Cache theo <see cref="Type"/> (attribute là hằng
/// của kiểu) → phản chiếu reflection chỉ chạy một lần cho mỗi kiểu. Trả mảng RỖNG (không null) khi không khai →
/// caller hiểu là "không yêu cầu permission" (pass-through).
/// </summary>
public static class PermissionMetadata
{
    private static readonly ConcurrentDictionary<Type, string[]> Cache = new();

    public static string[] For(Type inputType)
    {
        ArgumentNullException.ThrowIfNull(inputType);

        return Cache.GetOrAdd(inputType, static t =>
            [.. t.GetCustomAttributes<RequirePermissionAttribute>(inherit: true)
                 .Select(attribute => attribute.Permission)
                 .Distinct(StringComparer.Ordinal)]);
    }
}
