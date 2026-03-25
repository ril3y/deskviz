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
- [x] Implement Media Control widget (Windows media integration)
- [ ] Create orientation-aware layouts (portrait/landscape)
- [x] Add settings UI for display and widget configuration

## Phase 3: Multi-Page Widget System
- [x] Implement swipeable multi-page interface
  - [x] Create PagedWidgetContainer control with swipe gesture support
  - [x] Add touch/mouse swipe detection (left/right for pages)
  - [x] Implement page indicators (dots at bottom)
  - [x] Add smooth page transition animations
  - [x] Support keyboard navigation (arrow keys, Page Up/Down)
- [x] Extend settings system for multi-page configuration
  - [x] Add "Pages" section to settings window
  - [x] Implement add/remove/rename page functionality
  - [x] Create page-specific widget lists
  - [x] Add "Move to Page" option in widget context menus
  - [x] Save/load page configurations
- [x] Update widget management
  - [x] Track widgets per page instead of globally
  - [x] Maintain separate widget order for each page
  - [ ] Support duplicating widgets across pages
  - [ ] Add page preview in settings
- [x] Navigation enhancements
  - [x] Optional: Add swipe from top for quick page selector
  - [x] System tray menu to jump to specific pages
  - [x] Configurable auto-rotate through pages

## Phase 4: Widget Plugin Architecture (Hybrid Approach)
- [x] Create plugin infrastructure while keeping core widgets built-in
  - [x] Design DeskViz.Plugins assembly with IWidgetPlugin interface
  - [x] Keep core widgets (CPU, RAM, GPU, Clock, Logo) in main assembly
  - [x] Create plugin loading mechanism via reflection (WidgetDiscoveryService)
  - [x] Implement widget manifest/metadata system (IWidgetMetadata)
- [ ] Develop proof-of-concept plugin widget
  - [ ] Create sample Camera widget as separate DLL
  - [ ] Test dynamic loading and unloading
  - [ ] Verify service injection works across DLL boundaries
- [ ] Security and distribution preparation
  - [ ] Implement DLL signing for widget authenticity
  - [ ] Create widget package format (.dwp - DeskViz Widget Package)
  - [ ] Add plugin sandboxing/permissions system
  - [ ] Design widget marketplace API
- [ ] Developer experience
  - [ ] Create Visual Studio widget template
  - [ ] Widget debugging tools
  - [ ] Sample widgets and documentation
  - [ ] Widget submission guidelines

## Phase 5: Advanced Features
- [ ] Implement drag-and-drop support for widget reordering
- [x] Add widget visibility toggling
- [ ] Create custom layout persistence
- [x] Add system tray icon with quick actions
  - [x] Create notification area (system tray) icon using app icon
  - [x] Implement right-click context menu with options:
    - [x] "Widget Settings" - opens main settings window
    - [x] "About" - shows app version and information
    - [x] "Exit" - closes the application
  - [x] Add option to minimize to tray instead of taskbar
  - [x] Show/hide main window on tray icon double-click
- [ ] Create a widget marketplace concept
- [ ] Implement widget settings persistence per plugin
- [ ] Add a widget development SDK
- [ ] Support third-party widget themes
- [ ] Create a widget update mechanism

## Phase 6: Media Control Widget
- [x] Research Windows Media Session Manager (SMTC) integration
- [x] Create Windows Media Control service for session management
- [x] Implement media playback detection (current playing song/app)
- [x] Create media control widget UI with touch-friendly design
- [x] Add play/pause/stop/skip controls
- [x] Implement volume slider with touch support
- [x] Add now playing display (title, artist, album art)
- [x] Support multiple media sessions (Spotify, YouTube, etc.)
- [x] Add media widget settings for layout and controls
- [x] Handle media session changes and state updates

## Current Priority Tasks
- [ ] Fix CPU widget power and clock speed display issues
  - [ ] Ensure clock speed shows in GHz with proper formatting
  - [ ] Investigate why power usage shows 0 (may be hardware limitation)
  - [ ] Add fallback UI when sensors unavailable
- [ ] Implement proper multi-monitor fullscreen support
- [ ] Refine widget layout and appearance (padding, margins, alignment)
- [ ] Error handling for hardware monitoring failures
- [ ] Add tooltips for clarity
- [ ] Improve Settings window UI/UX
- [ ] Ensure proper disposal of resources (e.g., hardware monitor)
- [ ] Code cleanup and documentation

## Working Features
*   CPU Widget: Displays basic CPU info (Name, Usage %, Core count, Core usage bars).
*   CPU Widget Settings: Allows toggling core visibility.
*   Clock Widget: Displays current time.
*   Logo Widget: Displays logo, configurable image path.
*   HardDrive Widget: Displays disk usage information with settings dialog.
*   Media Control Widget: Play/pause/stop/skip controls, volume slider, now playing display (title, artist, album art), supports multiple media sessions (Spotify, YouTube, etc.).
*   Settings System: Loads/saves settings (Display, Fullscreen, Widget Visibility/Order, Pages, Auto-Rotation).
*   Basic Window Management: Moves to selected screen, basic fullscreen.
*   RAM Widget: Displays basic RAM info (Total, Used, Free, Usage Bar).
*   RAM Widget Settings (Page File Toggle): Allows toggling Page File info visibility.
*   GPU Widget: Displays GPU info (Name, Usage %, Temperature, Memory, Clock Speed, Power) with multi-GPU selection.
*   GPU Widget Settings: Allows toggling display of temperature, power, memory, clock speed and GPU selection.
*   Context Menus: Right-click on widgets to open their configuration dialogs.
*   System Tray: Minimize to tray, double-click to show/hide, context menu with Settings/About/Exit options.
*   About Window: Displays application version, copyright, and system information.
*   Multi-Page System: Swipeable PagedWidgetContainer with touch/mouse support, page indicators, smooth animations, keyboard navigation (arrow keys, Page Up/Down), swipe-down page selector overlay, add/remove/rename pages via settings, per-page widget lists and visibility.
*   Auto-Rotation: Configurable automatic page rotation with interval and mode settings.
*   Plugin Architecture: DeskViz.Plugins assembly with IWidgetPlugin interface, plugin discovery and loading via reflection (WidgetDiscoveryService), widget metadata system (IWidgetMetadata), plugin widget wrapper for integration with core app.

## Theming/Styling
*   Add padding/margin between widgets for better visual separation.

## Implementation Notes

### Multi-Page System Architecture
- Each page is a separate widget container with its own widget list
- Pages stored as: `Dictionary<string, PageConfig>` where PageConfig contains:
  * Page name/ID
  * Widget list and order
  * Page-specific settings (background, theme, etc.)
- Swipe detection using WPF touch events or mouse drag
- Use `TranslateTransform` for smooth page transitions
- Consider using `FlipView` control pattern or custom implementation

### Widget Architecture Evolution
1. **Phase 1 (Current)**: Monolithic - all widgets in main assembly
2. **Phase 2 (Multi-page)**: Same architecture but with page management
3. **Phase 3 (Hybrid)**: Core widgets built-in, plugin system ready
4. **Phase 4 (Full plugin)**: Gradual migration of widgets to plugins

### Page Navigation Gestures
- Horizontal swipe: Change pages
- Vertical swipe from top: Show page selector
- Pinch: Show all pages overview (optional)
- Double-tap: Widget-specific action

## Technical Notes
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
