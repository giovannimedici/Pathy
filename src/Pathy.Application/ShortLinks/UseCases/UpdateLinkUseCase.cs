using Microsoft.AspNetCore.Http;
using Pathy.Application.ShortLinks.Dtos;
using Pathy.Application.ShortLinks.Interfaces;
using Pathy.Domain.Entities;
using Pathy.Domain.Exceptions;

namespace Pathy.Application.ShortLinks.UseCases;

/// <summary>
/// Use case for updating a short link with audit logging.
/// </summary>
public sealed class UpdateLinkUseCase
{
    private readonly IShortLinkRepository _repository;
    private readonly ILinkAuditLogRepository _auditLogRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateLinkUseCase(
        IShortLinkRepository repository,
        ILinkAuditLogRepository auditLogRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Executes the use case to update a link.
    /// </summary>
    /// <param name="id">Link ID to update.</param>
    /// <param name="request">Update request with new values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated link details.</returns>
    public async Task<LinkDetailResponse> ExecuteAsync(
        Guid id,
        UpdateLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedException("Authentication required to update links.");
        }

        // Validate that at least one field is being updated
        if (string.IsNullOrWhiteSpace(request.OriginalUrl) && 
            !request.ExpiresAt.HasValue && 
            !request.RemoveExpiration)
        {
            throw new DomainException("At least one field must be provided for update.");
        }

        // Find link with ownership validation
        var link = await _repository.FindByIdAndUserAsync(id, userId, cancellationToken);

        if (link is null)
        {
            // Return 404 regardless of whether link doesn't exist or belongs to another user
            throw new NotFoundException($"Link with ID '{id}' not found.");
        }

        // TODO: When soft delete is implemented, validate that link is not inactive
        // if (link.Status == LinkStatus.Inactive)
        // {
        //     throw new DomainException("Cannot update an inactive link.");
        // }

        var auditLogs = new List<LinkAuditLog>();

        // Update OriginalUrl if provided
        if (!string.IsNullOrWhiteSpace(request.OriginalUrl))
        {
            var oldValue = link.OriginalUrl;
            link.UpdateDestinationUrl(request.OriginalUrl);
            
            auditLogs.Add(LinkAuditLog.Create(
                linkId: link.Id,
                userId: userId,
                fieldName: "OriginalUrl",
                oldValue: oldValue,
                newValue: request.OriginalUrl));
        }

        // Update ExpiresAt if provided or if removal is requested
        if (request.RemoveExpiration || request.ExpiresAt.HasValue)
        {
            var oldValue = link.ExpiresAt?.ToString("O");
            var newExpiresAt = request.RemoveExpiration ? null : request.ExpiresAt;
            
            link.UpdateExpiresAt(newExpiresAt);
            
            auditLogs.Add(LinkAuditLog.Create(
                linkId: link.Id,
                userId: userId,
                fieldName: "ExpiresAt",
                oldValue: oldValue,
                newValue: newExpiresAt?.ToString("O")));
        }

        // Save changes
        await _repository.UpdateAsync(link, cancellationToken);
        
        // Save audit logs
        foreach (var auditLog in auditLogs)
        {
            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

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
