using System;
using System.Collections.Generic;

namespace DeskViz.Plugins.Interfaces
{
    public interface IWidgetHost
    {
        IWidgetServiceProvider ServiceProvider { get; }

        void SaveWidgetSettings(string widgetId, object settings);
        T? LoadWidgetSettings<T>(string widgetId) where T : class, new();

        // Per-page settings support
        void SaveWidgetSettingsForPage(string widgetId, string pageId, object settings);
        T? LoadWidgetSettingsForPage<T>(string widgetId, string pageId) where T : class, new();

        void ShowMessage(string title, string message, MessageType messageType = MessageType.Information);
        bool ShowConfirmation(string title, string message);

        void RequestWidgetRefresh(string widgetId);
        void RequestWidgetRemoval(string widgetId);

        string GetWidgetDataPath(string widgetId);
        string GetSharedDataPath();

        void LogDebug(string widgetId, string message);
        void LogInfo(string widgetId, string message);
        void LogWarning(string widgetId, string message);
        void LogError(string widgetId, string message, Exception? exception = null);
    }

    public enum MessageType
    {
        Information,
        Warning,
        Error,
        Question
    }
}