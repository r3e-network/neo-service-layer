#!/usr/bin/env python3
import re
import sys

def fix_inline_tests(file_path):
    with open(file_path, 'r') as f:
        content = f.read()
    
    # Pattern to match inline test methods
    pattern = r'(\s*)\[Fact\] public async Task (\w+)\(\) \{([^}]+)\}'
    
    def replacement(match):
        indent = match.group(1)
        method_name = match.group(2)
        body = match.group(3)
        
        # Split body into statements
        statements = body.split(';')
        formatted_statements = []
        
        for stmt in statements:
            stmt = stmt.strip()
            if stmt:
                formatted_statements.append(f"{indent}    {stmt};")
        
        # Build the properly formatted method
        result = f"{indent}[Fact]\n"
        result += f"{indent}public async Task {method_name}()\n"
        result += f"{indent}{{\n"
        result += '\n'.join(formatted_statements).rstrip(';') + '\n'
        result += f"{indent}}}"
        
        return result
    
    # Apply the replacement
    fixed_content = re.sub(pattern, replacement, content)
    
    # Also fix Theory methods
    pattern2 = r'(\s*)\[Theory\]([^\]]*\]) public async Task (\w+)\(\) \{([^}]+)\}'
    
    def replacement2(match):
        indent = match.group(1)
        attributes = match.group(2)
        method_name = match.group(3)
        body = match.group(4)
        
        statements = body.split(';')
        formatted_statements = []
        
        for stmt in statements:
            stmt = stmt.strip()
            if stmt:
                formatted_statements.append(f"{indent}    {stmt};")
        
        result = f"{indent}[Theory]{attributes}\n"
        result += f"{indent}public async Task {method_name}()\n"
        result += f"{indent}{{\n"
        result += '\n'.join(formatted_statements).rstrip(';') + '\n'
        result += f"{indent}}}"
        
        return result
    
    fixed_content = re.sub(pattern2, replacement2, fixed_content)
    
    with open(file_path, 'w') as f:
        f.write(fixed_content)
    
    print(f"Fixed {file_path}")

if __name__ == "__main__":
    if len(sys.argv) > 1:
        fix_inline_tests(sys.argv[1])
    else:
        print("Usage: python fix-inline-tests.py <file_path>")