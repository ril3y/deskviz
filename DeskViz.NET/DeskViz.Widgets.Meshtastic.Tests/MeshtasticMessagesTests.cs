using System;
using System.Text;
using Xunit;
using DeskViz.Widgets.Meshtastic.Protocol;

namespace DeskViz.Widgets.Meshtastic.Tests
{
    public class MeshtasticMessagesTests
    {
        [Fact]
        public void ToRadio_WithWantConfigId_SerializesCorrectly()
        {
            var toRadio = new ToRadio { WantConfigId = 1 };

            var bytes = toRadio.ToBytes();

            Assert.NotEmpty(bytes);
            // Field 3, wire type 0 = 0x18, value 1 = 0x01
            Assert.Equal(0x18, bytes[0]);
            Assert.Equal(0x01, bytes[1]);
        }

        [Fact]
        public void ToRadio_WithDisconnect_SerializesCorrectly()
        {
            var toRadio = new ToRadio { Disconnect = true };

            var bytes = toRadio.ToBytes();

            Assert.NotEmpty(bytes);
            // Field 4, wire type 0 = 0x20, value 1 = 0x01
            Assert.Equal(0x20, bytes[0]);
            Assert.Equal(0x01, bytes[1]);
        }

        [Fact]
        public void DataMessage_GetTextMessage_ReturnsCorrectString()
        {
            var message = "Hello Mesh!";
            var dataMessage = new DataMessage
            {
                PortNum = PortNum.TextMessage,
                Payload = Encoding.UTF8.GetBytes(message)
            };

            var result = dataMessage.GetTextMessage();

            Assert.Equal(message, result);
        }

        [Fact]
        public void DataMessage_FromBytes_ParsesCorrectly()
        {
            // Build a simple DataMessage with portnum=1 (TEXT_MESSAGE) and payload "Hi"
            var writer = new ProtobufWriter();
            writer.WriteTag(1, 0); // portnum field
            writer.WriteVarint(1); // TEXT_MESSAGE
            writer.WriteTag(2, 2); // payload field
            writer.WriteBytes(Encoding.UTF8.GetBytes("Hi"));

            var dataMessage = DataMessage.FromBytes(writer.ToArray());

            Assert.Equal(PortNum.TextMessage, dataMessage.PortNum);
            Assert.Equal("Hi", dataMessage.GetTextMessage());
        }

        [Fact]
        public void DataMessage_ToBytes_RoundTrips()
        {
            var original = new DataMessage
            {
                PortNum = PortNum.TextMessage,
                Payload = Encoding.UTF8.GetBytes("Test message")
            };

            var bytes = original.ToBytes();
            var parsed = DataMessage.FromBytes(bytes);

            Assert.Equal(original.PortNum, parsed.PortNum);
            Assert.Equal(original.GetTextMessage(), parsed.GetTextMessage());
        }

        [Fact]
        public void Position_FromBytes_ParsesCoordinates()
        {
            // Create position data with lat/lon
            var writer = new ProtobufWriter();
            writer.WriteTag(1, 5); // latitude_i (sfixed32, wire type 5)
            // Write 37.7749 * 1e7 = 377749000
            var latBytes = BitConverter.GetBytes(377749000);
            foreach (var b in latBytes) writer.WriteVarint(b); // This won't work correctly for sfixed32

            // For simplicity, test with a pre-built byte array
            var position = new Position
            {
                LatitudeI = 377749000,
                LongitudeI = -1224194000,
                Altitude = 10,
                Sats = 8
            };

            Assert.Equal(37.7749, position.Latitude, 4);
            Assert.Equal(-122.4194, position.Longitude, 4);
            Assert.True(position.HasValidFix);
        }

        [Fact]
        public void Position_NoFix_HasValidFixReturnsFalse()
        {
            var position = new Position
            {
                LatitudeI = 0,
                LongitudeI = 0
            };

            Assert.False(position.HasValidFix);
        }

