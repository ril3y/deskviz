import 'dart:async';
import 'package:flutter/material.dart';
import 'desk_viz_widget.dart';
import '../services/system_info_service.dart';

class CpuWidget extends DeskVizWidget {
  final VoidCallback? onConfigPressed;
  final String widgetId;

  const CpuWidget({
    Key? key,
    required this.widgetId,
    this.onConfigPressed,
  }) : super(key: key);

  @override
  String get widgetKey => widgetId;

  @override
  String get displayName => 'CPU Monitor';

  @override
  Future<void> refresh() async {
    // Implement refresh logic if needed
    // This could trigger a data update in CpuWidgetContent
  }

  @override
  Widget build(BuildContext context) {
    return CpuWidgetContent(onConfigPressed: onConfigPressed);
  }
}

class CpuWidgetContent extends StatefulWidget {
  final VoidCallback? onConfigPressed;

  const CpuWidgetContent({Key? key, this.onConfigPressed}) : super(key: key);

  @override
  _CpuWidgetContentState createState() => _CpuWidgetContentState();
}

class _CpuWidgetContentState extends State<CpuWidgetContent> {
  String _cpuName = "Loading CPU info...";
  double _overallCpuUsage = 0.0;
  int _coreCount = 0;
  List<double> _coreCpuUsages = [];
  Timer? _updateTimer;

  // Configurable settings
  int _updateIntervalSeconds = 3;
  bool _showCores = true;
  bool _showOverallUsage = true;
  bool _autoDetectOrientation = true; // Automatically detect screen orientation
  
  // Get effective orientation based on settings and device
  String _getEffectiveOrientation(BuildContext context) {
    if (_autoDetectOrientation) {
      final orientation = MediaQuery.of(context).orientation;
      if (orientation == Orientation.landscape) {
        return 'horizontal-ltr';
      } else {
        return 'vertical';
      }
    }
    return 'vertical'; // Default to vertical as fallback
  }

  @override
  void initState() {
    super.initState();
    // Initialize with real CPU data instead of mock data
    _updateCpuData();
    // Start timer to periodically update real CPU data
    _startDataUpdateTimer();
  }

  @override
  void dispose() {
    _updateTimer?.cancel();
    super.dispose();
  }

  // Start timer to periodically update CPU data
  void _startDataUpdateTimer() {
    _updateTimer = Timer.periodic(
      Duration(seconds: _updateIntervalSeconds),
      (_) => _updateCpuData(), // Call real data method instead of mock
    );
  }

  // Get real CPU data using SystemInfoService
  void _updateCpuData() async {
    try {
      // Get real CPU name
      final cpuName = await SystemInfoService.getCpuName();
      
      // Get overall CPU usage
      final overallUsage = await SystemInfoService.getCpuUsage();
      
      // Get per-core CPU usage
      final coreUsages = await SystemInfoService.getCpuCoreUsage();
      
      // Update UI with real data
      setState(() {
        _cpuName = cpuName.isNotEmpty ? cpuName : "Unknown CPU";
        _overallCpuUsage = overallUsage;
        _coreCpuUsages = coreUsages;
        _coreCount = coreUsages.length;
      });
    } catch (e) {
      print('Error updating CPU data: $e');
      // If error occurs, fall back to mock data as a failsafe
      _mockCpuData();
    }
  }

  // Keep mock data as fallback for testing or if real data fails
  void _mockCpuData() {
    // In a real app, this would fetch actual CPU data
    setState(() {
      _cpuName = "Intel Core i7-10700K";
      _overallCpuUsage = 30.0 + (DateTime.now().second % 60) / 2; // Vary between 30-60%
      _coreCount = 8;
      _coreCpuUsages = List.generate(
        _coreCount, 
        (index) => 20.0 + ((index + DateTime.now().second) % 80), // Generate varying values
      );
    });
  }

  @override
  Widget build(BuildContext context) {
    // Theme colors
    final textStyle = Theme.of(context).textTheme;
    
    // Determine the current bar orientation to use
    String effectiveOrientation = _getEffectiveOrientation(context);
    bool isLandscape = effectiveOrientation.startsWith('horizontal');
    
    // Adjust child aspect ratio based on orientation
    double childAspectRatio = isLandscape ? 2.2 : 0.45;
    
    // In landscape mode, use more columns for better horizontal space usage
    int gridColumns = _getCoreGridColumns(isLandscape);
    
    return Card(
      elevation: 4.0,
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // CPU Title and info
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'CPU Usage', 
                  style: textStyle.titleLarge?.copyWith(fontWeight: FontWeight.bold)
                ),
                // Config button
                if (widget.onConfigPressed != null)
                  IconButton(
                    icon: const Icon(Icons.settings, size: 20),
                    onPressed: widget.onConfigPressed,
                    tooltip: 'Configure CPU Widget',
                    padding: EdgeInsets.zero,
                    constraints: const BoxConstraints(),
                  ),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              _cpuName,
              style: textStyle.bodySmall?.copyWith(color: Colors.grey),
              overflow: TextOverflow.ellipsis,
            ),
            const SizedBox(height: 12),
            
