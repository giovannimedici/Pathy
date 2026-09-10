namespace Pathy.Application.ShortLinks.Dtos;

/// <summary>
/// Request DTO for creating a short link.
/// </summary>
public sealed record CreateShortLinkRequest
{
    /// <summary>
    /// Original URL to be shortened.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Custom slug for the short link. Only available for authenticated users.
    /// </summary>
    public string? CustomSlug { get; init; }

    /// <summary>
    /// Optional expiration date for the link. Only available for authenticated users.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Optional password to protect the link. Only available for authenticated users.
    /// </summary>
    public string? Password { get; init; }
}
