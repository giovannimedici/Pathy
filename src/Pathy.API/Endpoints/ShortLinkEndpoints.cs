using Pathy.Application.ShortLinks.Dtos;
using Pathy.Application.ShortLinks.UseCases;
using QuickJwt.AspNetCore;

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

        group.MapGet("", ListUserLinksAsync)
            .WithName("ListUserLinks")
            .WithSummary("List user's short links")
            .WithDescription("Returns a paginated list of links created by the authenticated user. Supports filtering by status and creation date.")
            .Produces<PagedResponse<LinkListItemResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AddEndpointFilter<JwtEndpointFilter>();
            

        group.MapGet("{id:guid}", GetLinkByIdAsync)
            .WithName("GetLinkById")
            .WithSummary("Get link details")
            .WithDescription("Returns detailed information about a specific link. Only the owner can view the link.")
            .Produces<LinkDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<JwtEndpointFilter>();

        group.MapPatch("{id:guid}", UpdateLinkAsync)
            .WithName("UpdateLink")
            .WithSummary("Update link details")
            .WithDescription("Updates the destination URL and/or expiration date of a link. Only the owner can update the link. " +
                           "All changes are logged for audit purposes.")
            .Produces<LinkDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<JwtEndpointFilter>();

        group.MapPost("{id:guid}/deactivate", DeactivateLinkAsync)
            .WithName("DeactivateLink")
            .WithSummary("Deactivate a link (soft delete)")
            .WithDescription("Deactivates a link, making it unavailable for redirects. The link and its history are preserved. " +
                           "Only the owner can deactivate the link. This action is logged for audit purposes.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<JwtEndpointFilter>();

        group.MapDelete("{id:guid}", HardDeleteLinkAsync)
            .WithName("HardDeleteLink")
            .WithSummary("Permanently delete a link (hard delete)")
            .WithDescription("Permanently deletes a link and all related data including click history and audit logs. " +
                           "This operation is irreversible and complies with LGPD/GDPR 'right to be forgotten'. " +
                           "Only the owner can delete the link.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<JwtEndpointFilter>();

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

    private static async Task<IResult> ListUserLinksAsync(
        [AsParameters] ListLinksRequest request,
        ListUserLinksUseCase useCase,
        CancellationToken cancellationToken)
    {
        var response = await useCase.ExecuteAsync(request, cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetLinkByIdAsync(
        Guid id,
        GetLinkByIdUseCase useCase,
        CancellationToken cancellationToken)
    {
        var response = await useCase.ExecuteAsync(id, cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> UpdateLinkAsync(
        Guid id,
        UpdateLinkRequest request,
        UpdateLinkUseCase useCase,
        CancellationToken cancellationToken)
    {
        var response = await useCase.ExecuteAsync(id, request, cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> DeactivateLinkAsync(
        Guid id,
        DeactivateLinkUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(id, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> HardDeleteLinkAsync(
        Guid id,
        HardDeleteLinkUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(id, cancellationToken);
        return Results.NoContent();
    }
}
