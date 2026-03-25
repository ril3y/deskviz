using System;
using DeskViz.Plugins.Interfaces;

namespace DeskViz.Plugins.Base
{
    public class WidgetMetadata : IWidgetMetadata
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public Version Version { get; set; } = new Version(1, 0, 0);
        public string Category { get; set; } = "General";
        public string[] Tags { get; set; } = Array.Empty<string>();
        public bool RequiresElevatedPermissions { get; set; } = false;
        public Version MinimumHostVersion { get; set; } = new Version(1, 0, 0);
        public string IconPath { get; set; } = string.Empty;
    }
}