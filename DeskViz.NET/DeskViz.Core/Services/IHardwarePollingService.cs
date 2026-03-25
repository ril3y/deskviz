using System;

namespace DeskViz.Core.Services
{
    /// <summary>
    /// Provides centralized hardware polling to avoid redundant update calls from multiple widgets.
    /// </summary>
    public interface IHardwarePollingService : IDisposable
    {
        /// <summary>
        /// Fired when hardware data has been updated. This event is raised on the UI thread.
        /// </summary>
        event EventHandler? DataUpdated;

        /// <summary>
        /// Gets a value indicating whether the polling service is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the current polling interval in seconds.
        /// </summary>
        double IntervalSeconds { get; }

        /// <summary>
        /// Starts the hardware polling timer.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the hardware polling timer.
        /// </summary>
        void Stop();

        /// <summary>
        /// Sets the polling interval.
        /// </summary>
        /// <param name="seconds">The interval in seconds between hardware polls.</param>
        void SetInterval(double seconds);
    }
}
