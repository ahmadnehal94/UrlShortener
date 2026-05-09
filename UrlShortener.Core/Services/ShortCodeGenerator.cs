using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShortener.Core.Services
{
    public static class ShortCodeGenerator
    {
        private const string Chars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static string Generate(long id)
        {
            var result = new List<char>();
            while (id > 0)
            {
                result.Add(Chars[(int)(id % 62)]);
                id /= 62;
            }
            result.Reverse();
            // Pad to always return 6 chars.
            return string.Concat(result).PadLeft(6, 'a');
        }
    }
}
