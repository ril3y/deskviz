using System;

namespace DeskViz.Plugins.Interfaces
{
    /// <summary>
    /// Interface for the centralized hardware polling service available to plugins.
    /// Provides event-driven notifications when hardware data has been updated.
    /// </summary>
    public interface IPluginHardwarePollingService
    {
        /// <summary>
        /// Event fired when hardware data has been updated.
        /// Widgets should subscribe to this event and read cached values from IPluginHardwareMonitorService.
        /// </summary>
        event EventHandler? DataUpdated;

        /// <summary>
        /// Gets whether the polling service is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the current polling interval in seconds.
        /// </summary>
        double IntervalSeconds { get; }

        /// <summary>
        /// Sets the polling interval.
        /// </summary>
        /// <param name="seconds">The interval in seconds (minimum 0.5 seconds).</param>
        void SetInterval(double seconds);
    }
}
