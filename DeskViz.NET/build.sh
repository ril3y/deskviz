#!/bin/bash
# DeskViz.NET Build Script for WSL
# This script calls PowerShell to build the Windows application

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${CYAN}DeskViz.NET Build Script (WSL)${NC}"
echo -e "${CYAN}==============================${NC}"

# Default values
CONFIGURATION="Debug"
CLEAN=false
RUN=false
PACKAGE=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --clean)
            CLEAN=true
            shift
            ;;
        --run)
            RUN=true
            shift
            ;;
        --package)
            PACKAGE=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [options]"
            echo "Options:"
            echo "  -c, --configuration <Debug|Release>  Build configuration (default: Debug)"
            echo "  --clean                              Clean before building"
            echo "  --run                                Run after building"
            echo "  --package                            Create self-contained package"
            echo "  -h, --help                           Show this help message"
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            exit 1
            ;;
    esac
done

# Build PowerShell command
PS_CMD="powershell.exe -ExecutionPolicy Bypass -File build.ps1 -Configuration '$CONFIGURATION'"

if [ "$CLEAN" = true ]; then
    PS_CMD="$PS_CMD -Clean"
fi

if [ "$RUN" = true ]; then
    PS_CMD="$PS_CMD -Run"
fi

if [ "$PACKAGE" = true ]; then
    PS_CMD="$PS_CMD -Package"
fi

# Execute build
echo -e "${YELLOW}Executing: $PS_CMD${NC}"
eval $PS_CMD

# Check if build was successful
if [ $? -eq 0 ]; then
    echo -e "${GREEN}Build completed successfully!${NC}"
    
    # Show output location
    if [ "$PACKAGE" = false ]; then
        echo -e "${CYAN}Output: bin/Debug/net8.0-windows/${NC}"
    fi
else
    echo -e "${RED}Build failed!${NC}"
    exit 1
fi