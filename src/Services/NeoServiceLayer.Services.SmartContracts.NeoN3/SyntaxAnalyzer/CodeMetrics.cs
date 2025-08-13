using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.SmartContracts.NeoN3.SyntaxAnalyzer;

/// <summary>
/// Code metrics for analyzed smart contracts.
/// </summary>
public class CodeMetrics
{
    /// <summary>
    /// Total lines of code (excluding comments and blank lines).
    /// </summary>
    public int LinesOfCode { get; set; }

    /// <summary>
    /// Total lines in file.
    /// </summary>
    public int TotalLines { get; set; }

    /// <summary>
    /// Number of comment lines.
    /// </summary>
    public int CommentLines { get; set; }

    /// <summary>
    /// Number of blank lines.
    /// </summary>
    public int BlankLines { get; set; }

    /// <summary>
    /// Number of contracts defined.
    /// </summary>
    public int ContractCount { get; set; }

    /// <summary>
    /// Number of methods.
    /// </summary>
    public int MethodCount { get; set; }

    /// <summary>
    /// Number of properties.
    /// </summary>
    public int PropertyCount { get; set; }

    /// <summary>
    /// Number of fields.
    /// </summary>
    public int FieldCount { get; set; }

    /// <summary>
    /// Number of events.
    /// </summary>
    public int EventCount { get; set; }

    /// <summary>
    /// Cyclomatic complexity.
    /// </summary>
    public int CyclomaticComplexity { get; set; }

    /// <summary>
    /// Maximum nesting depth.
    /// </summary>
    public int MaxNestingDepth { get; set; }

    /// <summary>
    /// Number of external calls.
    /// </summary>
    public int ExternalCallCount { get; set; }

    /// <summary>
    /// Number of storage operations.
    /// </summary>
    public int StorageOperationCount { get; set; }

    /// <summary>
    /// Number of loops.
    /// </summary>
    public int LoopCount { get; set; }

    /// <summary>
    /// Number of conditional statements.
    /// </summary>
    public int ConditionalCount { get; set; }

    /// <summary>
    /// Estimated gas consumption.
    /// </summary>
    public long EstimatedGasConsumption { get; set; }

    /// <summary>
    /// Method metrics.
    /// </summary>
    public Dictionary<string, MethodMetrics> Methods { get; set; } = new();

    /// <summary>
    /// Dependency metrics.
    /// </summary>
    public DependencyMetrics Dependencies { get; set; } = new();

    /// <summary>
    /// Security metrics.
    /// </summary>
    public SecurityMetrics Security { get; set; } = new();
}

/// <summary>
/// Metrics for individual methods.
/// </summary>
public class MethodMetrics
{
    /// <summary>
    /// Method name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Lines of code in method.
    /// </summary>
    public int LinesOfCode { get; set; }

    /// <summary>
    /// Cyclomatic complexity.
    /// </summary>
    public int CyclomaticComplexity { get; set; }

    /// <summary>
    /// Number of parameters.
    /// </summary>
    public int ParameterCount { get; set; }

    /// <summary>
    /// Number of local variables.
    /// </summary>
    public int LocalVariableCount { get; set; }

    /// <summary>
    /// Number of return statements.
    /// </summary>
    public int ReturnStatementCount { get; set; }

    /// <summary>
    /// Maximum nesting depth.
    /// </summary>
    public int MaxNestingDepth { get; set; }

    /// <summary>
    /// Estimated gas consumption.
    /// </summary>
    public long EstimatedGasConsumption { get; set; }

    /// <summary>
    /// Cognitive complexity.
    /// </summary>
    public int CognitiveComplexity { get; set; }

    /// <summary>
    /// Whether method modifies state.
    /// </summary>
    public bool ModifiesState { get; set; }

    /// <summary>
    /// Whether method is safe.
    /// </summary>
    public bool IsSafe { get; set; }
}

/// <summary>
/// Dependency metrics.
/// </summary>
public class DependencyMetrics
{
    /// <summary>
    /// Number of using directives.
    /// </summary>
    public int UsingCount { get; set; }

    /// <summary>
    /// Number of external contract dependencies.
    /// </summary>
    public int ExternalContractCount { get; set; }

    /// <summary>
    /// List of external contracts referenced.
    /// </summary>
    public List<string> ExternalContracts { get; set; } = new();

    /// <summary>
    /// Number of native contract calls.
    /// </summary>
    public int NativeContractCallCount { get; set; }

    /// <summary>
    /// List of native contracts used.
    /// </summary>
    public List<string> NativeContractsUsed { get; set; } = new();

    /// <summary>
    /// Coupling metrics.
    /// </summary>
    public CouplingMetrics Coupling { get; set; } = new();
}

