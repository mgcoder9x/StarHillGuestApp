using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Bedrock.Application.Behaviors;

/// <summary>
/// Như <see cref="ValidationUseCaseDecorator{TInput,TOutput}"/> nhưng cho <see cref="ICommandUseCase{TInput}"/>
/// (use case không trả giá trị). Chạy FluentValidation trước thân.
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
        _validators = [.. validators];
    }

    public async Task<Result> ExecuteAsync(TInput input, CancellationToken ct = default)
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
                return Result.Failure(CommonErrors.Validation().WithDetails(
                    ValidationUseCaseDecorator<TInput, object>.ToDetails(failures)));
            }
        }

        return await _inner.ExecuteAsync(input, ct).ConfigureAwait(false);
    }

    public string? PersistenceKey => _inner.PersistenceKey;
}
