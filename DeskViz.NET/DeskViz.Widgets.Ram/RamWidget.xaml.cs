using System;
using System.ComponentModel;
using System.Diagnostics;
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

namespace DeskViz.Widgets.Ram
{
    public partial class RamWidget : BaseWidget
    {
        private IPluginHardwareMonitorService? _hardwareService;
        private RamWidgetSettings _settings = new();

        private float _ramUsagePercentage = 0f;
        private Brush _ramUsageColor = Brushes.LimeGreen;
        private double _totalRamGb = 0;
        private double _usedRamGb = 0;
        private double _freeRamGb = 0;
        private double _pageFileUsedGb = 0;

        // Fluid animation system
        private DispatcherTimer? _interpolationTimer;
        private DateTime _lastUpdateTime;
        private float _currentInterpolatedValue = 0f;
        private float _targetValue = 0f;
        private readonly Queue<float> _valueBuffer = new Queue<float>();
        private const int BufferSize = 3;
        private const int InterpolationFps = 60; // 60 FPS for smooth animation
        private const float SmoothingFactor = 0.7f; // Temporal smoothing factor

        public override IWidgetMetadata Metadata { get; } = new WidgetMetadata
        {
            Id = "RamWidget",
            Name = "RAM Monitor",
            Description = "Monitors RAM usage, total/available memory display, and page file information",
            Author = "DeskViz Team",
            Version = new Version(2, 0, 0),
            Category = "Hardware",
            Tags = new[] { "ram", "memory", "performance", "monitoring", "hardware" },
            RequiresElevatedPermissions = false,
            MinimumHostVersion = new Version(1, 0, 0)
        };

        public override string WidgetId => "RamWidget";
        public override string DisplayName => "RAM Monitor";

        public float RamUsagePercentage
        {
            get => _ramUsagePercentage;
            set
            {
                var oldValue = _ramUsagePercentage;
                _ramUsagePercentage = value;
                OnPropertyChanged();
                RamUsageColor = GetUsageColor(value);

                // Start fluid interpolation to new value
                StartFluidInterpolation(value);
            }
        }

        public Brush RamUsageColor
        {
            get => _ramUsageColor;
            set { _ramUsageColor = value; OnPropertyChanged(); }
        }

