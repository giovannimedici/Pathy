using Pathy.Application.ShortLinks.Dtos;
using Pathy.Application.ShortLinks.Interfaces;
using Pathy.Domain.Enums;
using Pathy.Domain.Exceptions;

namespace Pathy.Application.ShortLinks.UseCases;

/// <summary>
/// Use case for retrieving a short link by slug.
/// </summary>
public sealed class GetShortLinkUseCase
{
    private readonly IShortLinkRepository _repository;

    public GetShortLinkUseCase(IShortLinkRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Executes the use case to retrieve a short link.
    /// </summary>
    /// <param name="slug">The slug to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response with redirect URL or password requirement.</returns>
    /// <exception cref="NotFoundException">When slug doesn't exist or is deactivated.</exception>
    /// <exception cref="GoneException">When link exists but is expired.</exception>
    public async Task<GetShortLinkResponse> ExecuteAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        // Find the link without filters to differentiate between not found, expired, and inactive
        var link = await _repository.FindBySlugWithoutFiltersAsync(slug, cancellationToken);

        // Case 1: Slug doesn't exist at all
        if (link is null)
        {
            throw new NotFoundException("Short link not found.");
        }

        // Case 2: Link exists but is deactivated (soft delete)
        // Return the same error as "not found" to avoid leaking information
        if (link.Status == LinkStatus.Inactive)
        {
            throw new NotFoundException("Short link not found.");
        }

        // Case 3: Link exists and is active but expired
        if (link.ExpiresAt.HasValue && link.ExpiresAt.Value <= DateTimeOffset.UtcNow)
        {
            throw new GoneException("This link has expired and is no longer available.");
        }

        // Case 4: Link is valid and active but password-protected
        if (link.IsPasswordProtected)
        {
            return new GetShortLinkResponse
            {
                RequiresPassword = true,
                OriginalUrl = null
            };
        }

        // Case 5: Link is valid, active, not expired, and not password-protected
        return new GetShortLinkResponse
        {
            RequiresPassword = false,
            OriginalUrl = link.OriginalUrl
        };
    }
}
