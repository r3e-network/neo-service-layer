using System;

namespace NeoServiceLayer.Services.SmartContracts.NeoN3.SyntaxAnalyzer.Lexer;

/// <summary>
/// Represents a token in Neo N3 smart contract code.
/// </summary>
public class Token
{
    /// <summary>
    /// Token type.
    /// </summary>
    public TokenType Type { get; set; }

    /// <summary>
    /// Token value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Source location.
    /// </summary>
    public SourceLocation Location { get; set; } = new();

    /// <summary>
    /// Leading trivia (comments, whitespace).
    /// </summary>
    public string LeadingTrivia { get; set; } = string.Empty;

    /// <summary>
    /// Trailing trivia.
    /// </summary>
    public string TrailingTrivia { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new token.
    /// </summary>
    public Token(TokenType type, string value, SourceLocation location)
    {
        Type = type;
        Value = value;
        Location = location;
    }

    public override string ToString() => $"{Type}: {Value}";
}

/// <summary>
/// Token types for Neo N3 smart contracts.
/// </summary>
public enum TokenType
{
    // Literals
    IntegerLiteral,
    StringLiteral,
    BooleanLiteral,
    ByteArrayLiteral,
    NullLiteral,

    // Identifiers
    Identifier,

    // Keywords
    Abstract,
    As,
    Base,
    Bool,
    Break,
    Byte,
    ByteArray,
    Case,
    Catch,
    Class,
    Const,
    Continue,
    Contract,
    Default,
    Do,
    Else,
    Enum,
    Event,
    False,
    Finally,
    For,
    Foreach,
    Get,
    If,
    In,
    Int,
    Interface,
    Internal,
    Is,
    Namespace,
    New,
    Null,
    Object,
    Override,
    Private,
    Protected,
    Public,
    ReadOnly,
    Return,
    Safe,
    Set,
    Static,
    String,
    Struct,
    Switch,
    This,
    Throw,
    True,
    Try,
    Uint,
    Ulong,
    Using,
    Var,
    Virtual,
    Void,
    While,

    // Neo-specific keywords
    Deploy,
    Runtime,
    Storage,
    Trigger,
    Oracle,
    Crypto,
    StdLib,
    Native,
    Ledger,
    Policy,
    RoleManagement,
    GAS,
    NEO,
    ContractManagement,

    // Operators
    Plus,           // +
    Minus,          // -
    Multiply,       // *
    Divide,         // /
    Modulo,         // %
    Equal,          // ==
    NotEqual,       // !=
    LessThan,       // <
    LessThanOrEqual,// <=
    GreaterThan,    // >
    GreaterThanOrEqual, // >=
    And,            // &&
    Or,             // ||
    Not,            // !
    BitwiseAnd,     // &
    BitwiseOr,      // |
    BitwiseXor,     // ^
    BitwiseNot,     // ~
    LeftShift,      // <<
    RightShift,     // >>
    Assign,         // =
    PlusAssign,     // +=
    MinusAssign,    // -=
    MultiplyAssign, // *=
    DivideAssign,   // /=
    ModuloAssign,   // %=
    AndAssign,      // &=
    OrAssign,       // |=
    XorAssign,      // ^=
    LeftShiftAssign,// <<=
    RightShiftAssign,// >>=
    Increment,      // ++
    Decrement,      // --
    Question,       // ?
    Colon,          // :
    NullCoalescing, // ??
    Lambda,         // =>

    // Delimiters
    LeftParen,      // (
    RightParen,     // )
    LeftBrace,      // {
    RightBrace,     // }
    LeftBracket,    // [
    RightBracket,   // ]
    Semicolon,      // ;
    Comma,          // ,
    Dot,            // .
    Arrow,          // ->

    // Special
    Eof,            // End of file
    Unknown,        // Unknown token
    Whitespace,     // Whitespace
    Comment,        // Comment
    NewLine,        // New line

    // Attributes
    Attribute,      // [...]

    // Preprocessor
    PreprocessorDirective,

