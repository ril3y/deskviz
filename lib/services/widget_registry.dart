import 'package:shared_preferences/shared_preferences.dart';
import '../widgets/desk_viz_widget.dart';

/// A registry for all available DeskViz widgets.
/// Manages widget registration, visibility settings, and persistence.
class WidgetRegistry {
  static final WidgetRegistry _instance = WidgetRegistry._internal();
  
  // Private constructor
  WidgetRegistry._internal();
  
  // Singleton access
  factory WidgetRegistry() => _instance;
  
  // Maps to store registered widgets and their visibility
  final Map<String, DeskVizWidget Function()> _widgetBuilders = {};
  final Map<String, String> _widgetDisplayNames = {};
  final Map<String, bool> _widgetVisibility = {};
  
  // Store widget order
  List<String> _widgetOrder = [];
  
  // Indicates if visibility settings have been loaded
  bool _initialized = false;
  
  /// Registers a widget with the registry.
  /// 
  /// [key] Unique key for this widget
  /// [displayName] Human-readable name for settings UI
  /// [builder] Function that creates an instance of the widget
  /// [visible] Default visibility (true by default)
  void registerWidget({
    required String key,
    required String displayName,
    required DeskVizWidget Function() builder,
    bool visible = true,
  }) {
    _widgetBuilders[key] = builder;
    _widgetDisplayNames[key] = displayName;
    
    // Only set visibility if not already loaded from preferences
    if (!_initialized) {
      _widgetVisibility[key] = visible;
      
      // Add to default order if not already present
      if (!_widgetOrder.contains(key)) {
        _widgetOrder.add(key);
      }
    }

    print('Widget registered: $key ($displayName), visible=${_widgetVisibility[key]}');
  }
  
  /// Gets a list of all registered widget keys
  List<String> get allWidgetKeys => _widgetBuilders.keys.toList();
  
  /// Gets a map of all widget keys to their display names
  Map<String, String> get widgetDisplayNames => Map.unmodifiable(_widgetDisplayNames);
  
  /// Gets a list of all visible widget keys
  List<String> get visibleWidgetKeys => 
      _widgetBuilders.keys.where((key) => _widgetVisibility[key] == true).toList();
  
  /// Gets a list of all widget keys in their saved order
  List<String> get orderedWidgetKeys {
    // First create a set of all current keys
    final Set<String> currentKeys = _widgetBuilders.keys.toSet();
    
    // Filter _widgetOrder to only include current keys
    final List<String> orderedKeys = _widgetOrder
        .where((key) => currentKeys.contains(key))
        .toList();
    
    // Add any keys that aren't in the order yet
    for (final key in currentKeys) {
      if (!orderedKeys.contains(key)) {
        orderedKeys.add(key);
      }
    }
    
    return orderedKeys;
  }
  
  /// Gets a list of all visible widget keys in their saved order
  List<String> get orderedVisibleWidgetKeys => 
      orderedWidgetKeys.where((key) => _widgetVisibility[key] == true).toList();
  
  /// Gets a list of widget instances for all visible widgets in the saved order
  List<DeskVizWidget> getVisibleWidgets() {
    return orderedVisibleWidgetKeys
        .map((key) => _widgetBuilders[key]!())
        .toList();
  }
  
  /// Gets the visibility status of a widget
  bool isWidgetVisible(String key) => _widgetVisibility[key] ?? true;
  
  /// Sets the visibility of a widget and saves the settings
  Future<void> setWidgetVisibility(String key, bool visible) async {
    if (_widgetBuilders.containsKey(key)) {
      _widgetVisibility[key] = visible;
      await _saveSettings();
      print('[WidgetRegistry] Updated visibility for $key to $visible and saved settings');
    }
  }
  
