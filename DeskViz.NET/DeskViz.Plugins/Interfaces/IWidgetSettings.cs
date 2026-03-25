using System;
using System.ComponentModel;

namespace DeskViz.Plugins.Interfaces
{
    public interface IWidgetSettings : INotifyPropertyChanged, ICloneable
    {
        string WidgetId { get; }

        void Reset();
        bool IsDefault();
        bool Validate();
        string[] GetValidationErrors();
    }
}