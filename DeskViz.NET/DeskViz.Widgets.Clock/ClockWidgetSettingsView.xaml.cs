using System.Runtime.CompilerServices;
using System.Windows.Controls;
using DeskViz.Plugins.Base;

namespace DeskViz.Widgets.Clock
{
    public partial class ClockWidgetSettingsView : UserControl
    {
        public ClockWidgetSettingsView(ClockWidgetSettings settings)
        {
            InitializeComponent();
            DataContext = new ClockWidgetSettingsViewModel(settings);
        }
    }

    public class ClockWidgetSettingsViewModel : BaseWidgetSettingsViewModel<ClockWidgetSettings>
    {
        public bool HasValidationErrors => ValidationErrors.Length > 0;

        public ClockWidgetSettingsViewModel(ClockWidgetSettings settings)
        {
            LoadSettings(settings);
            Settings.PropertyChanged += (s, e) => OnPropertyChanged(nameof(HasValidationErrors));
        }

        // Expose settings properties for binding
        public bool Is24HourFormat
        {
            get => Settings.Is24HourFormat;
            set
            {
                Settings.Is24HourFormat = value;
                OnPropertyChanged();
            }
        }

        public double ClockFontSize
        {
            get => Settings.ClockFontSize;
            set
            {
                Settings.ClockFontSize = value;
                OnPropertyChanged();
            }
        }

        public double UpdateIntervalSeconds
        {
            get => Settings.UpdateIntervalSeconds;
            set
            {
                Settings.UpdateIntervalSeconds = value;
                OnPropertyChanged();
            }
        }
    }
}