import 'package:flutter/material.dart';
import 'package:window_size/window_size.dart' as window_size;
import 'package:shared_preferences/shared_preferences.dart';
import 'package:window_manager/window_manager.dart';
import 'widget_settings_screen.dart';

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({Key? key}) : super(key: key);

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  List<window_size.Screen> _screens = [];
  String? _selectedDisplayIdentifier;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadSettingsAndDisplays();
  }

  Future<void> _loadSettingsAndDisplays() async {
    setState(() => _isLoading = true);
    print('[SettingsScreen] _loadDisplays ENTERED'); // Check if function runs
    final screens = await window_size.getScreenList();
    print('[SettingsScreen] Detected screens:');
    for (int i = 0; i < screens.length; i++) {
      final frame = screens[i].frame;
      final identifier = "${frame.left.toInt()}_${frame.top.toInt()}";
      final isPrimary = frame.left == 0 && frame.top == 0;
      print('  Screen ${i + 1}: $identifier, Frame: $frame, ${isPrimary ? 'Primary' : ''}');
    }
    final prefs = await SharedPreferences.getInstance();
    // Use composite identifier (e.g., "left_top") from frame
    final savedIdentifier = prefs.getString('preferredDisplayIdentifier');
    final availableIdentifiers = screens.map((s) {
      final frame = s.frame;
      return "${frame.left.toInt()}_${frame.top.toInt()}";
    }).toSet();
    // Try to restore saved identifier, ensure it's still available, fallback to first screen's identifier
    String? selectedIdentifier;
    if (savedIdentifier != null && availableIdentifiers.contains(savedIdentifier)) {
      selectedIdentifier = savedIdentifier;
    } else if (screens.isNotEmpty) {
      final firstFrame = screens.first.frame;
      selectedIdentifier = "${firstFrame.left.toInt()}_${firstFrame.top.toInt()}";
    } else {
      // Handle case where no screens
      selectedIdentifier = null;
      print('[SettingsScreen] Warning: Could not determine a default screen identifier.');
    }
    print('[SettingsScreen] Determined selectedIdentifier: $selectedIdentifier (Saved: $savedIdentifier, Available: $availableIdentifiers)');
    setState(() {
      _screens = screens;
      _selectedDisplayIdentifier = selectedIdentifier;
      _isLoading = false;
    });
  }

  Future<void> _applySelection() async {
    if (_selectedDisplayIdentifier == null) return;
    final selectedScreen = _screens.firstWhere((s) {
      final frame = s.frame;
      final identifier = "${frame.left.toInt()}_${frame.top.toInt()}";
      return identifier == _selectedDisplayIdentifier;
    }, orElse: () {
      print('[SettingsScreen] Apply Warning: Saved screen identifier $_selectedDisplayIdentifier not found, using first screen.');
      return _screens.first;
    });

    final targetRect = selectedScreen.frame;
    print('[SettingsScreen] Preparing to apply window bounds: left=${targetRect.left}, top=${targetRect.top}, width=${targetRect.width}, height=${targetRect.height}');
    // Print all screens and which is selected
    for (int i = 0; i < _screens.length; i++) {
      final frame = _screens[i].frame;
      final identifier = "${frame.left.toInt()}_${frame.top.toInt()}";
      final isSelected = identifier == _selectedDisplayIdentifier;
      print('  Screen ${i + 1}: $identifier, Frame: $frame${isSelected ? " <-- SELECTED" : ""}');
    }

    // Print virtual desktop bounds
    double minLeft = double.infinity, minTop = double.infinity, maxRight = double.negativeInfinity, maxBottom = double.negativeInfinity;
    for (final s in _screens) {
      minLeft = s.frame.left < minLeft ? s.frame.left : minLeft;
      minTop = s.frame.top < minTop ? s.frame.top : minTop;
      maxRight = s.frame.right > maxRight ? s.frame.right : maxRight;
      maxBottom = s.frame.bottom > maxBottom ? s.frame.bottom : maxBottom;
    }
    print('[SettingsScreen] Virtual desktop bounds: left=$minLeft, top=$minTop, right=$maxRight, bottom=$maxBottom');

    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('preferredDisplayIdentifier', _selectedDisplayIdentifier!);

    // Add a short delay to ensure the window is ready
    print('[SettingsScreen] Starting window move sequence. First, make window normal state...');
    await windowManager.setFullScreen(false);
    await windowManager.restore(); // Ensure window is neither minimized nor maximized
    await windowManager.hide(); // Hide window before moving it

    // Use a longer delay to ensure window state changes are applied
    await Future.delayed(const Duration(milliseconds: 600));
    print('[SettingsScreen] Setting window bounds...');
    try {
      // Use window_size with direct API call
      window_size.setWindowFrame(
        Rect.fromLTWH(targetRect.left.toDouble(), targetRect.top.toDouble(), targetRect.width, targetRect.height)
      );
      print('[SettingsScreen] Window frame set using window_size.setWindowFrame');
    } catch (e) {
      print('[SettingsScreen] Error with window_size.setWindowFrame: $e');
      // Fall back to windowManager if window_size failed
      await windowManager.setBounds(
        Rect.fromLTWH(targetRect.left.toDouble(), targetRect.top.toDouble(), targetRect.width, targetRect.height),
        animate: false,
      );
      print('[SettingsScreen] Fallback: Window bounds set using windowManager.setBounds');
    }

    // Wait again to ensure bounds are applied before showing window
    await Future.delayed(const Duration(milliseconds: 300));

    print('[SettingsScreen] Making window visible and focused...');
    await windowManager.setAlwaysOnTop(true);
    await windowManager.show();
    await windowManager.focus();

    // Short delay, then remove always-on-top flag
    await Future.delayed(const Duration(milliseconds: 300));
    await windowManager.setAlwaysOnTop(false);
    print('[SettingsScreen] Window should now be visible on selected monitor.');

    // Optionally, re-enable full screen if you want:
    // await windowManager.setFullScreen(true);

    // Optionally, set always on top (if desired)
    // await windowManager.setAlwaysOnTop(true);
    if (mounted) Navigator.of(context).pop();
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    }
    if (_screens.isEmpty) {
      return const Scaffold(
        body: Center(child: Text('No screens detected or error loading.')),
      );
    }
    final uniqueDisplayItems = <DropdownMenuItem<String>>[];
    final addedIdentifiers = <String>{};
    for (int i = 0; i < _screens.length; i++) {
      final screen = _screens[i];
      final frame = screen.frame;
      final identifier = "${frame.left.toInt()}_${frame.top.toInt()}";
      if (addedIdentifiers.add(identifier)) {
        final size = '${frame.width.toInt()}x${frame.height.toInt()}';
        final isPrimary = frame.left == 0 && frame.top == 0;
        String label;
        if (isPrimary) {
          label = 'Primary Display ($size)';
        } else {
          label = 'Display ${i + 1} ($size)';
        }
        uniqueDisplayItems.add(DropdownMenuItem<String>(
          value: identifier,
          child: Text(label),
        ));
      } else {
        print('[SettingsScreen] Skipping screen with duplicate identifier: $identifier');
      }
    }
    final itemValues = uniqueDisplayItems.map((item) => item.value).toList();
    print('[SettingsScreen] Building Dropdown. Initial Selected Identifier: $_selectedDisplayIdentifier, Item Values (Unique): $itemValues');
    String? currentSelectedIdentifier = _selectedDisplayIdentifier;
    if (currentSelectedIdentifier == null || !itemValues.contains(currentSelectedIdentifier)) {
      print('[SettingsScreen] Selected Identifier $currentSelectedIdentifier not found in unique items. Resetting.');
      currentSelectedIdentifier = itemValues.isNotEmpty ? itemValues.first : null;
    }
    if (currentSelectedIdentifier == null || uniqueDisplayItems.isEmpty) {
      print('[SettingsScreen] No valid selection or unique items available. Displaying error.');
      return Scaffold(
        appBar: AppBar(title: Text('Settings')),
        body: const Center(child: Text('Error: Could not load screen list or select a valid screen.')),
      );
    }
    print('[SettingsScreen] Final check before DropdownButton: value=$currentSelectedIdentifier, items=$itemValues');
    return Scaffold(
      appBar: AppBar(title: const Text('Settings')), // Keep const here for the main Scaffold
      body: Padding(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Select Screen:', style: TextStyle(fontSize: 18)),
            const SizedBox(height: 16),
            DropdownButton<String>(
              value: currentSelectedIdentifier,
              items: uniqueDisplayItems,
              onChanged: (String? newValue) async {
                if (newValue != null) {
                  setState(() => _selectedDisplayIdentifier = newValue);
                  // Immediately update saved preference
                  final prefs = await SharedPreferences.getInstance();
                  await prefs.setString('preferredDisplayIdentifier', newValue);
                  print('[SettingsScreen] User selected display: $newValue');
                }
              },
            ),
            const SizedBox(height: 32),
            ElevatedButton(
              onPressed: _applySelection,
              child: const Text('Apply'),
            ),
            const SizedBox(height: 32),
            const Divider(),
            const SizedBox(height: 16),
            const Text('Widget Management:', style: TextStyle(fontSize: 18)),
            const SizedBox(height: 16),
            ElevatedButton.icon(
              icon: const Icon(Icons.widgets),
              label: const Text('Configure Widgets'),
              onPressed: () {
                Navigator.push(
                  context,
                  MaterialPageRoute(builder: (context) => const WidgetSettingsScreen()),
                );
              },
            ),
          ],
        ),
      ),
    );
  }
}
