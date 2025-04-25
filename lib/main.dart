import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';
import 'package:flutter/gestures.dart';
import 'package:window_manager/window_manager.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:window_size/window_size.dart' as window_size;
import 'widgets/cpu_widget.dart';
import 'widgets/ram_widget.dart';
import 'screens/settings_screen.dart';
import 'screens/home_screen.dart';
import 'screens/widget_settings_screen.dart';
import 'dart:io' show Platform;

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  
  // Initialize window_manager
  await windowManager.ensureInitialized();

  // Configure window options
  WindowOptions windowOptions = const WindowOptions(
    size: Size(800, 600),
    center: true,
    backgroundColor: Colors.transparent,
    titleBarStyle: TitleBarStyle.hidden,
    windowButtonVisibility: false,
    fullScreen: true,
  );

  // Wait for the window to be ready before setting fullscreen
  await windowManager.waitUntilReadyToShow(windowOptions, () async {
    await windowManager.show();
    await windowManager.focus();
    // Set to fullscreen
    print("Attempting to set fullscreen...");
    await windowManager.setFullScreen(true);
    await windowManager.setAlwaysOnTop(true);
    
    // First make sure we're on the right display
    // await applyLastDisplaySettings(); // Temporarily commented out
    
    // More reliable fullscreen sequence for Windows
    print('[main] Initializing window with fullscreen settings');
    
    // Start with hiding the window while we set up
    await windowManager.hide();
    
    // Clear window frame and prepare for fullscreen
    await windowManager.setAsFrameless();
    
    // For Windows, the sequencing is important
    if (Platform.isWindows) {
      // Windows-specific sequence that works more reliably
      await windowManager.setFullScreen(false); // Turn off first if it was on
      await Future.delayed(const Duration(milliseconds: 100));
      await windowManager.maximize(); // First maximize
      await Future.delayed(const Duration(milliseconds: 200));
    }
    
    // Set fullscreen mode
    await windowManager.setFullScreen(true);
    await windowManager.setAlwaysOnTop(true);
    
    // Now show the window
    await windowManager.focus();
    await windowManager.show();
    
    print('[main] Window initialized with fullscreen settings');
  });

  // Debug flags
  // Enable verbose gesture logging in debug mode
  if (kDebugMode) {
    debugPrintGestureArenaDiagnostics = kDebugMode;
    print('[main] Gesture recognition debugging enabled');
  }

  runApp(const DeskVizApp());
}

/// Loads and applies the last saved display settings
Future<void> applyLastDisplaySettings() async {
  print('[main] Loading saved display settings');
  final prefs = await SharedPreferences.getInstance();
  final savedIdentifier = prefs.getString('preferredDisplayIdentifier');
  
  if (savedIdentifier == null) {
    print('[main] No saved display identifier found, using default');
    return; // No saved preference, use default positioning
  }
  
  // Get all available screens
  final screens = await window_size.getScreenList();
  print('[main] Found ${screens.length} screens');
  
  // Look for the screen matching our saved identifier
  window_size.Screen? selectedScreen;
  
  for (var screen in screens) {
    final frame = screen.frame;
    final identifier = "${frame.left.toInt()}_${frame.top.toInt()}";
    print('[main] Checking screen: $identifier vs saved: $savedIdentifier');
    
    if (identifier == savedIdentifier) {
      selectedScreen = screen;
      print('[main] Found matching screen: $identifier');
      break;
    }
  }
  
  if (selectedScreen == null) {
    print('[main] Saved screen not found in available screens, using default');
    return; // Screen no longer available
  }
  
  final targetRect = selectedScreen.frame;
  print('[main] Moving window to: left=${targetRect.left}, top=${targetRect.top}, width=${targetRect.width}, height=${targetRect.height}');
  
  // Apply the same window positioning sequence as in settings_screen
  await windowManager.setFullScreen(false);
  await windowManager.restore();
  
  // Wait to ensure window is ready
  await Future.delayed(const Duration(milliseconds: 300));
  
  try {
    // Use window_size direct API
    window_size.setWindowFrame(
      Rect.fromLTWH(targetRect.left.toDouble(), targetRect.top.toDouble(), 
                    targetRect.width, targetRect.height)
    );
    print('[main] Window positioned using window_size.setWindowFrame');
  } catch (e) {
    print('[main] Error with window_size: $e, falling back to windowManager');
    await windowManager.setBounds(
      Rect.fromLTWH(targetRect.left.toDouble(), targetRect.top.toDouble(), 
                    targetRect.width, targetRect.height),
      animate: false,
    );
  }
  
  // Ensure window is visible
  await windowManager.show();
  await windowManager.focus();
  print('[main] Window should now be visible on selected monitor');
}

