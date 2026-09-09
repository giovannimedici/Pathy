using System.Text.RegularExpressions;
using Pathy.Domain.Exceptions;

namespace Pathy.Domain.Validation;

public static partial class SlugRules
{
    public const int MinLength = 6;
    public const int MaxLength = 8;

    public static void Validate(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainException("Slug cannot be empty.");
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
