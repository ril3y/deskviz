using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using DeskViz.Widgets.Cpu;

namespace DeskViz.Widgets.Cpu.Tests
{
    [TestClass]
    public class CpuWidgetSettingsTests
    {
        private CpuWidgetSettings _settings = null!;

        [TestInitialize]
        public void Setup()
        {
            _settings = new CpuWidgetSettings();
        }

        [TestMethod]
        public void CpuWidgetSettings_DefaultValues_ShouldBeValid()
        {
            // Assert
            _settings.WidgetId.Should().Be("CpuWidget");
            _settings.UpdateIntervalSeconds.Should().Be(2.5);
            _settings.ShowCores.Should().BeTrue();
            _settings.ShowTemperature.Should().BeTrue();
            _settings.UseFahrenheit.Should().BeFalse();
            _settings.TemperatureFontSize.Should().Be(12.0);
            _settings.ShowClockSpeed.Should().BeFalse();
            _settings.ShowPowerUsage.Should().BeFalse();
        }

        [TestMethod]
        public void CpuWidgetSettings_IsDefault_ShouldReturnTrueForDefaults()
        {
            // Act & Assert
            _settings.IsDefault().Should().BeTrue();
        }

        [TestMethod]
        public void CpuWidgetSettings_IsDefault_ShouldReturnFalseForModified()
        {
            // Arrange
            _settings.UpdateIntervalSeconds = 5.0;

            // Act & Assert
            _settings.IsDefault().Should().BeFalse();
        }

        [TestMethod]
        public void CpuWidgetSettings_Validate_ShouldPassForValidSettings()
        {
            // Act
            var isValid = _settings.Validate();
            var errors = _settings.GetValidationErrors();

            // Assert
            isValid.Should().BeTrue();
            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void CpuWidgetSettings_Validate_ShouldFailForInvalidUpdateInterval()
        {
            // Arrange
            _settings.UpdateIntervalSeconds = 0.05; // Too small

            // Act
            var isValid = _settings.Validate();
            var errors = _settings.GetValidationErrors();

            // Assert
            isValid.Should().BeFalse();
            errors.Should().Contain("Update interval must be at least 0.1 seconds");
        }

        [TestMethod]
        public void CpuWidgetSettings_Validate_ShouldFailForTooLargeUpdateInterval()
        {
            // Arrange
            _settings.UpdateIntervalSeconds = 120; // Too large

            // Act
            var isValid = _settings.Validate();
            var errors = _settings.GetValidationErrors();

            // Assert
            isValid.Should().BeFalse();
            errors.Should().Contain("Update interval cannot exceed 60 seconds");
        }

        [TestMethod]
        public void CpuWidgetSettings_Validate_ShouldFailForInvalidFontSize()
        {
            // Arrange
            _settings.TemperatureFontSize = 4; // Too small

            // Act
            var isValid = _settings.Validate();
            var errors = _settings.GetValidationErrors();

            // Assert
            isValid.Should().BeFalse();
            errors.Should().Contain("Temperature font size must be at least 6");
        }

        [TestMethod]
        public void CpuWidgetSettings_Clone_ShouldCreateExactCopy()
        {
            // Arrange
            _settings.UpdateIntervalSeconds = 3.5;
            _settings.ShowCores = false;
            _settings.UseFahrenheit = true;
            _settings.TemperatureFontSize = 16.0;

            // Act
            var clone = (CpuWidgetSettings)_settings.Clone();

            // Assert
            clone.Should().NotBeSameAs(_settings);
            clone.UpdateIntervalSeconds.Should().Be(_settings.UpdateIntervalSeconds);
            clone.ShowCores.Should().Be(_settings.ShowCores);
            clone.UseFahrenheit.Should().Be(_settings.UseFahrenheit);
            clone.TemperatureFontSize.Should().Be(_settings.TemperatureFontSize);
        }

        [TestMethod]
        public void CpuWidgetSettings_Reset_ShouldRestoreDefaults()
        {
            // Arrange
            _settings.UpdateIntervalSeconds = 10.0;
            _settings.ShowCores = false;
            _settings.UseFahrenheit = true;

            // Act
            _settings.Reset();

            // Assert
            _settings.UpdateIntervalSeconds.Should().Be(2.5);
            _settings.ShowCores.Should().BeTrue();
            _settings.UseFahrenheit.Should().BeFalse();
        }

        [TestMethod]
        public void CpuWidgetSettings_PropertyChanged_ShouldFireForAllProperties()
        {
            // Arrange
            var propertyChangedEvents = new List<string>();
            _settings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null)
                    propertyChangedEvents.Add(e.PropertyName);
            };

            // Act
            _settings.UpdateIntervalSeconds = 5.0;
            _settings.ShowCores = false;
            _settings.ShowTemperature = false;
            _settings.UseFahrenheit = true;
            _settings.TemperatureFontSize = 14.0;
            _settings.ShowClockSpeed = true;
            _settings.ShowPowerUsage = true;

            // Assert
            propertyChangedEvents.Should().Contain(nameof(_settings.UpdateIntervalSeconds));
            propertyChangedEvents.Should().Contain(nameof(_settings.ShowCores));
            propertyChangedEvents.Should().Contain(nameof(_settings.ShowTemperature));
            propertyChangedEvents.Should().Contain(nameof(_settings.UseFahrenheit));
            propertyChangedEvents.Should().Contain(nameof(_settings.TemperatureFontSize));
            propertyChangedEvents.Should().Contain(nameof(_settings.ShowClockSpeed));
            propertyChangedEvents.Should().Contain(nameof(_settings.ShowPowerUsage));
        }

        [TestMethod]
        public void CpuWidgetSettings_Equals_ShouldWorkCorrectly()
        {
            // Arrange
            var settings1 = new CpuWidgetSettings();
            var settings2 = new CpuWidgetSettings();
            var settings3 = new CpuWidgetSettings { UpdateIntervalSeconds = 5.0 };

            // Act & Assert
            settings1.Equals(settings2).Should().BeTrue();
            settings1.Equals(settings3).Should().BeFalse();
            settings1.Equals(null).Should().BeFalse();
            settings1.Equals("not a settings object" as object).Should().BeFalse();
        }

        [TestMethod]
        public void CpuWidgetSettings_GetHashCode_ShouldBeConsistent()
        {
            // Arrange
            var settings1 = new CpuWidgetSettings();
            var settings2 = new CpuWidgetSettings();

            // Act & Assert
            settings1.GetHashCode().Should().Be(settings2.GetHashCode());
        }
    }
}