#!/usr/bin/env python3
import xml.etree.ElementTree as ET
import os
from pathlib import Path
from collections import defaultdict
from datetime import datetime

def find_csproj_files():
    """Find all .csproj files in the project"""
    csproj_files = []
    for root, dirs, files in os.walk('.'):
        for file in files:
            if file.endswith('.csproj'):
                csproj_files.append(os.path.join(root, file))
    return sorted(csproj_files)

def extract_package_references(csproj_path):
    """Extract package references from a .csproj file"""
    packages = []
    try:
        tree = ET.parse(csproj_path)
        root = tree.getroot()
        
        # Find all PackageReference elements
        for item_group in root.findall('.//ItemGroup'):
            for package_ref in item_group.findall('PackageReference'):
                package_name = package_ref.get('Include')
                version = package_ref.get('Version')
                if package_name and version:
                    packages.append({
                        'name': package_name,
                        'version': version
                    })
    except Exception as e:
        print(f"Error parsing {csproj_path}: {e}")
    
    return packages

def categorize_package(package_name):
    """Categorize package based on its name"""
    if package_name.startswith('Microsoft.Extensions.'):
        return 'Microsoft Extensions'
    elif package_name.startswith('Microsoft.AspNetCore.'):
        return 'ASP.NET Core'
    elif package_name.startswith('Microsoft.EntityFrameworkCore.'):
        return 'Entity Framework Core'
    elif package_name.startswith('Microsoft.ML.'):
        return 'Machine Learning'
    elif package_name.startswith('Microsoft.NET.Test.'):
        return 'Testing'
    elif package_name.startswith('xunit'):
        return 'Testing'
    elif package_name.startswith('Moq'):
        return 'Testing'
    elif package_name in ['FluentAssertions', 'AutoFixture', 'Bogus']:
        return 'Testing'
    elif package_name.startswith('coverlet.'):
        return 'Testing'
    elif package_name.startswith('Serilog'):
        return 'Logging'
    elif package_name.startswith('Swashbuckle.'):
        return 'API Documentation'
    elif package_name.startswith('Neo'):
        return 'NEO Blockchain'
    elif package_name.startswith('Nethereum.'):
        return 'Ethereum Integration'
    elif package_name.startswith('StackExchange.'):
        return 'Caching/Redis'
    elif package_name.startswith('Npgsql'):
        return 'Database'
    elif package_name.startswith('System.'):
        return 'System Libraries'
    elif package_name.startswith('Asp.Versioning.'):
        return 'API Versioning'
    elif package_name.startswith('AspNetCore.HealthChecks.'):
        return 'Health Checks'
    elif package_name == 'Newtonsoft.Json':
        return 'JSON Processing'
    elif package_name.startswith('Microsoft.IdentityModel.'):
        return 'Identity/Security'
    elif package_name.startswith('Microsoft.CodeAnalysis.'):
        return 'Code Analysis'
    else:
        return 'Other'

def analyze_packages():
    """Main analysis function"""
    csproj_files = find_csproj_files()
    print(f"Found {len(csproj_files)} .csproj files")
    
    # Track all package references
    package_data = defaultdict(lambda: {
        'versions': defaultdict(list),
        'projects': set(),
        'category': ''
    })
    
    # Process each project file
    for csproj_path in csproj_files:
        project_name = os.path.basename(csproj_path)
        packages = extract_package_references(csproj_path)
        
        for package in packages:
            name = package['name']
            version = package['version']
            
            package_data[name]['versions'][version].append(project_name)
            package_data[name]['projects'].add(project_name)
            package_data[name]['category'] = categorize_package(name)
    
    return package_data, csproj_files

