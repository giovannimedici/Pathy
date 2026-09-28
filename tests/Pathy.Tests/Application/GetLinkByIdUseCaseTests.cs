using Pathy.Application.ShortLinks.UseCases;
using Pathy.Domain.Entities;
using Pathy.Domain.Exceptions;
using Pathy.Tests.Application.Helpers;

namespace Pathy.Tests.Application;

public class GetLinkByIdUseCaseTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    [Fact]
    public async Task ExecuteAsync_AsUnauthenticated_ThrowsUnauthorizedException()
    {
        var repository = new FakeShortLinkRepository();
        var useCase = new GetLinkByIdUseCase(repository, HttpContextTestHelper.Create());

        var linkId = Guid.NewGuid();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => useCase.ExecuteAsync(linkId));
    }

    [Fact]
    public async Task ExecuteAsync_WithValidLink_ReturnsDetails()
    {
        var repository = new FakeShortLinkRepository();
        var link = ShortLink.Create("https://example.com/page", _userId, "link001");
        repository.Seed(link);

        var useCase = new GetLinkByIdUseCase(repository, HttpContextTestHelper.Create(userId: _userId));

        var response = await useCase.ExecuteAsync(link.Id);

        Assert.Equal(link.Id, response.Id);
        Assert.Equal("link001", response.Slug);
        Assert.Equal("https://example.com/page", response.OriginalUrl);
        Assert.Equal("active", response.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentLink_ThrowsNotFoundException()
    {
        var repository = new FakeShortLinkRepository();
        var useCase = new GetLinkByIdUseCase(repository, HttpContextTestHelper.Create(userId: _userId));

        var nonExistentId = Guid.NewGuid();

        await Assert.ThrowsAsync<NotFoundException>(
            () => useCase.ExecuteAsync(nonExistentId));
    }

    [Fact]
    public async Task ExecuteAsync_WithOtherUsersLink_ThrowsNotFoundException()
    {
        var repository = new FakeShortLinkRepository();
        var otherUserLink = ShortLink.Create("https://example.com/page", _otherUserId, "link001");
        repository.Seed(otherUserLink);

        var useCase = new GetLinkByIdUseCase(repository, HttpContextTestHelper.Create(userId: _userId));

        // Should return 404 even though link exists, to prevent information leakage
        await Assert.ThrowsAsync<NotFoundException>(
            () => useCase.ExecuteAsync(otherUserLink.Id));
    }

    [Fact]
    public async Task ExecuteAsync_WithExpiredLink_StillReturnsDetails()
    {
        var repository = new FakeShortLinkRepository();
        // Create link with future expiration, then modify it to be expired using reflection
        var link = ShortLink.Create("https://example.com/page", _userId, "link001");
        var expiredDate = DateTimeOffset.UtcNow.AddDays(-1);
        typeof(ShortLink).GetProperty(nameof(ShortLink.ExpiresAt))!
            .SetValue(link, expiredDate);
        
        repository.Seed(link);

        var useCase = new GetLinkByIdUseCase(repository, HttpContextTestHelper.Create(userId: _userId));

        // Owner should be able to see details of expired links
        var response = await useCase.ExecuteAsync(link.Id);

        Assert.Equal(link.Id, response.Id);
        Assert.NotNull(response.ExpiresAt);
        Assert.True(response.ExpiresAt < DateTimeOffset.UtcNow);
    }
}
