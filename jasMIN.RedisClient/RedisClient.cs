using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace jasMIN.Redis
{
    public class RedisClient : IDisposable
    {
        private readonly CommandClient _command;
        private readonly RedisSerializer _serializer;
        private readonly string[] _credentials;
        private bool _disposedValue;

        public RedisClient(RedisClientOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this._command = new (options.Host, options.Port, options.UseSsl, Encoding.ASCII);
            this._serializer = new ();
            this._credentials = options.Credentials;
        }

        public async Task<IEnumerable<string>> SendCommandAsync(string command, CancellationToken cancellationToken = default)
        {
            var wasReconnected = await this._command.EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
            if (wasReconnected && this._credentials.Length > 0)
            {
                await this.SendConnectedCommandAsync(
                    string.Concat("AUTH ", string.Join(' ', this._credentials)), 
                    cancellationToken
                ).ConfigureAwait(false);
            }

            var result = await this.SendConnectedCommandAsync(command, cancellationToken).ConfigureAwait(false);
            return result;
        }

        private async Task<IEnumerable<string>> SendConnectedCommandAsync(string command, CancellationToken cancellationToken)
        {
            var commandSerialized = this._serializer.Serialize(command);
            var responseRaw = await this._command.SendAsync(commandSerialized, cancellationToken).ConfigureAwait(false);
            var response = this._serializer.Deserialize(responseRaw);
            return response.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    this._command.Dispose();
                }
                this._disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
