using Pathy.Application.ShortLinks.Dtos;
using Pathy.Application.ShortLinks.UseCases;
using Pathy.Domain.Exceptions;
using Pathy.Tests.Application.Helpers;

namespace Pathy.Tests.Application;

public class CreateShortLinkUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_AsAnonymous_CreatesNewLink()
    {
        var repository = new FakeShortLinkRepository();
        var useCase = new CreateShortLinkUseCase(repository, HttpContextTestHelper.Create());

        var request = new CreateShortLinkRequest { Url = "https://example.com/page" };

        var (response, isNew) = await useCase.ExecuteAsync(request);

        Assert.True(isNew);
        Assert.Equal("https://example.com/page", response.OriginalUrl);
        Assert.StartsWith("https://pathy.test/", response.ShortUrl);
        Assert.Contains(response.Slug, response.ShortUrl);
        Assert.False(response.IsPasswordProtected);
        Assert.Single(repository.Links);
    }

    [Theory]
    [InlineData("custom1", null, null)]
    [InlineData(null, "2026-12-31T00:00:00Z", null)]
    [InlineData(null, null, "secret")]
    public async Task ExecuteAsync_AsAnonymousWithOptionalFeatures_ThrowsUnauthorizedException(
        string? customSlug,
        string? expiresAt,
        string? password)
    {
        var useCase = new CreateShortLinkUseCase(
            new FakeShortLinkRepository(),
            HttpContextTestHelper.Create());

        var request = new CreateShortLinkRequest
        {
            Url = "https://example.com/page",
            CustomSlug = customSlug,
            ExpiresAt = expiresAt is null ? null : DateTimeOffset.Parse(expiresAt),
            Password = password
        };

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => useCase.ExecuteAsync(request));

        Assert.Equal(
            "Custom slug, expiration date, and password protection require authentication.",
            exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_AsAuthenticatedWithExistingUrl_ReturnsExistingLink()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeShortLinkRepository();
        var existingLink = ShortLinkTestBuilder.CreateActive(
            "exist12",
            "https://example.com/existing",
            userId: userId);
        repository.Seed(existingLink);

        var useCase = new CreateShortLinkUseCase(
            repository,
            HttpContextTestHelper.Create(userId: userId));

        var request = new CreateShortLinkRequest { Url = "https://example.com/existing" };

        var (response, isNew) = await useCase.ExecuteAsync(request);

        Assert.False(isNew);
        Assert.Equal("exist12", response.Slug);
        Assert.Equal("https://example.com/existing", response.OriginalUrl);
        Assert.Single(repository.Links);
    }

    [Fact]
    public async Task ExecuteAsync_AsAuthenticatedWithDuplicateCustomSlug_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeShortLinkRepository();
        repository.Seed(ShortLinkTestBuilder.CreateActive("taken12", "https://example.com/other", userId: userId));

        var useCase = new CreateShortLinkUseCase(
            repository,
            HttpContextTestHelper.Create(userId: userId));

        var request = new CreateShortLinkRequest
        {
            Url = "https://example.com/new",
            CustomSlug = "taken12"
        };

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => useCase.ExecuteAsync(request));

        Assert.Equal(
            "The slug 'taken12' is already in use. Please choose a different one.",
            exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_AsAuthenticatedWithCustomSlug_CreatesNewLink()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeShortLinkRepository();
        var useCase = new CreateShortLinkUseCase(
            repository,
            HttpContextTestHelper.Create(userId: userId));

        var request = new CreateShortLinkRequest
        {
            Url = "https://example.com/custom",
            CustomSlug = "mylink1"
        };

        var (response, isNew) = await useCase.ExecuteAsync(request);

        Assert.True(isNew);
        Assert.Equal("mylink1", response.Slug);
        Assert.Equal("https://pathy.test/mylink1", response.ShortUrl);
        Assert.Single(repository.Links);
    }

    [Fact]
    public async Task ExecuteAsync_AsAuthenticatedWithPassword_SetsPasswordProtected()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeShortLinkRepository();
        var useCase = new CreateShortLinkUseCase(
            repository,
            HttpContextTestHelper.Create(userId: userId));

        var request = new CreateShortLinkRequest
        {
            Url = "https://example.com/secure",
            Password = "secret123"
        };

        var (response, isNew) = await useCase.ExecuteAsync(request);

        Assert.True(isNew);
        Assert.True(response.IsPasswordProtected);
        Assert.NotNull(repository.Links[0].PasswordHash);
    }

    [Fact]
    public async Task ExecuteAsync_AsAuthenticatedWithExpiration_SetsExpiresAt()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeShortLinkRepository();
        var useCase = new CreateShortLinkUseCase(
            repository,
            HttpContextTestHelper.Create(userId: userId));

        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);
        var request = new CreateShortLinkRequest
        {
            Url = "https://example.com/temporary",
            ExpiresAt = expiresAt
        };

        var (response, isNew) = await useCase.ExecuteAsync(request);

        Assert.True(isNew);
        Assert.Equal(expiresAt, response.ExpiresAt);
    }

    [Theory]
    [InlineData("links")]
    [InlineData("auth")]
    [InlineData("swagger")]
    [InlineData("LINKS")] // case-insensitive
    public async Task ExecuteAsync_WithReservedCustomSlug_ThrowsDomainException(string reservedSlug)
    {
        var userId = Guid.NewGuid();
        var repository = new FakeShortLinkRepository();
        var useCase = new CreateShortLinkUseCase(
            repository,
            HttpContextTestHelper.Create(userId: userId));

        var request = new CreateShortLinkRequest
        {
            Url = "https://example.com/page",
            CustomSlug = reservedSlug
        };

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => useCase.ExecuteAsync(request));

        Assert.Contains("reserved", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
