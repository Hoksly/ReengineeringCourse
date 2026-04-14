using NetSdrClientApp.Messages;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetControlItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var codeBytes = msg.Skip(2).Take(2);
            var parametersBytes = msg.Skip(4);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes.ToArray());

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));

            Assert.That(actualCode, Is.EqualTo((short)code));

            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var parametersBytes = msg.Skip(2);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));

            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void TranslateMessage_ControlItem_RoundTrip()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency;
            var parameters = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            // Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, parameters);
            bool success = NetSdrMessageHelper.TranslateMessage(msg, out var decodedType, out var decodedCode, out _, out var body);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(decodedType, Is.EqualTo(type));
            Assert.That(decodedCode, Is.EqualTo(code));
            Assert.That(body, Is.EqualTo(parameters));
        }

        [Test]
        public void TranslateMessage_DataItem_RoundTrip()
        {
            // Arrange: data item layout is [header][2-byte seq number][body]
            var type = NetSdrMessageHelper.MsgTypes.DataItem0;
            var expectedSeqNum = (ushort)0;
            var expectedBody = new byte[] { 0xCC, 0xDD };
            // parameters = seq number bytes + body bytes
            var parameters = new byte[] { 0x00, 0x00, 0xCC, 0xDD };

            // Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, parameters);
            bool success = NetSdrMessageHelper.TranslateMessage(msg, out var decodedType, out _, out ushort seqNum, out var body);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(decodedType, Is.EqualTo(type));
            Assert.That(seqNum, Is.EqualTo(expectedSeqNum));
            Assert.That(body, Is.EqualTo(expectedBody));
        }

        [Test]
        public void GetSamples_16bit_ReturnsCorrectValues()
        {
            // Arrange: two 16-bit samples: 0x0102 and 0x0304
            var body = new byte[] { 0x02, 0x01, 0x04, 0x03 };

            // Act
            var samples = NetSdrMessageHelper.GetSamples(16, body).ToList();

            // Assert
            Assert.That(samples.Count, Is.EqualTo(2));
            Assert.That(samples[0], Is.EqualTo(0x0102));
            Assert.That(samples[1], Is.EqualTo(0x0304));
        }

        [Test]
        public void GetSamples_8bit_ReturnsCorrectValues()
        {
            // Arrange: three 8-bit samples
            var body = new byte[] { 0x10, 0x20, 0x30 };

            // Act
            var samples = NetSdrMessageHelper.GetSamples(8, body).ToList();

            // Assert
            Assert.That(samples.Count, Is.EqualTo(3));
            Assert.That(samples[0], Is.EqualTo(0x10));
            Assert.That(samples[1], Is.EqualTo(0x20));
            Assert.That(samples[2], Is.EqualTo(0x30));
        }

        [Test]
        public void GetSamples_InvalidSize_ThrowsException()
        {
            // sampleSize of 40 bits = 5 bytes > 4 bytes max
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetSdrMessageHelper.GetSamples(40, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }).ToList());
        }
    }
}
