# DeskViz.NET Build Script for Windows
# Run from PowerShell or from WSL using: powershell.exe -File build.ps1

param(
    [string]$Configuration = "Debug",
    [switch]$Clean,
    [switch]$Run,
    [switch]$Package,
    [switch]$Plugins,
    [string]$SpecificPlugin = ""
)

$ErrorActionPreference = "Stop"

Write-Host "DeskViz.NET Build Script" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan

# Clean if requested
if ($Clean) {
    Write-Host "`nCleaning solution..." -ForegroundColor Yellow
    dotnet clean
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

# Restore packages
Write-Host "`nRestoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Build solution
Write-Host "`nBuilding solution ($Configuration)..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Build plugins if requested, if Run is specified, or if specific plugin requested
if ($Plugins -or $Run -or $SpecificPlugin) {
    Write-Host "`nBuilding plugins..." -ForegroundColor Yellow

    # All widget .csproj targets copy to the single WidgetOutput/ folder via Directory.Build.props
    $widgetOutputDir = "WidgetOutput"
    New-Item -ItemType Directory -Force -Path $widgetOutputDir | Out-Null

    # Get all widget projects
    $widgetProjects = Get-ChildItem -Path "." -Filter "DeskViz.Widgets.*" -Directory | Where-Object { $_.Name -match "^DeskViz\.Widgets\.[^.]+$" }

    foreach ($project in $widgetProjects) {
        $projectName = $project.Name

        # Skip if specific plugin requested and this isn't it
        if ($SpecificPlugin -and $projectName -ne $SpecificPlugin) {
            continue
        }

        Write-Host "Building plugin: $projectName" -ForegroundColor Cyan

        # Build the plugin (post-build target copies DLLs to WidgetOutput/)
        dotnet build "$($project.FullName)/$projectName.csproj" --configuration $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Failed to build plugin: $projectName" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    }

    Write-Host "Plugin build completed. Output directory: $widgetOutputDir" -ForegroundColor Green
}

# Run if requested
if ($Run) {
    Write-Host "`nRunning DeskViz.App..." -ForegroundColor Green
    dotnet run --project DeskViz.App/DeskViz.App.csproj --configuration $Configuration --no-build
}

# Package if requested
if ($Package) {
    Write-Host "`nCreating package..." -ForegroundColor Yellow
    $outputDir = "bin/Package/$Configuration"
    
    # Create output directory
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
    
    # Publish self-contained executable
    dotnet publish DeskViz.App/DeskViz.App.csproj `
        --configuration $Configuration `
        --runtime win-x64 `
        --self-contained true `
        --output $outputDir `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nPackage created in: $outputDir" -ForegroundColor Green
    }
}

Write-Host "`nBuild completed successfully!" -ForegroundColor Green

Write-Host "`nUsage Examples:" -ForegroundColor Cyan
Write-Host "  .\build.ps1                              # Build main app only" -ForegroundColor Gray
Write-Host "  .\build.ps1 -Plugins                     # Build main app + all plugins" -ForegroundColor Gray
Write-Host "  .\build.ps1 -SpecificPlugin DeskViz.Widgets.Cpu  # Build specific plugin only" -ForegroundColor Gray
Write-Host "  .\build.ps1 -Run                         # Build everything and run app" -ForegroundColor Gray
Write-Host "  .\build.ps1 -Clean -Plugins              # Clean build with plugins" -ForegroundColor Gray