#!/usr/bin/env python3
import xml.etree.ElementTree as ET
import os
from pathlib import Path
import shutil
from datetime import datetime

# Register XML namespace
ET.register_namespace('', 'http://schemas.microsoft.com/developer/msbuild/2003')

def backup_file(filepath):
    """Create a backup of the file"""
    backup_path = f"{filepath}.backup_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
    shutil.copy2(filepath, backup_path)
    return backup_path

def update_csproj_file(csproj_path, dry_run=False):
    """Update a .csproj file to remove Version attributes from PackageReference elements"""
    try:
        # Parse the XML file
        tree = ET.parse(csproj_path)
        root = tree.getroot()
        
        changes_made = False
        
        # Find all PackageReference elements
        for item_group in root.findall('.//ItemGroup'):
            for package_ref in item_group.findall('PackageReference'):
                if 'Version' in package_ref.attrib:
                    package_name = package_ref.get('Include')
                    version = package_ref.get('Version')
                    
                    if not dry_run:
                        # Remove the Version attribute
                        del package_ref.attrib['Version']
                        changes_made = True
                    
                    print(f"  - {package_name}: {version}")
        
        if changes_made and not dry_run:
            # Create backup
            backup_path = backup_file(csproj_path)
            print(f"  Backup created: {backup_path}")
            
            # Write the updated XML
            tree.write(csproj_path, encoding='utf-8', xml_declaration=True)
            print(f"  ✓ Updated successfully")
        
        return changes_made
        
    except Exception as e:
        print(f"  ✗ Error: {e}")
        return False

def find_csproj_files(exclude_patterns=None):
    """Find all .csproj files in the project"""
    if exclude_patterns is None:
        exclude_patterns = []
    
    csproj_files = []
    for root, dirs, files in os.walk('.'):
        # Skip certain directories
        if any(pattern in root for pattern in exclude_patterns):
            continue
            
        for file in files:
            if file.endswith('.csproj'):
                csproj_files.append(os.path.join(root, file))
    
    return sorted(csproj_files)

def main():
    """Main function"""
    print("Migrating to Central Package Management")
    print("=" * 50)
    
    # Check if Directory.Build.props exists
    if not os.path.exists('Directory.Build.props'):
        print("ERROR: Directory.Build.props not found!")
        print("Please ensure Directory.Build.props exists before running this script.")
        return 1
    
    # Find all .csproj files
    exclude_patterns = ['.git', 'bin', 'obj', 'packages', 'node_modules']
    csproj_files = find_csproj_files(exclude_patterns)
    
    print(f"\nFound {len(csproj_files)} .csproj files")
    
    # Ask for confirmation
    response = input("\nDo you want to proceed with migration? (y/N): ")
    if response.lower() != 'y':
        print("Migration cancelled.")
        return 0
    
    dry_run_response = input("Do you want to do a dry run first? (Y/n): ")
    dry_run = dry_run_response.lower() != 'n'
    
    if dry_run:
        print("\n--- DRY RUN MODE ---")
    
    # Process each file
    updated_count = 0
    for csproj_path in csproj_files:
        print(f"\nProcessing: {csproj_path}")
        if update_csproj_file(csproj_path, dry_run):
            updated_count += 1
    
    if dry_run:
        print(f"\n--- DRY RUN COMPLETE ---")
        print(f"Would update {updated_count} files")
        
        if updated_count > 0:
            response = input("\nProceed with actual migration? (y/N): ")
            if response.lower() == 'y':
                # Run again without dry_run
                updated_count = 0
                print("\n--- ACTUAL MIGRATION ---")
                for csproj_path in csproj_files:
                    print(f"\nProcessing: {csproj_path}")
                    if update_csproj_file(csproj_path, dry_run=False):
                        updated_count += 1
    
    print(f"\n✓ Migration complete! Updated {updated_count} files.")
    print("\nNext steps:")
    print("1. Review the changes")
    print("2. Run 'dotnet restore' to ensure packages resolve correctly")
    print("3. Run 'dotnet build' to verify the build still works")
    print("4. If everything works, you can delete the .backup files")
    
    return 0

if __name__ == "__main__":
    exit(main())