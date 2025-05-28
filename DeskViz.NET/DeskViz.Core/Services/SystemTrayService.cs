using System;
using System.Drawing;
using System.Windows.Forms;

namespace DeskViz.Core.Services
{
    public interface ISystemTrayService : IDisposable
    {
        event EventHandler? SettingsRequested;
        event EventHandler? AboutRequested;
        event EventHandler? ExitRequested;
        event EventHandler? TrayIconDoubleClicked;
        
        void Initialize(Icon icon, string toolTipText);
        void Show();
        void Hide();
        void ShowBalloonTip(string title, string text, int timeout = 3000);
    }

    public class SystemTrayService : ISystemTrayService
    {
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        
        public event EventHandler? SettingsRequested;
        public event EventHandler? AboutRequested;
        public event EventHandler? ExitRequested;
        public event EventHandler? TrayIconDoubleClicked;

        public void Initialize(Icon icon, string toolTipText)
        {
            if (_notifyIcon != null)
            {
                Dispose();
            }

            _notifyIcon = new NotifyIcon
            {
                Icon = icon,
                Text = toolTipText,
                Visible = false
            };

            // Create context menu
            _contextMenu = new ContextMenuStrip();
            
            var settingsItem = new ToolStripMenuItem("Widget Settings");
            settingsItem.Click += (s, e) => SettingsRequested?.Invoke(this, EventArgs.Empty);
            settingsItem.Font = new Font(settingsItem.Font, FontStyle.Bold);
            
            var aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += (s, e) => AboutRequested?.Invoke(this, EventArgs.Empty);
            
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
            
            _contextMenu.Items.Add(settingsItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(aboutItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(exitItem);
            
            _notifyIcon.ContextMenuStrip = _contextMenu;
            
            // Handle double-click
            _notifyIcon.DoubleClick += (s, e) => TrayIconDoubleClicked?.Invoke(this, EventArgs.Empty);
        }

        public void Show()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
            }
        }

        public void Hide()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }
        }

        public void ShowBalloonTip(string title, string text, int timeout = 3000)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.BalloonTipTitle = title;
                _notifyIcon.BalloonTipText = text;
                _notifyIcon.ShowBalloonTip(timeout);
            }
        }

        public void Dispose()
        {
            Hide();
            _contextMenu?.Dispose();
            _notifyIcon?.Dispose();
            _notifyIcon = null;
            _contextMenu = null;
        }
    }
}