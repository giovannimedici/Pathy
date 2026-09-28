using Pathy.Application.ShortLinks.UseCases;
using Pathy.Domain.Entities;
using Pathy.Domain.Exceptions;
using Pathy.Tests.Application.Helpers;

namespace Pathy.Tests.Application;

public class HardDeleteLinkUseCaseTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    [Fact]
    public async Task ExecuteAsync_AsUnauthenticated_ThrowsUnauthorizedException()
    {
        var repository = new FakeShortLinkRepository();
        var useCase = new HardDeleteLinkUseCase(repository, HttpContextTestHelper.Create());

        var linkId = Guid.NewGuid();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => useCase.ExecuteAsync(linkId));
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentLink_ThrowsNotFoundException()
    {
        var repository = new FakeShortLinkRepository();
        var useCase = new HardDeleteLinkUseCase(repository, HttpContextTestHelper.Create(userId: _userId));

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

        var useCase = new HardDeleteLinkUseCase(repository, HttpContextTestHelper.Create(userId: _userId));

        // Should return 404 even though link exists, to prevent information leakage
        await Assert.ThrowsAsync<NotFoundException>(
            () => useCase.ExecuteAsync(otherUserLink.Id));
    }

    [Fact]
    public async Task ExecuteAsync_WithActiveLink_DeletesLink()
    {
        var repository = new FakeShortLinkRepository();
        var link = ShortLink.Create("https://example.com/page", _userId, "link001");
        repository.Seed(link);

        var useCase = new HardDeleteLinkUseCase(repository, HttpContextTestHelper.Create(userId: _userId));

        await useCase.ExecuteAsync(link.Id);

        Assert.Empty(repository.Links);
    }

    [Fact]
    public async Task ExecuteAsync_WithInactiveLink_DeletesLink()
    {
        var repository = new FakeShortLinkRepository();
        var link = ShortLink.Create("https://example.com/page", _userId, "link001");
        link.Deactivate(); // Make the link inactive
        repository.Seed(link);

        var useCase = new HardDeleteLinkUseCase(repository, HttpContextTestHelper.Create(userId: _userId));

        // Hard delete should work for both active and inactive links
        await useCase.ExecuteAsync(link.Id);

        Assert.Empty(repository.Links);
    }

    [Fact]
    public async Task ExecuteAsync_DeletesOnlyTargetLink_WhenMultipleLinksExist()
    {
        var repository = new FakeShortLinkRepository();
        var link1 = ShortLink.Create("https://example.com/page1", _userId, "link001");
        var link2 = ShortLink.Create("https://example.com/page2", _userId, "link002");
        var link3 = ShortLink.Create("https://example.com/page3", _userId, "link003");
        repository.Seed(link1);
        repository.Seed(link2);
        repository.Seed(link3);

        var useCase = new HardDeleteLinkUseCase(repository, HttpContextTestHelper.Create(userId: _userId));

        await useCase.ExecuteAsync(link2.Id);

        Assert.Equal(2, repository.Links.Count);
        Assert.Contains(repository.Links, l => l.Id == link1.Id);
        Assert.Contains(repository.Links, l => l.Id == link3.Id);
        Assert.DoesNotContain(repository.Links, l => l.Id == link2.Id);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotDeleteOtherUsersLinks_WhenTargetLinkNotFound()
    {
        var repository = new FakeShortLinkRepository();
        var otherUserLink = ShortLink.Create("https://example.com/page", _otherUserId, "link001");
        repository.Seed(otherUserLink);

        var useCase = new HardDeleteLinkUseCase(repository, HttpContextTestHelper.Create(userId: _userId));

        await Assert.ThrowsAsync<NotFoundException>(
            () => useCase.ExecuteAsync(otherUserLink.Id));

        // Other user's link should not be affected
        Assert.Single(repository.Links);
        Assert.Equal(otherUserLink.Id, repository.Links[0].Id);
    }
}
