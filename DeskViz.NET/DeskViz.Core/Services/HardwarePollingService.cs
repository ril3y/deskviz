using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeskViz.Core.Services
{
    /// <summary>
    /// Centralized service for polling hardware data at regular intervals.
    /// Replaces per-widget DispatcherTimers with a single coordinated polling mechanism.
    /// </summary>
    public class HardwarePollingService : IHardwarePollingService
    {
        private readonly IHardwareMonitorService _hardwareMonitorService;
        private readonly SynchronizationContext? _synchronizationContext;
        private readonly object _lock = new object();

        private System.Threading.Timer? _timer;
        private double _intervalSeconds = 1.0; // Reduced from 2.0 to 1.0 for smoother updates
        private bool _isRunning;
        private bool _isDisposed;
        private bool _isPolling;

        /// <inheritdoc />
        public event EventHandler? DataUpdated;

        /// <inheritdoc />
        public bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _isRunning;
                }
            }
        }

        /// <inheritdoc />
        public double IntervalSeconds
        {
            get
            {
                lock (_lock)
                {
                    return _intervalSeconds;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HardwarePollingService"/> class.
        /// </summary>
        /// <param name="hardwareMonitorService">The hardware monitor service to poll.</param>
        public HardwarePollingService(IHardwareMonitorService hardwareMonitorService)
        {
            _hardwareMonitorService = hardwareMonitorService ?? throw new ArgumentNullException(nameof(hardwareMonitorService));

            // Capture the current synchronization context (should be the UI thread context)
            _synchronizationContext = SynchronizationContext.Current;
        }

        /// <inheritdoc />
        public void Start()
        {
            lock (_lock)
            {
                ThrowIfDisposed();

                if (_isRunning)
                {
                    return;
                }

                var intervalMs = (int)(_intervalSeconds * 1000);
                _timer = new System.Threading.Timer(OnTimerElapsed, null, 0, intervalMs);
                _isRunning = true;
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
            lock (_lock)
            {
                ThrowIfDisposed();

                if (!_isRunning)
                {
                    return;
                }

                _timer?.Change(Timeout.Infinite, Timeout.Infinite);
                _timer?.Dispose();
                _timer = null;
                _isRunning = false;
            }
        }

        /// <inheritdoc />
        public void SetInterval(double seconds)
        {
            if (seconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), "Interval must be greater than zero.");
            }

            lock (_lock)
            {
                ThrowIfDisposed();

                _intervalSeconds = seconds;

                // If the timer is running, update its interval
                if (_isRunning && _timer != null)
                {
                    var intervalMs = (int)(seconds * 1000);
                    _timer.Change(intervalMs, intervalMs);
                }
            }
        }

        private void OnTimerElapsed(object? state)
        {
            // Prevent overlapping polls if hardware update takes longer than the interval
            lock (_lock)
            {
                if (_isPolling || _isDisposed || !_isRunning)
                {
                    return;
                }
                _isPolling = true;
            }

            // Run the hardware update on a background thread
            Task.Run(() =>
            {
                try
                {
                    _hardwareMonitorService.Update();
                }
                catch (Exception)
                {
                    // Swallow exceptions to prevent timer from stopping
                    // In production, consider logging the exception
                }
                finally
                {
                    lock (_lock)
                    {
                        _isPolling = false;
                    }

                    // Raise the event on the UI thread
                    RaiseDataUpdatedOnUIThread();
                }
            });
        }

        private void RaiseDataUpdatedOnUIThread()
        {
            var handler = DataUpdated;
            if (handler == null)
            {
                return;
            }

            if (_synchronizationContext != null)
            {
                // Post to the UI thread
                _synchronizationContext.Post(_ =>
                {
                    try
                    {
                        handler.Invoke(this, EventArgs.Empty);
                    }
                    catch (Exception)
                    {
                        // Swallow exceptions from event handlers
                        // In production, consider logging the exception
                    }
                }, null);
            }
            else
            {
                // No synchronization context available, invoke directly
                try
                {
                    handler.Invoke(this, EventArgs.Empty);
                }
                catch (Exception)
                {
                    // Swallow exceptions from event handlers
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(HardwarePollingService));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                lock (_lock)
                {
                    _isDisposed = true;
                    _isRunning = false;

                    _timer?.Change(Timeout.Infinite, Timeout.Infinite);
                    _timer?.Dispose();
                    _timer = null;
                }

                // Clear event handlers
                DataUpdated = null;
            }
        }
    }
}
