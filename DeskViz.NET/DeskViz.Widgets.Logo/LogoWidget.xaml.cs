using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DeskViz.Plugins.Base;
using DeskViz.Plugins.Interfaces;

namespace DeskViz.Widgets.Logo
{
    public partial class LogoWidget : BaseWidget
    {
        private LogoWidgetSettings _settings = new();
        private BitmapImage? _imageSource;

        public override IWidgetMetadata Metadata { get; } = new WidgetMetadata
        {
            Id = "LogoWidget",
            Name = "Logo/Image",
            Description = "Displays a custom image from your filesystem",
            Author = "DeskViz Team",
            Version = new Version(2, 0, 0),
            Category = "Display",
            Tags = new[] { "logo", "image", "picture", "display" },
            RequiresElevatedPermissions = false,
            MinimumHostVersion = new Version(1, 0, 0)
        };

        public override string WidgetId => "LogoWidget";
        public override string DisplayName => "Logo/Image";

        public BitmapImage? ImageSource
        {
            get => _imageSource;
            private set
            {
                if (_imageSource != value)
                {
                    _imageSource = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ImageVisibility));
                    OnPropertyChanged(nameof(NoImageVisibility));
                }
            }
        }

        public Visibility ImageVisibility => ImageSource != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility NoImageVisibility => ImageSource == null ? Visibility.Visible : Visibility.Collapsed;

        public double? ImageWidth => _settings.ImageWidth;
        public double? ImageHeight => _settings.ImageHeight;

        public Stretch ImageStretch
        {
            get
            {
                if (Enum.TryParse<Stretch>(_settings.Stretch, out var stretch))
                    return stretch;
                return Stretch.Uniform;
            }
        }

        public HorizontalAlignment ImageHorizontalAlignment
        {
            get
            {
                if (Enum.TryParse<HorizontalAlignment>(_settings.HorizontalAlignment, out var align))
                    return align;
                return HorizontalAlignment.Center;
            }
        }

        public VerticalAlignment ImageVerticalAlignment
        {
            get
            {
                if (Enum.TryParse<VerticalAlignment>(_settings.VerticalAlignment, out var align))
                    return align;
                return VerticalAlignment.Center;
            }
        }

        public LogoWidget()
        {
            try
            {
                InitializeComponent();
                DataContext = this;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logo Widget XAML initialization failed: {ex.Message}");
            }
        }

        protected override void InitializeWidget()
        {
            LoadSettings();
            StartUpdateTimer(_settings.UpdateIntervalSeconds);
        }

        protected override void ShutdownWidget()
        {
            StopUpdateTimer();
        }

        public override void RefreshData()
        {
            // Image doesn't need periodic refresh, but we could reload if file changed
        }

        public override FrameworkElement? CreateSettingsUI()
        {
            var settingsClone = _settings.Clone() as LogoWidgetSettings ?? new();
            var settingsView = new LogoWidgetSettingsView(settingsClone, this);
            return settingsView;
        }

        private void LoadSettings()
        {
            var loadedSettings = LoadPageSettings<LogoWidgetSettings>();
            if (loadedSettings != null)
            {
                _settings = loadedSettings;
            }
            LoadImage();
            NotifyAllPropertiesChanged();
        }

        public void ApplySettings(LogoWidgetSettings newSettings)
        {
            _settings = newSettings;
            SavePageSettings(_settings);
            LoadImage();
            NotifyAllPropertiesChanged();
        }

        private void LoadImage()
        {
            if (string.IsNullOrEmpty(_settings.ImagePath))
            {
                ImageSource = null;
                return;
            }

            try
            {
                if (!File.Exists(_settings.ImagePath))
                {
                    Log($"Image file not found: {_settings.ImagePath}", LogLevel.Warning);
                    ImageSource = null;
                    return;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_settings.ImagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                ImageSource = bitmap;
                Log($"Image loaded: {_settings.ImagePath}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Log($"Error loading image: {ex.Message}", LogLevel.Error);
                ImageSource = null;
            }
        }

        private void NotifyAllPropertiesChanged()
        {
            OnPropertyChanged(nameof(ImageSource));
            OnPropertyChanged(nameof(ImageWidth));
            OnPropertyChanged(nameof(ImageHeight));
            OnPropertyChanged(nameof(ImageStretch));
            OnPropertyChanged(nameof(ImageHorizontalAlignment));
            OnPropertyChanged(nameof(ImageVerticalAlignment));
            OnPropertyChanged(nameof(ImageVisibility));
            OnPropertyChanged(nameof(NoImageVisibility));
        }

        protected override void OnPageSettingsChanged(string pageId)
        {
            LoadSettings();
        }
    }
}
