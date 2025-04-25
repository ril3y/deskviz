import 'package:flutter/material.dart';
import 'dart:async';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:system_info2/system_info2.dart';
import 'desk_viz_widget.dart';
import '../services/system_info_service.dart';

class RamWidget extends DeskVizWidget {
  const RamWidget({Key? key}) : super(key: key);

  @override
  String get widgetKey => 'ram_widget';

  @override
  String get displayName => 'RAM Usage';

  @override
  Future<void> refresh() async {
    // Refresh handled in the StatefulWidget
  }

  @override
  Widget build(BuildContext context) {
    return const RamWidgetContent();
  }
  
  @override
  Widget buildConfigOptions(BuildContext context) {
    return const _RamWidgetConfigForm();
  }
}

class RamWidgetContent extends StatefulWidget {
  const RamWidgetContent({Key? key}) : super(key: key);

  @override
  _RamWidgetContentState createState() => _RamWidgetContentState();
}

class _RamWidgetContentState extends State<RamWidgetContent> {
  double _ramUsagePercent = 0.0;
  double _totalRamGB = 0.0;
  double _usedRamGB = 0.0;
  Timer? _updateTimer;

  // Configurable settings
  int _updateIntervalSeconds = 3;
  bool _showDetails = true;
  bool _showGBValues = true;
  
  // Color thresholds
  double _mediumThreshold = 50.0;
  double _highThreshold = 80.0;
  
  // Color mapping
  Map<String, Color> _usageColorMapping = {
    'low': Colors.green,
    'medium': Colors.orange,
    'high': Colors.red
  };

  @override
  void initState() {
    super.initState();
    _loadSettings();
    _fetchRamData();
    _startUpdateTimer();
  }

  @override
  void dispose() {
    _updateTimer?.cancel();
    super.dispose();
  }

  // Load settings
  Future<void> _loadSettings() async {
    final prefs = await SharedPreferences.getInstance();
    
    setState(() {
      _updateIntervalSeconds = prefs.getInt('widget_config_ram_update_interval') ?? 3;
      _showDetails = prefs.getBool('widget_config_ram_show_details') ?? true;
      _showGBValues = prefs.getBool('widget_config_ram_show_gb_values') ?? true;
      _mediumThreshold = prefs.getDouble('widget_config_ram_medium_threshold') ?? 50.0;
      _highThreshold = prefs.getDouble('widget_config_ram_high_threshold') ?? 80.0;
    });
    
    print('[RamWidget] Loaded settings: interval=$_updateIntervalSeconds, showDetails=$_showDetails');
  }
  
  // Start the update timer
  void _startUpdateTimer() {
    // Cancel any existing timer
    _updateTimer?.cancel();
    
    // Start a new timer with the configured interval
    _updateTimer = Timer.periodic(
      Duration(seconds: _updateIntervalSeconds),
      (_) => _fetchRamData()
    );
    
    print('[RamWidget] Started update timer with interval: $_updateIntervalSeconds seconds');
  }
  
  // Fetch RAM usage data
  Future<void> _fetchRamData() async {
    try {
      // Get RAM usage percentage
      final ramUsage = await SystemInfoService.getRamUsage();
      
      // Calculate total and used RAM in GB
      final totalBytes = SysInfo.getTotalPhysicalMemory();
      final freeBytes = SysInfo.getFreePhysicalMemory();
      final usedBytes = totalBytes - freeBytes;
      
      final totalGB = totalBytes / (1024 * 1024 * 1024);
      final usedGB = usedBytes / (1024 * 1024 * 1024);
      
      if (mounted) {
        setState(() {
          _ramUsagePercent = ramUsage;
          _totalRamGB = totalGB;
          _usedRamGB = usedGB;
        });
      }
    } catch (e) {
      print('[RamWidget] Error fetching RAM data: $e');
    }
  }
  
  // Get color based on memory usage
  Color _getColorForUsage(double usage) {
    if (usage >= _highThreshold) {
      return _usageColorMapping['high']!;
    } else if (usage >= _mediumThreshold) {
      return _usageColorMapping['medium']!;
    } else {
      return _usageColorMapping['low']!;
    }
  }

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    final usageColor = _getColorForUsage(_ramUsagePercent);
    
