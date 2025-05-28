# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Memories

- never write little programs to accomplish tasks ask the user

## Build Commands

### Windows PowerShell
```powershell
# Build and run
.\build.ps1 -Run

# Clean build
.\build.ps1 -Clean

# Release package
.\build.ps1 -Configuration Release -Package

# Watch mode (auto-rebuild on changes)
.\dev.ps1 watch

# Run without building
.\dev.ps1 run
```

### WSL/Linux
```bash
# Build and run (calls PowerShell)
./build.sh --run

# Or directly
powershell.exe -File build.ps1 -Run
```

**Important:** PowerShell uses single dash syntax (`-Run`), not double dash (`--run`)

[Rest of the file remains unchanged...]