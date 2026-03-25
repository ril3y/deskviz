using System;
using System.Collections.Generic;
using DeskViz.Plugins.Base;

namespace DeskViz.Widgets.Cpu
{
    public class CpuWidgetSettings : BaseWidgetSettings
    {
        public override string WidgetId => "CpuWidget";

        private double _updateIntervalSeconds = 1.0;
        private bool _showCores = true;
        private bool _showTemperature = true;
        private bool _useFahrenheit = false;
        private double _temperatureFontSize = 12.0;
        private bool _showClockSpeed = false;
        private bool _showPowerUsage = false;

        public double UpdateIntervalSeconds
        {
            get => _updateIntervalSeconds;
            set => SetProperty(ref _updateIntervalSeconds, value);
        }

        public bool ShowCores
        {
            get => _showCores;
            set => SetProperty(ref _showCores, value);
        }

        public bool ShowTemperature
        {
            get => _showTemperature;
            set => SetProperty(ref _showTemperature, value);
        }

        public bool UseFahrenheit
        {
            get => _useFahrenheit;
            set => SetProperty(ref _useFahrenheit, value);
        }

        public double TemperatureFontSize
        {
            get => _temperatureFontSize;
            set => SetProperty(ref _temperatureFontSize, value);
        }

        public bool ShowClockSpeed
        {
            get => _showClockSpeed;
            set => SetProperty(ref _showClockSpeed, value);
        }

        public bool ShowPowerUsage
        {
            get => _showPowerUsage;
            set => SetProperty(ref _showPowerUsage, value);
        }

        public override object Clone()
        {
            return new CpuWidgetSettings
            {
                UpdateIntervalSeconds = UpdateIntervalSeconds,
                ShowCores = ShowCores,
                ShowTemperature = ShowTemperature,
                UseFahrenheit = UseFahrenheit,
                TemperatureFontSize = TemperatureFontSize,
                ShowClockSpeed = ShowClockSpeed,
                ShowPowerUsage = ShowPowerUsage
            };
        }

        public override void Reset()
        {
            UpdateIntervalSeconds = 1.0;
            ShowCores = true;
            ShowTemperature = true;
            UseFahrenheit = false;
            TemperatureFontSize = 12.0;
            ShowClockSpeed = false;
            ShowPowerUsage = false;
        }

        protected override BaseWidgetSettings CreateDefault()
        {
            return new CpuWidgetSettings();
        }

        protected override void ValidateSettings(List<string> errors)
        {
            if (UpdateIntervalSeconds < 0.1)
                errors.Add("Update interval must be at least 0.1 seconds");

            if (UpdateIntervalSeconds > 60)
                errors.Add("Update interval cannot exceed 60 seconds");

            if (TemperatureFontSize < 6)
                errors.Add("Temperature font size must be at least 6");

            if (TemperatureFontSize > 72)
                errors.Add("Temperature font size cannot exceed 72");
        }

        public override bool Equals(object? obj)
        {
            if (obj is not CpuWidgetSettings other) return false;

            return UpdateIntervalSeconds == other.UpdateIntervalSeconds &&
                   ShowCores == other.ShowCores &&
                   ShowTemperature == other.ShowTemperature &&
                   UseFahrenheit == other.UseFahrenheit &&
                   Math.Abs(TemperatureFontSize - other.TemperatureFontSize) < 0.001 &&
                   ShowClockSpeed == other.ShowClockSpeed &&
                   ShowPowerUsage == other.ShowPowerUsage;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UpdateIntervalSeconds, ShowCores, ShowTemperature,
                UseFahrenheit, TemperatureFontSize, ShowClockSpeed, ShowPowerUsage);
        }
    }
}