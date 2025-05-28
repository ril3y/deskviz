using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using DeskViz.App.Widgets;
using DeskViz.App.Widgets.GpuWidget;
using DeskViz.App.Widgets.MediaControlWidget;
using DeskViz.Core.Services;
using ScreenInfo = DeskViz.Core.Services.ScreenInfo;

namespace DeskViz.App.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ScreenService _screenService;
        private readonly SettingsService _settingsService;
        private readonly IHardwareMonitorService _hardwareMonitorService; // Added for DI
        private readonly IMediaControlService _mediaControlService; // Added for media control
        private List<IWidget> _allWidgets = new List<IWidget>();

        public MainWindow()
        {
            Debug.WriteLine("MainWindow constructor started.");

            // Initialize services early to satisfy nullable checks, even if InitComponent fails
            _settingsService = new SettingsService();
            _screenService = new ScreenService();
            _hardwareMonitorService = new LibreHardwareMonitorService(); // Create single instance
            _mediaControlService = new WindowsMediaControlService(); // Create media control service

            try
            {
                Debug.WriteLine("Calling InitializeComponent()...");
                InitializeComponent();
                Debug.WriteLine("InitializeComponent() finished.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("!!!! EXCEPTION DURING InitializeComponent() !!!!");
                Debug.WriteLine($"Exception: {ex.GetType().Name}");
                Debug.WriteLine($"Message: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                // Consider showing a MessageBox or logging to a file here as well
                System.Windows.MessageBox.Show($"Fatal error during window initialization: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Optionally, rethrow or shutdown
                // throw;
                System.Windows.Application.Current.Shutdown();
                return; // Prevent further execution in constructor if init fails
            }

            RegisterWidgets();
            
            Loaded += Window_Loaded;

            Debug.WriteLine("MainWindow constructor finished (services initialized).");
        }

        private void RegisterWidgets()
        {
            // Create CPU widget
            var cpuWidget = new CpuWidget(_hardwareMonitorService, _settingsService);
            cpuWidget.DataContext = cpuWidget; // Set DataContext
            cpuWidget.ConfigButtonClicked += Widget_ConfigButtonClicked;
            _allWidgets.Add(cpuWidget);
            // WidgetPanel.Children.Add(cpuWidget); // REMOVED direct add

            // Create Logo widget
            var logoWidget = new LogoWidget(_settingsService); // Pass SettingsService
            logoWidget.DataContext = logoWidget;
            logoWidget.ConfigButtonClicked += Widget_ConfigButtonClicked; // Use the generic handler
            _allWidgets.Add(logoWidget);

            // Create RAM widget
            var ramWidget = new RamWidget(_hardwareMonitorService, _settingsService); // Pass SettingsService
            ramWidget.DataContext = ramWidget; // Set DataContext
            ramWidget.ConfigButtonClicked += Widget_ConfigButtonClicked;
            _allWidgets.Add(ramWidget);
            // WidgetPanel.Children.Add(ramWidget); // REMOVED direct add
            
            // Create GPU widget
            var gpuWidget = new GpuWidget(_hardwareMonitorService, _settingsService);
            gpuWidget.DataContext = gpuWidget; // Set DataContext
            gpuWidget.ConfigButtonClicked += Widget_ConfigButtonClicked;
            _allWidgets.Add(gpuWidget);
            
            // Create Clock widget
            var clockWidget = new ClockWidget(_settingsService); // Pass SettingsService
            clockWidget.DataContext = clockWidget;
            clockWidget.ConfigButtonClicked += Widget_ConfigButtonClicked; // Although unused in ClockWidget, keep consistency
            _allWidgets.Add(clockWidget);
            
            // Create Media Control widget
            var mediaControlWidget = new MediaControlWidget(_mediaControlService, _settingsService);
            mediaControlWidget.DataContext = mediaControlWidget;
            mediaControlWidget.ConfigButtonClicked += Widget_ConfigButtonClicked;
            _allWidgets.Add(mediaControlWidget);

            /*
            // Create YouTube Music widget
            var musicWidget = new YouTubeMusicWidget();
            musicWidget.DataContext = musicWidget; // Set DataContext
            musicWidget.ConfigButtonClicked += Widget_ConfigButtonClicked;
            _allWidgets.Add(musicWidget);
            */

            // Apply widget visibility based on saved settings
            foreach (var widget in _allWidgets) 
            {
                bool isVisible = true;

                if (_settingsService.Settings.WidgetVisibility.TryGetValue(widget.WidgetId, out bool savedVisibility))
                {
                    isVisible = savedVisibility;
                }
                widget.IsWidgetVisible = isVisible;
                
                // Add visible widgets to the UI panel
                if (isVisible && widget is UIElement uiElement)
                {
                    if (uiElement is FrameworkElement frameworkElement)
                    {
                        frameworkElement.Margin = new Thickness(10, 5, 10, 10); 
                    }
                    WidgetPanel.Children.Add(uiElement);
                }
            }
            
            // Apply widget order based on saved settings
            // Reorder based on the saved widget order in settings
            var widgetsInOrder = _settingsService.Settings.WidgetOrder
                .Where(widgetId => _settingsService.Settings.WidgetVisibility.GetValueOrDefault(widgetId, true)) 
                .Select(widgetId => WidgetPanel.Children.OfType<FrameworkElement>().FirstOrDefault(w => w is IWidget && ((IWidget)w).WidgetId == widgetId))
                .Where(widget => widget != null)
                .ToList();

            // Reorder widgets
            foreach (var widget in widgetsInOrder)
            {
                int currentIndex = WidgetPanel.Children.IndexOf(widget);
                if (currentIndex >= 0)
                {
                    WidgetPanel.Children.RemoveAt(currentIndex);
                    WidgetPanel.Children.Add(widget);
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Window_Loaded started.");
            ScreenInfo? targetScreen = ApplyDisplaySettings();
            if (targetScreen != null)
            {
                MoveToScreen(targetScreen);
                ApplyFullscreen(targetScreen);
            }
            else
            {
                ApplyFullscreen();
            }
            Debug.WriteLine("Window_Loaded finished.");
        }

        /// <summary>
        /// Applies the saved display settings and returns the target screen.
        /// </summary>
        /// <returns>The ScreenInfo of the target display, or null if none found.</returns>
        private ScreenInfo? ApplyDisplaySettings()
        {
            ScreenInfo? targetScreen = null;
            string? savedIdentifier = _settingsService?.Settings.PreferredDisplayIdentifier;

            if (!string.IsNullOrEmpty(savedIdentifier))
            {
                targetScreen = _screenService?.GetScreenByIdentifier(savedIdentifier);
            }

            if (targetScreen == null)
            {
                targetScreen = _screenService?.GetPrimaryScreen();
            }

            if (targetScreen == null)
            {
                var screens = _screenService?.GetAllScreens();
                if (screens?.Any() == true) targetScreen = screens.First();
            }

            if (targetScreen == null)
            {
                System.Windows.MessageBox.Show("Could not determine target display.", "Display Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
            return targetScreen; // Return the determined screen
        }

        private void MoveToScreen(ScreenInfo screen)
        {
            this.Left = screen.Bounds.X;
            this.Top = screen.Bounds.Y;
        }

        /// <summary>
        /// Applies fullscreen mode based on settings
        /// </summary>
        /// <param name="targetScreen">The screen to apply fullscreen to. If null, attempts to determine the current screen.</param>
        private void ApplyFullscreen(ScreenInfo? targetScreen = null)
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            
            // If targetScreen is not provided, try to determine it
            if (targetScreen == null)
            {
                 targetScreen = GetCurrentScreen();
            }

            if (targetScreen != null)
            {
                _screenService?.ApplyTrueFullscreen(windowHandle, targetScreen);
                
                // Determine final WidgetPanel orientation based on setting AND screen
                var panelOrientation = System.Windows.Controls.Orientation.Horizontal; // Default
                var orientationSetting = _settingsService?.Settings.WidgetOrientation;

                if (orientationSetting == Core.Services.WidgetOrientationSetting.Auto)
                {
                    // Auto: Use screen orientation
                    panelOrientation = targetScreen.Orientation == Core.Services.ScreenOrientation.Portrait
                        ? System.Windows.Controls.Orientation.Vertical
                        : System.Windows.Controls.Orientation.Horizontal;
                }
                else
                {
                    // Manual override
                    panelOrientation = (orientationSetting == Core.Services.WidgetOrientationSetting.Vertical)
                        ? System.Windows.Controls.Orientation.Vertical
                        : System.Windows.Controls.Orientation.Horizontal;
                }
                
                WidgetPanel.Orientation = panelOrientation;
            }
            else
            {
                System.Windows.MessageBox.Show("Could not determine current screen for fullscreen.", "Fullscreen Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private ScreenInfo? GetCurrentScreen()
        {
            var windowLeft = this.Left;
            var windowTop = this.Top;

            var screens = _screenService?.GetAllScreens();
            if (screens != null)
            {
                foreach (var screen in screens)
                {
                    if (screen.Bounds.X <= windowLeft && windowLeft < screen.Bounds.X + screen.Bounds.Width &&
                        screen.Bounds.Y <= windowTop && windowTop < screen.Bounds.Y + screen.Bounds.Height)
                    {
                        return screen;
                    }
                }
            }

            return _screenService?.GetPrimaryScreen() ?? _screenService?.GetAllScreens().FirstOrDefault();
        }

        /// <summary>
        /// Applies widget visibility settings from the settings service
        /// </summary>
        public void ApplyWidgetVisibilitySettings()
        {
            // Apply to all widgets
            foreach (var widget in _allWidgets)
            {
                // Default to visible if no setting exists
                bool isVisible = true; 
                
                // Set visibility on the widget based on saved setting
                if (_settingsService.Settings.WidgetVisibility.TryGetValue(widget.WidgetId, out bool savedVisibility))
                {
                    isVisible = savedVisibility;
                }
                widget.IsWidgetVisible = isVisible;
                
                // If the widget is a UIElement, update its presence in the panel
                if (widget is UIElement uiElement)
                {
                    if (isVisible)
                    {
                        // Only add if not already in the panel
                        if (!WidgetPanel.Children.Contains(uiElement))
                        {
                            WidgetPanel.Children.Add(uiElement);
                        }
                    }
                    else
                    {
                        // Remove from panel if hidden
                        if (WidgetPanel.Children.Contains(uiElement))
                        {
                            WidgetPanel.Children.Remove(uiElement);
                        }
                    }
                }
            }
            
            // After adjusting visibility, re-apply the correct order
            if (_settingsService != null && _allWidgets != null)
            {
                var orderedVisibleWidgets = _settingsService.Settings.WidgetOrder
                    .Select(widgetId => _allWidgets.FirstOrDefault(w => w.WidgetId == widgetId && w.IsWidgetVisible))
                    .Where(w => w != null)
                    .ToList();
                ReorderWidgets(orderedVisibleWidgets!); // Pass the ordered list of visible widgets
            }
        }

        /// <summary>
        /// Reorders the widgets in the UI according to the given list
        /// </summary>
        /// <param name="widgets">List of widgets in the desired order</param>
        public void ReorderWidgets(IEnumerable<IWidget> widgets)
        {
            if (widgets == null)
                return;

            // Get the container panel
            if (WidgetPanel != null)
            {
                // Remove all widgets from the UI
                WidgetPanel.Children.Clear();

                // Re-add only visible widgets in the specified order
                foreach (var widget in widgets.Where(w => w.IsWidgetVisible))
                {
                    // Only add if it's a UIElement
                    if (widget is UIElement uiElement)
                    {
                        WidgetPanel.Children.Add(uiElement);
                    }
                }
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                OpenSettingsWindow();
            }
        }

        private void ContextMenuSettings_Click(object sender, RoutedEventArgs e)
        {
            OpenSettingsWindow();
        }

        private void ContextMenuExit_Click(object sender, RoutedEventArgs e)
        {
            // Explicitly qualify Application to resolve ambiguity
            System.Windows.Application.Current.Shutdown();
        }

        private void Widget_ConfigButtonClicked(object? sender, EventArgs e)
        {
            if (sender is IWidget widget)
            {
                // Open the widget's specific settings dialog
                widget.OpenWidgetSettings();
            }
        }

        /// <summary>
        /// Opens the main application settings window.
        /// </summary>
        private void OpenSettingsWindow() // Remove optional parameter
        {
            Debug.WriteLine("Opening SettingsWindow..."); // Revert log message
            // Check if an instance already exists to prevent duplicates (optional, depends on desired behavior)
            var existingSettingsWindow = System.Windows.Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
            if (existingSettingsWindow != null)
            {
                Debug.WriteLine("SettingsWindow already open. Activating it.");
                if (existingSettingsWindow.WindowState == WindowState.Minimized) {
                    existingSettingsWindow.WindowState = WindowState.Normal; // Restore if minimized
                }
                existingSettingsWindow.Activate(); // Bring to front
                // Remove navigation call
                // existingSettingsWindow.NavigateToWidgetSettings(targetWidgetId);
            }
            else
            {
                Debug.WriteLine("Creating new SettingsWindow instance.");
                // Pass both required services to the constructor - Revert to 3 arguments
                var settingsWindow = new SettingsWindow(_screenService, _settingsService, _allWidgets);
                settingsWindow.Owner = this; // Set the owner to the MainWindow
                settingsWindow.Closed += (s, args) => Debug.WriteLine("SettingsWindow closed.");
                // Show as a dialog or non-dialog based on preference.
                // Show() allows interaction with MainWindow, ShowDialog() blocks it.
                settingsWindow.Show();
            }
        }
    }
}
