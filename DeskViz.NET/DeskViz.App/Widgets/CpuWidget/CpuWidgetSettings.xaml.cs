using System.Windows;

namespace DeskViz.App.Widgets
{
    /// <summary>
    /// Interaction logic for CpuWidgetSettings.xaml
    /// </summary>
    public partial class CpuWidgetSettings : Window
    {
        private readonly CpuWidget _cpuWidget;

        /// <summary>
        /// Initializes a new instance of the CpuWidgetSettings class
        /// </summary>
        public CpuWidgetSettings(CpuWidget cpuWidget)
        {
            InitializeComponent();
            _cpuWidget = cpuWidget;
            
            // Initialize controls with current settings
            UpdateIntervalSlider.Value = _cpuWidget.UpdateIntervalSeconds;
            ShowCoresCheckBox.IsChecked = _cpuWidget.ShowCores;
            
            // Initialize temperature controls
            ShowTemperatureCheckBox.IsChecked = _cpuWidget.ShowTemperature;
            CelsiusRadioButton.IsChecked = !_cpuWidget.UseFahrenheit;
            FahrenheitRadioButton.IsChecked = _cpuWidget.UseFahrenheit;
            TempFontSizeSlider.Value = _cpuWidget.TemperatureFontSize;
            
            // Initialize metrics controls
            ShowClockSpeedCheckBox.IsChecked = _cpuWidget.ShowClockSpeed;
            ShowPowerUsageCheckBox.IsChecked = _cpuWidget.ShowPowerUsage;
        }

        /// <summary>
        /// Handles the Click event of the OK button
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Apply settings to the widget
            _cpuWidget.UpdateIntervalSeconds = UpdateIntervalSlider.Value;
            _cpuWidget.ShowCores = ShowCoresCheckBox.IsChecked ?? true;
            
            // Apply temperature settings
            _cpuWidget.ShowTemperature = ShowTemperatureCheckBox.IsChecked ?? true;
            _cpuWidget.UseFahrenheit = FahrenheitRadioButton.IsChecked ?? false;
            _cpuWidget.TemperatureFontSize = TempFontSizeSlider.Value;
            
            // Apply metrics settings
            _cpuWidget.ShowClockSpeed = ShowClockSpeedCheckBox.IsChecked ?? true;
            _cpuWidget.ShowPowerUsage = ShowPowerUsageCheckBox.IsChecked ?? false;
            
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
