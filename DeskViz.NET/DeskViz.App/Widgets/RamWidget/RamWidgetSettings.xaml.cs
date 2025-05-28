using System.Windows;
using DeskViz.Core.Services;

namespace DeskViz.App.Widgets
{
    /// <summary>
    /// Interaction logic for RamWidgetSettings.xaml
    /// </summary>
    public partial class RamWidgetSettings : Window
    {
        private readonly RamWidget _ramWidget;
        private readonly SettingsService _settingsService;

        /// <summary>
        /// Initializes a new instance of the RamWidgetSettings class
        /// </summary>
        public RamWidgetSettings(RamWidget ramWidget, SettingsService settingsService)
        {
            InitializeComponent();
            _ramWidget = ramWidget;
            _settingsService = settingsService;
            
            // Initialize controls with current settings from the widget instance
            UpdateIntervalSlider.Value = _ramWidget.UpdateIntervalSeconds;
            ShowPageFileInfoCheckBox.IsChecked = _ramWidget.ShowPageFileInfo;
        }

        /// <summary>
        /// Handles the Click event of the OK button
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Apply settings to the widget instance
            _ramWidget.UpdateIntervalSeconds = UpdateIntervalSlider.Value;
            _ramWidget.ShowPageFileInfo = ShowPageFileInfoCheckBox.IsChecked ?? true;

            // Update and save settings via SettingsService
            var settings = _settingsService.Settings; 
            settings.RamUpdateIntervalSeconds = _ramWidget.UpdateIntervalSeconds;
            settings.RamShowPageFileInfo = _ramWidget.ShowPageFileInfo;
            _settingsService.SaveSettings(); 
            
            // Close the dialog
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Handles the Click event of the Cancel button
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Close the dialog without saving
            DialogResult = false;
            Close();
        }
    }
}
