using Pathy.Application.ShortLinks.Interfaces;
using Pathy.Domain.Entities;

namespace Pathy.Tests.Application.Helpers;

/// <summary>
/// In-memory fake implementation of <see cref="ILinkAuditLogRepository"/> for unit tests.
/// </summary>
internal sealed class FakeLinkAuditLogRepository : ILinkAuditLogRepository
{
    private readonly List<LinkAuditLog> _logs = [];

    public IReadOnlyList<LinkAuditLog> Logs => _logs;

    public Task AddAsync(LinkAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        _logs.Add(auditLog);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
