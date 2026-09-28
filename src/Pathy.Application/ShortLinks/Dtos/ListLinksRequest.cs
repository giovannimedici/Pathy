namespace Pathy.Application.ShortLinks.Dtos;

/// <summary>
/// Request DTO for listing and filtering user's short links.
/// </summary>
public sealed record ListLinksRequest
{
    /// <summary>
    /// Page number (1-indexed). Default: 1.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of items per page. Default: 20, Max: 100.
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Filter by link status (active, inactive). Optional.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Filter by creation date from (inclusive). Optional.
    /// </summary>
    public DateTimeOffset? CreatedFrom { get; init; }

    /// <summary>
    /// Filter by creation date to (inclusive). Optional.
    /// </summary>
    public DateTimeOffset? CreatedTo { get; init; }
}
