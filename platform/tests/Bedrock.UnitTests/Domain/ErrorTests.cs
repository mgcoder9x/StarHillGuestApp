using Bedrock.Domain.Results;
using Xunit;

namespace Bedrock.UnitTests.Domain;

/// <summary>
/// A-26 guard: <see cref="Error"/> ENFORCE invariant code/message KHÔNG rỗng ngay ở constructor (record positional
/// cũ không validate → tạo được Error rỗng vô nghĩa). <see cref="Error.None"/> là ngoại lệ CHỦ ĐÍCH (sentinel cho
/// Result.Success). Kèm: mọi factory gắn đúng <see cref="ErrorType"/>; <c>RateLimited</c> là first-class type (A-26).
/// </summary>
public sealed class ErrorTests
{
    [Theory]
    [InlineData("", "message")]
    [InlineData("  ", "message")]
    [InlineData("code", "")]
    [InlineData("code", "   ")]
    public void Constructor_rejects_blank_code_or_message(string code, string message)
    {
        Assert.Throws<ArgumentException>(() => new Error(code, message, ErrorType.Validation));
    }

    [Fact]
    public void Valid_error_is_constructed()
    {
        var error = new Error("some_code", "Some message.", ErrorType.Conflict);
        Assert.Equal("some_code", error.Code);
        Assert.Equal("Some message.", error.Message);
        Assert.Equal(ErrorType.Conflict, error.Type);
    }

    [Fact]
    public void None_sentinel_has_empty_code_and_message()
    {
        // None là NGOẠI LỆ chủ đích (không đi qua invariant) — dùng cho Result.Success (Error = None).
        Assert.Equal(string.Empty, Error.None.Code);
        Assert.Equal(string.Empty, Error.None.Message);
    }

    [Theory]
    [InlineData(nameof(Error.Validation), ErrorType.Validation)]
    [InlineData(nameof(Error.NotFound), ErrorType.NotFound)]
    [InlineData(nameof(Error.Conflict), ErrorType.Conflict)]
    [InlineData(nameof(Error.Forbidden), ErrorType.Forbidden)]
    [InlineData(nameof(Error.Unauthorized), ErrorType.Unauthorized)]
    [InlineData(nameof(Error.RateLimited), ErrorType.RateLimited)]
    [InlineData(nameof(Error.Unexpected), ErrorType.Failure)]
    public void Factory_sets_expected_error_type(string factory, ErrorType expected)
    {
        var error = factory switch
        {
            nameof(Error.Validation) => Error.Validation("c", "m"),
            nameof(Error.NotFound) => Error.NotFound("c", "m"),
            nameof(Error.Conflict) => Error.Conflict("c", "m"),
            nameof(Error.Forbidden) => Error.Forbidden("c", "m"),
            nameof(Error.Unauthorized) => Error.Unauthorized("c", "m"),
            nameof(Error.RateLimited) => Error.RateLimited("c", "m"),
            nameof(Error.Unexpected) => Error.Unexpected("c", "m"),
            _ => throw new ArgumentOutOfRangeException(nameof(factory)),
        };
        Assert.Equal(expected, error.Type);
    }

    // ---- P1-08: invariant fields GET-ONLY (không init) → không bypass constructor qua record `with` ----

    [Theory]
    [InlineData(nameof(Error.Code))]
    [InlineData(nameof(Error.Message))]
    [InlineData(nameof(Error.Type))]
    public void Invariant_fields_have_no_public_setter(string propertyName)
    {
        // P1-08: nếu Code/Message/Type có init/set public → caller `validError with { Code = "" }` vượt validation
        // constructor → invariant A-26 bị vô hiệu. Get-only đóng đường đó; reflection khẳng định không có setter.
        var property = typeof(Error).GetProperty(propertyName)!;
        Assert.Null(property.SetMethod); // get-only auto-property ⇒ không có set/init accessor.
    }

    [Fact]
    public void Details_remains_settable_for_with_expression()
    {
        // Details CÒN init (để WithDetails clone) — đây là field không thuộc invariant code/message/type.
        Assert.NotNull(typeof(Error).GetProperty(nameof(Error.Details))!.SetMethod);
    }

    [Fact]
    public void WithDetails_preserves_core_fields_and_attaches_details()
    {
        var error = Error.Validation("v_code", "invalid");
        var details = new Dictionary<string, string[]> { ["name"] = ["required"] };

        var withDetails = error.WithDetails(details);

        // Core bất biến giữ nguyên qua clone; chỉ Details thay đổi.
        Assert.Equal("v_code", withDetails.Code);
        Assert.Equal("invalid", withDetails.Message);
        Assert.Equal(ErrorType.Validation, withDetails.Type);
        Assert.NotNull(withDetails.Details);
        Assert.Equal(["required"], withDetails.Details!["name"]);
        Assert.Null(error.Details); // bản gốc không bị đụng (immutable).
    }
}
