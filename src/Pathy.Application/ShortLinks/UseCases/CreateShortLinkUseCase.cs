using Microsoft.AspNetCore.Http;
using Pathy.Application.ShortLinks.Dtos;
using Pathy.Application.ShortLinks.Interfaces;
using Pathy.Domain.Entities;
using Pathy.Domain.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace Pathy.Application.ShortLinks.UseCases;

/// <summary>
/// Use case for creating a new short link.
/// </summary>
public sealed class CreateShortLinkUseCase
{
    private readonly IShortLinkRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateShortLinkUseCase(
        IShortLinkRepository repository,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Executes the use case to create a short link.
    /// </summary>
    /// <param name="request">Request containing link details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>ShortLinkResponse with created or existing link data.</returns>
    public async Task<(ShortLinkResponse Response, bool IsNew)> ExecuteAsync(
        CreateShortLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var isAuthenticated = userId.HasValue;

        // Validate that optional fields are only used by authenticated users
        ValidateAuthenticatedFeatures(request, isAuthenticated);

        // Check idempotency: if authenticated user already created link for this URL, return existing
        if (isAuthenticated)
        {
            var existingLink = await _repository.FindByUserAndUrlAsync(
                userId,
                request.Url,
                cancellationToken);

            if (existingLink is not null)
            {
                return (MapToResponse(existingLink), IsNew: false);
            }
        }

        // Validate custom slug uniqueness if provided
        if (!string.IsNullOrWhiteSpace(request.CustomSlug))
        {
            var slugExists = await _repository.SlugExistsAsync(
                request.CustomSlug,
                cancellationToken);

            if (slugExists)
            {
                throw new ConflictException(
                    $"The slug '{request.CustomSlug}' is already in use. Please choose a different one.");
            }
        }

        // Hash password if provided
        string? passwordHash = null;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            passwordHash = HashPassword(request.Password);
        }

        // Create new short link
        var shortLink = ShortLink.Create(
            originalUrl: request.Url,
            userId: userId,
            customSlug: request.CustomSlug,
            expiresAt: request.ExpiresAt,
            passwordHash: passwordHash);

        await _repository.AddAsync(shortLink, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return (MapToResponse(shortLink), IsNew: true);
    }

    private Guid? GetCurrentUserId()
    {
        // TODO: Replace with actual authentication logic
        // For now, return null (anonymous user)
        // In a real implementation, extract from JWT claims or similar
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = httpContext.User.FindFirst("sub")?.Value
                ?? httpContext.User.FindFirst("userId")?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
        }

        return null;
    }

    private static void ValidateAuthenticatedFeatures(
        CreateShortLinkRequest request,
        bool isAuthenticated)
    {
        var hasOptionalFeatures =
            !string.IsNullOrWhiteSpace(request.CustomSlug) ||
            request.ExpiresAt.HasValue ||
            !string.IsNullOrWhiteSpace(request.Password);

        if (hasOptionalFeatures && !isAuthenticated)
        {
            throw new UnauthorizedException(
                "Custom slug, expiration date, and password protection require authentication.");
        }
    }

    private ShortLinkResponse MapToResponse(ShortLink shortLink)
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

        return new ShortLinkResponse
        {
            Slug = shortLink.Slug,
            ShortUrl = $"{baseUrl}/{shortLink.Slug}",
            OriginalUrl = shortLink.OriginalUrl,
            CreatedAt = shortLink.CreatedAt,
            ExpiresAt = shortLink.ExpiresAt,
            IsPasswordProtected = shortLink.IsPasswordProtected
        };
    }

    private static string HashPassword(string password)
    {
        // Use SHA256 for password hashing (in production, use bcrypt or PBKDF2)
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
