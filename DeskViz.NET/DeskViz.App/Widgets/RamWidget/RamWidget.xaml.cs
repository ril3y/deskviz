using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DeskViz.Core.Services;

namespace DeskViz.App.Widgets
{
    /// <summary>
    /// Interaction logic for RamWidget.xaml
    /// </summary>
    public partial class RamWidget : System.Windows.Controls.UserControl, IWidget, INotifyPropertyChanged
    {
        private readonly IHardwareMonitorService _hardwareMonitorService;
        private readonly SettingsService _settingsService;
        private DispatcherTimer _updateTimer;
        private double _updateIntervalSeconds = 2.5; 
        private float _ramUsagePercentage = 0f;
        private System.Windows.Media.Brush _ramUsageColor = System.Windows.Media.Brushes.LimeGreen;
        private double _totalRamGb = 0;
        private double _usedRamGb = 0;
        private double _freeRamGb = 0;
        private double _pageFileUsedGb = 0;
        private bool _showPageFileInfo = true; 
        private bool _isConfiguring = false;
        private bool _isWidgetVisible = true;
        private ICommand? _configureWidgetCommand;

        /// <summary>
        /// Gets the unique widget identifier
        /// </summary>
        public string WidgetId => "RamWidget";

        /// <summary>
        /// Gets the display name of the widget
        /// </summary>
        public string DisplayName => "RAM Monitor";
        
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
        /// Gets or sets the RAM usage percentage
        /// </summary>
        public float RamUsagePercentage
        {
            get => _ramUsagePercentage;
            set
            {
                if (_ramUsagePercentage != value)
                {
                    _ramUsagePercentage = value;
                    OnPropertyChanged();
                    UpdateUsageColor();
                }
            }
        }

        /// <summary>
        /// Gets or sets the brush for the RAM usage progress bar
        /// </summary>
        public System.Windows.Media.Brush RamUsageColor
        {
            get => _ramUsageColor;
            set
            {
                if (_ramUsageColor != value)
                {
                    _ramUsageColor = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the total RAM in gigabytes
        /// </summary>
        public double TotalRamGb
        {
            get => _totalRamGb;
            set
            {
                if (_totalRamGb != value)
                {
                    _totalRamGb = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalRamFormatted));
                }
            }
        }

        /// <summary>
        /// Gets the formatted total RAM string
        /// </summary>
        public string TotalRamFormatted => $"{TotalRamGb:F1} GB";

        /// <summary>
        /// Gets or sets the used RAM in gigabytes
        /// </summary>
        public double UsedRamGb
        {
            get => _usedRamGb;
            set
            {
                if (_usedRamGb != value)
                {
                    _usedRamGb = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(UsedRamFormatted));
                }
            }
        }

        /// <summary>
        /// Gets the formatted used RAM string
        /// </summary>
        public string UsedRamFormatted => $"{UsedRamGb:F1} GB";

