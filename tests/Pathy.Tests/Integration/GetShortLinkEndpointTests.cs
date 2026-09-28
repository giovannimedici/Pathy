using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Pathy.Application.ShortLinks.Dtos;
using Pathy.Domain.Entities;
using Pathy.Domain.Enums;
using Pathy.Infrastructure.Data;

namespace Pathy.Tests.Integration;

/// <summary>
/// Integration tests for GET /{slug} endpoint.
/// </summary>
public class GetShortLinkEndpointTests : IClassFixture<PathyWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly PathyWebApplicationFactory _factory;

    public GetShortLinkEndpointTests(PathyWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false // Don't follow redirects automatically
        });
    }

    [Fact]
    public async Task GetShortLink_ValidLink_ReturnsRedirect()
    {
        // Arrange
        await ClearDatabase();
        var slug = "abc123"; // 6 characters (valid)
        var originalUrl = "https://example.com/valid-page";
        
        await CreateShortLinkInDatabase(slug, originalUrl);

        // Act
        var response = await _client.GetAsync($"/{slug}");

        // Assert
        Assert.Equal(HttpStatusCode.Found, response.StatusCode); // 302
        Assert.NotNull(response.Headers.Location);
        Assert.Equal(originalUrl, response.Headers.Location.ToString());
    }

    [Fact]
    public async Task GetShortLink_NonExistentSlug_ReturnsNotFound()
    {
        // Arrange
        await ClearDatabase();

        // Act
        var response = await _client.GetAsync("/abc456");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("not found", problemDetails, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetShortLink_ExpiredLink_ReturnsGone()
    {
        // Arrange
        await ClearDatabase();
        var slug = "exp1234"; // 7 characters (valid)
        var originalUrl = "https://example.com/expired-page";
        var expiresAt = DateTimeOffset.UtcNow.AddDays(-1); // Expired yesterday
        
        // Create link with expired date (bypass domain validation by direct DB insert)
        await CreateExpiredShortLinkInDatabase(slug, originalUrl, expiresAt);

        // Act
        var response = await _client.GetAsync($"/{slug}");

        // Assert
        Assert.Equal(HttpStatusCode.Gone, response.StatusCode); // 410
        
        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("expired", problemDetails, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetShortLink_DeactivatedLink_ReturnsNotFound()
    {
        // Arrange
        await ClearDatabase();
        var slug = "deact12"; // 7 characters (valid)
        var originalUrl = "https://example.com/deactivated-page";
        
        await CreateShortLinkInDatabase(slug, originalUrl, status: LinkStatus.Inactive);

        // Act
        var response = await _client.GetAsync($"/{slug}");

        // Assert
        // Should return 404 with the same message as non-existent slug
        // (to avoid leaking information that the slug existed)
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("not found", problemDetails, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetShortLink_PasswordProtectedLink_ReturnsUnauthorized()
    {
        // Arrange
        await ClearDatabase();
        var slug = "pass123"; // 7 characters (valid)
        var originalUrl = "https://example.com/protected-page";
        var passwordHash = "hashed_password_123"; // In real scenario, this would be a proper hash
        
        await CreateShortLinkInDatabase(slug, originalUrl, passwordHash: passwordHash);

        // Act
        var response = await _client.GetAsync($"/{slug}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); // 401
        
        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("password", problemDetails, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetShortLink_ValidLinkWithFutureExpiration_ReturnsRedirect()
    {
        // Arrange
        await ClearDatabase();
        var slug = "future1"; // 7 characters (valid)
        var originalUrl = "https://example.com/future-page";
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7); // Expires in 7 days
        
        await CreateShortLinkInDatabase(slug, originalUrl, expiresAt: expiresAt);

        // Act
        var response = await _client.GetAsync($"/{slug}");

        // Assert
        Assert.Equal(HttpStatusCode.Found, response.StatusCode); // 302
        Assert.NotNull(response.Headers.Location);
        Assert.Equal(originalUrl, response.Headers.Location.ToString());
    }

    private async Task CreateShortLinkInDatabase(
        string slug,
        string originalUrl,
        DateTimeOffset? expiresAt = null,
        LinkStatus status = LinkStatus.Active,
        string? passwordHash = null)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PathyDbContext>();

        // Only validate if expiration is in the future (domain rule)
        // For expired dates, we'll use the direct DB insert method
        if (expiresAt.HasValue && expiresAt.Value <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Use CreateExpiredShortLinkInDatabase for expired links");
        }

        var shortLink = ShortLink.Create(
            originalUrl: originalUrl,
            userId: null,
            customSlug: slug,
            expiresAt: expiresAt,
            passwordHash: passwordHash);

        // If we need to set the status to Inactive, we need to use reflection
        // since the Status property has a private setter
        if (status == LinkStatus.Inactive)
        {
            var statusProperty = typeof(ShortLink).GetProperty("Status");
            statusProperty?.SetValue(shortLink, status);
        }

        context.ShortLinks.Add(shortLink);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a short link with an expired date directly in the database.
    /// Bypasses domain validation since the domain doesn't allow creating expired links.
    /// </summary>
    private async Task CreateExpiredShortLinkInDatabase(
        string slug,
        string originalUrl,
        DateTimeOffset expiresAt)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PathyDbContext>();

        // Create the entity directly without using the factory method
        // to bypass the "expiration must be in the future" validation
        var shortLink = (ShortLink)RuntimeHelpers.GetUninitializedObject(typeof(ShortLink));

        // Use reflection to set all properties
        typeof(ShortLink).GetProperty("Id")!.SetValue(shortLink, Guid.NewGuid());
        typeof(ShortLink).GetProperty("OriginalUrl")!.SetValue(shortLink, originalUrl);
        typeof(ShortLink).GetProperty("Slug")!.SetValue(shortLink, slug);
        typeof(ShortLink).GetProperty("CreatedAt")!.SetValue(shortLink, DateTimeOffset.UtcNow.AddDays(-2));
        typeof(ShortLink).GetProperty("ExpiresAt")!.SetValue(shortLink, expiresAt);
        typeof(ShortLink).GetProperty("Status")!.SetValue(shortLink, LinkStatus.Active);
        typeof(ShortLink).GetProperty("UserId")!.SetValue(shortLink, null);
        typeof(ShortLink).GetProperty("PasswordHash")!.SetValue(shortLink, null);

        context.ShortLinks.Add(shortLink);
        await context.SaveChangesAsync();
    }

    private async Task ClearDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PathyDbContext>();
        context.ShortLinks.RemoveRange(context.ShortLinks);
        await context.SaveChangesAsync();
    }
}