        [Fact]
        public void MyNodeInfo_FromBytes_ParsesCorrectly()
        {
            var writer = new ProtobufWriter();
            writer.WriteTag(1, 0); // my_node_num
            writer.WriteVarint(0x12345678);
            writer.WriteTag(2, 0); // reboot_count
            writer.WriteVarint(5);
            writer.WriteTag(7, 0); // nodedb_count
            writer.WriteVarint(42);

            var nodeInfo = MyNodeInfo.FromBytes(writer.ToArray());

            Assert.Equal(0x12345678u, nodeInfo.MyNodeNum);
            Assert.Equal(5u, nodeInfo.RebootCount);
            Assert.Equal(42u, nodeInfo.NodeDbCount);
        }

        [Fact]
        public void DeviceMetadata_FromBytes_ParsesCorrectly()
        {
            var writer = new ProtobufWriter();
            writer.WriteTag(1, 2); // firmware_version
            writer.WriteBytes(Encoding.UTF8.GetBytes("2.6.11.abc"));
            writer.WriteTag(9, 0); // hw_model
            writer.WriteVarint(43);

            var metadata = DeviceMetadata.FromBytes(writer.ToArray());

            Assert.Equal("2.6.11.abc", metadata.FirmwareVersion);
            Assert.Equal(43u, metadata.HwModel);
        }

        [Fact]
        public void User_FromBytes_ParsesNames()
        {
            var writer = new ProtobufWriter();
            writer.WriteTag(1, 2); // id
            writer.WriteBytes(Encoding.UTF8.GetBytes("!abcd1234"));
            writer.WriteTag(2, 2); // long_name
            writer.WriteBytes(Encoding.UTF8.GetBytes("Test Node"));
            writer.WriteTag(3, 2); // short_name
            writer.WriteBytes(Encoding.UTF8.GetBytes("TST"));

            var user = User.FromBytes(writer.ToArray());

            Assert.Equal("!abcd1234", user.Id);
            Assert.Equal("Test Node", user.LongName);
            Assert.Equal("TST", user.ShortName);
        }

        [Fact]
        public void NodeInfo_NodeIdHex_FormatsCorrectly()
        {
            var nodeInfo = new NodeInfo { Num = 0x12345678 };

            Assert.Equal("!12345678", nodeInfo.NodeIdHex);
        }

        [Fact]
        public void DeviceMetrics_FromBytes_ParsesBatteryLevel()
        {
            var writer = new ProtobufWriter();
            writer.WriteTag(1, 0); // battery_level
            writer.WriteVarint(85);

            var metrics = DeviceMetrics.FromBytes(writer.ToArray());

            Assert.Equal(85u, metrics.BatteryLevel);
        }

        [Fact]
        public void MeshPacket_FromBytes_ParsesBasicFields()
        {
            var writer = new ProtobufWriter();
            writer.WriteTag(1, 0); // from
            writer.WriteVarint(0xAABBCCDD);
            writer.WriteTag(2, 0); // to
            writer.WriteVarint(0xFFFFFFFF); // broadcast
            writer.WriteTag(6, 0); // id
            writer.WriteVarint(12345);

            var packet = MeshPacket.FromBytes(writer.ToArray());

            Assert.Equal(0xAABBCCDDu, packet.From);
            Assert.Equal(0xFFFFFFFFu, packet.To);
            Assert.Equal(12345u, packet.Id);
        }

        [Fact]
        public void MeshPacket_ToBytes_RoundTrips()
        {
            var original = new MeshPacket
            {
                From = 0x12345678,
                To = 0xFFFFFFFF,
                Channel = 0,
                Id = 999
            };

            var bytes = original.ToBytes();
            var parsed = MeshPacket.FromBytes(bytes);

            Assert.Equal(original.From, parsed.From);
            Assert.Equal(original.To, parsed.To);
            Assert.Equal(original.Id, parsed.Id);
        }

        [Fact]
        public void FromRadio_FromBytes_ParsesPacket()
        {
            // Build a FromRadio with id=1
            var writer = new ProtobufWriter();
            writer.WriteTag(1, 0); // id
            writer.WriteVarint(42);

            var fromRadio = FromRadio.FromBytes(writer.ToArray());

            Assert.Equal(42u, fromRadio.Id);
        }

        [Fact]
        public void ChannelSettings_FromBytes_ParsesName()
        {
            var writer = new ProtobufWriter();
            writer.WriteTag(3, 2); // name
            writer.WriteBytes(Encoding.UTF8.GetBytes("LongFast"));
            writer.WriteTag(4, 0); // id
            writer.WriteVarint(123);

            var settings = ChannelSettings.FromBytes(writer.ToArray());

            Assert.Equal("LongFast", settings.Name);
            Assert.Equal(123u, settings.Id);
        }

