using Pathy.Domain.Enums;
using Pathy.Domain.Exceptions;
using Pathy.Domain.Services;
using Pathy.Domain.Validation;

namespace Pathy.Domain.Entities;

public sealed class ShortLink
{
    public Guid Id { get; private set; }

    public string OriginalUrl { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public LinkStatus Status { get; private set; }

    /// <summary>
    /// User ID that created the link. Null for anonymous users.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Password hash for password-protected links. Null if not protected.
    /// </summary>
    public string? PasswordHash { get; private set; }

    private ShortLink()
    {
    }

    public static ShortLink Create(
        string originalUrl,
        Guid? userId = null,
        string? customSlug = null,
        DateTimeOffset? expiresAt = null,
        string? passwordHash = null)
    {
        UrlRules.Validate(originalUrl);

        if (expiresAt.HasValue && expiresAt.Value <= DateTimeOffset.UtcNow)
        {
            throw new DomainException("Expiration date must be in the future.");
        }

        var slug = string.IsNullOrWhiteSpace(customSlug)
            ? Base62SlugGenerator.Generate()
            : customSlug;

        SlugRules.Validate(slug);

        return new ShortLink
        {
            Id = Guid.NewGuid(),
            OriginalUrl = originalUrl,
            Slug = slug,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            Status = LinkStatus.Active,
            UserId = userId,
            PasswordHash = passwordHash
        };
    }

    public bool IsPasswordProtected => !string.IsNullOrEmpty(PasswordHash);
}
