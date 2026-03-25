using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DeskViz.Core.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DeskViz.Core.Tests.Models
{
    /// <summary>
    /// Comprehensive unit tests for PageConfig model
    /// Tests cloning, serialization, widget management, and all property behaviors
    /// </summary>
    [TestClass]
    public class PageConfigTests
    {
        private PageConfig _pageConfig;

        [TestInitialize]
        public void Setup()
        {
            _pageConfig = new PageConfig();
        }

        #region Constructor Tests

        [TestMethod]
        public void DefaultConstructor_SetsDefaultValues()
        {
            // Act
            var config = new PageConfig();

            // Assert
            config.Id.Should().NotBeNullOrEmpty();
            config.Name.Should().Be("New Page");
            config.WidgetIds.Should().NotBeNull().And.BeEmpty();
            config.WidgetVisibility.Should().NotBeNull().And.BeEmpty();
            config.WidgetSettings.Should().NotBeNull().And.BeEmpty();
            config.BackgroundSetting.Should().BeNull();
            config.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public void DefaultConstructor_GeneratesUniqueIds()
        {
            // Act
            var config1 = new PageConfig();
            var config2 = new PageConfig();

            // Assert
            config1.Id.Should().NotBe(config2.Id);
            config1.Id.Should().NotBeNullOrEmpty();
            config2.Id.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void ParameterizedConstructor_WithValidName_SetsNameCorrectly()
        {
            // Arrange
            var testName = "Test Page Name";

            // Act
            var config = new PageConfig(testName);

            // Assert
            config.Name.Should().Be(testName);
            config.Id.Should().NotBeNullOrEmpty();
            config.WidgetIds.Should().NotBeNull().And.BeEmpty();
            config.WidgetVisibility.Should().NotBeNull().And.BeEmpty();
            config.WidgetSettings.Should().NotBeNull().And.BeEmpty();
        }

        [TestMethod]
        public void ParameterizedConstructor_WithNullName_SetsNameCorrectly()
        {
            // Act
            var config = new PageConfig(null);

            // Assert
            config.Name.Should().BeNull();
        }

        [TestMethod]
        public void ParameterizedConstructor_WithEmptyName_SetsNameCorrectly()
        {
            // Act
            var config = new PageConfig("");

            // Assert
            config.Name.Should().BeEmpty();
        }

        [TestMethod]
        public void ParameterizedConstructor_WithVeryLongName_SetsNameCorrectly()
        {
            // Arrange
            var longName = new string('A', 1000);

            // Act
            var config = new PageConfig(longName);

            // Assert
            config.Name.Should().Be(longName);
        }

        #endregion

        #region Property Tests

        [TestMethod]
        public void Id_Property_CanBeSetAndRetrieved()
        {
            // Arrange
            var testId = "custom-test-id";

            // Act
            _pageConfig.Id = testId;

            // Assert
            _pageConfig.Id.Should().Be(testId);
        }

        [TestMethod]
        public void Name_Property_CanBeSetAndRetrieved()
        {
            // Arrange
            var testName = "Custom Page Name";

            // Act
            _pageConfig.Name = testName;

            // Assert
            _pageConfig.Name.Should().Be(testName);
        }

        [TestMethod]
        public void WidgetIds_Property_CanBeModified()
        {
            // Arrange
            var widgetIds = new List<string> { "Widget1", "Widget2", "Widget3" };

            // Act
            _pageConfig.WidgetIds.AddRange(widgetIds);

            // Assert
            _pageConfig.WidgetIds.Should().BeEquivalentTo(widgetIds);
        }

        [TestMethod]
        public void WidgetVisibility_Property_CanBeModified()
        {
            // Arrange
            var visibility = new Dictionary<string, bool>
            {
                { "Widget1", true },
                { "Widget2", false },
                { "Widget3", true }
            };

            // Act
            foreach (var kvp in visibility)
            {
                _pageConfig.WidgetVisibility[kvp.Key] = kvp.Value;
            }

            // Assert
            _pageConfig.WidgetVisibility.Should().BeEquivalentTo(visibility);
        }

        [TestMethod]
        public void WidgetSettings_Property_CanBeModified()
        {
            // Arrange
            var settings = new Dictionary<string, Dictionary<string, object>>
            {
                {
                    "Widget1",
                    new Dictionary<string, object>
                    {
                        { "Setting1", "Value1" },
                        { "Setting2", 42 }
                    }
                }
            };

            // Act
            _pageConfig.WidgetSettings = settings;

            // Assert
            _pageConfig.WidgetSettings.Should().BeEquivalentTo(settings);
        }

        [TestMethod]
        public void BackgroundSetting_Property_CanBeSetAndRetrieved()
        {
            // Arrange
            var backgroundSetting = "#FF0000";

            // Act
            _pageConfig.BackgroundSetting = backgroundSetting;

            // Assert
            _pageConfig.BackgroundSetting.Should().Be(backgroundSetting);
        }

        [TestMethod]
        public void CreatedAt_Property_CanBeSetAndRetrieved()
        {
            // Arrange
            var testDate = new DateTime(2023, 1, 1, 12, 0, 0);

            // Act
            _pageConfig.CreatedAt = testDate;

            // Assert
            _pageConfig.CreatedAt.Should().Be(testDate);
        }

        #endregion

        #region Clone Tests

        [TestMethod]
        public void Clone_CreatesDeepCopy()
        {
            // Arrange
            _pageConfig.Name = "Original Page";
            _pageConfig.WidgetIds.AddRange(new[] { "Widget1", "Widget2" });
            _pageConfig.WidgetVisibility["Widget1"] = true;
            _pageConfig.WidgetVisibility["Widget2"] = false;
            _pageConfig.WidgetSettings["Widget1"] = new Dictionary<string, object> { { "Key1", "Value1" } };
            _pageConfig.BackgroundSetting = "#FF0000";

            // Act
            var cloned = _pageConfig.Clone();

            // Assert
            cloned.Should().NotBeSameAs(_pageConfig);
            cloned.Id.Should().NotBe(_pageConfig.Id); // Should have new ID
            cloned.Name.Should().Be("Original Page (Copy)");
            cloned.WidgetIds.Should().BeEquivalentTo(_pageConfig.WidgetIds);
            cloned.WidgetIds.Should().NotBeSameAs(_pageConfig.WidgetIds);
            cloned.WidgetVisibility.Should().BeEquivalentTo(_pageConfig.WidgetVisibility);
            cloned.WidgetVisibility.Should().NotBeSameAs(_pageConfig.WidgetVisibility);
            cloned.WidgetSettings.Should().BeEquivalentTo(_pageConfig.WidgetSettings);
            cloned.WidgetSettings.Should().NotBeSameAs(_pageConfig.WidgetSettings);
            cloned.BackgroundSetting.Should().Be(_pageConfig.BackgroundSetting);
        }

        [TestMethod]
        public void Clone_WithComplexWidgetSettings_ClonesCorrectly()
        {
            // Arrange
            _pageConfig.WidgetSettings["Widget1"] = new Dictionary<string, object>
            {
                { "StringSetting", "Test" },
                { "IntSetting", 42 },
                { "BoolSetting", true },
                { "DoubleSetting", 3.14 }
            };

            // Act
            var cloned = _pageConfig.Clone();

            // Assert
            cloned.WidgetSettings["Widget1"].Should().BeEquivalentTo(_pageConfig.WidgetSettings["Widget1"]);
            cloned.WidgetSettings["Widget1"].Should().NotBeSameAs(_pageConfig.WidgetSettings["Widget1"]);

            // Verify deep copy by modifying original
            _pageConfig.WidgetSettings["Widget1"]["StringSetting"] = "Modified";
            cloned.WidgetSettings["Widget1"]["StringSetting"].Should().Be("Test");
        }

        [TestMethod]
        public void Clone_WithEmptyCollections_ClonesCorrectly()
        {
            // Act
            var cloned = _pageConfig.Clone();

            // Assert
            cloned.WidgetIds.Should().BeEmpty();
            cloned.WidgetVisibility.Should().BeEmpty();
            cloned.WidgetSettings.Should().BeEmpty();
            cloned.Id.Should().NotBe(_pageConfig.Id);
        }

        [TestMethod]
        public void Clone_ModificationDoesNotAffectOriginal()
        {
            // Arrange
            _pageConfig.WidgetIds.Add("OriginalWidget");
            var cloned = _pageConfig.Clone();

            // Act
            cloned.Name = "Modified Clone";
            cloned.WidgetIds.Add("ClonedWidget");
            cloned.WidgetVisibility["NewWidget"] = true;

            // Assert
            _pageConfig.Name.Should().Be("New Page");
            _pageConfig.WidgetIds.Should().NotContain("ClonedWidget");
            _pageConfig.WidgetVisibility.Should().NotContainKey("NewWidget");
        }

        [TestMethod]
        public void Clone_CreatesNewTimestamp()
        {
            // Arrange
            var originalTime = _pageConfig.CreatedAt;
            System.Threading.Thread.Sleep(10); // Ensure time difference

            // Act
            var cloned = _pageConfig.Clone();

            // Assert
            cloned.CreatedAt.Should().BeAfter(originalTime);
        }

        #endregion

        #region Widget Settings Management Tests

        [TestMethod]
        public void GetWidgetSettings_WithExistingWidget_ReturnsSettings()
        {
            // Arrange
            var widgetId = "TestWidget";
            var settings = new Dictionary<string, object> { { "Key1", "Value1" } };
            _pageConfig.WidgetSettings[widgetId] = settings;

            // Act
            var result = _pageConfig.GetWidgetSettings(widgetId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(settings);
        }

        [TestMethod]
        public void GetWidgetSettings_WithNonExistentWidget_ReturnsNull()
        {
            // Act
            var result = _pageConfig.GetWidgetSettings("NonExistentWidget");

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void GetWidgetSettings_WithNullWidgetId_ReturnsNull()
        {
            // Act
            var result = _pageConfig.GetWidgetSettings(null);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void GetWidgetSettings_WithEmptyWidgetId_ReturnsNull()
        {
            // Act
            var result = _pageConfig.GetWidgetSettings("");

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void SetWidgetSettings_WithValidParameters_SetsCorrectly()
        {
            // Arrange
            var widgetId = "TestWidget";
            var settings = new Dictionary<string, object>
            {
                { "Setting1", "Value1" },
                { "Setting2", 42 }
            };

            // Act
            _pageConfig.SetWidgetSettings(widgetId, settings);

            // Assert
            _pageConfig.WidgetSettings[widgetId].Should().BeEquivalentTo(settings);
            _pageConfig.WidgetSettings[widgetId].Should().NotBeSameAs(settings); // Should be a copy
        }

        [TestMethod]
        public void SetWidgetSettings_OverwritesExistingSettings()
        {
            // Arrange
            var widgetId = "TestWidget";
            var originalSettings = new Dictionary<string, object> { { "Old", "Value" } };
            var newSettings = new Dictionary<string, object> { { "New", "Value" } };
            _pageConfig.WidgetSettings[widgetId] = originalSettings;

            // Act
            _pageConfig.SetWidgetSettings(widgetId, newSettings);

            // Assert
            _pageConfig.WidgetSettings[widgetId].Should().BeEquivalentTo(newSettings);
            _pageConfig.WidgetSettings[widgetId].Should().NotContainKey("Old");
        }

        [TestMethod]
        public void SetWidgetSettings_WithNullSettings_DoesNotThrow()
        {
            // Act & Assert
            Action act = () => _pageConfig.SetWidgetSettings("TestWidget", null);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void SetWidgetSettings_WithEmptySettings_SetsEmptyDictionary()
        {
            // Arrange
            var widgetId = "TestWidget";
            var emptySettings = new Dictionary<string, object>();

            // Act
            _pageConfig.SetWidgetSettings(widgetId, emptySettings);

            // Assert
            _pageConfig.WidgetSettings[widgetId].Should().BeEmpty();
        }

        #endregion

        #region Individual Widget Setting Tests

        [TestMethod]
        public void GetWidgetSetting_WithExistingSetting_ReturnsValue()
        {
            // Arrange
            var widgetId = "TestWidget";
            var settingKey = "TestKey";
            var settingValue = "TestValue";
            _pageConfig.WidgetSettings[widgetId] = new Dictionary<string, object> { { settingKey, settingValue } };

            // Act
            var result = _pageConfig.GetWidgetSetting<string>(widgetId, settingKey);

            // Assert
            result.Should().Be(settingValue);
        }

        [TestMethod]
        public void GetWidgetSetting_WithNonExistentWidget_ReturnsDefaultValue()
        {
            // Arrange
            var defaultValue = "DefaultValue";

            // Act
            var result = _pageConfig.GetWidgetSetting("NonExistentWidget", "TestKey", defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        }

        [TestMethod]
        public void GetWidgetSetting_WithNonExistentSetting_ReturnsDefaultValue()
        {
            // Arrange
            var widgetId = "TestWidget";
            var defaultValue = 42;
            _pageConfig.WidgetSettings[widgetId] = new Dictionary<string, object>();

            // Act
            var result = _pageConfig.GetWidgetSetting(widgetId, "NonExistentKey", defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        }

        [TestMethod]
        public void GetWidgetSetting_WithWrongType_ReturnsDefaultValue()
        {
            // Arrange
            var widgetId = "TestWidget";
            var settingKey = "TestKey";
            var settingValue = "StringValue";
            var defaultValue = 42;
            _pageConfig.WidgetSettings[widgetId] = new Dictionary<string, object> { { settingKey, settingValue } };

            // Act
            var result = _pageConfig.GetWidgetSetting(widgetId, settingKey, defaultValue);

            // Assert
            result.Should().Be(defaultValue); // Should return default due to type mismatch
        }

        [TestMethod]
        public void GetWidgetSetting_WithCompatibleTypes_ReturnsConvertedValue()
        {
            // Arrange
            var widgetId = "TestWidget";
            var settingKey = "TestKey";
            var settingValue = 42; // int
            _pageConfig.WidgetSettings[widgetId] = new Dictionary<string, object> { { settingKey, settingValue } };

            // Act
            var result = _pageConfig.GetWidgetSetting<int>(widgetId, settingKey);

            // Assert
            result.Should().Be(42);
        }

        [TestMethod]
        public void GetWidgetSetting_WithNullValues_HandlesGracefully()
        {
            // Arrange
            var widgetId = "TestWidget";
            var settingKey = "TestKey";
            _pageConfig.WidgetSettings[widgetId] = new Dictionary<string, object> { { settingKey, null } };

            // Act
            var result = _pageConfig.GetWidgetSetting<string>(widgetId, settingKey, "default");

            // Assert
            result.Should().Be("default");
        }

        [TestMethod]
        public void SetWidgetSetting_WithNewWidget_CreatesWidgetEntry()
        {
            // Arrange
            var widgetId = "NewWidget";
            var settingKey = "TestKey";
            var settingValue = "TestValue";

            // Act
            _pageConfig.SetWidgetSetting(widgetId, settingKey, settingValue);

            // Assert
            _pageConfig.WidgetSettings.Should().ContainKey(widgetId);
            _pageConfig.WidgetSettings[widgetId].Should().ContainKey(settingKey);
            _pageConfig.WidgetSettings[widgetId][settingKey].Should().Be(settingValue);
        }

        [TestMethod]
        public void SetWidgetSetting_WithExistingWidget_AddsNewSetting()
        {
            // Arrange
            var widgetId = "TestWidget";
            _pageConfig.WidgetSettings[widgetId] = new Dictionary<string, object> { { "Existing", "Value" } };

            // Act
            _pageConfig.SetWidgetSetting(widgetId, "NewKey", "NewValue");

            // Assert
            _pageConfig.WidgetSettings[widgetId].Should().ContainKey("Existing");
            _pageConfig.WidgetSettings[widgetId].Should().ContainKey("NewKey");
            _pageConfig.WidgetSettings[widgetId]["NewKey"].Should().Be("NewValue");
        }

        [TestMethod]
        public void SetWidgetSetting_OverwritesExistingValue()
        {
            // Arrange
            var widgetId = "TestWidget";
            var settingKey = "TestKey";
            _pageConfig.WidgetSettings[widgetId] = new Dictionary<string, object> { { settingKey, "OldValue" } };

            // Act
            _pageConfig.SetWidgetSetting(widgetId, settingKey, "NewValue");

            // Assert
            _pageConfig.WidgetSettings[widgetId][settingKey].Should().Be("NewValue");
        }

        [TestMethod]
        [DataRow("string", "StringValue")]
        [DataRow("int", 42)]
        [DataRow("bool", true)]
        [DataRow("double", 3.14)]
        public void SetWidgetSetting_WithDifferentTypes_StoresCorrectly(string testType, object value)
        {
            // Arrange
            var widgetId = "TestWidget";
            var settingKey = $"TestKey_{testType}";

            // Act
            _pageConfig.SetWidgetSetting(widgetId, settingKey, value);

            // Assert
            _pageConfig.WidgetSettings[widgetId][settingKey].Should().Be(value);
        }

        [TestMethod]
        public void SetWidgetSetting_WithNullValue_StoresNull()
        {
            // Arrange
            var widgetId = "TestWidget";
            var settingKey = "TestKey";

            // Act
            _pageConfig.SetWidgetSetting(widgetId, settingKey, null);

            // Assert
            _pageConfig.WidgetSettings[widgetId][settingKey].Should().BeNull();
        }

        #endregion

        #region Serialization Tests

        [TestMethod]
        public void Serialization_RoundTrip_PreservesAllData()
        {
            // Arrange
            _pageConfig.Name = "Test Page";
            _pageConfig.WidgetIds.AddRange(new[] { "Widget1", "Widget2" });
            _pageConfig.WidgetVisibility["Widget1"] = true;
            _pageConfig.WidgetVisibility["Widget2"] = false;
            _pageConfig.WidgetSettings["Widget1"] = new Dictionary<string, object>
            {
                { "StringSetting", "Value" },
                { "IntSetting", 42 },
                { "BoolSetting", true }
            };
            _pageConfig.BackgroundSetting = "#FF0000";

            // Act
            var json = JsonSerializer.Serialize(_pageConfig, new JsonSerializerOptions { WriteIndented = true });
            var deserialized = JsonSerializer.Deserialize<PageConfig>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.Id.Should().Be(_pageConfig.Id);
            deserialized.Name.Should().Be(_pageConfig.Name);
            deserialized.WidgetIds.Should().BeEquivalentTo(_pageConfig.WidgetIds);
            deserialized.WidgetVisibility.Should().BeEquivalentTo(_pageConfig.WidgetVisibility);
            deserialized.BackgroundSetting.Should().Be(_pageConfig.BackgroundSetting);
            deserialized.CreatedAt.Should().BeCloseTo(_pageConfig.CreatedAt, TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public void Serialization_WithEmptyCollections_WorksCorrectly()
        {
            // Act
            var json = JsonSerializer.Serialize(_pageConfig);
            var deserialized = JsonSerializer.Deserialize<PageConfig>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.WidgetIds.Should().BeEmpty();
            deserialized.WidgetVisibility.Should().BeEmpty();
            deserialized.WidgetSettings.Should().BeEmpty();
        }

        [TestMethod]
        public void Serialization_WithNullValues_HandlesGracefully()
        {
            // Arrange
            _pageConfig.Name = null;
            _pageConfig.BackgroundSetting = null;

            // Act
            var json = JsonSerializer.Serialize(_pageConfig);
            var deserialized = JsonSerializer.Deserialize<PageConfig>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.Name.Should().BeNull();
            deserialized.BackgroundSetting.Should().BeNull();
        }

        [TestMethod]
        public void Serialization_WithComplexWidgetSettings_PreservesStructure()
        {
            // Arrange
            _pageConfig.WidgetSettings["ComplexWidget"] = new Dictionary<string, object>
            {
                { "NestedObject", new { Property1 = "Value1", Property2 = 42 } },
                { "Array", new[] { 1, 2, 3 } },
                { "DateTime", DateTime.Now }
            };

            // Act & Assert - Should not throw during serialization
            Action act = () =>
            {
                var json = JsonSerializer.Serialize(_pageConfig);
                var deserialized = JsonSerializer.Deserialize<PageConfig>(json);
            };
            act.Should().NotThrow();
        }

        #endregion

        #region Edge Cases and Error Handling

        [TestMethod]
        public void PageConfig_WithVeryLongCollections_HandlesCorrectly()
        {
            // Arrange
            var largeList = Enumerable.Range(0, 10000).Select(i => $"Widget_{i}").ToList();

            // Act
            _pageConfig.WidgetIds.AddRange(largeList);

            // Assert
            _pageConfig.WidgetIds.Should().HaveCount(10000);
            _pageConfig.WidgetIds.Should().Contain("Widget_0");
            _pageConfig.WidgetIds.Should().Contain("Widget_9999");
        }

        [TestMethod]
        public void PageConfig_WithSpecialCharactersInNames_HandlesCorrectly()
        {
            // Arrange
            var specialName = "Page with !@#$%^&*()_+-=[]{}|;':\",./<>?";

            // Act
            _pageConfig.Name = specialName;

            // Assert
            _pageConfig.Name.Should().Be(specialName);
        }

        [TestMethod]
        public void PageConfig_WithUnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            var unicodeName = "页面名称 🎉 Página 日本語";

            // Act
            _pageConfig.Name = unicodeName;

            // Assert
            _pageConfig.Name.Should().Be(unicodeName);
        }

        [TestMethod]
        public void WidgetSettings_WithDuplicateKeys_HandlesCorrectly()
        {
            // Arrange
            var widgetId = "TestWidget";
            var key = "DuplicateKey";

            // Act
            _pageConfig.SetWidgetSetting(widgetId, key, "Value1");
            _pageConfig.SetWidgetSetting(widgetId, key, "Value2");

            // Assert
            _pageConfig.GetWidgetSetting<string>(widgetId, key).Should().Be("Value2");
        }

        #endregion

        #region Performance Tests

        [TestMethod]
        public void Clone_WithLargeDataSet_PerformsAcceptably()
        {
            // Arrange
            for (int i = 0; i < 1000; i++)
            {
                _pageConfig.WidgetIds.Add($"Widget_{i}");
                _pageConfig.WidgetVisibility[$"Widget_{i}"] = i % 2 == 0;
                _pageConfig.WidgetSettings[$"Widget_{i}"] = new Dictionary<string, object>
                {
                    { "Setting1", $"Value_{i}" },
                    { "Setting2", i }
                };
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var cloned = _pageConfig.Clone();

            // Assert
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete in under 1 second
            cloned.WidgetIds.Should().HaveCount(1000);
            cloned.WidgetVisibility.Should().HaveCount(1000);
            cloned.WidgetSettings.Should().HaveCount(1000);
        }

        [TestMethod]
        public void WidgetSettings_MassOperations_PerformAcceptably()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 10000; i++)
            {
                _pageConfig.SetWidgetSetting($"Widget_{i % 100}", $"Setting_{i}", $"Value_{i}");
            }

            for (int i = 0; i < 10000; i++)
            {
                _pageConfig.GetWidgetSetting<string>($"Widget_{i % 100}", $"Setting_{i}");
            }

            // Assert
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete in under 2 seconds
        }

        #endregion
    }
}