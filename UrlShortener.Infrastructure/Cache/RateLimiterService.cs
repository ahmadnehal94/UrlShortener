using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.Infrastructure.Cache;

public class RateLimiterService : IRateLimiterService
{
    private readonly IDatabase _db;
    private const int MaxRequests = 10;        // max 10 requests
    private const int WindowSeconds = 60;      // per 60 seconds
    public RateLimiterService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase(); 
    }
    public async Task<bool> IsAllowedAsync(string clientIp)
    {
        var key = $"ratelimit:{clientIp}";
        var current = await _db.StringIncrementAsync(key);

        // Set expiry only on first request
        if (current == 1)
            await _db.KeyExpireAsync(key, TimeSpan.FromSeconds(WindowSeconds));

        return current <= MaxRequests;
    }
}
