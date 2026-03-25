using System;
using System.Collections.Generic;
using DeskViz.Plugins.Base;

namespace DeskViz.Widgets.Ram
{
    public class RamWidgetSettings : BaseWidgetSettings
    {
        public override string WidgetId => "RamWidget";

        private double _updateIntervalSeconds = 1.0;
        private bool _showPageFileInfo = true;

        public double UpdateIntervalSeconds
        {
            get => _updateIntervalSeconds;
            set => SetProperty(ref _updateIntervalSeconds, value);
        }

        public bool ShowPageFileInfo
        {
            get => _showPageFileInfo;
            set => SetProperty(ref _showPageFileInfo, value);
        }

        public override object Clone()
        {
            return new RamWidgetSettings
            {
                UpdateIntervalSeconds = UpdateIntervalSeconds,
                ShowPageFileInfo = ShowPageFileInfo
            };
        }

        public override void Reset()
        {
            UpdateIntervalSeconds = 1.0;
            ShowPageFileInfo = true;
        }

        protected override BaseWidgetSettings CreateDefault()
        {
            return new RamWidgetSettings();
        }

        protected override void ValidateSettings(List<string> errors)
        {
            if (UpdateIntervalSeconds < 0.1)
                errors.Add("Update interval must be at least 0.1 seconds");

            if (UpdateIntervalSeconds > 60)
                errors.Add("Update interval cannot exceed 60 seconds");
        }

        public override bool Equals(object? obj)
        {
            if (obj is not RamWidgetSettings other) return false;

            return UpdateIntervalSeconds == other.UpdateIntervalSeconds &&
                   ShowPageFileInfo == other.ShowPageFileInfo;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UpdateIntervalSeconds, ShowPageFileInfo);
        }
    }
}