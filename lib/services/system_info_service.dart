import 'package:system_info2/system_info2.dart';
import 'dart:io';
import 'dart:convert';
import 'dart:async';

// Helper function to run Windows commands and get output
Future<String?> _runCommand(String command, List<String> arguments) async {
  try {
    final process = await Process.run(command, arguments);
    if (process.exitCode == 0) {
      return process.stdout.toString();
    } else {
      print('Command failed: $command ${arguments.join(' ')}\nError: ${process.stderr}');
      return null;
    }
  } catch (e) {
    print('Error running command $command: $e');
    return null;
  }
}


class SystemInfoService {
  // Get CPU usage percentage (requires running a command on Windows)
  // Note: wmic is deprecated. PowerShell or Win32 APIs are better long term.
  // This is a common simple approach but can be unreliable.
  static Future<double> getCpuUsage() async {
    try {
      // Use PowerShell instead of wmic for more reliable results
      final processResult = await Process.run(
        'powershell', [
          '-Command',
          '(Get-Counter "\\Processor(_Total)\\% Processor Time").CounterSamples.CookedValue'
        ],
        runInShell: true,
      ).timeout(Duration(seconds: 5)); // Add a generous timeout

      if (processResult.exitCode == 0) {
        String output = processResult.stdout.toString().trim();
        double? percentage = double.tryParse(output);
        if (percentage != null) {
          return percentage;
        }
        print('Failed to parse CPU load from PowerShell. Output: $output');
      } else {
        print('Failed to get CPU load from PowerShell. ExitCode: ${processResult.exitCode}\nError: ${processResult.stderr}');
        
        // Fallback to older wmic approach
        return _getWmicCpuUsage();
      }
    } on TimeoutException {
      print('Timeout getting CPU load from PowerShell.');
      return _getWmicCpuUsage(); // Try fallback method
    } catch (e) {
      print('Error getting CPU load: $e');
      return _getWmicCpuUsage(); // Try fallback method
    }
    
    return 0.0; // Default return if all methods fail
  }
  
  // Fallback method using older wmic approach
  static Future<double> _getWmicCpuUsage() async {
    try {
      final processResult = await Process.run(
        'wmic', ['cpu', 'get', 'LoadPercentage'],
        runInShell: true,
      ).timeout(Duration(seconds: 3));

      if (processResult.exitCode == 0) {
        String output = processResult.stdout.toString();
        final lines = LineSplitter.split(output).toList();
        if (lines.length >= 2) {
          for(int i = 1; i < lines.length; i++) {
             String line = lines[i].trim();
             if(line.isNotEmpty) {
                 double? percentage = double.tryParse(line);
                 if (percentage != null) {
                   return percentage;
                 }
             }
          }
        }
      }
      return 0.0;
    } catch (e) {
      print('Error in fallback CPU load method: $e');
      return 0.0;
    }
  }
  
  // Get CPU usage for all logical processors
  static Future<List<double>> getCpuCoreUsage() async {
    try {
      // PowerShell can get all logical processor usage at once
      final processResult = await Process.run(
        'powershell', [
          '-Command',
          r'(Get-Counter "\Processor(*)\% Processor Time").CounterSamples | ForEach-Object { $_.CookedValue }'
        ],
        runInShell: true,
      ).timeout(Duration(seconds: 5));

      if (processResult.exitCode == 0) {
        String output = processResult.stdout.toString();
        final lines = LineSplitter.split(output).where((line) => line.trim().isNotEmpty).toList();
        
        // First value is _Total, we'll skip it
        final coreValues = <double>[];
        for (int i = 1; i < lines.length; i++) {
          final value = double.tryParse(lines[i].trim());
          if (value != null) {
            coreValues.add(value);
          }
        }
        
        if (coreValues.isNotEmpty) {
          return coreValues;
        }
        
        print('Failed to parse core CPU loads from PowerShell. Output: $output');
      } else {
        print('Failed to get core CPU loads from PowerShell. ExitCode: ${processResult.exitCode}\nError: ${processResult.stderr}');
      }
      
      // Fallback: return a list with just the overall CPU usage
      final overallUsage = await getCpuUsage();
      return [overallUsage];
    } catch (e) {
      print('Error getting core CPU loads: $e');
      return [0.0];
    }
  }
  
