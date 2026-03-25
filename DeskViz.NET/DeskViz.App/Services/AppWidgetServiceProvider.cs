using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeskViz.Core.Services;
using DeskViz.Plugins.Interfaces;

namespace DeskViz.App.Services
{
    public class AppWidgetServiceProvider : IWidgetServiceProvider
    {
        private readonly IPluginHardwareMonitorService _hardwareMonitorAdapter;
        private readonly IPluginHardwarePollingService _hardwarePollingAdapter;
        private readonly IPluginMediaControlService _mediaControlAdapter;

        public AppWidgetServiceProvider(
            IHardwareMonitorService hardwareMonitorService,
            IHardwarePollingService hardwarePollingService,
            IMediaControlService mediaControlService)
        {
            ArgumentNullException.ThrowIfNull(hardwareMonitorService);
            ArgumentNullException.ThrowIfNull(hardwarePollingService);
            ArgumentNullException.ThrowIfNull(mediaControlService);

            _hardwareMonitorAdapter = new HardwareMonitorAdapter(hardwareMonitorService);
            _hardwarePollingAdapter = new HardwarePollingAdapter(hardwarePollingService);
            _mediaControlAdapter = new MediaControlAdapter(mediaControlService);
        }

        public IPluginHardwareMonitorService HardwareMonitor => _hardwareMonitorAdapter;
        public IPluginHardwarePollingService HardwarePolling => _hardwarePollingAdapter;
        public IPluginMediaControlService? MediaControl => _mediaControlAdapter;

