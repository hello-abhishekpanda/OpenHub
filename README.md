# OpenHub

OpenHub is an enterprise-grade, open-source discovery hub for developer profiles, GitHub activity, and high-speed global rankings.

## Architecture

- **UI:** Blazor WebAssembly with a responsive crimson design system
- **API:** ASP.NET Core 9 controllers, OpenAPI, health checks, global error handling
- **Sync:** bounded in-memory queue + `BackgroundService`; HTTP calls never hold the UI request open
- **Persistence:** PostgreSQL via EF Core
- **Ranking:** Redis sorted set `opencode:leaderboard:global`
- **GitHub:** typed REST client with standard .NET resilience pipeline, timeout, exponential retry, and rate-limit inspection

The API deliberately returns `202 Accepted` when a sync is queued. The worker retrieves the user, paginates all public repositories, reads each repository's last 30 days of commits, upserts PostgreSQL metadata, calculates the score, and updates Redis.

```
Browser → POST /profiles/{username}/sync → bounded queue → sync worker
                                                     ├─ GitHub REST
                                                     ├─ PostgreSQL
                                                     └─ Redis ZSET
```

## Ranking formula

```
Score = Σ [Stars + (Forks × 2) + RecentCommitsLast30Days / (DaysSinceLastCommit + 1)]
```

The sum is across synchronized repositories. Fork and archived flags plus GitHub topics are persisted now, enabling upcoming filters for AI projects, tags, forks, and contribution tasks.

## Run locally

Prerequisites: .NET 9 SDK and Docker Desktop.

```bash
cp .env.example .env
docker compose up -d
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/OpenHub.Infrastructure --startup-project src/OpenHub.Api
dotnet ef database update --project src/OpenHub.Infrastructure --startup-project src/OpenHub.Api
```

Store the GitHub token outside source control:

```bash
dotnet user-secrets init --project src/OpenHub.Api
dotnet user-secrets set "GitHub:Token" "github_pat_..." --project src/OpenHub.Api
```

Run the API and UI in separate terminals:

```bash
dotnet run --project src/OpenHub.Api --urls https://localhost:7001
dotnet run --project src/OpenHub.Web --urls https://localhost:7101
```

Open `https://localhost:7101/settings` and select **Test API connection**. This calls the API, which authenticates server-side, returns the GitHub login and remaining request budget, and never exposes the token to Blazor.

## API

| Endpoint | Purpose |
| --- | --- |
| `GET /api/profiles/search?query=...` | Search synchronized profiles |
| `GET /api/profiles/{username}` | Profile card data and rank |
| `POST /api/profiles/{username}/sync` | Queue deep hydration |
| `GET /api/leaderboard?page=1&pageSize=20` | Redis-backed ranking page |
| `POST /api/settings/github/test` | Test credentials and rate limit |
| `GET /health` | PostgreSQL and Redis readiness |

## Production notes

Replace the process-local channel with Redis Streams, RabbitMQ, Azure Service Bus, or Kafka before horizontally scaling workers; the `ISyncQueue` abstraction makes this isolated. Protect sync/settings endpoints with authentication and rate limiting. Use a GitHub App installation token for production. Run migrations as a deployment job rather than on every API replica. Add OpenTelemetry and distributed tracing before production rollout.

GitHub REST does not provide a single total-contribution-calendar endpoint. The current `TotalContributions` is the synchronized 30-day commit count. A future GraphQL adapter can enrich contribution calendars without changing the dashboard contracts.
