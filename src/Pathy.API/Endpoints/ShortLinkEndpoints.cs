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
}
