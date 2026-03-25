using System.Collections.Generic;
using DeskViz.Core.Models;

namespace DeskViz.Core.Services
{
    public interface ISettingsService
    {
        AppSettings Settings { get; }

        AppSettings LoadSettings();
        void SaveSettings();
        void SaveSettings(AppSettings settings);

        void UpdatePreferredDisplay(string screenIdentifier);
        void MarkFirstRunComplete();

        void UpdateWidgetVisibility(string widgetId, bool isVisible);
        void UpdateWidgetVisibility(Dictionary<string, bool> visibilitySettings);
        void UpdateWidgetOrder(List<string> widgetOrder);
        void UpdateWidgetOrientation(WidgetOrientationSetting orientation);

        void AddPage(string pageName);
        void RemovePage(int pageIndex);
        void UpdatePage(int pageIndex, PageConfig pageConfig);
        PageConfig? GetPage(int pageIndex);
        void SetCurrentPageIndex(int pageIndex);
        void MovePageUp(int pageIndex);
        void MovePageDown(int pageIndex);

        void UpdateAutoRotationSettings(bool enabled, int intervalSeconds, AutoRotationMode mode, bool pauseOnInteraction);
        int GetNextPageIndex(AutoRotationMode mode, int currentIndex, ref bool pingPongDirection);
    }
}
