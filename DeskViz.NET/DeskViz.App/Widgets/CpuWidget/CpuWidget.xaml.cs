using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DeskViz.Core.Services;

namespace DeskViz.App.Widgets
{
    /// <summary>
    /// Interaction logic for CpuWidget.xaml
    /// </summary>
    public partial class CpuWidget : System.Windows.Controls.UserControl, IWidget, INotifyPropertyChanged
    {
        private readonly IHardwareMonitorService _hardwareMonitorService;
        private DispatcherTimer? _updateTimer;
        private string _cpuName = "Loading CPU info...";
        private float _overallCpuUsagePercentage = 0f;
        private System.Windows.Media.Brush _overallCpuUsageColor = System.Windows.Media.Brushes.LimeGreen;
        private ObservableCollection<CpuCoreInfo> _cpuCores = new ObservableCollection<CpuCoreInfo>();
        private bool _showCores = true;
        private double _updateIntervalSeconds = 2.5;
        private float _cpuTemperature = float.NaN;
        private bool _isConfiguring = false;
        private bool _isWidgetVisible = true;
        private ICommand? _configureWidgetCommand;

        // New properties for temperature display
        private bool _showTemperature = true;
        private bool _useFahrenheit = false;
        private double _temperatureFontSize = 12.0;

        // New properties for additional metrics
        private bool _showClockSpeed = false;
        private bool _showPowerUsage = false;
        private float _cpuClockSpeed = 0f;
        private float _cpuPowerUsage = 0f;

        private SettingsService? _settingsService; // Added to store the SettingsService instance

        /// <summary>
        /// Gets the unique widget identifier
        /// </summary>
        public string WidgetId => "CpuWidget";

        /// <summary>
        /// Gets the display name of the widget
        /// </summary>
        public string DisplayName => "CPU Monitor";

