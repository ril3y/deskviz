using System;

namespace DeskViz.App.Widgets
{
    /// <summary>
    /// Interface for all DeskViz widgets
    /// </summary>
    public interface IWidget
    {
        /// <summary>
        /// Gets the unique widget identifier
        /// </summary>
        string WidgetId { get; }
        
        /// <summary>
        /// Gets the display name of the widget
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// Gets or sets whether the widget is visible
        /// </summary>
        bool IsWidgetVisible { get; set; }
        
        /// <summary>
        /// Event raised when the widget configuration button is clicked
        /// </summary>
        event EventHandler ConfigButtonClicked;
        
        /// <summary>
        /// Refreshes the widget data
        /// </summary>
        void RefreshData();
        
        /// <summary>
        /// Opens the settings UI specific to this widget
        /// </summary>
        void OpenWidgetSettings();
    }
}
