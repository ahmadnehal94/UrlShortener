using StackExchange.Redis;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.Infrastructure.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<string?> GetAsync(string key)
    {
        var value = await _db.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : value.ToString();

    }

    public async Task SetAsync(string key, string value, TimeSpan expiry)
    {
        await _db.StringSetAsync(key, value, expiry);
    }

    public async Task DeleteAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }
}