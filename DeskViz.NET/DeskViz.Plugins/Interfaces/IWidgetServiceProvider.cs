using System;

namespace DeskViz.Plugins.Interfaces
{
    public interface IWidgetServiceProvider
    {
        IPluginHardwareMonitorService HardwareMonitor { get; }
        IPluginHardwarePollingService HardwarePolling { get; }
        IPluginMediaControlService? MediaControl { get; }

        T? GetService<T>() where T : class;
        object? GetService(Type serviceType);
        bool IsServiceAvailable<T>() where T : class;
        bool IsServiceAvailable(Type serviceType);
    }
}