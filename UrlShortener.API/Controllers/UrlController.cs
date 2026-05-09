using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UrlController : ControllerBase
    {
        private readonly IUrlRepository _repo;

        public UrlController(IUrlRepository repo) => _repo = repo;

        // POST /api/shorten.
        [HttpPost("shorten")]
        public async Task<IActionResult> Shorten([FromBody] ShortenRequest request)
        {
            var shortUrl = await _repo.CreateAsync(new ShortUrl
            {
                OriginalUrl = request.Url
            });
            return Ok(new { shortUrl = $"https://nehal.com/{shortUrl.ShortCode}" });
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
    public record ShortenRequest(string Url);
}
