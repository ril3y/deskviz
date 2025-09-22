using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace DeskViz.App.Widgets
{
    /// <summary>
    /// Interaction logic for HardDriveWidgetSettings.xaml
    /// </summary>
    public partial class HardDriveWidgetSettings : Window
    {
        private readonly HardDriveWidget _widget;
        private ObservableCollection<DriveSelectionItem> _driveSelectionItems = new ObservableCollection<DriveSelectionItem>();

        public HardDriveWidgetSettings(HardDriveWidget widget)
        {
            InitializeComponent();
            _widget = widget ?? throw new ArgumentNullException(nameof(widget));

            // Set the widget as data context to bind to its properties
            DataContext = _widget;

            // Initialize drive selection
            InitializeDriveSelection();
        }

        private void InitializeDriveSelection()
        {
            _driveSelectionItems.Clear();

            // Get all available drives
            var availableDrives = _widget.GetAvailableDrives();
            var selectedDrives = _widget.SelectedDrives;

            foreach (var (name, label) in availableDrives)
            {
                var displayText = string.IsNullOrWhiteSpace(label) ? name : $"{name} ({label})";
                _driveSelectionItems.Add(new DriveSelectionItem
                {
                    DriveName = name,
                    DisplayText = displayText,
                    IsSelected = selectedDrives.Contains(name)
                });
            }

            DriveSelectionList.ItemsSource = _driveSelectionItems;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate and apply settings
                if (double.TryParse(UpdateIntervalTextBox.Text, out double interval) && interval > 0)
                {
                    _widget.UpdateIntervalSeconds = interval;
                }
                else
                {
                    System.Windows.MessageBox.Show("Please enter a valid update interval (positive number).", "Invalid Input",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _widget.ShowTemperature = ShowTemperatureCheckBox.IsChecked ?? false;
                _widget.ShowLabel = ShowLabelCheckBox.IsChecked ?? false;

                // Apply drive selection
                ApplyDriveSelection();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error applying settings: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void DriveSelection_Changed(object sender, RoutedEventArgs e)
        {
            // Event handler for when drive selection changes
            // The actual change is handled by the data binding
        }

        private void ApplyDriveSelection()
        {
            // Get selected drives
            var selectedDrives = _driveSelectionItems
                .Where(item => item.IsSelected)
                .Select(item => item.DriveName)
                .ToArray();

            // Update the widget's selected drives
            _widget.SelectedDrivesString = string.Join(",", selectedDrives);
        }
    }

    /// <summary>
    /// Represents a drive selection item for the settings dialog
    /// </summary>
    public class DriveSelectionItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string DriveName { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}