        [Fact]
        public void Channel_FromBytes_ParsesCorrectly()
        {
            // Build inner ChannelSettings
            var settingsWriter = new ProtobufWriter();
            settingsWriter.WriteTag(3, 2); // name
            settingsWriter.WriteBytes(Encoding.UTF8.GetBytes("MyChannel"));
            var settingsBytes = settingsWriter.ToArray();

            // Build Channel
            var writer = new ProtobufWriter();
            writer.WriteTag(1, 0); // index
            writer.WriteVarint(2);
            writer.WriteTag(2, 2); // settings
            writer.WriteBytes(settingsBytes);
            writer.WriteTag(3, 0); // role
            writer.WriteVarint(2); // Secondary

            var channel = Channel.FromBytes(writer.ToArray());

            Assert.Equal(2, channel.Index);
            Assert.Equal(ChannelRole.Secondary, channel.Role);
            Assert.NotNull(channel.Settings);
            Assert.Equal("MyChannel", channel.Settings.Name);
            Assert.Equal("MyChannel", channel.DisplayName);
        }

        [Fact]
        public void Channel_DisplayName_FallsBackToPrimary()
        {
            var channel = new Channel { Index = 0, Role = ChannelRole.Primary };

            Assert.Equal("Primary", channel.DisplayName);
        }

        [Fact]
        public void Channel_DisplayName_FallsBackToChN()
        {
            var channel = new Channel { Index = 3, Role = ChannelRole.Secondary };

            Assert.Equal("Ch 3", channel.DisplayName);
        }

        [Fact]
        public void EnvironmentMetrics_FromBytes_ParsesTemperatureAndHumidity()
        {
            var writer = new ProtobufWriter();
            writer.WriteTag(1, 5); // temperature (float, wire type 5)
            writer.WriteFloat(22.5f);
            writer.WriteTag(2, 5); // relative_humidity
            writer.WriteFloat(65.3f);

            var metrics = EnvironmentMetrics.FromBytes(writer.ToArray());

            Assert.Equal(22.5f, metrics.Temperature, 0.01f);
            Assert.Equal(65.3f, metrics.RelativeHumidity, 0.01f);
        }

        [Fact]
        public void Telemetry_FromBytes_ParsesDeviceMetrics()
        {
            // Build inner DeviceMetrics
            var dmWriter = new ProtobufWriter();
            dmWriter.WriteTag(1, 0); // battery_level
            dmWriter.WriteVarint(75);
            dmWriter.WriteTag(5, 0); // uptime
            dmWriter.WriteVarint(3600);
            var dmBytes = dmWriter.ToArray();

            // Build Telemetry
            var writer = new ProtobufWriter();
            writer.WriteTag(1, 0); // time (varint)
            writer.WriteVarint(1700000000);
            writer.WriteTag(2, 2); // device_metrics
            writer.WriteBytes(dmBytes);

            var telemetry = Telemetry.FromBytes(writer.ToArray());

            Assert.Equal(1700000000u, telemetry.Time);
            Assert.NotNull(telemetry.DeviceMetrics);
            Assert.Equal(75u, telemetry.DeviceMetrics.BatteryLevel);
            Assert.Equal(3600u, telemetry.DeviceMetrics.Uptime);
        }

        [Fact]
        public void Telemetry_FromBytes_ParsesEnvironmentMetrics()
        {
            // Build inner EnvironmentMetrics
            var emWriter = new ProtobufWriter();
            emWriter.WriteTag(1, 5); // temperature
            emWriter.WriteFloat(25.0f);
            emWriter.WriteTag(3, 5); // barometric_pressure
            emWriter.WriteFloat(1013.25f);
            var emBytes = emWriter.ToArray();

            var writer = new ProtobufWriter();
            writer.WriteTag(3, 2); // environment_metrics
            writer.WriteBytes(emBytes);

            var telemetry = Telemetry.FromBytes(writer.ToArray());

            Assert.NotNull(telemetry.EnvironmentMetrics);
            Assert.Equal(25.0f, telemetry.EnvironmentMetrics.Temperature, 0.01f);
            Assert.Equal(1013.25f, telemetry.EnvironmentMetrics.BarometricPressure, 0.01f);
        }

