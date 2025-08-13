using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Services.SmartContracts.NeoN3.SyntaxAnalyzer;

/// <summary>
/// Interface for Neo N3 smart contract syntax analyzer.
/// </summary>
public interface INeoN3SyntaxAnalyzer
{
    /// <summary>
    /// Analyzes Neo N3 smart contract source code.
    /// </summary>
    /// <param name="sourceCode">The source code to analyze.</param>
    /// <param name="options">Analysis options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis result containing AST, errors, warnings, and suggestions.</returns>
    Task<SyntaxAnalysisResult> AnalyzeAsync(
        string sourceCode, 
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates Neo N3 smart contract bytecode.
    /// </summary>
    /// <param name="bytecode">The contract bytecode.</param>
    /// <param name="manifest">The contract manifest JSON.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<BytecodeValidationResult> ValidateBytecodeAsync(
        byte[] bytecode,
        string manifest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs security analysis on contract code.
    /// </summary>
    /// <param name="sourceCode">The source code to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Security analysis result.</returns>
    Task<SecurityAnalysisResult> AnalyzeSecurityAsync(
        string sourceCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggests optimizations for contract code.
    /// </summary>
    /// <param name="sourceCode">The source code to optimize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Optimization suggestions.</returns>
    Task<OptimizationResult> SuggestOptimizationsAsync(
        string sourceCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Formats Neo N3 smart contract code.
    /// </summary>
    /// <param name="sourceCode">The source code to format.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>Formatted code.</returns>
    string FormatCode(string sourceCode, FormattingOptions? options = null);

    /// <summary>
    /// Gets supported Neo N3 language features.
    /// </summary>
    /// <returns>List of supported features.</returns>
    IEnumerable<LanguageFeature> GetSupportedFeatures();
}

/// <summary>
/// Options for syntax analysis.
/// </summary>
public class AnalysisOptions
{
    /// <summary>
    /// Enable detailed semantic analysis.
    /// </summary>
    public bool EnableSemanticAnalysis { get; set; } = true;

    /// <summary>
    /// Enable security vulnerability detection.
    /// </summary>
    public bool EnableSecurityAnalysis { get; set; } = true;

    /// <summary>
    /// Enable gas optimization suggestions.
    /// </summary>
    public bool EnableOptimizationAnalysis { get; set; } = true;

    /// <summary>
    /// Maximum number of errors to report.
    /// </summary>
    public int MaxErrors { get; set; } = 100;

    /// <summary>
    /// Maximum number of warnings to report.
    /// </summary>
    public int MaxWarnings { get; set; } = 100;

    /// <summary>
    /// Language version to target.
    /// </summary>
    public string LanguageVersion { get; set; } = "N3";

    /// <summary>
    /// Custom validation rules.
    /// </summary>
    public List<IValidationRule> CustomRules { get; set; } = new();
}

/// <summary>
/// Result of syntax analysis.
/// </summary>
public class SyntaxAnalysisResult
{
    /// <summary>
    /// Whether the analysis was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Abstract syntax tree of the contract.
    /// </summary>
    public IAstNode? Ast { get; set; }

    /// <summary>
    /// List of syntax errors.
    /// </summary>
    public List<SyntaxError> Errors { get; set; } = new();

    /// <summary>
    /// List of warnings.
    /// </summary>
    public List<SyntaxWarning> Warnings { get; set; } = new();

    /// <summary>
    /// List of informational messages.
    /// </summary>
    public List<SyntaxInfo> Info { get; set; } = new();

    /// <summary>
    /// Code metrics.
    /// </summary>
    public CodeMetrics? Metrics { get; set; }

    /// <summary>
    /// Symbol table.
    /// </summary>
    public ISymbolTable? SymbolTable { get; set; }

    /// <summary>
    /// Control flow graph.
    /// </summary>
    public IControlFlowGraph? ControlFlowGraph { get; set; }

    /// <summary>
    /// Analysis duration in milliseconds.
    /// </summary>
    public long AnalysisDurationMs { get; set; }
}

/// <summary>
/// Result of bytecode validation.
/// </summary>
public class BytecodeValidationResult
{
    /// <summary>
    /// Whether the bytecode is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors.
    /// </summary>
    public List<ValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Estimated gas consumption.
    /// </summary>
    public long EstimatedGasConsumption { get; set; }

    /// <summary>
    /// Contract size in bytes.
    /// </summary>
    public int ContractSize { get; set; }

    /// <summary>
    /// Stack usage analysis.
    /// </summary>
    public StackUsageAnalysis? StackUsage { get; set; }
}

/// <summary>
/// Result of security analysis.
/// </summary>
public class SecurityAnalysisResult
{
    /// <summary>
    /// Overall security score (0-100).
    /// </summary>
    public int SecurityScore { get; set; }

    /// <summary>
    /// List of vulnerabilities found.
    /// </summary>
    public List<SecurityVulnerability> Vulnerabilities { get; set; } = new();

    /// <summary>
    /// Security best practices violations.
    /// </summary>
    public List<BestPracticeViolation> BestPracticeViolations { get; set; } = new();

    /// <summary>
    /// Security recommendations.
    /// </summary>
    public List<SecurityRecommendation> Recommendations { get; set; } = new();
}

/// <summary>
/// Result of optimization analysis.
/// </summary>
public class OptimizationResult
{
    /// <summary>
    /// List of optimization suggestions.
    /// </summary>
    public List<OptimizationSuggestion> Suggestions { get; set; } = new();

    /// <summary>
    /// Estimated gas savings.
    /// </summary>
    public long EstimatedGasSavings { get; set; }

    /// <summary>
    /// Optimized code snippets.
    /// </summary>
    public Dictionary<string, string> OptimizedCodeSnippets { get; set; } = new();
}

/// <summary>
/// Formatting options for code.
/// </summary>
public class FormattingOptions
{
    /// <summary>
    /// Indentation style.
    /// </summary>
    public IndentationStyle IndentStyle { get; set; } = IndentationStyle.Spaces;

    /// <summary>
    /// Number of spaces for indentation.
    /// </summary>
    public int IndentSize { get; set; } = 4;

    /// <summary>
    /// Maximum line length.
    /// </summary>
    public int MaxLineLength { get; set; } = 120;

    /// <summary>
    /// Brace style.
    /// </summary>
    public BraceStyle BraceStyle { get; set; } = BraceStyle.EndOfLine;
}

/// <summary>
/// Represents a language feature.
/// </summary>
public class LanguageFeature
{
    /// <summary>
    /// Feature name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Feature description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Minimum language version required.
    /// </summary>
    public string MinVersion { get; set; } = string.Empty;

    /// <summary>
    /// Whether the feature is experimental.
    /// </summary>
    public bool IsExperimental { get; set; }
}

/// <summary>
/// Interface for validation rules.
/// </summary>
public interface IValidationRule
{
    /// <summary>
    /// Rule identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Rule description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Rule severity.
    /// </summary>
    Severity Severity { get; }

    /// <summary>
    /// Validates an AST node.
    /// </summary>
    ValidationResult Validate(IAstNode node, IValidationContext context);
}

/// <summary>
/// Severity levels for issues.
/// </summary>
public enum Severity
{
    Error,
    Warning,
    Info,
    Hint
}

/// <summary>
/// Indentation style.
/// </summary>
public enum IndentationStyle
{
    Spaces,
    Tabs
}

/// <summary>
/// Brace style.
/// </summary>
public enum BraceStyle
{
    EndOfLine,
    NextLine,
    NextLineIndented
}