namespace UrlShortener.API.Models
{
    public record ShortenRequest(string Url, string? CustomAlias = null);
}
