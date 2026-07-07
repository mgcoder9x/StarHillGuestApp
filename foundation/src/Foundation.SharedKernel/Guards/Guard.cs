using System.Runtime.CompilerServices;

namespace Foundation.SharedKernel.Guards;

/// <summary>
/// Guard clauses tối giản, ném <see cref="ArgumentException"/> họ khi tiền điều kiện sai.
/// Trả lại value để dùng theo lối fluent. Dùng <c>[CallerArgumentExpression]</c> để tên tham số chính xác.
/// </summary>
public static class Guard
{
    public static T AgainstNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }

    public static string AgainstNullOrWhiteSpace(string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        return value;
    }

    public static int AgainstNegativeOrZero(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, paramName);
        return value;
    }

    public static Guid AgainstEmpty(Guid value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Giá trị Guid không được rỗng (Guid.Empty).", paramName);
        }

        return value;
    }
}
