using System;
using System.Collections.Generic;
using System.Linq;

namespace DeskViz.Core.Models
{
    /// <summary>
    /// Represents a GitHub release with its assets and metadata.
    /// </summary>
    public class ReleaseInfo
    {
        public string TagName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string HtmlUrl { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public List<ReleaseAsset> Assets { get; set; } = new();

        /// <summary>
        /// Parses the tag name into a Version object, stripping a leading 'v' if present.
        /// </summary>
        public Version? Version
        {
            get
            {
                var raw = TagName.TrimStart('v', 'V');
                return System.Version.TryParse(raw, out var v) ? v : null;
            }
        }

        /// <summary>
        /// Whether the release contains an application executable asset.
        /// </summary>
        public bool HasAppUpdate => AppAsset != null;

        /// <summary>
        /// Whether the release contains a widget archive asset.
        /// </summary>
        public bool HasWidgetUpdate => WidgetAsset != null;

        /// <summary>
        /// The application executable asset, if present.
        /// </summary>
        public ReleaseAsset? AppAsset =>
            Assets.FirstOrDefault(a => a.Name.Equals("DeskViz.App.exe", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// The widget archive asset, if present.
        /// </summary>
        public ReleaseAsset? WidgetAsset =>
            Assets.FirstOrDefault(a => a.Name.Equals("widgets.zip", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Represents a downloadable asset attached to a GitHub release.
    /// </summary>
    public class ReleaseAsset
    {
        public string Name { get; set; } = string.Empty;
        public string BrowserDownloadUrl { get; set; } = string.Empty;
        public long Size { get; set; }
    }

    /// <summary>
    /// Event arguments raised when a new update is available.
    /// </summary>
    public class UpdateAvailableEventArgs : EventArgs
    {
        public ReleaseInfo Release { get; }

        public UpdateAvailableEventArgs(ReleaseInfo release)
        {
            Release = release ?? throw new ArgumentNullException(nameof(release));
        }
    }

    /// <summary>
    /// Event arguments raised to report download progress.
    /// </summary>
    public class UpdateProgressEventArgs : EventArgs
    {
        public int ProgressPercent { get; }
        public string Message { get; }

        public UpdateProgressEventArgs(int progressPercent, string message)
        {
            ProgressPercent = progressPercent;
            Message = message;
        }
    }

    /// <summary>
    /// Event arguments raised when an update error occurs.
    /// </summary>
    public class UpdateErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public Exception? Exception { get; }

        public UpdateErrorEventArgs(string message, Exception? exception = null)
        {
            Message = message;
            Exception = exception;
        }
    }
}
