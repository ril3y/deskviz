using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DeskViz.Core.Models;
using Microsoft.Extensions.Logging;

namespace DeskViz.Core.Services
{
    /// <summary>
    /// Checks for and applies updates from the GitHub releases API.
    /// </summary>
    public sealed class UpdateService : IUpdateService
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/ril3y/deskviz/releases/latest";
        private const int DownloadBufferSize = 81920;

        private readonly ISettingsService _settingsService;
        private readonly string _widgetOutputDir;
        private readonly ILogger<UpdateService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _updatesDir;

        private System.Timers.Timer? _periodicTimer;
        private bool _disposed;

        public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;
        public event EventHandler<UpdateProgressEventArgs>? DownloadProgressChanged;
        public event EventHandler<UpdateErrorEventArgs>? UpdateError;

        public UpdateService(
            ISettingsService settingsService,
            string widgetOutputDir,
            ILogger<UpdateService>? logger = null)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _widgetOutputDir = widgetOutputDir ?? throw new ArgumentNullException(nameof(widgetOutputDir));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateService>.Instance;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DeskViz-AutoUpdater");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            _updatesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DeskViz", "updates");
        }

        /// <summary>
        /// Returns the version of the currently running application.
        /// </summary>
        private static Version CurrentVersion
        {
            get
            {
                var asm = Assembly.GetEntryAssembly();
                return asm?.GetName().Version ?? new Version(0, 0, 0);
            }
        }

        public async Task<ReleaseInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Checking for updates from GitHub...");

                var response = await _httpClient.GetAsync(GitHubApiUrl, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var release = ParseRelease(json);

                if (release == null)
                {
                    _logger.LogWarning("Failed to parse release information from GitHub response.");
                    return null;
                }

                // Record the check time
                _settingsService.Settings.LastUpdateCheck = DateTime.UtcNow;
                _settingsService.SaveSettings();

                var remoteVersion = release.Version;
                if (remoteVersion == null)
                {
                    _logger.LogWarning("Could not parse version from tag: {Tag}", release.TagName);
                    return null;
                }

                _logger.LogInformation("Current version: {Current}, Latest release: {Latest}",
                    CurrentVersion, remoteVersion);

                // Skip if this version was explicitly skipped by the user
                var skipped = _settingsService.Settings.SkippedVersion;
                if (!string.IsNullOrEmpty(skipped) && skipped == release.TagName)
                {
                    _logger.LogInformation("User has chosen to skip version {Version}.", release.TagName);
                    return null;
                }

                if (remoteVersion > CurrentVersion)
                {
                    _logger.LogInformation("Update available: {Version}", remoteVersion);
                    UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs(release));
                    return release;
                }

                _logger.LogInformation("Application is up to date.");
                return null;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Update check was cancelled.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates.");
                UpdateError?.Invoke(this, new UpdateErrorEventArgs("Failed to check for updates.", ex));
                return null;
            }
        }

        public async Task<string?> DownloadAppUpdateAsync(ReleaseInfo release, CancellationToken cancellationToken = default)
        {
            if (release.AppAsset == null)
            {
                _logger.LogWarning("Release does not contain an application update asset.");
                return null;
            }

            try
            {
                EnsureUpdatesDirectory();
                var targetPath = Path.Combine(_updatesDir, "DeskViz.App.exe");

                await DownloadFileAsync(
                    release.AppAsset.BrowserDownloadUrl,
                    targetPath,
                    release.AppAsset.Size,
                    "Downloading application update...",
                    cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("App update downloaded to: {Path}", targetPath);
                return targetPath;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("App update download was cancelled.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading app update.");
                UpdateError?.Invoke(this, new UpdateErrorEventArgs("Failed to download application update.", ex));
                return null;
            }
        }

        public async Task<string?> DownloadWidgetUpdateAsync(ReleaseInfo release, CancellationToken cancellationToken = default)
        {
            if (release.WidgetAsset == null)
            {
                _logger.LogWarning("Release does not contain a widget update asset.");
                return null;
            }

            try
            {
                EnsureUpdatesDirectory();
                var zipPath = Path.Combine(_updatesDir, "widgets.zip");
                var extractDir = Path.Combine(_updatesDir, "widgets_staging");

                // Clean up previous staging directory
                if (Directory.Exists(extractDir))
                {
                    Directory.Delete(extractDir, recursive: true);
                }

                await DownloadFileAsync(
                    release.WidgetAsset.BrowserDownloadUrl,
                    zipPath,
                    release.WidgetAsset.Size,
                    "Downloading widget update...",
                    cancellationToken).ConfigureAwait(false);

                ReportProgress(90, "Extracting widget update...");
                ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);

                // Clean up the zip
                File.Delete(zipPath);

                ReportProgress(100, "Widget update extracted.");
                _logger.LogInformation("Widget update extracted to: {Path}", extractDir);
                return extractDir;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Widget update download was cancelled.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading widget update.");
                UpdateError?.Invoke(this, new UpdateErrorEventArgs("Failed to download widget update.", ex));
                return null;
            }
        }

        public void ApplyAppUpdate(string downloadedFilePath, Action shutdownCallback)
        {
            if (string.IsNullOrEmpty(downloadedFilePath) || !File.Exists(downloadedFilePath))
            {
                _logger.LogError("Downloaded file does not exist: {Path}", downloadedFilePath);
                UpdateError?.Invoke(this, new UpdateErrorEventArgs("Downloaded update file not found."));
                return;
            }

            try
            {
                var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(currentExe))
                {
                    _logger.LogError("Could not determine current executable path.");
                    UpdateError?.Invoke(this, new UpdateErrorEventArgs("Could not determine current executable path."));
                    return;
                }

                var currentPid = Process.GetCurrentProcess().Id;
                var scriptPath = Path.Combine(_updatesDir, "update.cmd");

                // Write a batch script that:
                // 1. Waits for the current process to exit
                // 2. Copies the new exe over the old one
                // 3. Restarts the application
                // 4. Deletes itself
                var script = $"""
                              @echo off
                              echo Waiting for DeskViz to exit...
                              :waitloop
                              tasklist /FI "PID eq {currentPid}" 2>NUL | find /I "{currentPid}" >NUL
                              if not errorlevel 1 (
                                  timeout /t 1 /nobreak >NUL
                                  goto waitloop
                              )
                              echo Applying update...
                              copy /Y "{downloadedFilePath}" "{currentExe}"
                              echo Starting updated application...
                              start "" "{currentExe}"
                              echo Cleaning up...
                              del "{downloadedFilePath}" 2>NUL
                              del "%~f0" 2>NUL
                              """;

                File.WriteAllText(scriptPath, script);

                _logger.LogInformation("Starting update script: {Script}", scriptPath);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{scriptPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(startInfo);

                _logger.LogInformation("Update script launched. Shutting down application.");
                shutdownCallback();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying app update.");
                UpdateError?.Invoke(this, new UpdateErrorEventArgs("Failed to apply application update.", ex));
            }
        }

        public async Task ApplyWidgetUpdateAsync(string extractedDirectory)
        {
            if (string.IsNullOrEmpty(extractedDirectory) || !Directory.Exists(extractedDirectory))
            {
                _logger.LogError("Extracted widget directory does not exist: {Path}", extractedDirectory);
                UpdateError?.Invoke(this, new UpdateErrorEventArgs("Widget update staging directory not found."));
                return;
            }

            try
            {
                // Ensure the widget output directory exists
                if (!Directory.Exists(_widgetOutputDir))
                {
                    Directory.CreateDirectory(_widgetOutputDir);
                }

                var files = Directory.GetFiles(extractedDirectory, "*.*", SearchOption.AllDirectories);
                _logger.LogInformation("Copying {Count} widget files to {Dir}", files.Length, _widgetOutputDir);

                foreach (var sourceFile in files)
                {
                    // Preserve subdirectory structure relative to the staging directory
                    var relativePath = Path.GetRelativePath(extractedDirectory, sourceFile);
                    var destPath = Path.Combine(_widgetOutputDir, relativePath);
                    var destDir = Path.GetDirectoryName(destPath);

                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    File.Copy(sourceFile, destPath, overwrite: true);
                    _logger.LogDebug("Copied: {File}", relativePath);
                }

                // Clean up staging directory
                await Task.Run(() => Directory.Delete(extractedDirectory, recursive: true)).ConfigureAwait(false);

                _logger.LogInformation("Widget update applied successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying widget update.");
                UpdateError?.Invoke(this, new UpdateErrorEventArgs("Failed to apply widget update.", ex));
            }
        }

        public void StartPeriodicChecks()
        {
            if (!_settingsService.Settings.AutoUpdateEnabled)
            {
                _logger.LogDebug("Auto-update is disabled. Periodic checks will not start.");
                return;
            }

            StopPeriodicChecks();

            var intervalHours = Math.Max(1, _settingsService.Settings.UpdateCheckIntervalHours);
            var intervalMs = intervalHours * 3600_000.0;

            _periodicTimer = new System.Timers.Timer(intervalMs);
            _periodicTimer.Elapsed += async (_, _) =>
            {
                try
                {
                    await CheckForUpdateAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during periodic update check.");
                }
            };
            _periodicTimer.AutoReset = true;
            _periodicTimer.Start();

            _logger.LogInformation("Periodic update checks started (every {Hours} hours).", intervalHours);
        }

        public void StopPeriodicChecks()
        {
            if (_periodicTimer != null)
            {
                _periodicTimer.Stop();
                _periodicTimer.Dispose();
                _periodicTimer = null;
                _logger.LogInformation("Periodic update checks stopped.");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            StopPeriodicChecks();
            _httpClient.Dispose();
        }

        // ── Private helpers ───────────────────────────────────────────────

        private void EnsureUpdatesDirectory()
        {
            if (!Directory.Exists(_updatesDir))
            {
                Directory.CreateDirectory(_updatesDir);
            }
        }

        private async Task DownloadFileAsync(
            string url,
            string destinationPath,
            long expectedSize,
            string progressMessage,
            CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? expectedSize;

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, DownloadBufferSize, useAsync: true);

            var buffer = new byte[DownloadBufferSize];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    var percent = (int)(totalRead * 100 / totalBytes);
                    ReportProgress(percent, progressMessage);
                }
            }
        }

        private void ReportProgress(int percent, string message)
        {
            DownloadProgressChanged?.Invoke(this, new UpdateProgressEventArgs(percent, message));
        }

        private static ReleaseInfo? ParseRelease(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tag_name", out var tagProp))
                return null;

            var release = new ReleaseInfo
            {
                TagName = tagProp.GetString() ?? string.Empty,
                Name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty,
                Body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? string.Empty : string.Empty,
                HtmlUrl = root.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() ?? string.Empty : string.Empty,
                PublishedAt = root.TryGetProperty("published_at", out var dateProp) && dateProp.TryGetDateTime(out var dt)
                    ? dt
                    : DateTime.MinValue,
            };

            if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var assetEl in assetsProp.EnumerateArray())
                {
                    var asset = new ReleaseAsset
                    {
                        Name = assetEl.TryGetProperty("name", out var aN) ? aN.GetString() ?? string.Empty : string.Empty,
                        BrowserDownloadUrl = assetEl.TryGetProperty("browser_download_url", out var aUrl)
                            ? aUrl.GetString() ?? string.Empty
                            : string.Empty,
                        Size = assetEl.TryGetProperty("size", out var aSize) ? aSize.GetInt64() : 0,
                    };
                    release.Assets.Add(asset);
                }
            }

            return release;
        }
    }
}