    return Card(
      elevation: 4.0,
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Memory Usage Title
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Memory Usage', 
                  style: textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold)
                ),
                // Configuration button
                IconButton(
                  icon: const Icon(Icons.tune),
                  onPressed: () {
                    // Show configuration dialog directly
                    showDialog(
                      context: context,
                      builder: (context) => buildConfigDialog(context),
                    );
                  },
                  tooltip: 'Configure memory monitoring',
                ),
              ],
            ),
            const SizedBox(height: 16),
            
            // Memory usage percentage
            Row(
              children: [
                Expanded(
                  flex: 3,
                  child: LinearProgressIndicator(
                    value: _ramUsagePercent / 100.0,
                    valueColor: AlwaysStoppedAnimation<Color>(usageColor),
                    backgroundColor: Colors.grey.shade200,
                    minHeight: 12,
                  ),
                ),
                const SizedBox(width: 12),
                Text(
                  '${_ramUsagePercent.toStringAsFixed(1)}%',
                  style: textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
                ),
              ],
            ),
            
            // Memory details
            if (_showDetails) ...[
              const SizedBox(height: 16),
              
              // RAM values in GB
              if (_showGBValues) ...[
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      'Used: ${_usedRamGB.toStringAsFixed(1)} GB',
                      style: textTheme.bodyMedium,
                    ),
                    Text(
                      'Total: ${_totalRamGB.toStringAsFixed(1)} GB',
                      style: textTheme.bodyMedium,
                    ),
                  ],
                ),
              ],
              
              // Memory breakdown visualization
              const SizedBox(height: 12),
              Container(
                height: 24,
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(4.0),
                  border: Border.all(color: Colors.grey.shade300),
                ),
                child: Row(
                  children: [
                    // Used memory
                    Expanded(
                      flex: (_ramUsagePercent).round(),
                      child: Container(
                        decoration: BoxDecoration(
                          color: usageColor,
                          borderRadius: BorderRadius.only(
                            topLeft: Radius.circular(3.0),
                            bottomLeft: Radius.circular(3.0),
                            topRight: _ramUsagePercent >= 99 ? Radius.circular(3.0) : Radius.zero,
                            bottomRight: _ramUsagePercent >= 99 ? Radius.circular(3.0) : Radius.zero,
                          ),
                        ),
                      ),
                    ),
                    
                    // Free memory
                    Expanded(
                      flex: (100 - _ramUsagePercent).round(),
                      child: Container(
                        decoration: BoxDecoration(
                          color: Colors.transparent,
                          borderRadius: BorderRadius.only(
                            topRight: Radius.circular(3.0),
                            bottomRight: Radius.circular(3.0),
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              
              // Labels
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 4.0),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text('Used', style: TextStyle(fontSize: 12, color: Colors.grey.shade600)),
                    Text('Free', style: TextStyle(fontSize: 12, color: Colors.grey.shade600)),
                  ],
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
  
  // Configuration dialog for RAM widget
  Widget buildConfigDialog(BuildContext context) {
    final formKey = GlobalKey<_RamWidgetConfigFormState>();
    
    return AlertDialog(
      title: Text('Configure RAM Widget'),
      content: _RamWidgetConfigForm(key: formKey),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: Text('Cancel'),
        ),
        ElevatedButton(
          onPressed: () {
            // Save settings using the form
            formKey.currentState?.saveSettings();
            
            // Get values from form state
            final newInterval = formKey.currentState?._updateIntervalSeconds ?? _updateIntervalSeconds;
            final showDetails = formKey.currentState?._showDetails ?? _showDetails;
            final showGBValues = formKey.currentState?._showGBValues ?? _showGBValues;
            final mediumThreshold = formKey.currentState?._mediumThreshold ?? _mediumThreshold;
            final highThreshold = formKey.currentState?._highThreshold ?? _highThreshold;
            
            // Apply the changes to this widget
            setState(() {
              _updateIntervalSeconds = newInterval;
              _showDetails = showDetails;
              _showGBValues = showGBValues;
              _mediumThreshold = mediumThreshold;
              _highThreshold = highThreshold;
            });
            
            // Restart timer with new interval
            _startUpdateTimer();
            
            Navigator.pop(context);
          },
          child: Text('Apply'),
        ),
      ],
    );
  }
}

// Configuration form for RAM widget
class _RamWidgetConfigForm extends StatefulWidget {
  const _RamWidgetConfigForm({Key? key}) : super(key: key);

  @override
  State<_RamWidgetConfigForm> createState() => _RamWidgetConfigFormState();
}

class _RamWidgetConfigFormState extends State<_RamWidgetConfigForm> {
  int _updateIntervalSeconds = 3;
  bool _showDetails = true;
  bool _showGBValues = true;
  double _mediumThreshold = 50.0;
  double _highThreshold = 80.0;
  
  @override
  void initState() {
    super.initState();
    _loadSettings();
  }
  
  Future<void> _loadSettings() async {
    final prefs = await SharedPreferences.getInstance();
    setState(() {
      _updateIntervalSeconds = prefs.getInt('widget_config_ram_update_interval') ?? 3;
      _showDetails = prefs.getBool('widget_config_ram_show_details') ?? true;
      _showGBValues = prefs.getBool('widget_config_ram_show_gb_values') ?? true;
      _mediumThreshold = prefs.getDouble('widget_config_ram_medium_threshold') ?? 50.0;
      _highThreshold = prefs.getDouble('widget_config_ram_high_threshold') ?? 80.0;
    });
  }
  
  Future<void> saveSettings() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setInt('widget_config_ram_update_interval', _updateIntervalSeconds);
    await prefs.setBool('widget_config_ram_show_details', _showDetails);
    await prefs.setBool('widget_config_ram_show_gb_values', _showGBValues);
    await prefs.setDouble('widget_config_ram_medium_threshold', _mediumThreshold);
    await prefs.setDouble('widget_config_ram_high_threshold', _highThreshold);
    print('[RamWidget] Saved settings: interval=$_updateIntervalSeconds, showDetails=$_showDetails');
  }
  
  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Update interval
          Text('Update Interval', style: TextStyle(fontWeight: FontWeight.bold)),
          Slider(
            value: _updateIntervalSeconds.toDouble(),
            min: 1,
            max: 10,
            divisions: 9,
            label: '$_updateIntervalSeconds seconds',
            onChanged: (value) {
              setState(() {
                _updateIntervalSeconds = value.round();
              });
            },
          ),
          Text('$_updateIntervalSeconds seconds', style: TextStyle(color: Colors.grey)),
          const SizedBox(height: 16),
          
          // Display options
          Text('Display Options', style: TextStyle(fontWeight: FontWeight.bold)),
          SwitchListTile(
            title: Text('Show Details'),
            subtitle: Text('Show used/total RAM and visualization'),
            value: _showDetails,
            onChanged: (value) {
              setState(() {
                _showDetails = value;
              });
            },
          ),
          SwitchListTile(
            title: Text('Show GB Values'),
            subtitle: Text('Display memory in gigabytes'),
            value: _showGBValues,
            onChanged: (value) {
              setState(() {
                _showGBValues = value;
              });
            },
          ),
          const SizedBox(height: 16),
          
          // Thresholds for color changes
          Text('Color Thresholds', style: TextStyle(fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          Row(
            children: [
              Container(height: 16, width: 16, color: Colors.green),
              Text(' 0% - ${_mediumThreshold.toStringAsFixed(0)}%'),
            ],
          ),
          Row(
            children: [
              Container(height: 16, width: 16, color: Colors.orange),
              Text(' ${_mediumThreshold.toStringAsFixed(0)}% - ${_highThreshold.toStringAsFixed(0)}%'),
            ],
          ),
          Row(
            children: [
              Container(height: 16, width: 16, color: Colors.red),
              Text(' ${_highThreshold.toStringAsFixed(0)}% - 100%'),
            ],
          ),
          const SizedBox(height: 8),
          Text('Medium Usage Threshold', style: TextStyle(fontSize: 14)),
          Slider(
            value: _mediumThreshold,
            min: 20,
            max: 80,
            divisions: 12,
            label: '${_mediumThreshold.toStringAsFixed(0)}%',
            onChanged: (value) {
              setState(() {
                _mediumThreshold = value;
                // Ensure high threshold is greater than medium
                if (_highThreshold <= _mediumThreshold) {
                  _highThreshold = _mediumThreshold + 10;
                }
              });
            },
          ),
          Text('High Usage Threshold', style: TextStyle(fontSize: 14)),
          Slider(
            value: _highThreshold,
            min: _mediumThreshold + 5,
            max: 100,
            divisions: 19,
            label: '${_highThreshold.toStringAsFixed(0)}%',
            onChanged: (value) {
              setState(() {
                _highThreshold = value;
              });
            },
          ),
        ],
      ),
    );
  }
}
