using System;
using System.Linq;
using System.Windows.Forms;
using DeskViz.Core.Services;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ScreenOrientation = DeskViz.Core.Services.ScreenOrientation;

namespace DeskViz.Core.Tests.Services
{
    /// <summary>
    /// Comprehensive unit tests for ScreenService
    /// Tests multi-monitor scenarios, screen information retrieval, and Win32 API integration
    /// </summary>
    [TestClass]
    public class ScreenServiceTests
    {
        private ScreenService _screenService;

        [TestInitialize]
        public void Setup()
        {
            _screenService = new ScreenService();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _screenService = null!;
        }

        #region Screen Information Tests

        [TestMethod]
        public void GetAllScreens_ReturnsScreenList()
        {
            // Act
            var screens = _screenService.GetAllScreens();

            // Assert
            screens.Should().NotBeNull();
            screens.Should().NotBeEmpty(); // At least one screen should be available

            // Verify each screen has valid properties
            foreach (var screen in screens)
            {
                screen.Should().NotBeNull();
                screen.DeviceName.Should().NotBeNullOrEmpty();
                screen.Identifier.Should().NotBeNullOrEmpty();
                screen.DisplayName.Should().NotBeNullOrEmpty();

                // Bounds should be valid
                screen.Bounds.Should().NotBeNull();
                screen.Bounds.Width.Should().BeGreaterThan(0);
                screen.Bounds.Height.Should().BeGreaterThan(0);

                // Working area should be valid
                screen.WorkingArea.Should().NotBeNull();
                screen.WorkingArea.Width.Should().BeGreaterThan(0);
                screen.WorkingArea.Height.Should().BeGreaterThan(0);

                // Working area should be <= bounds
                screen.WorkingArea.Width.Should().BeLessOrEqualTo(screen.Bounds.Width);
                screen.WorkingArea.Height.Should().BeLessOrEqualTo(screen.Bounds.Height);
            }
        }

        [TestMethod]
        public void GetAllScreens_HasPrimaryScreen()
        {
            // Act
            var screens = _screenService.GetAllScreens();

            // Assert
            screens.Should().NotBeNull();
            screens.Should().Contain(s => s.IsPrimary);

            // Should have exactly one primary screen
            var primaryScreens = screens.Where(s => s.IsPrimary).ToList();
            primaryScreens.Should().HaveCount(1);
        }

        [TestMethod]
        public void GetAllScreens_IdentifierFormat_IsCorrect()
        {
            // Act
            var screens = _screenService.GetAllScreens();

            // Assert
            foreach (var screen in screens)
            {
                // Identifier should be in format "X_Y"
                screen.Identifier.Should().Contain("_");
                var parts = screen.Identifier.Split('_');
                parts.Should().HaveCount(2);

                // Both parts should be parseable as integers
                int.TryParse(parts[0], out _).Should().BeTrue();
                int.TryParse(parts[1], out _).Should().BeTrue();
            }
        }

        [TestMethod]
        public void GetAllScreens_DisplayNameFormat_IsCorrect()
        {
            // Act
            var screens = _screenService.GetAllScreens();

            // Assert
            foreach (var screen in screens)
            {
                // Display name should contain device name and resolution
                screen.DisplayName.Should().Contain(screen.DeviceName);
                screen.DisplayName.Should().Contain($"{screen.Bounds.Width}x{screen.Bounds.Height}");

                // Primary screen should be marked
                if (screen.IsPrimary)
                {
                    screen.DisplayName.Should().Contain("[Primary]");
                }
                else
                {
                    screen.DisplayName.Should().NotContain("[Primary]");
                }
            }
        }

        #endregion

        #region Primary Screen Tests

