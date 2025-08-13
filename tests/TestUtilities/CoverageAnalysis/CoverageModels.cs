using System;
using System.Collections.Generic;

namespace NeoServiceLayer.TestUtilities.CoverageAnalysis
{
    #region Configuration

    public class CoverageConfiguration
    {
        public double CriticalThreshold { get; set; } = 60.0;
        public double TargetThreshold { get; set; } = 80.0;
        public double OptimalThreshold { get; set; } = 90.0;
        public double CriticalFileThreshold { get; set; } = 90.0;
        public bool IncludeGeneratedCode { get; set; } = false;
        public List<string> ExcludePatterns { get; set; } = new();
        public List<string> IncludePatterns { get; set; } = new();

        public static CoverageConfiguration Default => new()
        {
            CriticalThreshold = 60.0,
            TargetThreshold = 80.0,
            OptimalThreshold = 90.0,
            CriticalFileThreshold = 90.0,
            IncludeGeneratedCode = false,
            ExcludePatterns = new List<string> { "*.Designer.cs", "*.g.cs", "*/Migrations/*" },
            IncludePatterns = new List<string> { "*.cs" }
        };
    }

    #endregion

    #region Analysis Results

    public class CoverageAnalysisResult
    {
        public string AnalysisId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string ProjectRoot { get; set; } = string.Empty;
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
        
        public List<ModuleCoverage> Modules { get; set; } = new();
        public CoverageMetrics OverallCoverage { get; set; } = new();
        public List<CoverageGap> CoverageGaps { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        
        public List<UntestedFile> UntestedFiles { get; set; } = new();
        public int TotalUntestedFiles { get; set; }
        
        public bool MeetsTargetThreshold { get; set; }
        public bool MeetsCriticalThreshold { get; set; }
    }

    public class ModuleCoverage
    {
        public string ModuleName { get; set; } = string.Empty;
        public double LineCoverage { get; set; }
        public double BranchCoverage { get; set; }
        public double MethodCoverage { get; set; }
        public int TotalLines { get; set; }
        public int CoveredLines { get; set; }
        public int TotalBranches { get; set; }
        public int CoveredBranches { get; set; }
        public int TotalMethods { get; set; }
        public int CoveredMethods { get; set; }
        public List<FileCoverage> Files { get; set; } = new();
    }

    public class FileCoverage
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public double LineCoverage { get; set; }
        public double BranchCoverage { get; set; }
        public double MethodCoverage { get; set; }
        public int TotalLines { get; set; }
        public int CoveredLines { get; set; }
        public List<ClassCoverage> Classes { get; set; } = new();
    }

    public class ClassCoverage
    {
        public string ClassName { get; set; } = string.Empty;
        public double LineCoverage { get; set; }
        public double BranchCoverage { get; set; }
        public double MethodCoverage { get; set; }
        public int Complexity { get; set; }
        public List<MethodCoverage> Methods { get; set; } = new();
    }

    public class MethodCoverage
    {
        public string MethodName { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public int LinesCovered { get; set; }
        public int LinesTotal { get; set; }
        public int BranchesCovered { get; set; }
        public int BranchesTotal { get; set; }
        public int CyclomaticComplexity { get; set; }
        public bool IsTested => LinesCovered > 0;
    }

    public class CoverageMetrics
    {
        public double LineCoverage { get; set; }
        public double BranchCoverage { get; set; }
        public double MethodCoverage { get; set; }
        public int TotalLines { get; set; }
        public int CoveredLines { get; set; }
        public int TotalBranches { get; set; }
        public int CoveredBranches { get; set; }
        public int TotalMethods { get; set; }
        public int CoveredMethods { get; set; }
    }

    #endregion

    #region Coverage Gaps

    public enum GapSeverity
    {
        Critical,
        Moderate,
        Minor
    }