        public T? GetService<T>() where T : class
        {
            if (typeof(T) == typeof(IPluginHardwareMonitorService))
                return _hardwareMonitorAdapter as T;
            if (typeof(T) == typeof(IPluginHardwarePollingService))
                return _hardwarePollingAdapter as T;
            if (typeof(T) == typeof(IPluginMediaControlService))
                return _mediaControlAdapter as T;
            return null;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IPluginHardwareMonitorService))
                return _hardwareMonitorAdapter;
            if (serviceType == typeof(IPluginHardwarePollingService))
                return _hardwarePollingAdapter;
            if (serviceType == typeof(IPluginMediaControlService))
                return _mediaControlAdapter;
            return null;
        }

        public bool IsServiceAvailable<T>() where T : class
        {
            return typeof(T) == typeof(IPluginHardwareMonitorService)
                || typeof(T) == typeof(IPluginHardwarePollingService)
                || typeof(T) == typeof(IPluginMediaControlService);
        }

        public bool IsServiceAvailable(Type serviceType)
        {
            return serviceType == typeof(IPluginHardwareMonitorService)
                || serviceType == typeof(IPluginHardwarePollingService)
                || serviceType == typeof(IPluginMediaControlService);
        }

        /// <summary>
        /// Direct delegation adapter — no reflection.
        /// </summary>
        private sealed class HardwareMonitorAdapter : IPluginHardwareMonitorService
        {
            private readonly IHardwareMonitorService _core;
            public HardwareMonitorAdapter(IHardwareMonitorService core) => _core = core;

            public bool IsInitialized => _core.IsInitialized;
            public void Update() => _core.Update();
            public Task UpdateAsync() => _core.UpdateAsync();

            public string GetCpuName() => _core.GetCpuName();
            public float GetOverallCpuUsage() => _core.GetOverallCpuUsage();
            public List<float> GetCpuCoreUsage() => _core.GetCpuCoreUsage();
            public float GetCpuPackageTemperature() => _core.GetCpuPackageTemperature();
            public float GetCpuClockSpeed() => _core.GetCpuClockSpeed();
            public float GetCpuPowerUsage() => _core.GetCpuPowerUsage();

            public float GetRamUsagePercentage() => _core.GetRamUsagePercentage();
            public (float TotalMB, float AvailableMB) GetRamInfo()
            {
                var info = _core.GetRamInfo();
                return (info.Total, info.Available);
            }
            public float GetUsedRamMB() => _core.GetUsedRam();

            public int GetGpuCount() => _core.GetGpuCount();
            public List<(int Index, string Name, string Type)> GetAvailableGpus() => _core.GetAvailableGpus();
            public string GetGpuName(int gpuIndex = 0) => _core.GetGpuName(gpuIndex);
            public float GetGpuUsage(int gpuIndex = 0) => _core.GetGpuUsage(gpuIndex);
            public float GetGpuTemperature(int gpuIndex = 0) => _core.GetGpuTemperature(gpuIndex);
            public (float UsedMB, float TotalMB, float UsagePercent) GetGpuMemoryInfo(int gpuIndex = 0) => _core.GetGpuMemoryInfo(gpuIndex);
            public float GetGpuClockSpeed(int gpuIndex = 0) => _core.GetGpuClockSpeed(gpuIndex);
            public float GetGpuPowerUsage(int gpuIndex = 0) => _core.GetGpuPowerUsage(gpuIndex);

            public List<Plugins.Interfaces.DriveInfo> GetDriveInfo()
            {
                return _core.GetDriveInfo().Select(d => new Plugins.Interfaces.DriveInfo(
                    d.Name, d.Label, d.TotalBytes, d.UsedBytes, d.UsagePercent,
                    _core.GetDriveTemperature(d.Name)
                )).ToList();
            }
            public float GetDriveTemperature(string driveName) => _core.GetDriveTemperature(driveName);
        }

        /// <summary>
        /// Direct delegation adapter — no reflection.
        /// </summary>
        private sealed class HardwarePollingAdapter : IPluginHardwarePollingService
        {
            private readonly IHardwarePollingService _core;
            public HardwarePollingAdapter(IHardwarePollingService core)
            {
                _core = core;
                _core.DataUpdated += (s, e) => DataUpdated?.Invoke(this, e);
            }

            public event EventHandler? DataUpdated;
            public bool IsRunning => _core.IsRunning;
            public double IntervalSeconds => _core.IntervalSeconds;
            public void SetInterval(double seconds) => _core.SetInterval(seconds);
        }

        /// <summary>
        /// Direct delegation adapter — no reflection.
        /// </summary>
        private sealed class MediaControlAdapter : IPluginMediaControlService
        {
            private readonly IMediaControlService _core;
            public MediaControlAdapter(IMediaControlService core)
            {
                _core = core;
                _core.MediaSessionChanged += (s, e) => MediaSessionChanged?.Invoke(this,
                    new PluginMediaSessionChangedEventArgs { CurrentSession = ConvertSession(e.CurrentSession) });
                _core.PlaybackStateChanged += (s, e) => PlaybackStateChanged?.Invoke(this,
                    new PluginPlaybackStateChangedEventArgs
                    {
                        State = ConvertState(e.State),
                        Session = ConvertSession(e.Session)
                    });
            }

            public bool IsInitialized => _core.IsInitialized;
            public event EventHandler<PluginMediaSessionChangedEventArgs>? MediaSessionChanged;
            public event EventHandler<PluginPlaybackStateChangedEventArgs>? PlaybackStateChanged;

            public Task<bool> InitializeAsync() => _core.InitializeAsync();
            public PluginMediaSessionInfo? GetCurrentSession() => ConvertSession(_core.GetCurrentSession());
            public List<PluginMediaSessionInfo> GetAllSessions() =>
                _core.GetAllSessions().Select(s => ConvertSession(s)!).Where(s => s != null).ToList()!;

            public Task<bool> PlayAsync() => _core.PlayAsync();
            public Task<bool> PauseAsync() => _core.PauseAsync();
            public Task<bool> StopAsync() => _core.StopAsync();
            public Task<bool> NextAsync() => _core.NextAsync();
            public Task<bool> PreviousAsync() => _core.PreviousAsync();
            public Task<bool> SetVolumeAsync(double volume) => _core.SetVolumeAsync(volume);
            public double GetVolume() => _core.GetVolume();
            public Task<bool> SetActiveSessionAsync(string sessionId) => _core.SetActiveSessionAsync(sessionId);

            private static PluginMediaSessionInfo? ConvertSession(MediaSessionInfo? s)
            {
                if (s == null) return null;
                return new PluginMediaSessionInfo
                {
                    Id = s.Id, Title = s.Title, Artist = s.Artist, Album = s.Album,
                    AppName = s.AppName, AlbumArt = s.AlbumArt,
                    State = ConvertState(s.State),
                    Position = s.Position, Duration = s.Duration,
                    CanPlay = s.CanPlay, CanPause = s.CanPause, CanStop = s.CanStop,
                    CanSkipNext = s.CanSkipNext, CanSkipPrevious = s.CanSkipPrevious
                };
            }

            private static PluginPlaybackState ConvertState(PlaybackState state) => state switch
            {
                PlaybackState.Closed => PluginPlaybackState.Closed,
                PlaybackState.Opened => PluginPlaybackState.Opened,
                PlaybackState.Changing => PluginPlaybackState.Changing,
                PlaybackState.Stopped => PluginPlaybackState.Stopped,
                PlaybackState.Playing => PluginPlaybackState.Playing,
                PlaybackState.Paused => PluginPlaybackState.Paused,
                _ => PluginPlaybackState.Unknown
            };
        }
    }
}
