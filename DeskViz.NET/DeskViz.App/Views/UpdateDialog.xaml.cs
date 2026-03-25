using System;
using System.Reflection;
using System.Windows;
using DeskViz.Core.Models;
using DeskViz.Core.Services;

namespace DeskViz.App.Views
{
    /// <summary>
    /// Describes the action the user chose in the update dialog.
    /// </summary>
    public enum UpdateAction
    {
        UpdateNow,
        SkipVersion,
        RemindLater
    }

    /// <summary>
    /// A dialog that presents update information and lets the user choose how to proceed.
    /// </summary>
    public partial class UpdateDialog : Window
    {
        private readonly IUpdateService _updateService;

        /// <summary>
        /// The release being presented to the user.
        /// </summary>
        public ReleaseInfo Release { get; }

        /// <summary>
        /// The action chosen by the user when the dialog closes.
        /// </summary>
        public UpdateAction ChosenAction { get; private set; } = UpdateAction.RemindLater;

        /// <summary>
        /// Whether the user wants to apply the app update.
        /// </summary>
        public bool IncludeAppUpdate => AppUpdateCheckBox.IsChecked == true;

        /// <summary>
        /// Whether the user wants to apply the widget update.
        /// </summary>
        public bool IncludeWidgetUpdate => WidgetUpdateCheckBox.IsChecked == true;

        public UpdateDialog(ReleaseInfo release, IUpdateService updateService)
        {
            Release = release ?? throw new ArgumentNullException(nameof(release));
            _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));

            InitializeComponent();

            // Populate version info
            var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
            CurrentVersionText.Text = currentVersion.ToString(3);
            NewVersionText.Text = release.Version?.ToString(3) ?? release.TagName;

            // Populate release notes
            ReleaseNotesText.Text = string.IsNullOrWhiteSpace(release.Body)
                ? "No release notes provided."
                : release.Body;

            // Configure checkboxes based on available assets
            AppUpdateCheckBox.IsChecked = release.HasAppUpdate;
            AppUpdateCheckBox.IsEnabled = release.HasAppUpdate;

            WidgetUpdateCheckBox.IsChecked = release.HasWidgetUpdate;
            WidgetUpdateCheckBox.IsEnabled = release.HasWidgetUpdate;

            // Subscribe to progress events
            _updateService.DownloadProgressChanged += OnDownloadProgress;
            _updateService.UpdateError += OnUpdateError;
        }

        private void OnDownloadProgress(object? sender, UpdateProgressEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                DownloadProgress.Value = e.ProgressPercent;
                StatusText.Text = e.Message;
            });
        }

        private void OnUpdateError(object? sender, UpdateErrorEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = $"Error: {e.Message}";
                SetButtonsEnabled(true);
            });
        }

        private void UpdateNow_Click(object sender, RoutedEventArgs e)
        {
            ChosenAction = UpdateAction.UpdateNow;

            // Show progress UI
            ProgressPanel.Visibility = Visibility.Visible;
            SetButtonsEnabled(false);
            StatusText.Text = "Starting update...";
            DownloadProgress.Value = 0;

            // The actual download/apply logic is handled by MainWindow after this dialog closes.
            DialogResult = true;
        }

        private void SkipVersion_Click(object sender, RoutedEventArgs e)
        {
            ChosenAction = UpdateAction.SkipVersion;
            DialogResult = true;
        }

        private void RemindLater_Click(object sender, RoutedEventArgs e)
        {
            ChosenAction = UpdateAction.RemindLater;
            DialogResult = false;
        }

        private void SetButtonsEnabled(bool enabled)
        {
            UpdateNowButton.IsEnabled = enabled;
            SkipButton.IsEnabled = enabled;
            RemindLaterButton.IsEnabled = enabled;
            AppUpdateCheckBox.IsEnabled = enabled && Release.HasAppUpdate;
            WidgetUpdateCheckBox.IsEnabled = enabled && Release.HasWidgetUpdate;
        }

        protected override void OnClosed(EventArgs e)
        {
            _updateService.DownloadProgressChanged -= OnDownloadProgress;
            _updateService.UpdateError -= OnUpdateError;
            base.OnClosed(e);
        }
    }
}