            // Overall CPU usage
            if (_showOverallUsage) ...[
              Row(
                children: [
                  Expanded(
                    flex: 3,
                    child: LinearProgressIndicator(
                      value: _overallCpuUsage / 100.0,
                      valueColor: AlwaysStoppedAnimation<Color>(_getColorForUsage(_overallCpuUsage)),
                      backgroundColor: Colors.grey.shade200,
                      minHeight: 10,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Text(
                    '${_overallCpuUsage.toStringAsFixed(1)}%',
                    style: textStyle.titleMedium?.copyWith(fontWeight: FontWeight.bold),
                  ),
                ],
              ),
              const SizedBox(height: 16),
            ],
            
            // Individual cores
            if (_showCores && _coreCount > 0) ...[
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    'Logical Processors',
                    style: textStyle.titleSmall,
                  ),
                  Text(
                    '$_coreCount cores',
                    style: textStyle.bodySmall?.copyWith(color: Colors.grey),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              
              // Grid of cores - always use Expanded to fill available space
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.all(4.0),
                  child: GridView.builder(
                    gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: gridColumns,
                      crossAxisSpacing: 6.0,
                      mainAxisSpacing: 6.0,
                      childAspectRatio: childAspectRatio,
                    ),
                    itemCount: _coreCount,
                    // Use scroll physics based on core count
                    physics: _coreCount > 32 
                      ? const AlwaysScrollableScrollPhysics() 
                      : const NeverScrollableScrollPhysics(),
                    itemBuilder: (context, index) {
                      final usage = index < _coreCpuUsages.length 
                          ? _coreCpuUsages[index] 
                          : 0.0;
                      
                      return _buildCoreItem(index, usage, effectiveOrientation);
                    },
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
  
  // Determine how many columns to use based on core count and orientation
  int _getCoreGridColumns([bool isLandscape = false]) {
    if (isLandscape) {
      // More columns for landscape orientation
      if (_coreCount <= 4) return 4;
      if (_coreCount <= 8) return 8;
      if (_coreCount <= 16) return 8;
      if (_coreCount <= 32) return 16;
      return 16; // For extremely high-core count systems
    } else {
      // Fewer columns for portrait orientation
      if (_coreCount <= 4) return 2;
      if (_coreCount <= 8) return 4;
      if (_coreCount <= 16) return 4;
      if (_coreCount <= 32) return 8;
      return 12;
    }
  }
  
  // Build individual core visualization
  Widget _buildCoreItem(int coreIndex, double usage, String orientation) {
    final color = _getColorForUsage(usage);
    
    if (orientation == 'vertical') {
      return Container(
        padding: const EdgeInsets.all(2),
        decoration: BoxDecoration(
          color: Colors.grey.shade900,
          borderRadius: BorderRadius.circular(3.0),
        ),
        child: Column(
          children: [
            // Core number
            Padding(
              padding: const EdgeInsets.only(top: 1.0, bottom: 2.0),
              child: Text(
                'CPU $coreIndex',
                style: const TextStyle(fontSize: 10, color: Colors.grey, fontWeight: FontWeight.bold),
              ),
            ),
            // Vertical bar with solid fill
            Expanded(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 1),
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(2),
                  child: Container(
                    color: Colors.grey.shade800,
                    child: LayoutBuilder(
                      builder: (context, constraints) {
                        return Stack(
                          children: [
                            Positioned(
                              bottom: 0,
                              left: 0,
                              right: 0,
                              child: AnimatedContainer(
                                duration: const Duration(milliseconds: 300),
                                curve: Curves.easeOutCubic,
                                height: constraints.maxHeight * (usage / 100),
                                color: color,
                              ),
                            ),
                            // Percentage label
                            Positioned(
                              top: 2,
                              right: 2,
                              child: Text(
                                '${usage.toInt()}%',
                                style: const TextStyle(
                                  fontSize: 8,
                                  color: Colors.white70,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ),
                          ],
                        );
                      },
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      );
    } else {
      // Horizontal bar (LTR or RTL)
      bool isRtl = orientation == 'horizontal-rtl';
      return Container(
        padding: const EdgeInsets.all(2),
        decoration: BoxDecoration(
          color: Colors.grey.shade900,
          borderRadius: BorderRadius.circular(3.0),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Core number and usage percentage
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'CPU $coreIndex',
                  style: const TextStyle(fontSize: 10, color: Colors.grey, fontWeight: FontWeight.bold),
                ),
                Text(
                  '${usage.toInt()}%',
                  style: const TextStyle(fontSize: 9, color: Colors.grey),
                ),
              ],
            ),
            const SizedBox(height: 2),
            // Horizontal bar
            Expanded(
              child: Padding(
                padding: const EdgeInsets.symmetric(vertical: 1),
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(2),
                  child: Container(
                    color: Colors.grey.shade800,
                    child: LayoutBuilder(
                      builder: (context, constraints) {
                        return Stack(
                          children: [
                            Positioned(
                              top: 0,
                              bottom: 0,
                              // Position based on RTL or LTR
                              right: isRtl ? 0 : null,
                              left: isRtl ? null : 0,
                              child: AnimatedContainer(
                                duration: const Duration(milliseconds: 300),
                                curve: Curves.easeOutCubic,
                                width: constraints.maxWidth * (usage / 100),
                                color: color,
                              ),
                            ),
                          ],
                        );
                      },
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      );
    }
  }
  
  // Get color based on usage percentage (green->yellow->red gradient)
  Color _getColorForUsage(double usage) {
    if (usage < 50) {
      // Green to yellow gradient (0-50%)
      return Color.lerp(
        Colors.green,
        Colors.yellow,
        usage / 50,
      )!;
    } else {
      // Yellow to red gradient (50-100%)
      return Color.lerp(
        Colors.yellow,
        Colors.red,
        (usage - 50) / 50,
      )!;
    }
  }
}
