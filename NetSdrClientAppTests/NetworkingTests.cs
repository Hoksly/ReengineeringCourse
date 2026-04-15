using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class NetworkingTests
{
    [Test]
    public void TcpClientWrapper_Connected_IsFalseWhenNotConnected()
    {
        var wrapper = new TcpClientWrapper("127.0.0.1", 5000);
        Assert.That(wrapper.Connected, Is.False);
    }

    [Test]
    public void TcpClientWrapper_Disconnect_WhenNotConnected_DoesNotThrow()
    {
        // Covers the "No active connection to disconnect" else-branch
        var wrapper = new TcpClientWrapper("127.0.0.1", 5000);
        Assert.DoesNotThrow(() => wrapper.Disconnect());
    }

    [Test]
    public async Task TcpClientWrapper_SendMessageAsync_WhenNotConnected_ThrowsInvalidOperation()
    {
        var wrapper = new TcpClientWrapper("127.0.0.1", 5000);
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            wrapper.SendMessageAsync(new byte[] { 0x01 }));
    }

    [Test]
    public void UdpClientWrapper_StopListening_WhenNotStarted_DoesNotThrow()
    {
        var wrapper = new UdpClientWrapper(59900);
        Assert.DoesNotThrow(() => wrapper.StopListening());
    }

    [Test]
    public void UdpClientWrapper_GetHashCode_ReturnsConsistentValue()
    {
        var wrapper = new UdpClientWrapper(59901);
        var hash1 = wrapper.GetHashCode();
        var hash2 = wrapper.GetHashCode();
        Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void UdpClientWrapper_Equals_ReturnsFalseForDifferentType()
    {
        var wrapper = new UdpClientWrapper(59902);
        Assert.That(wrapper.Equals("not a wrapper"), Is.False);
    }

    [Test]
    public void UdpClientWrapper_Equals_ReturnsTrueForSamePort()
    {
        var w1 = new UdpClientWrapper(59903);
        var w2 = new UdpClientWrapper(59903);
        Assert.That(w1.Equals(w2), Is.True);
    }
}
