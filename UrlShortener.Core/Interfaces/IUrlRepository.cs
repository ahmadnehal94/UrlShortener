using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShortener.Core.Entities;

namespace UrlShortener.Core.Interfaces
{
    public interface IUrlRepository
    {
        Task<ShortUrl?> GetByShortCodeAsync(string shortCode);
        Task<ShortUrl> CreateAsync(ShortUrl shortUrl);
        Task IncrementClickCountAsync(string shortCode);
        Task<bool> ShortCodeExistsAsync(string shortCode);
    }
}
