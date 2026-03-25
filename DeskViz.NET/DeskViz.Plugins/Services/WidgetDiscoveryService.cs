using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DeskViz.Plugins.Interfaces;

namespace DeskViz.Plugins.Services
{
    public class WidgetDiscoveryService
    {
        private readonly string _pluginDirectory;
        private readonly List<LoadedWidget> _loadedWidgets = new();

        public IReadOnlyList<LoadedWidget> LoadedWidgets => _loadedWidgets.AsReadOnly();

        public event EventHandler<WidgetDiscoveredEventArgs>? WidgetDiscovered;
        public event EventHandler<WidgetLoadErrorEventArgs>? WidgetLoadError;

        public WidgetDiscoveryService(string pluginDirectory)
        {
            _pluginDirectory = pluginDirectory ?? throw new ArgumentNullException(nameof(pluginDirectory));
            EnsurePluginDirectoryExists();
        }

        public void DiscoverWidgets()
        {
            _loadedWidgets.Clear();

            Console.WriteLine($"🔍 Plugin directory: {_pluginDirectory}");
            Console.WriteLine($"🔍 Directory exists: {Directory.Exists(_pluginDirectory)}");

            if (!Directory.Exists(_pluginDirectory))
            {
                Console.WriteLine("❌ Plugin directory does not exist");
                return;
            }

            var dllFiles = Directory.GetFiles(_pluginDirectory, "*.dll", SearchOption.AllDirectories);
            Console.WriteLine($"🔍 Found {dllFiles.Length} DLL files");

            foreach (var dllFile in dllFiles)
            {
                Console.WriteLine($"🔍 Examining: {Path.GetFileName(dllFile)}");
                try
                {
                    LoadWidgetsFromAssembly(dllFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error loading {Path.GetFileName(dllFile)}: {ex.Message}");
                    OnWidgetLoadError(dllFile, ex);
                }
            }

            Console.WriteLine($"🔍 Total widgets discovered: {_loadedWidgets.Count}");
        }

        public IWidgetPlugin? CreateWidgetInstance(string widgetId)
        {
            var loadedWidget = _loadedWidgets.FirstOrDefault(w => w.Metadata.Id == widgetId);
            if (loadedWidget == null)
            {
                return null;
            }

            try
            {
                return (IWidgetPlugin?)Activator.CreateInstance(loadedWidget.WidgetType);
            }
            catch (Exception ex)
            {
                OnWidgetLoadError(loadedWidget.AssemblyPath, ex);
                return null;
            }
        }

        public bool IsWidgetAvailable(string widgetId)
        {
            return _loadedWidgets.Any(w => w.Metadata.Id == widgetId);
        }

        public IWidgetMetadata? GetWidgetMetadata(string widgetId)
        {
            return _loadedWidgets.FirstOrDefault(w => w.Metadata.Id == widgetId)?.Metadata;
        }

        private void LoadWidgetsFromAssembly(string assemblyPath)
        {
            Console.WriteLine($"  📂 Loading assembly: {Path.GetFileName(assemblyPath)}");

            var assembly = Assembly.LoadFrom(assemblyPath);
            var allTypes = assembly.GetTypes();
            Console.WriteLine($"  📂 Assembly contains {allTypes.Length} types");

            var widgetTypes = allTypes
                .Where(t => typeof(IWidgetPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .ToList();

            Console.WriteLine($"  📂 Found {widgetTypes.Count} widget types implementing IWidgetPlugin");

            foreach (var type in widgetTypes)
            {
                Console.WriteLine($"    🔧 Widget type: {type.FullName}");
            }

            foreach (var widgetType in widgetTypes)
            {
                try
                {
                    Console.WriteLine($"  🔧 Creating instance of: {widgetType.Name}");
                    var tempInstance = (IWidgetPlugin?)Activator.CreateInstance(widgetType);

                    if (tempInstance?.Metadata != null)
                    {
                        Console.WriteLine($"  ✅ SUCCESS: Created widget '{tempInstance.Metadata.Id}' - {tempInstance.Metadata.Name}");

                        var loadedWidget = new LoadedWidget(
                            widgetType,
                            tempInstance.Metadata,
                            assemblyPath,
                            assembly
                        );

                        _loadedWidgets.Add(loadedWidget);
                        OnWidgetDiscovered(loadedWidget);
                    }
                    else
                    {
                        Console.WriteLine($"  ❌ Widget instance or metadata is null for {widgetType.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ❌ Error creating {widgetType.Name}: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"    🔧 Inner Exception: {ex.InnerException.Message}");
                        Console.WriteLine($"    🔧 Stack Trace: {ex.InnerException.StackTrace}");
                    }
                    OnWidgetLoadError(assemblyPath, ex);
                }
            }
        }

        private void EnsurePluginDirectoryExists()
        {
            if (!Directory.Exists(_pluginDirectory))
            {
                Directory.CreateDirectory(_pluginDirectory);
            }
        }

        private void OnWidgetDiscovered(LoadedWidget loadedWidget)
        {
            WidgetDiscovered?.Invoke(this, new WidgetDiscoveredEventArgs(loadedWidget));
        }

        private void OnWidgetLoadError(string assemblyPath, Exception exception)
        {
            WidgetLoadError?.Invoke(this, new WidgetLoadErrorEventArgs(assemblyPath, exception));
        }
    }

    public class LoadedWidget
    {
        public Type WidgetType { get; }
        public IWidgetMetadata Metadata { get; }
        public string AssemblyPath { get; }
        public Assembly Assembly { get; }

        public LoadedWidget(Type widgetType, IWidgetMetadata metadata, string assemblyPath, Assembly assembly)
        {
            WidgetType = widgetType ?? throw new ArgumentNullException(nameof(widgetType));
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            AssemblyPath = assemblyPath ?? throw new ArgumentNullException(nameof(assemblyPath));
            Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        }
    }

    public class WidgetDiscoveredEventArgs : EventArgs
    {
        public LoadedWidget LoadedWidget { get; }

        public WidgetDiscoveredEventArgs(LoadedWidget loadedWidget)
        {
            LoadedWidget = loadedWidget ?? throw new ArgumentNullException(nameof(loadedWidget));
        }
    }

    public class WidgetLoadErrorEventArgs : EventArgs
    {
        public string AssemblyPath { get; }
        public Exception Exception { get; }

        public WidgetLoadErrorEventArgs(string assemblyPath, Exception exception)
        {
            AssemblyPath = assemblyPath ?? throw new ArgumentNullException(nameof(assemblyPath));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }
    }
}