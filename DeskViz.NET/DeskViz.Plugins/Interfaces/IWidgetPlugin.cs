using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace DeskViz.Plugins.Interfaces
{
    public interface IWidgetPlugin : INotifyPropertyChanged
    {
        IWidgetMetadata Metadata { get; }

        string WidgetId { get; }
        string DisplayName { get; }

        bool IsWidgetVisible { get; set; }
        bool IsConfiguring { get; set; }

        event EventHandler? ConfigButtonClicked;

        ICommand? ConfigureWidgetCommand { get; }

        void Initialize(IWidgetHost host);
        void Shutdown();

        void RefreshData();
        void OpenWidgetSettings();

        FrameworkElement CreateWidgetUI();
        FrameworkElement? CreateSettingsUI();

        void OnPageChanged(string pageId);
        void OnVisibilityChanged(bool isVisible);
    }
}