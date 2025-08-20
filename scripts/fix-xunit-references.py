#!/usr/bin/env python3
"""
Fix missing xUnit references in test files
"""

from pathlib import Path
import re

def fix_xunit_references():
    project_root = Path("/home/ubuntu/neo-service-layer")
    
    # Find all test .cs files
    test_files = list(project_root.glob("tests/**/*.cs"))
    
    fixed_count = 0
    
    for test_file in test_files:
        if test_file.is_file():
            content = test_file.read_text()
            
            # Check if file uses xUnit attributes but missing using
            has_xunit_attributes = any(attr in content for attr in ["[Fact]", "[Theory]", "[InlineData"])
            has_xunit_using = "using Xunit;" in content
            
            if has_xunit_attributes and not has_xunit_using:
                # Add using Xunit; after other using statements
                lines = content.split('\n')
                
                # Find the last using statement
                last_using_index = -1
                for i, line in enumerate(lines):
                    if line.startswith("using "):
                        last_using_index = i
                
                if last_using_index >= 0:
                    # Insert xUnit using after the last using
                    lines.insert(last_using_index + 1, "using Xunit;")
                    
                    # Write back
                    new_content = '\n'.join(lines)
                    test_file.write_text(new_content)
                    fixed_count += 1
                    print(f"Fixed: {test_file.relative_to(project_root)}")
    
    print(f"\n✅ Fixed {fixed_count} test files with missing xUnit references")
    return fixed_count

if __name__ == "__main__":
    fix_xunit_references()