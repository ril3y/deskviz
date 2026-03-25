using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Extensions.Logging;
using DeskViz.App.Services;
using DeskViz.App.Widgets;
using DeskViz.Plugins.Interfaces;
using DeskViz.Plugins.Base;
using DeskViz.Core.Models;

namespace DeskViz.App.Controls
{
    /// <summary>
    /// Control that manages multiple pages of widgets with swipe navigation
    /// </summary>
    public partial class PagedWidgetContainer : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        private readonly ILogger _logger = AppLoggerFactory.CreateLogger<PagedWidgetContainer>();
        private List<PageConfig> _pages = new List<PageConfig>();
        private List<IWidgetPlugin> _allWidgets = new List<IWidgetPlugin>();
        private int _currentPageIndex = 0;
        private ObservableCollection<PageIndicator> _pageIndicators = new ObservableCollection<PageIndicator>();
        private ObservableCollection<PageListItem> _pageList = new ObservableCollection<PageListItem>();

        // Track widget UI elements to avoid recreating them
        private readonly Dictionary<string, UIElement> _widgetUICache = new Dictionary<string, UIElement>();
        private bool _widgetsInitialized = false;

        /// <summary>
        /// Gets whether the container has been initialized
        /// </summary>
        public bool IsContainerInitialized => _widgetsInitialized;
        
        // Swipe down gesture detection
        private System.Windows.Point _swipeStartPoint;
        private bool _isSwipeTracking = false;
        private const double SwipeThreshold = 50; // Minimum distance for swipe down
        private DateTime _swipeStartTime;
        private double _currentSwipeProgress = 0; // For visual feedback during swipe
        private bool _pageSelectorJustOpened = false; // Prevent immediate close on touch up
        private DateTime _lastCloseTime = DateTime.MinValue; // Prevent rapid repeated closes

        /// <summary>
        /// Gets the page indicators for binding
        /// </summary>
        public ObservableCollection<PageIndicator> PageIndicators => _pageIndicators;
        
        /// <summary>
        /// Gets the page list for the overlay selector
        /// </summary>
        public ObservableCollection<PageListItem> PageList => _pageList;

        /// <summary>
        /// Gets or sets the current page index
        /// </summary>
        public int CurrentPageIndex
        {
            get => _currentPageIndex;
            set
            {
                _logger.LogDebug($"Setting CurrentPageIndex from {_currentPageIndex} to {value}");
                if (value >= 0 && value < _pages.Count)
                {
                    _currentPageIndex = value;
                    OnPropertyChanged();
                    LoadCurrentPage();
                    UpdatePageIndicators();
                    _logger.LogDebug($"CurrentPageIndex set to {_currentPageIndex}");
                }
                else
                {
                    _logger.LogWarning($"CurrentPageIndex {value} out of bounds (0-{_pages.Count - 1})");
                }
            }
        }

        /// <summary>
        /// Event raised when the current page changes
        /// </summary>
        public event EventHandler<int>? PageChanged;

        /// <summary>
        /// Gets the page transform for animation
        /// </summary>
        public TranslateTransform PageTransformAccess => PageTransform;
        
        /// <summary>
        /// Initializes a new instance of PagedWidgetContainer
        /// </summary>
        public PagedWidgetContainer()
        {
            InitializeComponent();
            DataContext = this;

            // Force enable touch support
            IsManipulationEnabled = true;
            Stylus.SetIsTouchFeedbackEnabled(this, false);
            Stylus.SetIsPressAndHoldEnabled(this, false);
            Stylus.SetIsFlicksEnabled(this, false);
            Stylus.SetIsTapFeedbackEnabled(this, false);

            _logger.LogInformation($"[INIT] Touch support enabled for PagedWidgetContainer");
        }

        /// <summary>
        /// Sets up the container with pages and widgets
        /// </summary>
        public void Initialize(List<PageConfig> pages, List<IWidgetPlugin> allWidgets)
        {
            _logger.LogInformation($"Initializing PagedWidgetContainer with {pages?.Count ?? 0} pages and {allWidgets?.Count ?? 0} widgets");

            _pages = pages ?? new List<PageConfig>();
            _allWidgets = allWidgets ?? new List<IWidgetPlugin>();

            // Clear previous widget initialization
            CurrentPageWidgets.Items.Clear();
            _widgetUICache.Clear();
            _widgetsInitialized = false;

            if (_pages.Count == 0)
            {
                // Create a default page if none exist
                _pages.Add(new PageConfig("Main"));
            }

            // Reset to page 0 and load content
            _currentPageIndex = -1; // Force change
            CurrentPageIndex = 0;
            UpdatePageIndicators();
        }

