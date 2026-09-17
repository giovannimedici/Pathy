namespace Pathy.Application.ShortLinks.Dtos;

/// <summary>
/// Response DTO for detailed link information.
/// Currently identical to LinkListItemResponse, but kept separate for future extensibility.
/// </summary>
public sealed record LinkDetailResponse
{
    /// <summary>
    /// Link unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Short slug identifier.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Original destination URL.
    /// </summary>
    public required string OriginalUrl { get; init; }

    /// <summary>
    /// Complete shortened URL.
    /// </summary>
    public required string ShortUrl { get; init; }

    /// <summary>
    /// Link status (active, inactive).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Expiration timestamp (nullable).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Indicates if the link is password-protected.
    /// </summary>
    public required bool IsPasswordProtected { get; init; }
}
