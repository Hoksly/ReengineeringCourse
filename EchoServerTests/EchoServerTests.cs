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
            // Arrange
            var server = new EchoServer(5001, TextWriter.Null);
            var inputData = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            using var stream = new MemoryStream();
            stream.Write(inputData);
            stream.Position = 0;

            // Act
            await server.HandleClientAsync(stream, CancellationToken.None);

            // Assert: echoed bytes start at offset = inputData.Length
            stream.Position = inputData.Length;
            var echoed = new byte[inputData.Length];
            int read = stream.Read(echoed, 0, echoed.Length);

            Assert.That(read, Is.EqualTo(inputData.Length));
            Assert.That(echoed, Is.EqualTo(inputData));
        }

        [Test]
        public async Task HandleClientAsync_HandlesEmptyStream_WithoutException()
        {
            // Arrange: empty stream simulates immediate client disconnect
            var server = new EchoServer(5001, TextWriter.Null);
            using var stream = new MemoryStream();

            // Act & Assert: should not throw
            Assert.DoesNotThrowAsync(() => server.HandleClientAsync(stream, CancellationToken.None));
        }

        [Test]
        public async Task HandleClientAsync_RespectsCancel()
        {
            // Arrange
            var server = new EchoServer(5001, TextWriter.Null);
            using var cts = new CancellationTokenSource();

            // A stream that blocks on read indefinitely — simulate with a PipeStream
            // Instead: cancel before calling, so ReadAsync returns immediately
            cts.Cancel();
            using var stream = new MemoryStream(new byte[] { 0x01, 0x02 });

            // Act & Assert: should not throw even with cancelled token
            Assert.DoesNotThrowAsync(() => server.HandleClientAsync(stream, cts.Token));
        }

        [Test]
        public void Stop_DoesNotThrow_WhenNotStarted()
        {
            // Arrange
            var server = new EchoServer(5001, TextWriter.Null);

            // Act & Assert: Stop without Start should not throw NullReferenceException
            Assert.DoesNotThrow(() => server.Stop());
        }

        [Test]
        public void UdpTimedSender_BuildMessage_HasCorrectFormat()
        {
            // Arrange
            using var sender = new UdpTimedSender("127.0.0.1", 60000);
            var payload = new byte[1024];
            ushort sequence = 42;

            // Act
            var msg = UdpTimedSender.BuildMessage(sequence, payload);

            // Assert: header bytes, sequence number, then payload
            Assert.That(msg[0], Is.EqualTo(0x04));
            Assert.That(msg[1], Is.EqualTo(0x84));
            Assert.That(BitConverter.ToUInt16(msg, 2), Is.EqualTo(sequence));
            Assert.That(msg.Length, Is.EqualTo(2 + 2 + 1024));
        }
    }
}
