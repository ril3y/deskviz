using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DeskViz.Plugins.Interfaces;

namespace DeskViz.Plugins.Base
{
    public abstract class BaseWidget : UserControl, IWidgetPlugin
    {
        protected IWidgetHost? _host;
        protected DispatcherTimer? _updateTimer;
        protected string? _currentPageId = null;

        private bool _isWidgetVisible = true;
        private bool _isConfiguring = false;
        private ICommand? _configureWidgetCommand;

        public abstract IWidgetMetadata Metadata { get; }
        public abstract string WidgetId { get; }
        public abstract string DisplayName { get; }

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
                    OnVisibilityChanged(value);
                }
            }
        }

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

        public event EventHandler? ConfigButtonClicked;

        public ICommand? ConfigureWidgetCommand
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected BaseWidget()
        {
            DataContext = this;
        }

        public virtual void Initialize(IWidgetHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            InitializeWidget();
        }

        public virtual void Shutdown()
        {
            _updateTimer?.Stop();
            _updateTimer = null;
            ShutdownWidget();
        }

        public abstract void RefreshData();

        public virtual void OpenWidgetSettings()
        {
            var settingsUI = CreateSettingsUI();
            if (settingsUI != null && _host != null)
            {
                ShowSettingsDialog(settingsUI);
            }
            else
            {
                _host?.ShowMessage("Settings", "No settings available for this widget.");
            }
        }

        public virtual FrameworkElement CreateWidgetUI()
        {
            return this;
        }

        public abstract FrameworkElement? CreateSettingsUI();

        public virtual void OnPageChanged(string pageId)
        {
            _currentPageId = pageId;
            // Derived widgets can override this to reload page-specific settings
            OnPageSettingsChanged(pageId);
        }

        /// <summary>
        /// Called when the page changes - derived widgets can override to reload settings
        /// </summary>
        protected virtual void OnPageSettingsChanged(string pageId)
        {
            // Default implementation does nothing
            // Derived widgets should override this to reload their settings
        }

        public virtual void OnVisibilityChanged(bool isVisible)
        {
        }

        protected virtual void InitializeWidget()
        {
        }

        protected virtual void ShutdownWidget()
        {
        }

        protected virtual void ShowSettingsDialog(FrameworkElement settingsUI)
        {
            var ownerWindow = Window.GetWindow(this) ?? Application.Current.MainWindow;

            var window = new Window
            {
                Title = $"{DisplayName} Settings",
                Content = settingsUI,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = ownerWindow
            };

            window.ShowDialog();
        }

        protected void StartUpdateTimer(double intervalSeconds)
        {
            _updateTimer?.Stop();
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(intervalSeconds)
            };
            _updateTimer.Tick += (s, e) => RefreshData();
            _updateTimer.Start();
        }

        protected void StopUpdateTimer()
        {
            _updateTimer?.Stop();
        }

        protected void RestartUpdateTimer(double intervalSeconds)
        {
            StartUpdateTimer(intervalSeconds);
        }

        protected Brush GetUsageColor(double percentage)
        {
            return percentage switch
            {
                > 90 => Brushes.Red,
                > 70 => Brushes.Orange,
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")!)
            };
        }

        protected T? LoadSettings<T>() where T : class, new()
        {
            return _host?.LoadWidgetSettings<T>(WidgetId);
        }

        protected void SaveSettings<T>(T settings) where T : class
        {
            _host?.SaveWidgetSettings(WidgetId, settings);
        }

        /// <summary>
        /// Loads settings for the current page. Falls back to global settings if no page-specific settings exist.
        /// </summary>
        protected T? LoadPageSettings<T>() where T : class, new()
        {
            if (_currentPageId != null && _host != null)
            {
                // Try to load page-specific settings first
                var pageSettings = _host.LoadWidgetSettingsForPage<T>(WidgetId, _currentPageId);
                if (pageSettings != null)
                {
                    return pageSettings;
                }
            }

            // Fall back to global settings
            return LoadSettings<T>();
        }

        /// <summary>
        /// Saves settings for the current page
        /// </summary>
        protected void SavePageSettings<T>(T settings) where T : class
        {
            if (_currentPageId != null && _host != null)
            {
                _host.SaveWidgetSettingsForPage(WidgetId, _currentPageId, settings);
            }
            else
            {
                // Fall back to global settings if no page context
                SaveSettings(settings);
            }
        }

        protected void Log(string message, LogLevel level = LogLevel.Info, Exception? exception = null)
        {
            if (_host == null) return;

            switch (level)
            {
                case LogLevel.Debug:
                    _host.LogDebug(WidgetId, message);
                    break;
                case LogLevel.Info:
                    _host.LogInfo(WidgetId, message);
                    break;
                case LogLevel.Warning:
                    _host.LogWarning(WidgetId, message);
                    break;
                case LogLevel.Error:
                    _host.LogError(WidgetId, message, exception);
                    break;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnConfigButtonClicked()
        {
            ConfigButtonClicked?.Invoke(this, EventArgs.Empty);
        }
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}