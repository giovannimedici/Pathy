namespace Pathy.Application.ShortLinks.Dtos;

/// <summary>
/// Generic paginated response wrapper.
/// </summary>
/// <typeparam name="T">Type of items in the response.</typeparam>
public sealed record PagedResponse<T>
{
    /// <summary>
    /// List of items for the current page.
    /// </summary>
    public required List<T> Items { get; init; }

    /// <summary>
    /// Pagination metadata.
    /// </summary>
    public required PaginationMetadata Pagination { get; init; }
}

/// <summary>
/// Metadata about pagination state.
/// </summary>
public sealed record PaginationMetadata
{
    /// <summary>
    /// Current page number (1-indexed).
    /// </summary>
    public required int CurrentPage { get; init; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public required int TotalItems { get; init; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public required int TotalPages { get; init; }
}
