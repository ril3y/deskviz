using System;
using System.Collections.Generic;

namespace DeskViz.Core.Models
{
    /// <summary>
    /// Represents a page configuration containing widgets
    /// </summary>
    public class PageConfig
    {
        /// <summary>
        /// Unique identifier for the page
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name of the page
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// List of widget IDs in display order
        /// </summary>
        public List<string> WidgetIds { get; set; }

        /// <summary>
        /// Widget-specific visibility settings for this page
        /// </summary>
        public Dictionary<string, bool> WidgetVisibility { get; set; }

        /// <summary>
        /// Widget-specific configuration settings for this page
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> WidgetSettings { get; set; }

        /// <summary>
        /// Optional background color or image for the page
        /// </summary>
        public string? BackgroundSetting { get; set; }

        /// <summary>
        /// Page creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Initializes a new instance of PageConfig
        /// </summary>
        public PageConfig()
        {
            Id = Guid.NewGuid().ToString();
            Name = "New Page";
            WidgetIds = new List<string>();
            WidgetVisibility = new Dictionary<string, bool>();
            WidgetSettings = new Dictionary<string, Dictionary<string, object>>();
            CreatedAt = DateTime.Now;
        }

        /// <summary>
        /// Creates a new page with the specified name
        /// </summary>
        public PageConfig(string name) : this()
        {
            Name = name;
        }

        /// <summary>
        /// Clones this page configuration
        /// </summary>
        public PageConfig Clone()
        {
            var clonedSettings = new Dictionary<string, Dictionary<string, object>>();
            foreach (var kvp in WidgetSettings)
            {
                clonedSettings[kvp.Key] = new Dictionary<string, object>(kvp.Value);
            }

            return new PageConfig
            {
                Id = Guid.NewGuid().ToString(), // New ID for cloned page
                Name = $"{Name} (Copy)",
                WidgetIds = new List<string>(WidgetIds),
                WidgetVisibility = new Dictionary<string, bool>(WidgetVisibility),
                WidgetSettings = clonedSettings,
                BackgroundSetting = BackgroundSetting,
                CreatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Gets widget settings for a specific widget on this page
        /// </summary>
        public Dictionary<string, object>? GetWidgetSettings(string widgetId)
        {
            return WidgetSettings.TryGetValue(widgetId, out var settings) ? settings : null;
        }

        /// <summary>
        /// Sets widget settings for a specific widget on this page
        /// </summary>
        public void SetWidgetSettings(string widgetId, Dictionary<string, object> settings)
        {
            WidgetSettings[widgetId] = new Dictionary<string, object>(settings);
        }

        /// <summary>
        /// Gets a specific widget setting value
        /// </summary>
        public T? GetWidgetSetting<T>(string widgetId, string settingKey, T? defaultValue = default)
        {
            if (WidgetSettings.TryGetValue(widgetId, out var widgetSettings) &&
                widgetSettings.TryGetValue(settingKey, out var value))
            {
                try
                {
                    return (T)value;
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Sets a specific widget setting value
        /// </summary>
        public void SetWidgetSetting(string widgetId, string settingKey, object value)
        {
            if (!WidgetSettings.ContainsKey(widgetId))
            {
                WidgetSettings[widgetId] = new Dictionary<string, object>();
            }
            WidgetSettings[widgetId][settingKey] = value;
        }
    }
}