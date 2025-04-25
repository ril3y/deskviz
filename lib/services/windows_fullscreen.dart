import 'dart:ffi';
import 'package:ffi/ffi.dart';
import 'package:win32/win32.dart';
import 'dart:io';

/// A helper class for Windows-specific fullscreen handling
class WindowsFullscreen {
  /// Applies true fullscreen mode that hides the taskbar
  static void applyTrueFullscreen(int hwnd) {
    if (!Platform.isWindows) return;
    
    try {
      print('[WindowsFullscreen] Applying true fullscreen to handle: $hwnd');
      
      // Get information about the monitor containing window
      final hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
      
      // Get monitor info including work area (excludes taskbar)
      final monitorInfo = calloc<MONITORINFO>();
      monitorInfo.ref.cbSize = sizeOf<MONITORINFO>();
      
      if (GetMonitorInfo(hMonitor, monitorInfo) != 0) {
        // Get monitor dimensions (including taskbar area)
        final monitorRect = monitorInfo.ref.rcMonitor;
        
        print('[WindowsFullscreen] Monitor dimensions: left=${monitorRect.left}, top=${monitorRect.top}, '
              'right=${monitorRect.right}, bottom=${monitorRect.bottom}');
        
        // Set window to cover the entire monitor, including taskbar area
        SetWindowPos(
          hwnd,
          HWND_TOPMOST, // Keep on top
          monitorRect.left,
          monitorRect.top,
          monitorRect.right - monitorRect.left,
          monitorRect.bottom - monitorRect.top,
          SWP_SHOWWINDOW,
        );
        
        // Set window style to remove borders and caption
        int style = GetWindowLongW(hwnd, GWL_STYLE);
        style &= ~WS_CAPTION; // Remove caption
        style &= ~WS_THICKFRAME; // Remove resizable frame
        style &= ~WS_MINIMIZE; // Remove minimize button
        style &= ~WS_MAXIMIZE; // Remove maximize button
        style &= ~WS_SYSMENU; // Remove system menu
        SetWindowLongW(hwnd, GWL_STYLE, style);
        
        // Update window without changing position (refresh)
        SetWindowPos(
          hwnd, 
          0, 
          0, 
          0, 
          0, 
          0, 
          SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED
        );
      }
      
      calloc.free(monitorInfo);
      
      print('[WindowsFullscreen] Applied true fullscreen mode');
    } catch (e) {
      print('[WindowsFullscreen] Error applying true fullscreen: $e');
    }
  }
  
  /// Find window handle by searching for our application window
  static int findWindowHandle() {
    try {
      // Try different class names that Flutter might be using
      final classNames = [
        'FLUTTER_RUNNER_WIN32_WINDOW',
        'FLUTTERVIEW',
        'FLUTTER_WINDOW',
      ];
      
      final titleNames = [
        'DeskViz',
        'Flutter', 
        null
      ];
      
      // Try combinations of class and title
      for (final className in classNames) {
        for (final titleName in titleNames) {
          final classPtr = titleName != null ? TEXT(className) : nullptr;
          final titlePtr = titleName != null ? TEXT(titleName) : nullptr;
          
          final hwnd = FindWindowEx(classPtr, titlePtr);
          
          if (hwnd != 0) {
            print('[WindowsFullscreen] Found window with class: $className, title: $titleName, handle: $hwnd');
            return hwnd;
          }
        }
      }
      
      // If no specific window found, try to find any window
      final hwnd = GetActiveWindow();
      if (hwnd != 0) {
        print('[WindowsFullscreen] Using active window handle: $hwnd');
        return hwnd;
      }
      
      print('[WindowsFullscreen] Could not find window handle');
      return 0;
    } catch (e) {
      print('[WindowsFullscreen] Error finding window handle: $e');
      return 0;
    }
  }
  
  /// Apply true fullscreen directly without needing the handle
  static void applyFullscreen() {
    if (!Platform.isWindows) return;
    
    try {
      final hwnd = findWindowHandle();
      if (hwnd != 0) {
        applyTrueFullscreen(hwnd);
      } else {
        print('[WindowsFullscreen] Failed to find window handle to apply fullscreen');
      }
    } catch (e) {
      print('[WindowsFullscreen] Error in applyFullscreen: $e');
    }
  }
}
