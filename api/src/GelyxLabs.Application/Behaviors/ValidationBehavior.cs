namespace GelyxLabs.Application.Behaviors;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Common;
using FluentValidation;
using Microsoft.Extensions.Logging;

public class ValidationBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators, ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();
            if (failures.Count != 0)
            {
                _logger.LogWarning("Validation failed for {Request}: {Failures}", typeof(TRequest).Name, failures.Count);
                if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var resultType = typeof(TResponse).GetGenericArguments()[0];
                    var failureMethod = typeof(Result).GetMethod(nameof(Result.Failure))!.MakeGenericMethod(resultType);
                    var error = new Domain.Errors.Error("Error.Validation", string.Join(", ", failures.Select(f => f.ErrorMessage)));
                    return (TResponse)failureMethod.Invoke(null, [error])!;
                }
                else if (typeof(TResponse) == typeof(Result))
                {
                    var error = new Domain.Errors.Error("Error.Validation", string.Join(", ", failures.Select(f => f.ErrorMessage)));
                    return (TResponse)(object)Result.Failure(error);
                }
            }
        }
        return await next();
    }
}
