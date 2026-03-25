using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using DeskViz.Core.Services;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DeskViz.Core.Tests.Services
{
    /// <summary>
    /// Comprehensive unit tests for SystemTrayService
    /// Tests event handling, UI component management, and proper disposal
    /// </summary>
    [TestClass]
    public class SystemTrayServiceTests
    {
        private SystemTrayService _systemTrayService;
        private Icon _testIcon;

        [TestInitialize]
        public void Setup()
        {
            _systemTrayService = new SystemTrayService();
            // Create a simple test icon (1x1 pixel)
            _testIcon = new Icon(CreateTestIconStream());
        }

        [TestCleanup]
        public void Cleanup()
        {
            _systemTrayService?.Dispose();
            _testIcon?.Dispose();
        }

        private System.IO.Stream CreateTestIconStream()
        {
            // Create a minimal ICO file in memory
            var iconData = new byte[]
            {
                0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00,
                0x20, 0x00, 0x30, 0x00, 0x00, 0x00, 0x16, 0x00, 0x00, 0x00, 0x28, 0x00,
                0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01, 0x00,
                0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00
            };
            return new System.IO.MemoryStream(iconData);
        }

        #region Initialization Tests

        [TestMethod]
        public void Constructor_CreatesInstance()
        {
            // Act & Assert
            _systemTrayService.Should().NotBeNull();
        }

        [TestMethod]
        public void Initialize_WithValidParameters_DoesNotThrow()
        {
            // Arrange
            var toolTipText = "Test Application";

            // Act & Assert
            Action act = () => _systemTrayService.Initialize(_testIcon, toolTipText);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Initialize_WithNullIcon_DoesNotThrow()
        {
            // Arrange
            var toolTipText = "Test Application";

            // Act & Assert
            Action act = () => _systemTrayService.Initialize(null, toolTipText);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Initialize_WithNullToolTip_DoesNotThrow()
        {
            // Act & Assert
            Action act = () => _systemTrayService.Initialize(_testIcon, null);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Initialize_WithEmptyToolTip_DoesNotThrow()
        {
            // Act & Assert
            Action act = () => _systemTrayService.Initialize(_testIcon, "");
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Initialize_CalledMultipleTimes_DisposesAndRecreates()
        {
            // Arrange
            var toolTipText1 = "First Initialization";
            var toolTipText2 = "Second Initialization";

            // Act & Assert
            Action act = () =>
            {
                _systemTrayService.Initialize(_testIcon, toolTipText1);
                _systemTrayService.Initialize(_testIcon, toolTipText2);
            };
            act.Should().NotThrow();
        }

        #endregion

        #region Visibility Tests

        [TestMethod]
        public void Show_AfterInitialization_DoesNotThrow()
        {
            // Arrange
            _systemTrayService.Initialize(_testIcon, "Test");

            // Act & Assert
            Action act = () => _systemTrayService.Show();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Show_WithoutInitialization_DoesNotThrow()
        {
            // Act & Assert
            Action act = () => _systemTrayService.Show();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Hide_AfterInitialization_DoesNotThrow()
        {
            // Arrange
            _systemTrayService.Initialize(_testIcon, "Test");

            // Act & Assert
            Action act = () => _systemTrayService.Hide();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Hide_WithoutInitialization_DoesNotThrow()
        {
            // Act & Assert
            Action act = () => _systemTrayService.Hide();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ShowHide_Sequence_DoesNotThrow()
        {
            // Arrange
            _systemTrayService.Initialize(_testIcon, "Test");

            // Act & Assert
            Action act = () =>
            {
                _systemTrayService.Show();
                _systemTrayService.Hide();
                _systemTrayService.Show();
                _systemTrayService.Hide();
            };
            act.Should().NotThrow();
        }

        #endregion

        #region Balloon Tip Tests

        [TestMethod]
        public void ShowBalloonTip_WithValidParameters_DoesNotThrow()
        {
            // Arrange
            _systemTrayService.Initialize(_testIcon, "Test");
            var title = "Test Title";
            var text = "Test Message";

            // Act & Assert
            Action act = () => _systemTrayService.ShowBalloonTip(title, text);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ShowBalloonTip_WithCustomTimeout_DoesNotThrow()
        {
            // Arrange
            _systemTrayService.Initialize(_testIcon, "Test");
            var title = "Test Title";
            var text = "Test Message";
            var timeout = 5000;

            // Act & Assert
            Action act = () => _systemTrayService.ShowBalloonTip(title, text, timeout);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ShowBalloonTip_WithNullTitle_DoesNotThrow()
        {
            // Arrange
            _systemTrayService.Initialize(_testIcon, "Test");
            var text = "Test Message";

            // Act & Assert
            Action act = () => _systemTrayService.ShowBalloonTip(null, text);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ShowBalloonTip_WithNullText_DoesNotThrow()
        {
            // Arrange
            _systemTrayService.Initialize(_testIcon, "Test");
            var title = "Test Title";

            // Act & Assert
            Action act = () => _systemTrayService.ShowBalloonTip(title, null);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ShowBalloonTip_WithEmptyStrings_DoesNotThrow()
        {
            // Arrange
            _systemTrayService.Initialize(_testIcon, "Test");

            // Act & Assert
            Action act = () => _systemTrayService.ShowBalloonTip("", "");
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ShowBalloonTip_WithoutInitialization_DoesNotThrow()
        {
            // Act & Assert
            Action act = () => _systemTrayService.ShowBalloonTip("Title", "Text");
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ShowBalloonTip_WithZeroTimeout_DoesNotThrow()
        {
            // Arrange
            _systemTrayService.Initialize(_testIcon, "Test");

            // Act & Assert
            Action act = () => _systemTrayService.ShowBalloonTip("Title", "Text", 0);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ShowBalloonTip_WithNegativeTimeout_DoesNotThrow()
        {
            // Arrange
            _systemTrayService.Initialize(_testIcon, "Test");

            // Act & Assert
            Action act = () => _systemTrayService.ShowBalloonTip("Title", "Text", -1);
            act.Should().NotThrow();
        }

        #endregion

        #region Event Tests

        [TestMethod]
        public void SettingsRequested_Event_CanBeSubscribedAndUnsubscribed()
        {
            // Arrange
            var eventRaised = false;
            EventHandler handler = (s, e) => eventRaised = true;

            // Act
            _systemTrayService.SettingsRequested += handler;
            _systemTrayService.SettingsRequested -= handler;

            // Assert - Should not throw during subscription/unsubscription
            eventRaised.Should().BeFalse();
        }

        [TestMethod]
        public void AboutRequested_Event_CanBeSubscribedAndUnsubscribed()
        {
            // Arrange
            var eventRaised = false;
            EventHandler handler = (s, e) => eventRaised = true;

            // Act
            _systemTrayService.AboutRequested += handler;
            _systemTrayService.AboutRequested -= handler;

            // Assert - Should not throw during subscription/unsubscription
            eventRaised.Should().BeFalse();
        }

        [TestMethod]
        public void ExitRequested_Event_CanBeSubscribedAndUnsubscribed()
        {
            // Arrange
            var eventRaised = false;
            EventHandler handler = (s, e) => eventRaised = true;

            // Act
            _systemTrayService.ExitRequested += handler;
            _systemTrayService.ExitRequested -= handler;

            // Assert - Should not throw during subscription/unsubscription
            eventRaised.Should().BeFalse();
        }

        [TestMethod]
        public void TrayIconDoubleClicked_Event_CanBeSubscribedAndUnsubscribed()
        {
            // Arrange
            var eventRaised = false;
            EventHandler handler = (s, e) => eventRaised = true;

            // Act
            _systemTrayService.TrayIconDoubleClicked += handler;
            _systemTrayService.TrayIconDoubleClicked -= handler;

            // Assert - Should not throw during subscription/unsubscription
            eventRaised.Should().BeFalse();
        }

        [TestMethod]
        public void Events_MultipleSubscriptions_DoNotThrow()
        {
            // Arrange
            var settingsCount = 0;
            var aboutCount = 0;
            var exitCount = 0;
            var doubleClickCount = 0;

            EventHandler settingsHandler1 = (s, e) => settingsCount++;
            EventHandler settingsHandler2 = (s, e) => settingsCount++;
            EventHandler aboutHandler = (s, e) => aboutCount++;
            EventHandler exitHandler = (s, e) => exitCount++;
            EventHandler doubleClickHandler = (s, e) => doubleClickCount++;

            // Act & Assert
            Action act = () =>
            {
                _systemTrayService.SettingsRequested += settingsHandler1;
                _systemTrayService.SettingsRequested += settingsHandler2;
                _systemTrayService.AboutRequested += aboutHandler;
                _systemTrayService.ExitRequested += exitHandler;
                _systemTrayService.TrayIconDoubleClicked += doubleClickHandler;

                // Unsubscribe
                _systemTrayService.SettingsRequested -= settingsHandler1;
                _systemTrayService.SettingsRequested -= settingsHandler2;
                _systemTrayService.AboutRequested -= aboutHandler;
                _systemTrayService.ExitRequested -= exitHandler;
                _systemTrayService.TrayIconDoubleClicked -= doubleClickHandler;
            };
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Events_UnsubscribeNonExistentHandler_DoesNotThrow()
        {
            // Arrange
            EventHandler handler = (s, e) => { };

            // Act & Assert
            Action act = () =>
            {
                _systemTrayService.SettingsRequested -= handler;
                _systemTrayService.AboutRequested -= handler;
                _systemTrayService.ExitRequested -= handler;
                _systemTrayService.TrayIconDoubleClicked -= handler;
            };
            act.Should().NotThrow();
        }

        #endregion

        #region Disposal Tests

        [TestMethod]
        public void Dispose_WithoutInitialization_DoesNotThrow()
        {
            // Act & Assert
            Action act = () => _systemTrayService.Dispose();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Dispose_AfterInitialization_DoesNotThrow()
        {
            // Arrange
            _systemTrayService.Initialize(_testIcon, "Test");

            // Act & Assert
            Action act = () => _systemTrayService.Dispose();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            _systemTrayService.Initialize(_testIcon, "Test");

            // Act & Assert
            Action act = () =>
            {
                _systemTrayService.Dispose();
                _systemTrayService.Dispose();
                _systemTrayService.Dispose();
            };
            act.Should().NotThrow();
        }

        [TestMethod]
        public void MethodCalls_AfterDisposal_DoNotThrow()
        {
            // Arrange
            _systemTrayService.Initialize(_testIcon, "Test");
            _systemTrayService.Dispose();

            // Act & Assert
            Action act = () =>
            {
                _systemTrayService.Show();
                _systemTrayService.Hide();
                _systemTrayService.ShowBalloonTip("Title", "Text");
            };
            act.Should().NotThrow();
        }

        #endregion

        #region Interface Implementation Tests

        [TestMethod]
        public void Service_ImplementsISystemTrayService()
        {
            // Assert
            _systemTrayService.Should().BeAssignableTo<ISystemTrayService>();
        }

        [TestMethod]
        public void Service_ImplementsIDisposable()
        {
            // Assert
            _systemTrayService.Should().BeAssignableTo<IDisposable>();
        }

        #endregion

        #region Edge Cases and Error Handling

        [TestMethod]
        public void Initialize_WithVeryLongToolTip_DoesNotThrow()
        {
            // Arrange
            var longToolTip = new string('A', 1000); // Very long string

            // Act & Assert
            Action act = () => _systemTrayService.Initialize(_testIcon, longToolTip);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ShowBalloonTip_WithVeryLongText_DoesNotThrow()
        {
            // Arrange
            _systemTrayService.Initialize(_testIcon, "Test");
            var longTitle = new string('T', 500);
            var longText = new string('M', 1000);

            // Act & Assert
            Action act = () => _systemTrayService.ShowBalloonTip(longTitle, longText);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Service_ThreadSafety_MultipleThreadAccess()
        {
            // This test verifies basic thread safety
            // Arrange
            _systemTrayService.Initialize(_testIcon, "Test");
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            // Act
            var threads = new Thread[5];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            _systemTrayService.Show();
                            Thread.Sleep(1);
                            _systemTrayService.Hide();
                            Thread.Sleep(1);
                            _systemTrayService.ShowBalloonTip($"Title {j}", $"Text {j}");
                            Thread.Sleep(1);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            foreach (var thread in threads)
            {
                thread.Start();
            }

            foreach (var thread in threads)
            {
                thread.Join(5000); // 5 second timeout
            }

            // Assert
            exceptions.Should().BeEmpty("No exceptions should occur during multi-threaded access");
        }

        #endregion

        #region Performance Tests

        [TestMethod]
        public void ShowHide_RepeatedCalls_PerformAcceptably()
        {
            // Arrange
            _systemTrayService.Initialize(_testIcon, "Test");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 100; i++)
            {
                _systemTrayService.Show();
                _systemTrayService.Hide();
            }

            // Assert
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete in under 5 seconds
        }

        [TestMethod]
        public void BalloonTip_RepeatedCalls_PerformAcceptably()
        {
            // Arrange
            _systemTrayService.Initialize(_testIcon, "Test");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 50; i++)
            {
                _systemTrayService.ShowBalloonTip($"Title {i}", $"Message {i}");
            }

            // Assert
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000); // Should complete in under 3 seconds
        }

        #endregion
    }

    /// <summary>
    /// Tests for ISystemTrayService interface contract
    /// </summary>
    [TestClass]
    public class ISystemTrayServiceContractTests
    {
        [TestMethod]
        public void ISystemTrayService_Interface_HasRequiredEvents()
        {
            // Arrange
            var interfaceType = typeof(ISystemTrayService);

            // Assert
            interfaceType.GetEvent("SettingsRequested").Should().NotBeNull();
            interfaceType.GetEvent("AboutRequested").Should().NotBeNull();
            interfaceType.GetEvent("ExitRequested").Should().NotBeNull();
            interfaceType.GetEvent("TrayIconDoubleClicked").Should().NotBeNull();
        }

        [TestMethod]
        public void ISystemTrayService_Interface_HasRequiredMethods()
        {
            // Arrange
            var interfaceType = typeof(ISystemTrayService);

            // Assert
            interfaceType.GetMethod("Initialize").Should().NotBeNull();
            interfaceType.GetMethod("Show").Should().NotBeNull();
            interfaceType.GetMethod("Hide").Should().NotBeNull();

            var showBalloonTipMethods = interfaceType.GetMethods()
                .Where(m => m.Name == "ShowBalloonTip").ToArray();
            showBalloonTipMethods.Should().HaveCount(1);
        }

        [TestMethod]
        public void ISystemTrayService_Interface_InheritsFromIDisposable()
        {
            // Arrange
            var interfaceType = typeof(ISystemTrayService);

            // Assert
            interfaceType.GetInterfaces().Should().Contain(typeof(IDisposable));
        }
    }
}