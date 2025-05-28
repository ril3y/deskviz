# DeskViz.NET Build Script for Windows
# Run from PowerShell or from WSL using: powershell.exe -File build.ps1

param(
    [string]$Configuration = "Debug",
    [switch]$Clean,
    [switch]$Run,
    [switch]$Package
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