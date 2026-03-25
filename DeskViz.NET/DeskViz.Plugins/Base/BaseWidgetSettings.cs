using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DeskViz.Plugins.Interfaces;

namespace DeskViz.Plugins.Base
{
    public abstract class BaseWidgetSettings : IWidgetSettings
    {
        public abstract string WidgetId { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public abstract object Clone();

        public abstract void Reset();

        public virtual bool IsDefault()
        {
            var defaultSettings = CreateDefault();
            return Equals(defaultSettings);
        }

        public virtual bool Validate()
        {
            var errors = GetValidationErrors();
            return errors.Length == 0;
        }

        public virtual string[] GetValidationErrors()
        {
            var errors = new List<string>();
            ValidateSettings(errors);
            return errors.ToArray();
        }

        protected abstract BaseWidgetSettings CreateDefault();

        protected virtual void ValidateSettings(List<string> errors)
        {
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }
    }
}