        /// <summary>
        /// Gets or sets whether the widget is in configuration mode
        /// </summary>
        public bool IsConfiguring
        {
            get => _isConfiguring;
            set
            {
                if (_isConfiguring != value)
                {
                    _isConfiguring = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the widget is visible
        /// </summary>
        public bool IsWidgetVisible
        {
            get => _isWidgetVisible;
            set
            {
                if (_isWidgetVisible != value)
                {
                    _isWidgetVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Command to open the widget-specific settings UI.
        /// </summary>
        public ICommand ConfigureWidgetCommand
        {
            get
            {
                if (_configureWidgetCommand == null)
                {
                    _configureWidgetCommand = new RelayCommand(_ => OpenWidgetSettings());
                }
                return _configureWidgetCommand;
            }
        }

        /// <summary>
        /// Event raised when the widget configuration button is clicked
        /// </summary>
        public event EventHandler? ConfigButtonClicked;

        /// <summary>
        /// Gets or sets the CPU name
        /// </summary>
        public string CpuName
        {
            get => _cpuName;
            set
            {
                if (_cpuName != value)
                {
                    _cpuName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the overall CPU usage percentage
        /// </summary>
        public float OverallCpuUsagePercentage
        {
            get => _overallCpuUsagePercentage;
            set
            {
                if (_overallCpuUsagePercentage != value)
                {
                    _overallCpuUsagePercentage = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the brush for the overall CPU usage progress bar
        /// </summary>
        public System.Windows.Media.Brush OverallCpuUsageColor
        {
            get => _overallCpuUsageColor;
            set
            {
                if (_overallCpuUsageColor != value)
                {
                    _overallCpuUsageColor = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the CPU core information collection
        /// </summary>
        public ObservableCollection<CpuCoreInfo> CpuCores => _cpuCores;

        /// <summary>
        /// Gets or sets the CPU update interval in seconds
        /// </summary>
        public double UpdateIntervalSeconds
        {
            get => _updateIntervalSeconds;
            set
            {
                if (_updateIntervalSeconds != value)
                {
                    _updateIntervalSeconds = value;
                    OnPropertyChanged();
                    RestartTimer();
                    SaveSettings(); // Added to save settings when UpdateIntervalSeconds changes
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show individual cores
        /// </summary>
        public bool ShowCores
        {
            get => _showCores;
            set
            {
                if (_showCores != value)
                {
                    _showCores = value;
                    OnPropertyChanged();
                    UpdateCoreVisibility();
                    SaveSettings(); // Added to save settings when ShowCores changes
                }
            }
        }

        /// <summary>
        /// Gets or sets the current CPU package temperature.
        /// </summary>
        public float CpuTemperature
        {
            get => _cpuTemperature;
            set
            {
                if (_cpuTemperature != value)
                {
                    _cpuTemperature = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show the CPU temperature.
        /// </summary>
        public bool ShowTemperature
        {
            get => _showTemperature;
            set
            {
                if (_showTemperature != value)
                {
                    _showTemperature = value;
                    OnPropertyChanged();
                    SaveSettings(); // Added to save settings when ShowTemperature changes
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to use Fahrenheit instead of Celsius.
        /// </summary>
        public bool UseFahrenheit
        {
            get => _useFahrenheit;
            set
            {
                if (_useFahrenheit != value)
                {
                    _useFahrenheit = value;
                    // Update the static property in the converter
                    Converters.TemperatureToStringConverter.UseFahrenheit = value;
                    OnPropertyChanged();
                    SaveSettings(); // Added to save settings when UseFahrenheit changes
                }
            }
        }

        /// <summary>
        /// Gets or sets the temperature font size.
        /// </summary>
        public double TemperatureFontSize
        {
            get => _temperatureFontSize;
            set
            {
                if (_temperatureFontSize != value)
                {
                    _temperatureFontSize = value;
                    OnPropertyChanged();
                    SaveSettings(); // Added to save settings when TemperatureFontSize changes
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show the CPU clock speed.
        /// </summary>
        public bool ShowClockSpeed
        {
            get => _showClockSpeed;
            set
            {
                if (_showClockSpeed != value)
                {
                    _showClockSpeed = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show the CPU power usage.
        /// </summary>
        public bool ShowPowerUsage
        {
            get => _showPowerUsage;
            set
            {
                if (_showPowerUsage != value)
                {
                    _showPowerUsage = value;
                    OnPropertyChanged();
                    SaveSettings(); // Added to save settings when ShowPowerUsage changes
                }
            }
        }

        /// <summary>
        /// Gets or sets the current CPU clock speed in MHz.
        /// </summary>
        public float CpuClockSpeed
        {
            get => _cpuClockSpeed;
            set
            {
                if (_cpuClockSpeed != value)
                {
                    _cpuClockSpeed = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current CPU power usage in watts.
        /// </summary>
        public float CpuPowerUsage
        {
            get => _cpuPowerUsage;
            set
            {
                if (_cpuPowerUsage != value)
                {
                    _cpuPowerUsage = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Event raised when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the CpuWidget class
        /// </summary>
        public CpuWidget(IHardwareMonitorService hardwareMonitorService, SettingsService? settingsService = null)
        {
            InitializeComponent();
            DataContext = this;

            // Use injected service
            _hardwareMonitorService = hardwareMonitorService ?? throw new ArgumentNullException(nameof(hardwareMonitorService));
            
            // Load settings if provided
            if (settingsService != null)
            {
                LoadSettings(settingsService);
            }

            // Initialize timer for CPU updates
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(_updateIntervalSeconds);
            _updateTimer.Tick += UpdateCpuData;
            _updateTimer.Start();

            // Get initial CPU data
            RefreshData();
        }

        /// <summary>
        /// Loads widget settings from the settings service
        /// </summary>
        private void LoadSettings(SettingsService settingsService)
        {
            var settings = settingsService.Settings;
            
            // Apply CPU widget settings
            ShowCores = settings.CpuShowCores;
            ShowTemperature = settings.CpuShowTemperature;
            UseFahrenheit = settings.CpuUseFahrenheit;
            TemperatureFontSize = settings.CpuTemperatureFontSize;
            ShowPowerUsage = settings.CpuShowPowerUsage;
            UpdateIntervalSeconds = settings.CpuUpdateIntervalSeconds;
            
            // Keep a reference to the service for saving changes
            _settingsService = settingsService;
        }

        /// <summary>
        /// Saves widget settings to the settings service
        /// </summary>
        private void SaveSettings()
        {
            if (_settingsService == null)
                return;
                
            var settings = _settingsService.Settings;
            
            // Update CPU widget settings
            settings.CpuShowCores = ShowCores;
            settings.CpuShowTemperature = ShowTemperature;
            settings.CpuUseFahrenheit = UseFahrenheit;
            settings.CpuTemperatureFontSize = TemperatureFontSize;
            settings.CpuShowPowerUsage = ShowPowerUsage;
            settings.CpuUpdateIntervalSeconds = UpdateIntervalSeconds;
            
            // Save changes
            _settingsService.SaveSettings();
        }

        /// <summary>
        /// Handles the Tick event of the update timer
        /// </summary>
        private void UpdateCpuData(object? sender, EventArgs e)
        {
            RefreshData();
        }

        /// <summary>
        /// Refreshes the widget data
        /// </summary>
        public void RefreshData()
        {
            try
            {
                // Update all sensors first
                _hardwareMonitorService.Update();

                // Get CPU name if not already set
                if (CpuName == "Loading CPU info...")
                {
                    CpuName = _hardwareMonitorService.GetCpuName() ?? "Unknown CPU";
                }

                // Get overall CPU usage
                var overallUsage = _hardwareMonitorService.GetOverallCpuUsage();
                OverallCpuUsagePercentage = overallUsage;
                OverallCpuUsageColor = GetUsageColor(overallUsage);

                // Get CPU Temperature
                CpuTemperature = _hardwareMonitorService.GetCpuPackageTemperature();

                // Always get CPU clock speed (now always displayed)
                try {
                    var clockSpeed = _hardwareMonitorService.GetCpuClockSpeed();
                    // Only update if we got a non-zero value
                    if (clockSpeed > 0)
                    {
                        CpuClockSpeed = clockSpeed;
                    }
                }
                catch (Exception ex) {
                    // Method might not be implemented yet, handle gracefully
                    System.Diagnostics.Debug.WriteLine($"Failed to get CPU clock speed: {ex.Message}");
                }
                
                // Always get CPU power usage (even if not displayed)
                try {
                    var powerUsage = _hardwareMonitorService.GetCpuPowerUsage();
                    // Only update if we got a non-zero value
                    if (powerUsage > 0)
                    {
                        CpuPowerUsage = powerUsage;
                    }
                }
                catch (Exception ex) {
                    // Method might not be implemented yet, handle gracefully
                    System.Diagnostics.Debug.WriteLine($"Failed to get CPU power usage: {ex.Message}");
                }

                // Get per-core CPU usage if showing cores
                if (ShowCores)
                {
                    UpdateCpuCores();
                }
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error refreshing CPU data: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the CPU cores information
        /// </summary>
        private void UpdateCpuCores()
        {
            var coreUsages = _hardwareMonitorService.GetCpuCoreUsage();

            // Update the data in the ObservableCollection first
            for (int i = 0; i < coreUsages.Count; i++)
            {
                if (i < CpuCores.Count)
                {
                    // Existing core, update its usage
                    CpuCores[i].UsagePercentage = coreUsages[i];
                    // Name and Percentage are updated via CpuCoreInfo's setter now
                }
                else
                {
                    // Add new core if hardware reports more cores than we have
                    CpuCores.Add(new CpuCoreInfo { Name = $"Core {i + 1}", UsagePercentage = coreUsages[i] });
                }
            }

            // Remove excess cores if hardware reports fewer
            while (CpuCores.Count > coreUsages.Count)
            {
                CpuCores.RemoveAt(CpuCores.Count - 1);
            }
        }

        /// <summary>
        /// Updates the visibility of CPU cores
        /// </summary>
        private void UpdateCoreVisibility()
        {
            // No need to modify visibility here since it's bound in XAML
        }

        /// <summary>
        /// Restarts the update timer with the current interval
        /// </summary>
        private void RestartTimer()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Interval = TimeSpan.FromSeconds(_updateIntervalSeconds);
                _updateTimer.Start();
            }
        }

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Handles the Click event of the ConfigButton
        /// </summary>
        private void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            OnConfigButtonClicked();
            System.Windows.MessageBox.Show($"Configure {DisplayName}", "Widget Configuration", MessageBoxButton.OK, MessageBoxImage.Information);
            OpenWidgetSettings();
        }

        /// <summary>
        /// Raises the ConfigButtonClicked event
        /// </summary>
        protected void OnConfigButtonClicked()
        {
            ConfigButtonClicked?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Opens the settings UI specific to this widget.
        /// </summary>
        public void OpenWidgetSettings()
        {
            // Create and show the CPU widget settings window
            var settingsWindow = new CpuWidgetSettings(this);

            // Set the owner to the main window for proper modal behavior
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null)
            {
                settingsWindow.Owner = mainWindow;
            }

            // Show dialog and apply settings if OK was clicked
            settingsWindow.ShowDialog();
        }

        /// <summary>
        /// Determines the brush color based on the usage percentage.
        /// </summary>
        /// <param name="percentage">The usage percentage (0-100).</param>
        /// <returns>A Brush corresponding to the usage level.</returns>
        protected System.Windows.Media.Brush GetUsageColor(double percentage)
        {
            if (percentage > 90)
            {
                return System.Windows.Media.Brushes.Red;
            }
            else if (percentage > 70)
            {
                return System.Windows.Media.Brushes.Orange;
            }
            else
            {
                // Use a less intense green for the default
                return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4CAF50"));
            }
        }
    }

    /// <summary>
    /// Information about a CPU core
    /// </summary>
    public class CpuCoreInfo : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private float _usagePercentage;
        private System.Windows.Media.Brush _usageColor = System.Windows.Media.Brushes.LimeGreen;

        /// <summary>
        /// Gets or sets the name of the core
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the usage percentage of the core
        /// </summary>
        public float UsagePercentage
        {
            get => _usagePercentage;
            set
            {
                if (_usagePercentage != value)
                {
                    _usagePercentage = value;
                    OnPropertyChanged();
                    UpdateColor();
                    OnPropertyChanged(nameof(Percentage));
                }
            }
        }

        /// <summary>
        /// Gets the formatted percentage string
        /// </summary>
        public string Percentage => $"{UsagePercentage:F1}%";

        /// <summary>
        /// Gets or sets the color based on usage
        /// </summary>
        public System.Windows.Media.Brush UsageColor
        {
            get => _usageColor;
            set
            {
                if (_usageColor != value)
                {
                    _usageColor = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Event raised when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Updates the color based on usage
        /// </summary>
        private void UpdateColor()
        {
            if (UsagePercentage < 60)
                UsageColor = System.Windows.Media.Brushes.LimeGreen;
            else if (UsagePercentage < 85)
                UsageColor = System.Windows.Media.Brushes.Orange;
            else
                UsageColor = System.Windows.Media.Brushes.Red;
        }

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
