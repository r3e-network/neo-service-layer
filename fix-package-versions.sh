#!/bin/bash

echo "Fixing package version conflicts..."

# Update all Microsoft.Extensions.* packages to compatible 8.0.x versions
find . -name "*.csproj" -type f | xargs sed -i 's/Microsoft\.Extensions\.[^"]*" Version="9\.0\.[^"]*"/&/g' | \
xargs sed -i 's/Version="9\.0\.[0-9]*"/Version="8.0.11"/g'

# Update System.Text.Json to 8.0.x
find . -name "*.csproj" -type f | xargs sed -i 's/System\.Text\.Json" Version="9\.0\.[^"]*"/System.Text.Json" Version="8.0.5"/g'

# Update System.ComponentModel.Annotations to 5.0.0
find . -name "*.csproj" -type f | xargs sed -i 's/System\.ComponentModel\.Annotations" Version="9\.0\.[^"]*"/System.ComponentModel.Annotations" Version="5.0.0"/g'

# Update AutoFixture to stable version
find . -name "*.csproj" -type f | xargs sed -i 's/AutoFixture" Version="5\.0\.0"/AutoFixture" Version="4.18.1"/g'

# Update Neo packages to stable versions
find . -name "*.csproj" -type f | xargs sed -i 's/Neo[^"]*" Version="3\.9\.[^"]*"/&/g' | \
xargs sed -i 's/Version="3\.9\.[0-9]*"/Version="3.8.2"/g'

echo "Package versions updated successfully!"