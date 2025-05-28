using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DeskViz.Core.Services;

namespace DeskViz.App.Widgets.GpuWidget
{
    /// <summary>
    /// Interaction logic for GpuWidget.xaml
    /// </summary>
    public partial class GpuWidget : System.Windows.Controls.UserControl, IWidget, INotifyPropertyChanged
    {
        private readonly IHardwareMonitorService _hardwareMonitorService;
        private readonly SettingsService _settingsService;
        private readonly DispatcherTimer _updateTimer;

        // IWidget implementation
        public string WidgetId => "GpuWidget";
        public string DisplayName => "GPU Monitor";

        private bool _isWidgetVisible = true;
        public bool IsWidgetVisible
        {
            get => _isWidgetVisible;
            set
            {
                if (_isWidgetVisible != value)
                {
                    _isWidgetVisible = value;
                    OnPropertyChanged();
                    Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        public event EventHandler? ConfigButtonClicked;

        // GPU properties
        private string _gpuName = "GPU";
        public string GpuName
        {
            get => _gpuName;
            set { _gpuName = value; OnPropertyChanged(); }
        }

        private float _gpuUsage;
        public float GpuUsage
        {
            get => _gpuUsage;
            set { _gpuUsage = value; OnPropertyChanged(); }
        }

        private float _gpuTemperature;
        public float GpuTemperature
        {
            get => _gpuTemperature;
            set { _gpuTemperature = value; OnPropertyChanged(); }
        }

        private float _gpuPower;
        public float GpuPower
        {
            get => _gpuPower;
            set { _gpuPower = value; OnPropertyChanged(); }
        }

        private float _gpuMemoryUsagePercent;
        public float GpuMemoryUsagePercent
        {
            get => _gpuMemoryUsagePercent;
            set { _gpuMemoryUsagePercent = value; OnPropertyChanged(); }
        }

        private float _gpuMemoryUsedGB;
        public float GpuMemoryUsedGB
        {
            get => _gpuMemoryUsedGB;
            set { _gpuMemoryUsedGB = value; OnPropertyChanged(); }
        }

        private float _gpuMemoryTotalGB;
        public float GpuMemoryTotalGB
        {
            get => _gpuMemoryTotalGB;
            set { _gpuMemoryTotalGB = value; OnPropertyChanged(); }
        }

        private float _gpuClockSpeed;
        public float GpuClockSpeed
        {
            get => _gpuClockSpeed;
            set { _gpuClockSpeed = value; OnPropertyChanged(); }
        }

        private DateTime _lastUpdateTime;
        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set { _lastUpdateTime = value; OnPropertyChanged(); }
        }

        // Settings properties
        private bool _showTemperature = true;
        public bool ShowTemperature
        {
            get => _showTemperature;
            set
            {
                if (_showTemperature != value)
                {
                    _showTemperature = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        private bool _showPower = true;
        public bool ShowPower
        {
            get => _showPower;
            set
            {
                if (_showPower != value)
                {
                    _showPower = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        private bool _showMemory = true;
        public bool ShowMemory
        {
            get => _showMemory;
            set
            {
                if (_showMemory != value)
                {
                    _showMemory = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        private bool _showClockSpeed = true;
        public bool ShowClockSpeed
        {
            get => _showClockSpeed;
            set
            {
                if (_showClockSpeed != value)
                {
                    _showClockSpeed = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        private bool _isCelsius = true;
        public bool IsCelsius
        {
            get => _isCelsius;
            set
            {
                if (_isCelsius != value)
                {
                    _isCelsius = value;
                    // Update the static property in the converter (inverse because IsCelsius vs UseFahrenheit)
                    Converters.TemperatureToStringConverter.UseFahrenheit = !value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        private int _updateInterval = 2;
        public int UpdateInterval
        {
            get => _updateInterval;
            set
            {
                if (_updateInterval != value && value > 0)
                {
                    _updateInterval = value;
                    OnPropertyChanged();
                    _updateTimer.Interval = TimeSpan.FromSeconds(value);
                    SaveSettings();
                }
            }
        }

        private int _selectedGpuIndex = 0;
        public int SelectedGpuIndex
        {
            get => _selectedGpuIndex;
            set
            {
                if (_selectedGpuIndex != value)
                {
                    _selectedGpuIndex = value;
                    OnPropertyChanged();
                    SaveSettings();
                    // Refresh data for the new GPU
                    RefreshData();
                }
            }
        }

        // Available GPUs for selection
        private List<GpuInfo> _availableGpus = new();
        public List<GpuInfo> AvailableGpus
        {
            get => _availableGpus;
            set
            {
                _availableGpus = value;
                OnPropertyChanged();
            }
        }

        public GpuWidget(IHardwareMonitorService hardwareMonitorService, SettingsService settingsService)
        {
            _hardwareMonitorService = hardwareMonitorService ?? throw new ArgumentNullException(nameof(hardwareMonitorService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            InitializeComponent();
            DataContext = this;

            LoadSettings();

            // Load available GPUs
            var gpuTuples = _hardwareMonitorService.GetAvailableGpus();
            AvailableGpus = gpuTuples.Select(gpu => new GpuInfo 
            { 
                Index = gpu.Index, 
                Name = gpu.Name, 
                Type = gpu.Type 
            }).ToList();

            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_updateInterval)
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();

            // Initial data load
            RefreshData();
        }

        private void LoadSettings()
        {
            var settings = _settingsService.Settings;
            
            ShowTemperature = settings.GpuShowTemperature;
            ShowPower = settings.GpuShowPower;
            ShowMemory = settings.GpuShowMemory;
            ShowClockSpeed = settings.GpuShowClockSpeed;
            IsCelsius = settings.GpuIsCelsius;
            UpdateInterval = settings.GpuUpdateInterval;
            SelectedGpuIndex = settings.GpuSelectedIndex;
            
            // Initialize the converter's static property
            Converters.TemperatureToStringConverter.UseFahrenheit = !IsCelsius;
        }

        private void SaveSettings()
        {
            if (_settingsService == null)
                return;
                
            var settings = _settingsService.Settings;
            
            // Update GPU widget settings
            settings.GpuShowTemperature = ShowTemperature;
            settings.GpuShowPower = ShowPower;
            settings.GpuShowMemory = ShowMemory;
            settings.GpuShowClockSpeed = ShowClockSpeed;
            settings.GpuIsCelsius = IsCelsius;
            settings.GpuUpdateInterval = UpdateInterval;
            settings.GpuSelectedIndex = SelectedGpuIndex;
            
            // Save changes
            _settingsService.SaveSettings();
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            RefreshData();
        }

        public void RefreshData()
        {
            try
            {
                // Update hardware data
                _hardwareMonitorService.Update();

                // Get GPU name for selected GPU
                GpuName = _hardwareMonitorService.GetGpuName(SelectedGpuIndex);

                // Get GPU usage for selected GPU
                GpuUsage = _hardwareMonitorService.GetGpuUsage(SelectedGpuIndex);

                // Get temperature for selected GPU
                GpuTemperature = _hardwareMonitorService.GetGpuTemperature(SelectedGpuIndex);

                // Get power usage for selected GPU
                GpuPower = _hardwareMonitorService.GetGpuPowerUsage(SelectedGpuIndex);

                // Get memory info for selected GPU
                var memoryInfo = _hardwareMonitorService.GetGpuMemoryInfo(SelectedGpuIndex);
                GpuMemoryUsedGB = memoryInfo.UsedMB / 1024f;
                GpuMemoryTotalGB = memoryInfo.TotalMB / 1024f;
                GpuMemoryUsagePercent = memoryInfo.UsagePercent;

                // Get clock speed for selected GPU
                GpuClockSpeed = _hardwareMonitorService.GetGpuClockSpeed(SelectedGpuIndex);

                // Update timestamp
                LastUpdateTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing GPU data: {ex.Message}");
            }
        }

        public void OpenWidgetSettings()
        {
            var settingsWindow = new GpuWidgetSettings(this)
            {
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            settingsWindow.ShowDialog();
        }

        private void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigButtonClicked?.Invoke(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Helper class for GPU information binding
    public class GpuInfo
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}