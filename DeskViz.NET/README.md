# DeskViz.NET Build Instructions

## Prerequisites
- .NET 8.0 SDK or later
- Windows 10/11 (for running the application)
- Visual Studio 2022 or VS Code (optional)

## Building from Windows

### Using PowerShell Scripts

```powershell
# Basic build
.\build.ps1

# Build and run
.\build.ps1 -Run

# Clean build
.\build.ps1 -Clean

# Release build with packaging
.\build.ps1 -Configuration Release -Package

# Quick development commands
.\dev.ps1 build
.\dev.ps1 run
.\dev.ps1 watch  # Auto-rebuild on file changes
```

### Using dotnet CLI directly

```powershell
# Build
dotnet build

# Run
dotnet run --project DeskViz.App/DeskViz.App.csproj

# Watch mode (auto-rebuild)
dotnet watch --project DeskViz.App/DeskViz.App.csproj run
```

## Building from WSL

### Using the bash wrapper

```bash
# Basic build
./build.sh

# Build and run
./build.sh --run

# Clean build
./build.sh --clean

# Release package
./build.sh --configuration Release --package
```

### Using PowerShell from WSL

```bash
# Run PowerShell commands
powershell.exe -File build.ps1
powershell.exe -File build.ps1 -Run

# Or use the dev script
powershell.exe -File dev.ps1 run
```

## Output Locations

- Debug builds: `bin/Debug/net8.0-windows/`
- Release builds: `bin/Release/net8.0-windows/`
- Packaged builds: `bin/Package/{Configuration}/`

## Running the Application

After building, you can run DeskViz.NET by:
1. Double-clicking `DeskViz.App.exe` in the output directory
2. Using `.\dev.ps1 run` from PowerShell
3. Using `./build.sh --run` from WSL

## Troubleshooting

- If you get "missing .NET runtime" errors, install the .NET Desktop Runtime
- For LibreHardwareMonitor to work properly, the app needs to run as Administrator
- The application requires Windows 10 version 1809 or later