        /// <summary>
        /// Gets or sets the free RAM in gigabytes
        /// </summary>
        public double FreeRamGb
        {
            get => _freeRamGb;
            set
            {
                if (_freeRamGb != value)
                {
                    _freeRamGb = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FreeRamFormatted));
                }
            }
        }

        /// <summary>
        /// Gets the formatted free RAM string
        /// </summary>
        public string FreeRamFormatted => $"{FreeRamGb:F1} GB";

        /// <summary>
        /// Gets or sets the used page file in gigabytes
        /// </summary>
        public double PageFileUsedGb
        {
            get => _pageFileUsedGb;
            set
            {
                if (_pageFileUsedGb != value)
                {
                    _pageFileUsedGb = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PageFileUsedFormatted));
                }
            }
        }

        /// <summary>
        /// Gets the formatted page file used string
        /// </summary>
        public string PageFileUsedFormatted => $"{PageFileUsedGb:F1} GB";

        /// <summary>
        /// Gets or sets whether to show page file information
        /// </summary>
        public bool ShowPageFileInfo
        {
            get => _showPageFileInfo;
            set
            {
                if (_showPageFileInfo != value)
                {
                    _showPageFileInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the RAM update interval in seconds
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
                }
            }
        }

        /// <summary>
        /// Event raised when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the RamWidget class
        /// </summary>
        public RamWidget(IHardwareMonitorService hardwareMonitorService, SettingsService settingsService)
        {
            InitializeComponent();
            DataContext = this;

            // Initialize events
            // DO NOT SET PropertyChanged = null; as this breaks INotifyPropertyChanged
            // ConfigButtonClicked is already nullable, no need to set to null here.

            // Use injected services
            _hardwareMonitorService = hardwareMonitorService ?? throw new ArgumentNullException(nameof(hardwareMonitorService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            // Load settings
            var settings = _settingsService.Settings;
            _showPageFileInfo = settings.RamShowPageFileInfo;
            _updateIntervalSeconds = settings.RamUpdateIntervalSeconds;
            OnPropertyChanged(nameof(ShowPageFileInfo)); 

            // Initialize timer for RAM updates using loaded interval
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(_updateIntervalSeconds);
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();

            // Get initial RAM data
            RefreshData();
        }

        /// <summary>
        /// Handles the Tick event of the update timer
        /// </summary>
        private void UpdateTimer_Tick(object? sender, EventArgs e)
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
                // Update hardware monitor first
                _hardwareMonitorService.Update();

                if (!_hardwareMonitorService.IsInitialized)
                {
                    Debug.WriteLine("[RamWidget] HardwareMonitorService not initialized.");
                    TotalRamGb = 0;
                    UsedRamGb = 0;
                    FreeRamGb = 0;
                    RamUsagePercentage = 0;
                    // Optionally, set a status message or visual indicator
                    return; 
                }

                // Get RAM usage information using correct methods
                var (totalRamMb, availableRamMb) = _hardwareMonitorService.GetRamInfo(); 
                UsedRamGb = _hardwareMonitorService.GetUsedRam(); // Value is already in GiB
                TotalRamGb = totalRamMb / 1024.0; // Convert MB to GB
                FreeRamGb = TotalRamGb - UsedRamGb; // Calculate free based on used and total
                
                // Get percentage directly from the service
                RamUsagePercentage = _hardwareMonitorService.GetRamUsagePercentage();

                // Debugging output - Log both calculated and direct percentage for comparison if needed
                float calculatedPercentage = (TotalRamGb > 0) ? (float)((UsedRamGb / TotalRamGb) * 100) : 0f;
                Debug.WriteLine($"[RamWidget] Updated: Total={TotalRamGb:F1} GB, Used={UsedRamGb:F1} GB, Available={availableRamMb / 1024.0:F1} GB, UsageDirect={RamUsagePercentage:F1}%, UsageCalc={calculatedPercentage:F1}%");
                
                // TODO: Page file info needs reimplementation - GetPageFileUsed() does not exist on the interface.
                // if (ShowPageFileInfo)
                // {
                //     // PageFileUsedGb = _hardwareMonitorService.GetPageFileUsed() / 1024.0;
                // }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RamWidget] Error refreshing RAM data: {ex.Message}");
                System.Windows.MessageBox.Show($"Error refreshing RAM data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Consider setting properties to error state
                TotalRamGb = 0;
                UsedRamGb = 0;
                FreeRamGb = 0;
                RamUsagePercentage = 0;
            }
        }

        /// <summary>
        /// Updates the color of the RAM usage bar based on the current usage percentage
        /// </summary>
        private void UpdateUsageColor()
        {
            RamUsageColor = GetUsageBrush(RamUsagePercentage);
        }

        /// <summary>
        /// Restarts the update timer with the current interval
        /// </summary>
        private void RestartTimer()
        {
            _updateTimer.Stop();
            _updateTimer.Interval = TimeSpan.FromSeconds(_updateIntervalSeconds);
            _updateTimer.Start();
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
        /// Opens the settings UI specific to this widget.
        /// </summary>
        public void OpenWidgetSettings()
        {
            // Create and show the RAM widget settings window, passing the service
            var settingsWindow = new RamWidgetSettings(this, _settingsService);
            
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
        protected System.Windows.Media.Brush GetUsageBrush(double percentage)
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
}
