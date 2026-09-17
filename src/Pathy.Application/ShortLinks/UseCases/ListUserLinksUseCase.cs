using Microsoft.AspNetCore.Http;
using Pathy.Application.ShortLinks.Dtos;
using Pathy.Application.ShortLinks.Interfaces;
using Pathy.Domain.Entities;
using Pathy.Domain.Enums;
using Pathy.Domain.Exceptions;

namespace Pathy.Application.ShortLinks.UseCases;

/// <summary>
/// Use case for listing user's short links with pagination and filters.
/// </summary>
public sealed class ListUserLinksUseCase
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;
    private const int DefaultPage = 1;

    private readonly IShortLinkRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ListUserLinksUseCase(
        IShortLinkRepository repository,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Executes the use case to list user's links.
    /// </summary>
    /// <param name="request">Request with pagination and filter parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of links.</returns>
    public async Task<PagedResponse<LinkListItemResponse>> ExecuteAsync(
        ListLinksRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedException("Authentication required to list links.");
        }

        // Sanitize pagination parameters
        var page = Math.Max(request.Page, DefaultPage);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        // Parse status filter
        LinkStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<LinkStatus>(request.Status, ignoreCase: true, out var parsedStatus))
            {
                throw new DomainException($"Invalid status value: '{request.Status}'. Valid values are: 'active', 'inactive'.");
            }
            status = parsedStatus;
        }

        // Validate date range
        if (request.CreatedFrom.HasValue && request.CreatedTo.HasValue &&
            request.CreatedFrom.Value > request.CreatedTo.Value)
        {
            throw new DomainException("CreatedFrom cannot be greater than CreatedTo.");
        }

        // Query repository
        var result = await _repository.ListByUserAsync(
            userId: userId,
            page: page,
            pageSize: pageSize,
            status: status,
            createdFrom: request.CreatedFrom,
            createdTo: request.CreatedTo,
            cancellationToken: cancellationToken);

        var items = result.Items;
        var totalCount = result.TotalCount;

        // Map to response
        var itemResponses = items.Select(MapToListItemResponse).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<LinkListItemResponse>
        {
            Items = itemResponses,
            Pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = totalPages
            }
        };
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

    private LinkListItemResponse MapToListItemResponse(ShortLink shortLink)
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

        return new LinkListItemResponse
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
