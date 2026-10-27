using FluentValidation.Results;

namespace Market.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs all FluentValidation validators for the request
/// before the handler. Any failure is thrown as <see cref="ValidationException"/>
/// (mapped to HTTP 400 "validation.error" by the global exception handler).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var failures = new List<ValidationFailure>();

        // One ValidationContext is shared by all validators, so they run one after another
        // (ValidationContext is not thread-safe and must not be used concurrently).
        var context = new ValidationContext<TRequest>(request);
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(context, ct);
            failures.AddRange(result.Errors);
        }

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
