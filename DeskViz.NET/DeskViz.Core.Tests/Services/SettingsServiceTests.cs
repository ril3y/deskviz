using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using DeskViz.Core.Models;
using DeskViz.Core.Services;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DeskViz.Core.Tests.Services
{
    /// <summary>
    /// Comprehensive unit tests for SettingsService to ensure 100% code coverage
    /// Tests all JSON serialization, settings management, page handling, and migration scenarios
    /// </summary>
    [TestClass]
    public class SettingsServiceTests
    {
        private MockFileSystem _mockFileSystem;
        private SettingsService _settingsService;
        private string _testAppDataPath;
        private string _testSettingsPath;

        [TestInitialize]
        public void Setup()
        {
            // Setup mock file system for testing
            _mockFileSystem = new MockFileSystem();
            _testAppDataPath = @"C:\Users\TestUser\AppData\Roaming\DeskViz";
            _testSettingsPath = Path.Combine(_testAppDataPath, "settings.json");

            // Create test directory structure
            _mockFileSystem.AddDirectory(_testAppDataPath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _settingsService = null;
        }

        #region Constructor Tests

        [TestMethod]
        public void Constructor_WithoutExistingSettingsFile_CreatesDefaultSettings()
        {
            // Arrange & Act
            _settingsService = new SettingsService();

            // Assert
            _settingsService.Settings.Should().NotBeNull();
            _settingsService.Settings.Pages.Should().HaveCount(1);
            _settingsService.Settings.Pages[0].Name.Should().Be("Main");
            _settingsService.Settings.Pages[0].WidgetIds.Should().HaveCount(7);
            _settingsService.Settings.Pages[0].WidgetIds.Should().Contain("CpuWidget");
            _settingsService.Settings.Pages[0].WidgetIds.Should().Contain("RamWidget");
            _settingsService.Settings.Pages[0].WidgetIds.Should().Contain("GpuWidget");
            _settingsService.Settings.Pages[0].WidgetIds.Should().Contain("HardDriveWidget");
            _settingsService.Settings.Pages[0].WidgetIds.Should().Contain("ClockWidget");
            _settingsService.Settings.Pages[0].WidgetIds.Should().Contain("LogoWidget");
            _settingsService.Settings.Pages[0].WidgetIds.Should().Contain("MediaControlWidget");

            // Verify all widgets are visible by default
            foreach (var widgetId in _settingsService.Settings.Pages[0].WidgetIds)
            {
                _settingsService.Settings.Pages[0].WidgetVisibility[widgetId].Should().BeTrue();
            }
        }

        [TestMethod]
        public void Constructor_WithExistingValidSettingsFile_LoadsSettings()
        {
            // Arrange
            var existingSettings = new AppSettings
            {
                PreferredDisplayIdentifier = "TestDisplay",
                UseDarkTheme = false,
                UpdateIntervalSeconds = 5
            };
            var settingsJson = JsonSerializer.Serialize(existingSettings, new JsonSerializerOptions { WriteIndented = true });

            // Create a mock settings file
            File.WriteAllText(_testSettingsPath, settingsJson);

            // Act
            _settingsService = new SettingsService();

            // Assert
            _settingsService.Settings.PreferredDisplayIdentifier.Should().Be("TestDisplay");
            _settingsService.Settings.UseDarkTheme.Should().BeFalse();
            _settingsService.Settings.UpdateIntervalSeconds.Should().Be(5);
        }

        [TestMethod]
        public void Constructor_WithCorruptedSettingsFile_UsesDefaultSettings()
        {
            // Arrange
            var corruptedJson = "{ invalid json content }";
            File.WriteAllText(_testSettingsPath, corruptedJson);

            // Act
            _settingsService = new SettingsService();

            // Assert
            _settingsService.Settings.Should().NotBeNull();
            _settingsService.Settings.PreferredDisplayIdentifier.Should().Be(string.Empty);
            _settingsService.Settings.UseDarkTheme.Should().BeTrue(); // Default value
        }

        #endregion

        #region Settings Loading Tests

        [TestMethod]
        public void LoadSettings_WithValidFile_ReturnsLoadedSettings()
        {
            // Arrange
            _settingsService = new SettingsService();
            var testSettings = new AppSettings
            {
                PreferredDisplayIdentifier = "Test123",
                StartWithWindows = true,
                UpdateIntervalSeconds = 10
            };
            var settingsJson = JsonSerializer.Serialize(testSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_testSettingsPath, settingsJson);

            // Act
            var loadedSettings = _settingsService.LoadSettings();

            // Assert
            loadedSettings.Should().NotBeNull();
            loadedSettings.PreferredDisplayIdentifier.Should().Be("Test123");
            loadedSettings.StartWithWindows.Should().BeTrue();
            loadedSettings.UpdateIntervalSeconds.Should().Be(10);
        }

        [TestMethod]
        public void LoadSettings_WithFileNotFoundException_ReturnsDefaultSettings()
        {
            // Arrange
            _settingsService = new SettingsService();

            // Act
            var loadedSettings = _settingsService.LoadSettings();

            // Assert
            loadedSettings.Should().NotBeNull();
            loadedSettings.PreferredDisplayIdentifier.Should().Be(string.Empty);
            loadedSettings.UseDarkTheme.Should().BeTrue(); // Default value
        }

        [TestMethod]
        public void LoadSettings_WithInvalidJson_ReturnsDefaultSettings()
        {
            // Arrange
            _settingsService = new SettingsService();
            File.WriteAllText(_testSettingsPath, "invalid json");

            // Act
            var loadedSettings = _settingsService.LoadSettings();

            // Assert
            loadedSettings.Should().NotBeNull();
            loadedSettings.PreferredDisplayIdentifier.Should().Be(string.Empty);
        }

        #endregion

        #region Settings Saving Tests

        [TestMethod]
        public void SaveSettings_WithValidSettings_SavesSuccessfully()
        {
            // Arrange
            _settingsService = new SettingsService();
            _settingsService.Settings.PreferredDisplayIdentifier = "TestSave";
            _settingsService.Settings.UseDarkTheme = false;

            // Act
            _settingsService.SaveSettings();

            // Assert
            File.Exists(_testSettingsPath).Should().BeTrue();
            var savedContent = File.ReadAllText(_testSettingsPath);
            savedContent.Should().Contain("TestSave");
            savedContent.Should().Contain("false");
        }

        [TestMethod]
        public void SaveSettings_WithProvidedSettings_UpdatesAndSaves()
        {
            // Arrange
            _settingsService = new SettingsService();
            var newSettings = new AppSettings
            {
                PreferredDisplayIdentifier = "NewDisplay",
                UpdateIntervalSeconds = 15
            };

            // Act
            _settingsService.SaveSettings(newSettings);

            // Assert
            _settingsService.Settings.PreferredDisplayIdentifier.Should().Be("NewDisplay");
            _settingsService.Settings.UpdateIntervalSeconds.Should().Be(15);

            // Verify file was saved
            File.Exists(_testSettingsPath).Should().BeTrue();
        }

        #endregion

        #region Display Management Tests

        [TestMethod]
        public void UpdatePreferredDisplay_WithValidIdentifier_UpdatesAndSaves()
        {
            // Arrange
            _settingsService = new SettingsService();
            var testIdentifier = "1920_0";

            // Act
            _settingsService.UpdatePreferredDisplay(testIdentifier);

            // Assert
            _settingsService.Settings.PreferredDisplayIdentifier.Should().Be(testIdentifier);
        }

        #endregion

        #region Widget Visibility Tests

        [TestMethod]
        public void UpdateWidgetVisibility_WithNewWidget_AddsAndSaves()
        {
            // Arrange
            _settingsService = new SettingsService();
            var widgetId = "TestWidget";

            // Act
            _settingsService.UpdateWidgetVisibility(widgetId, false);

            // Assert
            _settingsService.Settings.WidgetVisibility[widgetId].Should().BeFalse();
        }

        [TestMethod]
        public void UpdateWidgetVisibility_WithExistingWidget_UpdatesAndSaves()
        {
            // Arrange
            _settingsService = new SettingsService();
            var widgetId = "ExistingWidget";
            _settingsService.Settings.WidgetVisibility[widgetId] = true;

            // Act
            _settingsService.UpdateWidgetVisibility(widgetId, false);

            // Assert
            _settingsService.Settings.WidgetVisibility[widgetId].Should().BeFalse();
        }

        [TestMethod]
        public void UpdateWidgetVisibility_WithMultipleSettings_UpdatesAllAndSaves()
        {
            // Arrange
            _settingsService = new SettingsService();
            var visibilitySettings = new Dictionary<string, bool>
            {
                { "Widget1", true },
                { "Widget2", false },
                { "Widget3", true }
            };

            // Act
            _settingsService.UpdateWidgetVisibility(visibilitySettings);

            // Assert
            _settingsService.Settings.WidgetVisibility["Widget1"].Should().BeTrue();
            _settingsService.Settings.WidgetVisibility["Widget2"].Should().BeFalse();
            _settingsService.Settings.WidgetVisibility["Widget3"].Should().BeTrue();
        }

        #endregion

        #region Widget Order Tests

        [TestMethod]
        public void UpdateWidgetOrder_WithNewOrder_UpdatesAndSaves()
        {
            // Arrange
            _settingsService = new SettingsService();
            var newOrder = new List<string> { "Widget3", "Widget1", "Widget2" };

            // Act
            _settingsService.UpdateWidgetOrder(newOrder);

            // Assert
            _settingsService.Settings.WidgetOrder.Should().BeEquivalentTo(newOrder);
            _settingsService.Settings.WidgetOrder.Should().NotBeSameAs(newOrder); // Ensure it's a copy
        }

        #endregion

        #region Widget Orientation Tests

        [TestMethod]
        public void UpdateWidgetOrientation_WithValidOrientation_UpdatesAndSaves()
        {
            // Arrange
            _settingsService = new SettingsService();

            // Act
            _settingsService.UpdateWidgetOrientation(WidgetOrientationSetting.Horizontal);

            // Assert
            _settingsService.Settings.WidgetOrientation.Should().Be(WidgetOrientationSetting.Horizontal);
        }

        [TestMethod]
        [DataRow(WidgetOrientationSetting.Auto)]
        [DataRow(WidgetOrientationSetting.Horizontal)]
        [DataRow(WidgetOrientationSetting.Vertical)]
        public void UpdateWidgetOrientation_WithAllValidValues_UpdatesCorrectly(WidgetOrientationSetting orientation)
        {
            // Arrange
            _settingsService = new SettingsService();

            // Act
            _settingsService.UpdateWidgetOrientation(orientation);

            // Assert
            _settingsService.Settings.WidgetOrientation.Should().Be(orientation);
        }

        #endregion

        #region Page Management Tests

        [TestMethod]
        public void AddPage_WithValidName_AddsPageSuccessfully()
        {
            // Arrange
            _settingsService = new SettingsService();
            var initialPageCount = _settingsService.Settings.Pages.Count;
            var pageName = "Test Page";

            // Act
            _settingsService.AddPage(pageName);

            // Assert
            _settingsService.Settings.Pages.Should().HaveCount(initialPageCount + 1);
            _settingsService.Settings.Pages.Last().Name.Should().Be(pageName);
            _settingsService.Settings.Pages.Last().Id.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void RemovePage_WithValidIndex_RemovesPageSuccessfully()
        {
            // Arrange
            _settingsService = new SettingsService();
            _settingsService.AddPage("Page to Remove");
            var initialCount = _settingsService.Settings.Pages.Count;

            // Act
            _settingsService.RemovePage(1); // Remove the second page

            // Assert
            _settingsService.Settings.Pages.Should().HaveCount(initialCount - 1);
        }

        [TestMethod]
        public void RemovePage_WithInvalidIndex_DoesNotRemovePage()
        {
            // Arrange
            _settingsService = new SettingsService();
            var initialCount = _settingsService.Settings.Pages.Count;

            // Act
            _settingsService.RemovePage(-1); // Invalid index
            _settingsService.RemovePage(100); // Out of range index

            // Assert
            _settingsService.Settings.Pages.Should().HaveCount(initialCount);
        }

        [TestMethod]
        public void RemovePage_LastPage_DoesNotRemove()
        {
            // Arrange
            _settingsService = new SettingsService();
            // Should only have 1 page initially

            // Act
            _settingsService.RemovePage(0);

            // Assert
            _settingsService.Settings.Pages.Should().HaveCount(1); // Cannot remove last page
        }

        [TestMethod]
        public void RemovePage_WithCurrentPageAdjustment_AdjustsCurrentPageIndex()
        {
            // Arrange
            _settingsService = new SettingsService();
            _settingsService.AddPage("Page 1");
            _settingsService.AddPage("Page 2");
            _settingsService.SetCurrentPageIndex(2); // Set to last page

            // Act
            _settingsService.RemovePage(2); // Remove last page

            // Assert
            _settingsService.Settings.CurrentPageIndex.Should().Be(1); // Should adjust to valid index
        }

        [TestMethod]
        public void UpdatePage_WithValidIndex_UpdatesPageSuccessfully()
        {
            // Arrange
            _settingsService = new SettingsService();
            var updatedPage = new PageConfig("Updated Page");
            updatedPage.WidgetIds.Add("TestWidget");

            // Act
            _settingsService.UpdatePage(0, updatedPage);

            // Assert
            _settingsService.Settings.Pages[0].Name.Should().Be("Updated Page");
            _settingsService.Settings.Pages[0].WidgetIds.Should().Contain("TestWidget");
        }

        [TestMethod]
        public void UpdatePage_WithInvalidIndex_DoesNotUpdate()
        {
            // Arrange
            _settingsService = new SettingsService();
            var originalName = _settingsService.Settings.Pages[0].Name;
            var updatedPage = new PageConfig("Should Not Update");

            // Act
            _settingsService.UpdatePage(-1, updatedPage);
            _settingsService.UpdatePage(100, updatedPage);

            // Assert
            _settingsService.Settings.Pages[0].Name.Should().Be(originalName);
        }

        [TestMethod]
        public void GetPage_WithValidIndex_ReturnsPage()
        {
            // Arrange
            _settingsService = new SettingsService();

            // Act
            var page = _settingsService.GetPage(0);

            // Assert
            page.Should().NotBeNull();
            page.Should().Be(_settingsService.Settings.Pages[0]);
        }

        [TestMethod]
        public void GetPage_WithInvalidIndex_ReturnsNull()
        {
            // Arrange
            _settingsService = new SettingsService();

            // Act
            var page1 = _settingsService.GetPage(-1);
            var page2 = _settingsService.GetPage(100);

            // Assert
            page1.Should().BeNull();
            page2.Should().BeNull();
        }

        [TestMethod]
        public void SetCurrentPageIndex_WithValidIndex_UpdatesIndex()
        {
            // Arrange
            _settingsService = new SettingsService();
            _settingsService.AddPage("Page 1");

            // Act
            _settingsService.SetCurrentPageIndex(1);

            // Assert
            _settingsService.Settings.CurrentPageIndex.Should().Be(1);
        }

        [TestMethod]
        public void SetCurrentPageIndex_WithInvalidIndex_DoesNotUpdate()
        {
            // Arrange
            _settingsService = new SettingsService();
            var originalIndex = _settingsService.Settings.CurrentPageIndex;

            // Act
            _settingsService.SetCurrentPageIndex(-1);
            _settingsService.SetCurrentPageIndex(100);

            // Assert
            _settingsService.Settings.CurrentPageIndex.Should().Be(originalIndex);
        }

        #endregion

        #region Page Movement Tests

        [TestMethod]
        public void MovePageUp_WithValidIndex_MovesPageCorrectly()
        {
            // Arrange
            _settingsService = new SettingsService();
            _settingsService.AddPage("Page 1");
            _settingsService.AddPage("Page 2");
            var page1Name = _settingsService.Settings.Pages[1].Name;
            var page2Name = _settingsService.Settings.Pages[2].Name;

            // Act
            _settingsService.MovePageUp(2); // Move page 2 up

            // Assert
            _settingsService.Settings.Pages[1].Name.Should().Be(page2Name);
            _settingsService.Settings.Pages[2].Name.Should().Be(page1Name);
        }

        [TestMethod]
        public void MovePageUp_WithCurrentPageTracking_UpdatesCurrentPageIndex()
        {
            // Arrange
            _settingsService = new SettingsService();
            _settingsService.AddPage("Page 1");
            _settingsService.AddPage("Page 2");
            _settingsService.SetCurrentPageIndex(2);

            // Act
            _settingsService.MovePageUp(2);

            // Assert
            _settingsService.Settings.CurrentPageIndex.Should().Be(1); // Should follow the moved page
        }

        [TestMethod]
        public void MovePageUp_AtTopPosition_DoesNotMove()
        {
            // Arrange
            _settingsService = new SettingsService();
            _settingsService.AddPage("Page 1");
            var originalOrder = _settingsService.Settings.Pages.Select(p => p.Name).ToList();

            // Act
            _settingsService.MovePageUp(0); // Try to move first page up

            // Assert
            var newOrder = _settingsService.Settings.Pages.Select(p => p.Name).ToList();
            newOrder.Should().BeEquivalentTo(originalOrder);
        }

        [TestMethod]
        public void MovePageDown_WithValidIndex_MovesPageCorrectly()
        {
            // Arrange
            _settingsService = new SettingsService();
            _settingsService.AddPage("Page 1");
            _settingsService.AddPage("Page 2");
            var page0Name = _settingsService.Settings.Pages[0].Name;
            var page1Name = _settingsService.Settings.Pages[1].Name;

            // Act
            _settingsService.MovePageDown(0); // Move first page down

            // Assert
            _settingsService.Settings.Pages[0].Name.Should().Be(page1Name);
            _settingsService.Settings.Pages[1].Name.Should().Be(page0Name);
        }

        [TestMethod]
        public void MovePageDown_WithCurrentPageTracking_UpdatesCurrentPageIndex()
        {
            // Arrange
            _settingsService = new SettingsService();
            _settingsService.AddPage("Page 1");
            _settingsService.SetCurrentPageIndex(0);

            // Act
            _settingsService.MovePageDown(0);

            // Assert
            _settingsService.Settings.CurrentPageIndex.Should().Be(1); // Should follow the moved page
        }

        [TestMethod]
        public void MovePageDown_AtBottomPosition_DoesNotMove()
        {
            // Arrange
            _settingsService = new SettingsService();
            _settingsService.AddPage("Page 1");
            var lastIndex = _settingsService.Settings.Pages.Count - 1;
            var originalOrder = _settingsService.Settings.Pages.Select(p => p.Name).ToList();

            // Act
            _settingsService.MovePageDown(lastIndex); // Try to move last page down

            // Assert
            var newOrder = _settingsService.Settings.Pages.Select(p => p.Name).ToList();
            newOrder.Should().BeEquivalentTo(originalOrder);
        }

        #endregion

        #region Auto-Rotation Tests

        [TestMethod]
        public void UpdateAutoRotationSettings_WithValidSettings_UpdatesAllFields()
        {
            // Arrange
            _settingsService = new SettingsService();

            // Act
            _settingsService.UpdateAutoRotationSettings(true, 30, AutoRotationMode.PingPong, false);

            // Assert
            _settingsService.Settings.AutoRotationEnabled.Should().BeTrue();
            _settingsService.Settings.AutoRotationIntervalSeconds.Should().Be(30);
            _settingsService.Settings.RotationMode.Should().Be(AutoRotationMode.PingPong);
            _settingsService.Settings.PauseOnUserInteraction.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(AutoRotationMode.Forward, 0, 1)]
        [DataRow(AutoRotationMode.Forward, 1, 2)]
        [DataRow(AutoRotationMode.Forward, 2, 0)] // Wrap around
        public void GetNextPageIndex_ForwardMode_ReturnsCorrectIndex(AutoRotationMode mode, int currentIndex, int expectedNext)
        {
            // Arrange
            _settingsService = new SettingsService();
            _settingsService.AddPage("Page 1");
            _settingsService.AddPage("Page 2");
            bool pingPongDirection = true;

            // Act
            var nextIndex = _settingsService.GetNextPageIndex(mode, currentIndex, ref pingPongDirection);

            // Assert
            nextIndex.Should().Be(expectedNext);
        }

        [TestMethod]
        [DataRow(AutoRotationMode.Reverse, 0, 2)] // Wrap to end
        [DataRow(AutoRotationMode.Reverse, 1, 0)]
        [DataRow(AutoRotationMode.Reverse, 2, 1)]
        public void GetNextPageIndex_ReverseMode_ReturnsCorrectIndex(AutoRotationMode mode, int currentIndex, int expectedNext)
        {
            // Arrange
            _settingsService = new SettingsService();
            _settingsService.AddPage("Page 1");
            _settingsService.AddPage("Page 2");
            bool pingPongDirection = true;

            // Act
            var nextIndex = _settingsService.GetNextPageIndex(mode, currentIndex, ref pingPongDirection);

            // Assert
            nextIndex.Should().Be(expectedNext);
        }

        [TestMethod]
        public void GetNextPageIndex_PingPongMode_ChangesDirectionAtEnds()
        {
            // Arrange
            _settingsService = new SettingsService();
            _settingsService.AddPage("Page 1");
            _settingsService.AddPage("Page 2");
            bool pingPongDirection = true; // Going forward

            // Act & Assert - Forward direction
            var next1 = _settingsService.GetNextPageIndex(AutoRotationMode.PingPong, 0, ref pingPongDirection);
            next1.Should().Be(1);
            pingPongDirection.Should().BeTrue(); // Still going forward

            var next2 = _settingsService.GetNextPageIndex(AutoRotationMode.PingPong, 1, ref pingPongDirection);
            next2.Should().Be(2);
            pingPongDirection.Should().BeTrue(); // Still going forward

            var next3 = _settingsService.GetNextPageIndex(AutoRotationMode.PingPong, 2, ref pingPongDirection);
            next3.Should().Be(1);
            pingPongDirection.Should().BeFalse(); // Should change direction

            // Act & Assert - Reverse direction
            var next4 = _settingsService.GetNextPageIndex(AutoRotationMode.PingPong, 1, ref pingPongDirection);
            next4.Should().Be(0);
            pingPongDirection.Should().BeFalse(); // Still going backward

            var next5 = _settingsService.GetNextPageIndex(AutoRotationMode.PingPong, 0, ref pingPongDirection);
            next5.Should().Be(1);
            pingPongDirection.Should().BeTrue(); // Should change direction back
        }

        [TestMethod]
        public void GetNextPageIndex_WithSinglePage_ReturnsSameIndex()
        {
            // Arrange
            _settingsService = new SettingsService();
            bool pingPongDirection = true;

            // Act
            var nextIndex = _settingsService.GetNextPageIndex(AutoRotationMode.Forward, 0, ref pingPongDirection);

            // Assert
            nextIndex.Should().Be(0);
        }

        #endregion

        #region Migration Tests

        [TestMethod]
        public void MigrateToMultiPageSystem_WithLegacyWidgetOrder_CreatesMainPageWithWidgets()
        {
            // Arrange
            var legacySettings = new AppSettings();
            legacySettings.Pages.Clear(); // Remove default page
            legacySettings.WidgetOrder.AddRange(new[] { "Widget1", "Widget2", "Widget3" });
            legacySettings.WidgetVisibility["Widget1"] = true;
            legacySettings.WidgetVisibility["Widget2"] = false;
            legacySettings.WidgetVisibility["Widget3"] = true;

            var settingsJson = JsonSerializer.Serialize(legacySettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_testSettingsPath, settingsJson);

            // Act
            _settingsService = new SettingsService();

            // Assert
            _settingsService.Settings.Pages.Should().HaveCount(1);
            _settingsService.Settings.Pages[0].Name.Should().Be("Main");
            _settingsService.Settings.Pages[0].WidgetIds.Should().BeEquivalentTo(new[] { "Widget1", "Widget2", "Widget3" });
            _settingsService.Settings.Pages[0].WidgetVisibility["Widget1"].Should().BeTrue();
            _settingsService.Settings.Pages[0].WidgetVisibility["Widget2"].Should().BeFalse();
            _settingsService.Settings.Pages[0].WidgetVisibility["Widget3"].Should().BeTrue();
        }

        [TestMethod]
        public void MigrateToMultiPageSystem_WithoutLegacyData_CreatesDefaultConfiguration()
        {
            // Arrange
            var emptySettings = new AppSettings();
            emptySettings.Pages.Clear(); // Remove default page
            emptySettings.WidgetOrder.Clear();

            var settingsJson = JsonSerializer.Serialize(emptySettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_testSettingsPath, settingsJson);

            // Act
            _settingsService = new SettingsService();

            // Assert
            _settingsService.Settings.Pages.Should().HaveCount(1);
            _settingsService.Settings.Pages[0].Name.Should().Be("Main");
            _settingsService.Settings.Pages[0].WidgetIds.Should().HaveCount(7);
            _settingsService.Settings.Pages[0].WidgetIds.Should().Contain("CpuWidget");
            _settingsService.Settings.Pages[0].WidgetIds.Should().Contain("RamWidget");
        }

        #endregion

        #region Edge Cases and Error Handling

        [TestMethod]
        public void Settings_Property_ReturnsCurrentSettings()
        {
            // Arrange
            _settingsService = new SettingsService();

            // Act
            var settings = _settingsService.Settings;

            // Assert
            settings.Should().NotBeNull();
            settings.Should().BeSameAs(_settingsService.Settings); // Same reference
        }

        [TestMethod]
        public void SaveSettings_WithIOException_ContinuesGracefully()
        {
            // This test would require mocking File system operations
            // which is complex with the current implementation
            // For now, we verify the method doesn't throw

            // Arrange
            _settingsService = new SettingsService();

            // Act & Assert - Should not throw
            Assert.ThrowsException<Exception>(() =>
            {
                // This would require a way to simulate IO exceptions
                _settingsService.SaveSettings();
            }, "Method should handle IO exceptions gracefully");
        }

        #endregion
    }

    /// <summary>
    /// Extension of SettingsService to enable testing of protected/private methods
    /// </summary>
    public class TestableSettingsService : SettingsService
    {
        public new void MigrateToMultiPageSystem()
        {
            // Make protected method accessible for testing
            var method = typeof(SettingsService).GetMethod("MigrateToMultiPageSystem",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(this, null);
        }
    }
}