import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'dart:async';
import 'dart:math';
import 'desk_viz_widget.dart';
import '../services/system_info_service.dart';

/// A configurable disk usage widget that shows space usage for multiple drives
class DiskWidget extends DeskVizWidget {
  const DiskWidget({Key? key}) : super(key: key);

  @override
  String get widgetKey => 'disk_widget';

  @override
  String get displayName => 'Disk Usage Monitor';

  @override
  Future<void> refresh() async {
    // Refresh is handled in the state class
  }
  
  @override
  Widget buildConfigOptions(BuildContext context) {
    return const _DiskWidgetConfigForm();
  }

  @override
  Widget build(BuildContext context) {
    return const _DiskWidgetContent();
  }
}

// Configuration form for disk widget
class _DiskWidgetConfigForm extends StatefulWidget {
  const _DiskWidgetConfigForm({Key? key}) : super(key: key);

  @override
  State<_DiskWidgetConfigForm> createState() => _DiskWidgetConfigFormState();
}

class _DiskWidgetConfigFormState extends State<_DiskWidgetConfigForm> {
  // Map of drive letters to whether they are enabled
  final Map<String, bool> _driveStates = {};
  int _refreshInterval = 30;
  bool _isLoading = true;
  
  // List of common Windows drive letters
  final List<String> _availableDrives = ['C', 'D', 'E', 'F', 'G'];
  
  @override
  void initState() {
    super.initState();
    _loadSettings();
  }
  
  Future<void> _loadSettings() async {
    final prefs = await SharedPreferences.getInstance();
    
    // Initialize all drives to disabled by default
    for (final drive in _availableDrives) {
      _driveStates[drive] = false;
    }
    
    // Load enabled drives
    final enabledDrives = prefs.getStringList('widget_config_disk_widget_enabled_drives');
    if (enabledDrives != null) {
      for (final drive in enabledDrives) {
        if (_driveStates.containsKey(drive)) {
          _driveStates[drive] = true;
        }
      }
    } else {
      // Default to C: drive if no settings exist
      _driveStates['C'] = true;
    }
    
    // Load refresh interval
    final interval = prefs.getInt('widget_config_disk_widget_refresh_interval');
    if (interval != null) {
      _refreshInterval = interval;
    }
    
    setState(() {
      _isLoading = false;
    });
  }
  
  Future<void> saveSettings() async {
    final prefs = await SharedPreferences.getInstance();
    
    // Save enabled drives
    final enabledDrives = _driveStates.entries
        .where((entry) => entry.value) // Only where true
        .map((entry) => entry.key)     // Extract drive letter
        .toList();
    
    await prefs.setStringList('widget_config_disk_widget_enabled_drives', enabledDrives);
    
    // Save refresh interval
    await prefs.setInt('widget_config_disk_widget_refresh_interval', _refreshInterval);
    
    print('[DiskWidget] Saved settings: interval=$_refreshInterval, drives=$enabledDrives');
  }
  
  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    
    return SizedBox(
      width: 300,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Drives to Monitor:',
            style: TextStyle(fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          
          // Drive selection checkboxes
          ..._availableDrives.map((drive) {
            return CheckboxListTile(
              title: Text('$drive: Drive'),
              value: _driveStates[drive] ?? false,
              onChanged: (bool? value) {
                setState(() {
                  _driveStates[drive] = value ?? false;
                });
              },
            );
          }).toList(),
          
          const Divider(),
          const SizedBox(height: 8),
          
          const Text(
            'Refresh Interval:',
            style: TextStyle(fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          Slider(
            value: _refreshInterval.toDouble(),
            min: 5,
            max: 120,
            divisions: 23,
            label: '${_refreshInterval}s',
            onChanged: (value) {
              setState(() {
                _refreshInterval = value.round();
              });
            },
          ),
          Text('Update every $_refreshInterval seconds'),
          const SizedBox(height: 16),
        ],
      ),
    );
  }
}

// The actual widget content that displays disk usage for multiple drives
class _DiskWidgetContent extends StatefulWidget {
  const _DiskWidgetContent();
  
  @override
  _DiskWidgetContentState createState() => _DiskWidgetContentState();
}

class _DiskWidgetContentState extends State<_DiskWidgetContent> {
  final Map<String, Map<String, dynamic>> _driveData = {};
  List<String> _enabledDrives = ['C']; // Default to C:
  int _refreshIntervalSeconds = 30;
  Timer? _timer;
  bool _loading = true;
  
  @override
  void initState() {
    super.initState();
    _loadSettings().then((_) {
      _fetchDiskInfo();
      _startTimer();
    });
  }
  
  Future<void> _loadSettings() async {
    final prefs = await SharedPreferences.getInstance();
    
    // Load enabled drives
    final drives = prefs.getStringList('widget_config_disk_widget_enabled_drives');
    if (drives != null && drives.isNotEmpty) {
      _enabledDrives = drives;
    }
    
    // Load refresh interval
    final interval = prefs.getInt('widget_config_disk_widget_refresh_interval');
    if (interval != null) {
      _refreshIntervalSeconds = interval;
    }
    
    // Initialize data structure for each drive
    for (final drive in _enabledDrives) {
      _driveData[drive] = {
        'name': '$drive:',
        'usage': 0.0,
        'free': 0.0,
        'total': 0.0,
      };
    }
    
    setState(() {
      _loading = false;
    });
  }
  