  // Get number of logical processors
  static Future<int> getLogicalProcessorCount() async {
    try {
      // Try environment variable first - this is very fast
      final envCount = Platform.numberOfProcessors;
      if (envCount > 0) {
        return envCount;
      }
      
      // Fallback to PowerShell if needed
      final processResult = await Process.run(
        'powershell', [
          '-Command',
          '(Get-CimInstance Win32_ComputerSystem).NumberOfLogicalProcessors'
        ],
        runInShell: true,
      ).timeout(Duration(seconds: 3));

      if (processResult.exitCode == 0) {
        String output = processResult.stdout.toString().trim();
        int? count = int.tryParse(output);
        if (count != null && count > 0) {
          return count;
        }
      }
      
      // If we're here, try one more method with wmic
      final wmicResult = await Process.run(
        'wmic', ['cpu', 'get', 'NumberOfLogicalProcessors'],
        runInShell: true,
      ).timeout(Duration(seconds: 3));
      
      if (wmicResult.exitCode == 0) {
        String output = wmicResult.stdout.toString();
        final lines = LineSplitter.split(output).toList();
        if (lines.length >= 2) {
          for(int i = 1; i < lines.length; i++) {
             String line = lines[i].trim();
             if(line.isNotEmpty) {
                 int? count = int.tryParse(line);
                 if (count != null && count > 0) {
                   return count;
                 }
             }
          }
        }
      }
      
      return 4; // Default fallback if all methods fail
    } catch (e) {
      print('Error getting logical processor count: $e');
      return 4; // Default fallback
    }
  }

  // Get RAM usage percentage
  static Future<double> getRamUsage() async {
    try {
      // system_info2 provides total and free physical memory
      final totalMemory = SysInfo.getTotalPhysicalMemory();
      final freeMemory = SysInfo.getFreePhysicalMemory();
      if (totalMemory <= 0) return 0.0; // Avoid division by zero
      final usedMemory = totalMemory - freeMemory;
      return (usedMemory / totalMemory) * 100.0;
    } catch (e) {
      print('Error getting RAM info: $e');
      return 0.0;
    }
  }

