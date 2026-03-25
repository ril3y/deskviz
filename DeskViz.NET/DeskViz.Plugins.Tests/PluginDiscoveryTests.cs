using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using DeskViz.Plugins.Services;
using System.IO;
using System.Reflection;

namespace DeskViz.Plugins.Tests
{
    [TestClass]
    public class PluginDiscoveryTests
    {
        private string _testPluginDirectory = null!;
        private WidgetDiscoveryService _discoveryService = null!;

        [TestInitialize]
        public void Setup()
        {
            _testPluginDirectory = Path.Combine(Path.GetTempPath(), "DeskVizPluginTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testPluginDirectory);
            _discoveryService = new WidgetDiscoveryService(_testPluginDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testPluginDirectory))
            {
                Directory.Delete(_testPluginDirectory, true);
            }
        }

        [TestMethod]
        public void WidgetDiscoveryService_EmptyDirectory_ShouldDiscoverNoWidgets()
        {
            // Act
            _discoveryService.DiscoverWidgets();

            // Assert
            _discoveryService.LoadedWidgets.Should().BeEmpty();
        }

        [TestMethod]
        public void WidgetDiscoveryService_WithValidWidget_ShouldDiscoverWidget()
        {
            // Arrange
            var cpuWidgetPath = Path.Combine(_testPluginDirectory, "DeskViz.Widgets.Cpu.dll");
            var sourcePath = Path.Combine("DeskViz.Widgets.Cpu", "bin", "Debug", "net8.0-windows10.0.19041.0", "DeskViz.Widgets.Cpu.dll");

            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, cpuWidgetPath);

                // Copy dependencies
                var sourceDir = Path.GetDirectoryName(sourcePath)!;
                foreach (var dep in new[] { "DeskViz.Plugins.dll", "LibreHardwareMonitorLib.dll" })
                {
                    var depSource = Path.Combine(sourceDir, dep);
                    var depTarget = Path.Combine(_testPluginDirectory, dep);
                    if (File.Exists(depSource))
                    {
                        File.Copy(depSource, depTarget);
                    }
                }

                // Act
                _discoveryService.DiscoverWidgets();

                // Assert
                _discoveryService.LoadedWidgets.Should().HaveCount(1);
                var widget = _discoveryService.LoadedWidgets[0];
                widget.Metadata.Id.Should().Be("CpuWidget");
                widget.Metadata.Name.Should().Be("CPU Monitor");
            }
            else
            {
                Assert.Inconclusive("CPU widget DLL not found. Build the CPU widget project first.");
            }
        }

        [TestMethod]
        public void WidgetDiscoveryService_CreateWidgetInstance_ShouldReturnValidInstance()
        {
            // Arrange
            var cpuWidgetPath = Path.Combine(_testPluginDirectory, "DeskViz.Widgets.Cpu.dll");
            var sourcePath = Path.Combine("DeskViz.Widgets.Cpu", "bin", "Debug", "net8.0-windows10.0.19041.0", "DeskViz.Widgets.Cpu.dll");

            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, cpuWidgetPath);

                var sourceDir = Path.GetDirectoryName(sourcePath)!;
                foreach (var dep in new[] { "DeskViz.Plugins.dll", "LibreHardwareMonitorLib.dll" })
                {
                    var depSource = Path.Combine(sourceDir, dep);
                    var depTarget = Path.Combine(_testPluginDirectory, dep);
                    if (File.Exists(depSource))
                    {
                        File.Copy(depSource, depTarget);
                    }
                }

                _discoveryService.DiscoverWidgets();

                // Act
                var widget = _discoveryService.CreateWidgetInstance("CpuWidget");

                // Assert
                widget.Should().NotBeNull();
                widget!.WidgetId.Should().Be("CpuWidget");
                widget.DisplayName.Should().Be("CPU Monitor");
            }
            else
            {
                Assert.Inconclusive("CPU widget DLL not found. Build the CPU widget project first.");
            }
        }

        [TestMethod]
        public void WidgetDiscoveryService_IsWidgetAvailable_ShouldReturnCorrectStatus()
        {
            // Arrange
            var cpuWidgetPath = Path.Combine(_testPluginDirectory, "DeskViz.Widgets.Cpu.dll");
            var sourcePath = Path.Combine("DeskViz.Widgets.Cpu", "bin", "Debug", "net8.0-windows10.0.19041.0", "DeskViz.Widgets.Cpu.dll");

            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, cpuWidgetPath);

                var sourceDir = Path.GetDirectoryName(sourcePath)!;
                foreach (var dep in new[] { "DeskViz.Plugins.dll" })
                {
                    var depSource = Path.Combine(sourceDir, dep);
                    var depTarget = Path.Combine(_testPluginDirectory, dep);
                    if (File.Exists(depSource))
                    {
                        File.Copy(depSource, depTarget);
                    }
                }

                _discoveryService.DiscoverWidgets();

                // Act & Assert
                _discoveryService.IsWidgetAvailable("CpuWidget").Should().BeTrue();
                _discoveryService.IsWidgetAvailable("NonExistentWidget").Should().BeFalse();
            }
            else
            {
                Assert.Inconclusive("CPU widget DLL not found. Build the CPU widget project first.");
            }
        }

        [TestMethod]
        public void WidgetDiscoveryService_GetWidgetMetadata_ShouldReturnCorrectMetadata()
        {
            // Arrange
            var cpuWidgetPath = Path.Combine(_testPluginDirectory, "DeskViz.Widgets.Cpu.dll");
            var sourcePath = Path.Combine("DeskViz.Widgets.Cpu", "bin", "Debug", "net8.0-windows10.0.19041.0", "DeskViz.Widgets.Cpu.dll");

            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, cpuWidgetPath);

                var sourceDir = Path.GetDirectoryName(sourcePath)!;
                File.Copy(Path.Combine(sourceDir, "DeskViz.Plugins.dll"), Path.Combine(_testPluginDirectory, "DeskViz.Plugins.dll"));

                _discoveryService.DiscoverWidgets();

                // Act
                var metadata = _discoveryService.GetWidgetMetadata("CpuWidget");

                // Assert
                metadata.Should().NotBeNull();
                metadata!.Id.Should().Be("CpuWidget");
                metadata.Name.Should().Be("CPU Monitor");
                metadata.Category.Should().Be("Hardware");
                metadata.Tags.Should().Contain("cpu");
            }
            else
            {
                Assert.Inconclusive("CPU widget DLL not found. Build the CPU widget project first.");
            }
        }
    }
}