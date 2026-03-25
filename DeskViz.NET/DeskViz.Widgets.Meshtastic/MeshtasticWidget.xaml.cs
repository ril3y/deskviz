using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DeskViz.Plugins.Base;
using DeskViz.Plugins.Interfaces;
using DeskViz.Widgets.Meshtastic.Protocol;

namespace DeskViz.Widgets.Meshtastic
{
    public partial class MeshtasticWidget : BaseWidget
    {
        private MeshtasticWidgetSettings _settings = new();
        private MeshtasticSerial? _serial;
        private readonly Dictionary<uint, NodeInfo> _nodes = new();
        private readonly Dictionary<int, Channel> _channels = new();
        private readonly Dictionary<uint, Telemetry> _nodeTelemetry = new();

        public override IWidgetMetadata Metadata { get; } = new WidgetMetadata
        {
            Id = "MeshtasticWidget",
            Name = "Meshtastic",
            Description = "Displays messages and status from a Meshtastic mesh network device",
            Author = "DeskViz Team",
            Version = new Version(1, 0, 0),
            Category = "Communication",
            Tags = new[] { "meshtastic", "mesh", "radio", "lora", "messages", "gps" },
            RequiresElevatedPermissions = false,
            MinimumHostVersion = new Version(1, 0, 0)
        };

        public override string WidgetId => "MeshtasticWidget";
        public override string DisplayName => "Meshtastic";

        // Observable collection for messages
        public ObservableCollection<MeshMessage> Messages { get; } = new();

        // Display-limited view of messages
        public List<MeshMessage> DisplayMessages => Messages.Take(_settings.DisplayMessageCount).ToList();

