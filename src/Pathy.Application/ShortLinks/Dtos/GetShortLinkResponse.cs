namespace Pathy.Application.ShortLinks.Dtos;

/// <summary>
/// Response for GET /{slug} endpoint.
/// </summary>
public sealed record GetShortLinkResponse
{
    /// <summary>
    /// Indicates if the link requires password authentication.
    /// </summary>
    public bool RequiresPassword { get; init; }

    /// <summary>
    /// The original URL to redirect to (only set when RequiresPassword is false).
    /// </summary>
    public string? OriginalUrl { get; init; }
}
