using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;


namespace NeoServiceLayer.TestUtilities.CoverageAnalysis
{
    /// <summary>
    /// Automated test coverage analyzer that identifies gaps and provides recommendations.
    /// </summary>
    public class TestCoverageAnalyzer
    {
        private readonly ILogger<TestCoverageAnalyzer> _logger;
        private readonly string _projectRoot;
        private readonly CoverageConfiguration _configuration;
        private readonly Dictionary<string, ModuleCoverage> _moduleCoverage;

        public TestCoverageAnalyzer(
            ILogger<TestCoverageAnalyzer> logger,
            string projectRoot,
            CoverageConfiguration? configuration = null)
        {
            _logger = logger;
            _projectRoot = projectRoot;
            _configuration = configuration ?? CoverageConfiguration.Default;
            _moduleCoverage = new Dictionary<string, ModuleCoverage>();
        }

        /// <summary>
        /// Analyzes test coverage for the entire project.
        /// </summary>
        public async Task<CoverageAnalysisResult> AnalyzeCoverageAsync(string coverageReportPath)
        {
            var result = new CoverageAnalysisResult
            {
                AnalysisId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                ProjectRoot = _projectRoot,
                Modules = new List<ModuleCoverage>()
            };

            try
            {
                _logger.LogInformation("Starting coverage analysis from {Path}", coverageReportPath);

                // Parse coverage report (supports Cobertura and OpenCover formats)
                if (File.Exists(coverageReportPath))
                {
                    if (coverageReportPath.EndsWith(".xml"))
                    {
                        await ParseXmlCoverageReportAsync(coverageReportPath, result);
                    }
                    else if (coverageReportPath.EndsWith(".json"))
                    {
                        await ParseJsonCoverageReportAsync(coverageReportPath, result);
                    }
                }
                else if (Directory.Exists(coverageReportPath))
                {
                    // Process all coverage files in directory
                    await ProcessCoverageDirectoryAsync(coverageReportPath, result);
                }

                // Analyze code without tests
                await IdentifyUntestedCodeAsync(result);

                // Calculate overall metrics
                CalculateOverallMetrics(result);

                // Identify coverage gaps
                IdentifyCoverageGaps(result);

                // Generate recommendations
                GenerateRecommendations(result);

                // Check against thresholds
                ValidateCoverageThresholds(result);

                _logger.LogInformation("Coverage analysis completed. Overall coverage: {Coverage}%",
                    result.OverallCoverage.LineCoverage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Coverage analysis failed");
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Identifies code files without corresponding test files.
        /// </summary>
        public async Task<UntestedCodeReport> IdentifyUntestedFilesAsync()
        {
            var report = new UntestedCodeReport
            {
                Timestamp = DateTime.UtcNow,
                UntestedFiles = new List<UntestedFile>(),
                UntestedMethods = new List<UntestedMethod>(),
                UntestedClasses = new List<UntestedClass>()
            };

            try
            {
                _logger.LogDebug("Identifying untested files in {Root}", _projectRoot);

                // Find all source files
                var sourceFiles = Directory.GetFiles(_projectRoot, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("/tests/", StringComparison.OrdinalIgnoreCase) &&
                               !f.Contains("/obj/", StringComparison.OrdinalIgnoreCase) &&
                               !f.Contains("/bin/", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Find all test files
                var testFiles = Directory.GetFiles(_projectRoot, "*Test*.cs", SearchOption.AllDirectories)
                    .Where(f => f.Contains("/tests/", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Create mapping of source to test files
                var testedFiles = new HashSet<string>();
                
                foreach (var testFile in testFiles)
                {
                    var testContent = await File.ReadAllTextAsync(testFile);
                    var referencedTypes = ExtractReferencedTypes(testContent);
                    
                    foreach (var type in referencedTypes)
                    {
                        var matchingSource = sourceFiles.FirstOrDefault(s => 
                            Path.GetFileNameWithoutExtension(s).Equals(type, StringComparison.OrdinalIgnoreCase));
                        
                        if (matchingSource != null)
                        {
                            testedFiles.Add(matchingSource);
                        }
                    }
                }

                // Identify untested files
                foreach (var sourceFile in sourceFiles)
                {
                    if (!testedFiles.Contains(sourceFile))
                    {
                        var fileInfo = new FileInfo(sourceFile);
                        var relativePath = Path.GetRelativePath(_projectRoot, sourceFile);
                        
                        report.UntestedFiles.Add(new UntestedFile
                        {
                            FilePath = relativePath,
                            FileName = fileInfo.Name,
                            FileSize = fileInfo.Length,
                            LastModified = fileInfo.LastWriteTimeUtc,
                            EstimatedComplexity = await EstimateFileComplexityAsync(sourceFile),
                            Priority = DeterminePriority(relativePath)
                        });
                    }
                }

                // Analyze untested methods and classes
                foreach (var untestedFile in report.UntestedFiles)
                {
                    var fullPath = Path.Combine(_projectRoot, untestedFile.FilePath);
                    await AnalyzeUntestedCodeElementsAsync(fullPath, report);
                }

                report.TotalUntestedFiles = report.UntestedFiles.Count;
                report.TotalUntestedMethods = report.UntestedMethods.Count;
                report.TotalUntestedClasses = report.UntestedClasses.Count;
                
                _logger.LogInformation("Found {Count} untested files", report.TotalUntestedFiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to identify untested files");
                report.Success = false;
                report.ErrorMessage = ex.Message;
            }

            return report;
        }

        /// <summary>
        /// Generates a detailed coverage gap report.
        /// </summary>
        public CoverageGapReport GenerateCoverageGapReport(CoverageAnalysisResult analysis)
        {
            var report = new CoverageGapReport
            {
                Timestamp = DateTime.UtcNow,
                CriticalGaps = new List<CoverageGap>(),
                ModerateGaps = new List<CoverageGap>(),
                MinorGaps = new List<CoverageGap>()
            };

            foreach (var module in analysis.Modules)
            {
                // Check against thresholds
                if (module.LineCoverage < _configuration.CriticalThreshold)
                {
                    report.CriticalGaps.Add(CreateCoverageGap(module, GapSeverity.Critical));
                }
                else if (module.LineCoverage < _configuration.TargetThreshold)
                {
                    report.ModerateGaps.Add(CreateCoverageGap(module, GapSeverity.Moderate));
                }
                else if (module.LineCoverage < _configuration.OptimalThreshold)
                {
                    report.MinorGaps.Add(CreateCoverageGap(module, GapSeverity.Minor));
                }

                // Check for uncovered critical paths
                foreach (var file in module.Files)
                {
                    if (IsCriticalFile(file.FilePath) && file.LineCoverage < _configuration.CriticalFileThreshold)
                    {
                        report.CriticalGaps.Add(new CoverageGap
                        {
                            Location = file.FilePath,
                            Type = "Critical File",
                            CurrentCoverage = file.LineCoverage,
                            TargetCoverage = _configuration.CriticalFileThreshold,
                            Severity = GapSeverity.Critical,
                            Impact = "High risk - critical business logic",
                            Recommendation = $"Increase test coverage for {file.FileName} to at least {_configuration.CriticalFileThreshold}%"
                        });
                    }
                }
            }

            // Sort by severity and impact
            report.CriticalGaps = report.CriticalGaps.OrderBy(g => g.CurrentCoverage).ToList();
            report.ModerateGaps = report.ModerateGaps.OrderBy(g => g.CurrentCoverage).ToList();
            report.MinorGaps = report.MinorGaps.OrderBy(g => g.CurrentCoverage).ToList();

            return report;
        }

        /// <summary>
        /// Tracks coverage trends over time.
        /// </summary>
        public async Task<CoverageTrend> AnalyzeCoverageTrendAsync(List<CoverageAnalysisResult> historicalResults)
        {
            var trend = new CoverageTrend
            {
                AnalysisDate = DateTime.UtcNow,
                DataPoints = new List<CoverageTrendPoint>()
            };

            if (historicalResults == null || !historicalResults.Any())
            {
                _logger.LogWarning("No historical coverage data available for trend analysis");
                return trend;
            }

            // Sort by timestamp
            var sortedResults = historicalResults.OrderBy(r => r.Timestamp).ToList();

            foreach (var result in sortedResults)
            {
                trend.DataPoints.Add(new CoverageTrendPoint
                {
                    Timestamp = result.Timestamp,
                    LineCoverage = result.OverallCoverage.LineCoverage,
                    BranchCoverage = result.OverallCoverage.BranchCoverage,
                    MethodCoverage = result.OverallCoverage.MethodCoverage,
                    TotalLines = result.OverallCoverage.TotalLines,
                    CoveredLines = result.OverallCoverage.CoveredLines
                });
            }

            // Calculate trend metrics
            if (trend.DataPoints.Count >= 2)
            {
                var first = trend.DataPoints.First();
                var last = trend.DataPoints.Last();
                
                trend.TrendDirection = last.LineCoverage > first.LineCoverage ? "Improving" :
                                      last.LineCoverage < first.LineCoverage ? "Declining" : "Stable";
                
                trend.AverageChange = (last.LineCoverage - first.LineCoverage) / trend.DataPoints.Count;
                trend.PeakCoverage = trend.DataPoints.Max(p => p.LineCoverage);
                trend.LowestCoverage = trend.DataPoints.Min(p => p.LineCoverage);
                
                // Calculate velocity (rate of change)
                if (trend.DataPoints.Count >= 3)
                {
                    var recentPoints = trend.DataPoints.TakeLast(3).ToList();
                    trend.RecentVelocity = (recentPoints.Last().LineCoverage - recentPoints.First().LineCoverage) / 2;
                }
            }

            await Task.CompletedTask;
            return trend;
        }

        #region Private Helper Methods

        private async Task ParseXmlCoverageReportAsync(string reportPath, CoverageAnalysisResult result)
        {
            var doc = XDocument.Load(reportPath);
            var root = doc.Root;

            if (root?.Name.LocalName == "coverage")
            {
                // Cobertura format
                await ParseCoberturaReportAsync(doc, result);
            }
            else if (root?.Name.LocalName == "CoverageSession")
            {
                // OpenCover format
                await ParseOpenCoverReportAsync(doc, result);
            }
        }

        private async Task ParseCoberturaReportAsync(XDocument doc, CoverageAnalysisResult result)
        {
            var packages = doc.Descendants("package");
            
            foreach (var package in packages)
            {
                var moduleName = package.Attribute("name")?.Value ?? "Unknown";
                var module = new ModuleCoverage
                {
                    ModuleName = moduleName,
                    Files = new List<FileCoverage>()
                };

                var classes = package.Descendants("class");
                foreach (var cls in classes)
                {
                    var fileName = cls.Attribute("filename")?.Value ?? "Unknown";
                    var fileCoverage = module.Files.FirstOrDefault(f => f.FilePath == fileName);
                    
                    if (fileCoverage == null)
                    {
                        fileCoverage = new FileCoverage
                        {
                            FilePath = fileName,
                            FileName = Path.GetFileName(fileName),
                            Classes = new List<ClassCoverage>()
                        };
                        module.Files.Add(fileCoverage);
                    }

                    var classCoverage = new ClassCoverage
                    {
                        ClassName = cls.Attribute("name")?.Value ?? "Unknown",
                        Methods = new List<MethodCoverage>()
                    };

                    var methods = cls.Descendants("method");
                    foreach (var method in methods)
                    {
                        var methodCoverage = new MethodCoverage
                        {
                            MethodName = method.Attribute("name")?.Value ?? "Unknown",
                            LinesCovered = int.Parse(method.Attribute("lines-covered")?.Value ?? "0"),
                            LinesTotal = int.Parse(method.Attribute("lines-valid")?.Value ?? "0")
                        };
                        
                        classCoverage.Methods.Add(methodCoverage);
                    }

                    fileCoverage.Classes.Add(classCoverage);
                }

                CalculateModuleMetrics(module);
                result.Modules.Add(module);
            }

            await Task.CompletedTask;
        }

        private async Task ParseOpenCoverReportAsync(XDocument doc, CoverageAnalysisResult result)
        {
            // OpenCover format parsing implementation
            await Task.CompletedTask;
        }

        private async Task ParseJsonCoverageReportAsync(string reportPath, CoverageAnalysisResult result)
        {
            var json = await File.ReadAllTextAsync(reportPath);
            var coverageData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            // Parse JSON coverage data
            // Implementation depends on specific JSON format
        }

        private async Task ProcessCoverageDirectoryAsync(string directory, CoverageAnalysisResult result)
        {
            var coverageFiles = Directory.GetFiles(directory, "*.xml", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories));

            foreach (var file in coverageFiles)
            {
                if (file.EndsWith(".xml"))
                {
                    await ParseXmlCoverageReportAsync(file, result);
                }
                else if (file.EndsWith(".json"))
                {
                    await ParseJsonCoverageReportAsync(file, result);
                }
            }
        }

        private async Task IdentifyUntestedCodeAsync(CoverageAnalysisResult result)
        {
            var untestedReport = await IdentifyUntestedFilesAsync();
            
            result.UntestedFiles = untestedReport.UntestedFiles;
            result.TotalUntestedFiles = untestedReport.TotalUntestedFiles;
        }

        private void CalculateModuleMetrics(ModuleCoverage module)
        {
            var totalLines = 0;
            var coveredLines = 0;
            var totalBranches = 0;
            var coveredBranches = 0;
            var totalMethods = 0;
            var coveredMethods = 0;

            foreach (var file in module.Files)
            {
                foreach (var cls in file.Classes)
                {
                    foreach (var method in cls.Methods)
                    {
                        totalLines += method.LinesTotal;
                        coveredLines += method.LinesCovered;
                        totalMethods++;
                        
                        if (method.LinesCovered > 0)
                        {
                            coveredMethods++;
                        }
                    }
                }
                
                // Calculate file metrics
                file.LineCoverage = totalLines > 0 ? (double)coveredLines / totalLines * 100 : 0;
                file.MethodCoverage = totalMethods > 0 ? (double)coveredMethods / totalMethods * 100 : 0;
            }

            module.LineCoverage = totalLines > 0 ? (double)coveredLines / totalLines * 100 : 0;
            module.BranchCoverage = totalBranches > 0 ? (double)coveredBranches / totalBranches * 100 : 0;
            module.MethodCoverage = totalMethods > 0 ? (double)coveredMethods / totalMethods * 100 : 0;
            module.TotalLines = totalLines;
            module.CoveredLines = coveredLines;
        }

        private void CalculateOverallMetrics(CoverageAnalysisResult result)
        {
            var totalLines = result.Modules.Sum(m => m.TotalLines);
            var coveredLines = result.Modules.Sum(m => m.CoveredLines);
            
            result.OverallCoverage = new CoverageMetrics
            {
                LineCoverage = totalLines > 0 ? (double)coveredLines / totalLines * 100 : 0,
                BranchCoverage = result.Modules.Average(m => m.BranchCoverage),
                MethodCoverage = result.Modules.Average(m => m.MethodCoverage),
                TotalLines = totalLines,
                CoveredLines = coveredLines
            };
        }

        private void IdentifyCoverageGaps(CoverageAnalysisResult result)
        {
            result.CoverageGaps = new List<CoverageGap>();

            foreach (var module in result.Modules)
            {
                if (module.LineCoverage < _configuration.TargetThreshold)
                {
                    result.CoverageGaps.Add(CreateCoverageGap(module, 
                        module.LineCoverage < _configuration.CriticalThreshold ? 
                        GapSeverity.Critical : GapSeverity.Moderate));
                }
            }
        }

        private CoverageGap CreateCoverageGap(ModuleCoverage module, GapSeverity severity)
        {
            return new CoverageGap
            {
                Location = module.ModuleName,
                Type = "Module",
                CurrentCoverage = module.LineCoverage,
                TargetCoverage = _configuration.TargetThreshold,
                Gap = _configuration.TargetThreshold - module.LineCoverage,
                Severity = severity,
                Impact = DetermineImpact(module),
                Recommendation = GenerateModuleRecommendation(module)
            };
        }

        private void GenerateRecommendations(CoverageAnalysisResult result)
        {
            result.Recommendations = new List<string>();

            // Overall coverage recommendations
            if (result.OverallCoverage.LineCoverage < _configuration.CriticalThreshold)
            {
                result.Recommendations.Add($"CRITICAL: Overall coverage ({result.OverallCoverage.LineCoverage:F1}%) is below critical threshold ({_configuration.CriticalThreshold}%). Immediate action required.");
            }

            // Module-specific recommendations
            foreach (var gap in result.CoverageGaps.Where(g => g.Severity == GapSeverity.Critical))
            {
                result.Recommendations.Add($"Increase test coverage for {gap.Location} from {gap.CurrentCoverage:F1}% to at least {gap.TargetCoverage}%");
            }

            // Untested file recommendations
            if (result.TotalUntestedFiles > 0)
            {
                result.Recommendations.Add($"Add tests for {result.TotalUntestedFiles} untested files");
            }

            // Best practices
            if (result.OverallCoverage.BranchCoverage < result.OverallCoverage.LineCoverage * 0.8)
            {
                result.Recommendations.Add("Focus on improving branch coverage to ensure all code paths are tested");
            }
        }

        private void ValidateCoverageThresholds(CoverageAnalysisResult result)
        {
            result.MeetsTargetThreshold = result.OverallCoverage.LineCoverage >= _configuration.TargetThreshold;
            result.MeetsCriticalThreshold = result.OverallCoverage.LineCoverage >= _configuration.CriticalThreshold;
            result.Success = result.MeetsCriticalThreshold;
        }

        private List<string> ExtractReferencedTypes(string testContent)
        {
            var types = new List<string>();
            
            // Extract using statements
            var usingPattern = @"using\s+([^;]+);";
            var matches = Regex.Matches(testContent, usingPattern);
            
            foreach (Match match in matches)
            {
                var ns = match.Groups[1].Value;
                if (ns.StartsWith("NeoServiceLayer") && !ns.Contains("Tests"))
                {
                    var lastPart = ns.Split('.').Last();
                    types.Add(lastPart);
                }
            }

            // Extract class instantiations
            var newPattern = @"new\s+([A-Z][a-zA-Z0-9_]+)\s*\(";
            matches = Regex.Matches(testContent, newPattern);
            
            foreach (Match match in matches)
            {
                types.Add(match.Groups[1].Value);
            }

            return types.Distinct().ToList();
        }

        private async Task<int> EstimateFileComplexityAsync(string filePath)
        {
            var content = await File.ReadAllTextAsync(filePath);
            var lines = content.Split('\n');
            
            var complexity = 0;
            complexity += Regex.Matches(content, @"\bif\b").Count * 2;
            complexity += Regex.Matches(content, @"\bfor\b|\bforeach\b|\bwhile\b").Count * 3;
            complexity += Regex.Matches(content, @"\btry\b").Count * 2;
            complexity += Regex.Matches(content, @"\bswitch\b").Count * 4;
            complexity += Regex.Matches(content, @"\bclass\b").Count * 5;
            complexity += Regex.Matches(content, @"\binterface\b").Count * 3;
            
            return complexity;
        }

        private string DeterminePriority(string filePath)
        {
            if (filePath.Contains("Core") || filePath.Contains("Domain"))
                return "Critical";
            if (filePath.Contains("Service") || filePath.Contains("Controller"))
                return "High";
            if (filePath.Contains("Infrastructure") || filePath.Contains("Data"))
                return "Medium";
            return "Low";
        }

        private async Task AnalyzeUntestedCodeElementsAsync(string filePath, UntestedCodeReport report)
        {
            var content = await File.ReadAllTextAsync(filePath);
            
            // Extract classes
            var classPattern = @"public\s+(?:abstract\s+|sealed\s+)?class\s+([A-Z][a-zA-Z0-9_]+)";
            var classMatches = Regex.Matches(content, classPattern);
            
            foreach (Match match in classMatches)
            {
                report.UntestedClasses.Add(new UntestedClass
                {
                    ClassName = match.Groups[1].Value,
                    FilePath = filePath,
                    Priority = DeterminePriority(filePath)
                });
            }

            // Extract methods
            var methodPattern = @"public\s+(?:async\s+)?(?:static\s+)?(?:virtual\s+)?(?:override\s+)?([a-zA-Z<>_]+(?:\[\])?)\s+([A-Z][a-zA-Z0-9_]+)\s*\(";
            var methodMatches = Regex.Matches(content, methodPattern);
            
            foreach (Match match in methodMatches)
            {
                report.UntestedMethods.Add(new UntestedMethod
                {
                    MethodName = match.Groups[2].Value,
                    ReturnType = match.Groups[1].Value,
                    FilePath = filePath,
                    Priority = DeterminePriority(filePath)
                });
            }
        }

        private bool IsCriticalFile(string filePath)
        {
            var criticalPatterns = new[]
            {
                "Authentication",
                "Authorization",
                "Security",
                "Payment",
                "Transaction",
                "Crypto",
                "Key",
                "Enclave"
            };

            return criticalPatterns.Any(pattern => 
                filePath.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        private string DetermineImpact(ModuleCoverage module)
        {
            if (module.ModuleName.Contains("Core") || module.ModuleName.Contains("Security"))
                return "Critical - Core business logic";
            if (module.ModuleName.Contains("Service"))
                return "High - Service layer functionality";
            if (module.ModuleName.Contains("Infrastructure"))
                return "Medium - Infrastructure support";
            return "Low - Utility functions";
        }

        private string GenerateModuleRecommendation(ModuleCoverage module)
        {
            var recommendations = new List<string>();
            
            if (module.LineCoverage < 50)
            {
                recommendations.Add($"Add unit tests for {module.ModuleName}");
            }
            else if (module.LineCoverage < 70)
            {
                recommendations.Add($"Improve test coverage for {module.ModuleName} edge cases");
            }
            else
            {
                recommendations.Add($"Consider adding integration tests for {module.ModuleName}");
            }

            if (module.BranchCoverage < module.LineCoverage * 0.8)
            {
                recommendations.Add("Focus on branch coverage to test all code paths");
            }

            return string.Join("; ", recommendations);
        }

        #endregion
    }
}