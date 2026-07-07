using FluentValidation;
using FluentValidation.Results;
using ResortQr.SharedKernel.Results;

namespace ResortQr.Application.Common;

/// <summary>
/// Như <see cref="ValidationUseCaseDecorator{TInput,TOutput}"/> nhưng cho <see cref="ICommandUseCase{TInput}"/>
/// (use case không trả giá trị, vd logout). Chạy FluentValidation trước thân (Req 12.1/12.2).
/// </summary>
public sealed class ValidationCommandUseCaseDecorator<TInput> : ICommandUseCase<TInput>
{
    private readonly ICommandUseCase<TInput> _inner;
    private readonly List<IValidator<TInput>> _validators;

    public ValidationCommandUseCaseDecorator(ICommandUseCase<TInput> inner, IEnumerable<IValidator<TInput>> validators)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(validators);
        _inner = inner;
        _validators = validators.ToList();
    }

    public async Task<Result> ExecuteAsync(TInput input, CancellationToken cancellationToken = default)
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

                return Result.Fail(CommonErrors.Validation().WithDetails(details));
            }
        }

        return await _inner.ExecuteAsync(input, cancellationToken);
    }
}
