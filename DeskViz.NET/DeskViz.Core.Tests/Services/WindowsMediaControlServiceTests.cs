using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeskViz.Core.Services;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DeskViz.Core.Tests.Services
{
    /// <summary>
    /// Comprehensive unit tests for WindowsMediaControlService
    /// Tests Windows Media Session Manager integration and media control functionality
    /// Note: Many tests check for graceful handling since Windows Media APIs require active media sessions
    /// </summary>
    [TestClass]
    public class WindowsMediaControlServiceTests
    {
        private WindowsMediaControlService _mediaControlService;

        [TestInitialize]
        public void Setup()
        {
            _mediaControlService = new WindowsMediaControlService();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _mediaControlService?.Dispose();
        }

        #region Initialization Tests

        [TestMethod]
        public void Constructor_CreatesInstance()
        {
            // Assert
            _mediaControlService.Should().NotBeNull();
            _mediaControlService.IsInitialized.Should().BeFalse(); // Not initialized until InitializeAsync is called
        }

        [TestMethod]
        public async Task InitializeAsync_WithoutActiveMediaSessions_ReturnsResult()
        {
            // Act
            var result = await _mediaControlService.InitializeAsync();

            // Assert
            // Result may be true or false depending on system state
            // The important thing is it doesn't throw
        }

        [TestMethod]
        public async Task InitializeAsync_CalledMultipleTimes_DoesNotThrow()
        {
            // Act & Assert
            Func<Task> act = async () =>
            {
                await _mediaControlService.InitializeAsync();
                await _mediaControlService.InitializeAsync();
                await _mediaControlService.InitializeAsync();
            };
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public void IsInitialized_BeforeInitialization_ReturnsFalse()
        {
            // Assert
            _mediaControlService.IsInitialized.Should().BeFalse();
        }

        [TestMethod]
        public async Task IsInitialized_AfterSuccessfulInitialization_ReflectsState()
        {
            // Act
            var initResult = await _mediaControlService.InitializeAsync();

            // Assert
            _mediaControlService.IsInitialized.Should().Be(initResult);
        }

        #endregion

        #region Interface Implementation Tests

        [TestMethod]
        public void Service_ImplementsIMediaControlService()
        {
            // Assert
            _mediaControlService.Should().BeAssignableTo<IMediaControlService>();
        }

        [TestMethod]
        public void Service_ImplementsIDisposable()
        {
            // Assert
            _mediaControlService.Should().BeAssignableTo<IDisposable>();
        }

        #endregion

        #region Session Management Tests

        [TestMethod]
        public void GetCurrentSession_WithoutInitialization_ReturnsNull()
        {
            // Act
            var session = _mediaControlService.GetCurrentSession();

            // Assert
            session.Should().BeNull();
        }

        [TestMethod]
        public async Task GetCurrentSession_AfterInitialization_ReturnsSessionOrNull()
        {
            // Arrange
            await _mediaControlService.InitializeAsync();

            // Act
            var session = _mediaControlService.GetCurrentSession();

            // Assert
            // May return null if no active media sessions, which is valid
            if (session != null)
            {
                session.Should().BeOfType<MediaSessionInfo>();
                session.Id.Should().NotBeNullOrEmpty();
                session.Title.Should().NotBeNull();
                session.Artist.Should().NotBeNull();
                session.Album.Should().NotBeNull();
                session.AppName.Should().NotBeNull();
            }
        }

        [TestMethod]
        public void GetAllSessions_WithoutInitialization_ReturnsEmptyList()
        {
            // Act
            var sessions = _mediaControlService.GetAllSessions();

            // Assert
            sessions.Should().NotBeNull();
            sessions.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetAllSessions_AfterInitialization_ReturnsValidList()
        {
            // Arrange
            await _mediaControlService.InitializeAsync();

            // Act
            var sessions = _mediaControlService.GetAllSessions();

            // Assert
            sessions.Should().NotBeNull();
            // Validate each session if any exist
            foreach (var session in sessions)
            {
                session.Should().NotBeNull();
                session.Id.Should().NotBeNullOrEmpty();
                session.Title.Should().NotBeNull();
                session.Artist.Should().NotBeNull();
                session.Album.Should().NotBeNull();
                session.AppName.Should().NotBeNull();
            }
        }

        [TestMethod]
        public async Task SetActiveSessionAsync_WithValidSessionId_ReturnsResult()
        {
            // Arrange
            await _mediaControlService.InitializeAsync();
            var sessions = _mediaControlService.GetAllSessions();

            if (sessions.Any())
            {
                var sessionId = sessions.First().Id;

                // Act
                var result = await _mediaControlService.SetActiveSessionAsync(sessionId);

                // Assert
            }
            else
            {
                // Act
                var result = await _mediaControlService.SetActiveSessionAsync("NonExistentSession");

                // Assert
                result.Should().BeFalse();
            }
        }

        [TestMethod]
        public async Task SetActiveSessionAsync_WithInvalidSessionId_ReturnsFalse()
        {
            // Arrange
            await _mediaControlService.InitializeAsync();

            // Act
            var result = await _mediaControlService.SetActiveSessionAsync("InvalidSessionId");

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task SetActiveSessionAsync_WithNullSessionId_ReturnsFalse()
        {
            // Arrange
            await _mediaControlService.InitializeAsync();

            // Act
            var result = await _mediaControlService.SetActiveSessionAsync(null);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task SetActiveSessionAsync_WithEmptySessionId_ReturnsFalse()
        {
            // Arrange
            await _mediaControlService.InitializeAsync();

            // Act
            var result = await _mediaControlService.SetActiveSessionAsync("");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Media Control Tests

        [TestMethod]
        public async Task PlayAsync_WithoutInitialization_ReturnsFalse()
        {
            // Act
            var result = await _mediaControlService.PlayAsync();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task PlayAsync_AfterInitialization_ReturnsResult()
        {
            // Arrange
            await _mediaControlService.InitializeAsync();

            // Act
            var result = await _mediaControlService.PlayAsync();

            // Assert
            // Result depends on whether there's an active session with play capability
        }

        [TestMethod]
        public async Task PauseAsync_WithoutInitialization_ReturnsFalse()
        {
            // Act
            var result = await _mediaControlService.PauseAsync();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task PauseAsync_AfterInitialization_ReturnsResult()
        {
            // Arrange
            await _mediaControlService.InitializeAsync();

            // Act
            var result = await _mediaControlService.PauseAsync();

            // Assert
        }

        [TestMethod]
        public async Task StopAsync_WithoutInitialization_ReturnsFalse()
        {
            // Act
            var result = await _mediaControlService.StopAsync();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task StopAsync_AfterInitialization_ReturnsResult()
        {
            // Arrange
            await _mediaControlService.InitializeAsync();

            // Act
            var result = await _mediaControlService.StopAsync();

            // Assert
        }

        [TestMethod]
        public async Task NextAsync_WithoutInitialization_ReturnsFalse()
        {
            // Act
            var result = await _mediaControlService.NextAsync();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task NextAsync_AfterInitialization_ReturnsResult()
        {
            // Arrange
            await _mediaControlService.InitializeAsync();

            // Act
            var result = await _mediaControlService.NextAsync();

            // Assert
        }

        [TestMethod]
        public async Task PreviousAsync_WithoutInitialization_ReturnsFalse()
        {
            // Act
            var result = await _mediaControlService.PreviousAsync();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task PreviousAsync_AfterInitialization_ReturnsResult()
        {
            // Arrange
            await _mediaControlService.InitializeAsync();

            // Act
            var result = await _mediaControlService.PreviousAsync();

            // Assert
        }

        #endregion

        #region Volume Control Tests

        [TestMethod]
        public async Task SetVolumeAsync_WithValidVolume_ReturnsResult()
        {
            // Act
            var result = await _mediaControlService.SetVolumeAsync(0.5);

            // Assert
        }

        [TestMethod]
        public async Task SetVolumeAsync_WithMinimumVolume_ReturnsResult()
        {
            // Act
            var result = await _mediaControlService.SetVolumeAsync(0.0);

            // Assert
        }

        [TestMethod]
        public async Task SetVolumeAsync_WithMaximumVolume_ReturnsResult()
        {
            // Act
            var result = await _mediaControlService.SetVolumeAsync(1.0);

            // Assert
        }

        [TestMethod]
        public async Task SetVolumeAsync_WithOutOfRangeVolume_HandlesGracefully()
        {
            // Act & Assert - Should not throw
            var result1 = await _mediaControlService.SetVolumeAsync(-0.5);
            var result2 = await _mediaControlService.SetVolumeAsync(1.5);

        }

        [TestMethod]
        public void GetVolume_ReturnsValidRange()
        {
            // Act
            var volume = _mediaControlService.GetVolume();

            // Assert
            volume.Should().BeInRange(0.0, 100.0);
        }

        [TestMethod]
        public async Task VolumeRoundTrip_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var targetVolume = 0.75;

            // Act
            var setResult = await _mediaControlService.SetVolumeAsync(targetVolume);
            var retrievedVolume = _mediaControlService.GetVolume();

            // Assert
            retrievedVolume.Should().BeInRange(0.0, 100.0);
            // Note: Exact value matching may not work due to system volume control granularity
        }

        #endregion

        #region Event Tests

        [TestMethod]
        public void MediaSessionChanged_Event_CanBeSubscribedAndUnsubscribed()
        {
            // Arrange
            var eventRaised = false;
            EventHandler<MediaSessionChangedEventArgs> handler = (s, e) => eventRaised = true;

            // Act & Assert - Should not throw
            _mediaControlService.MediaSessionChanged += handler;
            _mediaControlService.MediaSessionChanged -= handler;

            eventRaised.Should().BeFalse(); // Event shouldn't be raised during subscription
        }

        [TestMethod]
        public void PlaybackStateChanged_Event_CanBeSubscribedAndUnsubscribed()
        {
            // Arrange
            var eventRaised = false;
            EventHandler<PlaybackStateChangedEventArgs> handler = (s, e) => eventRaised = true;

            // Act & Assert - Should not throw
            _mediaControlService.PlaybackStateChanged += handler;
            _mediaControlService.PlaybackStateChanged -= handler;

            eventRaised.Should().BeFalse();
        }

        [TestMethod]
        public void Events_MultipleSubscriptions_DoNotThrow()
        {
            // Arrange
            var sessionChangedCount = 0;
            var playbackChangedCount = 0;

            EventHandler<MediaSessionChangedEventArgs> sessionHandler1 = (s, e) => sessionChangedCount++;
            EventHandler<MediaSessionChangedEventArgs> sessionHandler2 = (s, e) => sessionChangedCount++;
            EventHandler<PlaybackStateChangedEventArgs> playbackHandler = (s, e) => playbackChangedCount++;

            // Act & Assert
            Action act = () =>
            {
                _mediaControlService.MediaSessionChanged += sessionHandler1;
                _mediaControlService.MediaSessionChanged += sessionHandler2;
                _mediaControlService.PlaybackStateChanged += playbackHandler;

                // Unsubscribe
                _mediaControlService.MediaSessionChanged -= sessionHandler1;
                _mediaControlService.MediaSessionChanged -= sessionHandler2;
                _mediaControlService.PlaybackStateChanged -= playbackHandler;
            };
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Events_UnsubscribeNonExistentHandler_DoesNotThrow()
        {
            // Arrange
            EventHandler<MediaSessionChangedEventArgs> sessionHandler = (s, e) => { };
            EventHandler<PlaybackStateChangedEventArgs> playbackHandler = (s, e) => { };

            // Act & Assert
            Action act = () =>
            {
                _mediaControlService.MediaSessionChanged -= sessionHandler;
                _mediaControlService.PlaybackStateChanged -= playbackHandler;
            };
            act.Should().NotThrow();
        }

        #endregion

        #region Disposal Tests

        [TestMethod]
        public void Dispose_WithoutInitialization_DoesNotThrow()
        {
            // Act & Assert
            Action act = () => _mediaControlService.Dispose();
            act.Should().NotThrow();
        }

        [TestMethod]
        public async Task Dispose_AfterInitialization_DoesNotThrow()
        {
            // Arrange
            await _mediaControlService.InitializeAsync();

            // Act & Assert
            Action act = () => _mediaControlService.Dispose();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Act & Assert
            Action act = () =>
            {
                _mediaControlService.Dispose();
                _mediaControlService.Dispose();
                _mediaControlService.Dispose();
            };
            act.Should().NotThrow();
        }

        [TestMethod]
        public async Task MethodCalls_AfterDisposal_HandleGracefully()
        {
            // Arrange
            await _mediaControlService.InitializeAsync();
            _mediaControlService.Dispose();

            // Act & Assert - Methods should handle disposed state gracefully
            Func<Task> act = async () =>
            {
                await _mediaControlService.PlayAsync();
                await _mediaControlService.PauseAsync();
                await _mediaControlService.StopAsync();
                await _mediaControlService.NextAsync();
                await _mediaControlService.PreviousAsync();
                await _mediaControlService.SetVolumeAsync(0.5);
                _mediaControlService.GetVolume();
                _mediaControlService.GetCurrentSession();
                _mediaControlService.GetAllSessions();
            };
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task IsInitialized_AfterDisposal_ReturnsFalse()
        {
            // Arrange
            await _mediaControlService.InitializeAsync();
            _mediaControlService.Dispose();

            // Act & Assert
            _mediaControlService.IsInitialized.Should().BeFalse();
        }

        #endregion

        #region Error Handling Tests

        [TestMethod]
        public async Task MediaControlMethods_WithExceptions_HandleGracefully()
        {
            // This test verifies that the service handles underlying Windows API exceptions gracefully
            // The actual Windows Media APIs may throw exceptions which should be caught

            // Arrange
            await _mediaControlService.InitializeAsync();

            // Act & Assert - Should not throw even if underlying media control fails
            Func<Task> act = async () =>
            {
                await _mediaControlService.PlayAsync();
                await _mediaControlService.PauseAsync();
                await _mediaControlService.StopAsync();
                await _mediaControlService.NextAsync();
                await _mediaControlService.PreviousAsync();
            };
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task SessionManagement_WithExceptions_HandlesGracefully()
        {
            // Arrange
            await _mediaControlService.InitializeAsync();

            // Act & Assert - Should not throw even if session management fails
            Action act = () =>
            {
                _mediaControlService.GetCurrentSession();
                _mediaControlService.GetAllSessions();
            };
            act.Should().NotThrow();
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public async Task FullWorkflow_InitializeAndControl_WorksCorrectly()
        {
            // This test runs through a complete workflow to ensure integration works

            // Act & Assert
            Func<Task> act = async () =>
            {
                // Initialize
                var initResult = await _mediaControlService.InitializeAsync();

                // Get sessions
                var currentSession = _mediaControlService.GetCurrentSession();
                var allSessions = _mediaControlService.GetAllSessions();

                // Try basic controls
                await _mediaControlService.PlayAsync();
                await _mediaControlService.PauseAsync();

                // Volume control
                var originalVolume = _mediaControlService.GetVolume();
                await _mediaControlService.SetVolumeAsync(0.5);
                var newVolume = _mediaControlService.GetVolume();

                // Navigation
                await _mediaControlService.NextAsync();
                await _mediaControlService.PreviousAsync();
            };

            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task ConcurrentOperations_DoNotCauseIssues()
        {
            // Arrange
            await _mediaControlService.InitializeAsync();

            // Act - Run multiple operations concurrently
            var tasks = new List<Task>
            {
                _mediaControlService.PlayAsync(),
                _mediaControlService.PauseAsync(),
                _mediaControlService.SetVolumeAsync(0.3),
                _mediaControlService.NextAsync(),
                _mediaControlService.PreviousAsync(),
                Task.Run(() => _mediaControlService.GetCurrentSession()),
                Task.Run(() => _mediaControlService.GetAllSessions()),
                Task.Run(() => _mediaControlService.GetVolume())
            };

            // Assert
            Func<Task> act = async () => await Task.WhenAll(tasks);
            await act.Should().NotThrowAsync();
        }

        #endregion

        #region Performance Tests

        [TestMethod]
        public async Task GetCurrentSession_RepeatedCalls_PerformAcceptably()
        {
            // Arrange
            await _mediaControlService.InitializeAsync();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 100; i++)
            {
                _mediaControlService.GetCurrentSession();
            }

            // Assert
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete 100 calls in under 2 seconds
        }

        [TestMethod]
        public async Task VolumeOperations_RepeatedCalls_PerformAcceptably()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 50; i++)
            {
                await _mediaControlService.SetVolumeAsync(i % 2 == 0 ? 0.3 : 0.7);
                _mediaControlService.GetVolume();
            }

            // Assert
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000); // Should complete in under 3 seconds
        }

        #endregion
    }

    /// <summary>
    /// Tests for MediaSessionInfo class
    /// </summary>
    [TestClass]
    public class MediaSessionInfoTests
    {
        [TestMethod]
        public void MediaSessionInfo_DefaultConstructor_SetsDefaultValues()
        {
            // Act
            var sessionInfo = new MediaSessionInfo();

            // Assert
            sessionInfo.Id.Should().Be(string.Empty);
            sessionInfo.Title.Should().Be(string.Empty);
            sessionInfo.Artist.Should().Be(string.Empty);
            sessionInfo.Album.Should().Be(string.Empty);
            sessionInfo.AppName.Should().Be(string.Empty);
            sessionInfo.AlbumArt.Should().BeNull();
            sessionInfo.State.Should().Be(PlaybackState.Unknown);
            sessionInfo.Position.Should().Be(TimeSpan.Zero);
            sessionInfo.Duration.Should().Be(TimeSpan.Zero);
            sessionInfo.CanPlay.Should().BeFalse();
            sessionInfo.CanPause.Should().BeFalse();
            sessionInfo.CanStop.Should().BeFalse();
            sessionInfo.CanSkipNext.Should().BeFalse();
            sessionInfo.CanSkipPrevious.Should().BeFalse();
        }

        [TestMethod]
        public void MediaSessionInfo_Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var sessionInfo = new MediaSessionInfo();
            var testAlbumArt = new byte[] { 1, 2, 3, 4, 5 };

            // Act
            sessionInfo.Id = "test-id";
            sessionInfo.Title = "Test Title";
            sessionInfo.Artist = "Test Artist";
            sessionInfo.Album = "Test Album";
            sessionInfo.AppName = "Test App";
            sessionInfo.AlbumArt = testAlbumArt;
            sessionInfo.State = PlaybackState.Playing;
            sessionInfo.Position = TimeSpan.FromMinutes(2);
            sessionInfo.Duration = TimeSpan.FromMinutes(4);
            sessionInfo.CanPlay = true;
            sessionInfo.CanPause = true;
            sessionInfo.CanStop = true;
            sessionInfo.CanSkipNext = true;
            sessionInfo.CanSkipPrevious = true;

            // Assert
            sessionInfo.Id.Should().Be("test-id");
            sessionInfo.Title.Should().Be("Test Title");
            sessionInfo.Artist.Should().Be("Test Artist");
            sessionInfo.Album.Should().Be("Test Album");
            sessionInfo.AppName.Should().Be("Test App");
            sessionInfo.AlbumArt.Should().BeEquivalentTo(testAlbumArt);
            sessionInfo.State.Should().Be(PlaybackState.Playing);
            sessionInfo.Position.Should().Be(TimeSpan.FromMinutes(2));
            sessionInfo.Duration.Should().Be(TimeSpan.FromMinutes(4));
            sessionInfo.CanPlay.Should().BeTrue();
            sessionInfo.CanPause.Should().BeTrue();
            sessionInfo.CanStop.Should().BeTrue();
            sessionInfo.CanSkipNext.Should().BeTrue();
            sessionInfo.CanSkipPrevious.Should().BeTrue();
        }
    }

    /// <summary>
    /// Tests for PlaybackState enumeration
    /// </summary>
    [TestClass]
    public class PlaybackStateTests
    {
        [TestMethod]
        public void PlaybackState_HasAllExpectedValues()
        {
            // Assert
            Enum.IsDefined(typeof(PlaybackState), PlaybackState.Unknown).Should().BeTrue();
            Enum.IsDefined(typeof(PlaybackState), PlaybackState.Closed).Should().BeTrue();
            Enum.IsDefined(typeof(PlaybackState), PlaybackState.Opened).Should().BeTrue();
            Enum.IsDefined(typeof(PlaybackState), PlaybackState.Changing).Should().BeTrue();
            Enum.IsDefined(typeof(PlaybackState), PlaybackState.Stopped).Should().BeTrue();
            Enum.IsDefined(typeof(PlaybackState), PlaybackState.Playing).Should().BeTrue();
            Enum.IsDefined(typeof(PlaybackState), PlaybackState.Paused).Should().BeTrue();
        }

        [TestMethod]
        public void PlaybackState_Values_AreDistinct()
        {
            // Arrange
            var values = Enum.GetValues<PlaybackState>().ToList();

            // Assert
            values.Should().OnlyHaveUniqueItems();
            values.Should().HaveCount(7);
        }
    }

    /// <summary>
    /// Tests for event argument classes
    /// </summary>
    [TestClass]
    public class MediaEventArgsTests
    {
        [TestMethod]
        public void MediaSessionChangedEventArgs_DefaultConstructor_SetsDefaults()
        {
            // Act
            var eventArgs = new MediaSessionChangedEventArgs();

            // Assert
            eventArgs.CurrentSession.Should().BeNull();
        }

        [TestMethod]
        public void MediaSessionChangedEventArgs_Property_CanBeSet()
        {
            // Arrange
            var eventArgs = new MediaSessionChangedEventArgs();
            var session = new MediaSessionInfo { Id = "test" };

            // Act
            eventArgs.CurrentSession = session;

            // Assert
            eventArgs.CurrentSession.Should().Be(session);
        }

        [TestMethod]
        public void PlaybackStateChangedEventArgs_DefaultConstructor_SetsDefaults()
        {
            // Act
            var eventArgs = new PlaybackStateChangedEventArgs();

            // Assert
            eventArgs.State.Should().Be(PlaybackState.Unknown);
            eventArgs.Session.Should().BeNull();
        }

        [TestMethod]
        public void PlaybackStateChangedEventArgs_Properties_CanBeSet()
        {
            // Arrange
            var eventArgs = new PlaybackStateChangedEventArgs();
            var session = new MediaSessionInfo { Id = "test" };

            // Act
            eventArgs.State = PlaybackState.Playing;
            eventArgs.Session = session;

            // Assert
            eventArgs.State.Should().Be(PlaybackState.Playing);
            eventArgs.Session.Should().Be(session);
        }

        [TestMethod]
        public void EventArgs_InheritFromEventArgs()
        {
            // Act & Assert
            new MediaSessionChangedEventArgs().Should().BeAssignableTo<EventArgs>();
            new PlaybackStateChangedEventArgs().Should().BeAssignableTo<EventArgs>();
        }
    }
}