# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Structure

DeskViz is a desktop visualization application with both a .NET implementation and Flutter prototype:

- `DeskViz.NET/` - Main .NET 8 WPF desktop application for Windows
- Other directories contain Flutter prototype and related files

**Working Directory**: Always work from the `DeskViz.NET/` directory for .NET development.

## Architecture

### .NET Application Structure
- **DeskViz.App** - WPF UI layer with widgets, views, and controls
- **DeskViz.Core** - Core services and business logic
  - Hardware monitoring via LibreHardwareMonitor
  - Settings management with JSON serialization
  - Screen/display services
  - Media control integration
  - System tray functionality

### Widget System
The application uses a widget-based architecture where each widget:
- Has its own XAML view and code-behind
- Includes a settings dialog (e.g., `CpuWidgetSettings.xaml`)
- Implements `IWidget` interface
- Located in `DeskViz.App/Widgets/{WidgetName}/`

Available widgets: Clock, CPU, GPU, RAM, Logo, MediaControl

### Key Services
- `SettingsService` - Manages app configuration in JSON format
- `LibreHardwareMonitorService` - Hardware monitoring (CPU, GPU, RAM)
- `WindowsMediaControlService` - Media playback controls
- `ScreenService` - Multi-monitor support
- `SystemTrayService` - System tray integration

## Build Commands

**Important**: Always run commands from the `DeskViz.NET/` directory.

### Windows PowerShell
```powershell
# Build and run
.\build.ps1 -Run

# Clean build
.\build.ps1 -Clean

# Release package
.\build.ps1 -Configuration Release -Package

# Development commands
.\dev.ps1 run
.\dev.ps1 watch  # Auto-rebuild on changes
```

### WSL/Linux
```bash
# Build and run (calls PowerShell)
./build.sh --run

# Or directly
powershell.exe -File build.ps1 -Run
```

### Direct .NET CLI
```bash
# From DeskViz.NET/ directory
dotnet build
dotnet run --project DeskViz.App/DeskViz.App.csproj
dotnet watch --project DeskViz.App/DeskViz.App.csproj run
```

## Development Notes

### Requirements
- .NET 8.0 SDK
- Windows 10/11 (for running)
- LibreHardwareMonitor requires Administrator privileges for hardware access

### Key Dependencies
- WPF for UI framework
- LibreHardwareMonitorLib for hardware monitoring
- System.Management for Windows integration

### File Conventions
- XAML files for UI layouts
- Code-behind files (.xaml.cs) for UI logic
- Services in `DeskViz.Core/Services/`
- Models in `DeskViz.Core/Models/`
- Settings stored in `%APPDATA%/DeskViz/`

### Adding New Widgets
1. Create folder in `DeskViz.App/Widgets/{WidgetName}/`
2. Add `{WidgetName}.xaml` and `{WidgetName}Settings.xaml`
3. Implement `IWidget` interface
4. Register in MainWindow initialization

### Configuration
Settings are JSON-serialized via `SettingsService` and stored in user's AppData folder. The application supports multi-page widget layouts and per-widget configuration.