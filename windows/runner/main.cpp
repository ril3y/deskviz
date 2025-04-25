// Add this define *before* including windows.h to ensure newer constants are available
//#define _WIN32_WINNT 0x0A00 // This sets the minimum Windows version to Windows 10
// Commented out - no longer needed for this specific fullscreen approach

#include <flutter/dart_project.h>
#include <flutter/flutter_view_controller.h>
#include <windows.h>
//#include <shellapi.h> // No longer needed
//#include <shobjidl.h> // No longer needed

#include "flutter_window.h"
#include "utils.h"

// Remove GetPrimaryMonitorDimensions function
/*
bool GetPrimaryMonitorDimensions(int& width, int& height) {
  // ... implementation removed ...
}
*/

// Remove HideTaskbar function
/*
void HideTaskbar(bool hide) {
  // ... implementation removed ...
}
*/

// Remove SetWindowFullscreenKiosk function
/*
void SetWindowFullscreenKiosk(HWND hwnd) {
  // ... implementation removed ...
}
*/

// Remove custom WindowProc - no longer needed for taskbar restoration here
/*
LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam) {
  if (uMsg == WM_CLOSE || uMsg == WM_DESTROY || uMsg == WM_QUIT) {
    HideTaskbar(false); // Show the taskbar again
  }
  return DefWindowProc(hwnd, uMsg, wParam, lParam);
}
*/

int APIENTRY wWinMain(_In_ HINSTANCE instance, _In_opt_ HINSTANCE prev,
                      _In_ wchar_t *command_line, _In_ int show_command) {
  // Attach to console when present (e.g., 'flutter run') or create a
  // new console when running with a debugger.
  if (!::AttachConsole(ATTACH_PARENT_PROCESS) && ::IsDebuggerPresent()) {
    CreateAndAttachConsole();
  }

  // Initialize COM, so that it is available for use in the library and/or
  // plugins.
  ::CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);

  flutter::DartProject project(L"data");

  std::vector<std::string> command_line_arguments =
      GetCommandLineArguments();

  project.set_dart_entrypoint_arguments(std::move(command_line_arguments));

  FlutterWindow window(project);
  
  // Set the window's initial position and size (can be default or adjusted later by window_manager)
  Win32Window::Point origin(100, 100); // Default origin
  Win32Window::Size target_size(1280, 720); // Default size

  // Create the window
  if (!window.Create(L"deskviz", origin, target_size)) {
    return EXIT_FAILURE;
  }
  
  window.SetQuitOnClose(true);

  ::MSG msg;
  while (::GetMessage(&msg, nullptr, 0, 0)) {
    ::TranslateMessage(&msg);
    ::DispatchMessage(&msg);
  }

  ::CoUninitialize();
  return EXIT_SUCCESS;
}