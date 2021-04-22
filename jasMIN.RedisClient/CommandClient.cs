using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace jasMIN.Redis
{
    public class CommandClient : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly bool _useSsl;
        private readonly Encoding _encoding;
        private readonly bool _validateCertificate;
        private readonly IPAddress _ipAddress;

        private readonly TcpClient _tcpClient;
        private NetworkStream? _nsStream;
        private SslStream? _sslStream;

        private bool _disposedValue;
        private const int _responseBufferSize = 65535;
        public CommandClient(string host, int port, bool useSsl, Encoding encoding, bool validateCertificate = true)
        {
            this._host = host;
            this._port = port;
            this._useSsl = useSsl;
            this._encoding = encoding;
            this._validateCertificate = validateCertificate;
            if (!IPAddress.TryParse(host, out var ipAddress))
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(host);
                ipAddress = ipHostInfo.AddressList[0];
            }
            this._ipAddress = ipAddress;
            this._tcpClient = new TcpClient();

        }

        public async Task<bool> EnsureConnectedAsync(CancellationToken cancellationToken = default)
        {
            var needsToReConnect = !this._tcpClient.Connected;
            if (needsToReConnect)
            {
                await this._tcpClient.ConnectAsync(this._ipAddress, this._port, cancellationToken).ConfigureAwait(false);
                if (this._sslStream != null)
                {
                    this._sslStream.Close();
                    this._sslStream.Dispose();
                    this._sslStream = null;
                }
                if (this._nsStream != null)
                {
                    this._nsStream.Close();
                    this._nsStream.Dispose();
                }
                this._nsStream = this._tcpClient.GetStream();
                if (this._useSsl)
                {
                    this._sslStream = new SslStream(this._nsStream, true, this.IsCertificateValid!);
                    this._sslStream.AuthenticateAsClient(this._host);
                }
            }
            return needsToReConnect;
        }

        public async Task<string> SendAsync(string command, CancellationToken cancellationToken = default)
        {
            bool isDataAvailable() => this._nsStream != null && this._nsStream.DataAvailable;

            var commandBytes = this._encoding.GetBytes(command);
            Stream? stream = this._useSsl ? this._sslStream : this._nsStream;

            if (stream == null)
            {
                throw new InvalidOperationException("Stream is null");
            }

            var bytes = await SendCommandAsync(commandBytes, stream, isDataAvailable, cancellationToken).ConfigureAwait(false);

            return this._encoding.GetString(bytes, 0, bytes.Length);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S4457:Parameter validation in \"async\"/\"await\" methods should be wrapped", Justification = "<Pending>")]
        private static async Task<byte[]> SendCommandAsync(
            byte[] command,
            Stream stream,
            Func<bool> isDataAvailable,
            CancellationToken cancellationToken = default)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (isDataAvailable is null)
            {
                throw new ArgumentNullException(nameof(isDataAvailable));
            }

            byte[]? response = null;

            await stream.WriteAsync(command, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            using var ms = new MemoryStream();
            var responseBuffer = new byte[_responseBufferSize];
            bool eof;
            do
            {
                var bytesRead = await stream.ReadAsync(responseBuffer.AsMemory(0, responseBuffer.Length), cancellationToken).ConfigureAwait(false);
                ms.Write(responseBuffer, 0, bytesRead);
                eof = !isDataAvailable();

            } while (!eof);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            response = ms.ToArray();

            return response;
        }

        private bool IsCertificateValid(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return !this._validateCertificate || sslPolicyErrors == SslPolicyErrors.None;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    if (this._sslStream != null)
                    {
                        this._sslStream.Close();
                        this._sslStream.Dispose();
                    }
                    if (this._nsStream != null)
                    {
                        this._nsStream.Close();
                        this._nsStream.Dispose();
                    }
                    this._tcpClient.Dispose();
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
