using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using DeskViz.Core.Models;

namespace DeskViz.Core.Services
{
    /// <summary>
    /// Service for managing application settings
    /// </summary>
    public class SettingsService
    {
        private string _settingsFilePath;
        private AppSettings _settings;
        
        /// <summary>
        /// Initializes a new instance of the SettingsService class
        /// </summary>
        public SettingsService()
        {
            // Set settings file path to application data folder
            string appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DeskViz"
            );
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }
            
            _settingsFilePath = Path.Combine(appDataFolder, "settings.json");
            _settings = new AppSettings();
            
            // Load settings if file exists
            if (File.Exists(_settingsFilePath))
            {
                LoadSettings();
            }
            else
            {
                // Save default settings if file doesn't exist
                SaveSettings();
            }
        }
        
        /// <summary>
        /// Gets the current application settings
        /// </summary>
        public AppSettings Settings => _settings;
        
        /// <summary>
        /// Loads settings from file and returns them
        /// </summary>
        public AppSettings LoadSettings()
        {
            try
            {
                string json = File.ReadAllText(_settingsFilePath);
                var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json);
                
                if (loadedSettings != null)
                {
                    _settings = loadedSettings;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                // Use default settings if loading fails
                _settings = new AppSettings();
            }
            
            // Migrate from old single-page system to multi-page if needed
            MigrateToMultiPageSystem();
            
            return _settings;
        }
        
        /// <summary>
        /// Migrates from the old single-page widget system to multi-page
        /// </summary>
        private void MigrateToMultiPageSystem()
        {
            // Always ensure we have at least one page
            if (_settings.Pages.Count == 0)
            {
                var mainPage = new PageConfig("Main");
                
                // If we have existing widget configuration, use it
                if (_settings.WidgetOrder.Count > 0)
                {
                    mainPage.WidgetIds = new List<string>(_settings.WidgetOrder);
                    // Copy existing visibility settings
                    foreach (var widgetId in mainPage.WidgetIds)
                    {
                        if (_settings.WidgetVisibility.TryGetValue(widgetId, out bool isVisible))
                        {
                            mainPage.WidgetVisibility[widgetId] = isVisible;
                        }
                        else
                        {
                            // Default to visible if not specified
                            mainPage.WidgetVisibility[widgetId] = true;
                        }
                    }
                }
                else
                {
                    // Create default configuration with all widgets
                    mainPage.WidgetIds = new List<string> { "CpuWidget", "RamWidget", "GpuWidget", "HardDriveWidget", "ClockWidget", "LogoWidget", "MediaControlWidget" };
                    foreach (var widgetId in mainPage.WidgetIds)
                    {
                        mainPage.WidgetVisibility[widgetId] = true;
                    }
                }
                
                _settings.Pages.Add(mainPage);
                SaveSettings();
            }
        }
        
        /// <summary>
        /// Saves settings to file
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Saves the provided settings to file
        /// </summary>
        public void SaveSettings(AppSettings settings)
        {
            _settings = settings;
            SaveSettings();
        }
        
        /// <summary>
        /// Updates the preferred display identifier
        /// </summary>
        public void UpdatePreferredDisplay(string screenIdentifier)
        {
            _settings.PreferredDisplayIdentifier = screenIdentifier;
            SaveSettings();
        }
        
        /// <summary>
        /// Updates widget visibility
        /// </summary>
        public void UpdateWidgetVisibility(string widgetId, bool isVisible)
        {
            if (_settings.WidgetVisibility.ContainsKey(widgetId))
            {
                _settings.WidgetVisibility[widgetId] = isVisible;
            }
            else
            {
                _settings.WidgetVisibility.Add(widgetId, isVisible);
            }
            
            SaveSettings();
        }
        
        /// <summary>
        /// Updates multiple widget visibility settings at once
        /// </summary>
        public void UpdateWidgetVisibility(Dictionary<string, bool> visibilitySettings)
        {
            foreach (var kvp in visibilitySettings)
            {
                if (_settings.WidgetVisibility.ContainsKey(kvp.Key))
                {
                    _settings.WidgetVisibility[kvp.Key] = kvp.Value;
                }
                else
                {
                    _settings.WidgetVisibility.Add(kvp.Key, kvp.Value);
                }
            }
            
            SaveSettings();
        }
        
        /// <summary>
        /// Updates the widget order
        /// </summary>
        public void UpdateWidgetOrder(List<string> widgetOrder)
        {
            _settings.WidgetOrder = new List<string>(widgetOrder);
            SaveSettings();
        }
        
        /// <summary>
        /// Updates the user's widget orientation preference.
        /// </summary>
        public void UpdateWidgetOrientation(WidgetOrientationSetting orientation)
        {
            _settings.WidgetOrientation = orientation;
            SaveSettings();
        }
        
        /// <summary>
        /// Adds a new page
        /// </summary>
        public void AddPage(string pageName)
        {
            var newPage = new PageConfig(pageName);
            _settings.Pages.Add(newPage);
            SaveSettings();
        }
        
        /// <summary>
        /// Removes a page by index
        /// </summary>
        public void RemovePage(int pageIndex)
        {
            if (pageIndex >= 0 && pageIndex < _settings.Pages.Count && _settings.Pages.Count > 1)
            {
                _settings.Pages.RemoveAt(pageIndex);
                
                // Adjust current page index if needed
                if (_settings.CurrentPageIndex >= _settings.Pages.Count)
                {
                    _settings.CurrentPageIndex = _settings.Pages.Count - 1;
                }
                
                SaveSettings();
            }
        }
        
        /// <summary>
        /// Updates a page's configuration
        /// </summary>
        public void UpdatePage(int pageIndex, PageConfig pageConfig)
        {
            if (pageIndex >= 0 && pageIndex < _settings.Pages.Count)
            {
                _settings.Pages[pageIndex] = pageConfig;
                SaveSettings();
            }
        }
        
        /// <summary>
        /// Gets a page configuration by index
        /// </summary>
        public PageConfig? GetPage(int pageIndex)
        {
            if (pageIndex >= 0 && pageIndex < _settings.Pages.Count)
            {
                return _settings.Pages[pageIndex];
            }
            return null;
        }
        
        /// <summary>
        /// Updates the current page index
        /// </summary>
        public void SetCurrentPageIndex(int pageIndex)
        {
            if (pageIndex >= 0 && pageIndex < _settings.Pages.Count)
            {
                _settings.CurrentPageIndex = pageIndex;
                SaveSettings();
            }
        }
    }
    
    /// <summary>
    /// Defines how widget orientation should be determined.
    /// </summary>
    public enum WidgetOrientationSetting
    {
        Auto,       // Determine based on screen orientation
        Horizontal, // Always horizontal
        Vertical    // Always vertical
    }

    /// <summary>
    /// Application settings
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets the preferred display identifier
        /// </summary>
        public string PreferredDisplayIdentifier { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets whether the app should start automatically
        /// </summary>
        public bool StartWithWindows { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to use dark theme
        /// </summary>
        public bool UseDarkTheme { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether the app should remember the last display
        /// </summary>
        public bool RememberLastDisplay { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the update interval in seconds
        /// </summary>
        public int UpdateIntervalSeconds { get; set; } = 3;
        
        /// <summary>
        /// Gets or sets the widget visibility settings (deprecated - use Pages instead)
        /// </summary>
        public Dictionary<string, bool> WidgetVisibility { get; set; } = new Dictionary<string, bool>();

        /// <summary>
        /// Gets or sets the order of widgets (deprecated - use Pages instead)
        /// </summary>
        public List<string> WidgetOrder { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the page configurations
        /// </summary>
        public List<PageConfig> Pages { get; set; } = new List<PageConfig>();

        /// <summary>
        /// Gets or sets the current page index
        /// </summary>
        public int CurrentPageIndex { get; set; } = 0;
        
        /// <summary>
        /// Gets or sets the user's preferred widget orientation.
        /// </summary>
        public WidgetOrientationSetting WidgetOrientation { get; set; } = WidgetOrientationSetting.Auto;

        /// <summary>
        /// Gets or sets whether the Clock widget uses 24-hour format.
        /// </summary>
        public bool ClockIs24HourFormat { get; set; } = true; // Default to 24-hour

        /// <summary>
        /// Gets or sets the font size for the Clock widget.
        /// </summary>
        public double ClockFontSize { get; set; } = 16;

        // CPU Widget Settings
        /// <summary>
        /// Gets or sets whether to show CPU cores.
        /// </summary>
        public bool CpuShowCores { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show CPU temperature.
        /// </summary>
        public bool CpuShowTemperature { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use Fahrenheit for CPU temperature.
        /// </summary>
        public bool CpuUseFahrenheit { get; set; } = false;

        /// <summary>
        /// Gets or sets the CPU temperature font size.
        /// </summary>
        public double CpuTemperatureFontSize { get; set; } = 12;

        /// <summary>
        /// Gets or sets whether to show CPU clock speed.
        /// </summary>
        public bool CpuShowClockSpeed { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show CPU power usage.
        /// </summary>
        public bool CpuShowPowerUsage { get; set; } = true;

        /// <summary>
        /// Gets or sets the CPU update interval in seconds.
        /// </summary>
        public double CpuUpdateIntervalSeconds { get; set; } = 2.5;

        // RAM Widget Settings
        /// <summary>
        /// Gets or sets whether to show Page File info for the RAM widget.
        /// </summary>
        public bool RamShowPageFileInfo { get; set; } = true; // Default to showing it

        /// <summary>
        /// Gets or sets the RAM widget update interval in seconds.
        /// </summary>
        public double RamUpdateIntervalSeconds { get; set; } = 2.5; // Default interval

        // Logo Widget Settings
        public string LogoImagePath { get; set; } = "pack://application:,,,/DeskViz.App;component/Resources/deskviz.png";
        public double? LogoWidth { get; set; } = null; // Null means auto
        public double? LogoHeight { get; set; } = 100; // Default height, null means auto
        public string LogoStretch { get; set; } = "Uniform"; // Fill, None, Uniform, UniformToFill
        public string LogoHorizontalAlignment { get; set; } = "Center"; // Left, Center, Right, Stretch
        public string LogoVerticalAlignment { get; set; } = "Center"; // Top, Center, Bottom, Stretch
        
        // GPU Widget Settings
        /// <summary>
        /// Gets or sets whether to show GPU temperature.
        /// </summary>
        public bool GpuShowTemperature { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to show GPU power usage.
        /// </summary>
        public bool GpuShowPower { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to show GPU memory usage.
        /// </summary>
        public bool GpuShowMemory { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to show GPU clock speed.
        /// </summary>
        public bool GpuShowClockSpeed { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to use Celsius for GPU temperature.
        /// </summary>
        public bool GpuIsCelsius { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the GPU widget update interval in seconds.
        /// </summary>
        public int GpuUpdateInterval { get; set; } = 2;
        
        /// <summary>
        /// Gets or sets the selected GPU index for the GPU widget.
        /// </summary>
        public int GpuSelectedIndex { get; set; } = 0;
        
        // Media Control Widget Settings
        /// <summary>
        /// Gets or sets whether to show the media control widget title.
        /// </summary>
        public bool MediaControlShowTitle { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to show the media control widget subtitle.
        /// </summary>
        public bool MediaControlShowSubtitle { get; set; } = true;
    }
}