  /// Updates the order of widgets
  Future<void> updateWidgetOrder(List<String> newOrder) async {
    // Validate that all keys in newOrder are registered
    if (newOrder.every((key) => _widgetBuilders.containsKey(key)) &&
        newOrder.length == _widgetBuilders.length) {
      _widgetOrder = List.from(newOrder);
      await _saveSettings();
      print('[WidgetRegistry] Updated widget order: $_widgetOrder');
      return;
    }
    
    // If there's a mismatch, we need to reconcile the lists
    final Set<String> currentKeys = _widgetBuilders.keys.toSet();
    final Set<String> newOrderKeys = newOrder.toSet();
    
    // Start with the new order
    final List<String> reconciled = List.from(newOrder);
    
    // Add any missing keys to the end
    for (final key in currentKeys) {
      if (!newOrderKeys.contains(key)) {
        reconciled.add(key);
      }
    }
    
    // Remove any keys that aren't registered
    reconciled.removeWhere((key) => !currentKeys.contains(key));
    
    _widgetOrder = reconciled;
    await _saveSettings();
    print('[WidgetRegistry] Reconciled widget order: $_widgetOrder');
  }
  
  /// Reorder a widget in the list (move from oldIndex to newIndex)
  Future<void> reorderWidget(int oldIndex, int newIndex) async {
    if (oldIndex < 0 || 
        oldIndex >= _widgetOrder.length || 
        newIndex < 0 || 
        newIndex > _widgetOrder.length) {
      print('[WidgetRegistry] Invalid indices for reordering: $oldIndex -> $newIndex');
      return;
    }
    
    // Create a copy of the current order
    final List<String> newOrder = List.from(_widgetOrder);
    
    // Get the item to move
    final String item = newOrder.removeAt(oldIndex);
    
    // If newIndex was after oldIndex, it's now shifted one position left
    if (newIndex > oldIndex) {
      newIndex--;
    }
    
    // Insert item at the new position
    newOrder.insert(newIndex, item);
    
    // Update the order
    await updateWidgetOrder(newOrder);
  }
  
  /// Loads widget visibility settings from SharedPreferences
  Future<void> loadSettings() async {
    final prefs = await SharedPreferences.getInstance();
    print('[WidgetRegistry] Loading visibility settings from SharedPreferences');
    
    // Get visibility preferences for all registered widgets
    for (final key in _widgetBuilders.keys) {
      final isVisible = prefs.getBool('widget_visible_$key');
      
      // If no setting exists, default to visible
      if (isVisible == null) {
        _widgetVisibility[key] = true;
        print('[WidgetRegistry] No saved visibility for $key, defaulting to visible');
      } else {
        _widgetVisibility[key] = isVisible;
        print('[WidgetRegistry] Loaded visibility for $key: $isVisible');
      }
    }
    
    // Load widget order
    final List<String>? savedOrder = prefs.getStringList('widget_order');
    if (savedOrder != null && savedOrder.isNotEmpty) {
      // Make sure we include any newly registered widgets
      final Set<String> currentKeys = _widgetBuilders.keys.toSet();
      final Set<String> savedOrderKeys = savedOrder.toSet();
      
      // Start with saved order, filtering out any widgets that no longer exist
      _widgetOrder = savedOrder.where((key) => currentKeys.contains(key)).toList();
      
      // Add any new widgets to the end
      for (final key in currentKeys) {
        if (!savedOrderKeys.contains(key)) {
          _widgetOrder.add(key);
        }
      }
      print('[WidgetRegistry] Loaded widget order: $_widgetOrder');
    } else {
      // If no order saved, use all keys in alphabetical order by display name
      _widgetOrder = _widgetBuilders.keys.toList()
        ..sort((a, b) => (_widgetDisplayNames[a] ?? a).compareTo(_widgetDisplayNames[b] ?? b));
      print('[WidgetRegistry] No saved order found, using alphabetical order: $_widgetOrder');
    }
    
    _initialized = true;
    print('[WidgetRegistry] Widget visibility settings loaded: $_widgetVisibility');
  }
  
  /// Saves widget visibility settings to SharedPreferences
  Future<void> _saveSettings() async {
    final prefs = await SharedPreferences.getInstance();
    print('[WidgetRegistry] Saving widget visibility settings...');
    
    // Save visibility for each widget
    for (final entry in _widgetVisibility.entries) {
      final key = 'widget_visible_${entry.key}';
      final value = entry.value;
      await prefs.setBool(key, value);
      print('[WidgetRegistry] Saved $key = $value');
    }
    
    // Save widget order
    await prefs.setStringList('widget_order', _widgetOrder);
    print('[WidgetRegistry] Saved widget order: $_widgetOrder');
    
    print('[WidgetRegistry] All widget settings saved: $_widgetVisibility');
  }
}
