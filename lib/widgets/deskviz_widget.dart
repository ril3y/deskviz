// lib/widgets/deskviz_widget.dart
import 'package:flutter/material.dart';
import 'orientation_aware.dart';

// Abstract base class for all dashboard widgets
abstract class DeskVizWidget extends StatelessWidget with OrientationAware {
  // Common properties could go here (e.g., widget ID, enabled status)
  final String widgetId;
  final bool isEnabled;

  const DeskVizWidget({
    Key? key,
    required this.widgetId,
    this.isEnabled = true, // Default to enabled
  }) : super(key: key);

  // Each specific widget *must* implement this method to build its UI
  Widget buildWidget(BuildContext context);

  // The main build method delegates to the specific implementation
  @override
  Widget build(BuildContext context) {
    if (!isEnabled) {
      return const SizedBox.shrink(); // Don't display if disabled
    }
    return buildWidget(context);
  }

  // We might add abstract methods for data updates later if needed
  // Future<void> updateData();
}
