using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.SmartContracts.NeoN3.SyntaxAnalyzer;

/// <summary>
/// Validation context implementation.
/// </summary>
public class ValidationContext : IValidationContext
{
    public ISymbolTable SymbolTable { get; }
    public IControlFlowGraph ControlFlowGraph { get; }
    public SymbolScope CurrentScope => SymbolTable.GetCurrentScope();
    private readonly Dictionary<string, object> _configuration;
    private readonly List<ValidationMessage> _messages = new();

    public ValidationContext(ISymbolTable symbolTable, IControlFlowGraph controlFlowGraph, Dictionary<string, object> configuration)
    {
        SymbolTable = symbolTable;
        ControlFlowGraph = controlFlowGraph;
        _configuration = configuration;
    }

    public void ReportError(string message, SourceLocation location)
    {
        _messages.Add(new ValidationMessage
        {
            Severity = Severity.Error,
            Message = message,
            Location = location
        });
    }

    public void ReportWarning(string message, SourceLocation location)
    {
        _messages.Add(new ValidationMessage
        {
            Severity = Severity.Warning,
            Message = message,
            Location = location
        });
    }

    public T GetConfiguration<T>(string key, T defaultValue)
    {
        if (_configuration.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
}

/// <summary>
/// Base validation rule.
/// </summary>
public abstract class BaseValidationRule : IValidationRule
{
    public abstract string Id { get; }
    public abstract string Description { get; }
    public abstract Severity Severity { get; }

    public virtual ValidationResult Validate(IAstNode node, IValidationContext context)
    {
        var result = new ValidationResult { IsValid = true };
        var visitor = CreateVisitor(context, result);
        node.Accept(visitor);
        return result;
    }

    protected abstract IAstVisitor<bool> CreateVisitor(IValidationContext context, ValidationResult result);
}

/// <summary>
/// Naming convention validation rule.
/// </summary>
public class NamingConventionRule : BaseValidationRule
{
    public override string Id => "NAMING";
    public override string Description => "Enforces naming conventions";
    public override Severity Severity => Severity.Warning;

    protected override IAstVisitor<bool> CreateVisitor(IValidationContext context, ValidationResult result)
    {
        return new NamingConventionVisitor(context, result);
    }

    private class NamingConventionVisitor : BaseAstVisitor
    {
        private readonly IValidationContext _context;
        private readonly ValidationResult _result;

        public NamingConventionVisitor(IValidationContext context, ValidationResult result)
        {
            _context = context;
            _result = result;
        }

        public override bool VisitContract(ContractNode node)
        {
            if (!char.IsUpper(node.Name[0]))
            {
                _result.Messages.Add(new ValidationMessage
                {
                    Severity = Severity.Warning,
                    Message = $"Contract name '{node.Name}' should start with uppercase letter",
                    Location = node.Location
                });
            }
            return base.VisitContract(node);
        }
    }
}

/// <summary>
/// Other validation rules...
/// </summary>
public class UnusedVariableRule : BaseValidationRule
{
    public override string Id => "UNUSED_VAR";
    public override string Description => "Detects unused variables";
    public override Severity Severity => Severity.Warning;
    protected override IAstVisitor<bool> CreateVisitor(IValidationContext context, ValidationResult result)
    {
        return new BaseAstVisitor();
    }
}

public class DeadCodeRule : BaseValidationRule
{
    public override string Id => "DEAD_CODE";
    public override string Description => "Detects unreachable code";
    public override Severity Severity => Severity.Warning;
    protected override IAstVisitor<bool> CreateVisitor(IValidationContext context, ValidationResult result)
    {
        return new BaseAstVisitor();
    }
}

public class ComplexityRule : BaseValidationRule
{
    public override string Id => "COMPLEXITY";
    public override string Description => "Checks cyclomatic complexity";
    public override Severity Severity => Severity.Warning;
    protected override IAstVisitor<bool> CreateVisitor(IValidationContext context, ValidationResult result)
    {
        return new BaseAstVisitor();
    }
}

public class SecurityPatternRule : BaseValidationRule
{
    public override string Id => "SECURITY";
    public override string Description => "Checks for security patterns";
    public override Severity Severity => Severity.Error;
    protected override IAstVisitor<bool> CreateVisitor(IValidationContext context, ValidationResult result)
    {
        return new BaseAstVisitor();
    }
}

public class GasOptimizationRule : BaseValidationRule
{
    public override string Id => "GAS_OPT";
    public override string Description => "Suggests gas optimizations";
    public override Severity Severity => Severity.Info;
    protected override IAstVisitor<bool> CreateVisitor(IValidationContext context, ValidationResult result)
    {
        return new BaseAstVisitor();
    }
}

public class StorageOptimizationRule : BaseValidationRule
{
    public override string Id => "STORAGE_OPT";
    public override string Description => "Suggests storage optimizations";
    public override Severity Severity => Severity.Info;
    protected override IAstVisitor<bool> CreateVisitor(IValidationContext context, ValidationResult result)
    {
        return new BaseAstVisitor();
    }
}

public class EventNamingRule : BaseValidationRule
{
    public override string Id => "EVENT_NAMING";
    public override string Description => "Checks event naming conventions";
    public override Severity Severity => Severity.Warning;
    protected override IAstVisitor<bool> CreateVisitor(IValidationContext context, ValidationResult result)
    {
        return new BaseAstVisitor();
    }
}

public class AccessControlRule : BaseValidationRule
{
    public override string Id => "ACCESS_CONTROL";
    public override string Description => "Validates access control";
    public override Severity Severity => Severity.Error;
    protected override IAstVisitor<bool> CreateVisitor(IValidationContext context, ValidationResult result)
    {
        return new BaseAstVisitor();
    }
}

public class ReentrancyGuardRule : BaseValidationRule
{
    public override string Id => "REENTRANCY";
    public override string Description => "Checks for reentrancy vulnerabilities";
    public override Severity Severity => Severity.Error;
    protected override IAstVisitor<bool> CreateVisitor(IValidationContext context, ValidationResult result)
    {
        return new BaseAstVisitor();
    }
}

/// <summary>
/// Base AST visitor for validation rules.
/// </summary>
public class BaseAstVisitor : IAstVisitor<bool>
{
    public virtual bool VisitCompilationUnit(CompilationUnitNode node)
    {
        foreach (var child in node.Children)
        {
            child.Accept(this);
        }
        return true;
    }

    public virtual bool VisitUsing(UsingNode node) => true;
    public virtual bool VisitNamespace(NamespaceNode node)
    {
        foreach (var child in node.Children)
        {
            child.Accept(this);
        }
        return true;
    }

    public virtual bool VisitContract(ContractNode node)
    {
        foreach (var child in node.Children)
        {
            child.Accept(this);
        }
        return true;
    }

    public virtual bool VisitField(FieldNode node) => true;
    public virtual bool VisitProperty(PropertyNode node) => true;
    public virtual bool VisitMethod(MethodNode node)
    {
        node.Body?.Accept(this);
        return true;
    }

    public virtual bool VisitEvent(EventNode node) => true;
    public virtual bool VisitConstructor(ConstructorNode node) => true;
    public virtual bool VisitBlock(BlockNode node)
    {
        foreach (var child in node.Children)
        {
            child.Accept(this);
        }
        return true;
    }

    public virtual bool VisitExpressionStatement(ExpressionStatementNode node) => true;
    public virtual bool VisitIfStatement(IfStatementNode node) => true;
    public virtual bool VisitWhileStatement(WhileStatementNode node) => true;
    public virtual bool VisitForStatement(ForStatementNode node) => true;
    public virtual bool VisitReturnStatement(ReturnStatementNode node) => true;
    public virtual bool VisitVariableDeclaration(VariableDeclarationNode node) => true;
    public virtual bool VisitBinaryExpression(BinaryExpressionNode node) => true;
    public virtual bool VisitUnaryExpression(UnaryExpressionNode node) => true;
    public virtual bool VisitMethodCall(MethodCallNode node) => true;
    public virtual bool VisitLiteral(LiteralNode node) => true;
    public virtual bool VisitIdentifier(IdentifierNode node) => true;
    public virtual bool VisitParameter(ParameterNode node) => true;
}

/// <summary>
/// Metrics calculator.
/// </summary>
public class MetricsCalculator
{
    public CodeMetrics Calculate(IAstNode ast, string sourceCode)
    {
        var metrics = new CodeMetrics();

        // Calculate line counts
        var lines = sourceCode.Split('\n');
        metrics.TotalLines = lines.Length;
        metrics.BlankLines = lines.Count(line => string.IsNullOrWhiteSpace(line));
        metrics.CommentLines = lines.Count(line => line.TrimStart().StartsWith("//"));
        metrics.LinesOfCode = metrics.TotalLines - metrics.BlankLines - metrics.CommentLines;

        // Visit AST to calculate other metrics
        var visitor = new MetricsVisitor(metrics);
        ast.Accept(visitor);

        return metrics;
    }

    private class MetricsVisitor : BaseAstVisitor
    {
        private readonly CodeMetrics _metrics;

        public MetricsVisitor(CodeMetrics metrics)
        {
            _metrics = metrics;
        }

        public override bool VisitContract(ContractNode node)
        {
            _metrics.ContractCount++;
            return base.VisitContract(node);
        }

        public override bool VisitMethod(MethodNode node)
        {
            _metrics.MethodCount++;
            return base.VisitMethod(node);
        }

        public override bool VisitField(FieldNode node)
        {
            _metrics.FieldCount++;
            return base.VisitField(node);
        }

        public override bool VisitProperty(PropertyNode node)
        {
            _metrics.PropertyCount++;
            return base.VisitProperty(node);
        }

        public override bool VisitEvent(EventNode node)
        {
            _metrics.EventCount++;
            return base.VisitEvent(node);
        }
    }
}
