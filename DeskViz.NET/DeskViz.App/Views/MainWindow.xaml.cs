using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.IO;
using DeskViz.App.Services;
using DeskViz.Core.Models;
using DeskViz.Core.Services;
using DeskViz.Plugins.Services;
using DeskViz.Plugins.Interfaces;
using ScreenInfo = DeskViz.Core.Services.ScreenInfo;

namespace DeskViz.App.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger _logger = AppLoggerFactory.CreateLogger<MainWindow>();
        private readonly ScreenService _screenService;
        private readonly ISettingsService _settingsService;
        private readonly IHardwareMonitorService _hardwareMonitorService; // Added for DI
        private readonly IHardwarePollingService _hardwarePollingService; // Centralized hardware polling
        private readonly IMediaControlService _mediaControlService; // Added for media control
        private readonly ISystemTrayService _systemTrayService;
        private readonly AutoRotationService _autoRotationService;
        private UpdateService? _updateService;
        private List<IWidgetPlugin> _allWidgets = new List<IWidgetPlugin>();

        // Plugin system
        private readonly WidgetDiscoveryService? _widgetDiscoveryService;
        private readonly WidgetManager? _widgetManager;
        private readonly IWidgetHost? _widgetHost;
        
        // TODO: Add swipe tracking variables when implementing gestures

        public MainWindow()
        {
            _logger.LogDebug("===== MainWindow constructor started =====");

            // Initialize services early to satisfy nullable checks, even if InitComponent fails
            _settingsService = new SettingsService();
            _screenService = new ScreenService();
            _hardwareMonitorService = new LibreHardwareMonitorService(); // Create single instance
            _hardwarePollingService = new HardwarePollingService(_hardwareMonitorService); // Centralized polling
            _hardwarePollingService.Start(); // Start the polling service
            _mediaControlService = new WindowsMediaControlService(); // Create media control service
            _systemTrayService = new SystemTrayService();
            _autoRotationService = new AutoRotationService(_settingsService);

            // Initialize plugin system
            try
            {
                _logger.LogDebug("Starting plugin system initialization...");
                // Look for WidgetOutput in solution root (five levels up from bin/Debug/tfm/win-x64/), fallback to beside executable
                var solutionWidgetOutput = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "WidgetOutput"));
                var pluginDirectory = Directory.Exists(solutionWidgetOutput)
                    ? solutionWidgetOutput
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WidgetOutput");
                _logger.LogDebug($"Plugin directory: {pluginDirectory}");

                var serviceProvider = new AppWidgetServiceProvider(_hardwareMonitorService, _hardwarePollingService, _mediaControlService);
                _widgetHost = new PluginHost(serviceProvider);
                _widgetDiscoveryService = new WidgetDiscoveryService(pluginDirectory);
                _widgetManager = new WidgetManager(_widgetDiscoveryService, _widgetHost);

                // Subscribe to plugin events
                _widgetManager.WidgetActivated += OnPluginWidgetActivated;
                _widgetManager.WidgetDeactivated += OnPluginWidgetDeactivated;
                _widgetManager.WidgetError += OnPluginWidgetError;

                _logger.LogDebug("Plugin system initialized successfully");

                // Initialize update service with the same plugin directory
                _updateService = new UpdateService(_settingsService, pluginDirectory);
                _updateService.UpdateAvailable += OnUpdateAvailable;
                _logger.LogDebug("Update service initialized.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error initializing plugin system: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                // Continue without plugin system
            }

            try
            {
                _logger.LogDebug("Calling InitializeComponent()...");
                InitializeComponent();
                _logger.LogDebug("InitializeComponent() finished.");

                // Enable touch support for the main window
                Stylus.SetIsTouchFeedbackEnabled(this, false);
                Stylus.SetIsPressAndHoldEnabled(this, false);
                Stylus.SetIsFlicksEnabled(this, false);
                Stylus.SetIsTapFeedbackEnabled(this, false);
                _logger.LogDebug("Touch support configured for MainWindow");
            }
            catch (Exception ex)
            {
                _logger.LogError("!!!! EXCEPTION DURING InitializeComponent() !!!!");
                _logger.LogError($"Exception: {ex.GetType().Name}");
                _logger.LogError($"Message: {ex.Message}");
                _logger.LogError($"StackTrace: {ex.StackTrace}");
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

            _logger.LogDebug("MainWindow constructor finished (services initialized).");
        }

        private void RegisterWidgets()
        {
            _logger.LogDebug("RegisterWidgets started");

            try
            {
                // Discover plugin widgets first
                if (_widgetManager != null)
                {
                    _logger.LogDebug("Attempting to refresh available widgets...");
                    _widgetManager.RefreshAvailableWidgets();
                    _logger.LogDebug($"Discovered {_widgetManager.AvailableWidgets.Count} plugin widgets");

                    // Register discovered plugin widgets first
                    RegisterPluginWidgets();
                }
                else
                {
                    _logger.LogWarning("Widget manager is null, skipping plugin discovery");
                }

                // Only use plugins now - no more built-in widgets
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in RegisterWidgets: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");

                // No fallback widgets - only plugins
            }

            /*
            // Create YouTube Music widget
            var musicWidget = new YouTubeMusicWidget();
            musicWidget.DataContext = musicWidget; // Set DataContext
            musicWidget.ConfigButtonClicked += Widget_ConfigButtonClicked;
            _allWidgets.Add(musicWidget);
            */

            // Debug: Log widget registration
            _logger.LogDebug($"Registered {_allWidgets.Count} widgets:");
            foreach (var widget in _allWidgets)
            {
                _logger.LogDebug($"  - {widget.WidgetId}: {widget.GetType().Name}");
            }

            // Debug: Log page configuration
            _logger.LogDebug($"Settings has {_settingsService.Settings.Pages.Count} pages:");
            for (int i = 0; i < _settingsService.Settings.Pages.Count; i++)
            {
                var page = _settingsService.Settings.Pages[i];
                _logger.LogDebug($"  Page {i}: {page.Name} with {page.WidgetIds.Count} widgets");
                foreach (var widgetId in page.WidgetIds)
                {
                    var isVisible = page.WidgetVisibility.GetValueOrDefault(widgetId, true);
                    _logger.LogDebug($"    - {widgetId}: {(isVisible ? "visible" : "hidden")}");
                }
            }
            
            // Ensure all registered widgets are added to at least one page
            EnsureAllWidgetsInPages();

            // Initialize the PagedWidgetContainer with pages and widgets
            PagedContainer.Initialize(_settingsService.Settings.Pages, _allWidgets);

            // Set the current page (ensure it's within bounds)
            var targetPageIndex = Math.Max(0, Math.Min(_settingsService.Settings.CurrentPageIndex, _settingsService.Settings.Pages.Count - 1));
            _logger.LogDebug($"Setting current page to: {targetPageIndex} (saved was: {_settingsService.Settings.CurrentPageIndex})");
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
            _logger.LogDebug("Window_Loaded started.");

            // Check if this is the first run - show display selection dialog
            if (_settingsService.Settings.IsFirstRun)
            {
                _logger.LogInformation("First run detected - showing display selection dialog.");
                ShowFirstRunDisplaySelection();
            }

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

            // Start periodic update checks and optionally check on startup
            if (_updateService != null)
            {
                _updateService.StartPeriodicChecks();

                if (_settingsService.Settings.CheckForUpdatesOnStartup)
                {
                    _ = _updateService.CheckForUpdateAsync();
                }
            }

            _logger.LogDebug("Window_Loaded finished.");
        }

        /// <summary>
        /// Shows the display selection dialog on first run
        /// </summary>
        private void ShowFirstRunDisplaySelection()
        {
            var screens = _screenService.GetAllScreens();
            if (screens.Count == 0)
            {
                _logger.LogWarning("No screens detected for first run dialog.");
                _settingsService.MarkFirstRunComplete();
                return;
            }

            // If only one screen, just save it and skip the dialog
            if (screens.Count == 1)
            {
                _logger.LogDebug("Only one screen detected - skipping dialog and using it.");
                _settingsService.UpdatePreferredDisplay(screens[0].Identifier);
                _settingsService.MarkFirstRunComplete();
                return;
            }

            // Show the display selection dialog
            var dialog = new DisplaySelectionDialog(screens);

            if (dialog.ShowDialog() == true && dialog.SelectedScreen != null)
            {
                _logger.LogInformation($"User selected display: {dialog.SelectedScreen.DisplayName}");
                _settingsService.UpdatePreferredDisplay(dialog.SelectedScreen.Identifier);
            }
            else
            {
                // User closed without selecting - use primary screen
                var primaryScreen = _screenService.GetPrimaryScreen();
                if (primaryScreen != null)
                {
                    _logger.LogDebug("No selection made - defaulting to primary screen.");
                    _settingsService.UpdatePreferredDisplay(primaryScreen.Identifier);
                }
            }

            // Mark first run as complete
            _settingsService.MarkFirstRunComplete();
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
        public void ReorderWidgets(IEnumerable<IWidgetPlugin> widgets)
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
                _logger.LogDebug("F1 pressed - forcing page reload");
                PagedContainer.Initialize(_settingsService.Settings.Pages, _allWidgets);
                PagedContainer.CurrentPageIndex = PagedContainer.CurrentPageIndex;
            }
            else if (e.Key == System.Windows.Input.Key.F2)
            {
                // Debug: Simulate 2-finger swipe left (next page)
                _logger.LogDebug("F2 pressed - simulating 2-finger swipe left");
                if (PagedContainer.CurrentPageIndex < _settingsService.Settings.Pages.Count - 1)
                {
                    PagedContainer.NavigateToPage(PagedContainer.CurrentPageIndex + 1);
                }
            }
            else if (e.Key == System.Windows.Input.Key.F3)
            {
                // Debug: Simulate 2-finger swipe right (previous page)
                _logger.LogDebug("F3 pressed - simulating 2-finger swipe right");
                if (PagedContainer.CurrentPageIndex > 0)
                {
                    PagedContainer.NavigateToPage(PagedContainer.CurrentPageIndex - 1);
                }
            }
            else if (e.Key == System.Windows.Input.Key.F4)
            {
                // Debug: Test swipe-down page selector
                _logger.LogDebug("F4 pressed - testing page selector overlay");
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
            _logger.LogDebug($"ContextMenuPageSettings_Click called - Current page: {PagedContainer.CurrentPageIndex}");
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
            {
                _logger.LogWarning("EnsureAllWidgetsInPages: _allWidgets or _settingsService is null");
                return;
            }

            _logger.LogDebug($"EnsureAllWidgetsInPages: Found {_allWidgets.Count} total widgets");

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
                _logger.LogDebug($"Found {missingWidgets.Count} missing widgets, adding to first page:");

                // Add missing widgets to the first page
                var firstPage = _settingsService.Settings.Pages.FirstOrDefault();
                if (firstPage != null)
                {
                    foreach (var widget in missingWidgets)
                    {
                        _logger.LogDebug($"  Adding {widget.WidgetId} to page '{firstPage.Name}'");
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

            _logger.LogInformation($"Auto-rotation initialized. Enabled: {_settingsService.Settings.AutoRotationEnabled}");
        }

        private void OnAutoRotationRequested(object? sender, PageRotationEventArgs e)
        {
            // Switch to the next page
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    _logger.LogDebug($"Auto-rotation: switching from page {e.CurrentPageIndex} to {e.NextPageIndex} (mode: {e.RotationMode})");

                    // Update settings service current page
                    _settingsService.SetCurrentPageIndex(e.NextPageIndex);

                    // Navigate the UI to the new page
                    PagedContainer.NavigateToPage(e.NextPageIndex);

                    // Update the current page menu item
                    UpdateCurrentPageMenuItem();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error during auto-rotation: {ex.Message}");
                }
            });
        }

        private void OnAutoRotationStateChanged(object? sender, bool isEnabled)
        {
            _logger.LogInformation($"Auto-rotation state changed: {(isEnabled ? "Started" : "Stopped")}");
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
                    _logger.LogDebug("Auto-rotation started after settings update");
                }
                else
                {
                    _autoRotationService?.Stop();
                    _logger.LogDebug("Auto-rotation stopped after settings update");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error refreshing auto-rotation settings: {ex.Message}");
            }
        }

        private void Widget_ConfigButtonClicked(object? sender, EventArgs e)
        {
            // Notify auto-rotation of user interaction
            OnUserInteraction();

            if (sender is IWidgetPlugin widget)
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
            _logger.LogDebug($"OpenSettingsWindow called - openPageSettings: {openPageSettings}, CurrentPage: {PagedContainer.CurrentPageIndex}");
            // Check if an instance already exists to prevent duplicates (optional, depends on desired behavior)
            var existingSettingsWindow = System.Windows.Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
            if (existingSettingsWindow != null)
            {
                _logger.LogDebug("SettingsWindow already open. Activating it.");
                if (existingSettingsWindow.WindowState == WindowState.Minimized) {
                    existingSettingsWindow.WindowState = WindowState.Normal; // Restore if minimized
                }
                existingSettingsWindow.Activate(); // Bring to front
                // Remove navigation call
                // existingSettingsWindow.NavigateToWidgetSettings(targetWidgetId);
            }
            else
            {
                _logger.LogDebug("Creating new SettingsWindow instance.");
                // Pass both required services to the constructor - Revert to 3 arguments
                var settingsWindow = new SettingsWindow(_screenService, _settingsService, _allWidgets);

                // Subscribe to widget configuration changes for real-time updates
                settingsWindow.WidgetConfigurationChanged += (s, args) =>
                {
                    _logger.LogDebug("Widget configuration changed. Refreshing page display.");
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
                    _logger.LogDebug("SettingsWindow closed.");
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
                    _logger.LogDebug($"Loaded icon from file: {iconPath}");
                }
                else
                {
                    // Fallback to embedded resource
                    var iconUri = new Uri("pack://application:,,,/Resources/AppIcon.ico");
                    var iconStream = System.Windows.Application.GetResourceStream(iconUri);
                    if (iconStream != null)
                    {
                        icon = new System.Drawing.Icon(iconStream.Stream);
                        _logger.LogDebug("Loaded icon from embedded resource");
                    }
                }
                
                if (icon != null)
                {
                    _systemTrayService.Initialize(icon, "DeskViz - System Monitor");
                    
                    // Subscribe to events
                    _systemTrayService.SettingsRequested += SystemTray_SettingsRequested;
                    _systemTrayService.AboutRequested += SystemTray_AboutRequested;
                    _systemTrayService.CheckForUpdatesRequested += SystemTray_CheckForUpdatesRequested;
                    _systemTrayService.ExitRequested += SystemTray_ExitRequested;
                    _systemTrayService.TrayIconDoubleClicked += SystemTray_DoubleClicked;
                    
                    // Show the tray icon
                    _systemTrayService.Show();
                    _logger.LogDebug("System tray initialized successfully");
                }
                else
                {
                    _logger.LogWarning("Failed to load system tray icon from any source");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error initializing system tray: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
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

        private void SystemTray_CheckForUpdatesRequested(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
                if (_updateService == null) return;

                _systemTrayService?.ShowBalloonTip("DeskViz", "Checking for updates...", 2000);

                var release = await _updateService.CheckForUpdateAsync();
                if (release == null)
                {
                    _systemTrayService?.ShowBalloonTip("DeskViz", "You are running the latest version.", 3000);
                }
                // If an update is found, OnUpdateAvailable will handle it
            });
        }

        private void OnUpdateAvailable(object? sender, UpdateAvailableEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                ShowUpdateDialog(e.Release);
            });
        }

        private async void ShowUpdateDialog(ReleaseInfo release)
        {
            if (_updateService == null) return;

            try
            {
                var dialog = new UpdateDialog(release, _updateService)
                {
                    Owner = this
                };

                var result = dialog.ShowDialog();

                switch (dialog.ChosenAction)
                {
                    case UpdateAction.UpdateNow:
                        await PerformUpdateAsync(release, dialog.IncludeAppUpdate, dialog.IncludeWidgetUpdate);
                        break;

                    case UpdateAction.SkipVersion:
                        _settingsService.Settings.SkippedVersion = release.TagName;
                        _settingsService.SaveSettings();
                        _logger.LogInformation($"User chose to skip version {release.TagName}.");
                        break;

                    case UpdateAction.RemindLater:
                        _logger.LogInformation("User chose to be reminded later about the update.");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error showing update dialog: {ex.Message}");
            }
        }

        private async Task PerformUpdateAsync(ReleaseInfo release, bool includeApp, bool includeWidgets)
        {
            if (_updateService == null) return;

            try
            {
                // Apply widget update first (does not require restart)
                if (includeWidgets && release.HasWidgetUpdate)
                {
                    _logger.LogInformation("Downloading widget update...");
                    var extractedDir = await _updateService.DownloadWidgetUpdateAsync(release);
                    if (extractedDir != null)
                    {
                        await _updateService.ApplyWidgetUpdateAsync(extractedDir);
                        _systemTrayService?.ShowBalloonTip("DeskViz", "Widget update applied. Restart to load new widgets.", 3000);
                    }
                }

                // Apply app update last (triggers restart)
                if (includeApp && release.HasAppUpdate)
                {
                    _logger.LogInformation("Downloading application update...");
                    var downloadedPath = await _updateService.DownloadAppUpdateAsync(release);
                    if (downloadedPath != null)
                    {
                        _updateService.ApplyAppUpdate(downloadedPath, () =>
                        {
                            Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error performing update: {ex.Message}");
                System.Windows.MessageBox.Show($"Update failed: {ex.Message}", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Clean up update service
            _updateService?.Dispose();
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
                _logger.LogDebug($"Refreshing page display for page {currentPageIndex}");

                // Only initialize if not already initialized, otherwise sync pages and add new widgets
                if (!PagedContainer.IsContainerInitialized)
                {
                    _logger.LogDebug("Initializing PagedContainer for the first time");
                    PagedContainer.Initialize(_settingsService.Settings.Pages, _allWidgets);
                }
                else
                {
                    _logger.LogDebug("Syncing pages and widgets with existing container");
                    PagedContainer.SyncPages(_settingsService.Settings.Pages);
                    PagedContainer.AddNewWidgets(_allWidgets);
                }

                _logger.LogDebug($"Page display refreshed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error refreshing page display: {ex.Message}");
            }
        }

        private void TestPageSelector()
        {
            // Test the page selector by calling the public method we'll add
            PagedContainer.TestShowPageSelector();
        }


        private void RegisterPluginWidgets()
        {
            _logger.LogDebug("Attempting to load plugin widgets from DLLs");

            if (_widgetManager == null)
            {
                _logger.LogWarning("Widget manager is null - plugin system failed to initialize");
                return;
            }

            _logger.LogDebug($"Found {_widgetManager.AvailableWidgets.Count} plugin widgets");

            // Activate discovered plugin widgets
            foreach (var loadedWidget in _widgetManager.AvailableWidgets)
            {
                try
                {
                    _logger.LogDebug($"Loading plugin: {loadedWidget.Metadata.Id} ({loadedWidget.Metadata.Name})");

                    // Check if we already have a built-in widget with the same ID
                    var existingWidget = _allWidgets.FirstOrDefault(w => w.WidgetId == loadedWidget.Metadata.Id);
                    if (existingWidget != null)
                    {
                        _logger.LogInformation($"Replacing built-in widget '{loadedWidget.Metadata.Id}' with plugin version");
                        _allWidgets.Remove(existingWidget);
                    }

                    // Activate the plugin widget
                    if (_widgetManager.ActivateWidget(loadedWidget.Metadata.Id))
                    {
                        _logger.LogInformation($"Activated plugin widget: {loadedWidget.Metadata.Id}");
                    }
                    else
                    {
                        _logger.LogWarning($"Could not activate plugin widget: {loadedWidget.Metadata.Id}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to register plugin widget '{loadedWidget.Metadata.Id}': {ex.Message}");
                }
            }

            _logger.LogDebug("Plugin loading complete");
        }

        private void OnPluginWidgetActivated(object? sender, WidgetActivatedEventArgs e)
        {
            try
            {
                _logger.LogInformation($"Plugin widget activated: {e.Widget.WidgetId}");

                // Initialize the plugin widget with host
                e.Widget.Initialize(_widgetHost!);

                // Add the plugin widget directly
                _allWidgets.Add(e.Widget);

                // Ensure this new widget is added to pages
                EnsureAllWidgetsInPages();

                // If we have pages set up, refresh the display
                if (_settingsService?.Settings.Pages.Count > 0)
                {
                    RefreshCurrentPageDisplay();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling plugin widget activation: {ex.Message}");
            }
        }

        private void OnPluginWidgetDeactivated(object? sender, WidgetDeactivatedEventArgs e)
        {
            try
            {
                _logger.LogInformation($"Plugin widget deactivated: {e.WidgetId}");

                // Remove the plugin widget from the widgets list
                var widget = _allWidgets.FirstOrDefault(w => w.WidgetId == e.WidgetId);

                if (widget != null)
                {
                    _allWidgets.Remove(widget);
                    RefreshCurrentPageDisplay();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling plugin widget deactivation: {ex.Message}");
            }
        }

        private void OnPluginWidgetError(object? sender, WidgetErrorEventArgs e)
        {
            _logger.LogError($"Plugin widget error in '{e.WidgetId}': {e.Exception.Message}");
            // Could show a notification or error dialog here
        }

        // TODO: Add swipe gestures later after basic functionality works
    }
}
