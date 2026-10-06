using FrappFresh.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FrappFresh.Api.Middleware;

// Centraliza los errores para no repetir try/catch en cada controller.
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var status = StatusCodes.Status500InternalServerError;
            if (exception is RequestValidationException || exception is BusinessRuleException)
                status = StatusCodes.Status400BadRequest;
            else if (exception is UnauthorizedException)
                status = StatusCodes.Status401Unauthorized;
            else if (exception is ForbiddenException)
                status = StatusCodes.Status403Forbidden;
            else if (exception is NotFoundException)
                status = StatusCodes.Status404NotFound;
            else if (exception is ConflictException)
                status = StatusCodes.Status409Conflict;

            ProblemDetails problem;
            if (exception is RequestValidationException validation)
                problem = new ValidationProblemDetails(validation.Errors.ToDictionary(x => x.Key, x => x.Value));
            else
                problem = new ProblemDetails();

            problem.Status = status;
            problem.Title = status == 500 ? "Ocurrió un error inesperado." : exception.Message;
            problem.Instance = context.Request.Path;
            if (status == 500)
                logger.LogError(exception, "Error al procesar {Path}", context.Request.Path);

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync((object)problem, options: null,
                contentType: "application/problem+json", cancellationToken: context.RequestAborted);
        }
    }
}
