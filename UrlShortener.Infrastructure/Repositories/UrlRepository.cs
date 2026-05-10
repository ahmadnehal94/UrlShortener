using Microsoft.EntityFrameworkCore;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Interfaces;
using UrlShortener.Core.Services;
using UrlShortener.Infrastructure.Data;

namespace UrlShortener.Infrastructure.Repositories;

public class UrlRepository : IUrlRepository
{
    private readonly AppDbContext _db;
    private readonly ICacheService _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public UrlRepository(AppDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<ShortUrl?> GetByShortCodeAsync(string shortCode)
    {
        // 1. Check Redis first
        var cachedUrl = await _cache.GetAsync($"url:{shortCode}");
        if (cachedUrl != null)
            return new ShortUrl { ShortCode = shortCode, OriginalUrl = cachedUrl };

        // 2. Cache miss — go to SQL Server
        var shortUrl = await _db.ShortUrls
            .FirstOrDefaultAsync(u => u.ShortCode == shortCode);

        // 3. Write back to Redis for next time
        if (shortUrl != null)
            await _cache.SetAsync($"url:{shortCode}", shortUrl.OriginalUrl, CacheTtl);

        return shortUrl;
    }

    public async Task<ShortUrl> CreateAsync(ShortUrl shortUrl)
    {
        shortUrl.ShortCode= Guid.NewGuid().ToString("N").Substring(0, 8); // Temporary code to ensure uniqueness
        _db.ShortUrls.Add(shortUrl);
        await _db.SaveChangesAsync();

        // Use custom alias if provided, otherwise generate from Id
        if (string.IsNullOrEmpty(shortUrl.ShortCode))
            shortUrl.ShortCode = ShortCodeGenerator.Generate(shortUrl.Id);
       // shortUrl.ShortCode = ShortCodeGenerator.Generate(shortUrl.Id);
        await _db.SaveChangesAsync();

        // Cache immediately after creation
        await _cache.SetAsync($"url:{shortUrl.ShortCode}", shortUrl.OriginalUrl, CacheTtl);

        return shortUrl;
    }

    public async Task IncrementClickCountAsync(string shortCode)
    {
        await _db.ShortUrls
            .Where(u => u.ShortCode == shortCode)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(u => u.ClickCount, u => u.ClickCount + 1));
    }
    public async Task<bool> ShortCodeExistsAsync(string shortCode)
    {
        return await _db.ShortUrls.AnyAsync(u => u.ShortCode == shortCode);
    }
}