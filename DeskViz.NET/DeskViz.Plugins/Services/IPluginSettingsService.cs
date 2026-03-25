using System;

namespace DeskViz.Plugins.Services
{
    public interface IPluginSettingsService
    {
        T? LoadSettings<T>(string widgetId) where T : class, new();
        void SaveSettings<T>(string widgetId, T settings) where T : class;
        bool HasSettings(string widgetId);
        void DeleteSettings(string widgetId);

        event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
    }

    public class SettingsChangedEventArgs : EventArgs
    {
        public string WidgetId { get; }
        public Type SettingsType { get; }

        public SettingsChangedEventArgs(string widgetId, Type settingsType)
        {
            WidgetId = widgetId;
            SettingsType = settingsType;
        }
    }
}