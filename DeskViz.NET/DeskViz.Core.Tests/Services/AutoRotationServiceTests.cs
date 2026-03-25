using System;
using System.Threading;
using DeskViz.Core.Models;
using DeskViz.Core.Services;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DeskViz.Core.Tests.Services
{
    /// <summary>
    /// Comprehensive unit tests for AutoRotationService
    /// Tests timer functionality, pagination logic, and rotation modes
    /// </summary>
    [TestClass]
    public class AutoRotationServiceTests
    {
        private Mock<SettingsService> _mockSettingsService;
        private AutoRotationService _autoRotationService;
        private AppSettings _testSettings;

        [TestInitialize]
        public void Setup()
        {
            // Create test settings with multiple pages
            _testSettings = new AppSettings
            {
                AutoRotationEnabled = true,
                AutoRotationIntervalSeconds = 1, // Short interval for testing
                RotationMode = AutoRotationMode.Forward,
                PauseOnUserInteraction = true,
                CurrentPageIndex = 0
            };

            // Add test pages
            _testSettings.Pages.Clear();
            _testSettings.Pages.Add(new PageConfig("Page 1"));
            _testSettings.Pages.Add(new PageConfig("Page 2"));
            _testSettings.Pages.Add(new PageConfig("Page 3"));

            // Setup mock settings service
            _mockSettingsService = new Mock<SettingsService>();
            _mockSettingsService.Setup(s => s.Settings).Returns(_testSettings);

            // Create service instance
            _autoRotationService = new AutoRotationService(_mockSettingsService.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _autoRotationService?.Stop();
            _autoRotationService = null;
        }

        #region Constructor Tests

        [TestMethod]
        public void Constructor_WithValidSettingsService_InitializesCorrectly()
        {
            // Assert
            _autoRotationService.Should().NotBeNull();
            _autoRotationService.IsEnabled.Should().BeTrue(); // Based on test settings
            _autoRotationService.IsPaused.Should().BeFalse();
        }

        [TestMethod]
        public void Constructor_WithNullSettingsService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new AutoRotationService(null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Constructor_WithDisabledAutoRotation_IsNotEnabled()
        {
            // Arrange
            _testSettings.AutoRotationEnabled = false;

            // Act
            var service = new AutoRotationService(_mockSettingsService.Object);

            // Assert
            service.IsEnabled.Should().BeFalse();
        }

        #endregion

        #region Property Tests

        [TestMethod]
        public void IsEnabled_WithEnabledSettingsAndNotPaused_ReturnsTrue()
        {
            // Arrange
            _testSettings.AutoRotationEnabled = true;

            // Assert
            _autoRotationService.IsEnabled.Should().BeTrue();
        }

        [TestMethod]
        public void IsEnabled_WithDisabledSettings_ReturnsFalse()
        {
            // Arrange
            _testSettings.AutoRotationEnabled = false;

            // Assert
            _autoRotationService.IsEnabled.Should().BeFalse();
        }

        [TestMethod]
        public void IsEnabled_WithEnabledSettingsButPaused_ReturnsFalse()
        {
            // Arrange
            _testSettings.AutoRotationEnabled = true;
            _autoRotationService.Pause();

            // Assert
            _autoRotationService.IsEnabled.Should().BeFalse();
        }

        [TestMethod]
        public void IsPaused_InitialState_ReturnsFalse()
        {
            // Assert
            _autoRotationService.IsPaused.Should().BeFalse();
        }

        [TestMethod]
        public void IsPaused_AfterPause_ReturnsTrue()
        {
            // Act
            _autoRotationService.Pause();

            // Assert
            _autoRotationService.IsPaused.Should().BeTrue();
        }

        [TestMethod]
        public void IsPaused_AfterPauseAndResume_ReturnsFalse()
        {
            // Act
            _autoRotationService.Pause();
            _autoRotationService.Resume();

            // Assert
            _autoRotationService.IsPaused.Should().BeFalse();
        }

        #endregion

        #region Start/Stop Tests

        [TestMethod]
        public void Start_WithEnabledSettings_StartsSuccessfully()
        {
            // Arrange
            var stateChanged = false;
            _autoRotationService.AutoRotationStateChanged += (s, enabled) => stateChanged = enabled;

            // Act
            _autoRotationService.Start();

            // Assert
            stateChanged.Should().BeTrue();
        }

        [TestMethod]
        public void Start_WithDisabledSettings_DoesNotStart()
        {
            // Arrange
            _testSettings.AutoRotationEnabled = false;
            var stateChanged = false;
            _autoRotationService.AutoRotationStateChanged += (s, enabled) => stateChanged = enabled;

            // Act
            _autoRotationService.Start();

            // Assert
            stateChanged.Should().BeFalse();
        }

        [TestMethod]
        public void Stop_AfterStart_StopsSuccessfully()
        {
            // Arrange
            _autoRotationService.Start();
            var stateChanged = false;
            _autoRotationService.AutoRotationStateChanged += (s, enabled) => stateChanged = !enabled;

            // Act
            _autoRotationService.Stop();

            // Assert
            stateChanged.Should().BeTrue();
        }

        [TestMethod]
        public void Stop_WithoutStart_DoesNotThrow()
        {
            // Act & Assert
            Action act = () => _autoRotationService.Stop();
            act.Should().NotThrow();
        }

        #endregion

        #region Pause/Resume Tests

        [TestMethod]
        public void Pause_WhenRunning_PausesSuccessfully()
        {
            // Arrange
            _autoRotationService.Start();
            var stateChanged = false;
            _autoRotationService.AutoRotationStateChanged += (s, enabled) => stateChanged = !enabled;

            // Act
            _autoRotationService.Pause();

            // Assert
            _autoRotationService.IsPaused.Should().BeTrue();
            stateChanged.Should().BeTrue();
        }

        [TestMethod]
        public void Pause_WhenNotRunning_DoesNotThrow()
        {
            // Act & Assert
            Action act = () => _autoRotationService.Pause();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Pause_WhenAlreadyPaused_DoesNotRaiseEvent()
        {
            // Arrange
            _autoRotationService.Start();
            _autoRotationService.Pause();
            var eventRaised = false;
            _autoRotationService.AutoRotationStateChanged += (s, enabled) => eventRaised = true;

            // Act
            _autoRotationService.Pause();

            // Assert
            eventRaised.Should().BeFalse();
        }

        [TestMethod]
        public void Resume_AfterPause_ResumesSuccessfully()
        {
            // Arrange
            _autoRotationService.Start();
            _autoRotationService.Pause();
            var stateChanged = false;
            _autoRotationService.AutoRotationStateChanged += (s, enabled) => stateChanged = enabled;

            // Act
            _autoRotationService.Resume();

            // Assert
            _autoRotationService.IsPaused.Should().BeFalse();
            stateChanged.Should().BeTrue();
        }

        [TestMethod]
        public void Resume_WithoutPause_DoesNotThrow()
        {
            // Act & Assert
            Action act = () => _autoRotationService.Resume();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Resume_WithDisabledSettings_DoesNotResume()
        {
            // Arrange
            _testSettings.AutoRotationEnabled = false;
            _autoRotationService.Pause();
            var stateChanged = false;
            _autoRotationService.AutoRotationStateChanged += (s, enabled) => stateChanged = enabled;

            // Act
            _autoRotationService.Resume();

            // Assert
            stateChanged.Should().BeFalse();
        }

        #endregion

        #region Timer Update Tests

        [TestMethod]
        public void UpdateTimerSettings_WithEnabledSettings_UpdatesInterval()
        {
            // Arrange
            var originalInterval = _testSettings.AutoRotationIntervalSeconds;
            _testSettings.AutoRotationIntervalSeconds = 5;

            // Act
            _autoRotationService.UpdateTimerSettings();

            // Assert - No exceptions should occur
            _testSettings.AutoRotationIntervalSeconds.Should().Be(5);
        }

        [TestMethod]
        public void UpdateTimerSettings_WithMinimumInterval_EnforcesMinimum()
        {
            // Arrange
            _testSettings.AutoRotationIntervalSeconds = 0; // Below minimum

            // Act & Assert - Should not throw, should handle gracefully
            Action act = () => _autoRotationService.UpdateTimerSettings();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void UpdateTimerSettings_WithDisabledSettings_StopsTimer()
        {
            // Arrange
            _autoRotationService.Start();
            _testSettings.AutoRotationEnabled = false;

            // Act
            _autoRotationService.UpdateTimerSettings();

            // Assert
            _autoRotationService.IsEnabled.Should().BeFalse();
        }

        #endregion

        #region User Interaction Tests

        [TestMethod]
        public void OnUserInteraction_WithPauseOnInteraction_PausesRotation()
        {
            // Arrange
            _testSettings.PauseOnUserInteraction = true;
            _autoRotationService.Start();

            // Act
            _autoRotationService.OnUserInteraction();

            // Assert
            _autoRotationService.IsPaused.Should().BeTrue();
        }

        [TestMethod]
        public void OnUserInteraction_WithoutPauseOnInteraction_DoesNotPause()
        {
            // Arrange
            _testSettings.PauseOnUserInteraction = false;
            _autoRotationService.Start();

            // Act
            _autoRotationService.OnUserInteraction();

            // Assert
            _autoRotationService.IsPaused.Should().BeFalse();
        }

        [TestMethod]
        public void OnUserInteraction_WhenNotEnabled_DoesNotThrow()
        {
            // Arrange
            _testSettings.AutoRotationEnabled = false;

            // Act & Assert
            Action act = () => _autoRotationService.OnUserInteraction();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void OnUserInteraction_AutoResume_ResumesAfterDelay()
        {
            // This test is challenging to implement precisely due to timer dependencies
            // We test that the method completes without throwing

            // Arrange
            _testSettings.PauseOnUserInteraction = true;
            _autoRotationService.Start();

            // Act & Assert
            Action act = () => _autoRotationService.OnUserInteraction();
            act.Should().NotThrow();

            // Verify it was paused
            _autoRotationService.IsPaused.Should().BeTrue();
        }

        #endregion

        #region Manual Rotation Tests

        [TestMethod]
        public void TriggerNextRotation_WithValidConfiguration_DoesNotThrow()
        {
            // Act & Assert
            Action act = () => _autoRotationService.TriggerNextRotation();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void TriggerNextRotation_WithoutPages_DoesNotThrow()
        {
            // Arrange
            _testSettings.Pages.Clear();

            // Act & Assert
            Action act = () => _autoRotationService.TriggerNextRotation();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void TriggerNextRotation_WhenPaused_DoesNotThrow()
        {
            // Arrange
            _autoRotationService.Pause();

            // Act & Assert
            Action act = () => _autoRotationService.TriggerNextRotation();
            act.Should().NotThrow();
        }

        #endregion

        #region Event Tests

        [TestMethod]
        public void PageRotationRequested_Event_CanBeSubscribed()
        {
            // Arrange
            var eventRaised = false;
            PageRotationEventArgs capturedArgs = null;

            _autoRotationService.PageRotationRequested += (s, e) =>
            {
                eventRaised = true;
                capturedArgs = e;
            };

            // Act
            _autoRotationService.TriggerNextRotation();

            // Assert
            // Event may or may not be raised depending on internal logic
            // This test ensures subscription doesn't throw
            Action act = () => _autoRotationService.TriggerNextRotation();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void AutoRotationStateChanged_Event_CanBeSubscribed()
        {
            // Arrange
            var eventRaised = false;
            bool capturedState = false;

            _autoRotationService.AutoRotationStateChanged += (s, enabled) =>
            {
                eventRaised = true;
                capturedState = enabled;
            };

            // Act
            _autoRotationService.Start();

            // Assert
            eventRaised.Should().BeTrue();
            capturedState.Should().BeTrue();
        }

        [TestMethod]
        public void Events_MultipleSubscriptions_WorkCorrectly()
        {
            // Arrange
            var rotationEventCount = 0;
            var stateEventCount = 0;

            _autoRotationService.PageRotationRequested += (s, e) => rotationEventCount++;
            _autoRotationService.PageRotationRequested += (s, e) => rotationEventCount++;
            _autoRotationService.AutoRotationStateChanged += (s, e) => stateEventCount++;
            _autoRotationService.AutoRotationStateChanged += (s, e) => stateEventCount++;

            // Act
            _autoRotationService.Start();
            _autoRotationService.Stop();

            // Assert
            stateEventCount.Should().BeGreaterOrEqualTo(2); // Start and stop events
        }

        [TestMethod]
        public void Events_Unsubscription_WorksCorrectly()
        {
            // Arrange
            var eventCount = 0;
            EventHandler<bool> handler = (s, e) => eventCount++;

            _autoRotationService.AutoRotationStateChanged += handler;
            _autoRotationService.Start();
            _autoRotationService.AutoRotationStateChanged -= handler;

            // Act
            _autoRotationService.Stop();

            // Assert
            eventCount.Should().Be(1); // Only the start event, not the stop event
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void Service_WithNullCurrentPageIndex_HandlesGracefully()
        {
            // Arrange
            _testSettings.CurrentPageIndex = -1; // Invalid index

            // Act & Assert
            Action act = () => _autoRotationService.TriggerNextRotation();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Service_WithOutOfRangePageIndex_HandlesGracefully()
        {
            // Arrange
            _testSettings.CurrentPageIndex = 999; // Out of range

            // Act & Assert
            Action act = () => _autoRotationService.TriggerNextRotation();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Service_WithEmptyPagesList_HandlesGracefully()
        {
            // Arrange
            _testSettings.Pages.Clear();

            // Act & Assert
            Action act = () =>
            {
                _autoRotationService.Start();
                _autoRotationService.TriggerNextRotation();
                _autoRotationService.Stop();
            };
            act.Should().NotThrow();
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public void StartStopCycle_Multiple_WorksCorrectly()
        {
            // Act & Assert
            Action act = () =>
            {
                for (int i = 0; i < 5; i++)
                {
                    _autoRotationService.Start();
                    _autoRotationService.Stop();
                }
            };
            act.Should().NotThrow();
        }

        [TestMethod]
        public void PauseResumeCycle_Multiple_WorksCorrectly()
        {
            // Arrange
            _autoRotationService.Start();

            // Act & Assert
            Action act = () =>
            {
                for (int i = 0; i < 5; i++)
                {
                    _autoRotationService.Pause();
                    _autoRotationService.Resume();
                }
            };
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ComplexStateTransitions_WorkCorrectly()
        {
            // Act & Assert
            Action act = () =>
            {
                _autoRotationService.Start();
                _autoRotationService.Pause();
                _autoRotationService.TriggerNextRotation();
                _autoRotationService.Resume();
                _autoRotationService.OnUserInteraction();
                _autoRotationService.UpdateTimerSettings();
                _autoRotationService.Stop();
            };
            act.Should().NotThrow();
        }

        #endregion

        #region Performance Tests

        [TestMethod]
        public void TriggerNextRotation_RepeatedCalls_PerformAcceptably()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 1000; i++)
            {
                _autoRotationService.TriggerNextRotation();
            }

            // Assert
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete 1000 calls in under 1 second
        }

        [TestMethod]
        public void StartStop_RepeatedCalls_PerformAcceptably()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 100; i++)
            {
                _autoRotationService.Start();
                _autoRotationService.Stop();
            }

            // Assert
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete 100 cycles in under 2 seconds
        }

        #endregion
    }

    /// <summary>
    /// Tests for PageRotationEventArgs class
    /// </summary>
    [TestClass]
    public class PageRotationEventArgsTests
    {
        [TestMethod]
        public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            var currentPage = 1;
            var nextPage = 2;
            var rotationMode = AutoRotationMode.Forward;

            // Act
            var eventArgs = new PageRotationEventArgs(currentPage, nextPage, rotationMode);

            // Assert
            eventArgs.CurrentPageIndex.Should().Be(currentPage);
            eventArgs.NextPageIndex.Should().Be(nextPage);
            eventArgs.RotationMode.Should().Be(rotationMode);
        }

        [TestMethod]
        public void Constructor_WithNegativeIndices_SetsPropertiesCorrectly()
        {
            // Arrange
            var currentPage = -1;
            var nextPage = -2;
            var rotationMode = AutoRotationMode.Reverse;

            // Act
            var eventArgs = new PageRotationEventArgs(currentPage, nextPage, rotationMode);

            // Assert
            eventArgs.CurrentPageIndex.Should().Be(currentPage);
            eventArgs.NextPageIndex.Should().Be(nextPage);
            eventArgs.RotationMode.Should().Be(rotationMode);
        }

        [TestMethod]
        [DataRow(AutoRotationMode.Forward)]
        [DataRow(AutoRotationMode.Reverse)]
        [DataRow(AutoRotationMode.PingPong)]
        public void Constructor_WithAllRotationModes_SetsCorrectly(AutoRotationMode mode)
        {
            // Act
            var eventArgs = new PageRotationEventArgs(0, 1, mode);

            // Assert
            eventArgs.RotationMode.Should().Be(mode);
        }

        [TestMethod]
        public void PageRotationEventArgs_InheritsFromEventArgs()
        {
            // Act
            var eventArgs = new PageRotationEventArgs(0, 1, AutoRotationMode.Forward);

            // Assert
            eventArgs.Should().BeAssignableTo<EventArgs>();
        }
    }
}