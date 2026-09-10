namespace Pathy.Application.ShortLinks.Dtos;

/// <summary>
/// Response DTO for a created or existing short link.
/// </summary>
public sealed record ShortLinkResponse
{
    /// <summary>
    /// Short slug identifier.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Complete shortened URL.
    /// </summary>
    public required string ShortUrl { get; init; }

    /// <summary>
    /// Original URL.
    /// </summary>
    public required string OriginalUrl { get; init; }

    /// <summary>
    /// Creation date.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Expiration date (nullable).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Indicates if the link is password-protected.
    /// </summary>
    public required bool IsPasswordProtected { get; init; }
}
