using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls; // Keep this for XAML interaction if needed, but qualify base class
using System.Windows.Threading;
using DeskViz.Core; // Correct namespace for IWidget
using DeskViz.Core.Services; // Add necessary using
using System.Windows; // Add this for RoutedEventArgs

namespace DeskViz.App.Widgets
{
    /// <summary>
    /// Interaction logic for ClockWidget.xaml
    /// </summary>
    public partial class ClockWidget : System.Windows.Controls.UserControl, IWidget, INotifyPropertyChanged // Fully qualify UserControl
    {
        private DispatcherTimer? _timer;
        private string _currentTime = string.Empty;
        private string _timeFormatString = "HH:mm:ss"; // Default to 24-hour format
        private double _clockFontSize = 24; // Default font size
        private SettingsService _settingsService; // Store the service

        public string WidgetId => "ClockWidget";
        public string DisplayName => "Clock";
        
        // Basic implementation for IWidget interface
        private bool _isConfiguring;
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
                    this.Visibility = value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                }
            }
        }

        public string TimeFormatString
        {
            get => _timeFormatString;
            set
            {
                if (_timeFormatString != value && (value == "HH:mm:ss" || value == "hh:mm:ss tt")) // Basic validation
                {
                    _timeFormatString = value;
                    OnPropertyChanged();
                    UpdateTime(); // Update display immediately
                }
            }
        }

        public bool Is24HourFormat
        {
            get => TimeFormatString == "HH:mm:ss";
            set
            {
                if (value != Is24HourFormat)
                {
                    TimeFormatString = value ? "HH:mm:ss" : "hh:mm:ss tt";
                    OnPropertyChanged(); 
                }
            }
        }

        public double ClockFontSize
        {
            get => _clockFontSize;
            set
            {
                if (_clockFontSize != value && value > 0) // Basic validation
                {
                    _clockFontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentTime
        {
            get => _currentTime;
            private set // Make setter private, only updated internally
            {
                if (_currentTime != value)
                {
                    _currentTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? ConfigButtonClicked; // Not used for Clock, but required by IWidget

        public ClockWidget(SettingsService settingsService) // Accept SettingsService
        {
            InitializeComponent();
            DataContext = this;
            _settingsService = settingsService; // Store it

            // Load settings
            Is24HourFormat = _settingsService.Settings.ClockIs24HourFormat;
            ClockFontSize = _settingsService.Settings.ClockFontSize;

            UpdateTime(); // Set initial time

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Set initial visibility based on property
             this.Visibility = IsWidgetVisible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateTime();
        }

        private void UpdateTime()
        {
             CurrentTime = DateTime.Now.ToString(TimeFormatString); // Use the format property
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        // Method to raise ConfigButtonClicked event (called by MainWindow or similar)
        protected virtual void OnConfigButtonClicked()
        {
            // In a real scenario, you might toggle IsConfiguring here
            // IsConfiguring = !IsConfiguring; 
            ConfigButtonClicked?.Invoke(this, EventArgs.Empty);
        } 

        // Event handler for the ContextMenu's Settings item
        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenWidgetSettings(); // Directly open this widget's settings dialog
        }

        // Method required by IWidget interface - Populates and shows the settings dialog
        public void OpenWidgetSettings()
        {
            // Create the settings UI control, passing this widget instance for DataContext
            var settingsControl = new ClockWidgetSettings
            {
                DataContext = this // Ensure DataContext is set to this widget instance
            };

            // Create a host window for the settings control
            var hostWindow = new Window
            {
                Title = "Clock Settings",
                Content = settingsControl,
                Width = 350,
                Height = 250, // Adjusted height for potential OK/Cancel buttons
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = System.Windows.Application.Current.MainWindow, // Set owner
                ResizeMode = ResizeMode.NoResize // Prevent resizing
                // We will add OK/Cancel buttons to ClockWidgetSettings.xaml later
                // And handle the DialogResult here
            };

            // Show the window as a dialog and wait for it to close
            bool? dialogResult = hostWindow.ShowDialog();

            // If the user confirmed the dialog (we'll assume true for now, will be set by OK button later)
            if (dialogResult == true)
            {
                // Update the settings in the service
                _settingsService.Settings.ClockIs24HourFormat = this.Is24HourFormat;
                _settingsService.Settings.ClockFontSize = this.ClockFontSize;

                // Save the updated settings
                _settingsService.SaveSettings();
            }
            else
            {
                // User cancelled, reload settings from service to revert any changes made in the UI
                Is24HourFormat = _settingsService.Settings.ClockIs24HourFormat;
                ClockFontSize = _settingsService.Settings.ClockFontSize;
            }
        }

        // Method required by IWidget interface - Clock updates automatically
        public void RefreshData()
        {
            // Clock updates via its internal timer, no external refresh needed.
        }
    }
}
