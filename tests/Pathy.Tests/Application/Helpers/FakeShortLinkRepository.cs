using Pathy.Application.ShortLinks.Interfaces;
using Pathy.Domain.Entities;
using Pathy.Domain.Enums;

namespace Pathy.Tests.Application.Helpers;

/// <summary>
/// In-memory fake implementation of <see cref="IShortLinkRepository"/> for unit tests.
/// </summary>
internal sealed class FakeShortLinkRepository : IShortLinkRepository
{
    private readonly List<ShortLink> _links = [];

    public IReadOnlyList<ShortLink> Links => _links;

    public void Seed(ShortLink link) => _links.Add(link);

    public Task<ShortLink?> FindByUserAndUrlAsync(
        Guid? userId,
        string originalUrl,
        CancellationToken cancellationToken = default)
    {
        var link = _links.FirstOrDefault(l => l.UserId == userId && l.OriginalUrl == originalUrl);
        return Task.FromResult(link);
    }

    public Task<ShortLink?> FindBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var link = _links.FirstOrDefault(l =>
            l.Slug == slug &&
            l.Status == LinkStatus.Active &&
            (!l.ExpiresAt.HasValue || l.ExpiresAt.Value > DateTimeOffset.UtcNow));

        return Task.FromResult(link);
    }

    public Task<ShortLink?> FindBySlugWithoutFiltersAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var link = _links.FirstOrDefault(l => l.Slug == slug);
        return Task.FromResult(link);
    }

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        var exists = _links.Any(l => l.Slug == slug);
        return Task.FromResult(exists);
    }

    public Task AddAsync(ShortLink shortLink, CancellationToken cancellationToken = default)
    {
        _links.Add(shortLink);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
