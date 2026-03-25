using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DeskViz.Plugins.Base;
using DeskViz.Plugins.Interfaces;
using DeskViz.Plugins.Services;

namespace DeskViz.Widgets.Cpu
{
    public partial class CpuWidget : BaseWidget
    {
        private IPluginHardwareMonitorService? _hardwareService;
        private CpuWidgetSettings _settings = new();

        private string _cpuName = "Loading CPU info...";
        private float _overallCpuUsagePercentage = 0f;
        private Brush _overallCpuUsageColor = Brushes.LimeGreen;
        private ObservableCollection<CpuCoreInfo> _cpuCores = new();
        private float _cpuTemperature = float.NaN;
        private float _cpuClockSpeed = 0f;
        private float _cpuPowerUsage = 0f;

        // Smooth animation tracking
        private float _displayedUsagePercentage = 0f;
        private readonly Queue<float> _valueBuffer = new Queue<float>();
        private const int BufferSize = 5; // Increased for smoother averaging

        public override IWidgetMetadata Metadata { get; } = new WidgetMetadata
        {
            Id = "CpuWidget",
            Name = "CPU Monitor",
            Description = "Monitors CPU usage, temperature, clock speed, and power consumption",
            Author = "DeskViz Team",
            Version = new Version(2, 0, 0),
            Category = "Hardware",
            Tags = new[] { "cpu", "performance", "monitoring", "hardware" },
            RequiresElevatedPermissions = false,
            MinimumHostVersion = new Version(1, 0, 0)
        };

        public override string WidgetId => "CpuWidget";
        public override string DisplayName => "CPU Monitor";

        public string CpuName
        {
            get => _cpuName;
            set { _cpuName = value; OnPropertyChanged(); }
        }

        public float OverallCpuUsagePercentage
        {
            get => _overallCpuUsagePercentage;
            set
            {
                _overallCpuUsagePercentage = value;
                OnPropertyChanged();
                OverallCpuUsageColor = GetUsageColor(value);

                // Apply smoothing and animate to new value
                var smoothedValue = ApplyTemporalSmoothing(value);
                AnimateProgressBar(smoothedValue);
            }
        }

        /// <summary>
        /// The displayed/animated value for the progress bar
        /// </summary>
        public float DisplayedUsagePercentage
        {
            get => _displayedUsagePercentage;
            set
            {
                if (Math.Abs(_displayedUsagePercentage - value) > 0.01f)
                {
                    _displayedUsagePercentage = value;
                    OnPropertyChanged();
                }
            }
        }

        public Brush OverallCpuUsageColor
        {
            get => _overallCpuUsageColor;
            set { _overallCpuUsageColor = value; OnPropertyChanged(); }
        }

        public ObservableCollection<CpuCoreInfo> CpuCores => _cpuCores;

        public float CpuTemperature
        {
            get => _cpuTemperature;
            set { _cpuTemperature = value; OnPropertyChanged(); }
        }

        public float CpuClockSpeed
        {
            get => _cpuClockSpeed;
            set { _cpuClockSpeed = value; OnPropertyChanged(); }
        }

        public float CpuPowerUsage
        {
            get => _cpuPowerUsage;
            set { _cpuPowerUsage = value; OnPropertyChanged(); }
        }

        public bool ShowCores
        {
            get => _settings.ShowCores;
            set
            {
                _settings.ShowCores = value;
                OnPropertyChanged();
                SavePageSettings(_settings);
            }
        }

        public bool ShowTemperature
        {
            get => _settings.ShowTemperature;
            set
            {
                _settings.ShowTemperature = value;
                OnPropertyChanged();
                SavePageSettings(_settings);
            }
        }

        public bool UseFahrenheit
        {
            get => _settings.UseFahrenheit;
            set
            {
                _settings.UseFahrenheit = value;
                OnPropertyChanged();
                SavePageSettings(_settings);
            }
        }

        public double TemperatureFontSize
        {
            get => _settings.TemperatureFontSize;
            set
            {
                _settings.TemperatureFontSize = value;
                OnPropertyChanged();
                SavePageSettings(_settings);
            }
        }

        public bool ShowClockSpeed
        {
            get => _settings.ShowClockSpeed;
            set
            {
                _settings.ShowClockSpeed = value;
                OnPropertyChanged();
                SavePageSettings(_settings);
            }
        }

        public bool ShowPowerUsage
        {
            get => _settings.ShowPowerUsage;
            set
            {
                _settings.ShowPowerUsage = value;
                OnPropertyChanged();
                SavePageSettings(_settings);
            }
        }

        public double UpdateIntervalSeconds
        {
            get => _settings.UpdateIntervalSeconds;
            set
            {
                _settings.UpdateIntervalSeconds = value;
                OnPropertyChanged();
                RestartUpdateTimer(value);
                SavePageSettings(_settings);
            }
        }

