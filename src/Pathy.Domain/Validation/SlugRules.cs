using System.Text.RegularExpressions;
using Pathy.Domain.Exceptions;

namespace Pathy.Domain.Validation;

public static partial class SlugRules
{
    public const int MinLength = 6;
    public const int MaxLength = 8;

    /// <summary>
    /// Reserved slugs that cannot be used because they collide with API routes.
    /// Case-insensitive comparison.
    /// </summary>
    private static readonly HashSet<string> ReservedSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "links",
        "auth",
        "swagger",
        "api",
        "health",
        "metrics"
    };

    public static void Validate(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainException("Slug cannot be empty.");
        }

        // Check reserved slugs first, before length validation
        // (reserved routes may be shorter than MinLength)
        if (ReservedSlugs.Contains(slug))
        {
            throw new DomainException($"The slug '{slug}' is reserved and cannot be used.");
        }

        if (slug.Length < MinLength || slug.Length > MaxLength)
        {
            throw new DomainException($"Slug must be between {MinLength} and {MaxLength} characters.");
        }

        if (!Base62Pattern().IsMatch(slug))
        {
            throw new DomainException("Slug must contain only base62 characters (0-9, A-Z, a-z).");
        }
    }

    [GeneratedRegex("^[0-9A-Za-z]+$")]
    private static partial Regex Base62Pattern();
}
