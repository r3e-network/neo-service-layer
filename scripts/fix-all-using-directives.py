#!/usr/bin/env python3
"""
Fix all missing using directives across the entire codebase.
"""
import os
import re
from pathlib import Path

def add_common_usings(file_path):
    """Add common using directives that are frequently missing"""
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Common sets of using directives based on file patterns
    common_usings = {
        'default': [
            'using System;',
            'using System.Collections.Generic;',
            'using System.Linq;',
            'using System.Threading;',
            'using System.Threading.Tasks;'
        ],
        'extensions': [
            'using System;',
            'using System.Collections.Generic;',
            'using System.ComponentModel.DataAnnotations;',
            'using System.Globalization;',
            'using System.Linq;',
            'using System.Security.Cryptography;',
            'using System.Text;',
            'using System.Text.Json;',
            'using System.Text.RegularExpressions;'
        ],
        'networking': [
            'using System.Net;',
            'using System.Net.Http;',
            'using System.Net.Sockets;'
        ],
        'io': [
            'using System.IO;'
        ],
        'diagnostics': [
            'using System.Diagnostics;',
            'using System.Diagnostics.CodeAnalysis;'
        ],
        'runtime': [
            'using System.Runtime.CompilerServices;',
            'using System.Runtime.InteropServices;'
        ],
        'collections': [
            'using System.Collections.Concurrent;'
        ]
    }
    
    # Check which usings are missing
    existing_usings = set(re.findall(r'^using [\w\.]+;', content, re.MULTILINE))
    
    # Determine what to add based on error patterns in the file
    usings_to_add = set()
    
    # Always add default usings if not present
    for using in common_usings['default']:
        if using not in existing_usings and using not in content:
            usings_to_add.add(using)
    
    # Add specific usings based on content patterns
    if 'ValidationContext' in content or 'ValidationResult' in content:
        if 'using System.ComponentModel.DataAnnotations;' not in content:
            usings_to_add.add('using System.ComponentModel.DataAnnotations;')
    
    if 'NotNull' in content or 'NotNullAttribute' in content:
        if 'using System.Diagnostics.CodeAnalysis;' not in content:
            usings_to_add.add('using System.Diagnostics.CodeAnalysis;')
    
    if 'SocketException' in content:
        if 'using System.Net.Sockets;' not in content:
            usings_to_add.add('using System.Net.Sockets;')
    
    if 'CharUnicodeInfo' in content or 'UnicodeCategory' in content:
        if 'using System.Globalization;' not in content:
            usings_to_add.add('using System.Globalization;')
    
    if 'ConcurrentDictionary' in content or 'ConcurrentBag' in content:
        if 'using System.Collections.Concurrent;' not in content:
            usings_to_add.add('using System.Collections.Concurrent;')
    
    if 'HttpClient' in content or 'HttpResponseMessage' in content:
        if 'using System.Net.Http;' not in content:
            usings_to_add.add('using System.Net.Http;')
    
    if 'WebSocket' in content:
        if 'using System.Net.WebSockets;' not in content:
            usings_to_add.add('using System.Net.WebSockets;')
    
    if not usings_to_add:
        return False
    
    # Find the namespace declaration
    namespace_match = re.search(r'^namespace [\w\.]+', content, re.MULTILINE)
    if namespace_match:
        # Insert usings before namespace
        insertion_point = namespace_match.start()
        
        # Get existing using block
        using_block_end = content.rfind('\n', 0, insertion_point)
        
        # Build new using block
        new_usings = '\n'.join(sorted(usings_to_add))
        
        # Insert the new usings
        if using_block_end > 0:
            # Add after existing usings
            content = content[:using_block_end] + '\n' + new_usings + content[using_block_end:]
        else:
            # Add at the beginning
            content = new_usings + '\n\n' + content
        
        # Write back
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        
        return True
    
    return False

def main():
    os.chdir('/home/ubuntu/neo-service-layer')
    
    # Find all C# files
    cs_files = list(Path('.').glob('**/*.cs'))
    cs_files = [f for f in cs_files if f.is_file() and 'bin' not in str(f) and 'obj' not in str(f) and '.nuget' not in str(f)]
    
    print(f"Found {len(cs_files)} C# files to check")
    
    fixed_count = 0
    for file_path in cs_files:
        if add_common_usings(file_path):
            fixed_count += 1
            print(f"Fixed: {file_path}")
    
    print(f"\n✅ Fixed {fixed_count} files")

if __name__ == "__main__":
    main()