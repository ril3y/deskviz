using System.Configuration;
using System.Data;
using System.Windows;
using System.Diagnostics;
using System.Windows.Threading;
using System;

namespace DeskViz.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Console.WriteLine("App.OnStartup - CONSOLE TEST"); // Added for console output test
        Debug.WriteLine("App.OnStartup starting...");
        base.OnStartup(e);
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        Debug.WriteLine("App.OnStartup finished.");
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Debug.WriteLine("!!!! UNHANDLED EXCEPTION !!!!");
        Debug.WriteLine($"Exception: {e.Exception.GetType().Name}");
        Debug.WriteLine($"Message: {e.Exception.Message}");
        Debug.WriteLine($"StackTrace: {e.Exception.StackTrace}");

        // Prevent default WPF crash behavior
        e.Handled = true;

        // Optional: Show a user-friendly message
        System.Windows.MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}\n\nThe application might become unstable.", "Unhandled Error", MessageBoxButton.OK, MessageBoxImage.Error);

        // Decide if you want to shut down or attempt to continue
        // Application.Current.Shutdown(); 
    }
}
