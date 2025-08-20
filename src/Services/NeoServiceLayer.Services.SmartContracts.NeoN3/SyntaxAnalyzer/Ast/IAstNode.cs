using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.SmartContracts.NeoN3.SyntaxAnalyzer;

/// <summary>
/// Base interface for all AST nodes.
/// </summary>
public interface IAstNode
{
    /// <summary>
    /// Node type.
    /// </summary>
    AstNodeType NodeType { get; }

    /// <summary>
    /// Parent node.
    /// </summary>
    IAstNode? Parent { get; set; }

    /// <summary>
    /// Child nodes.
    /// </summary>
    IList<IAstNode> Children { get; }

    /// <summary>
    /// Source location information.
    /// </summary>
    SourceLocation Location { get; set; }

    /// <summary>
    /// Accept a visitor.
    /// </summary>
    T Accept<T>(IAstVisitor<T> visitor);

    /// <summary>
    /// Clone the node.
    /// </summary>
    IAstNode Clone();
}

/// <summary>
/// Types of AST nodes.
/// </summary>
public enum AstNodeType
{
    // Program structure
    CompilationUnit,
    Using,
    Namespace,
    Contract,

    // Declarations
    Field,
    Property,
    Method,
    Event,
    Constructor,

    // Statements
    Block,
    ExpressionStatement,
    IfStatement,
    WhileStatement,
    ForStatement,
    ForeachStatement,
    ReturnStatement,
    ThrowStatement,
    TryStatement,
    BreakStatement,
    ContinueStatement,
    VariableDeclaration,

    // Expressions
    BinaryExpression,
    UnaryExpression,
    TernaryExpression,
    MethodCall,
    PropertyAccess,
    ArrayAccess,
    Cast,
    Literal,
    Identifier,
    This,
    New,

    // Types
    PrimitiveType,
    ArrayType,
    GenericType,
    UserDefinedType,

    // Attributes
    Attribute,
    AttributeArgument,

    // Other
    Parameter,
    Argument,
    Modifier
}

/// <summary>
/// Source location information.
/// </summary>
public class SourceLocation
{
    /// <summary>
    /// Start line (1-based).
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// Start column (1-based).
    /// </summary>
    public int StartColumn { get; set; }

    /// <summary>
    /// End line (1-based).
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// End column (1-based).
    /// </summary>
    public int EndColumn { get; set; }

    /// <summary>
    /// Start position in source.
    /// </summary>
    public int StartPosition { get; set; }

    /// <summary>
    /// End position in source.
    /// </summary>
    public int EndPosition { get; set; }

    /// <summary>
    /// Source file name.
    /// </summary>
    public string? FileName { get; set; }
}

/// <summary>
/// Visitor pattern for AST traversal.
/// </summary>
public interface IAstVisitor<T>
{
    T VisitCompilationUnit(CompilationUnitNode node);
    T VisitUsing(UsingNode node);
    T VisitNamespace(NamespaceNode node);
    T VisitContract(ContractNode node);
    T VisitField(FieldNode node);
    T VisitProperty(PropertyNode node);
    T VisitMethod(MethodNode node);
    T VisitEvent(EventNode node);
    T VisitConstructor(ConstructorNode node);
    T VisitBlock(BlockNode node);
    T VisitExpressionStatement(ExpressionStatementNode node);
    T VisitIfStatement(IfStatementNode node);
    T VisitWhileStatement(WhileStatementNode node);
    T VisitForStatement(ForStatementNode node);
    T VisitReturnStatement(ReturnStatementNode node);
    T VisitVariableDeclaration(VariableDeclarationNode node);
    T VisitBinaryExpression(BinaryExpressionNode node);
    T VisitUnaryExpression(UnaryExpressionNode node);
    T VisitMethodCall(MethodCallNode node);
    T VisitLiteral(LiteralNode node);
    T VisitIdentifier(IdentifierNode node);
    T VisitParameter(ParameterNode node);
}

/// <summary>
/// Base class for AST nodes.
/// </summary>
public abstract class AstNode : IAstNode
{
    public abstract AstNodeType NodeType { get; }
    public IAstNode? Parent { get; set; }
    public IList<IAstNode> Children { get; } = new List<IAstNode>();
    public SourceLocation Location { get; set; } = new();