/// <summary>
/// Coupling metrics.
/// </summary>
public class CouplingMetrics
{
    /// <summary>
    /// Afferent coupling (Ca) - number of classes that depend on this contract.
    /// </summary>
    public int AfferentCoupling { get; set; }

    /// <summary>
    /// Efferent coupling (Ce) - number of classes this contract depends on.
    /// </summary>
    public int EfferentCoupling { get; set; }

    /// <summary>
    /// Instability (I = Ce / (Ca + Ce)).
    /// </summary>
    public double Instability => (AfferentCoupling + EfferentCoupling) == 0 ? 0 : 
        (double)EfferentCoupling / (AfferentCoupling + EfferentCoupling);

    /// <summary>
    /// Abstractness (A = Abstract Classes / Total Classes).
    /// </summary>
    public double Abstractness { get; set; }

    /// <summary>
    /// Distance from main sequence (D = |A + I - 1|).
    /// </summary>
    public double Distance => Math.Abs(Abstractness + Instability - 1);
}

/// <summary>
/// Security-related metrics.
/// </summary>
public class SecurityMetrics
{
    /// <summary>
    /// Number of access control checks.
    /// </summary>
    public int AccessControlCheckCount { get; set; }

    /// <summary>
    /// Number of external calls without checks.
    /// </summary>
    public int UncheckedExternalCallCount { get; set; }

    /// <summary>
    /// Number of reentrancy vulnerabilities.
    /// </summary>
    public int ReentrancyVulnerabilityCount { get; set; }

    /// <summary>
    /// Number of integer overflow risks.
    /// </summary>
    public int IntegerOverflowRiskCount { get; set; }

    /// <summary>
    /// Number of unchecked arithmetic operations.
    /// </summary>
    public int UncheckedArithmeticCount { get; set; }

    /// <summary>
    /// Number of hardcoded values.
    /// </summary>
    public int HardcodedValueCount { get; set; }

    /// <summary>
    /// Security score (0-100).
    /// </summary>
    public int SecurityScore { get; set; }

    /// <summary>
    /// List of security issues found.
    /// </summary>
    public List<string> SecurityIssues { get; set; } = new();
}

/// <summary>
/// Symbol table interface.
/// </summary>
public interface ISymbolTable
{
    /// <summary>
    /// Gets all symbols.
    /// </summary>
    IEnumerable<Symbol> GetAllSymbols();

    /// <summary>
    /// Looks up a symbol by name.
    /// </summary>
    Symbol? LookupSymbol(string name, SymbolScope scope);

    /// <summary>
    /// Adds a symbol.
    /// </summary>
    void AddSymbol(Symbol symbol);

    /// <summary>
    /// Enters a new scope.
    /// </summary>
    void EnterScope(string scopeName);

    /// <summary>
    /// Exits the current scope.
    /// </summary>
    void ExitScope();

    /// <summary>
    /// Gets the current scope.
    /// </summary>
    SymbolScope GetCurrentScope();
}

/// <summary>
/// Represents a symbol in the symbol table.
/// </summary>
public class Symbol
{
    /// <summary>
    /// Symbol name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Symbol type.
    /// </summary>
    public SymbolType Type { get; set; }

    /// <summary>
    /// Data type of the symbol.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Scope where symbol is defined.
    /// </summary>
    public SymbolScope Scope { get; set; } = null!;

    /// <summary>
    /// Source location.
    /// </summary>
    public SourceLocation Location { get; set; } = new();

    /// <summary>
    /// Additional attributes.
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = new();
}

/// <summary>
/// Symbol types.
/// </summary>
public enum SymbolType
{
    Contract,
    Method,
    Property,
    Field,
    Parameter,
    LocalVariable,
    Event,
    Type,
    Namespace
}

/// <summary>
/// Symbol scope.
/// </summary>
public class SymbolScope
{
    /// <summary>
    /// Scope name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parent scope.
    /// </summary>
    public SymbolScope? Parent { get; set; }

    /// <summary>
    /// Symbols in this scope.
    /// </summary>
    public Dictionary<string, Symbol> Symbols { get; set; } = new();

    /// <summary>
    /// Child scopes.
    /// </summary>
    public List<SymbolScope> Children { get; set; } = new();
}

/// <summary>
/// Control flow graph interface.
/// </summary>
public interface IControlFlowGraph
{
    /// <summary>
    /// Gets the entry node.
    /// </summary>
    ControlFlowNode Entry { get; }

    /// <summary>
    /// Gets all nodes.
    /// </summary>
    IEnumerable<ControlFlowNode> Nodes { get; }

    /// <summary>
    /// Gets all edges.
    /// </summary>
    IEnumerable<ControlFlowEdge> Edges { get; }

    /// <summary>
    /// Finds paths between nodes.
    /// </summary>
    IEnumerable<List<ControlFlowNode>> FindPaths(ControlFlowNode from, ControlFlowNode to);

