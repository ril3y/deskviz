using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DeskViz.Plugins.Interfaces;

namespace DeskViz.Plugins.Services
{
    public class PluginHost : IWidgetHost
    {
        private readonly IWidgetServiceProvider _serviceProvider;
        private readonly Dictionary<string, object> _widgetSettings = new();
        private readonly Dictionary<string, Dictionary<string, object>> _pageWidgetSettings = new();
        private readonly string _dataPath;
        private readonly Action<string, string, Exception?>? _logger;

        public IWidgetServiceProvider ServiceProvider => _serviceProvider;

        public PluginHost(IWidgetServiceProvider serviceProvider, string? dataPath = null, Action<string, string, Exception?>? logger = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dataPath = dataPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeskViz");
            _logger = logger;

            EnsureDataDirectoryExists();
        }

        public void SaveWidgetSettings(string widgetId, object settings)
        {
            try
            {
                _widgetSettings[widgetId] = settings;

                var settingsPath = Path.Combine(_dataPath, "Widgets", widgetId, "settings.json");
                var directory = Path.GetDirectoryName(settingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                LogError(widgetId, $"Failed to save widget settings: {ex.Message}", ex);
            }
        }

        public T? LoadWidgetSettings<T>(string widgetId) where T : class, new()
        {
            try
            {
                if (_widgetSettings.TryGetValue(widgetId, out var cached) && cached is T cachedSettings)
                {
                    return cachedSettings;
                }

                var settingsPath = Path.Combine(_dataPath, "Widgets", widgetId, "settings.json");
                if (!File.Exists(settingsPath))
                {
                    var defaultSettings = new T();
                    _widgetSettings[widgetId] = defaultSettings;
                    return defaultSettings;
                }

                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<T>(json);

                if (settings != null)
                {
                    _widgetSettings[widgetId] = settings;
                    return settings;
                }
            }
            catch (Exception ex)
            {
                LogError(widgetId, $"Failed to load widget settings: {ex.Message}", ex);
            }

            var fallbackSettings = new T();
            _widgetSettings[widgetId] = fallbackSettings;
            return fallbackSettings;
        }

        public void SaveWidgetSettingsForPage(string widgetId, string pageId, object settings)
        {
            try
            {
                var pageKey = $"{pageId}_{widgetId}";

                // Cache in memory
                if (!_pageWidgetSettings.ContainsKey(pageId))
                {
                    _pageWidgetSettings[pageId] = new Dictionary<string, object>();
                }
                _pageWidgetSettings[pageId][widgetId] = settings;

                // Save to disk in page-specific folder
                var settingsPath = Path.Combine(_dataPath, "Pages", pageId, "Widgets", widgetId, "settings.json");
                var directory = Path.GetDirectoryName(settingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                LogError(widgetId, $"Failed to save widget settings for page {pageId}: {ex.Message}", ex);
            }
        }

        public T? LoadWidgetSettingsForPage<T>(string widgetId, string pageId) where T : class, new()
        {
            try
            {
                // Check memory cache first
                if (_pageWidgetSettings.TryGetValue(pageId, out var pageSettings) &&
                    pageSettings.TryGetValue(widgetId, out var cached) && cached is T cachedSettings)
                {
                    return cachedSettings;
                }

                // Try to load from disk
                var settingsPath = Path.Combine(_dataPath, "Pages", pageId, "Widgets", widgetId, "settings.json");
                if (!File.Exists(settingsPath))
                {
                    // No page-specific settings found
                    return null;
                }

                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<T>(json);

                if (settings != null)
                {
                    // Cache the loaded settings
                    if (!_pageWidgetSettings.ContainsKey(pageId))
                    {
                        _pageWidgetSettings[pageId] = new Dictionary<string, object>();
                    }
                    _pageWidgetSettings[pageId][widgetId] = settings;
                    return settings;
                }
            }
            catch (Exception ex)
            {
                LogError(widgetId, $"Failed to load widget settings for page {pageId}: {ex.Message}", ex);
            }

            return null; // Return null to indicate no page-specific settings
        }

        public void ShowMessage(string title, string message, MessageType messageType = MessageType.Information)
        {
            // For now, just log the message. In a real implementation, this would show a UI dialog.
            var level = messageType switch
            {
                MessageType.Error => "ERROR",
                MessageType.Warning => "WARNING",
                MessageType.Question => "QUESTION",
                _ => "INFO"
            };

            _logger?.Invoke("PluginHost", $"[{level}] {title}: {message}", null);
        }

        public bool ShowConfirmation(string title, string message)
        {
            ShowMessage(title, message, MessageType.Question);
            // For testing purposes, always return true. In a real implementation, this would show a dialog.
            return true;
        }

        public void RequestWidgetRefresh(string widgetId)
        {
            LogInfo(widgetId, "Widget refresh requested");
        }

        public void RequestWidgetRemoval(string widgetId)
        {
            LogInfo(widgetId, "Widget removal requested");
        }

        public string GetWidgetDataPath(string widgetId)
        {
            var path = Path.Combine(_dataPath, "Widgets", widgetId);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        public string GetSharedDataPath()
        {
            var path = Path.Combine(_dataPath, "Shared");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        public void LogDebug(string widgetId, string message)
        {
            _logger?.Invoke(widgetId, $"[DEBUG] {message}", null);
        }

        public void LogInfo(string widgetId, string message)
        {
            _logger?.Invoke(widgetId, $"[INFO] {message}", null);
        }

        public void LogWarning(string widgetId, string message)
        {
            _logger?.Invoke(widgetId, $"[WARNING] {message}", null);
        }

        public void LogError(string widgetId, string message, Exception? exception = null)
        {
            _logger?.Invoke(widgetId, $"[ERROR] {message}", exception);
        }

        private void EnsureDataDirectoryExists()
        {
            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }
        }
    }
}