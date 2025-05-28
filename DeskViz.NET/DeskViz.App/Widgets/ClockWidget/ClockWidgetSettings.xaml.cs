using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace DeskViz.App.Widgets
{
    /// <summary>
    /// Interaction logic for ClockWidgetSettings.xaml
    /// </summary>
    public partial class ClockWidgetSettings : System.Windows.Controls.UserControl
    {
        public ClockWidgetSettings()
        {
            Debug.WriteLine("Initializing ClockWidgetSettings");
            InitializeComponent();
            Debug.WriteLine("ClockWidgetSettings initialized");
            // DataContext is set externally by the caller (ClockWidget.OpenWidgetSettings)
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("OkButton_Click triggered");
            try
            {
                Window parentWindow = Window.GetWindow(this);
                if (parentWindow != null)
                {
                    Debug.WriteLine("Setting DialogResult to true");
                    parentWindow.DialogResult = true;
                    Debug.WriteLine("Closing parent window");
                    parentWindow.Close();
                }
                else
                {
                    Debug.WriteLine("ERROR: Could not find parent window for ClockWidgetSettings");
                    System.Windows.MessageBox.Show("Error: Cannot find parent window.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EXCEPTION in OkButton_Click: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("CancelButton_Click triggered");
            try
            {
                Window parentWindow = Window.GetWindow(this);
                if (parentWindow != null)
                {
                    Debug.WriteLine("Setting DialogResult to false");
                    parentWindow.DialogResult = false;
                    Debug.WriteLine("Closing parent window");
                    parentWindow.Close();
                }
                else
                {
                    Debug.WriteLine("ERROR: Could not find parent window for ClockWidgetSettings");
                    System.Windows.MessageBox.Show("Error: Cannot find parent window.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EXCEPTION in CancelButton_Click: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
