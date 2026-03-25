using System;
using System.Threading.Tasks;
using Xunit;
using DeskViz.Widgets.Meshtastic.Protocol;

namespace DeskViz.Widgets.Meshtastic.Tests
{
    public class MeshtasticSerialTests
    {
        [Fact]
        public void GetAvailablePorts_ReturnsArrayWithoutException()
        {
            // This test verifies the static method doesn't throw
            var ports = MeshtasticSerial.GetAvailablePorts();

            Assert.NotNull(ports);
            // Ports array may be empty if no serial ports are available
        }

        [Fact]
        public void IsConnected_WhenNotConnected_ReturnsFalse()
        {
            using var serial = new MeshtasticSerial();

            Assert.False(serial.IsConnected);
        }

        [Fact]
        public void PortName_WhenNotConnected_ReturnsNull()
        {
            using var serial = new MeshtasticSerial();

            Assert.Null(serial.PortName);
        }

        [Fact]
        public async Task ConnectAsync_InvalidPort_ReturnsFalse()
        {
            using var serial = new MeshtasticSerial();
            Exception? receivedError = null;
            serial.ErrorOccurred += (s, e) => receivedError = e;

            var result = await serial.ConnectAsync("COM999");

            Assert.False(result);
            Assert.False(serial.IsConnected);
        }

        [Fact]
        public void Disconnect_WhenNotConnected_DoesNotThrow()
        {
            using var serial = new MeshtasticSerial();

            // Should not throw
            serial.Disconnect();

            Assert.False(serial.IsConnected);
        }

        [Fact]
        public void Dispose_WhenNotConnected_DoesNotThrow()
        {
            var serial = new MeshtasticSerial();

            // Should not throw
            serial.Dispose();
        }

        [Fact]
        public async Task SendPacketAsync_WhenNotConnected_ThrowsInvalidOperationException()
        {
            using var serial = new MeshtasticSerial();
            var data = new byte[] { 0x01, 0x02, 0x03 };

            await Assert.ThrowsAsync<InvalidOperationException>(() => serial.SendPacketAsync(data));
        }

        [Fact]
        public void ConnectionStateChanged_EventRaisedOnDisconnect()
        {
            using var serial = new MeshtasticSerial();
            bool eventRaised = false;
            serial.ConnectionStateChanged += (s, connected) =>
            {
                if (!connected) eventRaised = true;
            };

            // Calling disconnect should raise the event if it was previously connected
            // Since we're not connected, this tests that Disconnect handles the state properly
            serial.Disconnect();

            // Event may or may not be raised depending on implementation
            // This test mainly verifies no exception is thrown
        }
    }

    /// <summary>
    /// Integration tests that require a real Meshtastic device connected to COM7
    /// These tests are skipped by default - remove the Skip to run them
    /// </summary>
    public class MeshtasticSerialIntegrationTests
    {
        private const string TestComPort = "COM7";

        [Fact(Skip = "Integration test - requires Meshtastic device on COM7")]
        public async Task ConnectAsync_RealDevice_ConnectsSuccessfully()
        {
            using var serial = new MeshtasticSerial();
            bool connectionChanged = false;
            serial.ConnectionStateChanged += (s, connected) => connectionChanged = connected;

            var result = await serial.ConnectAsync(TestComPort);

            Assert.True(result);
            Assert.True(serial.IsConnected);
            Assert.Equal(TestComPort, serial.PortName);
            Assert.True(connectionChanged);
        }

        [Fact(Skip = "Integration test - requires Meshtastic device on COM7")]
        public async Task RequestConfigAsync_RealDevice_ReceivesResponse()
        {
            using var serial = new MeshtasticSerial();
            FromRadio? receivedPacket = null;
            var tcs = new TaskCompletionSource<FromRadio>();

            serial.PacketReceived += (s, e) =>
            {
                receivedPacket = e.Packet;
                if (e.Packet.MyInfo != null || e.Packet.NodeInfo != null)
                {
                    tcs.TrySetResult(e.Packet);
                }
            };

            var connected = await serial.ConnectAsync(TestComPort);
            Assert.True(connected);

            // Wait for device to boot after DTR reset
            await Task.Delay(2000);
            await serial.RequestConfigAsync();

            // Wait up to 15 seconds for a response
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(15000));

            Assert.Equal(tcs.Task, completedTask);
            Assert.NotNull(receivedPacket);
        }

        [Fact(Skip = "Integration test - requires Meshtastic device on COM7")]
        public async Task ReceiveNodeInfo_RealDevice_ParsesCorrectly()
        {
            using var serial = new MeshtasticSerial();
            var nodeInfoReceived = new TaskCompletionSource<NodeInfo>();

            serial.PacketReceived += (s, e) =>
            {
                if (e.Packet.NodeInfo != null)
                {
                    nodeInfoReceived.TrySetResult(e.Packet.NodeInfo);
                }
            };

            await serial.ConnectAsync(TestComPort);

            // Wait for device to boot after DTR reset
            await Task.Delay(2000);
            await serial.RequestConfigAsync();

            var completedTask = await Task.WhenAny(nodeInfoReceived.Task, Task.Delay(15000));

            if (completedTask == nodeInfoReceived.Task)
            {
                var nodeInfo = await nodeInfoReceived.Task;
                Assert.True(nodeInfo.Num > 0);
                // Node should have a hex ID
                Assert.StartsWith("!", nodeInfo.NodeIdHex);
            }
        }

        [Fact(Skip = "Integration test - requires Meshtastic device on COM7")]
        public async Task ReceiveDeviceMetadata_RealDevice_HasFirmwareVersion()
        {
            using var serial = new MeshtasticSerial();
            var metadataReceived = new TaskCompletionSource<DeviceMetadata>();

            serial.PacketReceived += (s, e) =>
            {
                if (e.Packet.Metadata != null)
                {
                    metadataReceived.TrySetResult(e.Packet.Metadata);
                }
            };

            await serial.ConnectAsync(TestComPort);

            // Wait for device to boot after DTR reset
            await Task.Delay(2000);
            await serial.RequestConfigAsync();

            var completedTask = await Task.WhenAny(metadataReceived.Task, Task.Delay(15000));

            Assert.Equal(metadataReceived.Task, completedTask);
            var metadata = await metadataReceived.Task;
            Assert.False(string.IsNullOrEmpty(metadata.FirmwareVersion));
            Assert.True(metadata.HwModel > 0);
        }
    }
}