        // Connection properties
        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ConnectionColor));
                    OnPropertyChanged(nameof(ConnectionStatusText));
                    OnPropertyChanged(nameof(NoConnectionVisibility));
                    OnPropertyChanged(nameof(ConnectedContentVisibility));
                }
            }
        }

        public Brush ConnectionColor => IsConnected
            ? new SolidColorBrush(Color.FromRgb(34, 197, 94))  // Green
            : new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red

        public string ConnectionStatusText => IsConnected ? "Connected" : "Disconnected";
        public Visibility NoConnectionVisibility => IsConnected ? Visibility.Collapsed : Visibility.Visible;
        public Visibility ConnectedContentVisibility => IsConnected ? Visibility.Visible : Visibility.Collapsed;
        public Visibility GpsInfoVisibility => _settings.ShowGpsInfo ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ChannelsVisibility => _settings.ShowChannels ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TelemetryVisibility => _settings.ShowTelemetry ? Visibility.Visible : Visibility.Collapsed;
        public Visibility NodeListVisibility => _settings.ShowNodeList ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MessagesVisibility => _settings.ShowMessages ? Visibility.Visible : Visibility.Collapsed;
        public Visibility NoChatMessagesVisibility => Messages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        // Channel items
        public ObservableCollection<ChannelDisplayItem> ChannelItems { get; } = new();

        // Node list items
        public ObservableCollection<NodeDisplayItem> NodeItems { get; } = new();

        // Telemetry display strings
        private string _temperature = "N/A";
        public string Temperature
        {
            get => _temperature;
            private set { if (_temperature != value) { _temperature = value; OnPropertyChanged(); } }
        }

        private string _humidity = "N/A";
        public string Humidity
        {
            get => _humidity;
            private set { if (_humidity != value) { _humidity = value; OnPropertyChanged(); } }
        }

        private string _pressure = "N/A";
        public string Pressure
        {
            get => _pressure;
            private set { if (_pressure != value) { _pressure = value; OnPropertyChanged(); } }
        }

        private string _channelUtilization = "---";
        public string ChannelUtilization
        {
            get => _channelUtilization;
            private set { if (_channelUtilization != value) { _channelUtilization = value; OnPropertyChanged(); } }
        }

        private string _airUtilTx = "---";
        public string AirUtilTx
        {
            get => _airUtilTx;
            private set { if (_airUtilTx != value) { _airUtilTx = value; OnPropertyChanged(); } }
        }

        private string _uptime = "---";
        public string Uptime
        {
            get => _uptime;
            private set { if (_uptime != value) { _uptime = value; OnPropertyChanged(); } }
        }

        private string _voltage = "---";
        public string Voltage
        {
            get => _voltage;
            private set { if (_voltage != value) { _voltage = value; OnPropertyChanged(); } }
        }

        // GPS properties
        private uint _gpsSatellites;
        public uint GpsSatellites
        {
            get => _gpsSatellites;
            private set { if (_gpsSatellites != value) { _gpsSatellites = value; OnPropertyChanged(); } }
        }

        private double _latitude;
        public double Latitude
        {
            get => _latitude;
            private set { if (Math.Abs(_latitude - value) > 0.000001) { _latitude = value; OnPropertyChanged(); } }
        }

        private double _longitude;
        public double Longitude
        {
            get => _longitude;
            private set { if (Math.Abs(_longitude - value) > 0.000001) { _longitude = value; OnPropertyChanged(); } }
        }

        private int _altitude;
        public int Altitude
        {
            get => _altitude;
            private set { if (_altitude != value) { _altitude = value; OnPropertyChanged(); } }
        }

        // Node properties
        private string _myNodeId = "---";
        public string MyNodeId
        {
            get => _myNodeId;
            private set { if (_myNodeId != value) { _myNodeId = value; OnPropertyChanged(); } }
        }

        private int _connectedNodes;
        public int ConnectedNodes
        {
            get => _connectedNodes;
            private set { if (_connectedNodes != value) { _connectedNodes = value; OnPropertyChanged(); } }
        }

        private string _firmwareVersion = "---";
        public string FirmwareVersion
        {
            get => _firmwareVersion;
            private set { if (_firmwareVersion != value) { _firmwareVersion = value; OnPropertyChanged(); } }
        }

        private string _batteryLevel = "---";
        public string BatteryLevel
        {
            get => _batteryLevel;
            private set { if (_batteryLevel != value) { _batteryLevel = value; OnPropertyChanged(); OnPropertyChanged(nameof(BatteryColor)); } }
        }

        public Brush BatteryColor
        {
            get
            {
                if (_batteryLevel == "---") return Brushes.Gray;
                if (int.TryParse(_batteryLevel.Replace("%", ""), out int level))
                {
                    if (level > 50) return new SolidColorBrush(Color.FromRgb(34, 197, 94));  // Green
                    if (level > 20) return new SolidColorBrush(Color.FromRgb(234, 179, 8)); // Yellow
                    return new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                }
                return Brushes.White;
            }
        }

        public int MessageCount => Messages.Count;

        public MeshtasticWidget()
        {
            try
            {
                InitializeComponent();
                DataContext = this;
                Console.WriteLine("Meshtastic Widget XAML initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Meshtastic Widget XAML initialization failed: {ex.Message}");
            }
        }

        protected override void InitializeWidget()
        {
            LoadSettings();
            Console.WriteLine($"Meshtastic settings loaded: ComPort={_settings.ComPort}, AutoConnect={_settings.AutoConnect}, BaudRate={_settings.BaudRate}");
            StartUpdateTimer(_settings.UpdateIntervalSeconds);

            if (_settings.AutoConnect && !string.IsNullOrEmpty(_settings.ComPort))
            {
                _ = ConnectAsync();
            }
        }

        protected override void ShutdownWidget()
        {
            StopUpdateTimer();
            Disconnect();
        }

        public override void RefreshData()
        {
            ConnectedNodes = _nodes.Count;
            OnPropertyChanged(nameof(MessageCount));
            OnPropertyChanged(nameof(NoChatMessagesVisibility));
            UpdateNodeListDisplay();
        }

        public override FrameworkElement? CreateSettingsUI()
        {
            var settingsClone = _settings.Clone() as MeshtasticWidgetSettings ?? new();
            var settingsView = new MeshtasticWidgetSettingsView(settingsClone, this);
            return settingsView;
        }

        private void LoadSettings()
        {
            var loadedSettings = LoadPageSettings<MeshtasticWidgetSettings>();
            if (loadedSettings != null)
            {
                _settings = loadedSettings;
            }
            NotifySectionVisibilities();
        }

        private void NotifySectionVisibilities()
        {
            OnPropertyChanged(nameof(GpsInfoVisibility));
            OnPropertyChanged(nameof(ChannelsVisibility));
            OnPropertyChanged(nameof(TelemetryVisibility));
            OnPropertyChanged(nameof(NodeListVisibility));
            OnPropertyChanged(nameof(MessagesVisibility));
        }

        public void ApplySettings(MeshtasticWidgetSettings newSettings)
        {
            var reconnect = _settings.ComPort != newSettings.ComPort ||
                           _settings.BaudRate != newSettings.BaudRate;

            _settings = newSettings;
            SavePageSettings(_settings);
            SaveSettings(_settings);

            NotifySectionVisibilities();
            OnPropertyChanged(nameof(DisplayMessages));

            if (reconnect && IsConnected)
            {
                Disconnect();
            }

            if (_settings.AutoConnect && !string.IsNullOrEmpty(_settings.ComPort) && !IsConnected)
            {
                _ = ConnectAsync();
            }
        }

        public async Task ConnectAsync()
        {
            if (string.IsNullOrEmpty(_settings.ComPort))
            {
                Log("No COM port configured", LogLevel.Warning);
                return;
            }

            // Clean up any existing connection first to release the COM port
            Disconnect();

            try
            {
                _serial = new MeshtasticSerial();
                _serial.PacketReceived += OnPacketReceived;
                _serial.DebugOutput += OnDebugOutput;
                _serial.ErrorOccurred += OnError;
                _serial.ConnectionStateChanged += OnConnectionStateChanged;

                var success = await _serial.ConnectAsync(_settings.ComPort, _settings.BaudRate);
                if (success)
                {
                    Log($"Connected to {_settings.ComPort}", LogLevel.Info);

                    // DTR/RTS causes device to reboot - wait for it to boot before requesting config
                    await Task.Delay(2000);
                    await _serial.RequestConfigAsync();
                    Log("Requested device configuration", LogLevel.Info);
                }
                else
                {
                    Log($"Failed to connect to {_settings.ComPort}", LogLevel.Error);
                }
            }
            catch (Exception ex)
            {
                Log($"Connection error: {ex.Message}", LogLevel.Error);
            }
        }

        public void Disconnect()
        {
            if (_serial != null)
            {
                _serial.PacketReceived -= OnPacketReceived;
                _serial.DebugOutput -= OnDebugOutput;
                _serial.ErrorOccurred -= OnError;
                _serial.ConnectionStateChanged -= OnConnectionStateChanged;
                _serial.Dispose();
                _serial = null;
            }
            IsConnected = false;
        }

        private void OnConnectionStateChanged(object? sender, bool connected)
        {
            Dispatcher.Invoke(() =>
            {
                IsConnected = connected;
            });
        }

        private void OnPacketReceived(object? sender, FromRadioEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    ProcessFromRadio(e.Packet);
                }
                catch (Exception ex)
                {
                    Log($"Error processing packet: {ex.Message}", LogLevel.Error);
                }
            });
        }

        private void ProcessFromRadio(FromRadio fromRadio)
        {
            // Handle MyNodeInfo
            if (fromRadio.MyInfo != null)
            {
                MyNodeId = $"!{fromRadio.MyInfo.MyNodeNum:x8}";
                Log($"My node: {MyNodeId}", LogLevel.Info);
            }

            // Handle DeviceMetadata (firmware version, hardware model)
            if (fromRadio.Metadata != null)
            {
                FirmwareVersion = fromRadio.Metadata.FirmwareVersion;
                Log($"Device firmware: {FirmwareVersion}, HwModel: {fromRadio.Metadata.HwModel}", LogLevel.Info);
            }

            // Handle NodeInfo
            if (fromRadio.NodeInfo != null)
            {
                var node = fromRadio.NodeInfo;
                _nodes[node.Num] = node;
                ConnectedNodes = _nodes.Count;

                // Update position if this is our node
                if (node.Position != null && node.Position.HasValidFix)
                {
                    Latitude = node.Position.Latitude;
                    Longitude = node.Position.Longitude;
                    Altitude = node.Position.Altitude;
                    GpsSatellites = node.Position.Sats;
                }

                // Update battery if available
                if (node.DeviceMetrics != null)
                {
                    BatteryLevel = $"{node.DeviceMetrics.BatteryLevel}%";
                }

                Log($"Node info: {node.User?.LongName ?? node.NodeIdHex}", LogLevel.Info);
            }

            // Handle Channel
            if (fromRadio.Channel != null && fromRadio.Channel.Role != ChannelRole.Disabled)
            {
                _channels[fromRadio.Channel.Index] = fromRadio.Channel;
                UpdateChannelDisplay();
                Log($"Channel {fromRadio.Channel.Index}: {fromRadio.Channel.DisplayName} ({fromRadio.Channel.Role})", LogLevel.Info);
            }

            // Handle MeshPacket
            if (fromRadio.Packet != null)
            {
                Log($"MeshPacket from=!{fromRadio.Packet.From:x8} to=!{fromRadio.Packet.To:x8} decoded={fromRadio.Packet.Decoded != null} encrypted={fromRadio.Packet.Encrypted != null}", LogLevel.Debug);
                ProcessMeshPacket(fromRadio.Packet);
            }
        }

        private void ProcessMeshPacket(MeshPacket packet)
        {
            if (packet.Decoded == null)
            {
                Log($"Packet has no decoded data (encrypted={packet.Encrypted?.Length ?? 0} bytes)", LogLevel.Debug);
                return;
            }

            var data = packet.Decoded;
            Log($"Decoded packet portnum={data.PortNum} payload={data.Payload.Length} bytes", LogLevel.Debug);

            switch (data.PortNum)
            {
                case PortNum.TextMessage:
                    AddMessage(packet, data.GetTextMessage());
                    break;

                case PortNum.Position:
                    var pos = data.GetPosition();
                    if (pos != null && pos.HasValidFix)
                    {
                        // Update position for the node that sent it
                        if (_nodes.TryGetValue(packet.From, out var node))
                        {
                            node.Position = pos;
                        }

                        // If it's from our node, update display
                        Latitude = pos.Latitude;
                        Longitude = pos.Longitude;
                        Altitude = pos.Altitude;
                        GpsSatellites = pos.Sats;
                    }
                    break;

                case PortNum.NodeInfo:
                    // Node info updates are handled separately
                    break;

                case PortNum.Telemetry:
                    var telemetry = data.GetTelemetry();
                    if (telemetry != null)
                    {
                        _nodeTelemetry[packet.From] = telemetry;
                        UpdateTelemetryDisplay(telemetry);
                    }
                    break;
            }
        }

        private void AddMessage(MeshPacket packet, string text)
        {
            var senderName = GetNodeName(packet.From);
            var channelIndex = packet.Channel;
            var channelName = _channels.TryGetValue((int)channelIndex, out var ch) ? ch.DisplayName : $"Ch {channelIndex}";

            var message = new MeshMessage
            {
                From = packet.From,
                SenderName = senderName,
                Text = text,
                Timestamp = DateTime.Now,
                Snr = packet.RxSnr,
                Rssi = packet.RxRssi,
                FontSize = _settings.FontSize,
                ChannelIndex = channelIndex,
                ChannelName = channelName
            };

            // Update channel message count
            UpdateChannelDisplay();

            Messages.Insert(0, message);

            // Trim old messages
            while (Messages.Count > _settings.MaxMessages)
            {
                Messages.RemoveAt(Messages.Count - 1);
            }

            OnPropertyChanged(nameof(MessageCount));
            OnPropertyChanged(nameof(DisplayMessages));
            OnPropertyChanged(nameof(NoChatMessagesVisibility));
            Log($"Message from {senderName}: {text}", LogLevel.Info);
        }

        private string GetNodeName(uint nodeNum)
        {
            if (_nodes.TryGetValue(nodeNum, out var node) && node.User != null)
            {
                return !string.IsNullOrEmpty(node.User.ShortName)
                    ? node.User.ShortName
                    : node.User.LongName;
            }
            return $"!{nodeNum:x8}";
        }

        private void OnDebugOutput(object? sender, string text)
        {
            // Could show debug output somewhere if needed
        }

        private void OnError(object? sender, Exception ex)
        {
            Log($"Meshtastic error: {ex.Message}", LogLevel.Error);
        }

        private void UpdateChannelDisplay()
        {
            ChannelItems.Clear();
            foreach (var kvp in _channels.OrderBy(c => c.Key))
            {
                var ch = kvp.Value;
                var msgCount = Messages.Count(m => m.ChannelIndex == (uint)ch.Index);
                ChannelItems.Add(new ChannelDisplayItem
                {
                    Index = ch.Index,
                    Name = ch.DisplayName,
                    RoleText = ch.Role == ChannelRole.Primary ? "P" : "S",
                    MessageCount = msgCount
                });
            }
        }

        private void UpdateNodeListDisplay()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            NodeItems.Clear();
            foreach (var kvp in _nodes.OrderByDescending(n => n.Value.LastHeard))
            {
                var node = kvp.Value;
                var name = node.User?.LongName ?? node.NodeIdHex;
                var shortName = node.User?.ShortName ?? "";
                var hwModel = node.User != null && node.User.HwModel > 0
                    ? User.HwModelName(node.User.HwModel)
                    : "";

                string lastHeardText;
                if (node.LastHeard == 0)
                {
                    lastHeardText = "Unknown";
                }
                else
                {
                    var seconds = now - node.LastHeard;
                    if (seconds < 60) lastHeardText = "Just now";
                    else if (seconds < 3600) lastHeardText = $"{seconds / 60}m ago";
                    else if (seconds < 86400) lastHeardText = $"{seconds / 3600}h ago";
                    else lastHeardText = $"{seconds / 86400}d ago";
                }

                var batteryText = node.DeviceMetrics != null ? $"{node.DeviceMetrics.BatteryLevel}%" : "";

                NodeItems.Add(new NodeDisplayItem
                {
                    Num = node.Num,
                    Name = name,
                    ShortName = shortName,
                    HwModel = hwModel,
                    LastHeardText = lastHeardText,
                    HopsAway = node.HopsAway,
                    BatteryText = batteryText,
                    Snr = node.Snr,
                    ViaMqtt = node.ViaMqtt
                });
            }
        }

        private void UpdateTelemetryDisplay(Telemetry telemetry)
        {
            if (telemetry.DeviceMetrics != null)
            {
                var dm = telemetry.DeviceMetrics;
                BatteryLevel = $"{dm.BatteryLevel}%";
                ChannelUtilization = $"{dm.ChannelUtilization:F1}%";
                AirUtilTx = $"{dm.AirUtilTx:F1}%";
                Voltage = $"{dm.Voltage:F2}V";

                if (dm.Uptime > 0)
                {
                    var ts = TimeSpan.FromSeconds(dm.Uptime);
                    Uptime = ts.Days > 0 ? $"{ts.Days}d {ts.Hours}h" : $"{ts.Hours}h {ts.Minutes}m";
                }
            }

            if (telemetry.EnvironmentMetrics != null)
            {
                var em = telemetry.EnvironmentMetrics;
                if (em.Temperature != 0)
                    Temperature = $"{em.Temperature:F1}\u00b0C";
                if (em.RelativeHumidity != 0)
                    Humidity = $"{em.RelativeHumidity:F1}%";
                if (em.BarometricPressure != 0)
                    Pressure = $"{em.BarometricPressure:F1} hPa";
            }
        }

        private static string FormatUptime(uint seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.Days > 0) return $"{ts.Days}d {ts.Hours}h";
            return $"{ts.Hours}h {ts.Minutes}m";
        }

        public async Task SendTextMessageAsync(uint destinationNode, string text)
        {
            if (_serial == null || !_serial.IsConnected)
                throw new InvalidOperationException("Not connected to a Meshtastic device");

            var toRadio = new ToRadio
            {
                Packet = new MeshPacket
                {
                    To = destinationNode,
                    Channel = 0,
                    Decoded = new DataMessage
                    {
                        PortNum = PortNum.TextMessage,
                        Payload = Encoding.UTF8.GetBytes(text)
                    }
                }
            };

            await _serial.SendPacketAsync(toRadio.ToBytes());
            Log($"Sent reply to !{destinationNode:x8}: {text}", LogLevel.Info);

            // Add sent message to local display
            var sentMessage = new MeshMessage
            {
                From = 0,
                SenderName = "You",
                Text = text,
                Timestamp = DateTime.Now,
                FontSize = _settings.FontSize
            };
            Messages.Insert(0, sentMessage);
            while (Messages.Count > _settings.MaxMessages)
            {
                Messages.RemoveAt(Messages.Count - 1);
            }
            OnPropertyChanged(nameof(MessageCount));
            OnPropertyChanged(nameof(DisplayMessages));
        }

        private void Message_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is MeshMessage message)
            {
                Func<uint, string, Task>? sendReply = (_serial != null && _serial.IsConnected)
                    ? SendTextMessageAsync
                    : null;

                var dialog = new MessageDetailDialog(message, sendReply)
                {
                    Owner = Window.GetWindow(this)
                };
                dialog.ShowDialog();
            }
        }

        protected override void OnPageSettingsChanged(string pageId)
        {
            LoadSettings();
        }
    }

    /// <summary>
    /// Represents a received mesh message
    /// </summary>
    public class MeshMessage
    {
        public uint From { get; set; }
        public string SenderName { get; set; } = "";
        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public float Snr { get; set; }
        public int Rssi { get; set; }
        public double FontSize { get; set; } = 12;
        public uint ChannelIndex { get; set; }
        public string ChannelName { get; set; } = "";

        public string TimeString => Timestamp.ToString("HH:mm:ss");
        public string SignalInfo => Rssi != 0 ? $"SNR:{Snr:F1} RSSI:{Rssi}" : "";
    }

    public class ChannelDisplayItem
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public string RoleText { get; set; } = "";
        public int MessageCount { get; set; }
    }

    public class NodeDisplayItem
    {
        public uint Num { get; set; }
        public string Name { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string HwModel { get; set; } = "";
        public string LastHeardText { get; set; } = "";
        public uint HopsAway { get; set; }
        public string BatteryText { get; set; } = "";
        public float Snr { get; set; }
        public bool ViaMqtt { get; set; }

        public string HopsText => HopsAway > 0 ? $"{HopsAway} hop{(HopsAway > 1 ? "s" : "")}" : "Direct";
        public string MqttBadge => ViaMqtt ? "MQTT" : "";
    }
}
