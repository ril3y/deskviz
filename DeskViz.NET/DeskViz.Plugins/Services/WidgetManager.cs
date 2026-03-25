using System;
using System.Collections.Generic;
using System.Linq;
using DeskViz.Plugins.Interfaces;

namespace DeskViz.Plugins.Services
{
    public class WidgetManager
    {
        private readonly WidgetDiscoveryService _discoveryService;
        private readonly IWidgetHost _host;
        private readonly Dictionary<string, IWidgetPlugin> _activeWidgets = new();

        public IReadOnlyDictionary<string, IWidgetPlugin> ActiveWidgets => _activeWidgets.AsReadOnly();
        public IReadOnlyList<LoadedWidget> AvailableWidgets => _discoveryService.LoadedWidgets;

        public event EventHandler<WidgetActivatedEventArgs>? WidgetActivated;
        public event EventHandler<WidgetDeactivatedEventArgs>? WidgetDeactivated;
        public event EventHandler<WidgetErrorEventArgs>? WidgetError;

        public WidgetManager(WidgetDiscoveryService discoveryService, IWidgetHost host)
        {
            _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public void RefreshAvailableWidgets()
        {
            _discoveryService.DiscoverWidgets();
        }

        public bool ActivateWidget(string widgetId)
        {
            if (_activeWidgets.ContainsKey(widgetId))
            {
                return true; // Already active
            }

            try
            {
                var widget = _discoveryService.CreateWidgetInstance(widgetId);
                if (widget == null)
                {
                    return false;
                }

                widget.Initialize(_host);
                _activeWidgets[widgetId] = widget;

                OnWidgetActivated(widget);
                return true;
            }
            catch (Exception ex)
            {
                OnWidgetError(widgetId, ex);
                return false;
            }
        }

        public bool DeactivateWidget(string widgetId)
        {
            if (!_activeWidgets.TryGetValue(widgetId, out var widget))
            {
                return false; // Not active
            }

            try
            {
                widget.Shutdown();
                _activeWidgets.Remove(widgetId);

                OnWidgetDeactivated(widgetId, widget);
                return true;
            }
            catch (Exception ex)
            {
                OnWidgetError(widgetId, ex);
                return false;
            }
        }

        public IWidgetPlugin? GetActiveWidget(string widgetId)
        {
            return _activeWidgets.TryGetValue(widgetId, out var widget) ? widget : null;
        }

        public bool IsWidgetActive(string widgetId)
        {
            return _activeWidgets.ContainsKey(widgetId);
        }

        public bool IsWidgetAvailable(string widgetId)
        {
            return _discoveryService.IsWidgetAvailable(widgetId);
        }

        public IWidgetMetadata? GetWidgetMetadata(string widgetId)
        {
            return _discoveryService.GetWidgetMetadata(widgetId);
        }

        public void RefreshWidget(string widgetId)
        {
            if (_activeWidgets.TryGetValue(widgetId, out var widget))
            {
                try
                {
                    widget.RefreshData();
                }
                catch (Exception ex)
                {
                    OnWidgetError(widgetId, ex);
                }
            }
        }

        public void RefreshAllWidgets()
        {
            foreach (var widget in _activeWidgets.Values)
            {
                try
                {
                    widget.RefreshData();
                }
                catch (Exception ex)
                {
                    OnWidgetError(widget.WidgetId, ex);
                }
            }
        }

        public void NotifyPageChanged(string pageId)
        {
            foreach (var widget in _activeWidgets.Values)
            {
                try
                {
                    widget.OnPageChanged(pageId);
                }
                catch (Exception ex)
                {
                    OnWidgetError(widget.WidgetId, ex);
                }
            }
        }

        public void SetWidgetVisibility(string widgetId, bool isVisible)
        {
            if (_activeWidgets.TryGetValue(widgetId, out var widget))
            {
                try
                {
                    widget.IsWidgetVisible = isVisible;
                    widget.OnVisibilityChanged(isVisible);
                }
                catch (Exception ex)
                {
                    OnWidgetError(widgetId, ex);
                }
            }
        }

        public void Shutdown()
        {
            var widgetIds = _activeWidgets.Keys.ToList();
            foreach (var widgetId in widgetIds)
            {
                DeactivateWidget(widgetId);
            }
        }

        private void OnWidgetActivated(IWidgetPlugin widget)
        {
            WidgetActivated?.Invoke(this, new WidgetActivatedEventArgs(widget));
        }

        private void OnWidgetDeactivated(string widgetId, IWidgetPlugin widget)
        {
            WidgetDeactivated?.Invoke(this, new WidgetDeactivatedEventArgs(widgetId, widget));
        }

        private void OnWidgetError(string widgetId, Exception exception)
        {
            WidgetError?.Invoke(this, new WidgetErrorEventArgs(widgetId, exception));
        }
    }

    public class WidgetActivatedEventArgs : EventArgs
    {
        public IWidgetPlugin Widget { get; }

        public WidgetActivatedEventArgs(IWidgetPlugin widget)
        {
            Widget = widget ?? throw new ArgumentNullException(nameof(widget));
        }
    }

    public class WidgetDeactivatedEventArgs : EventArgs
    {
        public string WidgetId { get; }
        public IWidgetPlugin Widget { get; }

        public WidgetDeactivatedEventArgs(string widgetId, IWidgetPlugin widget)
        {
            WidgetId = widgetId ?? throw new ArgumentNullException(nameof(widgetId));
            Widget = widget ?? throw new ArgumentNullException(nameof(widget));
        }
    }

    public class WidgetErrorEventArgs : EventArgs
    {
        public string WidgetId { get; }
        public Exception Exception { get; }

        public WidgetErrorEventArgs(string widgetId, Exception exception)
        {
            WidgetId = widgetId ?? throw new ArgumentNullException(nameof(widgetId));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }
    }
}