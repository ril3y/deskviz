using System.Windows;

namespace DeskViz.App.Widgets.MediaControlWidget
{
    /// <summary>
    /// Interaction logic for MediaControlWidgetSettings.xaml
    /// </summary>
    public partial class MediaControlWidgetSettings : Window
    {
        private readonly MediaControlWidget _mediaControlWidget;

        public MediaControlWidgetSettings(MediaControlWidget mediaControlWidget)
        {
            _mediaControlWidget = mediaControlWidget;
            InitializeComponent();
            DataContext = _mediaControlWidget;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}