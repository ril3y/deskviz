using System;
using System.Collections.Generic;
using DeskViz.Plugins.Base;

namespace DeskViz.Widgets.Clock
{
    public class ClockWidgetSettings : BaseWidgetSettings
    {
        public override string WidgetId => "ClockWidget";

        private bool _is24HourFormat = true;
        private double _clockFontSize = 24.0;
        private double _updateIntervalSeconds = 1.0;

        public bool Is24HourFormat
        {
            get => _is24HourFormat;
            set => SetProperty(ref _is24HourFormat, value);
        }

        public double ClockFontSize
        {
            get => _clockFontSize;
            set => SetProperty(ref _clockFontSize, value);
        }

        public double UpdateIntervalSeconds
        {
            get => _updateIntervalSeconds;
            set => SetProperty(ref _updateIntervalSeconds, value);
        }

        public override object Clone()
        {
            return new ClockWidgetSettings
            {
                Is24HourFormat = Is24HourFormat,
                ClockFontSize = ClockFontSize,
                UpdateIntervalSeconds = UpdateIntervalSeconds
            };
        }

        public override void Reset()
        {
            Is24HourFormat = true;
            ClockFontSize = 24.0;
            UpdateIntervalSeconds = 1.0;
        }

        protected override BaseWidgetSettings CreateDefault()
        {
            return new ClockWidgetSettings();
        }

        protected override void ValidateSettings(List<string> errors)
        {
            if (ClockFontSize < 6)
                errors.Add("Clock font size must be at least 6");

            if (ClockFontSize > 72)
                errors.Add("Clock font size cannot exceed 72");

            if (UpdateIntervalSeconds < 0.1)
                errors.Add("Update interval must be at least 0.1 seconds");

            if (UpdateIntervalSeconds > 60)
                errors.Add("Update interval cannot exceed 60 seconds");
        }

        public override bool Equals(object? obj)
        {
            if (obj is not ClockWidgetSettings other) return false;

            return Is24HourFormat == other.Is24HourFormat &&
                   Math.Abs(ClockFontSize - other.ClockFontSize) < 0.001 &&
                   Math.Abs(UpdateIntervalSeconds - other.UpdateIntervalSeconds) < 0.001;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Is24HourFormat, ClockFontSize, UpdateIntervalSeconds);
        }
    }
}