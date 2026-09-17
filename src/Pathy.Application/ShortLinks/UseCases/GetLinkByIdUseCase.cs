using Microsoft.AspNetCore.Http;
using Pathy.Application.ShortLinks.Dtos;
using Pathy.Application.ShortLinks.Interfaces;
using Pathy.Domain.Entities;
using Pathy.Domain.Exceptions;

namespace Pathy.Application.ShortLinks.UseCases;

/// <summary>
/// Use case for retrieving a specific link by ID with ownership validation.
/// </summary>
public sealed class GetLinkByIdUseCase
{
    private readonly IShortLinkRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetLinkByIdUseCase(
        IShortLinkRepository repository,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Executes the use case to retrieve a link by ID.
    /// </summary>
    /// <param name="id">Link ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Link details.</returns>
    /// <exception cref="NotFoundException">Thrown if link not found or user doesn't own it.</exception>
    public async Task<LinkDetailResponse> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedException("Authentication required to view link details.");
        }

        var link = await _repository.FindByIdAndUserAsync(id, userId, cancellationToken);

        if (link is null)
        {
            // Return 404 regardless of whether link doesn't exist or belongs to another user
            // This prevents leaking information about the existence of other users' links
            throw new NotFoundException($"Link with ID '{id}' not found.");
        }

        return MapToDetailResponse(link);
    }

    private Guid GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = httpContext.User.FindFirst("sub")?.Value
                ?? httpContext.User.FindFirst("userId")?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
        }

        return Guid.Empty;
    }

    private LinkDetailResponse MapToDetailResponse(ShortLink shortLink)
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

        return new LinkDetailResponse
        {
            Id = shortLink.Id,
            Slug = shortLink.Slug,
            OriginalUrl = shortLink.OriginalUrl,
            ShortUrl = $"{baseUrl}/{shortLink.Slug}",
            Status = shortLink.Status.ToString().ToLower(),
            CreatedAt = shortLink.CreatedAt,
            ExpiresAt = shortLink.ExpiresAt,
            IsPasswordProtected = shortLink.IsPasswordProtected
        };
    }
}
