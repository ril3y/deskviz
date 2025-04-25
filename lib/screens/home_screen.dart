import 'package:flutter/material.dart';
import 'dart:async';
import 'package:window_size/window_size.dart' as window_size;
import '../services/system_info_service.dart';
import '../widgets/cpu_widget.dart';
import '../widgets/ram_widget.dart';
import '../widgets/disk_widget.dart';
import '../services/widget_registry.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({Key? key}) : super(key: key);

  @override
  _HomeScreenState createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  String _cpuName = "Loading...";
  bool _isPortrait = false;
  final WidgetRegistry _registry = WidgetRegistry();

  Timer? _timer;

  @override
  void initState() {
    super.initState();
    _detectOrientation();
    _initializeWidgets();
  }

  /// Initialize widgets and load data
  Future<void> _initializeWidgets() async {
    // Register widgets first
    await _registerWidgets();
    
    // Then fetch initial stats
    await _fetchInitialStats();
    
    // Start timer for live updates
    _timer = Timer.periodic(Duration(seconds: 3), (Timer t) => _fetchLiveStats());
    
    // Force UI update
    if (mounted) setState(() {});
  }

  /// Register all available widgets with the registry
  Future<void> _registerWidgets() async {
    // Register CPU widget
    _registry.registerWidget(
      key: 'cpu_usage',
      displayName: 'CPU Usage',
      builder: () => const CpuWidget(widgetId: 'cpu_usage'),
    );

    // Register RAM widget
    _registry.registerWidget(
      key: 'ram_usage',
      displayName: 'Memory Usage',
      builder: () => const RamWidget(),
    );

    // Register multi-drive disk widget
    _registry.registerWidget(
      key: 'disk_widget', 
      displayName: 'Disk Usage Monitor',
      builder: () => const DiskWidget(),
    );

    // Load visibility settings from SharedPreferences
    await _registry.loadSettings();
    print('[HomeScreen] Widget settings loaded: ${_registry.visibleWidgetKeys}');
  }

  @override
  void dispose() {
    // Cancel the timer when the widget is removed
    _timer?.cancel();
    super.dispose();
  }

  Future<void> _detectOrientation() async {
    final screens = await window_size.getScreenList();
    if (screens.isNotEmpty) {
      for (final screen in screens) {
        final frame = screen.frame;
        if (frame.height > frame.width) {
          setState(() {
            _isPortrait = true;
            print('[HomeScreen] Detected portrait display: ${frame.width}x${frame.height}');
          });
          break;
        }
      }
    }
  }

  /// Fetch initial system stats
  Future<void> _fetchInitialStats() async {
    try {
      _cpuName = await SystemInfoService.getCpuName();
      if (mounted) setState(() {});
    } catch (e) {
      print('[HomeScreen] Error fetching initial stats: $e');
    }
  }

  /// Update system stats on a timer
  void _fetchLiveStats() {
    // CPU and RAM updates are now handled by their individual widgets
    // We don't need to update them here anymore
  }

  @override
  Widget build(BuildContext context) {
    // Ensure the tapping works by making the widget take up the full screen
    return GestureDetector(
      onTap: () {
        print('[HomeScreen] Screen tapped');
      },
      behavior: HitTestBehavior.translucent, // Important to capture taps anywhere
      child: Scaffold(
        backgroundColor: Colors.black,
        body: SafeArea(
          child: Padding(
            padding: const EdgeInsets.all(8.0),
            child: _isPortrait ? _buildPortraitLayout() : _buildLandscapeLayout(),
          ),
        ),
      ),
    );
  }

  // Portrait layout for displays that are taller than wide
  Widget _buildPortraitLayout() {
    final visibleWidgets = _registry.getVisibleWidgets();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Header Section
        Text(
          _cpuName,
          style: TextStyle(fontSize: 16, color: Colors.white70),
          overflow: TextOverflow.ellipsis,
        ),
        const SizedBox(height: 8),
        const Divider(color: Colors.grey, height: 8),

        // Stats in a column (stacked) for portrait mode
        Expanded(
          child: visibleWidgets.isEmpty
              ? Center(child: Text('No widgets enabled', style: TextStyle(color: Colors.white70)))
              : Column(
                  mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: _buildPortraitWidgets(visibleWidgets),
                ),
        ),
      ],
    );
  }

  // Build a list of widgets for portrait layout
  List<Widget> _buildPortraitWidgets(List<dynamic> widgets) {
    final List<Widget> result = [];

    for (var i = 0; i < widgets.length; i++) {
      // Add the widget wrapped in an Expanded
      result.add(Expanded(child: widgets[i]));

      // Add spacing between widgets (except after the last one)
      if (i < widgets.length - 1) {
        result.add(const SizedBox(height: 8.0));
      }
    }

    return result;
  }

  // Landscape layout for displays that are wider than tall
  Widget _buildLandscapeLayout() {
    final visibleWidgets = _registry.getVisibleWidgets();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 8.0),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Expanded(
                flex: 2,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      _cpuName,
                      style: TextStyle(fontSize: 18, color: Colors.white70),
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 4.0),
                  ],
                ),
              ),
            ],
          ),
        ),
        const Divider(color: Colors.grey, height: 16),

        // The main row of stat tiles
        Expanded(
          child: visibleWidgets.isEmpty
              ? Center(child: Text('No widgets enabled', style: TextStyle(color: Colors.white70)))
              : Row(
                  mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: _buildLandscapeWidgets(visibleWidgets),
                ),
        ),
      ],
    );
  }

  // Build a list of widgets for landscape layout
  List<Widget> _buildLandscapeWidgets(List<dynamic> widgets) {
    final List<Widget> result = [];

    for (var i = 0; i < widgets.length; i++) {
      // Add the widget wrapped in an Expanded
      result.add(Expanded(child: widgets[i]));

      // Add spacing between widgets (except after the last one)
      if (i < widgets.length - 1) {
        result.add(const SizedBox(width: 8.0));
      }
    }

    return result;
  }
}