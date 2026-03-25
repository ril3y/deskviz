using System.Windows.Controls;
using DeskViz.Plugins.Base;

namespace DeskViz.Widgets.Ram
{
    public partial class RamWidgetSettingsView : UserControl
    {
        public RamWidgetSettingsView(RamWidgetSettings settings)
        {
            InitializeComponent();
            DataContext = new RamWidgetSettingsViewModel(settings);
        }
    }

    public class RamWidgetSettingsViewModel : BaseWidgetSettingsViewModel<RamWidgetSettings>
    {
        public bool HasValidationErrors => ValidationErrors.Length > 0;

        public RamWidgetSettingsViewModel(RamWidgetSettings settings)
        {
            LoadSettings(settings);
            Settings.PropertyChanged += (s, e) => OnPropertyChanged(nameof(HasValidationErrors));
        }
    }
}