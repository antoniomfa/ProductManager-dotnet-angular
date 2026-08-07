using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProductManager.Application.Exceptions;
using ProductManager.Domain.Exceptions;

namespace ProductManager.Api.ErrorHandling;

/// <summary>
/// Translates every unhandled exception into an RFC 7807 problem document.
/// I decided to use this exception handler I've used previously in other of projects as it works as intended.
/// </summary>
/// <remarks>
/// Centralising this is what lets controllers stay free of try/catch: a service throws the
/// exception that describes what went wrong, and exactly one place decides how that maps onto
/// HTTP. Unexpected exceptions are logged in full but reported to the caller without detail,
/// so internals never leak into a response.
/// </remarks>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // A cancelled request has no client left to answer; writing a response would only
        // produce a second, noisier exception.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request {Path} was cancelled by the client.", httpContext.Request.Path);
            return true;
        }

        var problemDetails = Describe(exception, httpContext);

        switch (problemDetails.Status)
        {
            case (int)HttpStatusCode.InternalServerError:
                Log(LogLevel.Error, exception, httpContext);
                break;
            default:
                Log(LogLevel.Warning, exception, httpContext);
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        });
    }

    private static ProblemDetails Describe(Exception exception, HttpContext httpContext)
    {
        ProblemDetails problemDetails;

        switch (exception)
        {
            case ValidationException validation:
                problemDetails = ValidationProblem(validation);
                break;

            case NotFoundException notFound:
                problemDetails = Problem(
                    HttpStatusCode.NotFound,
                    "Resource not found",
                    notFound.Message);
                break;

            case InsufficientStockException insufficientStock:
                problemDetails = StockProblem(insufficientStock);
                break;

            case InvalidProductStateException invalidState:
                problemDetails = Problem(
                    HttpStatusCode.BadRequest,
                    "Invalid product state",
                    invalidState.Message);
                break;

            default:
                problemDetails = Problem(
                    HttpStatusCode.InternalServerError,
                    "An unexpected error occurred",
                    "The request could not be completed. Please try again, or contact support if the problem persists.");
                break;
        }

        problemDetails.Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}";

        return problemDetails;
    }

    private static ProblemDetails Problem(HttpStatusCode status, string title, string detail) => new()
    {
        Status = (int)status,
        Title = title,
        Detail = detail,
        Type = $"https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/{(int)status}",
    };

    private static ProblemDetails ValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        var problemDetails = Problem(
            HttpStatusCode.BadRequest,
            "One or more validation errors occurred",
            "The request payload failed validation. See 'errors' for details.");

        problemDetails.Extensions["errors"] = errors;

        return problemDetails;
    }

    private static ProblemDetails StockProblem(InsufficientStockException exception)
    {
        var problemDetails = Problem(
            HttpStatusCode.Conflict,
            "Insufficient stock",
            exception.Message);

        // Surfaced as first-class fields so a client can render "only N left" without
        // resorting to parsing the message text.
        problemDetails.Extensions["productId"] = exception.ProductId;
        problemDetails.Extensions["availableStock"] = exception.Available;
        problemDetails.Extensions["requestedQuantity"] = exception.Requested;

        return problemDetails;
    }

    private bool Log(LogLevel level, Exception exception, HttpContext httpContext)
    {
        _logger.Log(
            level,
            exception,
            "Request {Method} {Path} failed: {ExceptionType}.",
            httpContext.Request.Method,
            httpContext.Request.Path,
            exception.GetType().Name);

        return true;
    }
}
