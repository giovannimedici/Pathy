# Pathy

URL shortener focused on reliability, analytics, and scale.

## Architecture

The backend follows Clean Architecture with four layers:

- **Domain** — entities, value objects, and pure business rules
- **Application** — use cases, interfaces, and DTOs
- **Infrastructure** — EF Core, Postgres, queues, and external integrations
- **Api** — endpoints, middleware, and composition root

## Slug generation: why Base62?

Short links are identified by a random slug generated in **Base62** — the set of
62 URL-safe characters: `0-9`, `A-Z`, and `a-z`.

| Property | Benefit |
|---|---|
| URL-safe | No encoding needed in paths (`/aB3xK9`) — works in SMS, email, and social posts |
| Case-sensitive alphabet | 62 symbols per character vs. 36 in Base36, yielding shorter slugs for the same entropy |
| Human-readable | Avoids ambiguous symbols (`+`, `/`, `=`) found in Base64 |

With slugs of **6–8 characters**, Base62 provides between ~56 billion and ~218 trillion
possible combinations — enough for low collision risk at early scale while keeping URLs
compact. Slugs are generated with `RandomNumberGenerator` (cryptographically secure) rather
than `System.Random`.

Custom slugs (authenticated users, Week 2) will reuse the same Base62 alphabet and length
constraints.

## URL validation

All URLs are validated before a link is created:

- Only `http://` and `https://` schemes are accepted
- `localhost` and private/internal IP addresses are blocked (SSRF prevention)
- Slugs must be 6–8 Base62 characters

## Getting started

```bash
dotnet build
dotnet test
```

### Database migrations

Make sure to have PostgreSQL running and configure the connection string via environment variable:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=pathy;Username=your_user;Password=your_password"
cd src/Pathy.Infrastructure
dotnet ef database update --startup-project ../Pathy.API
```

## API Endpoints

### POST /links

Creates a new short link. Anonymous users can create basic links, while authenticated users have access to advanced features.

**Request body:**

```json
{
  "url": "https://example.com/very/long/url",
  "customSlug": "myslug",     // optional, authenticated only
  "expiresAt": "2026-12-31T23:59:59Z",  // optional, authenticated only
  "password": "secret123"      // optional, authenticated only
}
```

**Success responses:**

- **201 Created** — New link created
- **200 OK** — Existing link returned (idempotency for authenticated users)

```json
{
  "slug": "abc123",
  "shortUrl": "https://your-domain.com/abc123",
  "originalUrl": "https://example.com/very/long/url",
  "createdAt": "2026-09-09T11:00:00Z",
  "expiresAt": null,
  "isPasswordProtected": false
}
```

**Error responses:**

- **400 Bad Request** — Malformed request
- **401 Unauthorized** — Advanced features require authentication
- **409 Conflict** — Custom slug already in use
- **422 Unprocessable Entity** — Invalid URL (e.g., localhost, private IPs)

**Business rules:**

1. **Idempotency:** If an authenticated user already created a link for the same URL, the existing link is returned (200 OK). Anonymous users always get a new link (201 Created).

2. **Custom slugs:** Only authenticated users can specify custom slugs. Must be 6-8 Base62 characters and unique.

3. **Advanced features:** Expiration dates and password protection are only available for authenticated users.

4. **URL validation:** All URLs are validated against SSRF attacks (localhost and private IPs are blocked).

**Examples:**

```bash
# Anonymous user - basic link
curl -X POST https://your-domain.com/links \
  -H "Content-Type: application/json" \
  -d '{"url": "https://example.com/page"}'

# Authenticated user - custom slug with expiration
curl -X POST https://your-domain.com/links \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "url": "https://example.com/page",
    "customSlug": "promo24",
    "expiresAt": "2026-12-31T23:59:59Z"
  }'
```