  void _startTimer() {
    _timer?.cancel();
    _timer = Timer.periodic(
      Duration(seconds: _refreshIntervalSeconds),
      (_) => _fetchDiskInfo()
    );
  }
  
  Future<void> _fetchDiskInfo() async {
    for (final drive in _enabledDrives) {
      try {
        final usage = await SystemInfoService.getDiskUsage(drive: drive);
        final driveInfo = await SystemInfoService.getDriveInfo(drive: drive);
        
        if (mounted) {
          setState(() {
            _driveData[drive] = {
              'name': driveInfo['name'] ?? '$drive:',
              'usage': usage,
              'free': driveInfo['free'] ?? 0.0,
              'total': driveInfo['total'] ?? 0.0,
              'error': null,
            };
          });
        }
      } catch (e) {
        debugPrint('Error fetching disk info for $drive drive: $e');
        if (mounted) {
          setState(() {
            _driveData[drive] = {
              'name': '$drive:',
              'usage': 0.0,
              'free': 0.0,
              'total': 0.0,
              'error': 'Error: Unable to read drive',
            };
          });
        }
      }
    }
  }
  
  String _formatBytes(double bytes, int decimals) {
    if (bytes <= 0) return '0 B';
    const suffixes = ['B', 'KB', 'MB', 'GB', 'TB', 'PB'];
    var i = (log(bytes) / log(1024)).floor();
    return '${(bytes / pow(1024, i)).toStringAsFixed(decimals)} ${suffixes[i]}';
  }
  
  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }
  
  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Card(
        margin: EdgeInsets.all(8),
        child: Padding(
          padding: EdgeInsets.all(16),
          child: Center(child: CircularProgressIndicator()),
        ),
      );
    }
    
    if (_enabledDrives.isEmpty) {
      return Card(
        margin: const EdgeInsets.all(8),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            children: [
              const Text(
                'Disk Usage Monitor',
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 16),
              const Text('No drives selected for monitoring'),
              const SizedBox(height: 16),
              ElevatedButton.icon(
                icon: const Icon(Icons.settings),
                label: const Text('Configure Drives'),
                onPressed: () {
                  // Show the configuration dialog directly
                  showDialog(
                    context: context,
                    builder: (context) => buildConfigDialog(context),
                  );
                },
              ),
            ],
          ),
        ),
      );
    }
    
    return Card(
      elevation: 4,
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'Disk Usage Monitor',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                // Configuration button that uses the standard approach
                IconButton(
                  icon: const Icon(Icons.tune),
                  onPressed: () {
                    // Show the configuration dialog directly
                    showDialog(
                      context: context,
                      builder: (context) => buildConfigDialog(context),
                    );
                  },
                  tooltip: 'Configure disk drives',
                ),
              ],
            ),
            const Divider(),
            
            // Build individual drive cards
            ..._enabledDrives.map((drive) => _buildDriveInfo(drive)).toList(),
            
            Align(
              alignment: Alignment.centerRight,
              child: TextButton.icon(
                icon: const Icon(Icons.refresh),
                label: Text('Refreshes every $_refreshIntervalSeconds sec'),
                onPressed: _fetchDiskInfo,
              ),
            ),
          ],
        ),
      ),
    );
  }
  
  Widget buildConfigDialog(BuildContext context) {
    // Create a GlobalKey to access the config form's state
    final formKey = GlobalKey<_DiskWidgetConfigFormState>();
    
    return AlertDialog(
      title: const Text('Configure Disk Monitoring'),
      content: _DiskWidgetConfigForm(key: formKey),
      contentPadding: const EdgeInsets.fromLTRB(24, 20, 24, 0),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancel'),
        ),
        ElevatedButton(
          onPressed: () {
            // Save settings through the form
            formKey.currentState?.saveSettings();
            
            // Reload settings when dialog is closed and restart timer
            Navigator.of(context).pop();
            _loadSettings().then((_) => _startTimer());
          },
          child: const Text('Apply'),
        ),
      ],
    );
  }
  
  Widget _buildDriveInfo(String drive) {
    if (!_driveData.containsKey(drive)) {
      return const SizedBox.shrink();
    }
    
    final data = _driveData[drive]!;
    final error = data['error'];
    
    if (error != null) {
      return ListTile(
        title: Text('${data['name']}'),
        subtitle: Text(error.toString(), style: const TextStyle(color: Colors.red)),
        leading: const Icon(Icons.error_outline, color: Colors.red),
      );
    }
    
    final usage = data['usage'] as double;
    final free = data['free'] as double;
    final total = data['total'] as double;
    
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            data['name'].toString(),
            style: const TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 4),
          LinearProgressIndicator(
            value: usage / 100,
            backgroundColor: Colors.grey[300],
            valueColor: AlwaysStoppedAnimation<Color>(
              usage > 90 ? Colors.red : 
              usage > 70 ? Colors.orange : 
              Colors.green
            ),
            minHeight: 8,
          ),
          const SizedBox(height: 4),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                '${usage.toStringAsFixed(1)}% Used',
                style: const TextStyle(fontSize: 14),
              ),
              Text(
                '${_formatBytes(free, 1)} free of ${_formatBytes(total, 1)}',
                style: const TextStyle(fontSize: 14),
              ),
            ],
          ),
          const SizedBox(height: 8),
          const Divider(),
        ],
      ),
    );
  }
}
