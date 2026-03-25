using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeskViz.Plugins.Interfaces
{
    public interface IPluginMediaControlService
    {
        bool IsInitialized { get; }

        event EventHandler<PluginMediaSessionChangedEventArgs>? MediaSessionChanged;
        event EventHandler<PluginPlaybackStateChangedEventArgs>? PlaybackStateChanged;

        Task<bool> InitializeAsync();
        PluginMediaSessionInfo? GetCurrentSession();
        List<PluginMediaSessionInfo> GetAllSessions();

        Task<bool> PlayAsync();
        Task<bool> PauseAsync();
        Task<bool> StopAsync();
        Task<bool> NextAsync();
        Task<bool> PreviousAsync();
        Task<bool> SetVolumeAsync(double volume);
        double GetVolume();
        Task<bool> SetActiveSessionAsync(string sessionId);
    }

    public class PluginMediaSessionInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public byte[]? AlbumArt { get; set; }
        public PluginPlaybackState State { get; set; }
        public TimeSpan Position { get; set; }
        public TimeSpan Duration { get; set; }
        public bool CanPlay { get; set; }
        public bool CanPause { get; set; }
        public bool CanStop { get; set; }
        public bool CanSkipNext { get; set; }
        public bool CanSkipPrevious { get; set; }
    }

    public enum PluginPlaybackState
    {
        Unknown,
        Closed,
        Opened,
        Changing,
        Stopped,
        Playing,
        Paused
    }

    public class PluginMediaSessionChangedEventArgs : EventArgs
    {
        public PluginMediaSessionInfo? CurrentSession { get; set; }
    }

    public class PluginPlaybackStateChangedEventArgs : EventArgs
    {
        public PluginPlaybackState State { get; set; }
        public PluginMediaSessionInfo? Session { get; set; }
    }
}