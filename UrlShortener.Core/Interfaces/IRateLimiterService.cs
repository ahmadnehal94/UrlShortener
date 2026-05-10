using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShortener.Core.Interfaces;

public interface IRateLimiterService
{
    Task<bool> IsAllowedAsync(string clientIp);
}
