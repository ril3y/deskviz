using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeskViz.Core.Services
{
    /// <summary>
    /// Service for controlling media playback through Windows Media Session Manager
    /// </summary>
    public interface IMediaControlService : IDisposable
    {
        /// <summary>
        /// Gets whether the service is initialized and ready to use
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Event fired when media session information changes
        /// </summary>
        event EventHandler<MediaSessionChangedEventArgs>? MediaSessionChanged;

        /// <summary>
        /// Event fired when playback state changes
        /// </summary>
        event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;

        /// <summary>
        /// Initializes the media control service
        /// </summary>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Gets the current media session information
        /// </summary>
        MediaSessionInfo? GetCurrentSession();

        /// <summary>
        /// Gets all available media sessions
        /// </summary>
        List<MediaSessionInfo> GetAllSessions();

        /// <summary>
        /// Plays the current media
        /// </summary>
        Task<bool> PlayAsync();

        /// <summary>
        /// Pauses the current media
        /// </summary>
        Task<bool> PauseAsync();

        /// <summary>
        /// Stops the current media
        /// </summary>
        Task<bool> StopAsync();

        /// <summary>
        /// Skips to the next track
        /// </summary>
        Task<bool> NextAsync();

        /// <summary>
        /// Skips to the previous track
        /// </summary>
        Task<bool> PreviousAsync();

        /// <summary>
        /// Sets the volume (0.0 to 1.0)
        /// </summary>
        Task<bool> SetVolumeAsync(double volume);

        /// <summary>
        /// Gets the current volume (0.0 to 1.0)
        /// </summary>
        double GetVolume();

        /// <summary>
        /// Sets the active media session by ID
        /// </summary>
        Task<bool> SetActiveSessionAsync(string sessionId);
    }

    /// <summary>
    /// Information about a media session
    /// </summary>
    public class MediaSessionInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public byte[]? AlbumArt { get; set; }
        public PlaybackState State { get; set; }
        public TimeSpan Position { get; set; }
        public TimeSpan Duration { get; set; }
        public bool CanPlay { get; set; }
        public bool CanPause { get; set; }
        public bool CanStop { get; set; }
        public bool CanSkipNext { get; set; }
        public bool CanSkipPrevious { get; set; }
    }

    /// <summary>
    /// Playback state enumeration
    /// </summary>
    public enum PlaybackState
    {
        Unknown,
        Closed,
        Opened,
        Changing,
        Stopped,
        Playing,
        Paused
    }

    /// <summary>
    /// Event arguments for media session changes
    /// </summary>
    public class MediaSessionChangedEventArgs : EventArgs
    {
        public MediaSessionInfo? CurrentSession { get; set; }
    }

    /// <summary>
    /// Event arguments for playback state changes
    /// </summary>
    public class PlaybackStateChangedEventArgs : EventArgs
    {
        public PlaybackState State { get; set; }
        public MediaSessionInfo? Session { get; set; }
    }
}