    public class CoverageGap
    {
        public string Location { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double CurrentCoverage { get; set; }
        public double TargetCoverage { get; set; }
        public double Gap { get; set; }
        public GapSeverity Severity { get; set; }
        public string Impact { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    public class CoverageGapReport
    {
        public DateTime Timestamp { get; set; }
        public List<CoverageGap> CriticalGaps { get; set; } = new();
        public List<CoverageGap> ModerateGaps { get; set; } = new();
        public List<CoverageGap> MinorGaps { get; set; } = new();
        public int TotalGaps => CriticalGaps.Count + ModerateGaps.Count + MinorGaps.Count;
    }

    #endregion

    #region Untested Code

    public class UntestedCodeReport
    {
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public List<UntestedFile> UntestedFiles { get; set; } = new();
        public List<UntestedMethod> UntestedMethods { get; set; } = new();
        public List<UntestedClass> UntestedClasses { get; set; } = new();
        public int TotalUntestedFiles { get; set; }
        public int TotalUntestedMethods { get; set; }
        public int TotalUntestedClasses { get; set; }
    }

    public class UntestedFile
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public int EstimatedComplexity { get; set; }
        public string Priority { get; set; } = string.Empty;
        public List<string> ReasonForTesting { get; set; } = new();
    }

    public class UntestedMethod
    {
        public string MethodName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public List<string> Parameters { get; set; } = new();
        public int CyclomaticComplexity { get; set; }
        public string Priority { get; set; } = string.Empty;
    }

    public class UntestedClass
    {
        public string ClassName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int MethodCount { get; set; }
        public int PropertyCount { get; set; }
        public int LineCount { get; set; }
        public string Priority { get; set; } = string.Empty;
    }

    #endregion

    #region Trends and History

    public class CoverageTrend
    {
        public DateTime AnalysisDate { get; set; }
        public List<CoverageTrendPoint> DataPoints { get; set; } = new();
        public string TrendDirection { get; set; } = string.Empty;
        public double AverageChange { get; set; }
        public double RecentVelocity { get; set; }
        public double PeakCoverage { get; set; }
        public double LowestCoverage { get; set; }
    }

    public class CoverageTrendPoint
    {
        public DateTime Timestamp { get; set; }
        public double LineCoverage { get; set; }
        public double BranchCoverage { get; set; }
        public double MethodCoverage { get; set; }
        public int TotalLines { get; set; }
        public int CoveredLines { get; set; }
        public string? BuildId { get; set; }
        public string? CommitHash { get; set; }
    }

    #endregion

    #region Reports

    public class TestCoverageReport
    {
        public string ReportId { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        
        public CoverageAnalysisResult Analysis { get; set; } = new();
        public UntestedCodeReport UntestedCode { get; set; } = new();
        public CoverageGapReport GapAnalysis { get; set; } = new();
        public CoverageTrend Trend { get; set; } = new();
        
        public List<string> KeyFindings { get; set; } = new();
        public List<string> ActionItems { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ModuleCoverageReport
    {
        public string ModuleName { get; set; } = string.Empty;
        public CoverageMetrics Metrics { get; set; } = new();
        public List<FileCoverage> TopCoveredFiles { get; set; } = new();
        public List<FileCoverage> LeastCoveredFiles { get; set; } = new();
        public List<MethodCoverage> UncoveredMethods { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    #endregion

    #region Test Effectiveness

    public class TestEffectivenessMetrics
    {
        public double MutationScore { get; set; }
        public double DefectDetectionRate { get; set; }
        public double FalsePositiveRate { get; set; }
        public double TestExecutionTime { get; set; }
        public int TotalTests { get; set; }
        public int PassingTests { get; set; }
        public int FailingTests { get; set; }
        public int SkippedTests { get; set; }
        public double TestMaintenanceCost { get; set; }
    }

    public class TestQualityAssessment
    {
        public DateTime AssessmentDate { get; set; }
        public TestEffectivenessMetrics Effectiveness { get; set; } = new();
        public List<TestQualityIssue> Issues { get; set; } = new();
        public List<string> Improvements { get; set; } = new();
        public double OverallScore { get; set; }
    }

    public class TestQualityIssue
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    #endregion
}