        public CpuWidget()
        {
            try
            {
                InitializeComponent();
                Console.WriteLine($"🔧 CPU Widget XAML initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CPU Widget XAML initialization failed: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                // During plugin discovery, XAML initialization might fail
                // This is expected and the widget will be properly initialized later
            }
        }

        protected override void InitializeWidget()
        {
            _hardwareService = _host?.ServiceProvider.GetService<IPluginHardwareMonitorService>();
            if (_hardwareService == null)
            {
                Log("Hardware monitoring service not available", LogLevel.Warning);
                return;
            }

            LoadSettings();
            StartUpdateTimer(_settings.UpdateIntervalSeconds);
            RefreshData();
        }

        protected override void ShutdownWidget()
        {
            StopUpdateTimer();
        }

        public override void RefreshData()
        {
            if (_hardwareService == null) return;

            try
            {
                _hardwareService.Update();

                if (CpuName == "Loading CPU info...")
                {
                    CpuName = _hardwareService.GetCpuName() ?? "Unknown CPU";
                }

                OverallCpuUsagePercentage = _hardwareService.GetOverallCpuUsage();
                CpuTemperature = _hardwareService.GetCpuPackageTemperature();

                try
                {
                    var clockSpeed = _hardwareService.GetCpuClockSpeed();
                    if (clockSpeed > 0) CpuClockSpeed = clockSpeed;
                }
                catch (Exception ex)
                {
                    Log($"Failed to get CPU clock speed: {ex.Message}", LogLevel.Debug);
                }

                try
                {
                    var powerUsage = _hardwareService.GetCpuPowerUsage();
                    if (powerUsage > 0) CpuPowerUsage = powerUsage;
                }
                catch (Exception ex)
                {
                    Log($"Failed to get CPU power usage: {ex.Message}", LogLevel.Debug);
                }

                if (ShowCores)
                {
                    UpdateCpuCores();
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing CPU data: {ex.Message}", LogLevel.Error, ex);
            }
        }

        public override FrameworkElement? CreateSettingsUI()
        {
            var settingsClone = _settings.Clone() as CpuWidgetSettings ?? new CpuWidgetSettings();
            var settingsView = new CpuWidgetSettingsView(settingsClone);

            // Subscribe to settings events to apply changes back to the widget
            if (settingsView.DataContext is CpuWidgetSettingsViewModel viewModel)
            {
                viewModel.SettingsSaved += OnSettingsSaved;
                viewModel.SettingsReset += OnSettingsReset;
            }

            return settingsView;
        }

        private void OnSettingsSaved(object? sender, SettingsEventArgs e)
        {
            if (e.Settings is CpuWidgetSettings newSettings)
            {
                // Apply the new settings to the widget
                _settings = newSettings;

                // Update all bound properties
                OnPropertyChanged(nameof(ShowCores));
                OnPropertyChanged(nameof(ShowTemperature));
                OnPropertyChanged(nameof(UseFahrenheit));
                OnPropertyChanged(nameof(TemperatureFontSize));
                OnPropertyChanged(nameof(ShowClockSpeed));
                OnPropertyChanged(nameof(ShowPowerUsage));
                OnPropertyChanged(nameof(UpdateIntervalSeconds));

                // Restart timer with new interval if it changed
                RestartUpdateTimer(_settings.UpdateIntervalSeconds);

                // Save settings to disk (page-specific)
                SavePageSettings(_settings);

                Log($"Settings applied: ShowCores={ShowCores}, ShowTemp={ShowTemperature}, UpdateInterval={UpdateIntervalSeconds}s", LogLevel.Info);
            }
        }

        private void OnSettingsReset(object? sender, EventArgs e)
        {
            // Reset to default settings
            _settings.Reset();
            LoadSettings(); // Reload to refresh all property bindings
            Log("Settings reset to defaults", LogLevel.Info);
        }

        private void UpdateCpuCores()
        {
            if (_hardwareService == null) return;

            var coreUsages = _hardwareService.GetCpuCoreUsage();

            for (int i = 0; i < coreUsages.Count; i++)
            {
                if (i < CpuCores.Count)
                {
                    CpuCores[i].UsagePercentage = coreUsages[i];
                }
                else
                {
                    CpuCores.Add(new CpuCoreInfo { Name = $"Core {i + 1}", UsagePercentage = coreUsages[i] });
                }
            }

            while (CpuCores.Count > coreUsages.Count)
            {
                CpuCores.RemoveAt(CpuCores.Count - 1);
            }
        }

        private void LoadSettings()
        {
            // Try to load page-specific settings first, fall back to global settings
            var loadedSettings = LoadPageSettings<CpuWidgetSettings>();
            if (loadedSettings != null)
            {
                _settings = loadedSettings;
                OnPropertyChanged(nameof(ShowCores));
                OnPropertyChanged(nameof(ShowTemperature));
                OnPropertyChanged(nameof(UseFahrenheit));
                OnPropertyChanged(nameof(TemperatureFontSize));
                OnPropertyChanged(nameof(ShowClockSpeed));
                OnPropertyChanged(nameof(ShowPowerUsage));
                OnPropertyChanged(nameof(UpdateIntervalSeconds));
            }
        }

        protected override void OnPageSettingsChanged(string pageId)
        {
            // Reload settings when page changes to get page-specific configuration
            LoadSettings();
            Log($"Loaded settings for page {pageId}: ShowCores={ShowCores}, ShowTemp={ShowTemperature}, UpdateInterval={UpdateIntervalSeconds}s", LogLevel.Info);
        }

        /// <summary>
        /// Animates the progress bar to a new value using WPF's smooth animation system
        /// </summary>
        private void AnimateProgressBar(float targetValue)
        {
            try
            {
                var progressBar = this.FindName("OverallCpuProgressBar") as ProgressBar;
                if (progressBar == null) return;

                // Calculate animation duration - longer = smoother, shorter = more responsive
                // Use 80% of update interval for smooth transitions
                var durationMs = Math.Max(200, _settings.UpdateIntervalSeconds * 800);

                // Use WPF's DoubleAnimation for hardware-accelerated smooth animation
                var animation = new DoubleAnimation
                {
                    From = progressBar.Value,
                    To = targetValue,
                    Duration = TimeSpan.FromMilliseconds(durationMs),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                // Apply the animation to the progress bar's Value property
                progressBar.BeginAnimation(ProgressBar.ValueProperty, animation);

                // Also update the displayed value for any other bindings
                DisplayedUsagePercentage = targetValue;
            }
            catch (Exception ex)
            {
                Log($"Error animating progress bar: {ex.Message}", LogLevel.Debug);
            }
        }

        /// <summary>
        /// Applies exponential moving average smoothing to reduce visual noise
        /// </summary>
        private float ApplyTemporalSmoothing(float newValue)
        {
            _valueBuffer.Enqueue(newValue);
            while (_valueBuffer.Count > BufferSize)
                _valueBuffer.Dequeue();

            if (_valueBuffer.Count == 0)
                return newValue;

            // Exponential weighted average - most recent values have highest weight
            var values = _valueBuffer.ToArray();
            float alpha = 0.4f; // Smoothing factor (0 = very smooth, 1 = no smoothing)
            float smoothed = values[0];

            for (int i = 1; i < values.Length; i++)
            {
                smoothed = alpha * values[i] + (1 - alpha) * smoothed;
            }

            return smoothed;
        }
    }

    public class CpuCoreInfo : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private float _usagePercentage;
        private float _animatedUsagePercentage;
        private Brush _usageColor = Brushes.LimeGreen;
        private DispatcherTimer? _animationTimer;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public float UsagePercentage
        {
            get => _usagePercentage;
            set
            {
                var oldValue = _usagePercentage;
                _usagePercentage = value;
                OnPropertyChanged();
                UpdateColor();
                OnPropertyChanged(nameof(Percentage));

                // Animate to the new value
                AnimateToValue(oldValue, value);
            }
        }

        /// <summary>
        /// Animated value for smooth progress bar transitions
        /// </summary>
        public float AnimatedUsagePercentage
        {
            get => _animatedUsagePercentage;
            set
            {
                _animatedUsagePercentage = value;
                OnPropertyChanged();
            }
        }

        public string Percentage => $"{UsagePercentage:F1}%";

        public Brush UsageColor
        {
            get => _usageColor;
            set { _usageColor = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void UpdateColor()
        {
            UsageColor = UsagePercentage switch
            {
                < 60 => Brushes.LimeGreen,
                < 85 => Brushes.Orange,
                _ => Brushes.Red
            };
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Animates the usage percentage smoothly from old to new value
        /// </summary>
        private void AnimateToValue(float fromValue, float toValue)
        {
            try
            {
                // Cancel any existing animation
                _animationTimer?.Stop();

                var startTime = DateTime.Now;
                var duration = TimeSpan.FromMilliseconds(400); // Longer for smoother transitions
                var startValue = _animatedUsagePercentage; // Start from current animated position
                var targetValue = toValue;
                var valueRange = targetValue - startValue;

                // Skip animation if change is very small
                if (Math.Abs(valueRange) < 0.5f)
                {
                    AnimatedUsagePercentage = targetValue;
                    return;
                }

                _animationTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
                };

                _animationTimer.Tick += (s, e) =>
                {
                    var elapsed = DateTime.Now - startTime;
                    var progress = Math.Min(1.0, elapsed.TotalMilliseconds / duration.TotalMilliseconds);

                    // Easing function (ease out cubic for smooth deceleration)
                    var easedProgress = 1 - Math.Pow(1 - progress, 3);

                    var currentValue = startValue + (valueRange * (float)easedProgress);
                    AnimatedUsagePercentage = currentValue;

                    if (progress >= 1.0)
                    {
                        _animationTimer?.Stop();
                        AnimatedUsagePercentage = targetValue; // Ensure we end exactly at target
                    }
                };

                _animationTimer.Start();
            }
            catch
            {
                // Fallback to immediate value if animation fails
                AnimatedUsagePercentage = toValue;
            }
        }
    }
}