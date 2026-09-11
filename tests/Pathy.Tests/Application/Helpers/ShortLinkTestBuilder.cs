using System.Runtime.Serialization;
using Pathy.Domain.Entities;
using Pathy.Domain.Enums;

namespace Pathy.Tests.Application.Helpers;

/// <summary>
/// Builds ShortLink instances for unit tests, including states not allowed by the domain factory.
/// </summary>
internal static class ShortLinkTestBuilder
{
    public static ShortLink CreateActive(
        string slug,
        string originalUrl,
        Guid? userId = null,
        DateTimeOffset? expiresAt = null,
        string? passwordHash = null)
    {
        return ShortLink.Create(
            originalUrl: originalUrl,
            userId: userId,
            customSlug: slug,
            expiresAt: expiresAt,
            passwordHash: passwordHash);
    }

    public static ShortLink CreateInactive(string slug, string originalUrl)
    {
        var link = CreateActive(slug, originalUrl);
        SetProperty(link, "Status", LinkStatus.Inactive);
        return link;
    }

    public static ShortLink CreateExpired(string slug, string originalUrl, DateTimeOffset expiresAt)
    {
        var link = (ShortLink)FormatterServices.GetUninitializedObject(typeof(ShortLink));

        SetProperty(link, "Id", Guid.NewGuid());
        SetProperty(link, "OriginalUrl", originalUrl);
        SetProperty(link, "Slug", slug);
        SetProperty(link, "CreatedAt", DateTimeOffset.UtcNow.AddDays(-2));
        SetProperty(link, "ExpiresAt", expiresAt);
        SetProperty(link, "Status", LinkStatus.Active);
        SetProperty<Guid?>(link, "UserId", null);
        SetProperty<string?>(link, "PasswordHash", null);

        return link;
    }

    private static void SetProperty<T>(ShortLink link, string propertyName, T value)
    {
        typeof(ShortLink).GetProperty(propertyName)!.SetValue(link, value);
    }
}
