using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace DeskViz.Core.Services
{
    /// <summary>
    /// Windows implementation of media control service using Windows Media Session Manager
    /// </summary>
    public class WindowsMediaControlService : IMediaControlService
    {
        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
        private GlobalSystemMediaTransportControlsSession? _currentSession;
        private bool _isInitialized = false;

        public bool IsInitialized => _isInitialized;

        public event EventHandler<MediaSessionChangedEventArgs>? MediaSessionChanged;
        public event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;

        public async Task<bool> InitializeAsync()
        {
            try
            {
                Debug.WriteLine("Initializing WindowsMediaControlService...");
                
                // Request access to media control
                _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                
                if (_sessionManager != null)
                {
                    // Subscribe to session changes
                    _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;
                    _sessionManager.SessionsChanged += OnSessionsChanged;
                    
                    // Set initial current session
                    _currentSession = _sessionManager.GetCurrentSession();
                    if (_currentSession != null)
                    {
                        SubscribeToSessionEvents(_currentSession);
                    }
                    
                    _isInitialized = true;
                    Debug.WriteLine("WindowsMediaControlService initialized successfully.");
                    return true;
                }
                
                Debug.WriteLine("Failed to get GlobalSystemMediaTransportControlsSessionManager.");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing WindowsMediaControlService: {ex.Message}");
                return false;
            }
        }

        private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            try
            {
                // Unsubscribe from old session
                if (_currentSession != null)
                {
                    UnsubscribeFromSessionEvents(_currentSession);
                }
                
                // Subscribe to new session
                _currentSession = sender.GetCurrentSession();
                if (_currentSession != null)
                {
                    SubscribeToSessionEvents(_currentSession);
                }
                
                // Notify of session change
                var sessionInfo = GetCurrentSession();
                MediaSessionChanged?.Invoke(this, new MediaSessionChangedEventArgs { CurrentSession = sessionInfo });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnCurrentSessionChanged: {ex.Message}");
            }
        }

        private void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        {
            try
            {
                // Update current session if needed
                var newCurrentSession = sender.GetCurrentSession();
                if (newCurrentSession?.SourceAppUserModelId != _currentSession?.SourceAppUserModelId)
                {
                    OnCurrentSessionChanged(sender, null!);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnSessionsChanged: {ex.Message}");
            }
        }

        private void SubscribeToSessionEvents(GlobalSystemMediaTransportControlsSession session)
        {
            try
            {
                session.MediaPropertiesChanged += OnMediaPropertiesChanged;
                session.PlaybackInfoChanged += OnPlaybackInfoChanged;
                session.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error subscribing to session events: {ex.Message}");
            }
        }

        private void UnsubscribeFromSessionEvents(GlobalSystemMediaTransportControlsSession session)
        {
            try
            {
                session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                session.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                session.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error unsubscribing from session events: {ex.Message}");
            }
        }

        private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            try
            {
                var sessionInfo = GetCurrentSession();
                MediaSessionChanged?.Invoke(this, new MediaSessionChangedEventArgs { CurrentSession = sessionInfo });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnMediaPropertiesChanged: {ex.Message}");
            }
        }

        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            try
            {
                var sessionInfo = GetCurrentSession();
                if (sessionInfo != null)
                {
                    PlaybackStateChanged?.Invoke(this, new PlaybackStateChangedEventArgs 
                    { 
                        State = sessionInfo.State, 
                        Session = sessionInfo 
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnPlaybackInfoChanged: {ex.Message}");
            }
        }

        private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
        {
            try
            {
                var sessionInfo = GetCurrentSession();
                MediaSessionChanged?.Invoke(this, new MediaSessionChangedEventArgs { CurrentSession = sessionInfo });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnTimelinePropertiesChanged: {ex.Message}");
            }
        }

        public MediaSessionInfo? GetCurrentSession()
        {
            try
            {
                if (_currentSession == null) return null;

                var mediaProperties = _currentSession.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
                var playbackInfo = _currentSession.GetPlaybackInfo();
                var timelineProperties = _currentSession.GetTimelineProperties();

                var sessionInfo = new MediaSessionInfo
                {
                    Id = _currentSession.SourceAppUserModelId ?? "Unknown",
                    Title = mediaProperties?.Title ?? "Unknown Title",
                    Artist = mediaProperties?.Artist ?? "Unknown Artist",
                    Album = mediaProperties?.AlbumTitle ?? "Unknown Album",
                    AppName = GetAppNameFromId(_currentSession.SourceAppUserModelId),
                    State = ConvertPlaybackState(playbackInfo.PlaybackStatus),
                    Position = timelineProperties.Position,
                    Duration = timelineProperties.EndTime - timelineProperties.StartTime,
                    CanPlay = playbackInfo.Controls.IsPlayEnabled,
                    CanPause = playbackInfo.Controls.IsPauseEnabled,
                    CanStop = playbackInfo.Controls.IsStopEnabled,
                    CanSkipNext = playbackInfo.Controls.IsNextEnabled,
                    CanSkipPrevious = playbackInfo.Controls.IsPreviousEnabled
                };

                // Try to get album art
                try
                {
                    var thumbnail = mediaProperties?.Thumbnail;
                    if (thumbnail != null)
                    {
                        using var stream = thumbnail.OpenReadAsync().GetAwaiter().GetResult();
                        using var reader = new DataReader(stream);
                        var bytes = new byte[stream.Size];
                        reader.LoadAsync((uint)stream.Size).GetAwaiter().GetResult();
                        reader.ReadBytes(bytes);
                        sessionInfo.AlbumArt = bytes;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading album art: {ex.Message}");
                }

                return sessionInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting current session: {ex.Message}");
                return null;
            }
        }

        public List<MediaSessionInfo> GetAllSessions()
        {
            var sessions = new List<MediaSessionInfo>();
            
            try
            {
                if (_sessionManager == null) return sessions;

                var allSessions = _sessionManager.GetSessions();
                foreach (var session in allSessions)
                {
                    try
                    {
                        var mediaProperties = session.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
                        var playbackInfo = session.GetPlaybackInfo();
                        
                        var sessionInfo = new MediaSessionInfo
                        {
                            Id = session.SourceAppUserModelId ?? "Unknown",
                            Title = mediaProperties?.Title ?? "Unknown Title",
                            Artist = mediaProperties?.Artist ?? "Unknown Artist",
                            Album = mediaProperties?.AlbumTitle ?? "Unknown Album",
                            AppName = GetAppNameFromId(session.SourceAppUserModelId),
                            State = ConvertPlaybackState(playbackInfo.PlaybackStatus),
                            CanPlay = playbackInfo.Controls.IsPlayEnabled,
                            CanPause = playbackInfo.Controls.IsPauseEnabled,
                            CanStop = playbackInfo.Controls.IsStopEnabled,
                            CanSkipNext = playbackInfo.Controls.IsNextEnabled,
                            CanSkipPrevious = playbackInfo.Controls.IsPreviousEnabled
                        };
                        
                        sessions.Add(sessionInfo);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing session: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting all sessions: {ex.Message}");
            }

            return sessions;
        }

        public async Task<bool> PlayAsync()
        {
            try
            {
                if (_currentSession?.GetPlaybackInfo().Controls.IsPlayEnabled == true)
                {
                    await _currentSession.TryPlayAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error playing: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PauseAsync()
        {
            try
            {
                if (_currentSession?.GetPlaybackInfo().Controls.IsPauseEnabled == true)
                {
                    await _currentSession.TryPauseAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error pausing: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> StopAsync()
        {
            try
            {
                if (_currentSession?.GetPlaybackInfo().Controls.IsStopEnabled == true)
                {
                    await _currentSession.TryStopAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> NextAsync()
        {
            try
            {
                if (_currentSession?.GetPlaybackInfo().Controls.IsNextEnabled == true)
                {
                    await _currentSession.TrySkipNextAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error skipping next: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PreviousAsync()
        {
            try
            {
                if (_currentSession?.GetPlaybackInfo().Controls.IsPreviousEnabled == true)
                {
                    await _currentSession.TrySkipPreviousAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error skipping previous: {ex.Message}");
                return false;
            }
        }

        public Task<bool> SetVolumeAsync(double volume)
        {
            try
            {
                SetMasterVolume((float)volume);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting volume: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        public double GetVolume()
        {
            try
            {
                return GetMasterVolume() * 100.0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting volume: {ex.Message}");
                return 100.0;
            }
        }

        public Task<bool> SetActiveSessionAsync(string sessionId)
        {
            try
            {
                if (_sessionManager == null) return Task.FromResult(false);

                var sessions = _sessionManager.GetSessions();
                var targetSession = sessions.FirstOrDefault(s => s.SourceAppUserModelId == sessionId);
                
                if (targetSession != null)
                {
                    // Unsubscribe from current session
                    if (_currentSession != null)
                    {
                        UnsubscribeFromSessionEvents(_currentSession);
                    }
                    
                    // Set new session
                    _currentSession = targetSession;
                    SubscribeToSessionEvents(_currentSession);
                    
                    var sessionInfo = GetCurrentSession();
                    MediaSessionChanged?.Invoke(this, new MediaSessionChangedEventArgs { CurrentSession = sessionInfo });
                    
                    return Task.FromResult(true);
                }
                
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting active session: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        private static PlaybackState ConvertPlaybackState(GlobalSystemMediaTransportControlsSessionPlaybackStatus status)
        {
            return status switch
            {
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed => PlaybackState.Closed,
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Opened => PlaybackState.Opened,
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Changing => PlaybackState.Changing,
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => PlaybackState.Stopped,
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => PlaybackState.Playing,
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => PlaybackState.Paused,
                _ => PlaybackState.Unknown
            };
        }

        private static string GetAppNameFromId(string? appId)
        {
            if (string.IsNullOrEmpty(appId)) return "Unknown App";
            
            // Extract app name from app ID
            var parts = appId.Split('.');
            return parts.Length > 0 ? parts[^1] : appId;
        }

        public void Dispose()
        {
            try
            {
                if (_currentSession != null)
                {
                    UnsubscribeFromSessionEvents(_currentSession);
                    _currentSession = null;
                }
                
                if (_sessionManager != null)
                {
                    _sessionManager.CurrentSessionChanged -= OnCurrentSessionChanged;
                    _sessionManager.SessionsChanged -= OnSessionsChanged;
                    _sessionManager = null;
                }

                if (_volumeControl != null)
                {
                    Marshal.ReleaseComObject(_volumeControl);
                    _volumeControl = null;
                }
                
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disposing WindowsMediaControlService: {ex.Message}");
            }
        }

        #region Windows Core Audio API for Volume Control

        // COM interfaces for Windows Core Audio API
        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator
        {
        }

        [ComImport]
        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            int NotImpl1();
            [PreserveSig]
            int GetDefaultAudioEndpoint(DataFlow dataFlow, Role role, out IMMDevice ppDevice);
        }

        [ComImport]
        [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [PreserveSig]
            int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        }

        [ComImport]
        [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioEndpointVolume
        {
            [PreserveSig]
            int NotImpl1();
            [PreserveSig]
            int NotImpl2();
            [PreserveSig]
            int GetChannelCount(out int pnChannelCount);
            [PreserveSig]
            int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
            [PreserveSig]
            int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
            [PreserveSig]
            int GetMasterVolumeLevel(out float pfLevelDB);
            [PreserveSig]
            int GetMasterVolumeLevelScalar(out float pfLevel);
        }

        private enum DataFlow
        {
            Render,
            Capture,
            All
        }

        private enum Role
        {
            Console,
            Multimedia,
            Communications
        }

        private IAudioEndpointVolume? _volumeControl;

        private IAudioEndpointVolume? GetVolumeControl()
        {
            if (_volumeControl != null) return _volumeControl;

            try
            {
                var deviceEnumerator = new MMDeviceEnumerator() as IMMDeviceEnumerator;
                if (deviceEnumerator != null)
                {
                    var result = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia, out var device);
                    if (result == 0 && device != null)
                    {
                        var iid = typeof(IAudioEndpointVolume).GUID;
                        var activateResult = device.Activate(ref iid, 0, IntPtr.Zero, out var volumeObject);
                        if (activateResult == 0)
                        {
                            _volumeControl = volumeObject as IAudioEndpointVolume;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting volume control: {ex.Message}");
            }

            return _volumeControl;
        }

        private float GetMasterVolume()
        {
            try
            {
                var volumeControl = GetVolumeControl();
                if (volumeControl != null)
                {
                    volumeControl.GetMasterVolumeLevelScalar(out float volume);
                    return volume;
                }
                return 1.0f;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting master volume: {ex.Message}");
                return 1.0f;
            }
        }

        private void SetMasterVolume(float volume)
        {
            try
            {
                var volumeControl = GetVolumeControl();
                if (volumeControl != null)
                {
                    // Clamp volume to valid range
                    volume = Math.Max(0.0f, Math.Min(1.0f, volume));
                    
                    var eventContext = Guid.Empty;
                    volumeControl.SetMasterVolumeLevelScalar(volume, ref eventContext);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting master volume: {ex.Message}");
            }
        }

        #endregion
    }
}