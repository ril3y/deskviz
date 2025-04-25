/// DeskViz API for widgets: system stats, HTTP requests, etc.
/// Extend this class or use static methods for shared functionality.

import 'dart:async';
import 'package:system_info2/system_info2.dart';

class DeskVizApi {
  /// Example: Get CPU usage (stub for now)
  static Future<double> getCpuUsage() async {
    // TODO: Implement real CPU usage
    return 0.0;
  }

  /// Example: Get RAM usage
  static Future<double> getRamUsage() async {
    final total = SysInfo.getTotalPhysicalMemory();
    final free = SysInfo.getFreePhysicalMemory();
    return 1.0 - (free / total);
  }

  // Add more system/API methods as needed
}
