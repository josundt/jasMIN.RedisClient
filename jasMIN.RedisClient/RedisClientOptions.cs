using System;

namespace jasMIN.Redis
{
    public class RedisClientOptions
    {
        public RedisClientOptions(string host)
        {
            this.Host = host;
        }

        private int? _port;

        public string Host { get; set; }

        public int Port
        {
            get { return this._port ?? (this.UseSsl ? 6380 : 6379); }
            set { this._port = value; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "<Pending>")]
        public string[] Credentials { get; set; } = Array.Empty<string>();

        public bool UseSsl { get; set; }
    }
}