        public double TotalRamGb
        {
            get => _totalRamGb;
            set
            {
                _totalRamGb = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalRamFormatted));
            }
        }

        public string TotalRamFormatted => $"{TotalRamGb:F1} GB";

        public double UsedRamGb
        {
            get => _usedRamGb;
            set
            {
                _usedRamGb = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UsedRamFormatted));
            }
        }

        public string UsedRamFormatted => $"{UsedRamGb:F1} GB";

        public double FreeRamGb
        {
            get => _freeRamGb;
            set
            {
                _freeRamGb = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FreeRamFormatted));
            }
        }

        public string FreeRamFormatted => $"{FreeRamGb:F1} GB";

        public double PageFileUsedGb
        {
            get => _pageFileUsedGb;
            set
            {
                _pageFileUsedGb = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PageFileUsedFormatted));
            }
        }

        public string PageFileUsedFormatted => $"{PageFileUsedGb:F1} GB";

        public bool ShowPageFileInfo
        {
            get => _settings.ShowPageFileInfo;
            set
            {
                _settings.ShowPageFileInfo = value;
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

        public RamWidget()
        {
            try
            {
                InitializeComponent();
                Console.WriteLine($"🔧 RAM Widget XAML initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ RAM Widget XAML initialization failed: {ex.Message}");
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
            _interpolationTimer?.Stop();
        }

        public override void RefreshData()
        {
            if (_hardwareService == null) return;

            try
            {
                _hardwareService.Update();

                if (!_hardwareService.IsInitialized)
                {
                    Log("Hardware monitoring service not initialized", LogLevel.Debug);
                    TotalRamGb = 0;
                    UsedRamGb = 0;
                    FreeRamGb = 0;
                    RamUsagePercentage = 0;
                    return;
                }

                // Get RAM usage information
                var (totalRamMb, availableRamMb) = _hardwareService.GetRamInfo();
                var usedRamMb = _hardwareService.GetUsedRamMB();

                TotalRamGb = totalRamMb / 1024.0; // Convert MB to GB
                UsedRamGb = usedRamMb / 1024.0; // Convert MB to GB
                FreeRamGb = availableRamMb / 1024.0; // Convert MB to GB

                // Get percentage directly from the service
                RamUsagePercentage = _hardwareService.GetRamUsagePercentage();

                Log($"RAM updated: Total={TotalRamGb:F1} GB, Used={UsedRamGb:F1} GB, Free={FreeRamGb:F1} GB, Usage={RamUsagePercentage:F1}%", LogLevel.Debug);

                // TODO: Page file info needs implementation when available in hardware service
                // if (ShowPageFileInfo)
                // {
                //     PageFileUsedGb = _hardwareService.GetPageFileUsed() / 1024.0;
                // }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing RAM data: {ex.Message}", LogLevel.Error, ex);
                // Set properties to error state
                TotalRamGb = 0;
                UsedRamGb = 0;
                FreeRamGb = 0;
                RamUsagePercentage = 0;
            }
        }

        public override FrameworkElement? CreateSettingsUI()
        {
            var settingsClone = _settings.Clone() as RamWidgetSettings ?? new RamWidgetSettings();
            var settingsView = new RamWidgetSettingsView(settingsClone);

            // Subscribe to settings events to apply changes back to the widget
            if (settingsView.DataContext is RamWidgetSettingsViewModel viewModel)
            {
                viewModel.SettingsSaved += OnSettingsSaved;
                viewModel.SettingsReset += OnSettingsReset;
            }

            return settingsView;
        }

        private void OnSettingsSaved(object? sender, SettingsEventArgs e)
        {
            if (e.Settings is RamWidgetSettings newSettings)
            {
                // Apply the new settings to the widget
                _settings = newSettings;

                // Update all bound properties
                OnPropertyChanged(nameof(ShowPageFileInfo));
                OnPropertyChanged(nameof(UpdateIntervalSeconds));

                // Restart timer with new interval if it changed
                RestartUpdateTimer(_settings.UpdateIntervalSeconds);

                // Save settings to disk (page-specific)
                SavePageSettings(_settings);

                Log($"Settings applied: ShowPageFile={ShowPageFileInfo}, UpdateInterval={UpdateIntervalSeconds}s", LogLevel.Info);
            }
        }

        private void OnSettingsReset(object? sender, EventArgs e)
        {
            // Reset to default settings
            _settings.Reset();
            LoadSettings(); // Reload to refresh all property bindings
            Log("Settings reset to defaults", LogLevel.Info);
        }

        private void LoadSettings()
        {
            // Try to load page-specific settings first, fall back to global settings
            var loadedSettings = LoadPageSettings<RamWidgetSettings>();
            if (loadedSettings != null)
            {
                _settings = loadedSettings;
                OnPropertyChanged(nameof(ShowPageFileInfo));
                OnPropertyChanged(nameof(UpdateIntervalSeconds));
            }
        }

        protected override void OnPageSettingsChanged(string pageId)
        {
            // Reload settings when page changes to get page-specific configuration
            LoadSettings();
            Log($"Loaded settings for page {pageId}: ShowPageFile={ShowPageFileInfo}, UpdateInterval={UpdateIntervalSeconds}s", LogLevel.Info);
        }

        /// <summary>
        /// Starts fluid interpolation system for smooth, continuous animations
        /// </summary>
        private void StartFluidInterpolation(float newTargetValue)
        {
            try
            {
                // Apply temporal smoothing to reduce spikes
                var smoothedValue = ApplyTemporalSmoothing(newTargetValue);

                _targetValue = smoothedValue;
                _lastUpdateTime = DateTime.Now;

                // Stop any existing interpolation
                _interpolationTimer?.Stop();

                // Find the progress bar
                var progressBar = this.FindName("RamUsageProgressBar") as ProgressBar;
                if (progressBar == null) return;

                // Initialize current value if first run
                if (_currentInterpolatedValue == 0f)
                {
                    _currentInterpolatedValue = (float)progressBar.Value;
                }

                // Calculate animation duration based on update interval (80% of interval)
                var animationDuration = _settings.UpdateIntervalSeconds * 800; // 80% in milliseconds
                var frameRate = 1000 / InterpolationFps; // Frame interval in ms

                // Start continuous interpolation timer
                _interpolationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(frameRate) };
                _interpolationTimer.Tick += (s, e) => UpdateFluidInterpolation(progressBar, animationDuration);
                _interpolationTimer.Start();
            }
            catch (Exception ex)
            {
                Log($"Error starting fluid interpolation: {ex.Message}", LogLevel.Debug);
            }
        }

        /// <summary>
        /// Updates the fluid interpolation animation frame
        /// </summary>
        private void UpdateFluidInterpolation(ProgressBar progressBar, double animationDurationMs)
        {
            try
            {
                var elapsed = DateTime.Now - _lastUpdateTime;
                var progress = Math.Min(1.0, elapsed.TotalMilliseconds / animationDurationMs);

                // Smooth cubic easing for natural movement
                var easedProgress = EaseInOutCubic(progress);

                // Calculate current interpolated value
                var valueRange = _targetValue - _currentInterpolatedValue;
                var currentDisplayValue = _currentInterpolatedValue + (valueRange * (float)easedProgress);

                // Apply to progress bar
                progressBar.Value = currentDisplayValue;

                // Stop when animation is complete
                if (progress >= 1.0)
                {
                    _interpolationTimer?.Stop();
                    _currentInterpolatedValue = _targetValue; // Ensure we end exactly at target
                    progressBar.Value = _targetValue;
                }
            }
            catch (Exception ex)
            {
                Log($"Error updating fluid interpolation: {ex.Message}", LogLevel.Debug);
                _interpolationTimer?.Stop();
            }
        }

        /// <summary>
        /// Applies temporal smoothing to reduce spikes and provide stable values
        /// </summary>
        private float ApplyTemporalSmoothing(float newValue)
        {
            _valueBuffer.Enqueue(newValue);
            if (_valueBuffer.Count > BufferSize)
                _valueBuffer.Dequeue();

            // Weighted average (more recent = higher weight)
            var weights = new[] { 0.6f, 0.3f, 0.1f };
            var values = _valueBuffer.ToArray();

            float smoothed = 0;
            for (int i = 0; i < values.Length; i++)
            {
                var weightIndex = Math.Min(values.Length - 1 - i, weights.Length - 1);
                smoothed += values[i] * weights[weightIndex];
            }

            return smoothed;
        }

        /// <summary>
        /// Cubic easing function for natural acceleration and deceleration
        /// </summary>
        private static double EaseInOutCubic(double t)
        {
            return t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
        }
    }
}