using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EchoTcpServer
{
    public class EchoServer
    {
        private readonly int _port;
        private readonly TextWriter _logger;
        private TcpListener? _listener;
        private CancellationTokenSource _cts;

        public EchoServer(int port, TextWriter? logger = null)
        {
            _port = port;
            _logger = logger ?? Console.Out;
            _cts = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _logger.WriteLine($"Server started on port {_port}.");

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    _logger.WriteLine("Client connected.");
                    _ = Task.Run(() => HandleClientAsync(client.GetStream(), _cts.Token));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }

            _logger.WriteLine("Server shutdown.");
        }

        public async Task HandleClientAsync(Stream stream, CancellationToken token)
        {
            try
            {
                byte[] buffer = new byte[8192];
                int bytesRead;

                while (!token.IsCancellationRequested &&
                       (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    await stream.WriteAsync(buffer, 0, bytesRead, token);
                    _logger.WriteLine($"Echoed {bytesRead} bytes to the client.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                _logger.WriteLine("Client disconnected.");
            }
        }

        public void Stop()
        {
            _cts.Cancel();
            _listener?.Stop();
            _cts.Dispose();
            _logger.WriteLine("Server stopped.");
        }
    }
}