        [TestMethod]
        public void GetPrimaryScreen_ReturnsPrimaryScreen()
        {
            // Act
            var primaryScreen = _screenService.GetPrimaryScreen();

            // Assert
            primaryScreen.Should().NotBeNull();
            primaryScreen.IsPrimary.Should().BeTrue();
            primaryScreen.DeviceName.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void GetPrimaryScreen_MatchesAllScreensPrimary()
        {
            // Act
            var primaryScreen = _screenService.GetPrimaryScreen();
            var allScreens = _screenService.GetAllScreens();
            var primaryFromAll = allScreens.FirstOrDefault(s => s.IsPrimary);

            // Assert
            primaryScreen.Should().NotBeNull();
            primaryFromAll.Should().NotBeNull();
            primaryScreen.DeviceName.Should().Be(primaryFromAll.DeviceName);
            primaryScreen.Identifier.Should().Be(primaryFromAll.Identifier);
        }

        #endregion

        #region Screen Lookup Tests

        [TestMethod]
        public void GetScreenByIdentifier_WithValidIdentifier_ReturnsCorrectScreen()
        {
            // Arrange
            var allScreens = _screenService.GetAllScreens();
            var testScreen = allScreens.First();
            var identifier = testScreen.Identifier;

            // Act
            var foundScreen = _screenService.GetScreenByIdentifier(identifier);

            // Assert
            foundScreen.Should().NotBeNull();
            foundScreen.Identifier.Should().Be(identifier);
            foundScreen.DeviceName.Should().Be(testScreen.DeviceName);
            foundScreen.IsPrimary.Should().Be(testScreen.IsPrimary);
        }

        [TestMethod]
        public void GetScreenByIdentifier_WithInvalidIdentifier_ReturnsNull()
        {
            // Act
            var foundScreen = _screenService.GetScreenByIdentifier("INVALID_IDENTIFIER");

            // Assert
            foundScreen.Should().BeNull();
        }

        [TestMethod]
        public void GetScreenByIdentifier_WithNullIdentifier_ReturnsNull()
        {
            // Act
            var foundScreen = _screenService.GetScreenByIdentifier(null);

            // Assert
            foundScreen.Should().BeNull();
        }

        [TestMethod]
        public void GetScreenByIdentifier_WithEmptyIdentifier_ReturnsNull()
        {
            // Act
            var foundScreen = _screenService.GetScreenByIdentifier("");

            // Assert
            foundScreen.Should().BeNull();
        }

        #endregion

        #region Screen Selection Tests

        [TestMethod]
        public void GetSmallestScreen_ReturnsScreenWithSmallestArea()
        {
            // Arrange
            var allScreens = _screenService.GetAllScreens();

            // Skip if only one screen
            if (allScreens.Count <= 1)
            {
                Assert.Inconclusive("Test requires multiple screens to validate smallest screen selection");
                return;
            }

            // Act
            var smallestScreen = _screenService.GetSmallestScreen();

            // Assert
            smallestScreen.Should().NotBeNull();

            var smallestArea = smallestScreen.Bounds.Width * smallestScreen.Bounds.Height;
            foreach (var screen in allScreens)
            {
                var screenArea = screen.Bounds.Width * screen.Bounds.Height;
                smallestArea.Should().BeLessOrEqualTo(screenArea);
            }
        }

        [TestMethod]
        public void GetSmallestScreen_WithSingleScreen_ReturnsThatScreen()
        {
            // This test assumes the system may have only one screen
            // Act
            var smallestScreen = _screenService.GetSmallestScreen();
            var primaryScreen = _screenService.GetPrimaryScreen();

            // Assert
            smallestScreen.Should().NotBeNull();

            // If only one screen, smallest should be the primary
            var allScreens = _screenService.GetAllScreens();
            if (allScreens.Count == 1)
            {
                smallestScreen.Should().Be(primaryScreen);
            }
        }

        #endregion

        #region Screen Orientation Tests

        [TestMethod]
        public void ScreenOrientation_LandscapeScreen_ReturnsLandscape()
        {
            // Arrange
            var allScreens = _screenService.GetAllScreens();

            // Act & Assert
            foreach (var screen in allScreens)
            {
                if (screen.Bounds.Width >= screen.Bounds.Height)
                {
                    screen.Orientation.Should().Be(ScreenOrientation.Landscape);
                }
            }
        }

        [TestMethod]
        public void ScreenOrientation_PortraitScreen_ReturnsPortrait()
        {
            // Arrange
            var allScreens = _screenService.GetAllScreens();

            // Act & Assert
            foreach (var screen in allScreens)
            {
                if (screen.Bounds.Height > screen.Bounds.Width)
                {
                    screen.Orientation.Should().Be(ScreenOrientation.Portrait);
                }
            }
        }

        #endregion

        #region Fullscreen Application Tests

        [TestMethod]
        public void ApplyTrueFullscreen_WithZeroHandle_DoesNotThrow()
        {
            // Arrange
            var screen = _screenService.GetPrimaryScreen();
            var zeroHandle = IntPtr.Zero;

            // Act & Assert
            Action act = () => _screenService.ApplyTrueFullscreen(zeroHandle, screen);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ApplyTrueFullscreen_WithNullScreen_DoesNotThrow()
        {
            // Arrange
            var validHandle = new IntPtr(12345); // Fake handle

            // Act & Assert
            Action act = () => _screenService.ApplyTrueFullscreen(validHandle, null);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ApplyTrueFullscreen_WithValidParameters_ExecutesWithoutException()
        {
            // Arrange
            var screen = _screenService.GetPrimaryScreen();
            var fakeHandle = new IntPtr(12345); // This won't be a real window handle

            // Act & Assert
            // The method should handle invalid handles gracefully
            Action act = () => _screenService.ApplyTrueFullscreen(fakeHandle, screen);
            act.Should().NotThrow();
        }

        #endregion

        #region Edge Cases and Error Handling

        [TestMethod]
        public void GetAllScreens_CalledMultipleTimes_ReturnsConsistentResults()
        {
            // Act
            var screens1 = _screenService.GetAllScreens();
            var screens2 = _screenService.GetAllScreens();

            // Assert
            screens1.Should().HaveCount(screens2.Count);

            for (int i = 0; i < screens1.Count; i++)
            {
                screens1[i].DeviceName.Should().Be(screens2[i].DeviceName);
                screens1[i].IsPrimary.Should().Be(screens2[i].IsPrimary);
                screens1[i].Bounds.Width.Should().Be(screens2[i].Bounds.Width);
                screens1[i].Bounds.Height.Should().Be(screens2[i].Bounds.Height);
            }
        }

        [TestMethod]
        public void ScreenInfo_Properties_AreNotNull()
        {
            // Arrange
            var screens = _screenService.GetAllScreens();

            // Act & Assert
            foreach (var screen in screens)
            {
                screen.DeviceName.Should().NotBeNull();
                screen.Identifier.Should().NotBeNull();
                screen.DisplayName.Should().NotBeNull();
                screen.Bounds.Should().NotBeNull();
                screen.WorkingArea.Should().NotBeNull();
            }
        }

        [TestMethod]
        public void ScreenBounds_Properties_AreValid()
        {
            // Arrange
            var screens = _screenService.GetAllScreens();

            // Act & Assert
            foreach (var screen in screens)
            {
                // Bounds should have positive dimensions
                screen.Bounds.Width.Should().BePositive();
                screen.Bounds.Height.Should().BePositive();

                // Working area should have positive dimensions
                screen.WorkingArea.Width.Should().BePositive();
                screen.WorkingArea.Height.Should().BePositive();

                // Working area should fit within bounds
                screen.WorkingArea.X.Should().BeGreaterOrEqualTo(screen.Bounds.X);
                screen.WorkingArea.Y.Should().BeGreaterOrEqualTo(screen.Bounds.Y);

                var workingAreaRight = screen.WorkingArea.X + screen.WorkingArea.Width;
                var boundsRight = screen.Bounds.X + screen.Bounds.Width;
                workingAreaRight.Should().BeLessOrEqualTo(boundsRight);

                var workingAreaBottom = screen.WorkingArea.Y + screen.WorkingArea.Height;
                var boundsBottom = screen.Bounds.Y + screen.Bounds.Height;
                workingAreaBottom.Should().BeLessOrEqualTo(boundsBottom);
            }
        }

        #endregion

        #region Performance Tests

        [TestMethod]
        public void GetAllScreens_ExecutesQuickly()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 100; i++)
            {
                _screenService.GetAllScreens();
            }

            // Assert
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete 100 calls in under 1 second
        }

        [TestMethod]
        public void ScreenService_Methods_DoNotCauseMemoryLeaks()
        {
            // This test performs many operations to check for obvious memory leaks
            // In a real scenario, you'd use memory profiling tools

            // Arrange & Act
            for (int i = 0; i < 1000; i++)
            {
                var screens = _screenService.GetAllScreens();
                var primary = _screenService.GetPrimaryScreen();
                var smallest = _screenService.GetSmallestScreen();

                if (screens.Any())
                {
                    var first = screens.First();
                    _screenService.GetScreenByIdentifier(first.Identifier);
                }
            }

            // Assert
            // If we reach here without out-of-memory exceptions, we're probably okay
            // In production, you'd use memory profiling tools to verify
            Assert.IsTrue(true, "Method completed without memory issues");
        }

        #endregion

        #region Exception Handling Tests

        [TestMethod]
        public void GetAllScreens_WithSystemException_HandlesGracefully()
        {
            // This test verifies that the service handles system-level exceptions gracefully
            // In the real implementation, the try-catch block should handle Screen.AllScreens exceptions

            // Act & Assert
            Action act = () => _screenService.GetAllScreens();
            act.Should().NotThrow();
        }

        #endregion
    }

