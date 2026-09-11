# Pathy

URL shortener focused on reliability, analytics, and scale.

## Current status

Week 1 (core shorten and redirect) is complete. The API supports creating short links
and redirecting visitors to the original URL, with standardized error responses.

| Feature | Status |
|---|---|
| Create short links (`POST /links`) | Done |
| Redirect to original URL (`GET /{slug}`) | Done |
| Base62 slug generation (6–8 chars, cryptographically secure) | Done |
| URL validation and SSRF prevention | Done |
| Custom slugs, expiration, password protection (authenticated) | Done |
| Idempotency for authenticated users | Done |
| RFC 7807 Problem Details error responses | Done |
| EF Core + PostgreSQL persistence | Done |
| Unit and integration tests | Done |
| CI pipeline (GitHub Actions) | Done |
| JWT authentication | Planned (Week 2) |
| Link management (list, edit, delete) | Planned (Week 2) |
| Click analytics (async via queue) | Planned (Week 4) |

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

Authenticated users can also choose a **custom slug** using the same Base62 alphabet and
length constraints (6–8 characters, must be unique).

## URL validation

All URLs are validated before a link is created:

- Only `http://` and `https://` schemes are accepted
- `localhost` and private/internal IP addresses are blocked (SSRF prevention)
- Slugs must be 6–8 Base62 characters

## Error handling

All API errors follow [RFC 7807 Problem Details](https://tools.ietf.org/html/rfc7807).
The `ExceptionHandlingMiddleware` maps domain exceptions to HTTP status codes:

| Status | When |
|---|---|
| 401 Unauthorized | Advanced features used without authentication, or password required |
| 404 Not Found | Slug does not exist or link is deactivated |
| 409 Conflict | Custom slug already in use |
| 410 Gone | Link exists but has expired |
| 422 Unprocessable Entity | Domain rule violation (e.g., invalid URL) |

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for PostgreSQL)

### Build and test

```bash
dotnet build
dotnet test
```

Integration tests use an in-memory database and do not require PostgreSQL.

### Local development

1. Start PostgreSQL via Docker Compose:

```bash
docker compose up -d
```

2. Create a `.env` file in the project root (used by Docker Compose and the API):

```bash
POSTGRES_USER=pathy
POSTGRES_PASSWORD=pathy
POSTGRES_DB=pathy
ConnectionStrings__DefaultConnection=Host=localhost;Database=pathy;Username=pathy;Password=pathy
```

3. Apply database migrations:

```bash
cd src/Pathy.Infrastructure
dotnet ef database update --startup-project ../Pathy.API
```

4. Run the API:

```bash
cd src/Pathy.API
dotnet run
```

Swagger UI is available at `/swagger` in Development mode.

## API Endpoints

### POST /links

Creates a new short link. Anonymous users can create basic links, while authenticated
users have access to advanced features.

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

1. **Idempotency:** If an authenticated user already created a link for the same URL,
   the existing link is returned (200 OK). Anonymous users always get a new link
   (201 Created).

2. **Custom slugs:** Only authenticated users can specify custom slugs. Must be 6–8
   Base62 characters and unique.

3. **Advanced features:** Expiration dates and password protection are only available
   for authenticated users.

4. **URL validation:** All URLs are validated against SSRF attacks (localhost and
   private IPs are blocked).

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

### GET /{slug}

Redirects the visitor to the original URL.

**Success response:**

- **302 Found** — Redirect to the original URL

**Error responses:**

- **401 Unauthorized** — Link is password-protected (password submission not yet implemented)
- **404 Not Found** — Slug does not exist or link is deactivated (same response to avoid leaking information)
- **410 Gone** — Link exists but has expired

**Business rules:**

1. **Expired links** return 410 Gone with a friendly message, not a generic 404.
2. **Deactivated links** return 404 Not Found — indistinguishable from a non-existent slug.
3. **Password-protected links** return 401 Unauthorized without exposing the original URL.
4. **Click tracking** will be added asynchronously via queue (Week 4) and will not block the redirect.

**Examples:**

```bash
# Valid link - follows redirect
curl -L https://your-domain.com/abc123

# Expired link
curl https://your-domain.com/exp1234
# → 410 Gone: "This link has expired and is no longer available."

# Non-existent slug
curl https://your-domain.com/xyz999
# → 404 Not Found: "Short link not found."
```

## Testing

The test suite covers three layers:

- **Domain** — entity creation, slug/URL validation rules
- **Application** — use case logic (create, get/redirect) with fake repositories
- **Integration** — full HTTP request/response cycle via `WebApplicationFactory`

```bash
dotnet test --verbosity normal
```

## Roadmap

See [docs/roadmap.md](docs/roadmap.md) for the full project plan.
