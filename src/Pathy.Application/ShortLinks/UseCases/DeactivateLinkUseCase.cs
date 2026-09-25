using Microsoft.AspNetCore.Http;
using Pathy.Application.ShortLinks.Interfaces;
using Pathy.Domain.Entities;
using Pathy.Domain.Exceptions;

namespace Pathy.Application.ShortLinks.UseCases;

/// <summary>
/// Use case for deactivating a short link (soft delete) with audit logging.
/// </summary>
public sealed class DeactivateLinkUseCase
{
    private readonly IShortLinkRepository _repository;
    private readonly ILinkAuditLogRepository _auditLogRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeactivateLinkUseCase(
        IShortLinkRepository repository,
        ILinkAuditLogRepository auditLogRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Executes the use case to deactivate a link (soft delete).
    /// </summary>
    /// <param name="id">Link ID to deactivate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="UnauthorizedException">When user is not authenticated.</exception>
    /// <exception cref="NotFoundException">When link is not found or doesn't belong to the user.</exception>
    /// <exception cref="ConflictException">When link is already inactive.</exception>
    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        if (userId == Guid.Empty)
        {
            throw new UnauthorizedException("Authentication required to deactivate links.");
        }

        // Find link with ownership validation
        var link = await _repository.FindByIdAndUserAsync(id, userId, cancellationToken);

        if (link is null)
        {
            // Return 404 regardless of whether link doesn't exist or belongs to another user
            throw new NotFoundException($"Link with ID '{id}' not found.");
        }

        // Check if link is already inactive - return 409 Conflict
        if (link.IsInactive)
        {
            throw new ConflictException("Link is already inactive.");
        }

        var oldStatus = link.Status.ToString();

        // Deactivate the link
        link.Deactivate();

        // Create audit log entry
        var auditLog = LinkAuditLog.Create(
            linkId: link.Id,
            userId: userId,
            fieldName: "Status",
            oldValue: oldStatus,
            newValue: link.Status.ToString());

        // Save changes
        await _repository.UpdateAsync(link, cancellationToken);
        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);
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
}
