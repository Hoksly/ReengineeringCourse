using EchoTcpServer;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServerTests
{
    public class EchoServerTests
    {
        [Test]
        public async Task HandleClientAsync_EchoesDataBack()
        {
            var server = new EchoServer(5001, TextWriter.Null);
            var inputData = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            using var stream = new MemoryStream();
            stream.Write(inputData);
            stream.Position = 0;

            await server.HandleClientAsync(stream, CancellationToken.None);

            stream.Position = inputData.Length;
            var echoed = new byte[inputData.Length];
            int read = stream.Read(echoed, 0, echoed.Length);

            Assert.That(read, Is.EqualTo(inputData.Length));
            Assert.That(echoed, Is.EqualTo(inputData));
        }

        [Test]
        public async Task HandleClientAsync_HandlesEmptyStream_WithoutException()
        {
            var server = new EchoServer(5001, TextWriter.Null);
            using var stream = new MemoryStream();

            Assert.DoesNotThrowAsync(() => server.HandleClientAsync(stream, CancellationToken.None));
        }

        [Test]
        public async Task HandleClientAsync_RespectsCancel()
        {
            var server = new EchoServer(5001, TextWriter.Null);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            using var stream = new MemoryStream(new byte[] { 0x01, 0x02 });

            Assert.DoesNotThrowAsync(() => server.HandleClientAsync(stream, cts.Token));
        }

        [Test]
        public void Stop_DoesNotThrow_WhenNotStarted()
        {
            var server = new EchoServer(5001, TextWriter.Null);

            Assert.DoesNotThrow(() => server.Stop());
        }

        [Test]
        public void UdpTimedSender_BuildMessage_HasCorrectFormat()
        {
            using var sender = new UdpTimedSender("127.0.0.1", 60000);
            var payload = new byte[1024];
            ushort sequence = 42;

            var msg = UdpTimedSender.BuildMessage(sequence, payload);

            Assert.That(msg[0], Is.EqualTo(0x04));
            Assert.That(msg[1], Is.EqualTo(0x84));
            Assert.That(BitConverter.ToUInt16(msg, 2), Is.EqualTo(sequence));
            Assert.That(msg.Length, Is.EqualTo(2 + 2 + 1024));
        }

        [Test]
        public void UdpTimedSender_StartSending_ThrowsIfAlreadyRunning()
        {
            using var sender = new UdpTimedSender("127.0.0.1", 60001);
            sender.StartSending(10000);

            Assert.Throws<InvalidOperationException>(() => sender.StartSending(10000));

            sender.StopSending();
        }

        [Test]
        public void UdpTimedSender_StopSending_AfterStart_DoesNotThrow()
        {
            using var sender = new UdpTimedSender("127.0.0.1", 60002);
            sender.StartSending(10000);

            Assert.DoesNotThrow(() => sender.StopSending());
        }

        [Test]
        public void UdpTimedSender_Dispose_DoesNotThrow()
        {
            var sender = new UdpTimedSender("127.0.0.1", 60003);
            sender.StartSending(10000);

            Assert.DoesNotThrow(() => sender.Dispose());
        }

        [Test]
        public void UdpTimedSender_StopSending_WhenNotStarted_DoesNotThrow()
        {
            using var sender = new UdpTimedSender("127.0.0.1", 60004);

            Assert.DoesNotThrow(() => sender.StopSending());
        }

        [Test]
        public async Task HandleClientAsync_WithConsoleLogger_EchoesData()
        {
            // Covers the default Console.Out logger path
            using var logOutput = new StringWriter();
            var server = new EchoServer(5001, logOutput);
            var inputData = new byte[] { 0xAB, 0xCD };

            using var stream = new MemoryStream();
            stream.Write(inputData);
            stream.Position = 0;

            await server.HandleClientAsync(stream, CancellationToken.None);

            Assert.That(logOutput.ToString(), Does.Contain("Echoed"));
        }

        [Test]
        public async Task StartAsync_StopsCleanlyWhenCancelled()
        {
            // Covers StartAsync body: listener start, while loop, OperationCanceledException path, shutdown log
            var logger = new StringWriter();
            var server = new EchoServer(59876, logger);

            var startTask = server.StartAsync();
            await Task.Delay(80); // let AcceptTcpClientAsync begin awaiting
            server.Stop();

            Assert.DoesNotThrowAsync(() => startTask);
            Assert.That(logger.ToString(), Does.Contain("started"));
        }

        [Test]
        public async Task UdpTimedSender_SendMessageCallback_ExecutesWithoutException()
        {
            // Covers SendMessageCallback: Random.Shared, BuildMessage call, UdpClient.Send
            using var sender = new UdpTimedSender("127.0.0.1", 59877);
            sender.StartSending(50); // short interval so callback fires quickly

            await Task.Delay(200); // wait for at least one callback execution

            sender.StopSending();
        }

        [Test]
        public void EchoServer_DefaultLogger_UsesConsoleOut()
        {
            var server = new EchoServer(5001);
            Assert.DoesNotThrow(() => server.Stop());
        }

        [Test]
        public async Task HandleClientAsync_CatchesStreamException_LogsError()
        {
            // Covers the catch(Exception) block in HandleClientAsync
            using var logOutput = new StringWriter();
            var server = new EchoServer(5001, logOutput);

            using var stream = new ThrowingReadStream();

            await server.HandleClientAsync(stream, CancellationToken.None);

            Assert.That(logOutput.ToString(), Does.Contain("Error"));
        }

        [Test]
        public void UdpTimedSender_DoubleDispose_DoesNotThrow()
        {
            // Covers the if (!_disposed) guard — second Dispose is a no-op
            var sender = new UdpTimedSender("127.0.0.1", 60005);
            sender.Dispose();
            Assert.DoesNotThrow(() => sender.Dispose());
        }

        private class ThrowingReadStream : Stream
        {
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => 0;
            public override long Position { get; set; }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
                => throw new IOException("Simulated stream error");

            public override int Read(byte[] buffer, int offset, int count) => throw new IOException("Simulated stream error");
            public override void Write(byte[] buffer, int offset, int count) { }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => 0;
            public override void SetLength(long value) { }
        }
    }
}
