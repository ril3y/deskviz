using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DeskViz.Core.Services;

namespace DeskViz.App.Widgets.MediaControlWidget
{
    /// <summary>
    /// Interaction logic for MediaControlWidget.xaml
    /// </summary>
    public partial class MediaControlWidget : System.Windows.Controls.UserControl, IWidget, INotifyPropertyChanged
    {
        private readonly IMediaControlService _mediaControlService;
        private readonly SettingsService _settingsService;
        private readonly DispatcherTimer _updateTimer;

        // IWidget implementation
        public string WidgetId => "MediaControlWidget";
        public string DisplayName => "Media Control";

        private bool _isWidgetVisible = true;
        public bool IsWidgetVisible
        {
            get => _isWidgetVisible;
            set
            {
                if (_isWidgetVisible != value)
                {
                    _isWidgetVisible = value;
                    OnPropertyChanged();
                    Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        public event EventHandler? ConfigButtonClicked;

        // Media properties
        private string _title = "No Media Playing";
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        private string _artist = "";
        public string Artist
        {
            get => _artist;
            set { _artist = value; OnPropertyChanged(); }
        }

        private string _album = "";
        public string Album
        {
            get => _album;
            set { _album = value; OnPropertyChanged(); }
        }

        private string _appName = "";
        public string AppName
        {
            get => _appName;
            set { _appName = value; OnPropertyChanged(); }
        }

        private BitmapImage? _albumArtSource;
        public BitmapImage? AlbumArtSource
        {
            get => _albumArtSource;
            set { _albumArtSource = value; OnPropertyChanged(); }
        }

        private string _playPauseIcon = "▶";
        public string PlayPauseIcon
        {
            get => _playPauseIcon;
            set { _playPauseIcon = value; OnPropertyChanged(); }
        }

        private double _progressPercent;
        public double ProgressPercent
        {
            get => _progressPercent;
            set { _progressPercent = value; OnPropertyChanged(); }
        }

        private string _positionText = "0:00";
        public string PositionText
        {
            get => _positionText;
            set { _positionText = value; OnPropertyChanged(); }
        }

        private string _durationText = "0:00";
        public string DurationText
        {
            get => _durationText;
            set { _durationText = value; OnPropertyChanged(); }
        }

        private double _volume = 100;
        public double Volume
        {
            get => _volume;
            set 
            { 
                if (Math.Abs(_volume - value) > 0.1)
                {
                    _volume = value; 
                    OnPropertyChanged();
                    UpdateVolumeProgressBar();
                }
            }
        }

        // Control state properties
        private bool _canPlay = false;
        public bool CanPlay
        {
            get => _canPlay;
            set { _canPlay = value; OnPropertyChanged(); }
        }

        private bool _canPause = false;
        public bool CanPause
        {
            get => _canPause;
            set { _canPause = value; OnPropertyChanged(); }
        }

        private bool _canStop = false;
        public bool CanStop
        {
            get => _canStop;
            set { _canStop = value; OnPropertyChanged(); }
        }

        private bool _canSkipNext = false;
        public bool CanSkipNext
        {
            get => _canSkipNext;
            set { _canSkipNext = value; OnPropertyChanged(); }
        }

        private bool _canSkipPrevious = false;
        public bool CanSkipPrevious
        {
            get => _canSkipPrevious;
            set { _canSkipPrevious = value; OnPropertyChanged(); }
        }

        private PlaybackState _currentState = PlaybackState.Unknown;

        // Visibility settings
        private bool _showTitle = true;
        public bool ShowTitle
        {
            get => _showTitle;
            set 
            { 
                _showTitle = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(TitleVisibility));
                OnPropertyChanged(nameof(HeaderVisibility));
                SaveSettings();
            }
        }

        private bool _showSubtitle = true;
        public bool ShowSubtitle
        {
            get => _showSubtitle;
            set 
            { 
                _showSubtitle = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(SubtitleVisibility));
                OnPropertyChanged(nameof(HeaderVisibility));
                SaveSettings();
            }
        }

        public Visibility TitleVisibility => ShowTitle ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SubtitleVisibility => ShowSubtitle ? Visibility.Visible : Visibility.Collapsed;
        public Visibility HeaderVisibility => (ShowTitle || ShowSubtitle) ? Visibility.Visible : Visibility.Collapsed;

        public MediaControlWidget(IMediaControlService mediaControlService, SettingsService settingsService)
        {
            _mediaControlService = mediaControlService ?? throw new ArgumentNullException(nameof(mediaControlService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            InitializeComponent();
            DataContext = this;

            // Load settings
            LoadSettings();
            
            // Ensure volume progress bar is updated when control is loaded
            this.Loaded += (s, e) => UpdateVolumeProgressBar();

            // Subscribe to media events
            _mediaControlService.MediaSessionChanged += OnMediaSessionChanged;
            _mediaControlService.PlaybackStateChanged += OnPlaybackStateChanged;

            // Initialize the service
            _ = InitializeMediaServiceAsync();

            // Set up update timer
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private async System.Threading.Tasks.Task InitializeMediaServiceAsync()
        {
            try
            {
                var success = await _mediaControlService.InitializeAsync();
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("Media control service initialized successfully.");
                    
                    // Initialize volume with current system volume
                    Volume = _mediaControlService.GetVolume();
                    
                    // Initial data load
                    RefreshData();
                    
                    // Update volume progress bar after layout is loaded
                    Dispatcher.BeginInvoke(new Action(() => UpdateVolumeProgressBar()), 
                        System.Windows.Threading.DispatcherPriority.Loaded);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to initialize media control service.");
                    Title = "Media Control Unavailable";
                    AppName = "Initialization Failed";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing media service: {ex.Message}");
                Title = "Media Control Error";
                AppName = "Service Error";
            }
        }

        private void OnMediaSessionChanged(object? sender, MediaSessionChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateFromSession(e.CurrentSession);
            });
        }

        private void OnPlaybackStateChanged(object? sender, PlaybackStateChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _currentState = e.State;
                UpdatePlayPauseIcon();
                UpdateFromSession(e.Session);
            });
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            RefreshData();
        }

