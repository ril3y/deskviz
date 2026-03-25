using System.Windows.Controls;
using DeskViz.Plugins.Base;

namespace DeskViz.Widgets.Cpu
{
    public partial class CpuWidgetSettingsView : UserControl
    {
        public CpuWidgetSettingsView(CpuWidgetSettings settings)
        {
            InitializeComponent();
            DataContext = new CpuWidgetSettingsViewModel(settings);
        }
    }

    public class CpuWidgetSettingsViewModel : BaseWidgetSettingsViewModel<CpuWidgetSettings>
    {
        public bool HasValidationErrors => ValidationErrors.Length > 0;

        public CpuWidgetSettingsViewModel(CpuWidgetSettings settings)
        {
            LoadSettings(settings);
            Settings.PropertyChanged += (s, e) => OnPropertyChanged(nameof(HasValidationErrors));
        }
    }
}