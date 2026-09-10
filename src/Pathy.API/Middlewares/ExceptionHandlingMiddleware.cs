using Microsoft.AspNetCore.Mvc;
using Pathy.Domain.Exceptions;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex,
                "Unauthorized operation. Message={Message} Path={Path}",
                ex.Message, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = 401,
                Title = "Unauthorized",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
            });
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex,
                "Resource conflict. Message={Message} Path={Path}",
                ex.Message, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = 409,
                Title = "Conflict",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            });
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex,
                "Domain rule violation. Message={Message} Path={Path}",
                ex.Message, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = 422,
                Title = "Domain rule violation",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc4918#section-11.2"
            });
        }
        catch (NotFoundException ex)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = 404,
                Title = "Resource not found",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception. Message={Message} Path={Path}",
                ex.Message, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = 500,
                Title = "Internal Server Error",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            });
        }
    }
}