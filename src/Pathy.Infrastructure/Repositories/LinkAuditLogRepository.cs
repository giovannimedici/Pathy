using Pathy.Application.ShortLinks.Interfaces;
using Pathy.Domain.Entities;
using Pathy.Infrastructure.Data;

namespace Pathy.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for LinkAuditLog entity.
/// </summary>
public sealed class LinkAuditLogRepository : ILinkAuditLogRepository
{
    private readonly PathyDbContext _context;

    public LinkAuditLogRepository(PathyDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LinkAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.LinkAuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
