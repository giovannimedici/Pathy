# Project Roadmap

> This file is a planning reference, not an automatic Cursor rule.
> When requesting a task, mention the current phase/week for extra context.

## Phase 0 — Setup
Repo, .gitignore, .NET solution (Clean Architecture: Domain/Application/
Infrastructure/Api), initial README.

## Week 1 — Core: shorten and redirect (local)
ShortLink entity + unit tests for slug generation. POST /links endpoint
(EF Core + Postgres/SQLite local). GET /{slug} endpoint with 302 redirect.
Standardized errors (RFC 7807). Docker Compose + integration tests + basic CI.

## Week 2 — Authentication and link management
JWT (registration/login). Link ownership. Custom slug. Paginated listing and
editing. Soft delete vs hard delete. Basic rate limiting for anonymous users.

## Week 3 — Deploying to AWS
IaC (Terraform or CDK): RDS/Aurora Serverless, ECS/Fargate or Lambda. Public
deployment, custom domain (Route53 + certificate). Secrets Manager/Parameter
Store. CI/CD with automatic deployment on merge. Basic load testing.

## Week 4 — Asynchronous analytics
Click event published to SQS queue on redirect (non-blocking). Separate worker
consuming the queue. Aggregations (clicks/day, top referrers) with documented
eventual consistency. GET /links/{id}/analytics endpoint.
Approximate IP geolocation, without storing full IP (LGPD/GDPR).

## Week 5 — React frontend
Login/registration, create link, list my links, analytics dashboard
(click chart). Deployment (S3 + CloudFront). Polish: loading, errors,
responsiveness.

## Week 6 (optional) — Differentiators
QR code on link creation. Password protection for links. Simple admin panel.
Documented load tests (k6). Architecture diagram in README.
