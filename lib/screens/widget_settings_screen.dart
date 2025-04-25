import 'package:flutter/material.dart';
import '../services/widget_registry.dart';

class WidgetSettingsScreen extends StatefulWidget {
  const WidgetSettingsScreen({Key? key}) : super(key: key);

  @override
  State<WidgetSettingsScreen> createState() => _WidgetSettingsScreenState();
}

class _WidgetSettingsScreenState extends State<WidgetSettingsScreen> {
  final WidgetRegistry _registry = WidgetRegistry();
  final Map<String, bool> _currentVisibility = {};
  bool _loading = true;
  List<String> _widgetKeys = [];
  bool _settingsChanged = false; // Track if any settings were changed

  @override
  void initState() {
    super.initState();
    _loadWidgetSettings();
  }

  Future<void> _loadWidgetSettings() async {
    // Load current widget visibility
    setState(() => _loading = true);

    // Get ordered widget keys
    _widgetKeys = _registry.orderedWidgetKeys;
    
    // Initialize current visibility map
    print('[WidgetSettings] Loading widget settings for ${_widgetKeys.length} widgets');
    for (final key in _widgetKeys) {
      _currentVisibility[key] = _registry.isWidgetVisible(key);
      final isVisible = _currentVisibility[key] ?? false;
      print('[WidgetSettings] Widget $key is ${isVisible ? "visible" : "hidden"}');
    }

    _settingsChanged = false; // Reset change tracker on load
    setState(() => _loading = false);
  }

  Future<void> _saveSettings() async {
    setState(() => _loading = true);

    // Update visibility for each widget
    for (final entry in _currentVisibility.entries) {
      print('[WidgetSettings] Setting ${entry.key} to ${entry.value ? "visible" : "hidden"}');
      await _registry.setWidgetVisibility(entry.key, entry.value);
    }

    // Save widget order
    await _registry.updateWidgetOrder(_widgetKeys);

    setState(() => _loading = false);
    
    // Show confirmation
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Widget settings saved'),
          duration: Duration(seconds: 2),
        ),
      );
    }
    
    // Return to previous screen, indicating if changes were made
    final changed = _settingsChanged; // Capture before resetting
    _settingsChanged = false;
    if (mounted) {
      Navigator.pop(context, changed); // Return true if changes were made
    }
  }

  // Apply a single widget visibility change immediately
  Future<void> _applyWidgetChange(String key, bool value) async {
    final oldValue = _currentVisibility[key];
    if (oldValue != value) {
      _settingsChanged = true; // Mark that a setting has changed
    }
    
    // Update local state
    setState(() {
      _currentVisibility[key] = value;
    });
    
    // Apply change immediately
    print('[WidgetSettings] Immediately applying change: $key = $value');
    await _registry.setWidgetVisibility(key, value);
    
    // Show subtle confirmation
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('${value ? 'Enabled' : 'Disabled'} ${_registry.widgetDisplayNames[key] ?? key}'),
          duration: const Duration(seconds: 1),
          behavior: SnackBarBehavior.floating,
          margin: const EdgeInsets.only(bottom: 10, left: 10, right: 10),
        ),
      );
    }
  }

  void _onReorder(int oldIndex, int newIndex) {
    print('[WidgetSettings] Reordering widget from $oldIndex to $newIndex');
    _settingsChanged = true; // Mark that order changed
    
    setState(() {
      if (newIndex > oldIndex) {
        // If moving down, newIndex is actually -1 due to the removed item
        newIndex -= 1;
      }
      
      // Reorder in our local list
      final String item = _widgetKeys.removeAt(oldIndex);
      _widgetKeys.insert(newIndex, item);
    });
    
    // Save the new order to registry
    _registry.updateWidgetOrder(_widgetKeys);
    
    // Show subtle confirmation
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Widget order updated'),
          duration: Duration(seconds: 1),
          behavior: SnackBarBehavior.floating,
          margin: EdgeInsets.only(bottom: 10, left: 10, right: 10),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return Scaffold(
        appBar: AppBar(title: const Text('Widget Settings')),
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    final widgetNames = _registry.widgetDisplayNames;

    // Use WillPopScope to handle the back button press
    return WillPopScope(
      onWillPop: () async {
        // Pass back whether changes were made when using the back button
        Navigator.pop(context, _settingsChanged);
        // Return false because we handled the pop manually
        return false;
      },
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Widget Settings'),
          actions: [
            IconButton(
              icon: const Icon(Icons.save),
              onPressed: _saveSettings,
              tooltip: 'Save Settings',
            ),
          ],
        ),
        body: Padding(
          padding: const EdgeInsets.all(16.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: const [
                        Text(
                          'Manage Dashboard Widgets',
                          style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                        ),
                        SizedBox(height: 4),
                        Text(
                          'Toggle visibility and drag to reorder widgets',
                          style: TextStyle(fontSize: 14, color: Colors.grey),
                        ),
                      ],
                    ),
                  ),
                  // Help icon with tooltip
                  Tooltip(
                    message: 'Drag the handle (≡) to reorder widgets.\n'
                            'Toggle the switch to show/hide widgets.',
                    child: Icon(Icons.help_outline, color: Colors.blue.shade300),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              
              _widgetKeys.isEmpty 
                  ? const Center(
                      child: Padding(
                        padding: EdgeInsets.all(32.0),
                        child: Text(
                          'No widgets registered. Please restart the app.',
                          style: TextStyle(fontSize: 16),
                        ),
                      ),
                    )
                  : 
              Expanded(
                child: ReorderableListView.builder(
                  onReorder: _onReorder,
                  itemCount: _widgetKeys.length,
                  buildDefaultDragHandles: false, // We'll build our own drag handles
                  itemBuilder: (context, index) {
                    final key = _widgetKeys[index];
                    final name = widgetNames[key] ?? 'Unknown Widget';
                    
                    return Padding(
                      key: ValueKey(key),
                      padding: const EdgeInsets.symmetric(vertical: 2.0),
                      child: Card(
                        elevation: 2.0,
                        margin: EdgeInsets.zero,
                        child: ListTile(
                          leading: ReorderableDragStartListener(
                            index: index,
                            child: Container(
                              padding: const EdgeInsets.all(8.0),
                              child: const Icon(Icons.drag_handle),
                            ),
                          ),
                          title: Text(
                            name,
                            style: const TextStyle(fontWeight: FontWeight.w500),
                          ),
                          subtitle: Text(
                            key,
                            style: TextStyle(fontSize: 12, color: Colors.grey.shade600),
                          ),
                          trailing: Switch(
                            value: _currentVisibility[key] ?? false,
                            onChanged: (value) {
                              print('[WidgetSettings] Toggled $key to: $value');
                              _applyWidgetChange(key, value);
                            },
                          ),
                        ),
                      ),
                    );
                  },
                ),
              ),
              const SizedBox(height: 16),
              Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  TextButton(
                    onPressed: () => Navigator.pop(context),
                    child: const Text('Close'),
                  ),
                  const SizedBox(width: 16),
                  ElevatedButton(
                    onPressed: _saveSettings,
                    child: const Text('Apply & Return'),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
