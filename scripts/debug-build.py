#!/usr/bin/env python3
import subprocess
import os

os.chdir('/home/ubuntu/neo-service-layer')

# Try building the simplest core project with detailed output
project = 'src/Core/NeoServiceLayer.Shared/NeoServiceLayer.Shared.csproj'
cmd = ['dotnet', 'build', project, '--no-restore', '-v', 'normal']

print(f"Building {project} with detailed output...")
print(f"Command: {' '.join(cmd)}")
print("=" * 60)

result = subprocess.run(cmd, capture_output=True, text=True, shell=False)
print("STDOUT:")
print(result.stdout)
print("\nSTDERR:")
print(result.stderr)
print("\nReturn code:", result.returncode)