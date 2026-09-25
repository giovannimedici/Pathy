using Microsoft.AspNetCore.Http;
using Pathy.Application.ShortLinks.Interfaces;
using Pathy.Domain.Exceptions;

namespace Pathy.Application.ShortLinks.UseCases;

/// <summary>
/// Use case for permanently deleting a short link (hard delete).
/// This operation is irreversible and deletes all related data (cascade delete)
/// in compliance with LGPD/GDPR "right to be forgotten".
/// </summary>
public sealed class HardDeleteLinkUseCase
{
    private readonly IShortLinkRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HardDeleteLinkUseCase(
        IShortLinkRepository repository,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Executes the use case to permanently delete a link and all related data.
    /// </summary>
    /// <param name="id">Link ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="UnauthorizedException">When user is not authenticated.</exception>
    /// <exception cref="NotFoundException">When link is not found or doesn't belong to the user.</exception>
    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        if (userId == Guid.Empty)
        {
            throw new UnauthorizedException("Authentication required to delete links.");
        }

        // Find link with ownership validation
        var link = await _repository.FindByIdAndUserAsync(id, userId, cancellationToken);

        if (link is null)
        {
            // Return 404 regardless of whether link doesn't exist or belongs to another user
            throw new NotFoundException($"Link with ID '{id}' not found.");
        }

        // Hard delete - removes link and all related data (audit logs, click history)
        // This is allowed for both active and inactive links
        await _repository.DeleteAsync(link, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
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
