using System;

namespace DeskViz.Plugins.Interfaces
{
    public interface IWidgetMetadata
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        string Author { get; }
        Version Version { get; }
        string Category { get; }
        string[] Tags { get; }
        bool RequiresElevatedPermissions { get; }
        Version MinimumHostVersion { get; }
        string IconPath { get; }
    }
}