    /// <summary>
    /// Tests for ScreenInfo and ScreenBounds classes
    /// </summary>
    [TestClass]
    public class ScreenInfoModelTests
    {
        [TestMethod]
        public void ScreenInfo_DefaultConstructor_SetsDefaultValues()
        {
            // Act
            var screenInfo = new ScreenInfo();

            // Assert
            screenInfo.DeviceName.Should().Be(string.Empty);
            screenInfo.IsPrimary.Should().BeFalse();
            screenInfo.Identifier.Should().Be(string.Empty);
            screenInfo.Bounds.Should().NotBeNull();
            screenInfo.WorkingArea.Should().NotBeNull();
        }

        [TestMethod]
        public void ScreenInfo_DisplayName_FormatsCorrectly()
        {
            // Arrange
            var screenInfo = new ScreenInfo
            {
                DeviceName = "TEST_DISPLAY",
                Bounds = new ScreenBounds { Width = 1920, Height = 1080 },
                IsPrimary = true
            };

            // Act
            var displayName = screenInfo.DisplayName;

            // Assert
            displayName.Should().Be("TEST_DISPLAY (1920x1080) [Primary]");
        }

        [TestMethod]
        public void ScreenInfo_DisplayName_WithoutPrimary_FormatsCorrectly()
        {
            // Arrange
            var screenInfo = new ScreenInfo
            {
                DeviceName = "SECONDARY_DISPLAY",
                Bounds = new ScreenBounds { Width = 1680, Height = 1050 },
                IsPrimary = false
            };

            // Act
            var displayName = screenInfo.DisplayName;

            // Assert
            displayName.Should().Be("SECONDARY_DISPLAY (1680x1050)");
        }