class DeskVizApp extends StatelessWidget {
  const DeskVizApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'DeskViz',
      theme: ThemeData.dark(),
      initialRoute: '/',
      routes: {
        '/': (context) => const HomeScreenWrapper(),
        '/settings': (context) => const SettingsScreen(),
      },
      debugShowCheckedModeBanner: false,
    );
  }
}

class DeskVizDashboard extends StatelessWidget {
  const DeskVizDashboard({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('DeskViz Dashboard'),
        actions: [
          IconButton(
            icon: const Icon(Icons.settings),
            tooltip: 'Settings',
            onPressed: () {
              Navigator.of(context).push(
                MaterialPageRoute(builder: (context) => const SettingsScreen()),
              );
            },
          ),
        ],
      ),
      body: Center(
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: const [
            CpuWidget(widgetId: 'cpu_widget_main'),
            SizedBox(width: 24),
            RamWidget(),
          ],
        ),
      ),
    );
  }
}

class HomeScreenWrapper extends StatefulWidget {
  const HomeScreenWrapper({Key? key}) : super(key: key);

  @override
  State<HomeScreenWrapper> createState() => _HomeScreenWrapperState();
}

class _HomeScreenWrapperState extends State<HomeScreenWrapper> with SingleTickerProviderStateMixin {
  bool _showSettings = false;
  late AnimationController _controller;
  late Animation<double> _animation;
  double _startDragY = 0;
  double _dragDistance = 0;
  static const double _dragThreshold = 30.0; // Minimum distance to trigger panel
  static const double _topAreaHeight = 150.0; // Top area for gesture detection
  bool _isDragging = false;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      duration: const Duration(milliseconds: 400),
      vsync: this,
    );
    _animation = Tween<double>(begin: -250.0, end: 0.0).animate(
      CurvedAnimation(
        parent: _controller,
        curve: Curves.easeOut,
      ),
    );
    
    print('[HomeScreenWrapper] Initialized with swipe-down gesture support');
    
    // Add a swipe notification after widget is built
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (kDebugMode) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Swipe down from the top to show settings'),
            duration: Duration(seconds: 5),
          ),
        );
      }
    });
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _toggleSettings() {
    print('[HomeScreenWrapper] Toggling settings panel');
    setState(() {
      _showSettings = !_showSettings;
    });

    if (_showSettings) {
      _controller.forward();
      // Auto-hide after 10 seconds
      Future.delayed(const Duration(seconds: 10), () {
        if (mounted && _showSettings) {
          _hideSettings();
        }
      });
    } else {
      _controller.reverse();
    }
  }

  void _hideSettings() {
    setState(() {
      _showSettings = false;
    });
    _controller.reverse();
  }

  @override
  Widget build(BuildContext context) {
    print('[HomeScreenWrapper] Building HomeScreenWrapper widget');
    
    // Create a minimal swipe indicator (a small handle at the top center)
    Widget swipeIndicator = Positioned(
      top: 5,
      left: 0,
      right: 0,
      child: Center(
        child: Container(
          width: 50,
          height: 5,
          decoration: BoxDecoration(
            color: Colors.white.withOpacity(0.4),
            borderRadius: BorderRadius.circular(10),
            boxShadow: [
              BoxShadow(
                color: Colors.white.withOpacity(0.1),
                blurRadius: 3,
                spreadRadius: 1,
              ),
            ],
          ),
        ),
      ),
    );
    
    return GestureDetector( 
      // Track vertical drag gestures
      onVerticalDragStart: (details) {
        _isDragging = true;
        _startDragY = details.globalPosition.dy;
        print('[HomeScreenWrapper] Drag started at $_startDragY');
      },
      onVerticalDragUpdate: (details) {
        if (!_isDragging) return;
        
        _dragDistance = details.globalPosition.dy - _startDragY;
        print('[HomeScreenWrapper] Drag distance: $_dragDistance');
        
        // Only activate if drag starts near the top of the screen (iPhone style)
        if (_startDragY < _topAreaHeight && _dragDistance > 0 && !_showSettings && _dragDistance > _dragThreshold) {
          _toggleSettings();
          _isDragging = false; // Prevent multiple activations
        }
      },
      onVerticalDragEnd: (details) {
        // Reset drag state
        _dragDistance = 0;
        _isDragging = false;
        print('[HomeScreenWrapper] Drag ended');
      },
      onVerticalDragCancel: () {
        // Handle drag cancellation
        _dragDistance = 0;
        _isDragging = false;
        print('[HomeScreenWrapper] Drag canceled');
      },
      // Also support tap for easier testing
      onTap: () {
        print('[HomeScreenWrapper] Screen tapped at y=${_startDragY}');
        if (_startDragY < _topAreaHeight) {
          print('[HomeScreenWrapper] Top area tapped, showing settings');
          _toggleSettings();
        }
      },
      behavior: HitTestBehavior.translucent, // Important to capture taps anywhere
      child: Scaffold(
        body: Focus(
          autofocus: true,
          onKeyEvent: (node, event) {
            print('[HomeScreenWrapper] onKeyEvent ENTERED. Event: ${event.runtimeType}, Key: ${event.logicalKey}'); // Check if callback runs
            if (event is KeyDownEvent && event.logicalKey == LogicalKeyboardKey.keyX) {
              print('[HomeScreenWrapper] X key pressed, exiting app.');
              windowManager.destroy();
              return KeyEventResult.handled;
            }
            return KeyEventResult.ignored;
          },
          child: Stack(
            children: [
              HomeScreen(), // Remove 'const'
              // Minimal swipe indicator
              swipeIndicator,
              if (_showSettings)
                // Full screen tap detector to close settings when tapping outside
                GestureDetector(
                  onTap: _hideSettings, // Close settings when tapping outside the panel
                  behavior: HitTestBehavior.translucent,
                  child: Container(
                    color: Colors.transparent,
                    width: double.infinity,
                    height: double.infinity,
                  ),
                ),
                // Settings panel
                AnimatedBuilder(
                  animation: _animation,
                  builder: (context, child) {
                    return Positioned(
                      top: _animation.value,
                      left: 0,
                      right: 0,
                      child: child!,
                    );
                  },
                  child: GestureDetector(
                    onTap: () => {}, // Prevent taps on panel from closing it
                    child: Container(
                      height: 200,
                      decoration: BoxDecoration(
                        color: Colors.black.withOpacity(0.9),
                        borderRadius: const BorderRadius.only(
                          bottomLeft: Radius.circular(16),
                          bottomRight: Radius.circular(16),
                        ),
                        boxShadow: [
                          BoxShadow(
                            color: Colors.white.withOpacity(0.2),
                            blurRadius: 8,
                            spreadRadius: 1,
                          ),
                        ],
                      ),
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              const Text(
                                'Settings', 
                                style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: Colors.white)
                              ),
                              IconButton(
                                icon: const Icon(Icons.close, color: Colors.white),
                                onPressed: _hideSettings, // Close panel
                              ),
                            ],
                          ),
                          const SizedBox(height: 16),
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                            children: [
                              _buildSettingsButton(
                                icon: Icons.desktop_windows,
                                label: 'Display',
                                onTap: () {
                                  print('[SettingsPanel] Navigating to Display Settings');
                                  _hideSettings(); // Hide panel before navigating
                                  Navigator.pushNamed(context, '/settings');
                                },
                              ),
                              _buildSettingsButton(
                                icon: Icons.widgets, 
                                label: 'Widgets',
                                onTap: () async {
                                  print('[SettingsPanel] Navigating to Widget Settings');
                                  _hideSettings(); // Hide panel before navigating
                                  final result = await Navigator.push<bool>(
                                    context,
                                    MaterialPageRoute(builder: (context) => const WidgetSettingsScreen()),
                                  );
                                  print('[SettingsPanel] Returned from Widget Settings with result: $result');
                                  // If changes were made in WidgetSettingsScreen, trigger rebuild
                                  if (result == true) {
                                    setState(() {}); // Rebuild HomeScreenWrapper, which rebuilds HomeScreen
                                  }
                                },
                              ),
                              _buildSettingsButton(
                                icon: Icons.refresh,
                                label: 'Refresh',
                                onTap: () {
                                  print('[SettingsPanel] Refresh pressed');
                                  // Force HomeScreen to refresh by rebuilding
                                  Navigator.pushReplacement(
                                    context,
                                    MaterialPageRoute(
                                      builder: (context) => const DeskVizApp(),
                                    ),
                                  );
                                  _hideSettings();
                                },
                              ),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildSettingsButton({
    required IconData icon,
    required String label,
    required VoidCallback onTap,
  }) {
    return GestureDetector(
      onTap: onTap,
      behavior: HitTestBehavior.opaque, // Ensure the button is tappable
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: BoxDecoration(
          color: Colors.grey[800],
          borderRadius: BorderRadius.circular(10),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, color: Colors.white, size: 28),
            const SizedBox(height: 8),
            Text(
              label,
              style: const TextStyle(color: Colors.white),
            ),
          ],
        ),
      ),
    );
  }
}