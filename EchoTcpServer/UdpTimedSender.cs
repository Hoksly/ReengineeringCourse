using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EchoTcpServer
{
    public class UdpTimedSender : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly UdpClient _udpClient;
        private Timer? _timer;
        private ushort _sequence;
        private bool _disposed;

        public UdpTimedSender(string host, int port)
        {
            _host = host;
            _port = port;
            _udpClient = new UdpClient();
        }

        public void StartSending(int intervalMilliseconds)
        {
            if (_timer != null)
                throw new InvalidOperationException("Sender is already running.");

            _timer = new Timer(SendMessageCallback, null, 0, intervalMilliseconds);
        }

        public static byte[] BuildMessage(ushort sequence, byte[] payload)
        {
            return (new byte[] { 0x04, 0x84 })
                .Concat(BitConverter.GetBytes(sequence))
                .Concat(payload)
                .ToArray();
        }

        private void SendMessageCallback(object? state)
        {
            try
            {
                byte[] samples = new byte[1024];
                Random.Shared.NextBytes(samples);
                _sequence++;

                byte[] msg = BuildMessage(_sequence, samples);
                var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);

                _udpClient.Send(msg, msg.Length, endpoint);
                Console.WriteLine($"Message sent to {_host}:{_port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        public void StopSending()
        {
            _timer?.Dispose();
            _timer = null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    StopSending();
                    _udpClient.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
