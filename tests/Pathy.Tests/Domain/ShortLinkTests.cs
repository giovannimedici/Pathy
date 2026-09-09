using Pathy.Domain.Entities;
using Pathy.Domain.Enums;
using Pathy.Domain.Exceptions;
using Pathy.Domain.Services;
using Pathy.Domain.Validation;

namespace Pathy.Tests.Domain;

public class Base62SlugGeneratorTests
{
    [Fact]
    public void Generate_ReturnsSlugWithinLengthBounds()
    {
        for (var i = 0; i < 100; i++)
        {
            var slug = Base62SlugGenerator.Generate();

            Assert.InRange(slug.Length, SlugRules.MinLength, SlugRules.MaxLength);
        }
    }

    [Fact]
    public void Generate_ReturnsOnlyBase62Characters()
    {
        const string base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        for (var i = 0; i < 100; i++)
        {
            var slug = Base62SlugGenerator.Generate();

            Assert.All(slug, character => Assert.Contains(character, base62Chars));
        }
    }

    [Fact]
    public void Generate_ProducesDifferentSlugs()
    {
        var slugs = Enumerable.Range(0, 50)
            .Select(_ => Base62SlugGenerator.Generate())
            .ToHashSet();

        Assert.True(slugs.Count > 1);
    }
}

public class SlugRulesTests
{
    [Theory]
    [InlineData("abc123")]
    [InlineData("ABCDEF")]
    [InlineData("aZ0bY1cX")]
    public void Validate_AcceptsValidSlugs(string slug)
    {
        var exception = Record.Exception(() => SlugRules.Validate(slug));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("abc12")]
    [InlineData("abcdefghi")]
    public void Validate_RejectsInvalidLength(string slug)
    {
        Assert.Throws<DomainException>(() => SlugRules.Validate(slug));
    }

    [Theory]
    [InlineData("abc-123")]
    [InlineData("slug!")]
    public void Validate_RejectsNonBase62Characters(string slug)
    {
        Assert.Throws<DomainException>(() => SlugRules.Validate(slug));
    }
}

public class UrlRulesTests
{
    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com/path")]
    [InlineData("https://sub.domain.example.org/resource?query=1")]
    public void Validate_AcceptsPublicHttpAndHttpsUrls(string url)
    {
        var exception = Record.Exception(() => UrlRules.Validate(url));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///etc/passwd")]
    public void Validate_RejectsNonHttpSchemes(string url)
    {
        Assert.Throws<DomainException>(() => UrlRules.Validate(url));
    }

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("https://localhost/path")]
    [InlineData("http://app.localhost")]
    public void Validate_RejectsLocalhost(string url)
    {
        Assert.Throws<DomainException>(() => UrlRules.Validate(url));
    }

    [Theory]
    [InlineData("http://127.0.0.1")]
    [InlineData("http://10.0.0.1")]
    [InlineData("http://192.168.1.1")]
    [InlineData("http://172.16.0.1")]
    [InlineData("http://[::1]")]
    public void Validate_RejectsPrivateAndLoopbackIps(string url)
    {
        Assert.Throws<DomainException>(() => UrlRules.Validate(url));
    }
}

public class ShortLinkTests
{
    [Fact]
    public void Create_ReturnsActiveLinkWithGeneratedSlug()
    {
        var link = ShortLink.Create("https://example.com/page");

        Assert.NotEqual(Guid.Empty, link.Id);
        Assert.Equal("https://example.com/page", link.OriginalUrl);
        Assert.InRange(link.Slug.Length, SlugRules.MinLength, SlugRules.MaxLength);
        Assert.Equal(LinkStatus.Active, link.Status);
        Assert.Null(link.ExpiresAt);
        Assert.True(link.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Create_AcceptsOptionalExpirationDate()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        var link = ShortLink.Create("https://example.com/page", expiresAt);

        Assert.Equal(expiresAt, link.ExpiresAt);
    }

    [Fact]
    public void Create_RejectsInvalidUrl()
    {
        Assert.Throws<DomainException>(() => ShortLink.Create("http://localhost"));
    }

    [Fact]
    public void Create_RejectsPastExpirationDate()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        Assert.Throws<DomainException>(() => ShortLink.Create("https://example.com", expiresAt));
    }
}
