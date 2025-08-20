#!/usr/bin/env python3
"""Fix the OcclumFileStorageProvider.cs file"""

import os

os.chdir('/home/ubuntu/neo-service-layer')

file_path = 'src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/OcclumFileStorageProvider.cs'

# Read the file
with open(file_path, 'r') as f:
    lines = f.readlines()

# Remove all the misplaced using statements (lines 4-18)
# Keep the proper using statements at the top
new_lines = lines[:3]  # Keep first 3 using statements

# Skip the misplaced using statements (lines 4-18) and the duplicate usings after (20-25)
# Start from the namespace declaration
for i, line in enumerate(lines):
    if line.strip().startswith('namespace NeoServiceLayer.Infrastructure.Persistence'):
        new_lines.extend(lines[i:])
        break

# Add missing using statements at the top
proper_usings = [
    "using System;\n",
    "using System.Collections.Generic;\n", 
    "using System.ComponentModel.DataAnnotations;\n",
    "using System.IO;\n",
    "using System.IO.Compression;\n",
    "using System.Linq;\n",
    "using System.Security.Cryptography;\n",
    "using System.Text;\n",
    "using System.Text.Json;\n",
    "using System.Threading;\n",
    "using System.Threading.Tasks;\n",
    "using Microsoft.Extensions.Logging;\n",
    "\n"
]

# Reconstruct file with proper using statements
final_content = proper_usings + new_lines[3:]  # Skip original using statements

# Write back
with open(file_path, 'w') as f:
    f.writelines(final_content)

print(f"Fixed {file_path}")