using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DeskViz.Widgets.Meshtastic.Protocol;

namespace DeskViz.Widgets.Meshtastic
{
    public partial class MeshtasticWidgetSettingsView : UserControl
    {
        private readonly MeshtasticWidgetSettings _settings;
        private readonly MeshtasticWidget _widget;

        public MeshtasticWidgetSettingsView(MeshtasticWidgetSettings settings, MeshtasticWidget widget)
        {
            InitializeComponent();

            _settings = settings;
            _widget = widget;

            LoadSettings();
            RefreshPorts();
        }

        private void LoadSettings()
        {
            // Load COM port
            ComPortComboBox.Text = _settings.ComPort;

            // Load baud rate
            foreach (ComboBoxItem item in BaudRateComboBox.Items)
            {
                if (item.Tag?.ToString() == _settings.BaudRate.ToString())
                {
                    BaudRateComboBox.SelectedItem = item;
                    break;
                }
            }

            AutoConnectCheckBox.IsChecked = _settings.AutoConnect;
            ShowGpsCheckBox.IsChecked = _settings.ShowGpsInfo;
            ShowNodesCheckBox.IsChecked = _settings.ShowNodeList;
            ShowSignalCheckBox.IsChecked = _settings.ShowSignalStrength;
            ShowChannelsCheckBox.IsChecked = _settings.ShowChannels;
            ShowTelemetryCheckBox.IsChecked = _settings.ShowTelemetry;
            ShowMessagesCheckBox.IsChecked = _settings.ShowMessages;
            FontSizeSlider.Value = _settings.FontSize;
            DisplayMessageCountSlider.Value = _settings.DisplayMessageCount;
            DisplayMessageCountLabel.Text = _settings.DisplayMessageCount.ToString();
            MaxMessagesTextBox.Text = _settings.MaxMessages.ToString();

            UpdateConnectionStatus();
        }

        private void RefreshPorts()
        {
            var currentSelection = ComPortComboBox.Text;
            ComPortComboBox.Items.Clear();

            var ports = MeshtasticSerial.GetAvailablePorts();
            foreach (var port in ports.OrderBy(p => p))
            {
                ComPortComboBox.Items.Add(port);
            }

            // Restore selection if still available
            if (ports.Contains(currentSelection))
            {
                ComPortComboBox.Text = currentSelection;
            }
            else if (ports.Length > 0)
            {
                ComPortComboBox.SelectedIndex = 0;
            }
        }

        private void RefreshPorts_Click(object sender, RoutedEventArgs e)
        {
            RefreshPorts();
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            // Apply current settings first
            ApplySettingsToWidget();

            ConnectionStatusText.Text = "Connecting...";
            ConnectButton.IsEnabled = false;

            try
            {
                await _widget.ConnectAsync();
                UpdateConnectionStatus();
            }
            catch (Exception ex)
            {
                ConnectionStatusText.Text = $"Error: {ex.Message}";
            }
            finally
            {
                ConnectButton.IsEnabled = true;
            }
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            _widget.Disconnect();
            UpdateConnectionStatus();
        }

        private void UpdateConnectionStatus()
        {
            if (_widget.IsConnected)
            {
                ConnectionStatusText.Text = "Connected";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                ConnectionStatusText.Text = "Disconnected";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ApplySettingsToWidget();

            // Close the settings window
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.DialogResult = true;
                window.Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.DialogResult = false;
                window.Close();
            }
        }

        private void DisplayMessageCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DisplayMessageCountLabel != null)
                DisplayMessageCountLabel.Text = ((int)e.NewValue).ToString();
        }

        private void ApplySettingsToWidget()
        {
            _settings.ComPort = ComPortComboBox.Text;

            if (BaudRateComboBox.SelectedItem is ComboBoxItem baudItem &&
                int.TryParse(baudItem.Tag?.ToString(), out int baudRate))
            {
                _settings.BaudRate = baudRate;
            }

            _settings.AutoConnect = AutoConnectCheckBox.IsChecked ?? true;
            _settings.ShowGpsInfo = ShowGpsCheckBox.IsChecked ?? true;
            _settings.ShowNodeList = ShowNodesCheckBox.IsChecked ?? true;
            _settings.ShowSignalStrength = ShowSignalCheckBox.IsChecked ?? true;
            _settings.ShowChannels = ShowChannelsCheckBox.IsChecked ?? true;
            _settings.ShowTelemetry = ShowTelemetryCheckBox.IsChecked ?? true;
            _settings.ShowMessages = ShowMessagesCheckBox.IsChecked ?? true;
            _settings.FontSize = FontSizeSlider.Value;
            _settings.DisplayMessageCount = (int)DisplayMessageCountSlider.Value;

            if (int.TryParse(MaxMessagesTextBox.Text, out int maxMessages))
            {
                _settings.MaxMessages = Math.Clamp(maxMessages, 1, 500);
            }

            _widget.ApplySettings(_settings);
        }
    }
}
