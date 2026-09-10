using Pathy.Application.ShortLinks.Dtos;
using Pathy.Application.ShortLinks.UseCases;

namespace Pathy.API.Endpoints;

/// <summary>
/// Endpoints for short link operations.
/// </summary>
public static class ShortLinkEndpoints
{
    /// <summary>
    /// Maps short link endpoints to the application.
    /// </summary>
    public static void MapShortLinkEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/links")
            .WithTags("ShortLinks")
            .WithOpenApi();

        group.MapPost("", CreateShortLinkAsync)
            .WithName("CreateShortLink")
            .WithSummary("Create a new short link")
            .WithDescription("Creates a new short link. Authenticated users can use custom slugs, expiration dates, and password protection. " +
                           "For authenticated users, if the same URL was already shortened, returns the existing link (idempotency).")
            .Produces<ShortLinkResponse>(StatusCodes.Status201Created)
            .Produces<ShortLinkResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // GET /{slug} endpoint - redirect to original URL
        app.MapGet("/{slug}", GetShortLinkAsync)
            .WithName("GetShortLink")
            .WithTags("ShortLinks")
            .WithSummary("Redirect to original URL")
            .WithDescription("Redirects to the original URL. Returns 401 if password is required, 404 if not found or deactivated, 410 if expired.")
            .Produces(StatusCodes.Status302Found)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status410Gone)
            .WithOpenApi();
    }

    private static async Task<IResult> CreateShortLinkAsync(
        CreateShortLinkRequest request,
        CreateShortLinkUseCase useCase,
        CancellationToken cancellationToken)
    {
        var (response, isNew) = await useCase.ExecuteAsync(request, cancellationToken);

        return isNew
            ? Results.Created($"/{response.Slug}", response)
            : Results.Ok(response);
    }

    private static async Task<IResult> GetShortLinkAsync(
        string slug,
        GetShortLinkUseCase useCase,
        CancellationToken cancellationToken)
    {
        var response = await useCase.ExecuteAsync(slug, cancellationToken);

        // If password is required, return 401 Unauthorized
        if (response.RequiresPassword)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Password Required",
                detail: "This link is password-protected. Please provide a valid password to access it.",
                type: "https://tools.ietf.org/html/rfc7235#section-3.1");
        }

        // Otherwise, redirect to the original URL
        return Results.Redirect(response.OriginalUrl!);
    }
}