    public abstract T Accept<T>(IAstVisitor<T> visitor);
    public abstract IAstNode Clone();

    protected void AddChild(IAstNode child)
    {
        child.Parent = this;
        Children.Add(child);
    }
}

/// <summary>
/// Compilation unit node (root of AST).
/// </summary>
public class CompilationUnitNode : AstNode
{
    public override AstNodeType NodeType => AstNodeType.CompilationUnit;
    public List<UsingNode> Usings { get; } = new();
    public List<NamespaceNode> Namespaces { get; } = new();
    public List<ContractNode> Contracts { get; } = new();

    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitCompilationUnit(this);
    public override IAstNode Clone() => new CompilationUnitNode { Location = Location };
}

/// <summary>
/// Using directive node.
/// </summary>
public class UsingNode : AstNode
{
    public override AstNodeType NodeType => AstNodeType.Using;
    public string Namespace { get; set; } = string.Empty;
    public string? Alias { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUsing(this);
    public override IAstNode Clone() => new UsingNode { Namespace = Namespace, Alias = Alias, Location = Location };
}

/// <summary>
/// Namespace node.
/// </summary>
public class NamespaceNode : AstNode
{
    public override AstNodeType NodeType => AstNodeType.Namespace;
    public string Name { get; set; } = string.Empty;
    public List<ContractNode> Contracts { get; } = new();

    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitNamespace(this);
    public override IAstNode Clone() => new NamespaceNode { Name = Name, Location = Location };
}

/// <summary>
/// Contract node.
/// </summary>
public class ContractNode : AstNode
{
    public override AstNodeType NodeType => AstNodeType.Contract;
    public string Name { get; set; } = string.Empty;
    public List<string> BaseContracts { get; } = new();
    public List<AttributeNode> Attributes { get; } = new();
    public List<FieldNode> Fields { get; } = new();
    public List<PropertyNode> Properties { get; } = new();
    public List<MethodNode> Methods { get; } = new();
    public List<EventNode> Events { get; } = new();
    public ConstructorNode? Constructor { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool IsAbstract { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitContract(this);
    public override IAstNode Clone() => new ContractNode { Name = Name, IsPublic = IsPublic, IsAbstract = IsAbstract, Location = Location };
}

/// <summary>
/// Field node.
/// </summary>
public class FieldNode : AstNode
{
    public override AstNodeType NodeType => AstNodeType.Field;
    public string Name { get; set; } = string.Empty;
    public TypeNode Type { get; set; } = null!;
    public ExpressionNode? Initializer { get; set; }
    public AccessModifier AccessModifier { get; set; } = AccessModifier.Private;
    public bool IsStatic { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsConst { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitField(this);
    public override IAstNode Clone() => new FieldNode
    {
        Name = Name,
        Type = (TypeNode)Type.Clone(),
        AccessModifier = AccessModifier,
        IsStatic = IsStatic,
        IsReadOnly = IsReadOnly,
        IsConst = IsConst,
        Location = Location
    };
}

/// <summary>
/// Method node.
/// </summary>
public class MethodNode : AstNode
{
    public override AstNodeType NodeType => AstNodeType.Method;
    public string Name { get; set; } = string.Empty;
    public TypeNode ReturnType { get; set; } = null!;
    public List<ParameterNode> Parameters { get; } = new();
    public BlockNode? Body { get; set; }
    public AccessModifier AccessModifier { get; set; } = AccessModifier.Public;
    public bool IsStatic { get; set; }
    public bool IsAbstract { get; set; }
    public bool IsVirtual { get; set; }
    public bool IsOverride { get; set; }
    public bool IsSafe { get; set; }
    public List<AttributeNode> Attributes { get; } = new();

    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMethod(this);
    public override IAstNode Clone() => new MethodNode
    {
        Name = Name,
        ReturnType = (TypeNode)ReturnType.Clone(),
        AccessModifier = AccessModifier,
        IsStatic = IsStatic,
        IsAbstract = IsAbstract,
        IsVirtual = IsVirtual,
        IsOverride = IsOverride,
        IsSafe = IsSafe,
        Location = Location
    };
}

/// <summary>
/// Access modifiers.
/// </summary>
public enum AccessModifier
{
    Private,
    Protected,
    Internal,
    Public
}

/// <summary>
/// Base class for type nodes.
/// </summary>
public abstract class TypeNode : AstNode
{
    public abstract string TypeName { get; }
}

/// <summary>
/// Base class for expression nodes.
/// </summary>
public abstract class ExpressionNode : AstNode
{
    public TypeNode? ResolvedType { get; set; }
    public bool IsConstant { get; set; }
    public object? ConstantValue { get; set; }
}

/// <summary>
/// Base class for statement nodes.
/// </summary>
public abstract class StatementNode : AstNode
{
}

/// <summary>
/// Identifier node.
/// </summary>
public class IdentifierNode : ExpressionNode
{
    public override AstNodeType NodeType => AstNodeType.Identifier;
    public string Name { get; set; } = string.Empty;

    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIdentifier(this);
    public override IAstNode Clone() => new IdentifierNode { Name = Name, Location = Location };
}

/// <summary>
/// Literal node.
/// </summary>
public class LiteralNode : ExpressionNode
{
    public override AstNodeType NodeType => AstNodeType.Literal;
    public LiteralType LiteralType { get; set; }
    public object? Value { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLiteral(this);
    public override IAstNode Clone() => new LiteralNode { LiteralType = LiteralType, Value = Value, Location = Location };
}

/// <summary>
/// Literal types.
/// </summary>
public enum LiteralType
{
    Integer,
    String,
    Boolean,
    ByteArray,
    Null
}

/// <summary>
/// Additional AST node implementations.
/// </summary>
public class PropertyNode : AstNode
{
    public override AstNodeType NodeType => AstNodeType.Property;
    public string Name { get; set; } = string.Empty;
    public TypeNode Type { get; set; } = null!;
    public AccessModifier AccessModifier { get; set; } = AccessModifier.Public;
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitProperty(this);
    public override IAstNode Clone() => new PropertyNode { Name = Name, Type = (TypeNode)Type.Clone(), AccessModifier = AccessModifier, Location = Location };
}

public class EventNode : AstNode
{
    public override AstNodeType NodeType => AstNodeType.Event;
    public string Name { get; set; } = string.Empty;
    public TypeNode Type { get; set; } = null!;
    public AccessModifier AccessModifier { get; set; } = AccessModifier.Public;
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitEvent(this);
    public override IAstNode Clone() => new EventNode { Name = Name, Type = (TypeNode)Type.Clone(), AccessModifier = AccessModifier, Location = Location };
}

public class ConstructorNode : AstNode
{
    public override AstNodeType NodeType => AstNodeType.Constructor;
    public List<ParameterNode> Parameters { get; } = new();
    public BlockNode? Body { get; set; }
    public AccessModifier AccessModifier { get; set; } = AccessModifier.Public;
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitConstructor(this);
    public override IAstNode Clone() => new ConstructorNode { AccessModifier = AccessModifier, Location = Location };
}

public class BlockNode : StatementNode
{
    public override AstNodeType NodeType => AstNodeType.Block;
    public List<StatementNode> Statements { get; } = new();
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBlock(this);
    public override IAstNode Clone() => new BlockNode { Location = Location };
}

public class ExpressionStatementNode : StatementNode
{
    public override AstNodeType NodeType => AstNodeType.ExpressionStatement;
    public ExpressionNode Expression { get; set; } = null!;
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitExpressionStatement(this);
    public override IAstNode Clone() => new ExpressionStatementNode { Expression = (ExpressionNode)Expression.Clone(), Location = Location };
}

public class IfStatementNode : StatementNode
{
    public override AstNodeType NodeType => AstNodeType.IfStatement;
    public ExpressionNode Condition { get; set; } = null!;
    public StatementNode ThenStatement { get; set; } = null!;
    public StatementNode? ElseStatement { get; set; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIfStatement(this);
    public override IAstNode Clone() => new IfStatementNode 
    { 
        Condition = (ExpressionNode)Condition.Clone(), 
        ThenStatement = (StatementNode)ThenStatement.Clone(),
        ElseStatement = ElseStatement?.Clone() as StatementNode,
        Location = Location 
    };
}

public class WhileStatementNode : StatementNode
{
    public override AstNodeType NodeType => AstNodeType.WhileStatement;
    public ExpressionNode Condition { get; set; } = null!;
    public StatementNode Body { get; set; } = null!;
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitWhileStatement(this);
    public override IAstNode Clone() => new WhileStatementNode 
    { 
        Condition = (ExpressionNode)Condition.Clone(), 
        Body = (StatementNode)Body.Clone(), 
        Location = Location 
    };
}

public class ForStatementNode : StatementNode
{
    public override AstNodeType NodeType => AstNodeType.ForStatement;
    public StatementNode? Initializer { get; set; }
    public ExpressionNode? Condition { get; set; }
    public ExpressionNode? Iterator { get; set; }
    public StatementNode Body { get; set; } = null!;
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitForStatement(this);
    public override IAstNode Clone() => new ForStatementNode 
    { 
        Initializer = Initializer?.Clone() as StatementNode,
        Condition = Condition?.Clone() as ExpressionNode,
        Iterator = Iterator?.Clone() as ExpressionNode,
        Body = (StatementNode)Body.Clone(), 
        Location = Location 
    };
}

public class ReturnStatementNode : StatementNode
{
    public override AstNodeType NodeType => AstNodeType.ReturnStatement;
    public ExpressionNode? Value { get; set; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitReturnStatement(this);
    public override IAstNode Clone() => new ReturnStatementNode 
    { 
        Value = Value?.Clone() as ExpressionNode, 
        Location = Location 
    };
}

public class VariableDeclarationNode : StatementNode
{
    public override AstNodeType NodeType => AstNodeType.VariableDeclaration;
    public TypeNode Type { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public ExpressionNode? Initializer { get; set; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitVariableDeclaration(this);
    public override IAstNode Clone() => new VariableDeclarationNode 
    { 
        Type = (TypeNode)Type.Clone(),
        Name = Name,
        Initializer = Initializer?.Clone() as ExpressionNode,
        Location = Location 
    };
}

public class BinaryExpressionNode : ExpressionNode
{
    public override AstNodeType NodeType => AstNodeType.BinaryExpression;
    public ExpressionNode Left { get; set; } = null!;
    public ExpressionNode Right { get; set; } = null!;
    public string Operator { get; set; } = string.Empty;
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBinaryExpression(this);
    public override IAstNode Clone() => new BinaryExpressionNode 
    { 
        Left = (ExpressionNode)Left.Clone(),
        Right = (ExpressionNode)Right.Clone(),
        Operator = Operator,
        Location = Location 
    };
}

public class UnaryExpressionNode : ExpressionNode
{
    public override AstNodeType NodeType => AstNodeType.UnaryExpression;
    public ExpressionNode Operand { get; set; } = null!;
    public string Operator { get; set; } = string.Empty;
    public bool IsPostfix { get; set; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUnaryExpression(this);
    public override IAstNode Clone() => new UnaryExpressionNode 
    { 
        Operand = (ExpressionNode)Operand.Clone(),
        Operator = Operator,
        IsPostfix = IsPostfix,
        Location = Location 
    };
}

public class MethodCallNode : ExpressionNode
{
    public override AstNodeType NodeType => AstNodeType.MethodCall;
    public ExpressionNode? Target { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public List<ExpressionNode> Arguments { get; } = new();
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMethodCall(this);
    public override IAstNode Clone() => new MethodCallNode 
    { 
        Target = Target?.Clone() as ExpressionNode,
        MethodName = MethodName,
        Location = Location 
    };
}

public class ParameterNode : AstNode
{
    public override AstNodeType NodeType => AstNodeType.Parameter;
    public TypeNode Type { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public ExpressionNode? DefaultValue { get; set; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitParameter(this);
    public override IAstNode Clone() => new ParameterNode 
    { 
        Type = (TypeNode)Type.Clone(),
        Name = Name,
        DefaultValue = DefaultValue?.Clone() as ExpressionNode,
        Location = Location 
    };
}

public class AttributeNode : AstNode
{
    public override AstNodeType NodeType => AstNodeType.Attribute;
    public string Name { get; set; } = string.Empty;
    public List<ExpressionNode> Arguments { get; } = new();
    public override T Accept<T>(IAstVisitor<T> visitor) => throw new NotImplementedException("AttributeNode visitor not implemented");
    public override IAstNode Clone() => new AttributeNode { Name = Name, Location = Location };
}
