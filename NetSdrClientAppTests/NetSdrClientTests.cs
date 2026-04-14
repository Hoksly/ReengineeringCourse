using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _updMock;

    public NetSdrClientTests() { }

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
        {
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
        });

        _updMock = new Mock<IUdpClient>();

        _client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
    }

    [Test]
    public async Task ConnectAsyncTest()
    {
        //act
        await _client.ConnectAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public async Task DisconnectWithNoConnectionTest()
    {
        //act
        _client.Disconnect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        //Arrange
        await ConnectAsyncTest();

        //act
        _client.Disconnect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {
        //act
        await _client.StartIQAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task StartIQTest()
    {
        //Arrange
        await ConnectAsyncTest();

        //act
        await _client.StartIQAsync();

        //assert
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQTest()
    {
        //Arrange
        await ConnectAsyncTest();

        //act
        await _client.StopIQAsync();

        //assert
        _updMock.Verify(tcp => tcp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }

    [Test]
    public async Task StopIQNoConnectionTest()
    {
        // Act
        await _client.StopIQAsync();

        // Assert: no messages sent when not connected
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
    }

    [Test]
    public async Task ChangeFrequencyAsync_SendsCorrectMessage()
    {
        // Arrange
        await ConnectAsyncTest();

        // Act
        await _client.ChangeFrequencyAsync(20000000, 1);

        // Assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(4));
    }

    [Test]
    public async Task UdpMessageReceived_ProcessesSamplesWithoutException()
    {
        // Arrange: build a valid DataItem0 message [seq(2)][body(4)]
        var seqBytes = BitConverter.GetBytes((ushort)1);
        var bodyBytes = new byte[] { 0x01, 0x00, 0x02, 0x00 }; // two 16-bit samples
        var msg = NetSdrMessageHelper.GetDataItemMessage(
            NetSdrMessageHelper.MsgTypes.DataItem0,
            seqBytes.Concat(bodyBytes).ToArray());

        // Act: fire UDP MessageReceived — covers _udpClient_MessageReceived
        Assert.DoesNotThrow(() =>
            _updMock.Raise(u => u.MessageReceived += null, _updMock.Object, msg));

        // Cleanup the file written by the handler
        if (File.Exists("samples.bin"))
            File.Delete("samples.bin");
    }

    [Test]
    public async Task ConnectAsync_WhenAlreadyConnected_DoesNotReconnect()
    {
        // Arrange
        await ConnectAsyncTest();

        // Act: connect again
        await _client.ConnectAsync();

        // Assert: Connect() only called once (second call skipped because Connected=true)
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
    }
}
