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
