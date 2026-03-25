using System.Windows;
using System.Windows.Threading;
using System;
using DeskViz.App.Services;
using Microsoft.Extensions.Logging;

namespace DeskViz.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private readonly ILogger _logger = AppLoggerFactory.CreateLogger<App>();

    protected override void OnStartup(StartupEventArgs e)
    {
        _logger.LogInformation("App.OnStartup starting...");
        base.OnStartup(e);
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        _logger.LogInformation("App.OnStartup finished.");
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger.LogCritical(e.Exception, $"UNHANDLED EXCEPTION: {e.Exception.GetType().Name}: {e.Exception.Message}");

        // Log inner exceptions
        var inner = e.Exception.InnerException;
        while (inner != null)
        {
            _logger.LogCritical(inner, $"  Inner: {inner.GetType().Name}: {inner.Message}");
            inner = inner.InnerException;
        }

        // Show error to user so they know something went wrong
        System.Windows.MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nThe application will continue, but may be unstable.",
            "DeskViz Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        // Only handle exceptions we can reasonably recover from (UI-level errors).
        // For critical framework exceptions, let the app crash rather than run in corrupt state.
        if (e.Exception is OutOfMemoryException
            or StackOverflowException
            or AccessViolationException
            or System.Runtime.InteropServices.SEHException)
        {
            e.Handled = false; // Let it crash
        }
        else
        {
            e.Handled = true;
        }
    }
}
