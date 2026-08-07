using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProductManager.Api.Filters;

/// <summary>
/// Runs the registered FluentValidation validator for every action argument that has one,
/// before the action body executes.
/// </summary>
/// <remarks>
/// Applied globally so validation cannot be forgotten on a new endpoint; the rule is "if a
/// validator exists for this type, it runs", rather than each action opting in. Failures are
/// thrown rather than short-circuited here, so <see cref="ErrorHandling.GlobalExceptionHandler"/>
/// remains the single place that decides what a validation failure looks like on the wire.
/// </remarks>
internal sealed class FluentValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public FluentValidationFilter(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (_serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);

            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        await next();
    }
}