  // Get Disk usage percentage (for the C: drive typically)
  static Future<double> getDiskUsage({String drive = 'C'}) async {
     // system_info2 doesn't directly give disk usage percentage.
     // We can use a Windows command like `wmic logicaldisk where DriveType=3 get Size,FreeSpace`.
     // Target the specified drive
      try {
        final processResult = await Process.run(
           'wmic', ['logicaldisk', 'where', 'DeviceID="${drive}:"', 'get', 'Size,FreeSpace', '/FORMAT:CSV'],
           runInShell: true,
         ).timeout(Duration(seconds: 3));

        if (processResult.exitCode == 0) {
          String output = processResult.stdout.toString();
          print('[SystemInfoService] Disk info raw output: $output');
          
          // Output could be in different formats depending on the system:
          // Format 1: Node,FreeSpace,Size
          // Format 2: COMPUTERNAME,FreeSpace,Size
          final lines = LineSplitter.split(output).toList();
          
          // Remove empty lines
          final nonEmptyLines = lines.where((line) => line.trim().isNotEmpty).toList();
          
          if (nonEmptyLines.length >= 2) {
             final dataLine = nonEmptyLines[1].trim();
             final values = dataLine.split(',');
             
             if (values.length >= 3) {
                // The first value is the Node/ComputerName, second is FreeSpace, third is Size
                String freeSpaceStr = values[1].trim();
                String totalSizeStr = values[2].trim();
                
                // Extract numeric values
                final freeSpace = double.tryParse(freeSpaceStr);
                final totalSize = double.tryParse(totalSizeStr);

                if (freeSpace != null && totalSize != null && totalSize > 0) {
                   final usedSpace = totalSize - freeSpace;
                   return (usedSpace / totalSize) * 100.0;
                }
                
                print('[SystemInfoService] Failed to parse numeric values: Free=$freeSpaceStr, Total=$totalSizeStr');
             } else {
                print('[SystemInfoService] Not enough values in data line: $dataLine');
             }
          } else {
             // Alternative: try using PowerShell to get disk info
             print('[SystemInfoService] Not enough lines in output, trying PowerShell fallback');
             return await _getPowerShellDiskUsage(drive);
          }
          
          print('[SystemInfoService] Failed to parse disk info from wmic. Output: $output');
          return 0.0; // Indicate failure or unknown
        }
        
        print('[SystemInfoService] Failed to get disk info from wmic. ExitCode: ${processResult.exitCode}\nError: ${processResult.stderr}');
        // Try PowerShell fallback
        return await _getPowerShellDiskUsage(drive);
      } on TimeoutException {
         print('[SystemInfoService] Timeout getting disk info from wmic, trying PowerShell');
         return await _getPowerShellDiskUsage(drive);
      } catch (e) {
        print('[SystemInfoService] Error getting disk info: $e');
        return 0.0;
      }
  }
  
  // PowerShell fallback for disk usage
  static Future<double> _getPowerShellDiskUsage(String drive) async {
    try {
      final processResult = await Process.run(
        'powershell', [
          '-Command',
          r'$disk = Get-PSDrive $drive; $used = $disk.Used; $free = $disk.Free; $total = $used + $free; [math]::Round(($used / $total) * 100, 2)'
        ],
        runInShell: true,
      ).timeout(Duration(seconds: 3));
      
      if (processResult.exitCode == 0) {
        String output = processResult.stdout.toString().trim();
        double? percentage = double.tryParse(output);
        if (percentage != null) {
          return percentage;
        }
      }
      
      print('[SystemInfoService] PowerShell disk info fallback failed: ${processResult.stderr}');
      return 0.0;
    } catch (e) {
      print('[SystemInfoService] Error in PowerShell disk info fallback: $e');
      return 0.0;
    }
  }

