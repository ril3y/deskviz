using System.Linq;
using System.Windows;
using DeskViz.Core.Services;
using ScreenInfo = DeskViz.Core.Services.ScreenInfo; // Alias if needed
using System;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DeskViz.App.Widgets;
using WpfPanel = System.Windows.Controls.Panel;
using WpfButton = System.Windows.Controls.Button;

namespace DeskViz.App.Views
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly ScreenService _screenService;
        private ObservableCollection<IWidget> _widgets = new ObservableCollection<IWidget>();
        private List<IWidget> _originalWidgets;
        private bool _widgetOrderChanged = false;
        private bool _widgetVisibilityChanged = false;
        private bool _displayChanged = false;
        private bool _orientationChanged = false;

        // Constructor accepting services
        public SettingsWindow(ScreenService screenService, SettingsService settingsService, IEnumerable<IWidget> widgets)
        {
            InitializeComponent();
            _screenService = screenService;
            _settingsService = settingsService;
            
            // Store the original widgets for undo if canceled
            _originalWidgets = new List<IWidget>(widgets);
            
            // Set up the widgets in the collection
            foreach (var widget in widgets)
            {
                _widgets.Add(widget);
            }
            
            WidgetsListView.ItemsSource = _widgets;

            LoadDisplays();
            LoadOrientationSetting();
        }

        private void LoadDisplays()
        {
            var screens = _screenService.GetAllScreens();
            DisplayComboBox.Items.Clear();
            ScreenInfo? currentScreen = null;

            foreach (var screen in screens)
            {
                DisplayComboBox.Items.Add(screen);
                if (screen.Identifier == _settingsService.Settings.PreferredDisplayIdentifier)
                {
                    currentScreen = screen;
                }
            }

            if (currentScreen != null)
            {
                DisplayComboBox.SelectedItem = currentScreen;
            }
            else if (DisplayComboBox.Items.Count > 0)
            {
                // Select primary or first if no setting saved or saved is invalid
                var primary = screens.FirstOrDefault(s => s.IsPrimary);
                DisplayComboBox.SelectedItem = primary ?? DisplayComboBox.Items[0];
            }
            
            DisplayComboBox.SelectionChanged += (s, e) => _displayChanged = true;
        }

        private void LoadOrientationSetting()
        {
            // Populate OrientationComboBox with enum values
            OrientationComboBox.ItemsSource = Enum.GetValues(typeof(WidgetOrientationSetting));
            
            // Select the currently saved orientation setting
            OrientationComboBox.SelectedItem = _settingsService.Settings.WidgetOrientation;
            
            OrientationComboBox.SelectionChanged += (s, e) => _orientationChanged = true;
        }
        
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyChanges();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyChanges();
            Close();
        }
        
        private void ApplyChanges()
        {
            // Apply display settings
            if (_displayChanged && DisplayComboBox.SelectedItem is ScreenInfo selectedScreen)
            {
                _settingsService.UpdatePreferredDisplay(selectedScreen.Identifier);
            }
            
            // Apply orientation settings
            if (_orientationChanged && OrientationComboBox.SelectedItem is WidgetOrientationSetting selectedOrientation)
            {
                _settingsService.UpdateWidgetOrientation(selectedOrientation);
            }
            
            ApplyWidgetVisibility();
            
            // Apply widget order if changed
            if (_widgetOrderChanged)
            {
                // Save widget order to settings
                _settingsService.UpdateWidgetOrder(_widgets.Select(w => w.WidgetId).ToList());
                
                // Now we need to reorder the widgets in the UI, only if we're the owner
                if (this.Owner is MainWindow mainWindow)
                {
                    mainWindow.ReorderWidgets(_widgets);
                }
            }
            
            // Reset change flags
            _displayChanged = false;
            _orientationChanged = false;
            _widgetVisibilityChanged = false;
            _widgetOrderChanged = false;
        }

        private void ApplyWidgetVisibility()
        {
            if (_widgetVisibilityChanged)
            {
                // Get the main window instance
                if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                {
                    // Create a dictionary of visibility settings
                    Dictionary<string, bool> visibilitySettings = new Dictionary<string, bool>();
                    
                    foreach (var widget in _widgets)
                    {
                        // Update the Dictionary with the widget's visibility
                        visibilitySettings[widget.WidgetId] = widget.IsWidgetVisible;
                    }
                    
                    // Save the settings
                    _settingsService.UpdateWidgetVisibility(visibilitySettings);
                    
                    // Apply the changes to the UI
                    mainWindow.ApplyWidgetVisibilitySettings();
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Restore original widget visibility
            foreach (var widget in _originalWidgets)
            {
                var originalVisibility = widget.IsWidgetVisible;
                var currentWidget = _widgets.FirstOrDefault(w => w.WidgetId == widget.WidgetId);
                if (currentWidget != null)
                {
                    currentWidget.IsWidgetVisible = originalVisibility;
                }
            }
            
            Close();
        }
        
        private void WidgetVisibility_Click(object sender, RoutedEventArgs e)
        {
            _widgetVisibilityChanged = true;
        }
        
        private void MoveWidgetUp_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as WpfButton;
            if (button?.DataContext is IWidget widget)
            {
                int index = _widgets.IndexOf(widget);
                if (index > 0)
                {
                    _widgets.Move(index, index - 1);
                    _widgetOrderChanged = true;
                }
            }
        }
        
        private void MoveWidgetDown_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as WpfButton;
            if (button?.DataContext is IWidget widget)
            {
                int index = _widgets.IndexOf(widget);
                if (index < _widgets.Count - 1)
                {
                    _widgets.Move(index, index + 1);
                    _widgetOrderChanged = true;
                }
            }
        }
        
        private void WidgetSettings_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as WpfButton;
            if (button?.DataContext is IWidget widget)
            {
                // Call the widget's settings method
                widget.OpenWidgetSettings();
            }
        }
    }
}
