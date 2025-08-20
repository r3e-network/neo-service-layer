#!/usr/bin/env python3
"""
Fix common compilation errors in source files
"""

import subprocess
from pathlib import Path
import re

def fix_misplaced_using_statements():
    """Fix using statements that are in the wrong place in the file."""
    project_root = Path("/home/ubuntu/neo-service-layer")
    
    # Find all C# files
    cs_files = list(project_root.glob("src/**/*.cs"))
    
    fixed_count = 0
    
    for cs_file in cs_files:
        if cs_file.is_file():
            content = cs_file.read_text()
            lines = content.split('\n')
            
            # Find misplaced using statements (not at the top of the file)
            modified = False
            using_statements = []
            new_lines = []
            
            # First pass - collect all using statements
            for i, line in enumerate(lines):
                # Check if it's a using statement not at the top
                if line.strip().startswith("using ") and line.strip().endswith(";"):
                    if i > 20:  # Likely misplaced if after line 20
                        using_statements.append(line.strip())
                        modified = True
                        continue  # Skip adding this line
                new_lines.append(line)
            
            if modified:
                # Reconstruct file with using statements at the top
                final_lines = []
                
                # Find where to insert (after existing using statements)
                insert_index = 0
                for i, line in enumerate(new_lines):
                    if line.strip().startswith("using "):
                        insert_index = i + 1
                    elif line.strip().startswith("namespace"):
                        break
                
                # Insert collected using statements
                for i, line in enumerate(new_lines):
                    final_lines.append(line)
                    if i == insert_index and using_statements:
                        for using_stmt in using_statements:
                            final_lines.append(using_stmt)
                        using_statements = []  # Clear after inserting
                
                # Write back
                cs_file.write_text('\n'.join(final_lines))
                fixed_count += 1
                print(f"Fixed: {cs_file.relative_to(project_root)}")
    
    return fixed_count

def fix_syntax_errors():
    """Fix common syntax errors in C# files."""
    project_root = Path("/home/ubuntu/neo-service-layer")
    
    # Specific files with known errors
    files_to_fix = [
        "src/AI/NeoServiceLayer.AI.PatternRecognition/Analyzers/AnomalyPatternAnalyzer.cs",
        "src/AI/NeoServiceLayer.AI.PatternRecognition/Analyzers/TrendPatternAnalyzer.cs"
    ]
    
    for file_path in files_to_fix:
        full_path = project_root / file_path
        if full_path.exists():
            content = full_path.read_text()
            
            # Fix line 47 in AnomalyPatternAnalyzer.cs
            if "AnomalyPatternAnalyzer.cs" in file_path:
                content = content.replace(
                    "            // Detect outliers using IQR method\nusing NeoServiceLayer.ServiceFramework;\n            var outliers",
                    "            // Detect outliers using IQR method\n            var outliers"
                )
            
            # Fix line 170 in TrendPatternAnalyzer.cs  
            if "TrendPatternAnalyzer.cs" in file_path:
                lines = content.split('\n')
                if len(lines) > 170 and "using" in lines[169]:
                    lines[169] = "            // Analyze momentum"
                    content = '\n'.join(lines)
            
            full_path.write_text(content)
            print(f"Fixed syntax in: {file_path}")

def add_missing_types():
    """Add missing type definitions."""
    project_root = Path("/home/ubuntu/neo-service-layer")
    
    # Check if PatternTypes.cs exists
    pattern_types_file = project_root / "src/AI/NeoServiceLayer.AI.PatternRecognition/Models/PatternTypes.cs"
    
    if not pattern_types_file.parent.exists():
        pattern_types_file.parent.mkdir(parents=True, exist_ok=True)
    
    # Ensure MissingTypes.cs has all required types
    missing_types_file = project_root / "src/AI/NeoServiceLayer.AI.PatternRecognition/Models/MissingTypes.cs"
    
    if missing_types_file.exists():
        content = missing_types_file.read_text()
        
        # Add any missing type definitions
        if "PatternAnalysisRequest" not in content:
            content += """
public class PatternAnalysisRequest
{
    public double[] Data { get; set; } = Array.Empty<double>();
    public string AnalysisType { get; set; } = "General";
    public Dictionary<string, object> Parameters { get; set; } = new();
}
"""
        
        if "PatternAnalysisResult" not in content:
            content += """
public class PatternAnalysisResult
{
    public bool Success { get; set; }
    public Pattern[] Patterns { get; set; } = Array.Empty<Pattern>();
    public DateTime AnalysisTime { get; set; }
    public string? ErrorMessage { get; set; }
}
"""
        
        missing_types_file.write_text(content)
        print(f"Updated: {missing_types_file.relative_to(project_root)}")

def main():
    print("=" * 80)
    print("FIXING COMPILATION ERRORS")
    print("=" * 80)
    
    # Fix misplaced using statements
    print("\nFixing misplaced using statements...")
    fixed_using = fix_misplaced_using_statements()
    print(f"  Fixed {fixed_using} files with misplaced using statements")
    
    # Fix specific syntax errors
    print("\nFixing known syntax errors...")
    fix_syntax_errors()
    
    # Add missing types
    print("\nAdding missing type definitions...")
    add_missing_types()
    
    print("\n" + "=" * 80)
    print("COMPILATION FIXES COMPLETE")
    print("=" * 80)

if __name__ == "__main__":
    main()