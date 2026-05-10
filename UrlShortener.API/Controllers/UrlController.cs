using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.API.Controllers
{
    [Route("api/shorten")]
    [ApiController]
    public class UrlController : ControllerBase
    {
        private readonly IUrlRepository _repo;

        public UrlController(IUrlRepository repo) => _repo = repo;

        // POST /api/shorten.
        [HttpPost("shorten")]
        public async Task<IActionResult> Shorten([FromBody] ShortenRequest request)
        {
            /*
            var shortUrl = await _repo.CreateAsync(new ShortUrl
            {
                OriginalUrl = request.Url
            });
            return Ok(new { shortUrl = $"https://nehal.com/{shortUrl.ShortCode}" });
            */
            // Validate URL
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
                return BadRequest(new { error = "Invalid URL format." });

            // Check custom alias availability
            if (!string.IsNullOrEmpty(request.CustomAlias))
            {
                var exists = await _repo.ShortCodeExistsAsync(request.CustomAlias);
                if (exists)
                    return Conflict(new { error = "This alias is already taken." });
            }

            var shortUrl = await _repo.CreateAsync(new ShortUrl
            {
                OriginalUrl = request.Url,
                ShortCode = request.CustomAlias ?? string.Empty
            });

            return CreatedAtAction(nameof(Shorten), new
            {
                shortUrl = $"https://localhost:7102/{shortUrl.ShortCode}",
                shortCode = shortUrl.ShortCode,
                originalUrl = shortUrl.OriginalUrl
            });
        }

        // GET /{code}  — redirect
        [HttpGet("/{code}")]
        public async Task<IActionResult> Redirect(string code)
        {
            var shortUrl = await _repo.GetByShortCodeAsync(code);
            if (shortUrl == null) return NotFound();

            await _repo.IncrementClickCountAsync(code);
            return await Redirect(shortUrl.OriginalUrl); // 302 redirect
        }
    }
    public record ShortenRequest(string Url, string? CustomAlias = null);
}
