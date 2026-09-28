namespace Pathy.Application.ShortLinks.Dtos;

/// <summary>
/// Request DTO for updating a short link.
/// At least one field must be provided.
/// </summary>
public sealed record UpdateLinkRequest
{
    /// <summary>
    /// New destination URL. Optional.
    /// </summary>
    public string? OriginalUrl { get; init; }

    /// <summary>
    /// New expiration date. Optional. Set to null to remove expiration.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Flag to indicate if ExpiresAt should be explicitly set to null.
    /// This is needed because JSON deserialization treats missing fields as null.
    /// </summary>
    public bool RemoveExpiration { get; init; }
}
