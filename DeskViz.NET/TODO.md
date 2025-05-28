# DeskViz.NET Development TODO

## Initial Setup
- [x] Create .NET solution structure
- [x] Set up WPF application project
- [x] Set up Core library project
- [x] Create project documentation (README.md with build instructions)

## Build & Debugging
- [x] Resolve initial build errors and warnings (ambiguous refs, nullability, partial classes, icon issues)
- [x] Resolve progress bar binding issues
- [x] Create build scripts for Windows and WSL (build.ps1, build.sh, dev.ps1)

## Phase 1: Core Functionality
- [ ] Replace current system monitoring with LibreHardwareMonitor integration
  - [x] Add LibreHardwareMonitor NuGet package to Core project
  - [x] Create new LibreHardwareMonitorService to replace SystemInfoService
  - [x] Implement CPU monitoring (temperature, usage, clock speeds)
  - [x] Implement RAM monitoring (usage, free/used memory)
  - [x] Implement GPU monitoring (temperature, usage, memory, multi-GPU support)
  - [ ] Implement disk monitoring (usage, read/write speeds)
  - [ ] Implement motherboard sensor monitoring (temperatures, fan speeds)
- [ ] Create multi-monitor detection and management
- [ ] Implement fullscreen mode with proper taskbar hiding
- [ ] Design window positioning system for external monitors
- [x] Create settings persistence mechanism

## Phase 2: UI Implementation
- [ ] Design main dashboard UI with dark theme
- [ ] Create widget base class for modular components
- [x] Implement CPU usage widget with per-core visualization
- [x] Implement RAM usage widget
- [x] Implement GPU usage widget with multi-GPU selection
- [ ] Implement Disk usage widget
- [ ] Implement Media Control widget (Windows media integration)
- [ ] Create orientation-aware layouts (portrait/landscape)
- [x] Add settings UI for display and widget configuration

## Phase 3: Widget Plugin Architecture
- [ ] Create a plugin system for widget loading
- [ ] Design an interface for widget plugins
- [ ] Implement MEF (Managed Extensibility Framework) for dynamic plugin loading
- [ ] Set up a discovery system for widgets in a plugins directory
- [ ] Create a widget registration mechanism
- [ ] Implement versioning for widget plugins
- [ ] Add metadata support for widget discovery
- [ ] Create a basic widget plugin template project
- [ ] Add a widget manager to handle loading/unloading plugins
- [ ] Implement isolation between widget plugins

## Phase 4: Advanced Features
- [ ] Implement drag-and-drop support for widget reordering
- [ ] Add widget visibility toggling
- [ ] Create custom layout persistence
- [ ] Add system tray icon with quick actions
- [ ] Create a widget marketplace concept
- [ ] Implement widget settings persistence per plugin
- [ ] Add a widget development SDK
- [ ] Support third-party widget themes
- [ ] Create a widget update mechanism

## Phase 5: Media Control Widget
- [ ] Research Windows Media Session Manager (SMTC) integration
- [ ] Create Windows Media Control service for session management
- [ ] Implement media playback detection (current playing song/app)
- [ ] Create media control widget UI with touch-friendly design
- [ ] Add play/pause/stop/skip controls
- [ ] Implement volume slider with touch support
- [ ] Add now playing display (title, artist, album art)
- [ ] Support multiple media sessions (Spotify, YouTube, etc.)
- [ ] Add media widget settings for layout and controls
- [ ] Handle media session changes and state updates

## Current Priority Tasks
- [ ] Implement proper multi-monitor fullscreen support
- [ ] Replace PowerShell/WMI with LibreHardwareMonitor for system monitoring
- [ ] Design main dashboard UI
- [x] Implement basic CPU widget
- [x] Improve CPU widget UI with animated progress bars
- [x] Enhance CPU widget with temperature display options and additional metrics
- [ ] Create a plugin architecture proof-of-concept
- [x] Fix context menu issue
- [x] Right-click context menu for widget configuration
- [x] Implement actual widget configuration logic (opening settings window/dialog)
- [ ] Refine widget layout and appearance (padding, margins, alignment)
- [ ] Error handling for hardware monitoring failures
- [ ] Add tooltips for clarity
- [ ] Improve Settings window UI/UX
- [ ] Save/Load widget positions
- [ ] Ensure proper disposal of resources (e.g., hardware monitor)
- [ ] Code cleanup and documentation
- [ ] Start Media Control widget implementation (Phase 5)

## Working Features
*   CPU Widget: Displays basic CPU info (Name, Usage %, Core count, Core usage bars).
*   CPU Widget Settings: Allows toggling core visibility.
*   Clock Widget: Displays current time.
*   Logo Widget: Displays logo, configurable image path.
*   Settings System: Loads/saves settings (Display, Fullscreen, Widget Visibility/Order).
*   Basic Window Management: Moves to selected screen, basic fullscreen.
*   RAM Widget: Displays basic RAM info (Total, Used, Free, Usage Bar).
*   RAM Widget Settings (Page File Toggle): Allows toggling Page File info visibility.
*   GPU Widget: Displays GPU info (Name, Usage %, Temperature, Memory, Clock Speed, Power) with multi-GPU selection.
*   GPU Widget Settings: Allows toggling display of temperature, power, memory, clock speed and GPU selection.
*   Context Menus: Right-click on widgets to open their configuration dialogs.

## Theming/Styling
*   Add padding/margin between widgets for better visual separation.

## Notes
- For multi-monitor detection, use System.Windows.Forms.Screen class
- For fullscreen window handling, use WPF window properties (WindowStyle, ResizeMode, etc.)
- ~~For system monitoring, use System.Diagnostics.PerformanceCounter~~ Use LibreHardwareMonitor instead for more accurate and comprehensive hardware monitoring
- LibreHardwareMonitor provides access to:
  * CPU details (usage, temperature, clock speeds)
  * RAM information (usage, available memory)
  * GPU metrics (usage, temperature, memory)
  * Storage devices (usage, health status)
  * Motherboard sensors (temperatures, fan speeds)
  * Network adapters (throughput, usage)
- For the plugin system, look into MEF (System.ComponentModel.Composition)
- For widget discovery, consider a directory watcher pattern
- Each widget plugin should include:
  * Widget DLL with implementation
  * XAML resources if needed
  * Metadata for discovery and versioning
  * Self-contained settings
- For Media Control widget, use Windows.Media.Control (Windows 10 v1903+) for System Media Transport Controls
- Alternative: Windows Runtime API (Windows.Media.SystemMediaTransportControls)
- Touch slider implementation: Use Slider control with custom styling and touch events
- Album art support: Handle media thumbnail/artwork from media sessions