    // Interpolated strings
    InterpolatedStringStart,
    InterpolatedStringMid,
    InterpolatedStringEnd,
    InterpolationStart,
    InterpolationEnd
}

/// <summary>
/// Token classification for syntax highlighting.
/// </summary>
public enum TokenClassification
{
    Keyword,
    Identifier,
    Literal,
    Operator,
    Delimiter,
    Comment,
    String,
    Number,
    Type,
    Attribute,
    Error
}

/// <summary>
/// Extensions for token types.
/// </summary>
public static class TokenTypeExtensions
{
    /// <summary>
    /// Gets the classification for a token type.
    /// </summary>
    public static TokenClassification GetClassification(this TokenType type)
    {
        return type switch
        {
            TokenType.IntegerLiteral => TokenClassification.Number,
            TokenType.StringLiteral => TokenClassification.String,
            TokenType.BooleanLiteral => TokenClassification.Keyword,
            TokenType.ByteArrayLiteral => TokenClassification.Literal,
            TokenType.NullLiteral => TokenClassification.Keyword,
            TokenType.Identifier => TokenClassification.Identifier,
            TokenType.Comment => TokenClassification.Comment,
            TokenType.Unknown => TokenClassification.Error,
            
            // Keywords
            TokenType t when IsKeyword(t) => TokenClassification.Keyword,
            
            // Operators
            TokenType t when IsOperator(t) => TokenClassification.Operator,
            
            // Delimiters
            TokenType t when IsDelimiter(t) => TokenClassification.Delimiter,
            
            _ => TokenClassification.Identifier
        };
    }

    /// <summary>
    /// Checks if token type is a keyword.
    /// </summary>
    public static bool IsKeyword(this TokenType type)
    {
        return type >= TokenType.Abstract && type <= TokenType.ContractManagement;
    }

    /// <summary>
    /// Checks if token type is an operator.
    /// </summary>
    public static bool IsOperator(this TokenType type)
    {
        return type >= TokenType.Plus && type <= TokenType.Lambda;
    }

    /// <summary>
    /// Checks if token type is a delimiter.
    /// </summary>
    public static bool IsDelimiter(this TokenType type)
    {
        return type >= TokenType.LeftParen && type <= TokenType.Arrow;
    }

    /// <summary>
    /// Checks if token type is a literal.
    /// </summary>
    public static bool IsLiteral(this TokenType type)
    {
        return type >= TokenType.IntegerLiteral && type <= TokenType.NullLiteral;
    }

    /// <summary>
    /// Gets the keyword string for a token type.
    /// </summary>
    public static string? GetKeywordString(this TokenType type)
    {
        return type switch
        {
            TokenType.Abstract => "abstract",
            TokenType.As => "as",
            TokenType.Base => "base",
            TokenType.Bool => "bool",
            TokenType.Break => "break",
            TokenType.Byte => "byte",
            TokenType.ByteArray => "ByteArray",
            TokenType.Case => "case",
            TokenType.Catch => "catch",
            TokenType.Class => "class",
            TokenType.Const => "const",
            TokenType.Continue => "continue",
            TokenType.Contract => "contract",
            TokenType.Default => "default",
            TokenType.Do => "do",
            TokenType.Else => "else",
            TokenType.Enum => "enum",
            TokenType.Event => "event",
            TokenType.False => "false",
            TokenType.Finally => "finally",
            TokenType.For => "for",
            TokenType.Foreach => "foreach",
            TokenType.Get => "get",
            TokenType.If => "if",
            TokenType.In => "in",
            TokenType.Int => "int",
            TokenType.Interface => "interface",
            TokenType.Internal => "internal",
            TokenType.Is => "is",
            TokenType.Namespace => "namespace",
            TokenType.New => "new",
            TokenType.Null => "null",
            TokenType.Object => "object",
            TokenType.Override => "override",
            TokenType.Private => "private",
            TokenType.Protected => "protected",
            TokenType.Public => "public",
            TokenType.ReadOnly => "readonly",
            TokenType.Return => "return",
            TokenType.Safe => "safe",
            TokenType.Set => "set",
            TokenType.Static => "static",
            TokenType.String => "string",
            TokenType.Struct => "struct",
            TokenType.Switch => "switch",
            TokenType.This => "this",
            TokenType.Throw => "throw",
            TokenType.True => "true",
            TokenType.Try => "try",
            TokenType.Uint => "uint",
            TokenType.Ulong => "ulong",
            TokenType.Using => "using",
            TokenType.Var => "var",
            TokenType.Virtual => "virtual",
            TokenType.Void => "void",
            TokenType.While => "while",
            _ => null
        };
    }
}