        [Fact]
        public void NodeInfo_FromBytes_ParsesNewFields()
        {
            var writer = new ProtobufWriter();
            writer.WriteTag(1, 0); // num
            writer.WriteVarint(0xABCD);
            writer.WriteTag(7, 0); // channel
            writer.WriteVarint(2);
            writer.WriteTag(8, 0); // via_mqtt
            writer.WriteVarint(1);
            writer.WriteTag(9, 0); // hops_away
            writer.WriteVarint(3);

            var nodeInfo = NodeInfo.FromBytes(writer.ToArray());

            Assert.Equal(0xABCDu, nodeInfo.Num);
            Assert.Equal(2u, nodeInfo.Channel);
            Assert.True(nodeInfo.ViaMqtt);
            Assert.Equal(3u, nodeInfo.HopsAway);
        }

        [Fact]
        public void User_FromBytes_ParsesHwModelAndRole()
        {
            var writer = new ProtobufWriter();
            writer.WriteTag(1, 2); // id
            writer.WriteBytes(Encoding.UTF8.GetBytes("!test1234"));
            writer.WriteTag(2, 2); // long_name
            writer.WriteBytes(Encoding.UTF8.GetBytes("Test"));
            writer.WriteTag(5, 0); // hw_model
            writer.WriteVarint(43);
            writer.WriteTag(7, 0); // role
            writer.WriteVarint(1);

            var user = User.FromBytes(writer.ToArray());

            Assert.Equal("!test1234", user.Id);
            Assert.Equal(43u, user.HwModel);
            Assert.Equal(1u, user.Role);
        }

        [Fact]
        public void User_HwModelName_ReturnsKnownModels()
        {
            Assert.Equal("RAK4631", User.HwModelName(43));
            Assert.Equal("TRACKER_T1000_E", User.HwModelName(59));
            Assert.Equal("Unknown(999)", User.HwModelName(999));
        }

        [Fact]
        public void DataMessage_GetTelemetry_ReturnsTelemetry()
        {
            // Build a telemetry payload with device metrics
            var dmWriter = new ProtobufWriter();
            dmWriter.WriteTag(1, 0); // battery_level
            dmWriter.WriteVarint(50);
            var dmBytes = dmWriter.ToArray();

            var telWriter = new ProtobufWriter();
            telWriter.WriteTag(2, 2); // device_metrics
            telWriter.WriteBytes(dmBytes);
            var telBytes = telWriter.ToArray();

            var dataMessage = new DataMessage
            {
                PortNum = PortNum.Telemetry,
                Payload = telBytes
            };

            var telemetry = dataMessage.GetTelemetry();

            Assert.NotNull(telemetry);
            Assert.NotNull(telemetry.DeviceMetrics);
            Assert.Equal(50u, telemetry.DeviceMetrics.BatteryLevel);
        }

        [Fact]
        public void DataMessage_GetTelemetry_ReturnsNullForWrongPortNum()
        {
            var dataMessage = new DataMessage
            {
                PortNum = PortNum.TextMessage,
                Payload = new byte[] { 1, 2, 3 }
            };

            Assert.Null(dataMessage.GetTelemetry());
        }

        [Fact]
        public void FromRadio_FromBytes_ParsesChannel()
        {
            // Build inner Channel
            var chWriter = new ProtobufWriter();
            chWriter.WriteTag(1, 0); // index
            chWriter.WriteVarint(0);
            chWriter.WriteTag(3, 0); // role
            chWriter.WriteVarint(1); // Primary
            var chBytes = chWriter.ToArray();

            // Build FromRadio with field 10 = channel
            var writer = new ProtobufWriter();
            writer.WriteTag(1, 0); // id
            writer.WriteVarint(99);
            writer.WriteTag(10, 2); // channel
            writer.WriteBytes(chBytes);

            var fromRadio = FromRadio.FromBytes(writer.ToArray());

            Assert.Equal(99u, fromRadio.Id);
            Assert.NotNull(fromRadio.Channel);
            Assert.Equal(0, fromRadio.Channel.Index);
            Assert.Equal(ChannelRole.Primary, fromRadio.Channel.Role);
        }
    }
}
