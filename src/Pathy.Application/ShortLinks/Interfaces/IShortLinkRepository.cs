using Pathy.Domain.Entities;

namespace Pathy.Application.ShortLinks.Interfaces;

/// <summary>
/// Repository interface for ShortLink entity operations.
/// </summary>
public interface IShortLinkRepository
{
    /// <summary>
    /// Finds an existing short link by user ID and original URL.
    /// </summary>
    /// <param name="userId">User ID (null for anonymous).</param>
    /// <param name="originalUrl">Original URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Existing ShortLink or null if not found.</returns>
    Task<ShortLink?> FindByUserAndUrlAsync(Guid? userId, string originalUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a short link by slug.
    /// Only returns active, non-expired links. Expired or deactivated links are not returned.
    /// </summary>
    /// <param name="slug">Slug to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>ShortLink if found and valid, null otherwise.</returns>
    Task<ShortLink?> FindBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a short link by slug without any status or expiration filters.
    /// Used internally to differentiate between not found, expired, and inactive links.
    /// </summary>
    /// <param name="slug">Slug to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>ShortLink if found, null otherwise.</returns>
    Task<ShortLink?> FindBySlugWithoutFiltersAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a slug already exists.
    /// </summary>
    /// <param name="slug">Slug to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if slug exists, false otherwise.</returns>
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new short link to the repository.
    /// </summary>
    /// <param name="shortLink">ShortLink entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(ShortLink shortLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a link by ID and validates ownership.
    /// </summary>
    /// <param name="id">Link ID.</param>
    /// <param name="userId">User ID for ownership validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>ShortLink if found and owned by user, null otherwise.</returns>
    Task<ShortLink?> FindByIdAndUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns paginated list of links for a specific user with optional filters.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="page">Page number (1-indexed).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="status">Optional filter by link status.</param>
    /// <param name="createdFrom">Optional filter by creation date from (inclusive).</param>
    /// <param name="createdTo">Optional filter by creation date to (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple with list of links and total count.</returns>
    Task<(List<ShortLink> Items, int TotalCount)> ListByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        Pathy.Domain.Enums.LinkStatus? status,
        DateTimeOffset? createdFrom,
        DateTimeOffset? createdTo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing short link.
    /// </summary>
    /// <param name="shortLink">ShortLink entity to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(ShortLink shortLink, CancellationToken cancellationToken = default);
}
