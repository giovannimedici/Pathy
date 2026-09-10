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
}
