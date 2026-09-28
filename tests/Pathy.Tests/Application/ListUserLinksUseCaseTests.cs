using Pathy.Application.ShortLinks.Dtos;
using Pathy.Application.ShortLinks.UseCases;
using Pathy.Domain.Entities;
using Pathy.Domain.Enums;
using Pathy.Domain.Exceptions;
using Pathy.Tests.Application.Helpers;

namespace Pathy.Tests.Application;

public class ListUserLinksUseCaseTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    [Fact]
    public async Task ExecuteAsync_AsUnauthenticated_ThrowsUnauthorizedException()
    {
        var repository = new FakeShortLinkRepository();
        var useCase = new ListUserLinksUseCase(repository, HttpContextTestHelper.Create());

        var request = new ListLinksRequest();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => useCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsOnlyUserLinks()
    {
        var repository = new FakeShortLinkRepository();
        
        // User's links
        repository.Seed(CreateLink(_userId, "https://example.com/1", "link001"));
        repository.Seed(CreateLink(_userId, "https://example.com/2", "link002"));
        
        // Other user's links (should not be returned)
        repository.Seed(CreateLink(_otherUserId, "https://example.com/3", "link003"));
        repository.Seed(CreateLink(_otherUserId, "https://example.com/4", "link004"));

        var useCase = new ListUserLinksUseCase(repository, HttpContextTestHelper.Create(userId: _userId));
        var request = new ListLinksRequest { Page = 1, PageSize = 20 };

        var response = await useCase.ExecuteAsync(request);

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(2, response.Pagination.TotalItems);
        Assert.All(response.Items, item => Assert.Contains(item.Slug, new[] { "link001", "link002" }));
    }

    [Fact]
    public async Task ExecuteAsync_WithStatusFilter_ReturnsFilteredLinks()
    {
        var repository = new FakeShortLinkRepository();
        
        var activeLink = CreateLink(_userId, "https://example.com/1", "link001");
        var inactiveLink = CreateLink(_userId, "https://example.com/2", "link002");
        // Simulate inactive status (normally done through soft delete)
        typeof(ShortLink).GetProperty(nameof(ShortLink.Status))!
            .SetValue(inactiveLink, LinkStatus.Inactive);

        repository.Seed(activeLink);
        repository.Seed(inactiveLink);

        var useCase = new ListUserLinksUseCase(repository, HttpContextTestHelper.Create(userId: _userId));
        var request = new ListLinksRequest { Status = "active" };

        var response = await useCase.ExecuteAsync(request);

        Assert.Single(response.Items);
        Assert.Equal("link001", response.Items[0].Slug);
        Assert.Equal("active", response.Items[0].Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithCreatedFromFilter_ReturnsFilteredLinks()
    {
        var repository = new FakeShortLinkRepository();
        
        var oldLink = CreateLinkWithDate(_userId, "https://example.com/1", "link001",
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var newLink = CreateLinkWithDate(_userId, "https://example.com/2", "link002",
            DateTimeOffset.Parse("2026-09-15T00:00:00Z"));

        repository.Seed(oldLink);
        repository.Seed(newLink);

        var useCase = new ListUserLinksUseCase(repository, HttpContextTestHelper.Create(userId: _userId));
        var request = new ListLinksRequest 
        { 
            CreatedFrom = DateTimeOffset.Parse("2026-09-01T00:00:00Z") 
        };

        var response = await useCase.ExecuteAsync(request);

        Assert.Single(response.Items);
        Assert.Equal("link002", response.Items[0].Slug);
    }

    [Fact]
    public async Task ExecuteAsync_WithCreatedToFilter_ReturnsFilteredLinks()
    {
        var repository = new FakeShortLinkRepository();
        
        var oldLink = CreateLinkWithDate(_userId, "https://example.com/1", "link001",
            DateTimeOffset.Parse("2026-01-15T00:00:00Z"));
        var newLink = CreateLinkWithDate(_userId, "https://example.com/2", "link002",
            DateTimeOffset.Parse("2026-09-15T00:00:00Z"));

        repository.Seed(oldLink);
        repository.Seed(newLink);

        var useCase = new ListUserLinksUseCase(repository, HttpContextTestHelper.Create(userId: _userId));
        var request = new ListLinksRequest 
        { 
            CreatedTo = DateTimeOffset.Parse("2026-08-31T23:59:59Z") 
        };

        var response = await useCase.ExecuteAsync(request);

        Assert.Single(response.Items);
        Assert.Equal("link001", response.Items[0].Slug);
    }

    [Fact]
    public async Task ExecuteAsync_WithCombinedFilters_ReturnsFilteredLinks()
    {
        var repository = new FakeShortLinkRepository();
        
        repository.Seed(CreateLinkWithDate(_userId, "https://example.com/1", "link001",
            DateTimeOffset.Parse("2026-01-15T00:00:00Z")));
        repository.Seed(CreateLinkWithDate(_userId, "https://example.com/2", "link002",
            DateTimeOffset.Parse("2026-05-15T00:00:00Z")));
        repository.Seed(CreateLinkWithDate(_userId, "https://example.com/3", "link003",
            DateTimeOffset.Parse("2026-09-15T00:00:00Z")));

        var useCase = new ListUserLinksUseCase(repository, HttpContextTestHelper.Create(userId: _userId));
        var request = new ListLinksRequest 
        { 
            Status = "active",
            CreatedFrom = DateTimeOffset.Parse("2026-02-01T00:00:00Z"),
            CreatedTo = DateTimeOffset.Parse("2026-08-31T23:59:59Z")
        };

        var response = await useCase.ExecuteAsync(request);

        Assert.Single(response.Items);
        Assert.Equal("link002", response.Items[0].Slug);
    }

    [Theory]
    [InlineData(0, 20, 1, 20)] // Invalid page (0) -> defaults to 1
    [InlineData(-1, 20, 1, 20)] // Invalid page (-1) -> defaults to 1
    [InlineData(1, 0, 1, 1)] // Invalid pageSize (0) -> defaults to 1
    [InlineData(1, 150, 1, 100)] // PageSize > 100 -> clamped to 100
    public async Task ExecuteAsync_WithInvalidPagination_UsesSafeDefaults(
        int requestPage,
        int requestPageSize,
        int expectedPage,
        int expectedPageSize)
    {
        var repository = new FakeShortLinkRepository();
        repository.Seed(CreateLink(_userId, "https://example.com/1", "link001"));

        var useCase = new ListUserLinksUseCase(repository, HttpContextTestHelper.Create(userId: _userId));
        var request = new ListLinksRequest { Page = requestPage, PageSize = requestPageSize };

        var response = await useCase.ExecuteAsync(request);

        Assert.Equal(expectedPage, response.Pagination.CurrentPage);
        Assert.Equal(expectedPageSize, response.Pagination.PageSize);
    }

    [Fact]
    public async Task ExecuteAsync_WithPagination_ReturnsCorrectPage()
    {
        var repository = new FakeShortLinkRepository();
        
        for (int i = 1; i <= 25; i++)
        {
            repository.Seed(CreateLink(_userId, $"https://example.com/{i}", $"link{i:D3}"));
        }

        var useCase = new ListUserLinksUseCase(repository, HttpContextTestHelper.Create(userId: _userId));
        var request = new ListLinksRequest { Page = 2, PageSize = 10 };

        var response = await useCase.ExecuteAsync(request);

        Assert.Equal(10, response.Items.Count);
        Assert.Equal(2, response.Pagination.CurrentPage);
        Assert.Equal(10, response.Pagination.PageSize);
        Assert.Equal(25, response.Pagination.TotalItems);
        Assert.Equal(3, response.Pagination.TotalPages);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidStatus_ThrowsDomainException()
    {
        var repository = new FakeShortLinkRepository();
        var useCase = new ListUserLinksUseCase(repository, HttpContextTestHelper.Create(userId: _userId));
        var request = new ListLinksRequest { Status = "invalid_status" };

        await Assert.ThrowsAsync<DomainException>(
            () => useCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidDateRange_ThrowsDomainException()
    {
        var repository = new FakeShortLinkRepository();
        var useCase = new ListUserLinksUseCase(repository, HttpContextTestHelper.Create(userId: _userId));
        var request = new ListLinksRequest 
        { 
            CreatedFrom = DateTimeOffset.Parse("2026-09-01T00:00:00Z"),
            CreatedTo = DateTimeOffset.Parse("2026-08-01T00:00:00Z")
        };

        await Assert.ThrowsAsync<DomainException>(
            () => useCase.ExecuteAsync(request));
    }

    private ShortLink CreateLink(Guid userId, string url, string slug)
    {
        return ShortLink.Create(url, userId, slug);
    }

    private ShortLink CreateLinkWithDate(Guid userId, string url, string slug, DateTimeOffset createdAt)
    {
        var link = ShortLink.Create(url, userId, slug);
        typeof(ShortLink).GetProperty(nameof(ShortLink.CreatedAt))!
            .SetValue(link, createdAt);
        return link;
    }
}
