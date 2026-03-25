using System;
using System.Threading;
using System.Threading.Tasks;
using DeskViz.Core.Models;

namespace DeskViz.Core.Services
{
    /// <summary>
    /// Service for checking and applying application updates from GitHub releases.
    /// </summary>
    public interface IUpdateService : IDisposable
    {
        /// <summary>
        /// Raised when a new update is detected.
        /// </summary>
        event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;

        /// <summary>
        /// Raised to report download progress during an update.
        /// </summary>
        event EventHandler<UpdateProgressEventArgs>? DownloadProgressChanged;

        /// <summary>
        /// Raised when an error occurs during update operations.
        /// </summary>
        event EventHandler<UpdateErrorEventArgs>? UpdateError;

        /// <summary>
        /// Checks GitHub for a newer release than the currently running version.
        /// Returns the release info if an update is available, or null if up to date.
        /// </summary>
        Task<ReleaseInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads the application executable from the given release.
        /// Returns the local file path of the downloaded executable, or null on failure.
        /// </summary>
        Task<string?> DownloadAppUpdateAsync(ReleaseInfo release, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads and extracts the widget archive from the given release.
        /// Returns the path to the extracted staging directory, or null on failure.
        /// </summary>
        Task<string?> DownloadWidgetUpdateAsync(ReleaseInfo release, CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies a downloaded application update by spawning a helper script that replaces the
        /// running executable and restarts the application. The provided shutdownCallback is invoked
        /// to close the current application process.
        /// </summary>
        void ApplyAppUpdate(string downloadedFilePath, Action shutdownCallback);

        /// <summary>
        /// Copies updated widget DLLs from the extracted staging directory into the widget output directory.
        /// </summary>
        Task ApplyWidgetUpdateAsync(string extractedDirectory);

        /// <summary>
        /// Starts periodic background checks for updates based on the configured interval.
        /// </summary>
        void StartPeriodicChecks();

        /// <summary>
        /// Stops periodic background update checks.
        /// </summary>
        void StopPeriodicChecks();
    }
}
