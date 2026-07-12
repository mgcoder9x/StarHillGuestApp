using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Bedrock.Application.Behaviors;

/// <summary>
/// Decorator chạy FluentValidation TRƯỚC thân use case. Input sai → trả <c>validation_error</c> kèm danh sách
/// field lỗi, KHÔNG chạy thân (không đổi trạng thái). Không có validator cho <typeparamref name="TInput"/> →
/// pass-through. Đăng ký qua Decorate (kế thừa lifetime của use case bị bọc).
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
        _validators = [.. validators];
    }

    public async Task<Result<TOutput>> ExecuteAsync(TInput input, CancellationToken ct = default)
    {
        if (_validators.Count > 0)
        {
            var context = new ValidationContext<TInput>(input);
            var failures = new List<ValidationFailure>();

            foreach (var validator in _validators)
            {
                var result = await validator.ValidateAsync(context, ct).ConfigureAwait(false);
                if (!result.IsValid)
                {
                    failures.AddRange(result.Errors);
                }
            }

            if (failures.Count > 0)
            {
                return Result<TOutput>.Failure(CommonErrors.Validation().WithDetails(ToDetails(failures)));
            }
        }

        return await _inner.ExecuteAsync(input, ct).ConfigureAwait(false);
    }

    internal static IReadOnlyDictionary<string, string[]> ToDetails(IEnumerable<ValidationFailure> failures) =>
        failures
            .GroupBy(f => f.PropertyName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray(), StringComparer.Ordinal);
}
