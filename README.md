# URL Shortener

A production-grade URL shortener built with .NET 9, Redis, SQL Server and Docker.
Designed to demonstrate real-world system design concepts including caching,
rate limiting, and async processing.

---

## Tech Stack

| Technology | Purpose |
|---|---|
| .NET 9 Web API | Backend API |
| SQL Server | Persistent storage |
| Redis | Caching & rate limiting |
| Docker | Container management |
| EF Core 9 | ORM / database migrations |

---

## Architecture

- **API Layer** — Controllers, middleware, request validation
- **Core Layer** — Entities, interfaces, business logic
- **Infrastructure Layer** — Database, Redis, repositories

### Project Structure

```
UrlShortener/
  ├── UrlShortener.API/             → Controllers, Middleware, Program.cs
  │     ├── Controllers/            → UrlController.cs
  │     ├── Middleware/             → RateLimitMiddleware.cs
  │     └── Models/                 → ShortenRequest.cs
  │
  ├── UrlShortener.Core/            → Business logic, Interfaces, Entities
  │     ├── Entities/               → ShortUrl.cs
  │     ├── Interfaces/             → IUrlRepository, ICacheService, IRateLimiterService
  │     └── Services/               → ShortCodeGenerator.cs
  │
  └── UrlShortener.Infrastructure/  → DB, Redis, Repositories
        ├── Data/                   → AppDbContext.cs
        ├── Cache/                  → RedisCacheService.cs, RateLimiterService.cs
        └── Repositories/           → UrlRepository.cs
```

---

## Features

### ✅  1 — Core Shortening
- Shorten any long URL to a 6-character Base62 code
- GET /{code} redirects to original URL (302)
- SQL Server for persistent storage

### ✅ 2 — Redis Caching
- Cache-aside pattern for fast redirects
- First request served from SQL Server, cached in Redis
- Subsequent requests served from Redis (sub 1ms)
- 24 hour TTL on cached URLs

### ✅ 3 — Rate Limiting & Custom Alias
- Sliding window rate limiting using Redis
- Max 10 requests per minute per IP
- Returns 429 Too Many Requests when limit exceeded
- Custom alias support (e.g. yourdomain.com/mylink)
- Returns 409 Conflict if alias already taken

---

## Design Decisions

### Why Base62 for short code generation?
Base62 uses a-z, A-Z, 0-9 characters. A 6-character code gives 56 billion
unique combinations. It is URL-safe with no special characters unlike MD5
or UUID. We use the database Id to generate the code so there are zero collisions.

### Why Redis cache-aside pattern?
Every redirect hitting SQL Server directly would be slow under high traffic.
Redis serves hot URLs from memory in under 1ms. Cache is written on first
request and expires after 24 hours automatically.

### Why 302 over 301 redirect?
301 is cached permanently by browsers. If we ever update where a short URL
points, browsers would ignore the server completely. 302 keeps control on
our server side and allows us to update destinations anytime.

### Why rate limiting in middleware?
Middleware runs before the controller so invalid requests are rejected early
without wasting DB or business logic resources. Using Redis for rate limit
counters means it works correctly across multiple server instances.

### Why Guid as temporary ShortCode?
During creation we save the record first to get the auto-generated DB Id,
then use that Id to generate the Base62 code. Between these two steps we
need a temporary unique value. Guid guarantees uniqueness even under high
concurrent traffic, avoiding duplicate key errors.

### Why async analytics? (Week 4)
Adding click tracking synchronously would add latency to every redirect.
Instead we publish to RabbitMQ and process analytics in the background,
keeping redirects fast.

---

## Trade-offs

| Decision | Chosen | Rejected | Reason |
|---|---|---|---|
| Code generation | Base62 counter | MD5 hash | Shorter, URL-safe, no collisions |
| Caching strategy | Cache-aside | Write-through | Simpler, works well for read-heavy |
| Redirect type | 302 temporary | 301 permanent | Flexibility to update destination |
| Rate limiting storage | Redis | In-memory | Works across multiple server instances |
| Temp ShortCode | Guid | Empty string | Empty string causes duplicate key error |
| Analytics | Async RabbitMQ | Sync write | Avoid adding latency to redirects |

---

## System Design Considerations

### What if the same long URL is submitted twice?
Currently we create a new short code each time. In production we could add
a hash index on OriginalUrl and return the existing short code if it already exists.

### How would you scale to 100 million URLs?
- Add read replicas for SQL Server
- Shard the database by ShortCode prefix
- Use Redis cluster for distributed caching
- Deploy multiple API instances behind a load balancer

### How would you handle cache invalidation?
- TTL based expiry (currently 24 hours)
- Explicit cache delete when a URL is updated or deleted
- Cache warming for popular URLs on startup

---

## How to Run Locally

### Prerequisites
- .NET 9 SDK
- Docker Desktop

### Steps

```bash
# Start Redis with Docker
docker run -d -p 6379:6379 --name urlshortener_redis --restart always redis:latest

# Run database migrations
dotnet ef database update --project UrlShortener.Infrastructure --startup-project UrlShortener.API

# Run the API
dotnet run --project UrlShortener.API
```

Open Swagger at: `https://localhost:7102/swagger`

---

## API Endpoints

### POST /api/Url/shorten
Shorten a long URL with optional custom alias.

```json
// Request — auto generated code
{ "url": "https://google.com" }

// Request — custom alias
{ "url": "https://google.com", "customAlias": "google" }

// Response
{
  "shortUrl": "https://localhost:7102/google",
  "shortCode": "google",
  "originalUrl": "https://google.com"
}
```

### GET /{code}
Redirects to the original URL.

```
GET /google → 302 → https://google.com
GET /aaaaab → 302 → https://google.com
```

### GET /api/Url/stats/{code} (Week 4 — Coming Soon)
Returns click analytics for a short URL.

```json
{
  "shortCode": "google",
  "totalClicks": 152,
  "topCountries": ["India", "USA", "UK"],
  "topDevices": ["Mobile", "Desktop"]
}
```

---

## What I Would Add Next
- User authentication with JWT tokens
- Analytics dashboard with charts
- URL expiry support
- QR code generation for each short URL
- Custom domain support
- Kubernetes deployment configs
- Load testing results with k6

## How to check the Rate Limiting
- Open Developer Power Shell
- Run The below Code
- for first 10 Request in 60 seconds, you will Get status :201 and 426 for Rest 5
# Ignore SSL errors
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

for ($i = 1; $i -le 15; $i++) {
    try {
        $response = Invoke-WebRequest -Uri "https://localhost:7102/api/shorten/shorten" `
            -Method POST `
            -ContentType "application/json" `
            -Body '{"url": "https://google.com"}'

        Write-Host "Request $i - Status: $($response.StatusCode)" -ForegroundColor Green
    }
    catch [System.Net.WebException] {
        $statusCode = [int]$_.Exception.Response.StatusCode
        Write-Host "Request $i - Status: $statusCode" -ForegroundColor Red
    }
    catch {
        Write-Host "Request $i - Error: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}