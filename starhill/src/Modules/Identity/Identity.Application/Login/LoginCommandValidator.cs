using FluentValidation;

namespace Identity.Application.Login;

/// <summary>
/// Validator <see cref="LoginCommand"/> (chạy ở ValidationUseCaseDecorator §8 trước thân). CHỈ kiểm rỗng/độ dài
/// hợp lý — KHÔNG kiểm "đúng credential" (đó là việc của use case, trả một mã chung invalid_credentials). MaxLength
/// chặn payload bất thường (DoS nhẹ + Argon2 không băm chuỗi khổng lồ).
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
    }
}
