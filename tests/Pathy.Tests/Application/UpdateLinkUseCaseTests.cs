using Pathy.Application.ShortLinks.Dtos;
using Pathy.Application.ShortLinks.UseCases;
using Pathy.Domain.Entities;
using Pathy.Domain.Exceptions;
using Pathy.Tests.Application.Helpers;

namespace Pathy.Tests.Application;

public class UpdateLinkUseCaseTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    [Fact]
    public async Task ExecuteAsync_AsUnauthenticated_ThrowsUnauthorizedException()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var useCase = new UpdateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create());

        var request = new UpdateLinkRequest { OriginalUrl = "https://new-example.com" };

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => useCase.ExecuteAsync(Guid.NewGuid(), request));
    }

    [Fact]
    public async Task ExecuteAsync_WithNoFieldsToUpdate_ThrowsDomainException()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var useCase = new UpdateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));

        var request = new UpdateLinkRequest(); // No fields provided

        await Assert.ThrowsAsync<DomainException>(
            () => useCase.ExecuteAsync(Guid.NewGuid(), request));
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentLink_ThrowsNotFoundException()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var useCase = new UpdateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));

        var request = new UpdateLinkRequest { OriginalUrl = "https://new-example.com" };
        var nonExistentId = Guid.NewGuid();

        await Assert.ThrowsAsync<NotFoundException>(
            () => useCase.ExecuteAsync(nonExistentId, request));
    }

    [Fact]
    public async Task ExecuteAsync_WithOtherUsersLink_ThrowsNotFoundException()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var otherUserLink = ShortLink.Create("https://example.com/page", _otherUserId, "link001");
        repository.Seed(otherUserLink);

        var useCase = new UpdateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));
        var request = new UpdateLinkRequest { OriginalUrl = "https://new-example.com" };

        // Should return 404 even though link exists, to prevent information leakage
        await Assert.ThrowsAsync<NotFoundException>(
            () => useCase.ExecuteAsync(otherUserLink.Id, request));
    }

    [Fact]
    public async Task ExecuteAsync_UpdateOriginalUrl_UpdatesAndCreatesAuditLog()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var link = ShortLink.Create("https://old-url.com", _userId, "link001");
        repository.Seed(link);

        var useCase = new UpdateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));
        var request = new UpdateLinkRequest { OriginalUrl = "https://new-url.com" };

        var response = await useCase.ExecuteAsync(link.Id, request);

        Assert.Equal("https://new-url.com", response.OriginalUrl);
        Assert.Single(auditLogRepository.Logs);
        
        var auditLog = auditLogRepository.Logs[0];
        Assert.Equal(link.Id, auditLog.LinkId);
        Assert.Equal(_userId, auditLog.UserId);
        Assert.Equal("OriginalUrl", auditLog.FieldName);
        Assert.Equal("https://old-url.com", auditLog.OldValue);
        Assert.Equal("https://new-url.com", auditLog.NewValue);
    }

    [Fact]
    public async Task ExecuteAsync_UpdateExpiresAt_UpdatesAndCreatesAuditLog()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var oldExpiration = DateTimeOffset.UtcNow.AddDays(7);
        var link = ShortLink.Create("https://example.com", _userId, "link001", oldExpiration);
        repository.Seed(link);

        var useCase = new UpdateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));
        var newExpiration = DateTimeOffset.UtcNow.AddDays(30);
        var request = new UpdateLinkRequest { ExpiresAt = newExpiration };

        var response = await useCase.ExecuteAsync(link.Id, request);

        Assert.NotNull(response.ExpiresAt);
        Assert.Equal(newExpiration.ToString("O"), response.ExpiresAt.Value.ToString("O"));
        Assert.Single(auditLogRepository.Logs);
        
        var auditLog = auditLogRepository.Logs[0];
        Assert.Equal("ExpiresAt", auditLog.FieldName);
        Assert.Equal(oldExpiration.ToString("O"), auditLog.OldValue);
        Assert.Equal(newExpiration.ToString("O"), auditLog.NewValue);
    }

    [Fact]
    public async Task ExecuteAsync_RemoveExpiration_UpdatesAndCreatesAuditLog()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var oldExpiration = DateTimeOffset.UtcNow.AddDays(7);
        var link = ShortLink.Create("https://example.com", _userId, "link001", oldExpiration);
        repository.Seed(link);

        var useCase = new UpdateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));
        var request = new UpdateLinkRequest { RemoveExpiration = true };

        var response = await useCase.ExecuteAsync(link.Id, request);

        Assert.Null(response.ExpiresAt);
        Assert.Single(auditLogRepository.Logs);
        
        var auditLog = auditLogRepository.Logs[0];
        Assert.Equal("ExpiresAt", auditLog.FieldName);
        Assert.Equal(oldExpiration.ToString("O"), auditLog.OldValue);
        Assert.Null(auditLog.NewValue);
    }

    [Fact]
    public async Task ExecuteAsync_UpdateBothFields_CreatesTwoAuditLogs()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var oldExpiration = DateTimeOffset.UtcNow.AddDays(7);
        var link = ShortLink.Create("https://old-url.com", _userId, "link001", oldExpiration);
        repository.Seed(link);

        var useCase = new UpdateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));
        var newExpiration = DateTimeOffset.UtcNow.AddDays(30);
        var request = new UpdateLinkRequest 
        { 
            OriginalUrl = "https://new-url.com",
            ExpiresAt = newExpiration
        };

        var response = await useCase.ExecuteAsync(link.Id, request);

        Assert.Equal("https://new-url.com", response.OriginalUrl);
        Assert.NotNull(response.ExpiresAt);
        Assert.Equal(2, auditLogRepository.Logs.Count);
        
        Assert.Contains(auditLogRepository.Logs, log => log.FieldName == "OriginalUrl");
        Assert.Contains(auditLogRepository.Logs, log => log.FieldName == "ExpiresAt");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidUrl_ThrowsDomainException()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var link = ShortLink.Create("https://example.com", _userId, "link001");
        repository.Seed(link);

        var useCase = new UpdateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));
        var request = new UpdateLinkRequest { OriginalUrl = "not-a-valid-url" };

        await Assert.ThrowsAsync<DomainException>(
            () => useCase.ExecuteAsync(link.Id, request));
        
        // No audit log should be created for failed updates
        Assert.Empty(auditLogRepository.Logs);
    }

    [Fact]
    public async Task ExecuteAsync_WithPastExpirationDate_ThrowsDomainException()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var link = ShortLink.Create("https://example.com", _userId, "link001");
        repository.Seed(link);

        var useCase = new UpdateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));
        var request = new UpdateLinkRequest 
        { 
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) // Past date
        };

        await Assert.ThrowsAsync<DomainException>(
            () => useCase.ExecuteAsync(link.Id, request));
        
        // No audit log should be created for failed updates
        Assert.Empty(auditLogRepository.Logs);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotModifySlug()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var link = ShortLink.Create("https://example.com", _userId, "link001");
        repository.Seed(link);

        var useCase = new UpdateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));
        var request = new UpdateLinkRequest { OriginalUrl = "https://new-url.com" };

        var response = await useCase.ExecuteAsync(link.Id, request);

        // Slug should remain unchanged
        Assert.Equal("link001", response.Slug);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotModifyCreatedAt()
    {
        var repository = new FakeShortLinkRepository();
        var auditLogRepository = new FakeLinkAuditLogRepository();
        var link = ShortLink.Create("https://example.com", _userId, "link001");
        var originalCreatedAt = link.CreatedAt;
        repository.Seed(link);

        var useCase = new UpdateLinkUseCase(repository, auditLogRepository, HttpContextTestHelper.Create(userId: _userId));
        var request = new UpdateLinkRequest { OriginalUrl = "https://new-url.com" };

        await Task.Delay(100); // Ensure some time passes

        var response = await useCase.ExecuteAsync(link.Id, request);

        // CreatedAt should remain unchanged
        Assert.Equal(originalCreatedAt, response.CreatedAt);
    }
}
