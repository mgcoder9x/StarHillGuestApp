using FluentValidation;

namespace Identity.Application.RefreshToken;

/// <summary>Validator cho <see cref="RefreshTokenCommand"/> — chạy trong ValidationUseCaseDecorator (§8) trước thân.</summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator() =>
        RuleFor(x => x.RawRefreshToken)
            .NotEmpty();
}
