using Pathy.Domain.Entities;

namespace Pathy.Application.ShortLinks.Interfaces;

/// <summary>
/// Repository interface for LinkAuditLog entity operations.
/// </summary>
public interface ILinkAuditLogRepository
{
    /// <summary>
    /// Adds a new audit log entry to the repository.
    /// </summary>
    /// <param name="auditLog">Audit log entry to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(LinkAuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
