# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

### Windows PowerShell
```powershell
# Build and run
.\build.ps1 -Run

# Clean build
.\build.ps1 -Clean

# Release package
.\build.ps1 -Configuration Release -Package

# Watch mode (auto-rebuild on changes)
.\dev.ps1 watch

# Run without building
.\dev.ps1 run
```

### WSL/Linux
```bash
# Build and run (calls PowerShell)
./build.sh --run

# Or directly
powershell.exe -File build.ps1 -Run
```

**Important:** PowerShell uses single dash syntax (`-Run`), not double dash (`--run`)

## Architecture Overview

### Project Structure
- **DeskViz.App** - WPF presentation layer (.NET 8.0 Windows app)
- **DeskViz.Core** - Business logic and services

### Core Patterns

1. **Widget Architecture**
   - All widgets implement `IWidget` interface
   - Inherit from `System.Windows.Controls.UserControl` (use fully qualified name to avoid ambiguity)
   - Implement `INotifyPropertyChanged` for data binding
   - Self-contained with own settings persistence

2. **Service Pattern**
   - `IHardwareMonitorService` - Hardware monitoring abstraction (LibreHardwareMonitor)
   - `SettingsService` - JSON-based settings persistence
   - `ScreenService` - Multi-monitor management
   - Services are manually injected via constructors

3. **Widget Registration**
   - Widgets are manually registered in `MainWindow.RegisterWidgets()`
   - Each widget needs constructor injection of required services
   - Widget visibility and ordering managed by settings

## Creating a New Widget

1. Create folder: `DeskViz.App/Widgets/YourWidget/`

2. Create XAML file with this structure:
```xml
<UserControl x:Class="DeskViz.App.Widgets.YourWidget.YourWidget"
             xmlns:converters="clr-namespace:DeskViz.App.Converters"
             xmlns:controls="clr-namespace:DeskViz.App.Controls">
    <UserControl.ContextMenu>
        <ContextMenu>
            <MenuItem Header="Configure Widget" Click="ConfigButton_Click" />
        </ContextMenu>
    </UserControl.ContextMenu>
    <!-- Widget content -->
</UserControl>
```

3. Code-behind pattern:
```csharp
public partial class YourWidget : System.Windows.Controls.UserControl, IWidget, INotifyPropertyChanged
{
    private readonly IHardwareMonitorService _hardwareMonitorService;
    private readonly SettingsService _settingsService;
    private readonly DispatcherTimer _updateTimer;

    public string WidgetId => "YourWidget";
    public string DisplayName => "Your Widget";
    
    public YourWidget(IHardwareMonitorService hardwareMonitorService, SettingsService settingsService)
    {
        InitializeComponent();
        DataContext = this;
        LoadSettings();
        // Initialize timer
    }
}
```

4. Register in `MainWindow.RegisterWidgets()`:
```csharp
var yourWidget = new YourWidget(_hardwareMonitorService, _settingsService);
yourWidget.DataContext = yourWidget;
yourWidget.ConfigButtonClicked += Widget_ConfigButtonClicked;
_allWidgets.Add(yourWidget);
```

5. Add settings to `AppSettings` class in `SettingsService.cs`

## Hardware Monitoring

The app uses LibreHardwareMonitor which requires administrator privileges for full functionality. Hardware data is accessed through `IHardwareMonitorService`:

- Call `Update()` before reading sensors
- Check `IsInitialized` before using
- Handle `float.NaN` for missing temperature sensors
- Single shared instance across all widgets

## Common Issues

1. **Ambiguous UserControl Reference**
   - Use `System.Windows.Controls.UserControl` instead of just `UserControl`

2. **Build from WSL**
   - Application won't run in WSL (WPF requires Windows)
   - Build creates Windows executables that must be run from Windows

3. **Hardware Monitoring**
   - Requires administrator privileges
   - May return 0 or NaN if sensors unavailable

## Widget Settings Pattern

Settings are stored in `AppData\Roaming\DeskViz\settings.json`. Each widget's settings are part of the main `AppSettings` class. Settings are automatically saved when properties change if the property setter calls `SaveSettings()`.

## Update Mechanism

Widgets use `DispatcherTimer` for periodic updates. Each widget can have its own update interval. The `RefreshData()` method should:
1. Call `_hardwareMonitorService.Update()`
2. Update all bound properties
3. Handle exceptions gracefully