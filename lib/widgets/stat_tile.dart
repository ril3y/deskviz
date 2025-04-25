import 'package:flutter/material.dart';
import 'desk_viz_widget.dart';

class StatTile extends DeskVizWidget {
  final String label;
  final String value;
  final double percentage; // Expected value between 0.0 and 1.0
  final Color progressColor;
  final IconData icon;
  final String widgetIdentifier;
  final String displayText;

  const StatTile({
    Key? key,
    required this.label,
    required this.value,
    required this.percentage,
    required this.progressColor,
    required this.icon,
    required this.widgetIdentifier,
    this.displayText = '',
  }) : super(key: key);

  @override
  String get widgetKey => widgetIdentifier;

  @override
  String get displayName => displayText.isNotEmpty ? displayText : label;

  @override
  Future<void> refresh() async {
    // This is handled by the parent HomeScreen widget
    return;
  }

  @override
  Widget buildConfigOptions(BuildContext context) {
    // Override in subclasses with specific config options
    return const Center(
      child: Text('No configurable options available'),
    );
  }

  @override
  Widget build(BuildContext context) {
    // Clamp percentage value to be safe
    final clampedPercentage = percentage.clamp(0.0, 1.0);

    return Card(
      elevation: 2.0,
      child: Padding(
        padding: const EdgeInsets.all(12.0),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.spaceBetween, // Distribute space
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  label,
                  style: Theme.of(context).textTheme.titleSmall?.copyWith(color: Colors.grey[400]),
                ),
                Icon(icon, size: 20.0, color: Colors.grey[500]),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              value,
              style: Theme.of(context).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            LinearProgressIndicator(
              value: clampedPercentage,
              backgroundColor: Colors.grey[700], // Background for the progress bar
              valueColor: AlwaysStoppedAnimation<Color>(progressColor),
              minHeight: 6.0, // Make the progress bar a bit thicker
            ),
          ],
        ),
      ),
    );
  }
}