        /// <summary>
        /// Loads widgets for the current page using visibility-based switching
        /// </summary>
        private void LoadCurrentPage()
        {
            if (_currentPageIndex < 0 || _currentPageIndex >= _pages.Count)
                return;

            var currentPage = _pages[_currentPageIndex];

            // Debug: Log page info
            _logger.LogDebug($"Loading page {_currentPageIndex}: {currentPage.Name}");
            _logger.LogDebug($"Page has {currentPage.WidgetIds.Count} widgets");

            // Initialize widgets only once to preserve timers and animations
            if (!_widgetsInitialized)
            {
                InitializeAllWidgets();
                _widgetsInitialized = true;
            }

            // Update visibility based on current page configuration
            UpdateWidgetVisibility(currentPage);

            // Notify all widgets about the page change so they can load page-specific settings
            NotifyWidgetsOfPageChange(currentPage);

            _logger.LogDebug($"Page switch completed - widgets remain in visual tree");
        }

        /// <summary>
        /// Notifies all widgets of page changes so they can load page-specific settings
        /// </summary>
        private void NotifyWidgetsOfPageChange(PageConfig currentPage)
        {
            foreach (var kvp in _widgetUICache)
            {
                var uiElement = kvp.Value;
                if (uiElement is BaseWidget widget)
                {
                    try
                    {
                        widget.OnPageChanged(currentPage.Id);
                        _logger.LogDebug($"Notified widget {widget.WidgetId} of page change to {currentPage.Id}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error notifying widget {widget.WidgetId} of page change: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Initializes all widgets once and adds them to the visual tree
        /// </summary>
        private void InitializeAllWidgets()
        {
            _logger.LogDebug($"Initializing all widgets - Available: {string.Join(", ", _allWidgets.Select(w => w.WidgetId))}");

            foreach (var widget in _allWidgets)
            {
                if (_widgetUICache.ContainsKey(widget.WidgetId))
                    continue; // Already initialized

                _logger.LogDebug($"Initializing widget: {widget.WidgetId}");
                UIElement? uiElement = null;

                // Handle plugin widgets vs old-style widgets
                var widgetType = widget.GetType();
                bool isPluginWidget = widgetType.Assembly != System.Reflection.Assembly.GetExecutingAssembly();

                if (isPluginWidget)
                {
                    // Plugin widget - create UI via CreateWidgetUI()
                    _logger.LogDebug($"Creating UI via CreateWidgetUI() for plugin widget: {widget.WidgetId}");
                    try
                    {
                        uiElement = widget.CreateWidgetUI();
                        _logger.LogDebug($"CreateWidgetUI() succeeded for {widget.WidgetId}: {uiElement?.GetType().Name ?? "null"}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"CreateWidgetUI() failed for {widget.WidgetId}: {ex.Message}");
                        uiElement = null;
                    }
                }
                else if (widget is UIElement directElement)
                {
                    // Old-style widget that's directly a UI element
                    _logger.LogDebug($"Using direct UI element for hardcoded widget: {widget.WidgetId}");
                    uiElement = directElement;
                }
                else
                {
                    _logger.LogDebug($"Unknown widget type for {widget.WidgetId}: {widgetType.FullName}");
                    uiElement = null;
                }

                if (uiElement != null)
                {
                    // Add margin to widgets
                    if (uiElement is FrameworkElement frameworkElement)
                    {
                        frameworkElement.Margin = new Thickness(10, 5, 10, 10);
                    }

                    // Cache the widget UI and add to visual tree
                    _widgetUICache[widget.WidgetId] = uiElement;
                    CurrentPageWidgets.Items.Add(uiElement);
                    _logger.LogDebug($"Initialized and cached widget: {widget.WidgetId}");
                }
                else
                {
                    _logger.LogWarning($"Widget UI creation failed: {widget.WidgetId}");
                }
            }
        }

        /// <summary>
        /// Updates widget visibility and order based on current page configuration
        /// </summary>
        private void UpdateWidgetVisibility(PageConfig currentPage)
        {
            // Reorder items in the ItemsControl to match WidgetIds order
            CurrentPageWidgets.Items.Clear();

            // First add widgets in the order specified by the page config
            foreach (var widgetId in currentPage.WidgetIds)
            {
                if (_widgetUICache.TryGetValue(widgetId, out var uiElement))
                {
                    bool shouldBeVisible = currentPage.WidgetVisibility.GetValueOrDefault(widgetId, true);
                    uiElement.Visibility = shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;
                    CurrentPageWidgets.Items.Add(uiElement);
                    _logger.LogDebug($"Widget {widgetId}: {(shouldBeVisible ? "Visible" : "Hidden")} (ordered)");
                }
            }

            // Then add any remaining cached widgets not in this page's list (hidden)
            foreach (var kvp in _widgetUICache)
            {
                if (!currentPage.WidgetIds.Contains(kvp.Key))
                {
                    kvp.Value.Visibility = Visibility.Collapsed;
                    CurrentPageWidgets.Items.Add(kvp.Value);
                    _logger.LogDebug($"Widget {kvp.Key}: Hidden (not on page)");
                }
            }
        }

        /// <summary>
        /// Adds new widgets to the container without reinitializing existing ones
        /// </summary>
        public void AddNewWidgets(List<IWidgetPlugin> allWidgets)
        {
            _logger.LogDebug($"Adding new widgets - Total available: {allWidgets.Count}, Currently cached: {_widgetUICache.Count}");

            // Update the widget list
            _allWidgets = allWidgets ?? new List<IWidgetPlugin>();

            // Only initialize widgets that aren't already cached
            foreach (var widget in _allWidgets)
            {
                if (!_widgetUICache.ContainsKey(widget.WidgetId))
                {
                    _logger.LogDebug($"Adding new widget: {widget.WidgetId}");
                    InitializeSingleWidget(widget);
                }
            }

            // Update visibility for current page
            if (_currentPageIndex >= 0 && _currentPageIndex < _pages.Count)
            {
                UpdateWidgetVisibility(_pages[_currentPageIndex]);
            }
        }

        /// <summary>
        /// Updates the page list and refreshes the display without reinitializing widgets.
        /// Call this after pages are added/removed/reordered in settings.
        /// </summary>
        public void SyncPages(List<PageConfig> pages)
        {
            _logger.LogDebug($"SyncPages called: {pages.Count} pages (was {_pages.Count})");
            _pages = pages ?? new List<PageConfig>();

            // Clamp current page index if pages were removed
            if (_currentPageIndex >= _pages.Count)
            {
                _currentPageIndex = Math.Max(0, _pages.Count - 1);
            }

            // Refresh visibility and indicators for the current page
            if (_pages.Count > 0 && _currentPageIndex >= 0 && _currentPageIndex < _pages.Count)
            {
                UpdateWidgetVisibility(_pages[_currentPageIndex]);
            }
            UpdatePageIndicators();
            OnPropertyChanged(nameof(CurrentPageIndex));
        }

        /// <summary>
        /// Initializes a single widget and adds it to the cache
        /// </summary>
        private void InitializeSingleWidget(IWidgetPlugin widget)
        {
            _logger.LogDebug($"Initializing single widget: {widget.WidgetId}");
            UIElement? uiElement = null;

            // Handle plugin widgets vs old-style widgets
            var widgetType = widget.GetType();
            bool isPluginWidget = widgetType.Assembly != System.Reflection.Assembly.GetExecutingAssembly();

            if (isPluginWidget)
            {
                // Plugin widget - create UI via CreateWidgetUI()
                _logger.LogDebug($"Creating UI via CreateWidgetUI() for plugin widget: {widget.WidgetId}");
                try
                {
                    uiElement = widget.CreateWidgetUI();
                    _logger.LogDebug($"CreateWidgetUI() succeeded for {widget.WidgetId}: {uiElement?.GetType().Name ?? "null"}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"CreateWidgetUI() failed for {widget.WidgetId}: {ex.Message}");
                    uiElement = null;
                }
            }
            else if (widget is UIElement directElement)
            {
                // Old-style widget that's directly a UI element
                _logger.LogDebug($"Using direct UI element for hardcoded widget: {widget.WidgetId}");
                uiElement = directElement;
            }
            else
            {
                _logger.LogDebug($"Unknown widget type for {widget.WidgetId}: {widgetType.FullName}");
                uiElement = null;
            }

            if (uiElement != null)
            {
                // Add margin to widgets
                if (uiElement is FrameworkElement frameworkElement)
                {
                    frameworkElement.Margin = new Thickness(10, 5, 10, 10);
                }

                // Cache the widget UI and add to visual tree
                _widgetUICache[widget.WidgetId] = uiElement;
                CurrentPageWidgets.Items.Add(uiElement);
                _logger.LogDebug($"Initialized and cached new widget: {widget.WidgetId}");
            }
            else
            {
                _logger.LogWarning($"Widget UI creation failed: {widget.WidgetId}");
            }
        }

        /// <summary>
        /// Updates the page indicator dots and page list
        /// </summary>
        private void UpdatePageIndicators()
        {
            _pageIndicators.Clear();
            _pageList.Clear();
            
            for (int i = 0; i < _pages.Count; i++)
            {
                var page = _pages[i];
                var isCurrentPage = i == _currentPageIndex;
                
                // Update page indicators
                _pageIndicators.Add(new PageIndicator
                {
                    PageIndex = i,
                    PageName = page.Name,
                    Fill = isCurrentPage ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Gray
                });
                
                // Update page list for overlay
                var widgetCount = page.WidgetIds.Count(widgetId => 
                    page.WidgetVisibility.GetValueOrDefault(widgetId, true));
                    
                _pageList.Add(new PageListItem
                {
                    PageIndex = i,
                    PageName = page.Name,
                    WidgetCount = widgetCount,
                    Fill = isCurrentPage ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Gray
                });
            }
        }


        /// <summary>
        /// Navigates to a specific page with animation
        /// </summary>
        public void NavigateToPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _pages.Count || pageIndex == _currentPageIndex)
            {
                AnimatePageTransform(0);
                return;
            }

            // Animate the transition
            var direction = pageIndex > _currentPageIndex ? -1 : 1;
            var targetX = ActualWidth * direction;

            var storyboard = new Storyboard();
            var animation = new DoubleAnimation
            {
                To = targetX,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(animation, PageTransform);
            Storyboard.SetTargetProperty(animation, new PropertyPath(TranslateTransform.XProperty));
            storyboard.Children.Add(animation);

            storyboard.Completed += (s, e) =>
            {
                CurrentPageIndex = pageIndex;
                PageTransform.X = 0;
                PageChanged?.Invoke(this, pageIndex);
            };

            storyboard.Begin();
        }

        private void AnimatePageTransform(double targetX)
        {
            var animation = new DoubleAnimation
            {
                To = targetX,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            PageTransform.BeginAnimation(TranslateTransform.XProperty, animation);
        }

        private void PageIndicator_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is int pageIndex)
            {
                NavigateToPage(pageIndex);
            }
        }

        #region Touch Manipulation Events

        private bool _isManipulating = false;
        
        private void PageContainer_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = this;
            e.Mode = ManipulationModes.All; // Enable all manipulation modes
            _isManipulating = false;

            _logger.LogDebug($"[TOUCH] Manipulation starting with {e.Manipulators.Count()} fingers");
            e.Handled = true;
        }

        private void PageContainer_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            var deltaX = e.DeltaManipulation.Translation.X;
            var deltaY = e.DeltaManipulation.Translation.Y;
            var totalX = e.CumulativeManipulation.Translation.X;
            var totalY = e.CumulativeManipulation.Translation.Y;
            var fingerCount = e.Manipulators.Count();

            _logger.LogDebug($"[TOUCH] Delta: X={deltaX:F1}, Y={deltaY:F1}, Total: X={totalX:F1}, Y={totalY:F1}, Fingers={fingerCount}");

            // Process horizontal swipes for any finger count (1, 2, 3+)
            if (Math.Abs(totalX) > Math.Abs(totalY) && Math.Abs(totalX) > 5)
            {
                _isManipulating = true;

                // Apply transform for visual feedback with some resistance at edges
                var resistance = 1.0;
                if ((_currentPageIndex == 0 && totalX > 0) || (_currentPageIndex == _pages.Count - 1 && totalX < 0))
                {
                    resistance = 0.3; // Reduced movement at edges
                }

                PageTransform.X = totalX * resistance;

                _logger.LogDebug($"[TOUCH] Horizontal swipe detected: {PageTransform.X:F1}");
                e.Handled = true;
            }
            else if (Math.Abs(totalY) > Math.Abs(totalX) && Math.Abs(totalY) > 30)
            {
                // Vertical swipe - could be for page selector from top
                if (totalY > 50 && e.ManipulationOrigin.Y <= 50)
                {
                    _logger.LogDebug("[TOUCH] Vertical swipe from top - showing page selector");
                    ShowPageSelector();
                    e.Complete();
                }
                else
                {
                    // Other vertical movements, let widgets handle
                    e.Complete();
                }
            }
        }

        private void PageContainer_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            var totalX = e.TotalManipulation.Translation.X;
            var fingerCount = e.Manipulators.Count();

            _logger.LogDebug($"[TOUCH] Manipulation completed: totalX={totalX:F1}, was manipulating={_isManipulating}, fingers={fingerCount}");

            if (_isManipulating || Math.Abs(totalX) > 10)
            {
                // Use a lower threshold for touch gestures (1/4 of container width)
                var threshold = Math.Max(ActualWidth / 4, 60); // Minimum 60px threshold

                _logger.LogDebug($"[TOUCH] Threshold: {threshold:F1}, totalX: {totalX:F1}");

                if (Math.Abs(totalX) > threshold)
                {
                    if (totalX > 0 && _currentPageIndex > 0)
                    {
                        // Swiped right - go to previous page
                        _logger.LogDebug($"[TOUCH] Swiping right: page {_currentPageIndex} -> {_currentPageIndex - 1}");
                        NavigateToPage(_currentPageIndex - 1);
                    }
                    else if (totalX < 0 && _currentPageIndex < _pages.Count - 1)
                    {
                        // Swiped left - go to next page
                        _logger.LogDebug($"[TOUCH] Swiping left: page {_currentPageIndex} -> {_currentPageIndex + 1}");
                        NavigateToPage(_currentPageIndex + 1);
                    }
                    else
                    {
                        // At edge, bounce back
                        _logger.LogDebug($"[TOUCH] At edge, bouncing back");
                        AnimatePageTransform(0);
                    }
                }
                else
                {
                    // Not enough movement, bounce back
                    _logger.LogDebug($"[TOUCH] Not enough movement, bouncing back");
                    AnimatePageTransform(0);
                }

                _isManipulating = false;
            }

            e.Handled = true;
        }

        private void PageContainer_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            // Configure inertia for smooth completion
            e.TranslationBehavior.DesiredDeceleration = 0.01;
            e.Handled = true;
        }

        #endregion
        
        #region Edge Click Navigation
        
        private void LeftEdge_Click(object sender, MouseButtonEventArgs e)
        {
            _logger.LogDebug("Left edge clicked - navigating to previous page");
            if (_currentPageIndex > 0)
            {
                NavigateToPage(_currentPageIndex - 1);
            }
            e.Handled = true;
        }
        
        private void RightEdge_Click(object sender, MouseButtonEventArgs e)
        {
            _logger.LogDebug("Right edge clicked - navigating to next page");
            if (_currentPageIndex < _pages.Count - 1)
            {
                NavigateToPage(_currentPageIndex + 1);
            }
            e.Handled = true;
        }
        
        private void TopEdge_Click(object sender, MouseButtonEventArgs e)
        {
            _logger.LogDebug("Top edge clicked - showing page selector");
            ShowPageSelector();
            e.Handled = true;
        }
        
        #endregion
        
        #region Swipe Down Page Selector
        
        private void PageContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);
            _swipeStartPoint = position;
            _swipeStartTime = DateTime.Now;
            _currentSwipeProgress = 0;

