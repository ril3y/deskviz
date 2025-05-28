using DeskViz.Core.Services; // Keep ONLY for SettingsService
using Microsoft.Win32; // For OpenFileDialog
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DeskViz.App.Widgets
{
    /// <summary>
    /// Interaction logic for LogoWidget.xaml
    /// </summary>
    public partial class LogoWidget : System.Windows.Controls.UserControl, IWidget, INotifyPropertyChanged
    {
        private readonly SettingsService _settingsService;
        private BitmapImage? _imageSource;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // IWidget Implementation
        public string WidgetId => "LogoWidget";
        public string WidgetName => "Logo Display";
        public string WidgetDescription => "Displays a custom image from your filesystem.";
        public string DisplayName => "Logo Display";

        private bool _isWidgetVisible = true;
        public bool IsWidgetVisible
        {
            get => _isWidgetVisible;
            set
            {
                if (_isWidgetVisible != value)
                {
                    _isWidgetVisible = value;
                    OnPropertyChanged(nameof(IsWidgetVisible));
                    // Optional: Add logic here if visibility change needs immediate action
                }
            }
        }

        // Properties bound to UI or used in Settings
        public BitmapImage? ImageSource
        {
            get => _imageSource;
            private set { _imageSource = value; OnPropertyChanged(nameof(ImageSource)); }
        }

        private string _logoImagePath = string.Empty;
        public string LogoImagePath
        {
            get => _logoImagePath;
            set
            {
                if (_logoImagePath != value)
                {
                    _logoImagePath = value;
                    OnPropertyChanged(nameof(LogoImagePath));
                    LoadImage(); // Reload image when path changes
                }
            }
        }

        private double? _logoWidth;
        public double? LogoWidth
        {
            get => _logoWidth;
            set { _logoWidth = value; OnPropertyChanged(nameof(LogoWidth)); }
        }

        private double? _logoHeight;
        public double? LogoHeight
        {
            get => _logoHeight;
            set { _logoHeight = value; OnPropertyChanged(nameof(LogoHeight)); }
        }

        private System.Windows.Media.Stretch _logoStretch = System.Windows.Media.Stretch.Uniform;
        public System.Windows.Media.Stretch LogoStretch
        {
            get => _logoStretch;
            set { _logoStretch = value; OnPropertyChanged(nameof(LogoStretch)); }
        }

        private System.Windows.HorizontalAlignment _logoHorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        public System.Windows.HorizontalAlignment LogoHorizontalAlignment
        {
            get => _logoHorizontalAlignment;
            set { _logoHorizontalAlignment = value; OnPropertyChanged(nameof(LogoHorizontalAlignment)); }
        }

        private System.Windows.VerticalAlignment _logoVerticalAlignment = System.Windows.VerticalAlignment.Center;
        public System.Windows.VerticalAlignment LogoVerticalAlignment
        {
            get => _logoVerticalAlignment;
            set { _logoVerticalAlignment = value; OnPropertyChanged(nameof(LogoVerticalAlignment)); }
        }

        // Constructor
        public LogoWidget(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            DataContext = this; // Bindings in LogoWidget.xaml use this instance
            LoadSettings();
        }

        // Load settings from the service
        private void LoadSettings()
        {
            var settings = _settingsService.Settings;
            LogoImagePath = settings.LogoImagePath;
            LogoWidth = settings.LogoWidth;
            LogoHeight = settings.LogoHeight;

            // Safely parse enums from string settings
            LogoStretch = Enum.TryParse<System.Windows.Media.Stretch>(settings.LogoStretch, out var stretch) ? stretch : System.Windows.Media.Stretch.Uniform;
            LogoHorizontalAlignment = Enum.TryParse<System.Windows.HorizontalAlignment>(settings.LogoHorizontalAlignment, out var hAlign) ? hAlign : System.Windows.HorizontalAlignment.Center;
            LogoVerticalAlignment = Enum.TryParse<System.Windows.VerticalAlignment>(settings.LogoVerticalAlignment, out var vAlign) ? vAlign : System.Windows.VerticalAlignment.Center;
        }

        // Load the actual image file
        private void LoadImage()
        {
            // Check if the path string itself is valid, not necessarily if the file exists on disk
            if (!string.IsNullOrEmpty(LogoImagePath))
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(LogoImagePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Ensure image is loaded fully
                    bitmap.EndInit();
                    ImageSource = bitmap;
                    Debug.WriteLine($"LogoWidget: Image loaded from {LogoImagePath}");
                }
                catch (Exception ex)
                {
                    ImageSource = null;
                    Debug.WriteLine($"LogoWidget: Error loading image '{LogoImagePath}'. Exception: {ex.Message}");
                    // Consider showing an error indicator on the widget
                }
            }
            else
            {
                ImageSource = null;
                Debug.WriteLine("LogoWidget: Image path is empty.");
            }
        }

        // Open the settings dialog for this widget
        public void OpenWidgetSettings()
        {
            var settingsViewModel = new LogoWidgetSettingsViewModel
            {
                ImagePath = this.LogoImagePath,
                ImageWidth = this.LogoWidth,
                ImageHeight = this.LogoHeight,
                SelectedStretch = this.LogoStretch,
                SelectedHorizontalAlignment = this.LogoHorizontalAlignment,
                SelectedVerticalAlignment = this.LogoVerticalAlignment
            };

            var settingsControl = new LogoWidgetSettings { DataContext = settingsViewModel };

            var hostWindow = new Window
            {
                Title = $"{WidgetName} Settings",
                Content = settingsControl,
                Width = 450,
                Height = 400, // Adjusted size
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize
            };

            if (hostWindow.ShowDialog() == true)
            {
                // OK was clicked - Update widget properties from ViewModel
                this.LogoImagePath = settingsViewModel.ImagePath;
                this.LogoWidth = settingsViewModel.ImageWidth;
                this.LogoHeight = settingsViewModel.ImageHeight;
                this.LogoStretch = settingsViewModel.SelectedStretch;
                this.LogoHorizontalAlignment = settingsViewModel.SelectedHorizontalAlignment;
                this.LogoVerticalAlignment = settingsViewModel.SelectedVerticalAlignment;

                // Save to global settings
                _settingsService.Settings.LogoImagePath = this.LogoImagePath;
                _settingsService.Settings.LogoWidth = this.LogoWidth;
                _settingsService.Settings.LogoHeight = this.LogoHeight;
                _settingsService.Settings.LogoStretch = this.LogoStretch.ToString();
                _settingsService.Settings.LogoHorizontalAlignment = this.LogoHorizontalAlignment.ToString();
                _settingsService.Settings.LogoVerticalAlignment = this.LogoVerticalAlignment.ToString();
                _settingsService.SaveSettings();

                Debug.WriteLine("LogoWidget settings saved and applied.");
                // LoadImage() is called automatically by the LogoImagePath setter if it changed.
            }
            else
            {
                // Cancel was clicked - ViewModel changes are discarded
                Debug.WriteLine("LogoWidget settings canceled.");
                // No need to reload settings here, as the widget's properties were not changed
            }
        }

        // Event handler for the context menu 'Settings...' click
        private void LogoSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("LogoWidget: LogoSettingsMenuItem_Click handler executed.");
            ConfigButtonClicked?.Invoke(this, EventArgs.Empty);
        }

        // Interface implementation
        public event EventHandler? ConfigButtonClicked;

        // Explicit implementation to avoid potential naming conflicts if a method named 'RefreshData' already exists
        void IWidget.RefreshData() 
        { 
            // Nothing to refresh periodically for the logo itself, 
            // but the method is required by the interface.
            // Image is loaded on init and when settings change.
            // Consider if a file watcher would be needed in the future.
        }
    }
}
