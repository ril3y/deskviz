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
    /// Interaction logic for HardDriveWidget.xaml
    /// </summary>
    public partial class HardDriveWidget : System.Windows.Controls.UserControl, IWidget, INotifyPropertyChanged
    {
        private readonly IHardwareMonitorService _hardwareMonitorService;
        private DispatcherTimer? _updateTimer;
        private ObservableCollection<DriveInfo> _drives = new ObservableCollection<DriveInfo>();
        private double _updateIntervalSeconds = 3.0;
        private bool _isConfiguring = false;
        private bool _isWidgetVisible = true;
        private ICommand? _configureWidgetCommand;

        // Widget settings
        private bool _showTemperature = true;
        private bool _showLabel = true;
        private HashSet<string> _selectedDrives = new HashSet<string>(); // Which drives to show

        private SettingsService? _settingsService;

        /// <summary>
        /// Gets the unique widget identifier
        /// </summary>
        public string WidgetId => "HardDriveWidget";

        /// <summary>
        /// Gets the display name of the widget
        /// </summary>
        public string DisplayName => "Hard Drive Monitor";

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
        /// Command to open the widget-specific settings UI
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
        /// Gets the drive information collection
        /// </summary>
        public ObservableCollection<DriveInfo> Drives => _drives;

        /// <summary>
        /// Gets or sets the update interval in seconds
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
                    SaveSettings();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show drive temperatures
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
                    UpdateDriveTemperatureVisibility();
                    SaveSettings();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show drive labels
        /// </summary>
        public bool ShowLabel
        {
            get => _showLabel;
            set
            {
                if (_showLabel != value)
                {
                    _showLabel = value;
                    OnPropertyChanged();
                    UpdateDriveLabelVisibility();
                    SaveSettings();
                }
            }
        }

        /// <summary>
        /// Gets the collection of selected drives to display
        /// </summary>
        public HashSet<string> SelectedDrives => _selectedDrives;

        /// <summary>
        /// Gets or sets the selected drives as a comma-separated string (for serialization)
        /// </summary>
        public string SelectedDrivesString
        {
            get => string.Join(",", _selectedDrives);
            set
            {
                var newDrives = new HashSet<string>(
                    (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(d => d.Trim())
                        .Where(d => !string.IsNullOrEmpty(d))
                );

                if (!_selectedDrives.SetEquals(newDrives))
                {
                    _selectedDrives = newDrives;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedDrives));
                    RefreshData(); // Refresh to apply new filter
                    SaveSettings();
                }
            }
        }

        /// <summary>
        /// Event raised when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the HardDriveWidget class
        /// </summary>
        public HardDriveWidget(IHardwareMonitorService hardwareMonitorService, SettingsService? settingsService = null)
        {
            InitializeComponent();
            DataContext = this;

            _hardwareMonitorService = hardwareMonitorService ?? throw new ArgumentNullException(nameof(hardwareMonitorService));

            if (settingsService != null)
            {
                LoadSettings(settingsService);
            }

            // Initialize timer for drive updates
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(_updateIntervalSeconds);
            _updateTimer.Tick += UpdateDriveData;
            _updateTimer.Start();

            // Get initial drive data
            RefreshData();
        }

        /// <summary>
        /// Loads widget settings from the settings service
        /// </summary>
        private void LoadSettings(SettingsService settingsService)
        {
            // For now, use default settings - can be extended later
            // ShowTemperature = settings.HardDriveShowTemperature;
            // ShowLabel = settings.HardDriveShowLabel;
            // UpdateIntervalSeconds = settings.HardDriveUpdateIntervalSeconds;

            _settingsService = settingsService;
        }

        /// <summary>
        /// Saves widget settings to the settings service
        /// </summary>
        private void SaveSettings()
        {
            // For now, settings are not persisted - can be extended later
            // if (_settingsService != null)
            // {
            //     var settings = _settingsService.Settings;
            //     settings.HardDriveShowTemperature = ShowTemperature;
            //     settings.HardDriveShowLabel = ShowLabel;
            //     settings.HardDriveUpdateIntervalSeconds = UpdateIntervalSeconds;
            //     _settingsService.SaveSettings();
            // }
        }

        /// <summary>
        /// Handles the Tick event of the update timer
        /// </summary>
        private void UpdateDriveData(object? sender, EventArgs e)
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
                _hardwareMonitorService.Update();

                var driveInfos = _hardwareMonitorService.GetDriveInfo();

                // Filter drives based on selection (if any drives are selected, only show those)
                if (_selectedDrives.Any())
                {
                    driveInfos = driveInfos.Where(d => _selectedDrives.Contains(d.Name)).ToList();
                }

                // Update existing drives or add new ones
                foreach (var driveInfo in driveInfos)
                {
                    var existingDrive = _drives.FirstOrDefault(d => d.Name == driveInfo.Name);
                    if (existingDrive != null)
                    {
                        // Update existing drive
                        existingDrive.Label = driveInfo.Label;
                        existingDrive.TotalBytes = driveInfo.TotalBytes;
                        existingDrive.UsedBytes = driveInfo.UsedBytes;
                        existingDrive.UsagePercent = driveInfo.UsagePercent;

                        // Get temperature if available
                        if (_showTemperature)
                        {
                            var temp = _hardwareMonitorService.GetDriveTemperature(driveInfo.Name);
                            existingDrive.Temperature = float.IsNaN(temp) ? 0f : temp;
                        }
                    }
                    else
                    {
                        // Add new drive
                        var newDrive = new DriveInfo
                        {
                            Name = driveInfo.Name,
                            Label = driveInfo.Label,
                            TotalBytes = driveInfo.TotalBytes,
                            UsedBytes = driveInfo.UsedBytes,
                            UsagePercent = driveInfo.UsagePercent,
                            ShowTemperature = _showTemperature,
                            ShowLabel = _showLabel
                        };

                        if (_showTemperature)
                        {
                            var temp = _hardwareMonitorService.GetDriveTemperature(driveInfo.Name);
                            newDrive.Temperature = float.IsNaN(temp) ? 0f : temp;
                        }

                        _drives.Add(newDrive);
                    }
                }

                // Remove drives that no longer exist
                var drivesToRemove = _drives.Where(d => !driveInfos.Any(di => di.Name == d.Name)).ToList();
                foreach (var drive in drivesToRemove)
                {
                    _drives.Remove(drive);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing hard drive data: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates temperature visibility for all drives
        /// </summary>
        private void UpdateDriveTemperatureVisibility()
        {
            foreach (var drive in _drives)
            {
                drive.ShowTemperature = _showTemperature;
            }
        }

        /// <summary>
        /// Updates label visibility for all drives
        /// </summary>
        private void UpdateDriveLabelVisibility()
        {
            foreach (var drive in _drives)
            {
                drive.ShowLabel = _showLabel;
            }
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
        /// Gets all available drives from the hardware monitor service
        /// </summary>
        public List<(string Name, string Label)> GetAvailableDrives()
        {
            try
            {
                _hardwareMonitorService.Update();
                return _hardwareMonitorService.GetDriveInfo()
                    .Select(d => (d.Name, d.Label))
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting available drives: {ex.Message}");
                return new List<(string Name, string Label)>();
            }
        }

        /// <summary>
        /// Opens the settings UI specific to this widget
        /// </summary>
        public void OpenWidgetSettings()
        {
            // Create and show the hard drive widget settings window
            var settingsWindow = new HardDriveWidgetSettings(this);

            // Set the owner to the main window for proper modal behavior
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null)
            {
                settingsWindow.Owner = mainWindow;
            }

            // Show dialog
            settingsWindow.ShowDialog();
        }
    }

    /// <summary>
    /// Information about a hard drive
    /// </summary>
    public class DriveInfo : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _label = string.Empty;
        private long _totalBytes;
        private long _usedBytes;
        private float _usagePercent;
        private float _temperature;
        private bool _showTemperature = true;
        private bool _showLabel = true;
        private System.Windows.Media.Brush _usageColor = System.Windows.Media.Brushes.LimeGreen;

        /// <summary>
        /// Gets or sets the drive name (e.g., "C:\")
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
        /// Gets or sets the drive label
        /// </summary>
        public string Label
        {
            get => _label;
            set
            {
                if (_label != value)
                {
                    _label = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the total bytes
        /// </summary>
        public long TotalBytes
        {
            get => _totalBytes;
            set
            {
                if (_totalBytes != value)
                {
                    _totalBytes = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the used bytes
        /// </summary>
        public long UsedBytes
        {
            get => _usedBytes;
            set
            {
                if (_usedBytes != value)
                {
                    _usedBytes = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the usage percentage
        /// </summary>
        public float UsagePercent
        {
            get => _usagePercent;
            set
            {
                if (_usagePercent != value)
                {
                    _usagePercent = value;
                    OnPropertyChanged();
                    UpdateUsageColor();
                }
            }
        }

        /// <summary>
        /// Gets or sets the drive temperature
        /// </summary>
        public float Temperature
        {
            get => _temperature;
            set
            {
                if (_temperature != value)
                {
                    _temperature = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show temperature
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
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show label
        /// </summary>
        public bool ShowLabel
        {
            get => _showLabel;
            set
            {
                if (_showLabel != value)
                {
                    _showLabel = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the usage color based on percentage
        /// </summary>
        public System.Windows.Media.Brush UsageColor
        {
            get => _usageColor;
            private set
            {
                if (_usageColor != value)
                {
                    _usageColor = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Updates the usage color based on percentage
        /// </summary>
        private void UpdateUsageColor()
        {
            if (UsagePercent > 90)
            {
                UsageColor = System.Windows.Media.Brushes.Red;
            }
            else if (UsagePercent > 75)
            {
                UsageColor = System.Windows.Media.Brushes.Orange;
            }
            else if (UsagePercent > 50)
            {
                UsageColor = System.Windows.Media.Brushes.Yellow;
            }
            else
            {
                UsageColor = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4CAF50"));
            }
        }

        /// <summary>
        /// Event raised when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}