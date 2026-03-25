using Microsoft.Extensions.Logging;

namespace DeskViz.App.Services
{
    /// <summary>
    /// Static factory for creating loggers throughout the application.
    /// Call Initialize() once at startup to configure the logging pipeline.
    /// </summary>
    public static class AppLoggerFactory
    {
        private static ILoggerFactory _factory = LoggerFactory.Create(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        public static void Initialize(ILoggerFactory factory)
        {
            _factory = factory;
        }

        public static ILogger<T> CreateLogger<T>() => _factory.CreateLogger<T>();

        public static ILogger CreateLogger(string categoryName) => _factory.CreateLogger(categoryName);
    }
}