    /// <summary>
    /// Detects cycles in the graph.
    /// </summary>
    IEnumerable<List<ControlFlowNode>> DetectCycles();
}

/// <summary>
/// Control flow node.
/// </summary>
public class ControlFlowNode
{
    /// <summary>
    /// Node identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Node type.
    /// </summary>
    public ControlFlowNodeType Type { get; set; }

    /// <summary>
    /// Associated AST node.
    /// </summary>
    public IAstNode? AstNode { get; set; }

    /// <summary>
    /// Incoming edges.
    /// </summary>
    public List<ControlFlowEdge> IncomingEdges { get; set; } = new();

    /// <summary>
    /// Outgoing edges.
    /// </summary>
    public List<ControlFlowEdge> OutgoingEdges { get; set; } = new();

    /// <summary>
    /// Basic block information.
    /// </summary>
    public BasicBlock? BasicBlock { get; set; }
}

/// <summary>
/// Control flow node types.
/// </summary>
public enum ControlFlowNodeType
{
    Entry,
    Exit,
    Statement,
    Condition,
    Loop,
    Switch,
    Try,
    Catch,
    Finally,
    Return,
    Throw
}

/// <summary>
/// Control flow edge.
/// </summary>
public class ControlFlowEdge
{
    /// <summary>
    /// Source node.
    /// </summary>
    public ControlFlowNode Source { get; set; } = null!;

    /// <summary>
    /// Target node.
    /// </summary>
    public ControlFlowNode Target { get; set; } = null!;

    /// <summary>
    /// Edge type.
    /// </summary>
    public ControlFlowEdgeType Type { get; set; }

    /// <summary>
    /// Condition for conditional edges.
    /// </summary>
    public string? Condition { get; set; }
}

/// <summary>
/// Control flow edge types.
/// </summary>
public enum ControlFlowEdgeType
{
    Sequential,
    Conditional,
    Loop,
    Break,
    Continue,
    Return,
    Exception,
    Finally
}

/// <summary>
/// Basic block in control flow.
/// </summary>
public class BasicBlock
{
    /// <summary>
    /// Block identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Statements in the block.
    /// </summary>
    public List<IAstNode> Statements { get; set; } = new();

    /// <summary>
    /// Live variables at entry.
    /// </summary>
    public HashSet<string> LiveIn { get; set; } = new();

    /// <summary>
    /// Live variables at exit.
    /// </summary>
    public HashSet<string> LiveOut { get; set; } = new();

    /// <summary>
    /// Variables defined in block.
    /// </summary>
    public HashSet<string> Defined { get; set; } = new();

    /// <summary>
    /// Variables used in block.
    /// </summary>
    public HashSet<string> Used { get; set; } = new();
}

/// <summary>
/// Stack usage analysis.
/// </summary>
public class StackUsageAnalysis
{
    /// <summary>
    /// Maximum stack depth.
    /// </summary>
    public int MaxStackDepth { get; set; }

    /// <summary>
    /// Average stack depth.
    /// </summary>
    public double AverageStackDepth { get; set; }

    /// <summary>
    /// Stack depth at each instruction.
    /// </summary>
    public Dictionary<int, int> StackDepthMap { get; set; } = new();

    /// <summary>
    /// Potential stack overflow points.
    /// </summary>
    public List<int> PotentialOverflowPoints { get; set; } = new();

    /// <summary>
    /// Potential stack underflow points.
    /// </summary>
    public List<int> PotentialUnderflowPoints { get; set; } = new();
}

/// <summary>
/// Validation context interface.
/// </summary>
public interface IValidationContext
{
    /// <summary>
    /// Gets the symbol table.
    /// </summary>
    ISymbolTable SymbolTable { get; }

    /// <summary>
    /// Gets the control flow graph.
    /// </summary>
    IControlFlowGraph ControlFlowGraph { get; }

    /// <summary>
    /// Gets the current scope.
    /// </summary>
    SymbolScope CurrentScope { get; }

    /// <summary>
    /// Reports an error.
    /// </summary>
    void ReportError(string message, SourceLocation location);

    /// <summary>
    /// Reports a warning.
    /// </summary>
    void ReportWarning(string message, SourceLocation location);

    /// <summary>
    /// Gets configuration value.
    /// </summary>
    T GetConfiguration<T>(string key, T defaultValue);
}

/// <summary>
/// Validation result.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation messages.
    /// </summary>
    public List<ValidationMessage> Messages { get; set; } = new();
}

/// <summary>
/// Validation message.
/// </summary>
public class ValidationMessage
{
    /// <summary>
    /// Message severity.
    /// </summary>
    public Severity Severity { get; set; }

    /// <summary>
    /// Message text.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Source location.
    /// </summary>
    public SourceLocation Location { get; set; } = new();
}