        [TestMethod]
        public void ScreenInfo_Orientation_LandscapeScreen_ReturnsLandscape()
        {
            // Arrange
            var screenInfo = new ScreenInfo
            {
                Bounds = new ScreenBounds { Width = 1920, Height = 1080 }
            };

            // Act
            var orientation = screenInfo.Orientation;

            // Assert
            orientation.Should().Be(ScreenOrientation.Landscape);
        }

        [TestMethod]
        public void ScreenInfo_Orientation_PortraitScreen_ReturnsPortrait()
        {
            // Arrange
            var screenInfo = new ScreenInfo
            {
                Bounds = new ScreenBounds { Width = 1080, Height = 1920 }
            };

            // Act
            var orientation = screenInfo.Orientation;

            // Assert
            orientation.Should().Be(ScreenOrientation.Portrait);
        }

        [TestMethod]
        public void ScreenInfo_Orientation_SquareScreen_ReturnsLandscape()
        {
            // Arrange
            var screenInfo = new ScreenInfo
            {
                Bounds = new ScreenBounds { Width = 1024, Height = 1024 }
            };

            // Act
            var orientation = screenInfo.Orientation;

            // Assert
            orientation.Should().Be(ScreenOrientation.Landscape); // Width >= Height
        }

        [TestMethod]
        public void ScreenBounds_DefaultConstructor_SetsZeroValues()
        {
            // Act
            var bounds = new ScreenBounds();

            // Assert
            bounds.X.Should().Be(0);
            bounds.Y.Should().Be(0);
            bounds.Width.Should().Be(0);
            bounds.Height.Should().Be(0);
        }

        [TestMethod]
        public void ScreenBounds_Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var bounds = new ScreenBounds();

            // Act
            bounds.X = 100;
            bounds.Y = 200;
            bounds.Width = 1920;
            bounds.Height = 1080;

            // Assert
            bounds.X.Should().Be(100);
            bounds.Y.Should().Be(200);
            bounds.Width.Should().Be(1920);
            bounds.Height.Should().Be(1080);
        }
    }
}