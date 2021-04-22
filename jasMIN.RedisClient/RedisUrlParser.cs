using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace jasMIN.Redis
{
    static class RedisUrlParser
    {
        private static readonly Regex _regex = new(@"^(tcps?)://(?:([^@]+)@)?([^:]+)(?::(\d+))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool TryParseRedisUrl(string url, out RedisClientOptions options)
        {
            try
            {
                var match = _regex.Match(url);
                if (match == null)
                {
                    options = null!;
                    return false;
                }
                var useSsl = match.Groups[1].Value.ToLowerInvariant() == "tcps";
                var defaultPort = useSsl ? 6380 : 6379;

                options = new RedisClientOptions(host: match.Groups[3].Value.ToLowerInvariant())
                {
                    UseSsl = useSsl,
                    Credentials = string.IsNullOrEmpty(match.Groups[2].Value) ? Array.Empty<string>() : match.Groups[2].Value.Split(":"),
                    Port = string.IsNullOrEmpty(match.Groups[4].Value) ? defaultPort : int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture)
                };
                return true;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                options = null!;
                return false;
            }

        }
    }
}
