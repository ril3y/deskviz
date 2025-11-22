using System.Collections.Generic;
using System.Windows;
using DeskViz.Core.Services;

namespace DeskViz.App.Views
{
    /// <summary>
    /// Dialog for selecting which display to show DeskViz on (shown on first run)
    /// </summary>
    public partial class DisplaySelectionDialog : Window
    {
        private readonly List<ScreenInfo> _screens;

        /// <summary>
        /// Gets the selected screen
        /// </summary>
        public ScreenInfo? SelectedScreen => DisplayComboBox.SelectedItem as ScreenInfo;

        /// <summary>
        /// Initializes a new instance of DisplaySelectionDialog
        /// </summary>
        public DisplaySelectionDialog(List<ScreenInfo> screens)
        {
            InitializeComponent();

            _screens = screens;
            LoadDisplays();

            DisplayComboBox.SelectionChanged += DisplayComboBox_SelectionChanged;
        }

        private void LoadDisplays()
        {
            DisplayComboBox.Items.Clear();

            foreach (var screen in _screens)
            {
                DisplayComboBox.Items.Add(screen);
            }

            // Select primary screen by default, or first screen if no primary
            ScreenInfo? defaultScreen = null;
            foreach (var screen in _screens)
            {
                if (screen.IsPrimary)
                {
                    defaultScreen = screen;
                    break;
                }
            }

            if (defaultScreen != null)
            {
                DisplayComboBox.SelectedItem = defaultScreen;
            }
            else if (DisplayComboBox.Items.Count > 0)
            {
                DisplayComboBox.SelectedIndex = 0;
            }

            UpdateDisplayInfo();
        }

        private void DisplayComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateDisplayInfo();
        }

        private void UpdateDisplayInfo()
        {
            if (DisplayComboBox.SelectedItem is ScreenInfo screen)
            {
                DisplayResolutionText.Text = $"Resolution: {screen.Bounds.Width} x {screen.Bounds.Height}";
                DisplayPositionText.Text = $"Position: ({screen.Bounds.X}, {screen.Bounds.Y})";
                DisplayPrimaryText.Text = screen.IsPrimary ? "This is your primary display" : "";
            }
            else
            {
                DisplayResolutionText.Text = "Resolution: -";
                DisplayPositionText.Text = "Position: -";
                DisplayPrimaryText.Text = "";
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