        public void RefreshData()
        {
            try
            {
                var session = _mediaControlService.GetCurrentSession();
                UpdateFromSession(session);
                
                // Update volume from system
                Volume = _mediaControlService.GetVolume();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing media data: {ex.Message}");
            }
        }

        private void UpdateFromSession(MediaSessionInfo? session)
        {
            if (session == null)
            {
                Title = "No Media Playing";
                Artist = "";
                Album = "";
                AppName = "";
                AlbumArtSource = null;
                CanPlay = false;
                CanPause = false;
                CanStop = false;
                CanSkipNext = false;
                CanSkipPrevious = false;
                ProgressPercent = 0;
                PositionText = "0:00";
                DurationText = "0:00";
                return;
            }

            Title = session.Title;
            Artist = session.Artist;
            Album = session.Album;
            AppName = session.AppName;
            CanPlay = session.CanPlay;
            CanPause = session.CanPause;
            CanStop = session.CanStop;
            CanSkipNext = session.CanSkipNext;
            CanSkipPrevious = session.CanSkipPrevious;
            _currentState = session.State;

            // Update progress
            if (session.Duration.TotalSeconds > 0)
            {
                ProgressPercent = (session.Position.TotalSeconds / session.Duration.TotalSeconds) * 100;
            }
            else
            {
                ProgressPercent = 0;
            }

            PositionText = FormatTime(session.Position);
            DurationText = FormatTime(session.Duration);

            // Update album art
            UpdateAlbumArt(session.AlbumArt);

            UpdatePlayPauseIcon();
        }

        private void UpdatePlayPauseIcon()
        {
            PlayPauseIcon = _currentState == PlaybackState.Playing ? "⏸" : "▶";
        }

