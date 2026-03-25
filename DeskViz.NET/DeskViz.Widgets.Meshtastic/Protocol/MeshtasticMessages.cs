using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeskViz.Widgets.Meshtastic.Protocol
{
    /// <summary>
    /// Port numbers that identify the type of payload in a Data message
    /// </summary>
    public enum PortNum
    {
        Unknown = 0,
        TextMessage = 1,
        RemoteHardware = 2,
        Position = 3,
        NodeInfo = 4,
        Routing = 5,
        Admin = 6,
        TextMessageCompressed = 7,
        Waypoint = 8,
        Audio = 9,
        DetectionSensor = 10,
        Reply = 32,
        IpTunnel = 33,
        Paxcounter = 34,
        Serial = 64,
        StoreForward = 65,
        RangeTest = 66,
        Telemetry = 67,
        Zps = 68,
        Simulator = 69,
        TracerRoute = 70,
        NeighborInfo = 71,
        Atak = 72,
        Map = 73,
        PowerStress = 74,
        PrivateApp = 256,
        AtakForwarder = 257,
        Max = 511
    }

    public enum ChannelRole
    {
        Disabled = 0,
        Primary = 1,
        Secondary = 2
    }

    /// <summary>
    /// Simple protobuf reader for Meshtastic messages
    /// </summary>
    public class ProtobufReader
    {
        private readonly byte[] _data;
        private int _position;

        public ProtobufReader(byte[] data)
        {
            _data = data;
            _position = 0;
        }

        public bool HasMore => _position < _data.Length;

        public (int fieldNumber, int wireType) ReadTag()
        {
            var tag = ReadVarint();
            return ((int)(tag >> 3), (int)(tag & 0x7));
        }

        public uint ReadVarint()
        {
            uint result = 0;
            int shift = 0;
            while (_position < _data.Length)
            {
                byte b = _data[_position++];
                result |= (uint)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) break;
                shift += 7;
            }
            return result;
        }

        public int ReadSignedVarint()
        {
            var v = ReadVarint();
            return (int)((v >> 1) ^ -(v & 1));
        }

        public uint ReadFixed32()
        {
            if (_position + 4 > _data.Length) return 0;
            var result = BitConverter.ToUInt32(_data, _position);
            _position += 4;
            return result;
        }

        public int ReadSFixed32()
        {
            if (_position + 4 > _data.Length) return 0;
            var result = BitConverter.ToInt32(_data, _position);
            _position += 4;
            return result;
        }

        public float ReadFloat()
        {
            if (_position + 4 > _data.Length) return 0;
            var result = BitConverter.ToSingle(_data, _position);
            _position += 4;
            return result;
        }

        public byte[] ReadBytes()
        {
            var len = (int)ReadVarint();
            if (_position + len > _data.Length) return Array.Empty<byte>();
            var result = new byte[len];
            Array.Copy(_data, _position, result, 0, len);
            _position += len;
            return result;
        }

        public string ReadString() => Encoding.UTF8.GetString(ReadBytes());

        public void Skip(int wireType)
        {
            switch (wireType)
            {
                case 0: ReadVarint(); break;
                case 1: _position += 8; break;
                case 2: var len = (int)ReadVarint(); _position += len; break;
                case 5: _position += 4; break;
            }
        }
    }

    /// <summary>
    /// Simple protobuf writer for Meshtastic messages
    /// </summary>
    public class ProtobufWriter
    {
        private readonly MemoryStream _stream = new();

        public void WriteTag(int fieldNumber, int wireType)
        {
            WriteVarint((uint)((fieldNumber << 3) | wireType));
        }

        public void WriteVarint(uint value)
        {
            while (value > 0x7F)
            {
                _stream.WriteByte((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }
            _stream.WriteByte((byte)value);
        }

        public void WriteBytes(byte[] data)
        {
            WriteVarint((uint)data.Length);
            _stream.Write(data, 0, data.Length);
        }

        public void WriteFixed32(uint value)
        {
            var bytes = BitConverter.GetBytes(value);
            _stream.Write(bytes, 0, 4);
        }

        public void WriteFloat(float value)
        {
            var bytes = BitConverter.GetBytes(value);
            _stream.Write(bytes, 0, 4);
        }

        public void WriteBool(bool value)
        {
            _stream.WriteByte((byte)(value ? 1 : 0));
        }

        public byte[] ToArray() => _stream.ToArray();
    }

    /// <summary>
    /// Simplified FromRadio message (device to app)
    /// </summary>
    public class FromRadio
    {
        public uint Id { get; set; }
        public MeshPacket? Packet { get; set; }
        public MyNodeInfo? MyInfo { get; set; }
        public NodeInfo? NodeInfo { get; set; }
        public Channel? Channel { get; set; }
        public DeviceMetadata? Metadata { get; set; }
        public bool ConfigComplete { get; set; }
        public bool Rebooted { get; set; }

        public static FromRadio FromBytes(byte[] data)
        {
            var result = new FromRadio();
            var reader = new ProtobufReader(data);

            while (reader.HasMore)
            {
                var (fieldNumber, wireType) = reader.ReadTag();

                switch (fieldNumber)
                {
                    case 1: result.Id = reader.ReadVarint(); break;
                    case 2: result.Packet = MeshPacket.FromBytes(reader.ReadBytes()); break;
                    case 3: result.MyInfo = MyNodeInfo.FromBytes(reader.ReadBytes()); break;
                    case 4: result.NodeInfo = NodeInfo.FromBytes(reader.ReadBytes()); break;
                    case 7: reader.ReadVarint(); result.ConfigComplete = true; break;
                    case 8: result.Rebooted = reader.ReadVarint() != 0; break;
                    case 10: result.Channel = Channel.FromBytes(reader.ReadBytes()); break;
                    case 13: result.Metadata = DeviceMetadata.FromBytes(reader.ReadBytes()); break;
                    default: reader.Skip(wireType); break;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Simplified ToRadio message (app to device)
    /// </summary>
    public class ToRadio
    {
        public MeshPacket? Packet { get; set; }
        public uint WantConfigId { get; set; }
        public bool Disconnect { get; set; }

        public byte[] ToBytes()
        {
            var writer = new ProtobufWriter();

            if (Packet != null)
            {
                writer.WriteTag(1, 2); // LengthDelimited
                writer.WriteBytes(Packet.ToBytes());
            }

            if (WantConfigId > 0)
            {
                writer.WriteTag(3, 0); // Varint
                writer.WriteVarint(WantConfigId);
            }

            if (Disconnect)
            {
                writer.WriteTag(4, 0); // Varint
                writer.WriteBool(true);
            }

            return writer.ToArray();
        }
    }

    /// <summary>
    /// A mesh packet containing either decoded data or encrypted bytes
    /// </summary>
    public class MeshPacket
    {
        public uint From { get; set; }
        public uint To { get; set; }
        public uint Channel { get; set; }
        public DataMessage? Decoded { get; set; }
        public byte[]? Encrypted { get; set; }
        public uint Id { get; set; }
        public uint RxTime { get; set; }
        public float RxSnr { get; set; }
        public int RxRssi { get; set; }
        public uint HopLimit { get; set; }
        public uint HopStart { get; set; }

        public static MeshPacket FromBytes(byte[] data)
        {
            var result = new MeshPacket();
            var reader = new ProtobufReader(data);

            while (reader.HasMore)
            {
                var (fieldNumber, wireType) = reader.ReadTag();

                switch (fieldNumber)
                {
                    case 1: result.From = reader.ReadVarint(); break;
                    case 2: result.To = reader.ReadVarint(); break;
                    case 3: result.Channel = reader.ReadVarint(); break;
                    case 4: result.Decoded = DataMessage.FromBytes(reader.ReadBytes()); break;
                    case 5: result.Encrypted = reader.ReadBytes(); break;
                    case 6: result.Id = reader.ReadVarint(); break;
                    case 7: result.RxTime = reader.ReadVarint(); break;
                    case 8: result.RxSnr = reader.ReadFloat(); break;
                    case 9: result.HopLimit = reader.ReadVarint(); break;
                    case 14: result.RxRssi = (int)reader.ReadVarint(); break;
                    case 15: result.HopStart = reader.ReadVarint(); break;
                    default: reader.Skip(wireType); break;
                }
            }

            return result;
        }

        public byte[] ToBytes()
        {
            var writer = new ProtobufWriter();

            if (From > 0) { writer.WriteTag(1, 0); writer.WriteVarint(From); }
            if (To > 0) { writer.WriteTag(2, 0); writer.WriteVarint(To); }
            if (Channel > 0) { writer.WriteTag(3, 0); writer.WriteVarint(Channel); }
            if (Decoded != null) { writer.WriteTag(4, 2); writer.WriteBytes(Decoded.ToBytes()); }
            if (Id > 0) { writer.WriteTag(6, 0); writer.WriteVarint(Id); }

            return writer.ToArray();
        }
    }

    /// <summary>
    /// Data payload inside a MeshPacket
    /// </summary>
    public class DataMessage
    {
        public PortNum PortNum { get; set; }
        public byte[] Payload { get; set; } = Array.Empty<byte>();
        public bool WantResponse { get; set; }
        public uint Dest { get; set; }
        public uint Source { get; set; }
        public uint RequestId { get; set; }

        public string GetTextMessage() => Encoding.UTF8.GetString(Payload);

        public Position? GetPosition()
        {
            if (PortNum != PortNum.Position || Payload.Length == 0) return null;
            return Position.FromBytes(Payload);
        }

        public Telemetry? GetTelemetry()
        {
            if (PortNum != PortNum.Telemetry || Payload.Length == 0) return null;
            return Telemetry.FromBytes(Payload);
        }

        public static DataMessage FromBytes(byte[] data)
        {
            var result = new DataMessage();
            var reader = new ProtobufReader(data);

            while (reader.HasMore)
            {
                var (fieldNumber, wireType) = reader.ReadTag();

                switch (fieldNumber)
                {
                    case 1: result.PortNum = (PortNum)reader.ReadVarint(); break;
                    case 2: result.Payload = reader.ReadBytes(); break;
                    case 3: result.WantResponse = reader.ReadVarint() != 0; break;
                    case 4: result.Dest = reader.ReadFixed32(); break;
                    case 5: result.Source = reader.ReadFixed32(); break;
                    case 6: result.RequestId = reader.ReadFixed32(); break;
                    default: reader.Skip(wireType); break;
                }
            }

            return result;
        }

        public byte[] ToBytes()
        {
            var writer = new ProtobufWriter();
            writer.WriteTag(1, 0);
            writer.WriteVarint((uint)PortNum);
            if (Payload.Length > 0) { writer.WriteTag(2, 2); writer.WriteBytes(Payload); }
            return writer.ToArray();
        }
    }

    /// <summary>
    /// Position information
    /// </summary>
    public class Position
    {
        public int LatitudeI { get; set; }
        public int LongitudeI { get; set; }
        public int Altitude { get; set; }
        public uint Time { get; set; }
        public uint Sats { get; set; }
        public uint PrecisionBits { get; set; }
        public int GroundSpeed { get; set; }
        public int GroundTrack { get; set; }

        public double Latitude => LatitudeI / 1e7;
        public double Longitude => LongitudeI / 1e7;
        public bool HasValidFix => LatitudeI != 0 || LongitudeI != 0;

        public static Position FromBytes(byte[] data)
        {
            var result = new Position();
            var reader = new ProtobufReader(data);

            while (reader.HasMore)
            {
                var (fieldNumber, wireType) = reader.ReadTag();

                switch (fieldNumber)
                {
                    case 1: result.LatitudeI = reader.ReadSFixed32(); break;
                    case 2: result.LongitudeI = reader.ReadSFixed32(); break;
                    case 3: result.Altitude = (int)reader.ReadVarint(); break;
                    case 4: result.Time = reader.ReadFixed32(); break;
                    case 13: result.Sats = reader.ReadVarint(); break;
                    case 14: result.PrecisionBits = reader.ReadVarint(); break;
                    case 15: result.GroundSpeed = (int)reader.ReadVarint(); break;
                    case 16: result.GroundTrack = (int)reader.ReadVarint(); break;
                    default: reader.Skip(wireType); break;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Information about this node (updated for current Meshtastic proto)
    /// </summary>
    public class MyNodeInfo
    {
        public uint MyNodeNum { get; set; }
        public uint RebootCount { get; set; }
        public uint MinAppVersion { get; set; }
        public byte[] DeviceId { get; set; } = Array.Empty<byte>();
        public string PioEnv { get; set; } = "";
        public uint NodeDbCount { get; set; }

        // For backward compatibility - firmware version is now in DeviceMetadata
        public string FirmwareVersion { get; set; } = "";

        public static MyNodeInfo FromBytes(byte[] data)
        {
            var result = new MyNodeInfo();
            var reader = new ProtobufReader(data);

            while (reader.HasMore)
            {
                var (fieldNumber, wireType) = reader.ReadTag();

                switch (fieldNumber)
                {
                    case 1: result.MyNodeNum = reader.ReadVarint(); break;
                    case 2: result.RebootCount = reader.ReadVarint(); break;
                    case 3: result.MinAppVersion = reader.ReadVarint(); break;
                    case 4: result.DeviceId = reader.ReadBytes(); break;
                    case 5: result.PioEnv = reader.ReadString(); break;
                    case 7: result.NodeDbCount = reader.ReadVarint(); break;
                    default: reader.Skip(wireType); break;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Information about a node in the mesh
    /// </summary>
    public class NodeInfo
    {
        public uint Num { get; set; }
        public User? User { get; set; }
        public Position? Position { get; set; }
        public float Snr { get; set; }
        public uint LastHeard { get; set; }
        public DeviceMetrics? DeviceMetrics { get; set; }
        public uint Channel { get; set; }
        public bool ViaMqtt { get; set; }
        public uint HopsAway { get; set; }

        public string NodeIdHex => $"!{Num:x8}";

        public static NodeInfo FromBytes(byte[] data)
        {
            var result = new NodeInfo();
            var reader = new ProtobufReader(data);

            while (reader.HasMore)
            {
                var (fieldNumber, wireType) = reader.ReadTag();

                switch (fieldNumber)
                {
                    case 1: result.Num = reader.ReadVarint(); break;
                    case 2: result.User = User.FromBytes(reader.ReadBytes()); break;
                    case 3: result.Position = Position.FromBytes(reader.ReadBytes()); break;
                    case 4: result.Snr = reader.ReadFloat(); break;
                    case 5: result.LastHeard = reader.ReadFixed32(); break;
                    case 6: result.DeviceMetrics = DeviceMetrics.FromBytes(reader.ReadBytes()); break;
                    case 7: result.Channel = reader.ReadVarint(); break;
                    case 8: result.ViaMqtt = reader.ReadVarint() != 0; break;
                    case 9: result.HopsAway = reader.ReadVarint(); break;
                    default: reader.Skip(wireType); break;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// User information for a node
    /// </summary>
    public class User
    {
        public string Id { get; set; } = "";
        public string LongName { get; set; } = "";
        public string ShortName { get; set; } = "";
        public uint HwModel { get; set; }
        public uint Role { get; set; }

        public static string HwModelName(uint model) => model switch
        {
            1 => "TLORA_V2",
            2 => "TLORA_V1",
            3 => "TLORA_V2_1_1P6",
            4 => "TBEAM",
            5 => "HELTEC_V2_0",
            6 => "TBEAM_V0P7",
            7 => "T_ECHO",
            8 => "TLORA_V1_1P3",
            9 => "RAK4631",
            10 => "HELTEC_V2_1",
            11 => "HELTEC_V1",
            25 => "RAK11200",
            26 => "NANO_G1",
            29 => "STATION_G1",
            39 => "RAK11310",
            41 => "HELTEC_V3",
            42 => "HELTEC_WSL_V3",
            43 => "RAK4631",
            47 => "TBEAM_S3_CORE",
            58 => "HELTEC_V3",
            59 => "TRACKER_T1000_E",
            _ => $"Unknown({model})"
        };

        public static User FromBytes(byte[] data)
        {
            var result = new User();
            var reader = new ProtobufReader(data);

            while (reader.HasMore)
            {
                var (fieldNumber, wireType) = reader.ReadTag();

                switch (fieldNumber)
                {
                    case 1: result.Id = reader.ReadString(); break;
                    case 2: result.LongName = reader.ReadString(); break;
                    case 3: result.ShortName = reader.ReadString(); break;
                    case 5: result.HwModel = reader.ReadVarint(); break;
                    case 7: result.Role = reader.ReadVarint(); break;
                    default: reader.Skip(wireType); break;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Device metrics (battery, voltage, etc.) - from telemetry
    /// </summary>
    public class DeviceMetrics
    {
        public uint BatteryLevel { get; set; }
        public float Voltage { get; set; }
        public float ChannelUtilization { get; set; }
        public float AirUtilTx { get; set; }
        public uint Uptime { get; set; }

        public static DeviceMetrics FromBytes(byte[] data)
        {
            var result = new DeviceMetrics();
            var reader = new ProtobufReader(data);

            while (reader.HasMore)
            {
                var (fieldNumber, wireType) = reader.ReadTag();

                switch (fieldNumber)
                {
                    case 1: result.BatteryLevel = reader.ReadVarint(); break;
                    case 2: result.Voltage = reader.ReadFloat(); break;
                    case 3: result.ChannelUtilization = reader.ReadFloat(); break;
                    case 4: result.AirUtilTx = reader.ReadFloat(); break;
                    case 5: result.Uptime = reader.ReadVarint(); break;
                    default: reader.Skip(wireType); break;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Settings for a single channel
    /// </summary>
    public class ChannelSettings
    {
        public byte[] Psk { get; set; } = Array.Empty<byte>();
        public string Name { get; set; } = "";
        public uint Id { get; set; }
        public bool UplinkEnabled { get; set; }
        public bool DownlinkEnabled { get; set; }

        public static ChannelSettings FromBytes(byte[] data)
        {
            var result = new ChannelSettings();
            var reader = new ProtobufReader(data);

            while (reader.HasMore)
            {
                var (fieldNumber, wireType) = reader.ReadTag();

                switch (fieldNumber)
                {
                    case 2: result.Psk = reader.ReadBytes(); break;
                    case 3: result.Name = reader.ReadString(); break;
                    case 4: result.Id = reader.ReadVarint(); break;
                    case 5: result.UplinkEnabled = reader.ReadVarint() != 0; break;
                    case 6: result.DownlinkEnabled = reader.ReadVarint() != 0; break;
                    default: reader.Skip(wireType); break;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// A channel definition from the device config
    /// </summary>
    public class Channel
    {
        public int Index { get; set; }
        public ChannelSettings? Settings { get; set; }
        public ChannelRole Role { get; set; }

        public string DisplayName
        {
            get
            {
                if (Settings != null && !string.IsNullOrEmpty(Settings.Name))
                    return Settings.Name;
                return Role == ChannelRole.Primary ? "Primary" : $"Ch {Index}";
            }
        }

        public static Channel FromBytes(byte[] data)
        {
            var result = new Channel();
            var reader = new ProtobufReader(data);

            while (reader.HasMore)
            {
                var (fieldNumber, wireType) = reader.ReadTag();

                switch (fieldNumber)
                {
                    case 1: result.Index = (int)reader.ReadVarint(); break;
                    case 2: result.Settings = ChannelSettings.FromBytes(reader.ReadBytes()); break;
                    case 3: result.Role = (ChannelRole)reader.ReadVarint(); break;
                    default: reader.Skip(wireType); break;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Environment sensor metrics (temperature, humidity, pressure)
    /// </summary>
    public class EnvironmentMetrics
    {
        public float Temperature { get; set; }
        public float RelativeHumidity { get; set; }
        public float BarometricPressure { get; set; }
        public float GasResistance { get; set; }
        public float Voltage { get; set; }
        public float Current { get; set; }

        public static EnvironmentMetrics FromBytes(byte[] data)
        {
            var result = new EnvironmentMetrics();
            var reader = new ProtobufReader(data);

            while (reader.HasMore)
            {
                var (fieldNumber, wireType) = reader.ReadTag();

                switch (fieldNumber)
                {
                    case 1: result.Temperature = reader.ReadFloat(); break;
                    case 2: result.RelativeHumidity = reader.ReadFloat(); break;
                    case 3: result.BarometricPressure = reader.ReadFloat(); break;
                    case 4: result.GasResistance = reader.ReadFloat(); break;
                    case 5: result.Voltage = reader.ReadFloat(); break;
                    case 6: result.Current = reader.ReadFloat(); break;
                    default: reader.Skip(wireType); break;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Telemetry container with device and environment metrics
    /// </summary>
    public class Telemetry
    {
        public uint Time { get; set; }
        public DeviceMetrics? DeviceMetrics { get; set; }
        public EnvironmentMetrics? EnvironmentMetrics { get; set; }

        public static Telemetry FromBytes(byte[] data)
        {
            var result = new Telemetry();
            var reader = new ProtobufReader(data);

            while (reader.HasMore)
            {
                var (fieldNumber, wireType) = reader.ReadTag();

                switch (fieldNumber)
                {
                    case 1:
                        // time can be varint or fixed32 depending on encoding
                        if (wireType == 5)
                            result.Time = reader.ReadFixed32();
                        else
                            result.Time = reader.ReadVarint();
                        break;
                    case 2: result.DeviceMetrics = DeviceMetrics.FromBytes(reader.ReadBytes()); break;
                    case 3: result.EnvironmentMetrics = EnvironmentMetrics.FromBytes(reader.ReadBytes()); break;
                    default: reader.Skip(wireType); break;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Device metadata returned during config (firmware version, capabilities, etc.)
    /// </summary>
    public class DeviceMetadata
    {
        public string FirmwareVersion { get; set; } = "";
        public uint DeviceStateVersion { get; set; }
        public bool CanShutdown { get; set; }
        public bool HasWifi { get; set; }
        public bool HasBluetooth { get; set; }
        public bool HasEthernet { get; set; }
        public uint Role { get; set; }
        public uint PositionFlags { get; set; }
        public uint HwModel { get; set; }
        public bool HasRemoteHardware { get; set; }
        public bool HasPkc { get; set; }

        public static DeviceMetadata FromBytes(byte[] data)
        {
            var result = new DeviceMetadata();
            var reader = new ProtobufReader(data);

            while (reader.HasMore)
            {
                var (fieldNumber, wireType) = reader.ReadTag();

                switch (fieldNumber)
                {
                    case 1: result.FirmwareVersion = reader.ReadString(); break;
                    case 2: result.DeviceStateVersion = reader.ReadVarint(); break;
                    case 3: result.CanShutdown = reader.ReadVarint() != 0; break;
                    case 4: result.HasWifi = reader.ReadVarint() != 0; break;
                    case 5: result.HasBluetooth = reader.ReadVarint() != 0; break;
                    case 6: result.HasEthernet = reader.ReadVarint() != 0; break;
                    case 7: result.Role = reader.ReadVarint(); break;
                    case 8: result.PositionFlags = reader.ReadVarint(); break;
                    case 9: result.HwModel = reader.ReadVarint(); break;
                    case 10: result.HasRemoteHardware = reader.ReadVarint() != 0; break;
                    case 11: result.HasPkc = reader.ReadVarint() != 0; break;
                    default: reader.Skip(wireType); break;
                }
            }

            return result;
        }
    }
}
