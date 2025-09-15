#!/bin/bash

echo "Starting PicBed - Self-hosted Image Storage Service"
echo "=================================================="

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET is not installed. Please install .NET 8.0 or later."
    exit 1
fi

# Restore packages
echo "Restoring NuGet packages..."
dotnet restore

# Build the project
echo "Building the project..."
dotnet build

# Run the application
echo "Starting the application..."
echo "The application will be available at:"
echo "  - Web Interface: http://localhost:5000"
echo "  - Swagger API: http://localhost:5000/swagger"
echo "  - API Endpoints: http://localhost:5000/api/images"
echo ""
echo "Press Ctrl+C to stop the application"
echo ""

dotnet run --urls "http://localhost:5000"
