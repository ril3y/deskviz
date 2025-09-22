using System.Linq;
using System.Windows;
using DeskViz.Core.Services;
using DeskViz.Core.Models;
using ScreenInfo = DeskViz.Core.Services.ScreenInfo; // Alias if needed
using System;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DeskViz.App.Widgets;
using WpfPanel = System.Windows.Controls.Panel;
using WpfButton = System.Windows.Controls.Button;
using System.Windows.Media;
using System.Windows.Threading;

namespace DeskViz.App.Views
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly ScreenService _screenService;
        private List<IWidget> _originalWidgets;
        private bool _displayChanged = false;
        private bool _orientationChanged = false;
        private bool _autoRotationChanged = false;
        private bool _pagesChanged = false;
        private int _selectedPageIndex = -1;

        public event EventHandler? WidgetConfigurationChanged;

        // Constructor accepting services
        public SettingsWindow(ScreenService screenService, SettingsService settingsService, IEnumerable<IWidget> widgets)
        {
            InitializeComponent();
            _screenService = screenService;
            _settingsService = settingsService;
            
            // Store the original widgets for undo if canceled
            _originalWidgets = new List<IWidget>(widgets);

            LoadDisplays();
            LoadOrientationSetting();
            LoadAutoRotationSettings();
            LoadPages();
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

        private void LoadAutoRotationSettings()
        {
            var settings = _settingsService.Settings;

            // Load auto-rotation settings
            AutoRotationEnabledCheckBox.IsChecked = settings.AutoRotationEnabled;
            RotationIntervalTextBox.Text = settings.AutoRotationIntervalSeconds.ToString();
            PauseOnInteractionCheckBox.IsChecked = settings.PauseOnUserInteraction;

            // Set rotation mode
            foreach (ComboBoxItem item in RotationModeComboBox.Items)
            {
                if (item.Tag?.ToString() == settings.RotationMode.ToString())
                {
                    RotationModeComboBox.SelectedItem = item;
                    break;
                }
            }
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
            bool pageDisplayNeedsRefresh = false;

            // Apply display settings
            if (_displayChanged && DisplayComboBox.SelectedItem is ScreenInfo selectedScreen)
            {
                _settingsService.UpdatePreferredDisplay(selectedScreen.Identifier);
                pageDisplayNeedsRefresh = true;
            }

            // Apply orientation settings
            if (_orientationChanged && OrientationComboBox.SelectedItem is WidgetOrientationSetting selectedOrientation)
            {
                _settingsService.UpdateWidgetOrientation(selectedOrientation);
                pageDisplayNeedsRefresh = true;
            }

            // Apply auto-rotation settings
            if (_autoRotationChanged)
            {
                ApplyAutoRotationSettings();
            }

            // Check if pages changed (this affects widget visibility)
            if (_pagesChanged)
            {
                pageDisplayNeedsRefresh = true;
            }

            // Reset change flags
            _displayChanged = false;
            _orientationChanged = false;
            _autoRotationChanged = false;
            _pagesChanged = false;

            // Notify that widget configuration has changed
            if (pageDisplayNeedsRefresh)
            {
                WidgetConfigurationChanged?.Invoke(this, EventArgs.Empty);
            }
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        private void ApplyAutoRotationSettings()
        {
            try
            {
                bool enabled = AutoRotationEnabledCheckBox.IsChecked ?? false;
                int intervalSeconds = 10; // Default value

                if (int.TryParse(RotationIntervalTextBox.Text, out int parsedInterval) && parsedInterval > 0)
                {
                    intervalSeconds = parsedInterval;
                }

                bool pauseOnInteraction = PauseOnInteractionCheckBox.IsChecked ?? true;

                AutoRotationMode mode = AutoRotationMode.Forward; // Default
                if (RotationModeComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    if (Enum.TryParse<AutoRotationMode>(selectedItem.Tag?.ToString(), out AutoRotationMode parsedMode))
                    {
                        mode = parsedMode;
                    }
                }

                _settingsService.UpdateAutoRotationSettings(enabled, intervalSeconds, mode, pauseOnInteraction);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error applying auto-rotation settings: {ex.Message}",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Auto-rotation event handlers
        private void AutoRotationEnabled_Changed(object sender, RoutedEventArgs e)
        {
            _autoRotationChanged = true;
        }

        private void RotationInterval_Changed(object sender, TextChangedEventArgs e)
        {
            _autoRotationChanged = true;
        }

        private void RotationMode_Changed(object sender, SelectionChangedEventArgs e)
        {
            _autoRotationChanged = true;
        }

        private void PauseOnInteraction_Changed(object sender, RoutedEventArgs e)
        {
            _autoRotationChanged = true;
        }

        // Page management methods
        private void LoadPages()
        {
            PagesListBox.Items.Clear();
            for (int i = 0; i < _settingsService.Settings.Pages.Count; i++)
            {
                var page = _settingsService.Settings.Pages[i];
                PagesListBox.Items.Add($"{i + 1}. {page.Name}");
            }

            if (PagesListBox.Items.Count > 0)
            {
                PagesListBox.SelectedIndex = _settingsService.Settings.CurrentPageIndex;
            }
        }

        private void PagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PagesListBox.SelectedIndex >= 0)
            {
                _selectedPageIndex = PagesListBox.SelectedIndex;
                LoadPageConfiguration(_selectedPageIndex);
            }
        }

        private void LoadPageConfiguration(int pageIndex)
        {
            var page = _settingsService.GetPage(pageIndex);
            if (page != null)
            {
                PageNameTextBox.Text = page.Name;
                LoadPageWidgets(page);
            }
        }

        private void LoadPageWidgets(PageConfig page)
        {
            var pageWidgets = new List<PageWidgetItem>();

            foreach (var widget in _originalWidgets)
            {
                // For new pages or widgets not yet configured, default to false (not enabled)
                bool isEnabled = page.WidgetVisibility.TryGetValue(widget.WidgetId, out bool visible) && visible;

                // Get settings summary for this widget on this page
                string settingsSummary = GetWidgetSettingsSummary(page, widget.WidgetId);

                pageWidgets.Add(new PageWidgetItem
                {
                    WidgetId = widget.WidgetId,
                    WidgetName = widget.DisplayName,
                    IsEnabled = isEnabled,
                    SettingsSummary = settingsSummary
                });
            }

            PageWidgetsListView.ItemsSource = pageWidgets;

            // Add a note to help users understand what this page configuration does
            System.Diagnostics.Debug.WriteLine($"Loaded page widgets for '{page.Name}': {pageWidgets.Count} widgets, {pageWidgets.Count(w => w.IsEnabled)} enabled");
        }

        private string GetWidgetSettingsSummary(PageConfig page, string widgetId)
        {
            var widgetSettings = page.GetWidgetSettings(widgetId);
            if (widgetSettings == null || widgetSettings.Count == 0)
            {
                return "Default";
            }

            // Create a simple summary of the settings for display
            var settingsPairs = widgetSettings.Take(2).Select(kvp => $"{kvp.Key}: {kvp.Value}");
            var summary = string.Join(", ", settingsPairs);

            if (widgetSettings.Count > 2)
            {
                summary += "...";
            }

            return summary.Length > 40 ? summary.Substring(0, 37) + "..." : summary;
        }

        private void AddPage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Enter page name:", "New Page");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputValue))
            {
                _settingsService.AddPage(dialog.InputValue);
                LoadPages();
                _pagesChanged = true;
            }
        }

        private void DeletePage_Click(object sender, RoutedEventArgs e)
        {
            if (PagesListBox.SelectedIndex >= 0 && _settingsService.Settings.Pages.Count > 1)
            {
                if (System.Windows.MessageBox.Show("Are you sure you want to delete this page?", "Confirm Delete",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _settingsService.RemovePage(PagesListBox.SelectedIndex);
                    LoadPages();
                    _pagesChanged = true;
                }
            }
            else if (_settingsService.Settings.Pages.Count <= 1)
            {
                System.Windows.MessageBox.Show("Cannot delete the last page.", "Cannot Delete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PageName_Changed(object sender, TextChangedEventArgs e)
        {
            if (_selectedPageIndex >= 0 && PageNameTextBox.Text.Length > 0)
            {
                var page = _settingsService.GetPage(_selectedPageIndex);
                if (page != null && page.Name != PageNameTextBox.Text)
                {
                    page.Name = PageNameTextBox.Text;
                    _settingsService.UpdatePage(_selectedPageIndex, page);
                    LoadPages(); // Refresh the list
                    PagesListBox.SelectedIndex = _selectedPageIndex; // Maintain selection
                    _pagesChanged = true;
                }
            }
        }

        private void PageWidgetVisibility_Changed(object sender, RoutedEventArgs e)
        {
            if (_selectedPageIndex >= 0 && sender is System.Windows.Controls.CheckBox checkBox && checkBox.DataContext is PageWidgetItem item)
            {
                var page = _settingsService.GetPage(_selectedPageIndex);
                if (page != null)
                {
                    page.WidgetVisibility[item.WidgetId] = checkBox.IsChecked ?? false;

                    // Add/remove from widget order list
                    if (checkBox.IsChecked == true && !page.WidgetIds.Contains(item.WidgetId))
                    {
                        page.WidgetIds.Add(item.WidgetId);
                    }
                    else if (checkBox.IsChecked == false && page.WidgetIds.Contains(item.WidgetId))
                    {
                        page.WidgetIds.Remove(item.WidgetId);
                    }

                    _settingsService.UpdatePage(_selectedPageIndex, page);
                    _pagesChanged = true;
                }
            }
        }

        private void MovePageWidgetUp_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement widget reordering within page
        }

        private void MovePageWidgetDown_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement widget reordering within page
        }

        private void MovePageUp_Click(object sender, RoutedEventArgs e)
        {
            if (PagesListBox.SelectedIndex > 0)
            {
                int currentIndex = PagesListBox.SelectedIndex;
                _settingsService.MovePageUp(currentIndex);
                LoadPages();
                PagesListBox.SelectedIndex = currentIndex - 1;
                _pagesChanged = true;
            }
        }

        private void MovePageDown_Click(object sender, RoutedEventArgs e)
        {
            if (PagesListBox.SelectedIndex >= 0 && PagesListBox.SelectedIndex < _settingsService.Settings.Pages.Count - 1)
            {
                int currentIndex = PagesListBox.SelectedIndex;
                _settingsService.MovePageDown(currentIndex);
                LoadPages();
                PagesListBox.SelectedIndex = currentIndex + 1;
                _pagesChanged = true;
            }
        }

        private void RenamePage_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPageIndex >= 0)
            {
                var page = _settingsService.GetPage(_selectedPageIndex);
                if (page != null)
                {
                    var dialog = new InputDialog("Enter new page name:", page.Name);
                    if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputValue))
                    {
                        page.Name = dialog.InputValue;
                        _settingsService.UpdatePage(_selectedPageIndex, page);
                        LoadPages();
                        PagesListBox.SelectedIndex = _selectedPageIndex;
                        PageNameTextBox.Text = dialog.InputValue;
                        _pagesChanged = true;
                    }
                }
            }
        }

        private void ConfigurePageWidget_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as WpfButton;
            if (button?.DataContext is PageWidgetItem pageWidgetItem && _selectedPageIndex >= 0)
            {
                // Find the actual widget instance
                var widget = _originalWidgets.FirstOrDefault(w => w.WidgetId == pageWidgetItem.WidgetId);
                if (widget != null)
                {
                    var page = _settingsService.GetPage(_selectedPageIndex);
                    if (page != null)
                    {
                        // Open widget settings with page-specific context
                        OpenPageSpecificWidgetSettings(widget, page);
                    }
                }
            }
        }

        private void OpenPageSpecificWidgetSettings(IWidget widget, PageConfig page)
        {
            try
            {
                // For now, open the widget's standard settings
                // TODO: Enhance this to pass page-specific context to the widget
                widget.OpenWidgetSettings();

                // After settings are closed, refresh the page widget display
                // This is a simple approach - in a full implementation, we'd want to:
                // 1. Pass the page context to the widget settings
                // 2. Store settings per-page in the PageConfig
                // 3. Apply those settings when the page is displayed

                System.Diagnostics.Debug.WriteLine($"Opened settings for widget '{widget.WidgetId}' on page '{page.Name}'");

                // Refresh the widget list to update any settings summary
                LoadPageWidgets(page);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening widget settings: {ex.Message}");
                System.Windows.MessageBox.Show($"Error opening widget settings: {ex.Message}",
                    "Widget Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public class PageWidgetItem
        {
            public string WidgetId { get; set; } = string.Empty;
            public string WidgetName { get; set; } = string.Empty;
            public bool IsEnabled { get; set; }
            public string SettingsSummary { get; set; } = "Default";
        }


        /// <summary>
        /// Opens the settings window directly to the Pages tab with the specified page selected
        /// </summary>
        /// <param name="pageIndex">The index of the page to select</param>
        public void OpenPageSettings(int pageIndex = -1)
        {
            System.Diagnostics.Debug.WriteLine($"OpenPageSettings called with pageIndex: {pageIndex}, Total pages: {_settingsService.Settings.Pages.Count}");
            // Find the TabControl (assuming it's named or we can find it in the visual tree)
            var tabControl = this.FindName("MainTabControl") as System.Windows.Controls.TabControl;
            if (tabControl == null)
            {
                // Find the TabControl in the visual tree
                tabControl = FindVisualChild<System.Windows.Controls.TabControl>(this);
            }

            if (tabControl != null)
            {
                // Navigate to the Pages tab (index 1 - Display=0, Pages & Widgets=1, Auto-Rotation=2)
                tabControl.SelectedIndex = 1; // Pages & Widgets tab

                // If a specific page index is provided, select it and load its configuration
                if (pageIndex >= 0 && pageIndex < _settingsService.Settings.Pages.Count)
                {
                    _selectedPageIndex = pageIndex;

                    // Use Dispatcher to ensure the UI is updated after tab selection
                    Dispatcher.BeginInvoke(() =>
                    {
                        PagesListBox.SelectedIndex = pageIndex;
                        LoadPageConfiguration(pageIndex);

                        // Focus on the page configuration area
                        PageNameTextBox.Focus();
                    });
                }
                else if (pageIndex >= 0)
                {
                    // If page index is out of bounds, select the first page as fallback
                    _selectedPageIndex = Math.Min(pageIndex, _settingsService.Settings.Pages.Count - 1);

                    Dispatcher.BeginInvoke(() =>
                    {
                        PagesListBox.SelectedIndex = _selectedPageIndex;
                        LoadPageConfiguration(_selectedPageIndex);
                    });
                }
            }
        }

        /// <summary>
        /// Helper method to find a visual child of a specific type
        /// </summary>
        private static T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T result)
                    return result;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
    }
}
