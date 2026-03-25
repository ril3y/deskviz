using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.ComponentModel;
using DeskViz.App.Controls;
using DeskViz.Core.Models;
using DeskViz.Plugins.Interfaces;

namespace DeskViz.App.Tests.Controls
{
    [TestClass]
    public class PagedWidgetContainerTests
    {
        private PagedWidgetContainer _container = null!;
        private List<PageConfig> _testPages = null!;
        private List<IWidgetPlugin> _testWidgets = null!;
        private Mock<IWidgetPlugin> _mockWidget1 = null!;
        private Mock<IWidgetPlugin> _mockWidget2 = null!;
        private TestWidget _testUIWidget1 = null!;
        private TestWidget _testUIWidget2 = null!;

        [TestInitialize]
        public void Setup()
        {
            // Initialize on UI thread
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _container = new PagedWidgetContainer();

                // Create test UI elements
                _testUIWidget1 = new TestWidget("Widget1");
                _testUIWidget2 = new TestWidget("Widget2");

                // Setup mock widgets
                _mockWidget1 = new Mock<IWidgetPlugin>();
                _mockWidget1.Setup(w => w.WidgetId).Returns("Widget1");
                _mockWidget1.Setup(w => w.DisplayName).Returns("Test Widget 1");
                _mockWidget1.Setup(w => w.CreateWidgetUI()).Returns(_testUIWidget1);
                _mockWidget1.Setup(w => w.IsWidgetVisible).Returns(true);

                _mockWidget2 = new Mock<IWidgetPlugin>();
                _mockWidget2.Setup(w => w.WidgetId).Returns("Widget2");
                _mockWidget2.Setup(w => w.DisplayName).Returns("Test Widget 2");
                _mockWidget2.Setup(w => w.CreateWidgetUI()).Returns(_testUIWidget2);
                _mockWidget2.Setup(w => w.IsWidgetVisible).Returns(true);

                _testWidgets = new List<IWidgetPlugin> { _mockWidget1.Object, _mockWidget2.Object };

                // Create test pages
                var page1 = new PageConfig("Page 1");
                page1.WidgetIds.Add("Widget1");
                page1.WidgetVisibility["Widget1"] = true;

                var page2 = new PageConfig("Page 2");
                page2.WidgetIds.Add("Widget2");
                page2.WidgetVisibility["Widget2"] = true;

                var page3 = new PageConfig("Page 3");
                page3.WidgetIds.Add("Widget1");
                page3.WidgetIds.Add("Widget2");
                page3.WidgetVisibility["Widget1"] = true;
                page3.WidgetVisibility["Widget2"] = true;

                _testPages = new List<PageConfig> { page1, page2, page3 };
            });
        }

        [TestMethod]
        public void Initialize_ShouldSetupPagesAndWidgets()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // Act
                _container.Initialize(_testPages, _testWidgets);

                // Assert
                _container.PageIndicators.Should().HaveCount(3);
                _container.PageList.Should().HaveCount(3);
                _container.CurrentPageIndex.Should().Be(0);
            });
        }

        [TestMethod]
        public void WidgetsShouldRemainInVisualTreeAcrossPageSwitches()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // Arrange
                _container.Initialize(_testPages, _testWidgets);

                // Force widget initialization by accessing the private field through reflection
                var currentPageWidgets = _container.FindName("CurrentPageWidgets") as ItemsControl;
                currentPageWidgets.Should().NotBeNull();

                // Act - Switch to different pages
                _container.CurrentPageIndex = 0; // Page 1 (Widget1 visible)
                var itemsCountAfterPage1 = currentPageWidgets!.Items.Count;
                var widget1ReferenceAfterPage1 = currentPageWidgets.Items.Cast<UIElement>().FirstOrDefault(w => w == _testUIWidget1);

                _container.CurrentPageIndex = 1; // Page 2 (Widget2 visible)
                var itemsCountAfterPage2 = currentPageWidgets.Items.Count;
                var widget1ReferenceAfterPage2 = currentPageWidgets.Items.Cast<UIElement>().FirstOrDefault(w => w == _testUIWidget1);

                _container.CurrentPageIndex = 2; // Page 3 (Both widgets visible)
                var itemsCountAfterPage3 = currentPageWidgets.Items.Count;
                var widget1ReferenceAfterPage3 = currentPageWidgets.Items.Cast<UIElement>().FirstOrDefault(w => w == _testUIWidget1);

                // Assert - Widgets should remain in visual tree, only visibility changes
                itemsCountAfterPage1.Should().BeGreaterThan(0, "Widgets should be initialized");
                itemsCountAfterPage2.Should().Be(itemsCountAfterPage1, "Widget count should remain constant");
                itemsCountAfterPage3.Should().Be(itemsCountAfterPage1, "Widget count should remain constant");

                // Assert - Same widget instances should be maintained
                widget1ReferenceAfterPage1.Should().NotBeNull("Widget1 should exist in visual tree");
                widget1ReferenceAfterPage2.Should().BeSameAs(widget1ReferenceAfterPage1, "Widget1 instance should be preserved");
                widget1ReferenceAfterPage3.Should().BeSameAs(widget1ReferenceAfterPage1, "Widget1 instance should be preserved");
            });
        }

        [TestMethod]
        public void WidgetVisibility_ShouldChangeBasedOnPageConfiguration()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // Arrange
                _container.Initialize(_testPages, _testWidgets);

                // Act & Assert - Page 1 (Widget1 visible, Widget2 hidden)
                _container.CurrentPageIndex = 0;
                _testUIWidget1.Visibility.Should().Be(Visibility.Visible, "Widget1 should be visible on Page 1");
                _testUIWidget2.Visibility.Should().Be(Visibility.Collapsed, "Widget2 should be hidden on Page 1");

                // Act & Assert - Page 2 (Widget1 hidden, Widget2 visible)
                _container.CurrentPageIndex = 1;
                _testUIWidget1.Visibility.Should().Be(Visibility.Collapsed, "Widget1 should be hidden on Page 2");
                _testUIWidget2.Visibility.Should().Be(Visibility.Visible, "Widget2 should be visible on Page 2");

                // Act & Assert - Page 3 (Both widgets visible)
                _container.CurrentPageIndex = 2;
                _testUIWidget1.Visibility.Should().Be(Visibility.Visible, "Widget1 should be visible on Page 3");
                _testUIWidget2.Visibility.Should().Be(Visibility.Visible, "Widget2 should be visible on Page 3");
            });
        }

        [TestMethod]
        public void CreateWidgetUI_ShouldOnlyBeCalledOnce()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // Arrange
                _container.Initialize(_testPages, _testWidgets);

                // Act - Switch between pages multiple times
                _container.CurrentPageIndex = 0;
                _container.CurrentPageIndex = 1;
                _container.CurrentPageIndex = 2;
                _container.CurrentPageIndex = 0;
                _container.CurrentPageIndex = 1;

                // Assert - CreateWidgetUI should only be called once per widget
                _mockWidget1.Verify(w => w.CreateWidgetUI(), Times.Once,
                    "CreateWidgetUI should only be called once for Widget1");
                _mockWidget2.Verify(w => w.CreateWidgetUI(), Times.Once,
                    "CreateWidgetUI should only be called once for Widget2");
            });
        }

        [TestMethod]
        public void TimerBasedWidgets_ShouldMaintainTimingAcrossPageSwitches()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // Arrange
                var timerWidget1 = new TimerTestWidget("TimerWidget1", TimeSpan.FromMilliseconds(100));
                var timerWidget2 = new TimerTestWidget("TimerWidget2", TimeSpan.FromMilliseconds(150));

                var mockTimerWidget1 = new Mock<IWidgetPlugin>();
                mockTimerWidget1.Setup(w => w.WidgetId).Returns("TimerWidget1");
                mockTimerWidget1.Setup(w => w.CreateWidgetUI()).Returns(timerWidget1);

                var mockTimerWidget2 = new Mock<IWidgetPlugin>();
                mockTimerWidget2.Setup(w => w.WidgetId).Returns("TimerWidget2");
                mockTimerWidget2.Setup(w => w.CreateWidgetUI()).Returns(timerWidget2);

                var timerWidgets = new List<IWidgetPlugin> { mockTimerWidget1.Object, mockTimerWidget2.Object };

                var timerPage1 = new PageConfig("Timer Page 1");
                timerPage1.WidgetIds.Add("TimerWidget1");
                timerPage1.WidgetVisibility["TimerWidget1"] = true;

                var timerPage2 = new PageConfig("Timer Page 2");
                timerPage2.WidgetIds.Add("TimerWidget2");
                timerPage2.WidgetVisibility["TimerWidget2"] = true;

                var timerPages = new List<PageConfig> { timerPage1, timerPage2 };

                _container.Initialize(timerPages, timerWidgets);

                // Act - Let timers run, then switch pages
                _container.CurrentPageIndex = 0;
                System.Threading.Thread.Sleep(250); // Let timer1 tick ~2 times

                var ticks1BeforeSwitch = timerWidget1.TickCount;
                ticks1BeforeSwitch.Should().BeGreaterThan(0, "Timer1 should have ticked");

                _container.CurrentPageIndex = 1;
                System.Threading.Thread.Sleep(300); // Let timer2 tick ~2 times

                var ticks2AfterSwitch = timerWidget2.TickCount;
                ticks2AfterSwitch.Should().BeGreaterThan(0, "Timer2 should have ticked");

                _container.CurrentPageIndex = 0;
                System.Threading.Thread.Sleep(250); // Let timer1 tick more

                var ticks1AfterSwitchBack = timerWidget1.TickCount;

                // Assert - Timer should continue running even when widget was hidden
                ticks1AfterSwitchBack.Should().BeGreaterThan(ticks1BeforeSwitch,
                    "Timer1 should have continued ticking while hidden");
            });
        }

        [TestMethod]
        public void PageNavigation_ShouldRaisePageChangedEvent()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // Arrange
                _container.Initialize(_testPages, _testWidgets);
                var pageChangedEvents = new List<int>();
                _container.PageChanged += (sender, pageIndex) => pageChangedEvents.Add(pageIndex);

                // Act
                _container.NavigateToPage(1);
                _container.NavigateToPage(2);
                _container.NavigateToPage(0);

                // Assert
                pageChangedEvents.Should().ContainInOrder(1, 2, 0);
            });
        }

        [TestCleanup]
        public void Cleanup()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _container = null!;
                _testPages = null!;
                _testWidgets = null!;
                _mockWidget1 = null!;
                _mockWidget2 = null!;
                _testUIWidget1?.Dispose();
                _testUIWidget2?.Dispose();
            });
        }
    }

    /// <summary>
    /// Test widget that implements IDisposable for cleanup
    /// </summary>
    internal class TestWidget : System.Windows.Controls.UserControl, IDisposable
    {
        public string TestId { get; }

        public TestWidget(string testId)
        {
            TestId = testId;
            Width = 100;
            Height = 50;
            Content = new TextBlock { Text = testId };
        }

        public void Dispose()
        {
            // Cleanup resources
        }
    }

    /// <summary>
    /// Test widget with timer functionality to test timing preservation
    /// </summary>
    internal class TimerTestWidget : System.Windows.Controls.UserControl, IDisposable
    {
        private readonly DispatcherTimer _timer;
        public int TickCount { get; private set; }
        public string TestId { get; }

        public TimerTestWidget(string testId, TimeSpan interval)
        {
            TestId = testId;
            Width = 100;
            Height = 50;
            Content = new TextBlock { Text = $"{testId}: {TickCount}" };

            _timer = new DispatcherTimer
            {
                Interval = interval
            };
            _timer.Tick += (s, e) =>
            {
                TickCount++;
                if (Content is TextBlock textBlock)
                {
                    textBlock.Text = $"{TestId}: {TickCount}";
                }
            };
            _timer.Start();
        }

        public void Dispose()
        {
            _timer?.Stop();
        }
    }
}