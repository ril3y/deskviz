using System.Windows;

namespace DeskViz.App.Widgets.GpuWidget
{
    /// <summary>
    /// Interaction logic for GpuWidgetSettings.xaml
    /// </summary>
    public partial class GpuWidgetSettings : Window
    {
        private readonly GpuWidget _gpuWidget;

        public GpuWidgetSettings(GpuWidget gpuWidget)
        {
            _gpuWidget = gpuWidget;
            InitializeComponent();
            DataContext = _gpuWidget;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}