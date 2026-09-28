namespace Pathy.Domain.Entities;

/// <summary>
/// Represents an audit log entry for link modifications.
/// Tracks changes to link destination URL and expiration date.
/// </summary>
public sealed class LinkAuditLog
{
    /// <summary>
    /// Unique identifier for this audit log entry.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// ID of the link that was modified.
    /// </summary>
    public Guid LinkId { get; private set; }

    /// <summary>
    /// ID of the user who made the change.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Name of the field that was changed (e.g., "OriginalUrl", "ExpiresAt").
    /// </summary>
    public string FieldName { get; private set; } = string.Empty;

    /// <summary>
    /// Previous value before the change (serialized as string).
    /// </summary>
    public string? OldValue { get; private set; }

    /// <summary>
    /// New value after the change (serialized as string).
    /// </summary>
    public string? NewValue { get; private set; }

    /// <summary>
    /// Timestamp when the change occurred.
    /// </summary>
    public DateTimeOffset ChangedAt { get; private set; }

    private LinkAuditLog()
    {
    }

    /// <summary>
    /// Creates a new audit log entry.
    /// </summary>
    /// <param name="linkId">ID of the modified link.</param>
    /// <param name="userId">ID of the user who made the change.</param>
    /// <param name="fieldName">Name of the changed field.</param>
    /// <param name="oldValue">Previous value.</param>
    /// <param name="newValue">New value.</param>
    /// <returns>A new LinkAuditLog instance.</returns>
    public static LinkAuditLog Create(
        Guid linkId,
        Guid userId,
        string fieldName,
        string? oldValue,
        string? newValue)
    {
        return new LinkAuditLog
        {
            Id = Guid.NewGuid(),
            LinkId = linkId,
            UserId = userId,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedAt = DateTimeOffset.UtcNow
        };
    }
}
