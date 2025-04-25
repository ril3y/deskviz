# DeskViz Development TODO

## Completed
- Robust multi-monitor support: app launches on user-selected (or smallest) monitor, full screen, always on top, and remembers preference.
- Placeholder dashboard implemented.
- Created abstract base class for dashboard widgets (DeskVizWidget) to enforce consistent interface.
- Implemented orientation detection and appropriate layouts for portrait/landscape screens.
- Added fullscreen mode with hidden title bar.
- Implemented tap-to-show settings button that auto-hides after 5 seconds.
- Updated StatTile widgets to use the DeskVizWidget abstract base class.
- True immersive fullscreen mode that hides Windows taskbar and all window decorations.
- Fixed window positioning on external monitor.
- Implemented widget registration system to track all available widgets
- Created a WidgetRegistry class to manage all registered widgets
- Updated settings screen to show/hide individual widgets
- Persist widget visibility settings using SharedPreferences
- Made HomeScreen dynamically create layouts based on visible widgets
- Added drag-and-drop support for widget reordering in settings
- Enhanced CPU widget with configurable polling time, per-core visualization, and detailed settings

## In Progress / Next Steps
- [ ] Each system stat widget (CPU, RAM, etc.) will be a self-contained file in the widgets directory.
- [ ] Create a shared API layer for widgets (system stats, HTTP, etc.).
- [ ] Implement settings UI (orientation, widget enable/disable, etc.).
- [ ] Replace placeholder dashboard with modular, enable/disable widgets.
- [ ] Add real system stats widgets (CPU, RAM, Disk, Network, etc.).
- [ ] Polish UI for external monitor (ultra-wide, dark mode, etc.).
- [ ] Refactor each system stat widget (CPU, RAM, etc.) into self-contained files in the widgets directory.
- [ ] Improve the shared API layer for system stats, etc.
- [ ] Enhance settings UI with widget enable/disable options.
- [ ] Make widgets modular with ability to enable/disable them.
- [ ] Add more system stat widgets (Network, GPU, Temperature, etc.).
- [ ] Optimize for different display shapes and sizes.

## Current Priority Tasks
- [x] Implement widget registration system to track all available widgets
- [x] Create a WidgetRegistry class to manage all registered widgets
- [x] Update settings screen to show/hide individual widgets
- [x] Persist widget visibility settings using SharedPreferences
- [x] Make HomeScreen dynamically create layouts based on visible widgets
- [x] Add drag-and-drop support for widget reordering in settings
- [x] Enhance CPU widget with configurable polling time and per-core visualization

## Ideas / Future
- [ ] Custom widget layout (drag/drop, resize, etc.)
- [ ] Export/import settings
- [ ] Theming support
- [ ] Widget auto-arrangement based on display characteristics
- [ ] Keyboard shortcuts for common actions (change displays, etc.)
- [ ] Add system tray icon with quick actions
- [ ] Multiple dashboard pages/layouts that can be switched between
- [ ] Widget categories (system stats, network, weather, etc.)
- [ ] Widget marketplace for downloading new widgets
- [ ] User-customizable color themes for each widget