def generate_markdown_report(package_data, csproj_files):
    """Generate markdown report"""
    output = []
    output.append("# Package References Analysis")
    output.append("")
    output.append(f"Generated on: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    output.append("")
    output.append("## Summary")
    output.append("")
    output.append(f"- Total Projects: {len(csproj_files)}")
    output.append(f"- Total Unique Packages: {len(package_data)}")
    output.append("")
    
    # Group by category
    categories = defaultdict(list)
    for package_name, data in package_data.items():
        categories[data['category']].append((package_name, data))
    
    # Sort categories
    for category in sorted(categories.keys()):
        packages = sorted(categories[category], key=lambda x: x[0])
        
        output.append(f"## {category}")
        output.append("")
        output.append("| Package | Versions | Project Count | Version Details |")
        output.append("|---------|----------|---------------|-----------------|")
        
        for package_name, data in packages:
            version_count = len(data['versions'])
            project_count = len(data['projects'])
            
            # Format version details
            version_details = []
            for version in sorted(data['versions'].keys(), reverse=True):
                projects = sorted(set(data['versions'][version]))
                version_details.append(f"**{version}** ({len(projects)} projects)")
            
            version_details_str = "<br/>".join(version_details)
            
            output.append(f"| {package_name} | {version_count} | {project_count} | {version_details_str} |")
        
        output.append("")
    
    # Version inconsistencies
    output.append("## Version Inconsistencies")
    output.append("")
    output.append("Packages with multiple versions across projects:")
    output.append("")
    
    inconsistencies = [(name, data) for name, data in package_data.items() if len(data['versions']) > 1]
    
    if inconsistencies:
        output.append("| Package | Versions | Projects |")
        output.append("|---------|----------|----------|")
        
        for package_name, data in sorted(inconsistencies):
            versions = []
            for version in sorted(data['versions'].keys(), reverse=True):
                projects = sorted(set(data['versions'][version]))
                versions.append(f"**{version}**: {', '.join(projects)}")
            
            versions_str = "<br/>".join(versions)
            all_versions = ", ".join(sorted(data['versions'].keys(), reverse=True))
            
            output.append(f"| {package_name} | {all_versions} | {versions_str} |")
    else:
        output.append("No version inconsistencies found!")
    
    output.append("")
    
    # Recommendations
    output.append("## Recommendations")
    output.append("")
    output.append("### Version Alignment")
    output.append("")
    output.append("Based on the analysis, here are the most common versions for key package groups:")
    output.append("")
    
    # Find most common versions for key packages
    package_patterns = {
        'Microsoft.Extensions.*': {},
        'Microsoft.AspNetCore.*': {},
        'Microsoft.ML.*': {},
        'System.*': {}
    }
    
    for package_name, data in package_data.items():
        for pattern in package_patterns:
            if pattern.endswith('*') and package_name.startswith(pattern[:-1]):
                for version, projects in data['versions'].items():
                    if version not in package_patterns[pattern]:
                        package_patterns[pattern][version] = 0
                    package_patterns[pattern][version] += len(projects)
    
    output.append("| Package Group | Most Common Version(s) |")
    output.append("|---------------|------------------------|")
    
    for pattern, versions in sorted(package_patterns.items()):
        if versions:
            # Sort by usage count
            sorted_versions = sorted(versions.items(), key=lambda x: x[1], reverse=True)
            top_versions = [v[0] for v in sorted_versions[:2]]  # Top 2 versions
            output.append(f"| {pattern} | {', '.join(top_versions)} |")
    
    output.append("")
    
    # Package consolidation opportunities
    output.append("### Consolidation Opportunities")
    output.append("")
    
    # Find packages used in only one project
    single_use_packages = [(name, data) for name, data in package_data.items() if len(data['projects']) == 1]
    
    if single_use_packages:
        output.append(f"Found {len(single_use_packages)} packages used in only one project - consider if these are necessary:")
        output.append("")
        for name, data in sorted(single_use_packages)[:10]:  # Show top 10
            project = list(data['projects'])[0]
            output.append(f"- **{name}** (only in {project})")
    
    return "\n".join(output)

def main():
    """Main entry point"""
    print("Analyzing package references...")
    package_data, csproj_files = analyze_packages()
    
    print("Generating report...")
    report = generate_markdown_report(package_data, csproj_files)
    
    output_path = "PackageReferences.md"
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(report)
    
    print(f"\nAnalysis complete! Report written to: {output_path}")
    
    # Print summary statistics
    print("\nQuick Statistics:")
    print(f"- Total unique packages: {len(package_data)}")
    
    version_inconsistencies = sum(1 for data in package_data.values() if len(data['versions']) > 1)
    print(f"- Packages with version inconsistencies: {version_inconsistencies}")
    
    category_counts = defaultdict(int)
    for data in package_data.values():
        category_counts[data['category']] += 1
    
    print("\nPackages by category:")
    for category, count in sorted(category_counts.items(), key=lambda x: x[1], reverse=True):
        print(f"  - {category}: {count}")

if __name__ == "__main__":
    main()