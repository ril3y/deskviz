using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using DeskViz.Plugins.Base;
using DeskViz.Plugins.Interfaces;

namespace DeskViz.Widgets.Clock
{
    public partial class ClockWidget : BaseWidget
    {
        private ClockWidgetSettings _settings = new();
        private string _currentTime = string.Empty;

        public override IWidgetMetadata Metadata { get; } = new WidgetMetadata
        {
            Id = "ClockWidget",
            Name = "Clock",
            Description = "Displays current time with customizable format and font size",
            Author = "DeskViz Team",
            Version = new Version(2, 0, 0),
            Category = "System",
            Tags = new[] { "time", "clock", "display" },
            RequiresElevatedPermissions = false,
            MinimumHostVersion = new Version(1, 0, 0)
        };

        public override string WidgetId => "ClockWidget";
        public override string DisplayName => "Clock";

        public string CurrentTime
        {
            get => _currentTime;
            private set
            {
                if (_currentTime != value)
                {
                    _currentTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Is24HourFormat
        {
            get => _settings.Is24HourFormat;
            set
            {
                if (_settings.Is24HourFormat != value)
                {
                    _settings.Is24HourFormat = value;
                    OnPropertyChanged();
                    SavePageSettings(_settings);
                    UpdateTime(); // Immediately update display
                }
            }
        }

        public double ClockFontSize
        {
            get => _settings.ClockFontSize;
            set
            {
                if (Math.Abs(_settings.ClockFontSize - value) > 0.001)
                {
                    _settings.ClockFontSize = value;
                    OnPropertyChanged();
                    SavePageSettings(_settings);
                }
            }
        }

        public double UpdateIntervalSeconds
        {
            get => _settings.UpdateIntervalSeconds;
            set
            {
                if (Math.Abs(_settings.UpdateIntervalSeconds - value) > 0.001)
                {
                    _settings.UpdateIntervalSeconds = value;
                    OnPropertyChanged();
                    RestartUpdateTimer(value);
                    SavePageSettings(_settings);
                }
            }
        }

        public ClockWidget()
        {
            try
            {
                InitializeComponent();
                Console.WriteLine($"🔧 Clock Widget XAML initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Clock Widget XAML initialization failed: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                // During plugin discovery, XAML initialization might fail
                // This is expected and the widget will be properly initialized later
            }
        }

        protected override void InitializeWidget()
        {
            LoadSettings();
            StartUpdateTimer(_settings.UpdateIntervalSeconds);
            UpdateTime(); // Set initial time
        }

        protected override void ShutdownWidget()
        {
            StopUpdateTimer();
        }

        public override void RefreshData()
        {
            UpdateTime();
        }

        public override FrameworkElement? CreateSettingsUI()
        {
            var settingsClone = _settings.Clone() as ClockWidgetSettings ?? new ClockWidgetSettings();
            var settingsView = new ClockWidgetSettingsView(settingsClone);

            // Subscribe to settings events to apply changes back to the widget
            if (settingsView.DataContext is ClockWidgetSettingsViewModel viewModel)
            {
                viewModel.SettingsSaved += OnSettingsSaved;
                viewModel.SettingsReset += OnSettingsReset;
            }

            return settingsView;
        }

        private void OnSettingsSaved(object? sender, SettingsEventArgs e)
        {
            if (e.Settings is ClockWidgetSettings newSettings)
            {
                // Apply the new settings to the widget
                _settings = newSettings;

                // Update all bound properties
                OnPropertyChanged(nameof(Is24HourFormat));
                OnPropertyChanged(nameof(ClockFontSize));
                OnPropertyChanged(nameof(UpdateIntervalSeconds));

                // Restart timer with new interval if it changed
                RestartUpdateTimer(_settings.UpdateIntervalSeconds);

                // Save settings to disk (page-specific)
                SavePageSettings(_settings);

                // Update time display immediately
                UpdateTime();

                Log($"Settings applied: 24HFormat={Is24HourFormat}, FontSize={ClockFontSize}, UpdateInterval={UpdateIntervalSeconds}s", LogLevel.Info);
            }
        }

        private void OnSettingsReset(object? sender, EventArgs e)
        {
            // Reset to default settings
            _settings.Reset();
            LoadSettings(); // Reload to refresh all property bindings
            UpdateTime(); // Update display immediately
            Log("Settings reset to defaults", LogLevel.Info);
        }

        private void UpdateTime()
        {
            var format = Is24HourFormat ? "HH:mm:ss" : "hh:mm:ss tt";
            CurrentTime = DateTime.Now.ToString(format);
        }

        private void LoadSettings()
        {
            // Try to load page-specific settings first, fall back to global settings
            var loadedSettings = LoadPageSettings<ClockWidgetSettings>();
            if (loadedSettings != null)
            {
                _settings = loadedSettings;
                OnPropertyChanged(nameof(Is24HourFormat));
                OnPropertyChanged(nameof(ClockFontSize));
                OnPropertyChanged(nameof(UpdateIntervalSeconds));
            }
        }

        protected override void OnPageSettingsChanged(string pageId)
        {
            // Reload settings when page changes to get page-specific configuration
            LoadSettings();
            UpdateTime(); // Update display with new settings
            Log($"Loaded settings for page {pageId}: 24HFormat={Is24HourFormat}, FontSize={ClockFontSize}, UpdateInterval={UpdateIntervalSeconds}s", LogLevel.Info);
        }
    }
}