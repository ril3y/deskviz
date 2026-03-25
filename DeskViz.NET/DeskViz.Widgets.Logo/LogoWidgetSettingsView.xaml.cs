using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace DeskViz.Widgets.Logo
{
    public partial class LogoWidgetSettingsView : UserControl
    {
        private readonly LogoWidgetSettings _settings;
        private readonly LogoWidget _widget;

        public LogoWidgetSettingsView(LogoWidgetSettings settings, LogoWidget widget)
        {
            InitializeComponent();
            _settings = settings;
            _widget = widget;
            DataContext = _settings;

            // Initialize stretch combo box
            StretchComboBox.Items.Clear();
            StretchComboBox.Items.Add("None");
            StretchComboBox.Items.Add("Fill");
            StretchComboBox.Items.Add("Uniform");
            StretchComboBox.Items.Add("UniformToFill");
            StretchComboBox.SelectedItem = _settings.Stretch;

            // Load preview if path exists
            UpdatePreview();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|All files (*.*)|*.*",
                Title = "Select Image"
            };

            if (dialog.ShowDialog() == true)
            {
                _settings.ImagePath = dialog.FileName;
                ImagePathTextBox.Text = dialog.FileName;
                UpdatePreview();
            }
        }

        private void UpdatePreview()
        {
            try
            {
                if (!string.IsNullOrEmpty(_settings.ImagePath) && File.Exists(_settings.ImagePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_settings.ImagePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    PreviewImage.Source = bitmap;
                }
                else
                {
                    PreviewImage.Source = null;
                }
            }
            catch
            {
                PreviewImage.Source = null;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            // Update settings from UI
            _settings.Stretch = StretchComboBox.SelectedItem?.ToString() ?? "Uniform";

            // Parse width/height
            if (double.TryParse(WidthTextBox.Text, out var width))
                _settings.ImageWidth = width;
            else
                _settings.ImageWidth = null;

            if (double.TryParse(HeightTextBox.Text, out var height))
                _settings.ImageHeight = height;
            else
                _settings.ImageHeight = null;

            _widget.ApplySettings(_settings);

            // Close the settings panel (find parent window or panel)
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.DialogResult = true;
                parentWindow.Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.DialogResult = false;
                parentWindow.Close();
            }
        }
    }
}
