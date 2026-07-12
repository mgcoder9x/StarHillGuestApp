using System.Reflection;

namespace Bedrock.ContractTests;

/// <summary>
/// A-22 — mô tả CANONICAL kiểu/property cho contract snapshot (event/error). Chính xác hơn <c>Type.Name</c> thuần:
/// render ĐẦY ĐỦ generic arguments (<c>List&lt;String&gt;</c> ≠ <c>List&lt;Int32&gt;</c>), mảng (<c>String[]</c>), và
/// nullability CẢ value-type (<c>Guid?</c>) LẪN reference-type (<c>String?</c> qua <see cref="NullabilityInfoContext"/>).
/// Mục tiêu: snapshot bắt được nhiều dạng breaking-change hơn (đổi generic arg / thêm nullable / đổi element mảng) —
/// làm mạnh chính cơ chế chống-drift, không chỉ dựa tên rút gọn.
/// </summary>
internal static class ContractSchema
{
    /// <summary>Mô tả một property = <c>{FriendlyType + nullability}</c> (dùng NullabilityInfoContext cho ref-type).</summary>
    public static string DescribeProperty(PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);
        var type = property.PropertyType;

        // Nullable<value> (vd Guid?/Int32?) → underlying + "?".
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return $"{FriendlyTypeName(underlying)}?";
        }

        var name = FriendlyTypeName(type);

        // Reference-type nullable (vd string?) — chỉ biết qua annotation, không qua Type.
        if (!type.IsValueType)
        {
            var nullability = new NullabilityInfoContext().Create(property);
            if (nullability.ReadState == NullabilityState.Nullable)
            {
                return $"{name}?";
            }
        }

        return name;
    }

    /// <summary>Tên kiểu canonical: generic args đầy đủ + mảng đệ quy + Nullable&lt;value&gt; hậu tố "?".</summary>
    public static string FriendlyTypeName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (Nullable.GetUnderlyingType(type) is { } inner)
        {
            return $"{FriendlyTypeName(inner)}?";
        }

        if (type.IsArray)
        {
            return $"{FriendlyTypeName(type.GetElementType()!)}[]";
        }

        if (type.IsGenericType)
        {
            var backtick = type.Name.IndexOf('`', StringComparison.Ordinal);
            var rawName = backtick >= 0 ? type.Name[..backtick] : type.Name;
            var args = string.Join(", ", type.GetGenericArguments().Select(FriendlyTypeName));
            return $"{rawName}<{args}>";
        }

        return type.Name;
    }
}
