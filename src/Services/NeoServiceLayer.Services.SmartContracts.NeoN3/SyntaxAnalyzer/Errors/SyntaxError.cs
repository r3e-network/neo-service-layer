using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.SmartContracts.NeoN3.SyntaxAnalyzer;

/// <summary>
/// Represents a syntax error in Neo N3 contract code.
/// </summary>
public class SyntaxError
{
    /// <summary>
    /// Error code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Source location where the error occurred.
    /// </summary>
    public SourceLocation Location { get; set; } = new();

    /// <summary>
    /// Error severity.
    /// </summary>
    public Severity Severity { get; set; } = Severity.Error;

    /// <summary>
    /// Suggested fixes for the error.
    /// </summary>
    public List<CodeFix> SuggestedFixes { get; set; } = new();

    /// <summary>
    /// Additional context information.
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Represents a syntax warning.
/// </summary>
public class SyntaxWarning : SyntaxError
{
    public SyntaxWarning()
    {
        Severity = Severity.Warning;
    }
}

/// <summary>
/// Represents informational syntax message.
/// </summary>
public class SyntaxInfo : SyntaxError
{
    public SyntaxInfo()
    {
        Severity = Severity.Info;
    }
}

/// <summary>
/// Represents a validation error.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Bytecode offset where error occurred.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// OpCode that caused the error.
    /// </summary>
    public string? OpCode { get; set; }

    /// <summary>
    /// Error type.
    /// </summary>
    public ValidationErrorType ErrorType { get; set; }
}

/// <summary>
/// Types of validation errors.
/// </summary>
public enum ValidationErrorType
{
    InvalidOpCode,
    StackUnderflow,
    StackOverflow,
    InvalidJump,
    InvalidMethodCall,
    TypeMismatch,
    ContractTooLarge,
    GasLimitExceeded,
    InvalidManifest,
    MissingPermission,
    Other
}

/// <summary>
/// Represents a suggested code fix.
/// </summary>
public class CodeFix
{
    /// <summary>
    /// Fix description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Code changes to apply.
    /// </summary>
    public List<CodeChange> Changes { get; set; } = new();

    /// <summary>
    /// Confidence level (0-100).
    /// </summary>
    public int Confidence { get; set; }
}

/// <summary>
/// Represents a code change.
/// </summary>
public class CodeChange
{
    /// <summary>
    /// Location to apply the change.
    /// </summary>
    public SourceLocation Location { get; set; } = new();

    /// <summary>
    /// New text to insert.
    /// </summary>
    public string NewText { get; set; } = string.Empty;

    /// <summary>
    /// Type of change.
    /// </summary>
    public ChangeType Type { get; set; }
}

/// <summary>
/// Types of code changes.
/// </summary>
public enum ChangeType
{
    Insert,
    Replace,
    Delete
}

/// <summary>
/// Security vulnerability found in contract.
/// </summary>
public class SecurityVulnerability
{
    /// <summary>
    /// Vulnerability identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Vulnerability name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Severity level.
    /// </summary>
    public SecuritySeverity Severity { get; set; }

    /// <summary>
    /// Location in code.
    /// </summary>
    public SourceLocation Location { get; set; } = new();

    /// <summary>
    /// CWE reference if applicable.
    /// </summary>
    public string? CweReference { get; set; }

    /// <summary>
    /// Exploitation difficulty.
    /// </summary>
    public ExploitDifficulty ExploitDifficulty { get; set; }

    /// <summary>
    /// Remediation suggestions.
    /// </summary>
    public List<string> Remediations { get; set; } = new();
}

/// <summary>
/// Security severity levels.
/// </summary>
public enum SecuritySeverity
{
    Critical,
    High,
    Medium,
    Low,
    Informational
}

/// <summary>
/// Exploit difficulty levels.
/// </summary>
public enum ExploitDifficulty
{
    Trivial,
    Easy,
    Medium,
    Hard,
    Theoretical
}

/// <summary>
/// Best practice violation.
/// </summary>
public class BestPracticeViolation
{
    /// <summary>
    /// Rule identifier.
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Rule name.
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the violation.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Location in code.
    /// </summary>
    public SourceLocation Location { get; set; } = new();

    /// <summary>
    /// Recommendation.
    /// </summary>
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// Security recommendation.
/// </summary>
public class SecurityRecommendation
{
    /// <summary>
    /// Recommendation title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Priority level.
    /// </summary>
    public RecommendationPriority Priority { get; set; }

    /// <summary>
    /// Implementation complexity.
    /// </summary>
    public ImplementationComplexity Complexity { get; set; }

    /// <summary>
    /// Code examples.
    /// </summary>
    public List<CodeExample> Examples { get; set; } = new();
}

/// <summary>
/// Recommendation priority levels.
/// </summary>
public enum RecommendationPriority
{
    Critical,
    High,
    Medium,
    Low
}

/// <summary>
/// Implementation complexity levels.
/// </summary>
public enum ImplementationComplexity
{
    Simple,
    Moderate,
    Complex
}

/// <summary>
/// Code example.
/// </summary>
public class CodeExample
{
    /// <summary>
    /// Example title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Code before optimization.
    /// </summary>
    public string Before { get; set; } = string.Empty;

    /// <summary>
    /// Code after optimization.
    /// </summary>
    public string After { get; set; } = string.Empty;

    /// <summary>
    /// Explanation of changes.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;
}

/// <summary>
/// Optimization suggestion.
/// </summary>
public class OptimizationSuggestion
{
    /// <summary>
    /// Optimization type.
    /// </summary>
    public OptimizationType Type { get; set; }

    /// <summary>
    /// Description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Location in code.
    /// </summary>
    public SourceLocation Location { get; set; } = new();

    /// <summary>
    /// Estimated gas savings.
    /// </summary>
    public long EstimatedGasSavings { get; set; }

    /// <summary>
    /// Optimized code snippet.
    /// </summary>
    public string OptimizedCode { get; set; } = string.Empty;

    /// <summary>
    /// Risk level of applying this optimization.
    /// </summary>
    public OptimizationRisk RiskLevel { get; set; }
}

/// <summary>
/// Types of optimizations.
/// </summary>
public enum OptimizationType
{
    LoopOptimization,
    StorageOptimization,
    ComputationReduction,
    DeadCodeElimination,
    ConstantFolding,
    InlineExpansion,
    CommonSubexpressionElimination,
    MethodConsolidation,
    DataStructureOptimization,
    GasReduction
}

/// <summary>
/// Risk levels for optimizations.
/// </summary>
public enum OptimizationRisk
{
    None,
    Low,
    Medium,
    High
}