        private void UpdateAlbumArt(byte[]? albumArtData)
        {
            try
            {
                if (albumArtData != null && albumArtData.Length > 0)
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(albumArtData);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    AlbumArtSource = bitmap;
                }
                else
                {
                    AlbumArtSource = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading album art: {ex.Message}");
                AlbumArtSource = null;
            }
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
            {
                return time.ToString(@"h\:mm\:ss");
            }
            else
            {
                return time.ToString(@"m\:ss");
            }
        }

        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentState == PlaybackState.Playing)
                {
                    await _mediaControlService.PauseAsync();
                }
                else
                {
                    await _mediaControlService.PlayAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in play/pause: {ex.Message}");
            }
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _mediaControlService.StopAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in stop: {ex.Message}");
            }
        }

        private async void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _mediaControlService.PreviousAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in previous: {ex.Message}");
            }
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _mediaControlService.NextAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in next: {ex.Message}");
            }
        }

        private bool _isDraggingVolume = false;

        private void VolumeTrack_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _isDraggingVolume = true;
                var border = sender as Border;
                border?.CaptureMouse();
                
                UpdateVolumeFromMouse(sender, e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in volume mouse down: {ex.Message}");
            }
        }

        private void VolumeTrack_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                if (_isDraggingVolume && e.LeftButton == MouseButtonState.Pressed)
                {
                    UpdateVolumeFromMouse(sender, e);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in volume mouse move: {ex.Message}");
            }
        }

        private void VolumeTrack_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _isDraggingVolume = false;
                var border = sender as Border;
                border?.ReleaseMouseCapture();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in volume mouse up: {ex.Message}");
            }
        }

        private async void UpdateVolumeFromMouse(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                if (sender is Border border)
                {
                    var position = e.GetPosition(border);
                    var percentage = Math.Max(0, Math.Min(100, (position.X / border.ActualWidth) * 100));
                    
                    Volume = percentage;
                    UpdateVolumeProgressBar();
                    await _mediaControlService.SetVolumeAsync(Volume / 100.0);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating volume from mouse: {ex.Message}");
            }
        }

        private void UpdateVolumeProgressBar()
        {
            try
            {
                if (VolumeProgress != null && VolumeTrack != null)
                {
                    var progressWidth = (Volume / 100.0) * VolumeTrack.ActualWidth;
                    VolumeProgress.Width = Math.Max(0, progressWidth);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating volume progress: {ex.Message}");
            }
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                // Only update if user is dragging (not from data binding updates)
                if (sender is Slider slider && slider.IsMouseCaptured)
                {
                    ProgressPercent = e.NewValue;
                    // TODO: Implement seek functionality when media service supports it
                    System.Diagnostics.Debug.WriteLine($"Seek to: {e.NewValue}%");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error seeking: {ex.Message}");
            }
        }

        public void OpenWidgetSettings()
        {
            var settingsWindow = new MediaControlWidgetSettings(this)
            {
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            settingsWindow.ShowDialog();
        }

        private void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigButtonClicked?.Invoke(this, EventArgs.Empty);
        }

        private void SaveSettings()
        {
            if (_isLoadingSettings) return;
            
            try
            {
                _settingsService.Settings.MediaControlShowTitle = ShowTitle;
                _settingsService.Settings.MediaControlShowSubtitle = ShowSubtitle;
                _settingsService.SaveSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving media control settings: {ex.Message}");
            }
        }

        private bool _isLoadingSettings = false;

        private void LoadSettings()
        {
            try
            {
                _isLoadingSettings = true;
                _showTitle = _settingsService.Settings.MediaControlShowTitle;
                _showSubtitle = _settingsService.Settings.MediaControlShowSubtitle;
                
                // Notify of property changes
                OnPropertyChanged(nameof(ShowTitle));
                OnPropertyChanged(nameof(ShowSubtitle));
                OnPropertyChanged(nameof(TitleVisibility));
                OnPropertyChanged(nameof(SubtitleVisibility));
                OnPropertyChanged(nameof(HeaderVisibility));
                
                _isLoadingSettings = false;
            }
            catch (Exception ex)
            {
                _isLoadingSettings = false;
                System.Diagnostics.Debug.WriteLine($"Error loading media control settings: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}