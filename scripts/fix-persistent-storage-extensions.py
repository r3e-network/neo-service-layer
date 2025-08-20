#!/usr/bin/env python3
"""
Fix the PersistentStorageExtensions.cs file properly.
"""
import os

os.chdir('/home/ubuntu/neo-service-layer')

file_path = 'src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/PersistentStorageExtensions.cs'

# Read the file
with open(file_path, 'r') as f:
    lines = f.readlines()

# The line "using var transaction = await storage.BeginTransactionAsync();" is code, not a using directive
# It should be inside a method, not at the top of the file

# Collect proper using statements
proper_usings = []
code_lines = []
namespace_found = False

for line in lines:
    stripped = line.strip()
    
    # Skip the misplaced code line
    if stripped == 'using var transaction = await storage.BeginTransactionAsync();':
        continue  # This will be placed inside the method where it belongs
    
    # Collect proper using directives
    if stripped.startswith('using ') and stripped.endswith(';') and not namespace_found:
        if 'var ' not in stripped:  # Make sure it's not a using statement for disposable
            proper_usings.append(line)
    elif stripped.startswith('namespace '):
        namespace_found = True
        code_lines.append(line)
    elif namespace_found or not stripped:
        code_lines.append(line)

# Build proper file content
new_content = []
new_content.extend(proper_usings)
if proper_usings:
    new_content.append('\n')
new_content.extend(code_lines)

# Write back
with open(file_path, 'w') as f:
    f.writelines(new_content)

print(f"Fixed {file_path}")

# Now we need to find where that transaction line belongs and put it there
# Let's read the file again and fix the method
with open(file_path, 'r') as f:
    content = f.read()

# The transaction line should be inside ExecuteTransactionAsync method
# Find the method and fix it
import re

# Find the ExecuteTransactionAsync method
pattern = r'(if \(storage\.SupportsTransactions\)\s*\{)\s*(\n\s*try\s*\{)'
replacement = r'\1\n            var transaction = await storage.BeginTransactionAsync();\2'

content = re.sub(pattern, replacement, content)

# Also need to fix the using statement - it should be in a using block
pattern2 = r'var transaction = await storage\.BeginTransactionAsync\(\);'
replacement2 = r'using var transaction = await storage.BeginTransactionAsync();'

content = re.sub(pattern2, replacement2, content)

with open(file_path, 'w') as f:
    f.write(content)

print("Fixed transaction usage in method")