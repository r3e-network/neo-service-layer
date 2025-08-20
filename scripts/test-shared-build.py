#!/usr/bin/env python3
import subprocess
import os

os.chdir('/home/ubuntu/neo-service-layer')
cmd = ['dotnet', 'build', 'src/Core/NeoServiceLayer.Shared/NeoServiceLayer.Shared.csproj', '--no-restore', '-v', 'quiet']
result = subprocess.run(cmd, capture_output=True, text=True)

if "Build succeeded" in result.stdout:
    print("✅ Shared project builds successfully!")
else:
    print("❌ Build failed")
    # Extract errors
    lines = result.stdout.split('\n')
    for line in lines:
        if 'error CS' in line or 'error MSB' in line:
            print(line)