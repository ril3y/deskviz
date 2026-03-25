using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using DeskViz.Plugins.Base;
using DeskViz.Plugins.Interfaces;
using DeskViz.Plugins.Tests.Mocks;
using System.Windows;

namespace DeskViz.Plugins.Tests.Base
{
    [TestClass]
    public abstract class BaseWidgetTests<TWidget> where TWidget : BaseWidget, new()
    {
        protected TWidget Widget { get; private set; } = null!;
        protected MockWidgetHost MockHost { get; private set; } = null!;

        [TestInitialize]
        public virtual void Setup()
        {
            Widget = new TWidget();
            MockHost = new MockWidgetHost();
            Widget.Initialize(MockHost);
        }

        [TestCleanup]
        public virtual void Cleanup()
        {
            Widget?.Shutdown();
        }

        [TestMethod]
        public void Widget_ShouldHaveValidMetadata()
        {
            Widget.Metadata.Should().NotBeNull();
            Widget.Metadata.Id.Should().NotBeNullOrEmpty();
            Widget.Metadata.Name.Should().NotBeNullOrEmpty();
            Widget.Metadata.Version.Should().NotBeNull();
        }

        [TestMethod]
        public void Widget_ShouldHaveValidIdentifiers()
        {
            Widget.WidgetId.Should().NotBeNullOrEmpty();
            Widget.DisplayName.Should().NotBeNullOrEmpty();
            Widget.WidgetId.Should().Be(Widget.Metadata.Id);
        }

        [TestMethod]
        public void Widget_ShouldInitializeCorrectly()
        {
            Widget.Should().NotBeNull();
            Widget.IsWidgetVisible.Should().BeTrue();
            Widget.IsConfiguring.Should().BeFalse();
        }

        [TestMethod]
        public void Widget_VisibilityChange_ShouldUpdateProperty()
        {
            Widget.IsWidgetVisible = false;
            Widget.IsWidgetVisible.Should().BeFalse();
            Widget.Visibility.Should().Be(Visibility.Collapsed);

            Widget.IsWidgetVisible = true;
            Widget.IsWidgetVisible.Should().BeTrue();
            Widget.Visibility.Should().Be(Visibility.Visible);
        }

        [TestMethod]
        public void Widget_ConfiguringMode_ShouldUpdateProperty()
        {
            Widget.IsConfiguring = true;
            Widget.IsConfiguring.Should().BeTrue();

            Widget.IsConfiguring = false;
            Widget.IsConfiguring.Should().BeFalse();
        }

        [TestMethod]
        public void Widget_ConfigureCommand_ShouldNotBeNull()
        {
            Widget.ConfigureWidgetCommand.Should().NotBeNull();
        }

        [TestMethod]
        public void Widget_RefreshData_ShouldNotThrow()
        {
            Action action = () => Widget.RefreshData();
            action.Should().NotThrow();
        }

        [TestMethod]
        public void Widget_CreateWidgetUI_ShouldReturnValidElement()
        {
            var ui = Widget.CreateWidgetUI();
            ui.Should().NotBeNull();
            ui.Should().Be(Widget); // Base implementation returns self
        }

        [TestMethod]
        public void Widget_Shutdown_ShouldCleanupResources()
        {
            Widget.Shutdown();
            // Verify no exceptions and proper cleanup
            // Derived tests can override to check specific cleanup
        }

        protected virtual void AssertWidgetSpecificBehavior()
        {
            // Override in derived tests for widget-specific assertions
        }
    }
}