using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace DeskViz.App.Widgets
{
    /// <summary>
    /// Interaction logic for LogoWidgetSettings.xaml
    /// </summary>
    public partial class LogoWidgetSettings : System.Windows.Controls.UserControl
    {
        public LogoWidgetSettings()
        {
            InitializeComponent();
        }

        private LogoWidgetSettingsViewModel? ViewModel => DataContext as LogoWidgetSettingsViewModel;

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Logo Image",
                Filter = "Image Files(*.PNG;*.JPG;*.JPEG;*.BMP;*.GIF)|*.PNG;*.JPG;*.JPEG;*.BMP;*.GIF|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) // Start in Pictures
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ViewModel.ImagePath = openFileDialog.FileName;
                Debug.WriteLine($"Logo image selected: {ViewModel.ImagePath}");
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("LogoWidgetSettings OkButton_Click triggered");
            try
            {
                Window parentWindow = Window.GetWindow(this);
                if (parentWindow != null)
                {
                    parentWindow.DialogResult = true;
                    parentWindow.Close();
                }
                else
                {
                    Debug.WriteLine("ERROR: Could not find parent window for LogoWidgetSettings");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EXCEPTION in LogoWidgetSettings OkButton_Click: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("LogoWidgetSettings CancelButton_Click triggered");
            try
            {
                Window parentWindow = Window.GetWindow(this);
                if (parentWindow != null)
                {
                    parentWindow.DialogResult = false;
                    parentWindow.Close();
                }
                else
                {
                    Debug.WriteLine("ERROR: Could not find parent window for LogoWidgetSettings");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EXCEPTION in LogoWidgetSettings CancelButton_Click: {ex.Message}");
            }
        }
    }
}
