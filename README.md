# URL Shortener

A production-grade URL shortener built with .NET 9, Redis, and SQL Server.

## Tech Stack
| Technology | Purpose |
|---|---|
| .NET 9 Web API | Backend API |
| SQL Server | Persistent storage |
| Redis | Caching & rate limiting |
| Docker | Container management |
| EF Core | ORM / database migrations |

## Architecture
- **API Layer** — Controllers, middleware, request validation
- **Core Layer** — Entities, interfaces, business logic
- **Infrastructure Layer** — Database, Redis, repositories

## Features
- Shorten any long URL to a 6-character code
- Custom alias support (e.g. yourdomain.com/mylink)
- Redis cache-aside pattern for fast redirects
- Rate limiting — max 10 requests per minute per IP
- Click count tracking

## Design Decisions

### Why Base62 for short code generation?
Base62 uses a-z, A-Z, 0-9 characters. A 6-character
code gives 56 billion unique combinations. It is URL-safe
with no special characters unlike MD5 or UUID.

### Why Redis cache-aside pattern?
Every redirect hitting SQL Server directly would be slow
under high traffic. Redis serves hot URLs from memory in
under 1ms. Cache is written on first request and expires
after 24 hours.

### Why 302 over 301 redirect?
301 is cached permanently by browsers. If we ever update
where a short URL points, browsers would ignore the server.
302 keeps control on our server side.

### Why rate limiting in middleware?
Middleware runs before the controller so invalid requests
are rejected early without wasting DB or business logic
resources.

## Trade-offs
| Decision | Chosen | Rejected | Reason |
|---|---|---|---|
| Code generation | Base62 counter | MD5 hash | Shorter, URL-safe, no collisions |
| Caching strategy | Cache-aside | Write-through | Simpler, works well for read-heavy |
| Redirect type | 302 temporary | 301 permanent | Flexibility to update destination |
| Rate limiting storage | Redis | In-memory | Works across multiple servers |

## How to Run Locally

### Prerequisites
- .NET 9 SDK
- Docker Desktop

### Steps
```bash
# Start Redis with Docker
docker run -d -p 6379:6379 --name urlshortener_redis redis:latest

# Run database migrations
dotnet ef database update --project UrlShortener.Infrastructure --startup-project UrlShortener.API

# Run the API
dotnet run --project UrlShortener.API
```

Open Swagger at: `https://localhost:7001/swagger`

## API Endpoints

### POST /api/shorten
Shorten a long URL.
```json
// Request
{ "url": "https://google.com", "customAlias": "google" }

// Response
{ "shortUrl": "https://localhost:7001/google", "shortCode": "google" }
```

### GET /{code}
Redirects to the original URL.