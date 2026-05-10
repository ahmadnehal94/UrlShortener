using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UrlShortener.API.Middleware;
using UrlShortener.Core.Interfaces;
using UrlShortener.Infrastructure.Cache;
using UrlShortener.Infrastructure.Data;
using UrlShortener.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency injection
builder.Services.AddScoped<IUrlRepository, UrlRepository>();
// Redis
var redisConnection = builder.Configuration.GetConnectionString("Redis")!;
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddScoped<ICacheService, RedisCacheService>();
// Rate limiter
builder.Services.AddScoped<IRateLimiterService, RateLimiterService>();
var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<RateLimitMiddleware>(); // add this line
app.UseAuthorization();
app.MapControllers();

app.Run();