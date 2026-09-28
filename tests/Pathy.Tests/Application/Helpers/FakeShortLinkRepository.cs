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

    public Task<ShortLink?> FindByIdAndUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var link = _links.FirstOrDefault(l => l.Id == id && l.UserId == userId);
        return Task.FromResult(link);
    }

    public Task<(List<ShortLink> Items, int TotalCount)> ListByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        LinkStatus? status,
        DateTimeOffset? createdFrom,
        DateTimeOffset? createdTo,
        CancellationToken cancellationToken = default)
    {
        var query = _links.Where(l => l.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        if (createdFrom.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= createdFrom.Value);
        }

        if (createdTo.HasValue)
        {
            var endOfDay = createdTo.Value.Date.AddDays(1);
            query = query.Where(l => l.CreatedAt < endOfDay);
        }

        var totalCount = query.Count();
        var items = query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult((items, totalCount));
    }

    public Task UpdateAsync(ShortLink shortLink, CancellationToken cancellationToken = default)
    {
        // No need to do anything in memory, changes are already reflected
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ShortLink shortLink, CancellationToken cancellationToken = default)
    {
        _links.Remove(shortLink);
        return Task.CompletedTask;
    }
}
