using Microsoft.EntityFrameworkCore;
using Pathy.Application.ShortLinks.Interfaces;
using Pathy.Domain.Entities;
using Pathy.Infrastructure.Data;

namespace Pathy.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of IShortLinkRepository.
/// </summary>
public sealed class ShortLinkRepository : IShortLinkRepository
{
    private readonly PathyDbContext _context;

    public ShortLinkRepository(PathyDbContext context)
    {
        _context = context;
    }

    public async Task<ShortLink?> FindByUserAndUrlAsync(
        Guid? userId,
        string originalUrl,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShortLinks
            .Where(link => link.UserId == userId && link.OriginalUrl == originalUrl)
            .OrderByDescending(link => link.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ShortLink?> FindBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        
        return await _context.ShortLinks
            .Where(link => 
                link.Slug == slug &&
                link.Status == Domain.Enums.LinkStatus.Active &&
                (link.ExpiresAt == null || link.ExpiresAt > now))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ShortLink?> FindBySlugWithoutFiltersAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShortLinks
            .Where(link => link.Slug == slug)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShortLinks
            .AnyAsync(link => link.Slug == slug, cancellationToken);
    }

    public async Task AddAsync(
        ShortLink shortLink,
        CancellationToken cancellationToken = default)
    {
        await _context.ShortLinks.AddAsync(shortLink, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ShortLink?> FindByIdAndUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShortLinks
            .Where(link => link.Id == id && link.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(List<ShortLink> Items, int TotalCount)> ListByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        Domain.Enums.LinkStatus? status,
        DateTimeOffset? createdFrom,
        DateTimeOffset? createdTo,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ShortLinks
            .Where(link => link.UserId == userId);

        // Apply status filter
        if (status.HasValue)
        {
            query = query.Where(link => link.Status == status.Value);
        }

        // Apply date range filters
        if (createdFrom.HasValue)
        {
            query = query.Where(link => link.CreatedAt >= createdFrom.Value);
        }

        if (createdTo.HasValue)
        {
            // Include the entire day of createdTo
            var endOfDay = createdTo.Value.Date.AddDays(1);
            query = query.Where(link => link.CreatedAt < endOfDay);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var items = await query
            .OrderByDescending(link => link.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task UpdateAsync(
        ShortLink shortLink,
        CancellationToken cancellationToken = default)
    {
        _context.ShortLinks.Update(shortLink);
        await Task.CompletedTask;
    }
}