            _logger.LogDebug($"[MOUSE] Button down at {position}");

            // Track for swipe down gesture
            _isSwipeTracking = true;
            _logger.LogDebug($"[MOUSE] Swipe down tracking started at {position}");

            // Don't mark as handled initially to allow widget interactions
        }
        
        private void PageContainer_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _isSwipeTracking)
            {
                var currentPosition = e.GetPosition(this);
                var deltaY = currentPosition.Y - _swipeStartPoint.Y;
                var timeElapsed = (DateTime.Now - _swipeStartTime).TotalMilliseconds;

                _logger.LogDebug($"[MOUSE] Mouse move: deltaY={deltaY:F1}, time={timeElapsed:F0}ms");

                // Update swipe progress for visual feedback
                _currentSwipeProgress = Math.Max(0, deltaY);
                UpdateSwipeVisualFeedback(_currentSwipeProgress);

                // Vertical swipe detection (for page selector)
                if (deltaY > SwipeThreshold)
                {
                    _logger.LogDebug($"[MOUSE] Swipe down detected: {deltaY:F1}px - showing animated page selector");
                    ShowAnimatedPageSelector();
                    _isSwipeTracking = false;
                    e.Handled = true;
                }
            }
        }
        
        private void PageContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSwipeTracking)
            {
                _logger.LogDebug($"[MOUSE] Mouse up - PageSelector visible: {PageSelectorOverlay.Visibility}");

                // Reset visual feedback
                ResetSwipeVisualFeedback();
                _isSwipeTracking = false;

                // Prevent the mouse up from immediately closing the page selector
                if (PageSelectorOverlay.Visibility == Visibility.Visible)
                {
                    e.Handled = true;
                    return;
                }

                // Only handle if we were actually tracking a gesture
                var currentPosition = e.GetPosition(this);
                var deltaY = currentPosition.Y - _swipeStartPoint.Y;

                if (Math.Abs(deltaY) > 10)
                {
                    e.Handled = true;
                }
            }
        }
        
        private void ShowPageSelector()
        {
            _logger.LogDebug("Showing page selector overlay");
            UpdatePageIndicators(); // Refresh the page list
            PageSelectorOverlay.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Shows the page selector with iPhone-style slide animation
        /// </summary>
        private void ShowAnimatedPageSelector()
        {
            _logger.LogDebug("[PAGE SELECTOR] Showing animated page selector");

            UpdatePageIndicators(); // Refresh the page list
            _pageSelectorJustOpened = true;

            _logger.LogDebug($"[PAGE SELECTOR] Container size: {ActualWidth}x{ActualHeight}");

            // Stop any running animations first
            PageSelectorOverlay.BeginAnimation(UIElement.OpacityProperty, null);

            // Ensure completely clean state first
            PageSelectorOverlay.RenderTransform = null;
            PageSelectorOverlay.ClearValue(UIElement.OpacityProperty);
            PageSelectorOverlay.Opacity = 1.0;
            PageSelectorOverlay.Visibility = Visibility.Visible;

            _logger.LogDebug($"[PAGE SELECTOR] State before animation - Visibility: {PageSelectorOverlay.Visibility}, Opacity: {PageSelectorOverlay.Opacity}");

            // Set initial position (off-screen above)
            var transform = new TranslateTransform(0, -ActualHeight);
            PageSelectorOverlay.RenderTransform = transform;

            _logger.LogDebug($"[PAGE SELECTOR] Overlay visibility: {PageSelectorOverlay.Visibility}, Opacity: {PageSelectorOverlay.Opacity}, Transform Y: {transform.Y}");

            // Animate slide down
            var storyboard = new Storyboard();
            var slideAnimation = new DoubleAnimation
            {
                From = -ActualHeight,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(slideAnimation, transform);
            Storyboard.SetTargetProperty(slideAnimation, new PropertyPath(TranslateTransform.YProperty));
            storyboard.Children.Add(slideAnimation);

            // Add fade in for background
            var fadeAnimation = new DoubleAnimation
            {
                From = 0.7,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            Storyboard.SetTarget(fadeAnimation, PageSelectorOverlay);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(fadeAnimation);

            storyboard.Completed += (s, e) =>
            {
                // Allow closing after animation completes
                _logger.LogDebug("[PAGE SELECTOR] Show animation completed, can now close");
                _pageSelectorJustOpened = false;
            };

            storyboard.Begin();
        }

        /// <summary>
        /// Updates visual feedback during swipe gesture
        /// </summary>
        private void UpdateSwipeVisualFeedback(double swipeProgress)
        {
            // Create subtle visual feedback during swipe
            var normalizedProgress = Math.Min(1.0, swipeProgress / SwipeThreshold);

            // Slightly fade the current page content to indicate gesture progress
            CurrentPageScroller.Opacity = 1.0 - (normalizedProgress * 0.3);

            // You could add more visual effects here like:
            // - Slight scaling of content
            // - Blur effects
            // - Color tinting
        }

        /// <summary>
        /// Resets visual feedback when swipe is released
        /// </summary>
        private void ResetSwipeVisualFeedback()
        {
            var storyboard = new Storyboard();
            var opacityAnimation = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(opacityAnimation, CurrentPageScroller);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(opacityAnimation);
            storyboard.Begin();
        }
        
        /// <summary>
        /// Public method to test the page selector (for debugging)
        /// </summary>
        public void TestShowPageSelector()
        {
            ShowPageSelector();
        }

        /// <summary>
        /// Public method to test navigation (for debugging)
        /// </summary>
        public void TestNavigateNext()
        {
            if (_currentPageIndex < _pages.Count - 1)
            {
                _logger.LogDebug($"[TEST] Navigating from page {_currentPageIndex} to {_currentPageIndex + 1}");
                NavigateToPage(_currentPageIndex + 1);
            }
        }

        /// <summary>
        /// Public method to test navigation (for debugging)
        /// </summary>
        public void TestNavigatePrevious()
        {
            if (_currentPageIndex > 0)
            {
                _logger.LogDebug($"[TEST] Navigating from page {_currentPageIndex} to {_currentPageIndex - 1}");
                NavigateToPage(_currentPageIndex - 1);
            }
        }
        
        private void HidePageSelector()
        {
            _logger.LogDebug("Hiding page selector overlay");
            HideAnimatedPageSelector();
        }

        /// <summary>
        /// Hides the page selector with iPhone-style slide animation
        /// </summary>
        private void HideAnimatedPageSelector()
        {
            _logger.LogDebug("[PAGE SELECTOR] Starting hide animation");

            var transform = PageSelectorOverlay.RenderTransform as TranslateTransform;
            if (transform == null)
            {
                transform = new TranslateTransform();
                PageSelectorOverlay.RenderTransform = transform;
            }

            var storyboard = new Storyboard();

            // Animate slide up
            var slideAnimation = new DoubleAnimation
            {
                From = transform.Y,
                To = -ActualHeight,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            Storyboard.SetTarget(slideAnimation, transform);
            Storyboard.SetTargetProperty(slideAnimation, new PropertyPath(TranslateTransform.YProperty));
            storyboard.Children.Add(slideAnimation);

            // Add fade out
            var fadeAnimation = new DoubleAnimation
            {
                From = PageSelectorOverlay.Opacity,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(150)
            };

            Storyboard.SetTarget(fadeAnimation, PageSelectorOverlay);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(fadeAnimation);

            storyboard.Completed += (s, e) =>
            {
                // Completely reset all state
                PageSelectorOverlay.Visibility = Visibility.Collapsed;
                PageSelectorOverlay.RenderTransform = null;
                PageSelectorOverlay.ClearValue(UIElement.OpacityProperty);
                PageSelectorOverlay.Opacity = 1.0;

                // Stop any lingering animations
                PageSelectorOverlay.BeginAnimation(UIElement.OpacityProperty, null);

                _logger.LogDebug("[PAGE SELECTOR] Hide animation completed - all state reset");
            };

            storyboard.Begin();
        }
        
        private void PageSelectorOverlay_Click(object sender, MouseButtonEventArgs e)
        {
            var timeSinceLastClose = (DateTime.Now - _lastCloseTime).TotalMilliseconds;

            _logger.LogDebug($"[PAGE SELECTOR] Overlay clicked - Source: {e.Source?.GetType().Name}, JustOpened: {_pageSelectorJustOpened}, TimeSinceLastClose: {timeSinceLastClose}ms");

            // Prevent rapid repeated closes (less than 500ms apart)
            if (timeSinceLastClose < 500)
            {
                _logger.LogDebug("[PAGE SELECTOR] Ignoring click - too soon after last close");
                e.Handled = true;
                return;
            }

            // Don't close immediately after opening
            if (_pageSelectorJustOpened)
            {
                _logger.LogDebug("[PAGE SELECTOR] Ignoring click - just opened");
                e.Handled = true;
                return;
            }

            // Only hide if clicking the overlay background itself (not content borders)
            if (e.Source == PageSelectorOverlay && e.OriginalSource == PageSelectorOverlay)
            {
                _logger.LogDebug("[PAGE SELECTOR] Hiding page selector - clicked true background");
                _lastCloseTime = DateTime.Now;
                HidePageSelector();
                e.Handled = true;
            }
            else
            {
                _logger.LogDebug($"[PAGE SELECTOR] Not closing - Source: {e.Source?.GetType().Name}, OriginalSource: {e.OriginalSource?.GetType().Name}");
                e.Handled = true;
            }
        }
        
        private void PageListItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is int pageIndex)
            {
                _logger.LogDebug($"[PAGE SELECTOR] Page {pageIndex} selected from overlay");
                NavigateToPage(pageIndex);
                _lastCloseTime = DateTime.Now;
                HidePageSelector();
                e.Handled = true;
            }
        }

        private void ClosePageSelector_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogDebug("[PAGE SELECTOR] Close button clicked");
            _lastCloseTime = DateTime.Now;
            HidePageSelector();
        }
        
        #endregion

        #region Touch and Stylus Events

        private void PageContainer_TouchDown(object sender, TouchEventArgs e)
        {
            var position = e.GetTouchPoint(this).Position;
            _logger.LogDebug($"[TOUCH] Touch down at {position}");

            _swipeStartPoint = position;
            _swipeStartTime = DateTime.Now;
            _currentSwipeProgress = 0;

            // Only track swipe down from anywhere on screen (simplified)
            _isSwipeTracking = true;
        }

        private void PageContainer_TouchMove(object sender, TouchEventArgs e)
        {
            if (_isSwipeTracking)
            {
                var currentPosition = e.GetTouchPoint(this).Position;
                var deltaY = currentPosition.Y - _swipeStartPoint.Y;
                var timeElapsed = (DateTime.Now - _swipeStartTime).TotalMilliseconds;

                _logger.LogDebug($"[TOUCH] Touch move: deltaY={deltaY:F1}, time={timeElapsed:F0}ms");

                // Update swipe progress for visual feedback
                _currentSwipeProgress = Math.Max(0, deltaY);
                UpdateSwipeVisualFeedback(_currentSwipeProgress);

                // Check for swipe down (page selector)
                if (deltaY > SwipeThreshold)
                {
                    _logger.LogDebug($"[TOUCH] Swipe down detected: {deltaY:F1}px - showing animated page selector");
                    ShowAnimatedPageSelector();
                    _isSwipeTracking = false;
                    e.Handled = true;
                }
            }
            else
            {
                _logger.LogDebug($"[TOUCH] Touch move ignored - not tracking (isSwipeTracking: {_isSwipeTracking})");
            }
        }

        private void PageContainer_TouchUp(object sender, TouchEventArgs e)
        {
            _logger.LogDebug($"[TOUCH] Touch up - PageSelector visible: {PageSelectorOverlay.Visibility}");

            if (_isSwipeTracking)
            {
                // Reset visual feedback
                ResetSwipeVisualFeedback();
                _isSwipeTracking = false;
            }

            // Prevent the touch up from immediately closing the page selector
            if (PageSelectorOverlay.Visibility == Visibility.Visible)
            {
                e.Handled = true;
            }
        }

        private void PageContainer_StylusDown(object sender, StylusDownEventArgs e)
        {
            var position = e.GetPosition(this);
            _logger.LogDebug($"[STYLUS] Stylus down at {position}");
        }

        private void PageContainer_StylusMove(object sender, StylusEventArgs e)
        {
            var position = e.GetPosition(this);
            _logger.LogDebug($"[STYLUS] Stylus move at {position}");
        }

        private void PageContainer_StylusUp(object sender, StylusEventArgs e)
        {
            _logger.LogDebug($"[STYLUS] Stylus up");
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a page indicator dot
    /// </summary>
    public class PageIndicator
    {
        public int PageIndex { get; set; }
        public string PageName { get; set; } = string.Empty;
        public System.Windows.Media.Brush Fill { get; set; } = System.Windows.Media.Brushes.Gray;
    }
    
    /// <summary>
    /// Represents a page item in the selector overlay
    /// </summary>
    public class PageListItem
    {
        public int PageIndex { get; set; }
        public string PageName { get; set; } = string.Empty;
        public int WidgetCount { get; set; }
        public System.Windows.Media.Brush Fill { get; set; } = System.Windows.Media.Brushes.Gray;
    }

    /// <summary>
    /// Converts page index to display number (0-based to 1-based)
    /// </summary>
    public class PageNumberConverter : IValueConverter
    {
        public static readonly PageNumberConverter Instance = new PageNumberConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int pageIndex)
                return (pageIndex + 1).ToString();
            return "1";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}