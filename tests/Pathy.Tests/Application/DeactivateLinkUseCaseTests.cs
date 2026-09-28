using Pathy.Application.ShortLinks.UseCases;
using Pathy.Domain.Entities;
using Pathy.Domain.Exceptions;
using Pathy.Tests.Application.Helpers;

namespace Pathy.Tests.Application;

public class DeactivateLinkUseCaseTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    [Fact]
    public async Task ExecuteAsync_AsUnauthenticated_ThrowsUnauthorizedException()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var useCase = new DeactivateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create());

        var linkId = Guid.NewGuid();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => useCase.ExecuteAsync(linkId));
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentLink_ThrowsNotFoundException()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var useCase = new DeactivateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));

        var nonExistentId = Guid.NewGuid();

        await Assert.ThrowsAsync<NotFoundException>(
            () => useCase.ExecuteAsync(nonExistentId));
    }

    [Fact]
    public async Task ExecuteAsync_WithOtherUsersLink_ThrowsNotFoundException()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var otherUserLink = ShortLink.Create("https://example.com/page", _otherUserId, "link001");
        repository.Seed(otherUserLink);

        var useCase = new DeactivateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));

        // Should return 404 even though link exists, to prevent information leakage
        await Assert.ThrowsAsync<NotFoundException>(
            () => useCase.ExecuteAsync(otherUserLink.Id));
    }

    [Fact]
    public async Task ExecuteAsync_WithAlreadyInactiveLink_ThrowsConflictException()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var link = ShortLink.Create("https://example.com/page", _userId, "link001");
        link.Deactivate(); // Make the link inactive
        repository.Seed(link);

        var useCase = new DeactivateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));

        await Assert.ThrowsAsync<ConflictException>(
            () => useCase.ExecuteAsync(link.Id));
    }

    [Fact]
    public async Task ExecuteAsync_WithActiveLink_DeactivatesLink()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var link = ShortLink.Create("https://example.com/page", _userId, "link001");
        repository.Seed(link);

        var useCase = new DeactivateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));

        await useCase.ExecuteAsync(link.Id);

        Assert.True(link.IsInactive);
    }

    [Fact]
    public async Task ExecuteAsync_WithActiveLink_CreatesAuditLog()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var link = ShortLink.Create("https://example.com/page", _userId, "link001");
        repository.Seed(link);

        var useCase = new DeactivateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));

        await useCase.ExecuteAsync(link.Id);

        Assert.Single(auditLogRepository.Logs);
        
        var auditLog = auditLogRepository.Logs[0];
        Assert.Equal(link.Id, auditLog.LinkId);
        Assert.Equal(_userId, auditLog.UserId);
        Assert.Equal("Status", auditLog.FieldName);
        Assert.Equal("Active", auditLog.OldValue);
        Assert.Equal("Inactive", auditLog.NewValue);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCreateAuditLog_WhenLinkNotFound()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var useCase = new DeactivateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));

        var nonExistentId = Guid.NewGuid();

        await Assert.ThrowsAsync<NotFoundException>(
            () => useCase.ExecuteAsync(nonExistentId));

        Assert.Empty(auditLogRepository.Logs);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCreateAuditLog_WhenLinkAlreadyInactive()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var link = ShortLink.Create("https://example.com/page", _userId, "link001");
        link.Deactivate();
        repository.Seed(link);

        var useCase = new DeactivateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));

        await Assert.ThrowsAsync<ConflictException>(
            () => useCase.ExecuteAsync(link.Id));

        Assert.Empty(auditLogRepository.Logs);
    }
}
