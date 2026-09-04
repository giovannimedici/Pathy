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

    private ShortLink()
    {
    }

    public static ShortLink Create(string originalUrl, DateTimeOffset? expiresAt = null)
    {
        UrlRules.Validate(originalUrl);

        if (expiresAt.HasValue && expiresAt.Value <= DateTimeOffset.UtcNow)
        {
            throw new DomainException("Expiration date must be in the future.");
        }

        var slug = Base62SlugGenerator.Generate();
        SlugRules.Validate(slug);

        return new ShortLink
        {
            Id = Guid.NewGuid(),
            OriginalUrl = originalUrl,
            Slug = slug,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            Status = LinkStatus.Active
        };
    }
}
