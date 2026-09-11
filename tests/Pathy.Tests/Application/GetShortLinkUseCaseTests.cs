using Pathy.Application.ShortLinks.UseCases;
using Pathy.Domain.Exceptions;
using Pathy.Tests.Application.Helpers;

namespace Pathy.Tests.Application;

public class GetShortLinkUseCaseTests
{
    private readonly FakeShortLinkRepository _repository = new();
    private readonly GetShortLinkUseCase _useCase;

    public GetShortLinkUseCaseTests()
    {
        _useCase = new GetShortLinkUseCase(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSlugNotFound_ThrowsNotFoundException()
    {
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _useCase.ExecuteAsync("missing1"));

        Assert.Equal("Short link not found.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLinkInactive_ThrowsNotFoundException()
    {
        _repository.Seed(ShortLinkTestBuilder.CreateInactive("deact12", "https://example.com/inactive"));

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _useCase.ExecuteAsync("deact12"));

        Assert.Equal("Short link not found.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLinkExpired_ThrowsGoneException()
    {
        _repository.Seed(ShortLinkTestBuilder.CreateExpired(
            "exp1234",
            "https://example.com/expired",
            DateTimeOffset.UtcNow.AddDays(-1)));

        var exception = await Assert.ThrowsAsync<GoneException>(
            () => _useCase.ExecuteAsync("exp1234"));

        Assert.Equal("This link has expired and is no longer available.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPasswordProtected_ReturnsRequiresPasswordTrue()
    {
        _repository.Seed(ShortLinkTestBuilder.CreateActive(
            "pass123",
            "https://example.com/protected",
            passwordHash: "hashed_password"));

        var result = await _useCase.ExecuteAsync("pass123");

        Assert.True(result.RequiresPassword);
        Assert.Null(result.OriginalUrl);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidLink_ReturnsOriginalUrl()
    {
        const string originalUrl = "https://example.com/valid";
        _repository.Seed(ShortLinkTestBuilder.CreateActive("valid12", originalUrl));

        var result = await _useCase.ExecuteAsync("valid12");

        Assert.False(result.RequiresPassword);
        Assert.Equal(originalUrl, result.OriginalUrl);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidLinkWithFutureExpiration_ReturnsOriginalUrl()
    {
        const string originalUrl = "https://example.com/future";
        _repository.Seed(ShortLinkTestBuilder.CreateActive(
            "future1",
            originalUrl,
            expiresAt: DateTimeOffset.UtcNow.AddDays(7)));

        var result = await _useCase.ExecuteAsync("future1");

        Assert.False(result.RequiresPassword);
        Assert.Equal(originalUrl, result.OriginalUrl);
    }
}
