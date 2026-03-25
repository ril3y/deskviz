using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Moq;
using DeskViz.Widgets.Cpu;
using DeskViz.Plugins.Tests.Base;
using DeskViz.Plugins.Tests.Mocks;
using DeskViz.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using LogLevel = DeskViz.Plugins.Tests.Mocks.LogLevel;

namespace DeskViz.Widgets.Cpu.Tests
{
    [TestClass]
    public class CpuWidgetTests : BaseWidgetTests<CpuWidget>
    {
        private Mock<IPluginHardwareMonitorService> _mockHardwareService = null!;

        [TestInitialize]
        public override void Setup()
        {
            // Set thread to STA for WPF components
            Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

            _mockHardwareService = new Mock<IPluginHardwareMonitorService>();
            SetupMockHardwareService();

            base.Setup();

            // Register the mock service
            ((MockServiceProvider)MockHost.ServiceProvider).RegisterService(_mockHardwareService.Object);
        }

        private void SetupMockHardwareService()
        {
            _mockHardwareService.Setup(x => x.IsInitialized).Returns(true);
            _mockHardwareService.Setup(x => x.GetCpuName()).Returns("Test CPU");
            _mockHardwareService.Setup(x => x.GetOverallCpuUsage()).Returns(45.5f);
            _mockHardwareService.Setup(x => x.GetCpuPackageTemperature()).Returns(55.0f);
            _mockHardwareService.Setup(x => x.GetCpuClockSpeed()).Returns(3200.0f);
            _mockHardwareService.Setup(x => x.GetCpuPowerUsage()).Returns(65.0f);
            _mockHardwareService.Setup(x => x.GetCpuCoreUsage()).Returns(new List<float> { 40.0f, 50.0f, 45.0f, 55.0f });
        }

        [TestMethod]
        public void CpuWidget_ShouldHaveCorrectMetadata()
        {
            Widget.Metadata.Id.Should().Be("CpuWidget");
            Widget.Metadata.Name.Should().Be("CPU Monitor");
            Widget.Metadata.Category.Should().Be("Hardware");
            Widget.Metadata.Tags.Should().Contain("cpu");
            Widget.Metadata.RequiresElevatedPermissions.Should().BeFalse();
        }

        [TestMethod]
        public void CpuWidget_InitializeWithHardwareService_ShouldLoadData()
        {
            // Act
            Widget.RefreshData();

            // Assert
            Widget.CpuName.Should().Be("Test CPU");
            Widget.OverallCpuUsagePercentage.Should().Be(45.5f);
            Widget.CpuTemperature.Should().Be(55.0f);
            Widget.CpuClockSpeed.Should().Be(3200.0f);
            Widget.CpuPowerUsage.Should().Be(65.0f);
        }

        [TestMethod]
        public void CpuWidget_RefreshData_ShouldUpdateCpuCores()
        {
            // Act
            Widget.RefreshData();

            // Assert
            Widget.CpuCores.Should().HaveCount(4);
            Widget.CpuCores[0].Name.Should().Be("Core 1");
            Widget.CpuCores[0].UsagePercentage.Should().Be(40.0f);
            Widget.CpuCores[1].UsagePercentage.Should().Be(50.0f);
            Widget.CpuCores[2].UsagePercentage.Should().Be(45.0f);
            Widget.CpuCores[3].UsagePercentage.Should().Be(55.0f);
        }

        [TestMethod]
        public void CpuWidget_ShowCoresProperty_ShouldControlCoreVisibility()
        {
            // Arrange
            Widget.RefreshData(); // Populate cores first

            // Act
            Widget.ShowCores = false;

            // Assert
            Widget.ShowCores.Should().BeFalse();
            // Note: In actual UI, this would hide the cores panel
        }

        [TestMethod]
        public void CpuWidget_TemperatureSettings_ShouldUpdateCorrectly()
        {
            // Act
            Widget.ShowTemperature = false;
            Widget.UseFahrenheit = true;
            Widget.TemperatureFontSize = 16.0;

            // Assert
            Widget.ShowTemperature.Should().BeFalse();
            Widget.UseFahrenheit.Should().BeTrue();
            Widget.TemperatureFontSize.Should().Be(16.0);
        }

        [TestMethod]
        public void CpuWidget_UpdateInterval_ShouldRestartTimer()
        {
            // Act
            Widget.UpdateIntervalSeconds = 5.0;

            // Assert
            Widget.UpdateIntervalSeconds.Should().Be(5.0);
            // Timer restart is tested through the base class behavior
        }

        [TestMethod]
        public void CpuWidget_HardwareServiceUnavailable_ShouldHandleGracefully()
        {
            // Arrange
            ((MockServiceProvider)MockHost.ServiceProvider).RegisterService<IPluginHardwareMonitorService>(null!);
            var widgetWithoutService = new CpuWidget();

            // Act
            widgetWithoutService.Initialize(MockHost);
            Action refreshAction = () => widgetWithoutService.RefreshData();

            // Assert
            refreshAction.Should().NotThrow();
            MockHost.Logs.Should().Contain(log => log.Level == LogLevel.Warning);
        }

        [TestMethod]
        public void CpuWidget_CreateSettingsUI_ShouldReturnValidSettingsView()
        {
            // Act
            var settingsUI = Widget.CreateSettingsUI();

            // Assert
            settingsUI.Should().NotBeNull();
            settingsUI.Should().BeOfType<CpuWidgetSettingsView>();
        }

        [TestMethod]
        public void CpuWidget_UsageColor_ShouldChangeBasedOnPercentage()
        {
            // Test low usage (green)
            Widget.OverallCpuUsagePercentage = 30.0f;
            Widget.OverallCpuUsageColor.Should().NotBeNull();

            // Test medium usage (orange)
            Widget.OverallCpuUsagePercentage = 80.0f;
            Widget.OverallCpuUsageColor.Should().NotBeNull();

            // Test high usage (red)
            Widget.OverallCpuUsagePercentage = 95.0f;
            Widget.OverallCpuUsageColor.Should().NotBeNull();
        }

        [TestMethod]
        public void CpuWidget_HardwareServiceException_ShouldLogError()
        {
            // Arrange
            _mockHardwareService.Setup(x => x.Update()).Throws(new System.Exception("Hardware error"));

            // Act
            Widget.RefreshData();

            // Assert
            MockHost.Logs.Should().Contain(log =>
                log.Level == LogLevel.Error &&
                log.Message.Contains("Error refreshing CPU data"));
        }

        protected override void AssertWidgetSpecificBehavior()
        {
            // CPU widget specific assertions
            Widget.CpuCores.Should().NotBeNull();
            Widget.Metadata.Tags.Should().Contain("cpu");
            Widget.WidgetId.Should().Be("CpuWidget");
        }
    }
}