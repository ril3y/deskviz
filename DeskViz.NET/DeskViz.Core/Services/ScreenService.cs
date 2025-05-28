using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DeskViz.Core.Services
{
    /// <summary>
    /// Service for managing screen information and window positioning
    /// </summary>
    public class ScreenService
    {
        // Win32 API calls for proper full-screen handling
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        // Constants for window positioning
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_MINIMIZE = 0x20000000;
        private const int WS_MAXIMIZE = 0x01000000;
        private const int WS_SYSMENU = 0x00080000;
        private const int WS_EX_DLGMODALFRAME = 0x00000001;
        private const int WS_EX_WINDOWEDGE = 0x00000100;
        private const int WS_EX_CLIENTEDGE = 0x00000200;
        private const int WS_EX_STATICEDGE = 0x00020000;
        
        /// <summary>
        /// Gets all available screens
        /// </summary>
        public List<ScreenInfo> GetAllScreens()
        {
            var screens = new List<ScreenInfo>();
            
            try
            {
                // Use System.Windows.Forms.Screen to get screen information
                foreach (var screen in Screen.AllScreens)
                {
                    screens.Add(new ScreenInfo
                    {
                        DeviceName = screen.DeviceName,
                        IsPrimary = screen.Primary,
                        Bounds = new ScreenBounds
                        {
                            X = screen.Bounds.X,
                            Y = screen.Bounds.Y,
                            Width = screen.Bounds.Width,
                            Height = screen.Bounds.Height
                        },
                        WorkingArea = new ScreenBounds
                        {
                            X = screen.WorkingArea.X,
                            Y = screen.WorkingArea.Y,
                            Width = screen.WorkingArea.Width,
                            Height = screen.WorkingArea.Height
                        },
                        Identifier = $"{screen.Bounds.X}_{screen.Bounds.Y}"
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting screens: {ex.Message}");
            }
            
            return screens;
        }
        
        /// <summary>
        /// Gets the primary screen
        /// </summary>
        public ScreenInfo? GetPrimaryScreen()
        {
            return GetAllScreens().FirstOrDefault(s => s.IsPrimary);
        }
        
        /// <summary>
        /// Gets screen by identifier (X_Y coordinates)
        /// </summary>
        public ScreenInfo? GetScreenByIdentifier(string identifier)
        {
            return GetAllScreens().FirstOrDefault(s => s.Identifier == identifier);
        }
        
        /// <summary>
        /// Gets the smallest screen (by area)
        /// </summary>
        public ScreenInfo? GetSmallestScreen()
        {
            var screens = GetAllScreens();
            return screens.OrderBy(s => s.Bounds.Width * s.Bounds.Height).FirstOrDefault();
        }
        
        /// <summary>
        /// Applies true fullscreen mode to a window
        /// </summary>
        public void ApplyTrueFullscreen(IntPtr windowHandle, ScreenInfo targetScreen)
        {
            try
            {
                if (windowHandle == IntPtr.Zero)
                {
                    windowHandle = GetForegroundWindow();
                }
                
                if (windowHandle == IntPtr.Zero) return;
                
                // Remove window decorations for true fullscreen
                int style = GetWindowLong(windowHandle, GWL_STYLE);
                style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZE | WS_MAXIMIZE | WS_SYSMENU);
                SetWindowLong(windowHandle, GWL_STYLE, style);
                
                // Remove extended window styles
                int exStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
                exStyle &= ~(WS_EX_DLGMODALFRAME | WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE);
                SetWindowLong(windowHandle, GWL_EXSTYLE, exStyle);
                
                // Position window to cover the entire screen and make it topmost
                SetWindowPos(
                    windowHandle,
                    HWND_TOPMOST,
                    targetScreen.Bounds.X,
                    targetScreen.Bounds.Y,
                    targetScreen.Bounds.Width,
                    targetScreen.Bounds.Height,
                    SWP_SHOWWINDOW
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying fullscreen: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Defines screen orientation based on dimensions.
    /// </summary>
    public enum ScreenOrientation
    {
        Landscape, // Width >= Height
        Portrait   // Height > Width
    }
    
    /// <summary>
    /// Represents screen information
    /// </summary>
    public class ScreenInfo
    {
        /// <summary>
        /// Gets or sets the device name
        /// </summary>
        public string DeviceName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets a value indicating whether this is the primary screen
        /// </summary>
        public bool IsPrimary { get; set; }
        
        /// <summary>
        /// Gets or sets the bounds of the screen
        /// </summary>
        public ScreenBounds Bounds { get; set; } = new ScreenBounds();
        
        /// <summary>
        /// Gets or sets the working area of the screen (excluding taskbar)
        /// </summary>
        public ScreenBounds WorkingArea { get; set; } = new ScreenBounds();
        
        /// <summary>
        /// Gets or sets the screen identifier (X_Y coordinates)
        /// </summary>
        public string Identifier { get; set; } = string.Empty;

        /// <summary>
        /// Gets a user-friendly display name including resolution and primary status.
        /// </summary>
        public string DisplayName => $"{DeviceName} ({Bounds.Width}x{Bounds.Height}){(IsPrimary ? " [Primary]" : "")}";
        
        /// <summary>
        /// Gets the calculated orientation of the screen.
        /// </summary>
        public ScreenOrientation Orientation => Bounds.Height > Bounds.Width 
                                                ? ScreenOrientation.Portrait 
                                                : ScreenOrientation.Landscape;
    }
    
    /// <summary>
    /// Represents screen bounds
    /// </summary>
    public class ScreenBounds
    {
        /// <summary>
        /// Gets or sets the X coordinate
        /// </summary>
        public int X { get; set; }
        
        /// <summary>
        /// Gets or sets the Y coordinate
        /// </summary>
        public int Y { get; set; }
        
        /// <summary>
        /// Gets or sets the width
        /// </summary>
        public int Width { get; set; }
        
        /// <summary>
        /// Gets or sets the height
        /// </summary>
        public int Height { get; set; }
    }
}
