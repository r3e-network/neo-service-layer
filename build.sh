#!/bin/bash
# Build script for NeoServiceLayer

echo "Building NeoServiceLayer..."
exec /usr/bin/dotnet build NeoServiceLayer.sln --no-restore "$@"