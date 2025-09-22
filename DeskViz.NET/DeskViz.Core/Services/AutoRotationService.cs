using System;
using System.Timers;

namespace DeskViz.Core.Services
{
    /// <summary>
    /// Service for managing automatic page rotation
    /// </summary>
    public class AutoRotationService
    {
        private readonly SettingsService _settingsService;
        private System.Timers.Timer _rotationTimer;
        private bool _isPaused = false;
        private bool _pingPongDirection = true; // true = forward, false = backward

        /// <summary>
        /// Event raised when a page rotation should occur
        /// </summary>
        public event EventHandler<PageRotationEventArgs>? PageRotationRequested;

        /// <summary>
        /// Event raised when auto-rotation is paused or resumed
        /// </summary>
        public event EventHandler<bool>? AutoRotationStateChanged;

        /// <summary>
        /// Initializes a new instance of the AutoRotationService
        /// </summary>
        public AutoRotationService(SettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _rotationTimer = new System.Timers.Timer();
            _rotationTimer.Elapsed += OnRotationTick;
            _rotationTimer.AutoReset = true;

            UpdateTimerSettings();
        }

        /// <summary>
        /// Gets whether auto-rotation is currently enabled
        /// </summary>
        public bool IsEnabled => _settingsService.Settings.AutoRotationEnabled && !_isPaused;

        /// <summary>
        /// Gets whether auto-rotation is currently paused
        /// </summary>
        public bool IsPaused => _isPaused;

        /// <summary>
        /// Starts auto-rotation if enabled in settings
        /// </summary>
        public void Start()
        {
            if (_settingsService.Settings.AutoRotationEnabled)
            {
                UpdateTimerSettings();
                _rotationTimer.Enabled = true;
                AutoRotationStateChanged?.Invoke(this, true);
            }
        }

        /// <summary>
        /// Stops auto-rotation
        /// </summary>
        public void Stop()
        {
            _rotationTimer.Enabled = false;
            AutoRotationStateChanged?.Invoke(this, false);
        }

        /// <summary>
        /// Pauses auto-rotation temporarily
        /// </summary>
        public void Pause()
        {
            if (!_isPaused && _rotationTimer.Enabled)
            {
                _isPaused = true;
                _rotationTimer.Enabled = false;
                AutoRotationStateChanged?.Invoke(this, false);
            }
        }

        /// <summary>
        /// Resumes auto-rotation if it was paused
        /// </summary>
        public void Resume()
        {
            if (_isPaused && _settingsService.Settings.AutoRotationEnabled)
            {
                _isPaused = false;
                _rotationTimer.Enabled = true;
                AutoRotationStateChanged?.Invoke(this, true);
            }
        }

        /// <summary>
        /// Updates the timer settings based on current configuration
        /// </summary>
        public void UpdateTimerSettings()
        {
            var settings = _settingsService.Settings;
            _rotationTimer.Interval = Math.Max(1000, settings.AutoRotationIntervalSeconds * 1000); // Convert to milliseconds

            if (settings.AutoRotationEnabled && !_isPaused)
            {
                if (!_rotationTimer.Enabled)
                {
                    _rotationTimer.Enabled = true;
                }
            }
            else
            {
                _rotationTimer.Enabled = false;
            }
        }

        /// <summary>
        /// Handles user interaction that may pause auto-rotation
        /// </summary>
        public void OnUserInteraction()
        {
            if (_settingsService.Settings.PauseOnUserInteraction && IsEnabled)
            {
                Pause();

                // Auto-resume after a delay (optional feature)
                var resumeTimer = new System.Timers.Timer(5000); // Resume after 5 seconds of no interaction
                resumeTimer.Elapsed += (s, e) =>
                {
                    resumeTimer.Enabled = false;
                    resumeTimer.Dispose();
                    Resume();
                };
                resumeTimer.AutoReset = false;
                resumeTimer.Enabled = true;
            }
        }

        /// <summary>
        /// Manually triggers the next page rotation
        /// </summary>
        public void TriggerNextRotation()
        {
            OnRotationTick(this, null);
        }

        private void OnRotationTick(object? sender, ElapsedEventArgs? e)
        {
            if (!_settingsService.Settings.AutoRotationEnabled || _isPaused)
            {
                Stop();
                return;
            }

            var currentPageIndex = _settingsService.Settings.CurrentPageIndex;
            var nextPageIndex = _settingsService.GetNextPageIndex(
                _settingsService.Settings.RotationMode,
                currentPageIndex,
                ref _pingPongDirection);

            if (nextPageIndex != currentPageIndex)
            {
                var eventArgs = new PageRotationEventArgs(currentPageIndex, nextPageIndex, _settingsService.Settings.RotationMode);
                PageRotationRequested?.Invoke(this, eventArgs);
            }
        }
    }

    /// <summary>
    /// Event arguments for page rotation events
    /// </summary>
    public class PageRotationEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the current page index
        /// </summary>
        public int CurrentPageIndex { get; }

        /// <summary>
        /// Gets the next page index to rotate to
        /// </summary>
        public int NextPageIndex { get; }

        /// <summary>
        /// Gets the rotation mode being used
        /// </summary>
        public AutoRotationMode RotationMode { get; }

        /// <summary>
        /// Initializes a new instance of the PageRotationEventArgs
        /// </summary>
        public PageRotationEventArgs(int currentPageIndex, int nextPageIndex, AutoRotationMode rotationMode)
        {
            CurrentPageIndex = currentPageIndex;
            NextPageIndex = nextPageIndex;
            RotationMode = rotationMode;
        }
    }
}