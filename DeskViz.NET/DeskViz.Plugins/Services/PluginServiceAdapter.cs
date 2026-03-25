using System;
using DeskViz.Plugins.Interfaces;

namespace DeskViz.Plugins.Services
{
    public class PluginServiceProvider : IWidgetServiceProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public PluginServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IPluginHardwareMonitorService HardwareMonitor =>
            GetService<IPluginHardwareMonitorService>() ?? throw new InvalidOperationException("Hardware monitor service not available");

        public IPluginHardwarePollingService HardwarePolling =>
            GetService<IPluginHardwarePollingService>() ?? throw new InvalidOperationException("Hardware polling service not available");

        public IPluginMediaControlService? MediaControl =>
            GetService<IPluginMediaControlService>();

        public T? GetService<T>() where T : class
        {
            return _serviceProvider.GetService(typeof(T)) as T;
        }

        public object? GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        public bool IsServiceAvailable<T>() where T : class
        {
            return _serviceProvider.GetService(typeof(T)) != null;
        }

        public bool IsServiceAvailable(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType) != null;
        }
    }
}