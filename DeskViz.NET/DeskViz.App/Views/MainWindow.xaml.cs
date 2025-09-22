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
        private readonly ISystemTrayService _systemTrayService;
        private readonly AutoRotationService _autoRotationService;
        private List<IWidget> _allWidgets = new List<IWidget>();
        
        // TODO: Add swipe tracking variables when implementing gestures

        public MainWindow()
        {
            Debug.WriteLine("MainWindow constructor started.");

            // Initialize services early to satisfy nullable checks, even if InitComponent fails
            _settingsService = new SettingsService();
            _screenService = new ScreenService();
            _hardwareMonitorService = new LibreHardwareMonitorService(); // Create single instance
            _mediaControlService = new WindowsMediaControlService(); // Create media control service
            _systemTrayService = new SystemTrayService();
            _autoRotationService = new AutoRotationService(_settingsService);

            try
            {
                Debug.WriteLine("Calling InitializeComponent()...");
                InitializeComponent();
                Debug.WriteLine("InitializeComponent() finished.");

                // Enable touch support for the main window
                Stylus.SetIsTouchFeedbackEnabled(this, false);
                Stylus.SetIsPressAndHoldEnabled(this, false);
                Stylus.SetIsFlicksEnabled(this, false);
                Stylus.SetIsTapFeedbackEnabled(this, false);
                Debug.WriteLine("Touch support configured for MainWindow");
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

            // Initialize system tray
            InitializeSystemTray();

            // Initialize auto-rotation
            InitializeAutoRotation();

            // Start with window not in taskbar
            ShowInTaskbar = false;

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

            // Create Hard Drive widget
            var hardDriveWidget = new HardDriveWidget(_hardwareMonitorService, _settingsService);
            hardDriveWidget.DataContext = hardDriveWidget; // Set DataContext
            hardDriveWidget.ConfigButtonClicked += Widget_ConfigButtonClicked;
            _allWidgets.Add(hardDriveWidget);

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

            // Debug: Log widget registration
            System.Diagnostics.Debug.WriteLine($"Registered {_allWidgets.Count} widgets:");
            foreach (var widget in _allWidgets)
            {
                System.Diagnostics.Debug.WriteLine($"  - {widget.WidgetId}: {widget.GetType().Name}");
            }
            
            // Debug: Log page configuration
            System.Diagnostics.Debug.WriteLine($"Settings has {_settingsService.Settings.Pages.Count} pages:");
            for (int i = 0; i < _settingsService.Settings.Pages.Count; i++)
            {
                var page = _settingsService.Settings.Pages[i];
                System.Diagnostics.Debug.WriteLine($"  Page {i}: {page.Name} with {page.WidgetIds.Count} widgets");
                foreach (var widgetId in page.WidgetIds)
                {
                    var isVisible = page.WidgetVisibility.GetValueOrDefault(widgetId, true);
                    System.Diagnostics.Debug.WriteLine($"    - {widgetId}: {(isVisible ? "visible" : "hidden")}");
                }
            }
            
            // Ensure all registered widgets are added to at least one page
            EnsureAllWidgetsInPages();

            // Initialize the PagedWidgetContainer with pages and widgets
            PagedContainer.Initialize(_settingsService.Settings.Pages, _allWidgets);

            // Set the current page (ensure it's within bounds)
            var targetPageIndex = Math.Max(0, Math.Min(_settingsService.Settings.CurrentPageIndex, _settingsService.Settings.Pages.Count - 1));
            System.Diagnostics.Debug.WriteLine($"Setting current page to: {targetPageIndex} (saved was: {_settingsService.Settings.CurrentPageIndex})");
            PagedContainer.CurrentPageIndex = targetPageIndex;
            
            // Handle page changes
            PagedContainer.PageChanged += (s, pageIndex) => 
            {
                _settingsService.SetCurrentPageIndex(pageIndex);
                UpdateCurrentPageMenuItem();
            };
            
            // Update the current page display
            UpdateCurrentPageMenuItem();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Window_Loaded started.");
            
            // Apply display settings and show the window normally
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
                
                // TODO: Apply orientation to PagedWidgetContainer when implemented
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
            // Re-initialize the PagedWidgetContainer with updated settings
            PagedContainer.Initialize(_settingsService.Settings.Pages, _allWidgets);
            
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
            // With the new paged system, we need to update the current page's widget order
            if (widgets == null || _settingsService == null)
                return;

            var currentPageIndex = PagedContainer.CurrentPageIndex;
            var currentPage = _settingsService.GetPage(currentPageIndex);
            
            if (currentPage != null)
            {
                // Update the widget order for the current page
                currentPage.WidgetIds.Clear();
                foreach (var widget in widgets)
                {
                    currentPage.WidgetIds.Add(widget.WidgetId);
                    currentPage.WidgetVisibility[widget.WidgetId] = widget.IsWidgetVisible;
                }
                
                // Save the updated page configuration
                _settingsService.UpdatePage(currentPageIndex, currentPage);
                
                // Refresh the PagedWidgetContainer
                PagedContainer.Initialize(_settingsService.Settings.Pages, _allWidgets);
                PagedContainer.CurrentPageIndex = currentPageIndex;
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Notify auto-rotation of user interaction for navigation keys
            if (e.Key == System.Windows.Input.Key.Left || e.Key == System.Windows.Input.Key.Right ||
                e.Key == System.Windows.Input.Key.PageUp || e.Key == System.Windows.Input.Key.PageDown)
            {
                OnUserInteraction();
            }

            if (e.Key == System.Windows.Input.Key.Escape)
            {
                OpenSettingsWindow();
            }
            else if (e.Key == System.Windows.Input.Key.Left)
            {
                // Navigate to previous page
                if (PagedContainer.CurrentPageIndex > 0)
                {
                    PagedContainer.NavigateToPage(PagedContainer.CurrentPageIndex - 1);
                }
            }
            else if (e.Key == System.Windows.Input.Key.Right)
            {
                // Navigate to next page
                var pageCount = _settingsService.Settings.Pages.Count;
                if (PagedContainer.CurrentPageIndex < pageCount - 1)
                {
                    PagedContainer.NavigateToPage(PagedContainer.CurrentPageIndex + 1);
                }
            }
            else if (e.Key == System.Windows.Input.Key.PageUp)
            {
                // Navigate to previous page
                if (PagedContainer.CurrentPageIndex > 0)
                {
                    PagedContainer.NavigateToPage(PagedContainer.CurrentPageIndex - 1);
                }
            }
            else if (e.Key == System.Windows.Input.Key.PageDown)
            {
                // Navigate to next page
                var pageCount = _settingsService.Settings.Pages.Count;
                if (PagedContainer.CurrentPageIndex < pageCount - 1)
                {
                    PagedContainer.NavigateToPage(PagedContainer.CurrentPageIndex + 1);
                }
            }
            else if (e.Key == System.Windows.Input.Key.F1)
            {
                // Debug: Force reload current page
                System.Diagnostics.Debug.WriteLine("F1 pressed - forcing page reload");
                PagedContainer.Initialize(_settingsService.Settings.Pages, _allWidgets);
                PagedContainer.CurrentPageIndex = PagedContainer.CurrentPageIndex;
            }
            else if (e.Key == System.Windows.Input.Key.F2)
            {
                // Debug: Simulate 2-finger swipe left (next page)
                System.Diagnostics.Debug.WriteLine("F2 pressed - simulating 2-finger swipe left");
                if (PagedContainer.CurrentPageIndex < _settingsService.Settings.Pages.Count - 1)
                {
                    PagedContainer.NavigateToPage(PagedContainer.CurrentPageIndex + 1);
                }
            }
            else if (e.Key == System.Windows.Input.Key.F3)
            {
                // Debug: Simulate 2-finger swipe right (previous page)
                System.Diagnostics.Debug.WriteLine("F3 pressed - simulating 2-finger swipe right");
                if (PagedContainer.CurrentPageIndex > 0)
                {
                    PagedContainer.NavigateToPage(PagedContainer.CurrentPageIndex - 1);
                }
            }
            else if (e.Key == System.Windows.Input.Key.F4)
            {
                // Debug: Test swipe-down page selector
                System.Diagnostics.Debug.WriteLine("F4 pressed - testing page selector overlay");
                // Add a method to access the page selector from MainWindow
                TestPageSelector();
            }
        }

        private void ContextMenuSettings_Click(object sender, RoutedEventArgs e)
        {
            OpenSettingsWindow();
        }

        private void ContextMenuPageSettings_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"ContextMenuPageSettings_Click called - Current page: {PagedContainer.CurrentPageIndex}");
            OpenSettingsWindow(openPageSettings: true);
        }

        private void ContextMenuExit_Click(object sender, RoutedEventArgs e)
        {
            // Explicitly qualify Application to resolve ambiguity
            System.Windows.Application.Current.Shutdown();
        }
        
        private void AddNewPage_Click(object sender, RoutedEventArgs e)
        {
            // Create input dialog to get page name
            var dialog = new InputDialog(
                "Enter a name for the new page:", 
                "New Page", 
                $"Page {_settingsService.Settings.Pages.Count + 1}");
            
            dialog.Owner = this;
            
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputValue))
            {
                // Add the new page
                _settingsService.AddPage(dialog.InputValue);
                
                // Navigate to the new page
                var newPageIndex = _settingsService.Settings.Pages.Count - 1;
                PagedContainer.Initialize(_settingsService.Settings.Pages, _allWidgets);
                PagedContainer.NavigateToPage(newPageIndex);
            }
        }
        
        /// <summary>
        /// Ensures all registered widgets are present in at least one page
        /// </summary>
        private void EnsureAllWidgetsInPages()
        {
            if (_allWidgets == null || _settingsService == null)
                return;

            // Get all widget IDs that are in pages
            var widgetsInPages = new HashSet<string>();
            foreach (var page in _settingsService.Settings.Pages)
            {
                foreach (var widgetId in page.WidgetIds)
                {
                    widgetsInPages.Add(widgetId);
                }
            }

            // Find missing widgets
            var missingWidgets = _allWidgets
                .Where(w => !widgetsInPages.Contains(w.WidgetId))
                .ToList();

            if (missingWidgets.Any())
            {
                System.Diagnostics.Debug.WriteLine($"Found {missingWidgets.Count} missing widgets, adding to first page:");

                // Add missing widgets to the first page
                var firstPage = _settingsService.Settings.Pages.FirstOrDefault();
                if (firstPage != null)
                {
                    foreach (var widget in missingWidgets)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Adding {widget.WidgetId} to page '{firstPage.Name}'");
                        firstPage.WidgetIds.Add(widget.WidgetId);
                        firstPage.WidgetVisibility[widget.WidgetId] = true; // Default to visible
                    }

                    // Save the updated page
                    _settingsService.UpdatePage(0, firstPage);
                }
            }
        }

        private void UpdateCurrentPageMenuItem()
        {
            if (CurrentPageMenuItem != null && _settingsService != null)
            {
                var currentPage = _settingsService.GetPage(PagedContainer.CurrentPageIndex);
                if (currentPage != null)
                {
                    CurrentPageMenuItem.Header = $"Current Page: {currentPage.Name} ({PagedContainer.CurrentPageIndex + 1}/{_settingsService.Settings.Pages.Count})";
                }
            }
        }

        private void InitializeAutoRotation()
        {
            // Subscribe to auto-rotation events
            _autoRotationService.PageRotationRequested += OnAutoRotationRequested;
            _autoRotationService.AutoRotationStateChanged += OnAutoRotationStateChanged;

            // Start auto-rotation if enabled
            if (_settingsService.Settings.AutoRotationEnabled)
            {
                _autoRotationService.Start();
            }

            Debug.WriteLine($"Auto-rotation initialized. Enabled: {_settingsService.Settings.AutoRotationEnabled}");
        }

        private void OnAutoRotationRequested(object? sender, PageRotationEventArgs e)
        {
            // Switch to the next page
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    Debug.WriteLine($"Auto-rotation: switching from page {e.CurrentPageIndex} to {e.NextPageIndex} (mode: {e.RotationMode})");

                    // Update settings service current page
                    _settingsService.SetCurrentPageIndex(e.NextPageIndex);

                    // Navigate the UI to the new page
                    PagedContainer.NavigateToPage(e.NextPageIndex);

                    // Update the current page menu item
                    UpdateCurrentPageMenuItem();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during auto-rotation: {ex.Message}");
                }
            });
        }

        private void OnAutoRotationStateChanged(object? sender, bool isEnabled)
        {
            Debug.WriteLine($"Auto-rotation state changed: {(isEnabled ? "Started" : "Stopped")}");
        }

        public void OnUserInteraction()
        {
            // Notify auto-rotation service of user interaction
            _autoRotationService?.OnUserInteraction();
        }

        private void RefreshAutoRotationSettings()
        {
            try
            {
                // Update the timer settings based on current configuration
                _autoRotationService?.UpdateTimerSettings();

                // Start or stop auto-rotation based on settings
                if (_settingsService.Settings.AutoRotationEnabled)
                {
                    _autoRotationService?.Start();
                    Debug.WriteLine("Auto-rotation started after settings update");
                }
                else
                {
                    _autoRotationService?.Stop();
                    Debug.WriteLine("Auto-rotation stopped after settings update");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing auto-rotation settings: {ex.Message}");
            }
        }

        private void Widget_ConfigButtonClicked(object? sender, EventArgs e)
        {
            // Notify auto-rotation of user interaction
            OnUserInteraction();

            if (sender is IWidget widget)
            {
                // Open the widget's specific settings dialog
                widget.OpenWidgetSettings();
            }
        }

        /// <summary>
        /// Opens the main application settings window.
        /// </summary>
        private void OpenSettingsWindow(bool openPageSettings = false) // Add optional parameter
        {
            Debug.WriteLine($"OpenSettingsWindow called - openPageSettings: {openPageSettings}, CurrentPage: {PagedContainer.CurrentPageIndex}"); // Enhanced log message
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

                // Subscribe to widget configuration changes for real-time updates
                settingsWindow.WidgetConfigurationChanged += (s, args) =>
                {
                    Debug.WriteLine("Widget configuration changed. Refreshing page display.");
                    RefreshCurrentPageDisplay();
                };

                // If opening page settings, navigate to Pages tab and select current page
                if (openPageSettings)
                {
                    settingsWindow.OpenPageSettings(PagedContainer.CurrentPageIndex);
                }
                settingsWindow.Owner = this; // Set the owner to the MainWindow
                settingsWindow.Closed += (s, args) =>
                {
                    Debug.WriteLine("SettingsWindow closed.");
                    // Refresh auto-rotation settings when settings window closes
                    RefreshAutoRotationSettings();
                    // Also refresh the page display in case there were changes
                    RefreshCurrentPageDisplay();
                };
                // Show as a dialog or non-dialog based on preference.
                // Show() allows interaction with MainWindow, ShowDialog() blocks it.
                settingsWindow.Show();
            }
        }

        private void InitializeSystemTray()
        {
            try
            {
                // Load the icon from the file system directly
                var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "AppIcon.ico");
                
                System.Drawing.Icon? icon = null;
                
                // Try loading from file first
                if (System.IO.File.Exists(iconPath))
                {
                    icon = new System.Drawing.Icon(iconPath);
                    Debug.WriteLine($"Loaded icon from file: {iconPath}");
                }
                else
                {
                    // Fallback to embedded resource
                    var iconUri = new Uri("pack://application:,,,/Resources/AppIcon.ico");
                    var iconStream = System.Windows.Application.GetResourceStream(iconUri);
                    if (iconStream != null)
                    {
                        icon = new System.Drawing.Icon(iconStream.Stream);
                        Debug.WriteLine("Loaded icon from embedded resource");
                    }
                }
                
                if (icon != null)
                {
                    _systemTrayService.Initialize(icon, "DeskViz - System Monitor");
                    
                    // Subscribe to events
                    _systemTrayService.SettingsRequested += SystemTray_SettingsRequested;
                    _systemTrayService.AboutRequested += SystemTray_AboutRequested;
                    _systemTrayService.ExitRequested += SystemTray_ExitRequested;
                    _systemTrayService.TrayIconDoubleClicked += SystemTray_DoubleClicked;
                    
                    // Show the tray icon
                    _systemTrayService.Show();
                    Debug.WriteLine("System tray initialized successfully");
                }
                else
                {
                    Debug.WriteLine("Failed to load system tray icon from any source");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing system tray: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void SystemTray_SettingsRequested(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // Restore window
                RestoreWindow();
                
                // Open settings
                OpenSettingsWindow();
            });
        }

        private void SystemTray_AboutRequested(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ShowAboutDialog();
            });
        }

        private void SystemTray_ExitRequested(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                System.Windows.Application.Current.Shutdown();
            });
        }

        private void SystemTray_DoubleClicked(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (Visibility != Visibility.Visible)
                {
                    // Show window
                    Show();
                    Activate();
                    Focus();
                }
                else
                {
                    // Hide window
                    Hide();
                    _systemTrayService?.ShowBalloonTip("DeskViz", "Application hidden to system tray", 2000);
                }
            });
        }
        
        private void RestoreWindow()
        {
            // Show window
            Visibility = Visibility.Visible;
            Show();
            WindowState = WindowState.Normal;
            
            // Apply fullscreen settings
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
            
            // Bring to front
            Activate();
            Focus();
        }

        private void ShowAboutDialog()
        {
            var aboutWindow = new AboutWindow
            {
                Owner = this
            };
            aboutWindow.ShowDialog();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            
            // If minimized, hide the window instead
            if (WindowState == WindowState.Minimized)
            {
                // Prevent actual minimize, hide instead
                WindowState = WindowState.Normal;
                Hide();
                
                // Show notification
                _systemTrayService?.ShowBalloonTip("DeskViz", "Application hidden to system tray", 2000);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Clean up system tray on close
            _systemTrayService?.Dispose();
            base.OnClosed(e);
        }

        /// <summary>
        /// Refreshes the current page display to reflect any widget configuration changes
        /// </summary>
        private void RefreshCurrentPageDisplay()
        {
            try
            {
                var currentPageIndex = PagedContainer.CurrentPageIndex;
                Debug.WriteLine($"Refreshing page display for page {currentPageIndex}");

                // Re-initialize the PagedContainer with updated settings
                PagedContainer.Initialize(_settingsService.Settings.Pages, _allWidgets);

                // Restore the current page index
                PagedContainer.CurrentPageIndex = currentPageIndex;

                Debug.WriteLine($"Page display refreshed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing page display: {ex.Message}");
            }
        }

        private void TestPageSelector()
        {
            // Test the page selector by calling the public method we'll add
            PagedContainer.TestShowPageSelector();
        }
        
        // TODO: Add swipe gestures later after basic functionality works
    }
}
