using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DeskViz.Plugins.Interfaces;

namespace DeskViz.Plugins.Tests.Mocks
{
    public class MockWidgetHost : IWidgetHost
    {
        private readonly Dictionary<string, object> _widgetSettings = new();
        private readonly List<LogEntry> _logs = new();

        public IWidgetServiceProvider ServiceProvider { get; set; } = new MockServiceProvider();

        public List<LogEntry> Logs => _logs;
        public Dictionary<string, object> WidgetSettings => _widgetSettings;

        public string LastMessageTitle { get; private set; } = string.Empty;
        public string LastMessageText { get; private set; } = string.Empty;
        public MessageType LastMessageType { get; private set; }

        public void SaveWidgetSettings(string widgetId, object settings)
        {
            _widgetSettings[widgetId] = settings;
        }

        public T? LoadWidgetSettings<T>(string widgetId) where T : class, new()
        {
            if (_widgetSettings.TryGetValue(widgetId, out var settings) && settings is T typedSettings)
            {
                return typedSettings;
            }
            return new T();
        }

        public void SaveWidgetSettingsForPage(string widgetId, string pageId, object settings)
        {
            var key = $"{pageId}_{widgetId}";
            _widgetSettings[key] = settings;
        }

        public T? LoadWidgetSettingsForPage<T>(string widgetId, string pageId) where T : class, new()
        {
            var key = $"{pageId}_{widgetId}";
            if (_widgetSettings.TryGetValue(key, out var settings) && settings is T typedSettings)
                return typedSettings;

            return null; // Return null to indicate no page-specific settings
        }

        public void ShowMessage(string title, string message, MessageType messageType = MessageType.Information)
        {
            LastMessageTitle = title;
            LastMessageText = message;
            LastMessageType = messageType;
        }

        public bool ShowConfirmation(string title, string message)
        {
            ShowMessage(title, message, MessageType.Question);
            return true; // Mock always confirms
        }

        public void RequestWidgetRefresh(string widgetId)
        {
            // Mock implementation
        }

        public void RequestWidgetRemoval(string widgetId)
        {
            // Mock implementation
        }

        public string GetWidgetDataPath(string widgetId)
        {
            return Path.Combine(Path.GetTempPath(), "DeskViz", "Widgets", widgetId);
        }

        public string GetSharedDataPath()
        {
            return Path.Combine(Path.GetTempPath(), "DeskViz", "Shared");
        }

        public void LogDebug(string widgetId, string message)
        {
            _logs.Add(new LogEntry(widgetId, message, LogLevel.Debug));
        }

        public void LogInfo(string widgetId, string message)
        {
            _logs.Add(new LogEntry(widgetId, message, LogLevel.Info));
        }

        public void LogWarning(string widgetId, string message)
        {
            _logs.Add(new LogEntry(widgetId, message, LogLevel.Warning));
        }

        public void LogError(string widgetId, string message, Exception? exception = null)
        {
            _logs.Add(new LogEntry(widgetId, message, LogLevel.Error, exception));
        }
    }

    public class MockServiceProvider : IWidgetServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new();

        public IPluginHardwareMonitorService HardwareMonitor { get; set; } = new MockHardwareMonitorService();
        public IPluginHardwarePollingService HardwarePolling { get; set; } = new MockHardwarePollingService();
        public IPluginMediaControlService? MediaControl { get; set; }

        public void RegisterService<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
        }

        public T? GetService<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var service) ? service as T : null;
        }

        public object? GetService(Type serviceType)
        {
            return _services.TryGetValue(serviceType, out var service) ? service : null;
        }

        public bool IsServiceAvailable<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        public bool IsServiceAvailable(Type serviceType)
        {
            return _services.ContainsKey(serviceType);
        }
    }

    public record LogEntry(string WidgetId, string Message, LogLevel Level, Exception? Exception = null);

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public class MockHardwareMonitorService : IPluginHardwareMonitorService
    {
        public bool IsInitialized => true;

        public void Update() { }
        public Task UpdateAsync() => Task.CompletedTask;

        public string GetCpuName() => "Mock CPU";
        public float GetOverallCpuUsage() => 25.5f;
        public List<float> GetCpuCoreUsage() => new() { 20.0f, 30.0f, 25.0f, 15.0f };
        public float GetCpuPackageTemperature() => 45.5f;
        public float GetCpuClockSpeed() => 3200.0f;
        public float GetCpuPowerUsage() => 65.0f;

        public float GetRamUsagePercentage() => 60.0f;
        public (float TotalMB, float AvailableMB) GetRamInfo() => (16384.0f, 6553.6f);
        public float GetUsedRamMB() => 9830.4f;

        public int GetGpuCount() => 1;
        public List<(int Index, string Name, string Type)> GetAvailableGpus() =>
            new() { (0, "Mock GPU", "NVIDIA") };
        public string GetGpuName(int gpuIndex = 0) => "Mock GPU";
        public float GetGpuUsage(int gpuIndex = 0) => 15.0f;
        public float GetGpuTemperature(int gpuIndex = 0) => 55.0f;
        public (float UsedMB, float TotalMB, float UsagePercent) GetGpuMemoryInfo(int gpuIndex = 0) =>
            (2048.0f, 8192.0f, 25.0f);
        public float GetGpuClockSpeed(int gpuIndex = 0) => 1800.0f;
        public float GetGpuPowerUsage(int gpuIndex = 0) => 120.0f;

        public List<DeskViz.Plugins.Interfaces.DriveInfo> GetDriveInfo() => new()
        {
            new DeskViz.Plugins.Interfaces.DriveInfo("C:", "System", 1000000000000, 500000000000, 50.0f, 35.0f)
        };
        public float GetDriveTemperature(string driveName) => 35.0f;
    }

    public class MockHardwarePollingService : IPluginHardwarePollingService
    {
        public event EventHandler? DataUpdated;
        public bool IsRunning => true;
        public double IntervalSeconds => 2.5;
        public void SetInterval(double seconds) { }
        public void FireDataUpdated() => DataUpdated?.Invoke(this, EventArgs.Empty);
    }
}