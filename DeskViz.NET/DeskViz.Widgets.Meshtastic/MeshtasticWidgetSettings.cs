using System;
using System.Collections.Generic;
using DeskViz.Plugins.Base;

namespace DeskViz.Widgets.Meshtastic
{
    public class MeshtasticWidgetSettings : BaseWidgetSettings
    {
        public override string WidgetId => "MeshtasticWidget";

        private string _comPort = "";
        private int _baudRate = 115200;
        private bool _autoConnect = true;
        private int _maxMessages = 50;
        private bool _showGpsInfo = true;
        private bool _showNodeList = true;
        private bool _showSignalStrength = true;
        private double _fontSize = 12;
        private double _updateIntervalSeconds = 2;
        private int _displayMessageCount = 5;
        private bool _showChannels = true;
        private bool _showTelemetry = true;
        private bool _showMessages = true;

        /// <summary>
        /// COM port to connect to (e.g., "COM3")
        /// </summary>
        public string ComPort
        {
            get => _comPort;
            set => SetProperty(ref _comPort, value);
        }

        /// <summary>
        /// Baud rate for serial connection
        /// </summary>
        public int BaudRate
        {
            get => _baudRate;
            set => SetProperty(ref _baudRate, value);
        }

        /// <summary>
        /// Whether to auto-connect on widget load
        /// </summary>
        public bool AutoConnect
        {
            get => _autoConnect;
            set => SetProperty(ref _autoConnect, value);
        }

        /// <summary>
        /// Maximum number of messages to keep in history
        /// </summary>
        public int MaxMessages
        {
            get => _maxMessages;
            set => SetProperty(ref _maxMessages, value);
        }

        /// <summary>
        /// Whether to show GPS information
        /// </summary>
        public bool ShowGpsInfo
        {
            get => _showGpsInfo;
            set => SetProperty(ref _showGpsInfo, value);
        }

        /// <summary>
        /// Whether to show the node list
        /// </summary>
        public bool ShowNodeList
        {
            get => _showNodeList;
            set => SetProperty(ref _showNodeList, value);
        }

        /// <summary>
        /// Whether to show signal strength indicators
        /// </summary>
        public bool ShowSignalStrength
        {
            get => _showSignalStrength;
            set => SetProperty(ref _showSignalStrength, value);
        }

        /// <summary>
        /// Font size for messages
        /// </summary>
        public double FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        /// <summary>
        /// Update interval in seconds
        /// </summary>
        public double UpdateIntervalSeconds
        {
            get => _updateIntervalSeconds;
            set => SetProperty(ref _updateIntervalSeconds, value);
        }

        /// <summary>
        /// Number of recent messages to display on the widget face (1-20)
        /// </summary>
        public int DisplayMessageCount
        {
            get => _displayMessageCount;
            set => SetProperty(ref _displayMessageCount, value);
        }

        /// <summary>
        /// Whether to show the channel list section
        /// </summary>
        public bool ShowChannels
        {
            get => _showChannels;
            set => SetProperty(ref _showChannels, value);
        }

        /// <summary>
        /// Whether to show telemetry data section
        /// </summary>
        public bool ShowTelemetry
        {
            get => _showTelemetry;
            set => SetProperty(ref _showTelemetry, value);
        }

        /// <summary>
        /// Whether to show the messages section
        /// </summary>
        public bool ShowMessages
        {
            get => _showMessages;
            set => SetProperty(ref _showMessages, value);
        }

        public override object Clone()
        {
            return new MeshtasticWidgetSettings
            {
                ComPort = ComPort,
                BaudRate = BaudRate,
                AutoConnect = AutoConnect,
                MaxMessages = MaxMessages,
                ShowGpsInfo = ShowGpsInfo,
                ShowNodeList = ShowNodeList,
                ShowSignalStrength = ShowSignalStrength,
                FontSize = FontSize,
                UpdateIntervalSeconds = UpdateIntervalSeconds,
                DisplayMessageCount = DisplayMessageCount,
                ShowChannels = ShowChannels,
                ShowTelemetry = ShowTelemetry,
                ShowMessages = ShowMessages
            };
        }

        public override void Reset()
        {
            ComPort = "";
            BaudRate = 115200;
            AutoConnect = true;
            MaxMessages = 50;
            ShowGpsInfo = true;
            ShowNodeList = true;
            ShowSignalStrength = true;
            FontSize = 12;
            UpdateIntervalSeconds = 2;
            DisplayMessageCount = 5;
            ShowChannels = true;
            ShowTelemetry = true;
            ShowMessages = true;
        }

        protected override BaseWidgetSettings CreateDefault()
        {
            return new MeshtasticWidgetSettings();
        }

        protected override void ValidateSettings(List<string> errors)
        {
            if (BaudRate < 9600 || BaudRate > 921600)
                errors.Add("Baud rate must be between 9600 and 921600");

            if (MaxMessages < 1 || MaxMessages > 500)
                errors.Add("Max messages must be between 1 and 500");

            if (FontSize < 8 || FontSize > 48)
                errors.Add("Font size must be between 8 and 48");

            if (UpdateIntervalSeconds < 0.5 || UpdateIntervalSeconds > 60)
                errors.Add("Update interval must be between 0.5 and 60 seconds");

            if (DisplayMessageCount < 1 || DisplayMessageCount > 20)
                errors.Add("Display message count must be between 1 and 20");
        }
    }
}