  // Get detailed information about a disk drive
  static Future<Map<String, dynamic>> getDriveInfo({String drive = 'C'}) async {
    final result = <String, dynamic>{
      'name': '$drive:',
      'free': 0.0,
      'total': 0.0,
    };
    
    try {
      // Try PowerShell first for more reliable results
      final powerShellResult = await Process.run(
        'powershell', [
          '-Command',
          r'$disk = Get-PSDrive ' + drive + r'; ' +
          r'$used = $disk.Used; $free = $disk.Free; ' +
          r'$total = $used + $free; ' +
          r'$volumeName = (Get-Volume -DriveLetter ' + drive + r').FileSystemLabel; ' +
          r'Write-Host "${volumeName},${free},${total}"'
        ],
        runInShell: true,
      ).timeout(Duration(seconds: 3));
      
      if (powerShellResult.exitCode == 0) {
        String output = powerShellResult.stdout.toString().trim();
        final values = output.split(',');
        
        if (values.length >= 3) {
          // Extract volume name if available
          final volumeName = values[0].trim();
          if (volumeName.isNotEmpty) {
            result['name'] = '$drive: ($volumeName)';
          }
          
          // Extract free space and total size in bytes
          final freeSpace = double.tryParse(values[1].trim());
          final totalSize = double.tryParse(values[2].trim());
          
          if (freeSpace != null && totalSize != null) {
            result['free'] = freeSpace;
            result['total'] = totalSize;
            return result;
          }
        }
      }
      
      // Fallback to wmic if PowerShell fails
      final processResult = await Process.run(
        'wmic', ['logicaldisk', 'where', 'DeviceID="${drive}:"', 'get', 'Size,FreeSpace,VolumeName', '/FORMAT:CSV'],
        runInShell: true,
      ).timeout(Duration(seconds: 3));
      
      if (processResult.exitCode == 0) {
        String output = processResult.stdout.toString();
        print('[SystemInfoService] Drive info raw output: $output');
        
        // Process output
        final lines = LineSplitter.split(output).toList();
        final nonEmptyLines = lines.where((line) => line.trim().isNotEmpty).toList();
        
        if (nonEmptyLines.length >= 2) {
          final dataLine = nonEmptyLines[1].trim();
          final values = dataLine.split(',');
          
          if (values.length >= 4) {
            // Extract volume name if available
            final volumeName = values[3].trim();
            if (volumeName.isNotEmpty) {
              result['name'] = '$drive: ($volumeName)';
            }
            
            // Extract free space and total size in bytes
            final freeSpace = double.tryParse(values[1].trim());
            final totalSize = double.tryParse(values[2].trim());
            
            if (freeSpace != null && totalSize != null) {
              result['free'] = freeSpace;
              result['total'] = totalSize;
            }
          }
        }
      }
      
      return result;
    } catch (e) {
      print('[SystemInfoService] Error getting drive info: $e');
      return result;
    }
  }
  
  // Get CPU Name
  static Future<String> getCpuName() async {
    try {
      // system_info2 doesn't expose the full friendly CPU name like "Intel Core i7..."
      // This requires Win32 API calls or parsing wmic output.
      // Let's use `wmic cpu get name` for the name.
       final processResult = await Process.run(
         'wmic', ['cpu', 'get', 'name'],
         runInShell: true, // Required for wmic
       ).timeout(Duration(seconds: 3)); // Add a timeout

      if (processResult.exitCode == 0) {
        String output = processResult.stdout.toString();
        final lines = LineSplitter.split(output).toList();
        if (lines.length >= 2) {
          for(int i = 1; i < lines.length; i++) {
            String line = lines[i].trim();
            if(line.isNotEmpty) {
              return line; // This should be the CPU name string
            }
          }
        }
        print('Failed to parse CPU name from wmic. Output: $output');
        return "CPU Info N/A"; // Fallback
      }
      print('Failed to get CPU name from wmic. ExitCode: ${processResult.exitCode}\nError: ${processResult.stderr}');
      return "CPU Info N/A"; // Fallback
    } on TimeoutException {
      print('Timeout getting CPU name from wmic.');
      return "CPU Info N/A";
    } catch (e) {
      print('Error getting CPU name: $e');
      return "CPU Info N/A"; // Fallback
    }
  }

  // Get OS Info
  
  // Helper to format seconds into a human-readable string (e.g., 1d 5h 30m)
  static String formatUptime(int totalSeconds) {
    if (totalSeconds <= 0) return '0s';
    int days = totalSeconds ~/ (24 * 3600);
    totalSeconds %= (24 * 3600);
    int hours = totalSeconds ~/ 3600;
    totalSeconds %= 3600;
    int minutes = totalSeconds ~/ 60;
    int seconds = totalSeconds % 60;

    List<String> parts = [];
    if (days > 0) parts.add('${days}d');
    if (hours > 0) parts.add('${hours}h');
    if (minutes > 0) parts.add('${minutes}m');
    // Only add seconds if it's less than a minute or if no other parts were added
    if (seconds > 0 || parts.isEmpty) parts.add('${seconds}s');

    return parts.join(' ');
  }

  // Note: Network statistics (traffic, adapter list) are not readily available
  // via system_info2. Implementing this would require platform-specific code
  // (e.g., using the 'win32' package to access Windows APIs or running commands).
}