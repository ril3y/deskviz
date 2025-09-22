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
            return new PageConfig
            {
                Id = Guid.NewGuid().ToString(), // New ID for cloned page
                Name = $"{Name} (Copy)",
                WidgetIds = new List<string>(WidgetIds),
                WidgetVisibility = new Dictionary<string, bool>(WidgetVisibility),
                BackgroundSetting = BackgroundSetting,
                CreatedAt = DateTime.Now
            };
        }
    }
}