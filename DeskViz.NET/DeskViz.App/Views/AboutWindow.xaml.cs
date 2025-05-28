using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace DeskViz.App.Views
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            
            // Set version from assembly
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                VersionText.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GitHubLink_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Open the GitHub repository in the default browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/ril3y/DeskViz",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to open link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SystemInfoButton_Click(object sender, RoutedEventArgs e)
        {
            var systemInfo = 
                $"Operating System: {Environment.OSVersion}\n" +
                $"Framework: {Environment.Version}\n" +
                $"Processor Count: {Environment.ProcessorCount}\n" +
                $"Machine Name: {Environment.MachineName}\n" +
                $"User: {Environment.UserName}\n" +
                $"System Directory: {Environment.SystemDirectory}\n" +
                $"Working Set: {Environment.WorkingSet / 1024 / 1024} MB";

            System.Windows.MessageBox.Show(systemInfo, "System Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}