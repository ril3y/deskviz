# DeskViz.NET Development Helper Script
# Quick commands for development workflow

param(
    [Parameter(Position=0)]
    [ValidateSet("build", "run", "clean", "watch", "test", "package")]
    [string]$Command = "build"
)

$ErrorActionPreference = "Stop"

switch ($Command) {
    "build" {
        Write-Host "Building DeskViz.NET..." -ForegroundColor Cyan
        dotnet build
    }
    
    "run" {
        Write-Host "Running DeskViz.NET..." -ForegroundColor Green
        dotnet run --project DeskViz.App/DeskViz.App.csproj
    }
    
    "clean" {
        Write-Host "Cleaning solution..." -ForegroundColor Yellow
        dotnet clean
        Remove-Item -Recurse -Force bin, obj -ErrorAction SilentlyContinue
    }
    
    "watch" {
        Write-Host "Starting file watcher..." -ForegroundColor Magenta
        Write-Host "Press Ctrl+C to stop" -ForegroundColor Gray
        dotnet watch --project DeskViz.App/DeskViz.App.csproj run
    }
    
    "test" {
        Write-Host "Running tests..." -ForegroundColor Blue
        dotnet test
    }
    
    "package" {
        Write-Host "Creating release package..." -ForegroundColor Cyan
        & ./build.ps1 -Configuration Release -Clean -Package
    }
}