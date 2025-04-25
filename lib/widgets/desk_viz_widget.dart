import 'package:flutter/material.dart';
import 'orientation_aware.dart';

/// Abstract base class for all DeskViz dashboard widgets.
abstract class DeskVizWidget extends StatelessWidget with OrientationAware {
  const DeskVizWidget({Key? key}) : super(key: key);

  /// Unique key for this widget, used for enable/disable and settings.
  String get widgetKey;

  /// Human-readable name for UI.
  String get displayName;

  /// Called to refresh the widget's data (if needed).
  Future<void> refresh();

  /// Open configuration screen for this widget
  void openConfig(BuildContext context) {
    showDialog(
      context: context,
      builder: (context) => _buildConfigDialog(context),
    );
  }

  /// Build the configuration dialog
  Widget _buildConfigDialog(BuildContext context) {
    return AlertDialog(
      title: Text('Configure $displayName'),
      content: SingleChildScrollView(
        child: buildConfigOptions(context),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancel'),
        ),
        TextButton(
          onPressed: () {
            saveConfig();
            Navigator.of(context).pop();
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text('$displayName configuration saved'),
                duration: const Duration(seconds: 1),
              ),
            );
          },
          child: const Text('Save'),
        ),
      ],
    );
  }

  /// Build configuration options UI 
  Widget buildConfigOptions(BuildContext context) {
    return const Center(
      child: Text('No configurable options for this widget'),
    );
  }

  /// Save widget configuration to SharedPreferences
  Future<void> saveConfig() async {
    // Override in subclasses to save specific settings
  }

  /// Load widget configuration from SharedPreferences
  Future<void> loadConfig() async {
    // Override in subclasses to load specific settings
  }

  /// Get a preference key specific to this widget
  String getPreferenceKey(String setting) {
    return 'widget_config_${widgetKey}_$setting';
  }

  /// Returns the widget's settings UI, or null if none.
  Widget buildSettings(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Text('$displayName Settings', 
            style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
          const SizedBox(height: 16),
          ElevatedButton.icon(
            icon: const Icon(Icons.settings),
            label: const Text('Configure Widget'),
            onPressed: () => openConfig(context),
          ),
        ],
      ),
    );
  }
}
