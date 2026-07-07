using FluentValidation;
using FluentValidation.Results;
using ResortQr.SharedKernel.Results;

namespace ResortQr.Application.Common;

/// <summary>
/// Decorator chạy FluentValidation TRƯỚC thân use case (Req 12.1). Input sai → trả
/// <c>validation_error</c> kèm danh sách field lỗi (Req 12.2), KHÔNG chạy thân (không đổi trạng thái).
/// Không có validator cho <typeparamref name="TInput"/> → đi thẳng vào inner (pass-through).
/// Đăng ký qua Scrutor Decorate (kế thừa lifetime của use case bị bọc).
/// </summary>
public sealed class ValidationUseCaseDecorator<TInput, TOutput> : IUseCase<TInput, TOutput>
{
    private readonly IUseCase<TInput, TOutput> _inner;
    private readonly List<IValidator<TInput>> _validators;

    public ValidationUseCaseDecorator(IUseCase<TInput, TOutput> inner, IEnumerable<IValidator<TInput>> validators)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(validators);
        _inner = inner;
        _validators = validators.ToList();
    }

    public async Task<Result<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default)
    {
        if (_validators.Count > 0)
        {
            var context = new ValidationContext<TInput>(input);
            var failures = new List<ValidationFailure>();

            foreach (var validator in _validators)
            {
                var result = await validator.ValidateAsync(context, cancellationToken);
                if (!result.IsValid)
                {
                    failures.AddRange(result.Errors);
                }
            }

            if (failures.Count > 0)
            {
                var details = failures
                    .GroupBy(f => f.PropertyName, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray(), StringComparer.Ordinal);

                return Result.Fail<TOutput>(CommonErrors.Validation().WithDetails(details));
            }
        }

        return await _inner.ExecuteAsync(input, cancellationToken);
    }
}
