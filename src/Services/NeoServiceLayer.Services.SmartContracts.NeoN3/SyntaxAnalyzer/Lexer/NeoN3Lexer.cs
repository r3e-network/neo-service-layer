using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeoServiceLayer.Services.SmartContracts.NeoN3.SyntaxAnalyzer.Lexer;

/// <summary>
/// Lexer for Neo N3 smart contract code.
/// </summary>
public class NeoN3Lexer
{
    private readonly string _source;
    private int _position;
    private int _line = 1;
    private int _column = 1;
    private readonly List<Token> _tokens = new();
    private readonly Dictionary<string, TokenType> _keywords;

    public NeoN3Lexer(string source)
    {
        _source = source ?? string.Empty;
        _keywords = InitializeKeywords();
    }

    /// <summary>
    /// Tokenizes the source code.
    /// </summary>
    public List<Token> Tokenize()
    {
        _tokens.Clear();
        _position = 0;
        _line = 1;
        _column = 1;

        while (_position < _source.Length)
        {
            var leadingTrivia = ScanTrivia();

            if (_position >= _source.Length)
                break;

            var token = ScanToken();
            if (token != null)
            {
                token.LeadingTrivia = leadingTrivia;
                _tokens.Add(token);
            }
        }

        // Add EOF token
        _tokens.Add(new Token(TokenType.Eof, "", GetCurrentLocation()));

        return _tokens;
    }

    private Token? ScanToken()
    {
        var startLocation = GetCurrentLocation();
        var ch = Current();

        // Skip whitespace (should be handled by trivia, but just in case)
        if (char.IsWhiteSpace(ch))
        {
            Advance();
            return null;
        }

        // Numbers
        if (char.IsDigit(ch))
        {
            return ScanNumber(startLocation);
        }

        // Identifiers and keywords
        if (char.IsLetter(ch) || ch == '_')
        {
            return ScanIdentifierOrKeyword(startLocation);
        }

        // String literals
        if (ch == '"')
        {
            return ScanStringLiteral(startLocation);
        }

        // Character literals
        if (ch == '\'')
        {
            return ScanCharacterLiteral(startLocation);
        }

        // Operators and delimiters
        return ScanOperatorOrDelimiter(startLocation);
    }

    private Token ScanNumber(SourceLocation startLocation)
    {
        var sb = new StringBuilder();
        var hasDecimal = false;
        var isHex = false;

        // Check for hex prefix
        if (Current() == '0' && (Peek() == 'x' || Peek() == 'X'))
        {
            isHex = true;
            sb.Append(Current());
            Advance();
            sb.Append(Current());
            Advance();

            while (IsHexDigit(Current()))
            {
                sb.Append(Current());
                Advance();
            }
        }
        else
        {
            // Decimal number
            while (char.IsDigit(Current()))
            {
                sb.Append(Current());
                Advance();
            }

            // Check for decimal point
            if (Current() == '.' && char.IsDigit(Peek()))
            {
                hasDecimal = true;
                sb.Append(Current());
                Advance();

                while (char.IsDigit(Current()))
                {
                    sb.Append(Current());
                    Advance();
                }
            }

            // Check for scientific notation
            if (Current() == 'e' || Current() == 'E')
            {
                sb.Append(Current());
                Advance();

                if (Current() == '+' || Current() == '-')
                {
                    sb.Append(Current());
                    Advance();
                }

                while (char.IsDigit(Current()))
                {
                    sb.Append(Current());
                    Advance();
                }
            }
        }

        // Check for type suffix
        if (Current() == 'L' || Current() == 'l' ||
            Current() == 'U' || Current() == 'u' ||
            Current() == 'F' || Current() == 'f' ||
            Current() == 'D' || Current() == 'd' ||
            Current() == 'M' || Current() == 'm')
        {
            sb.Append(Current());
            Advance();

            // Handle UL or LU
            if ((sb[sb.Length - 1] == 'U' || sb[sb.Length - 1] == 'u') &&
                (Current() == 'L' || Current() == 'l'))
            {
                sb.Append(Current());
                Advance();
            }
        }

        startLocation.EndLine = _line;
        startLocation.EndColumn = _column - 1;
        startLocation.EndPosition = _position - 1;

        return new Token(TokenType.IntegerLiteral, sb.ToString(), startLocation);
    }

    private Token ScanIdentifierOrKeyword(SourceLocation startLocation)
    {
        var sb = new StringBuilder();

        while (char.IsLetterOrDigit(Current()) || Current() == '_')
        {
            sb.Append(Current());
            Advance();
        }

        var value = sb.ToString();
        var type = _keywords.TryGetValue(value, out var keywordType)
            ? keywordType
            : TokenType.Identifier;

        startLocation.EndLine = _line;
        startLocation.EndColumn = _column - 1;
        startLocation.EndPosition = _position - 1;

        return new Token(type, value, startLocation);
    }

    private Token ScanStringLiteral(SourceLocation startLocation)
    {
        var sb = new StringBuilder();
        Advance(); // Skip opening quote

        while (Current() != '"' && !IsAtEnd())
        {
            if (Current() == '\\')
            {
                Advance();
                var escaped = ScanEscapeSequence();
                sb.Append(escaped);
            }
            else
            {
                if (Current() == '\n')
                {
                    _line++;
                    _column = 1;
                }
                sb.Append(Current());
                Advance();
            }
        }

        if (Current() == '"')
        {
            Advance(); // Skip closing quote
        }

        startLocation.EndLine = _line;
        startLocation.EndColumn = _column - 1;
        startLocation.EndPosition = _position - 1;

        return new Token(TokenType.StringLiteral, sb.ToString(), startLocation);
    }

    private Token ScanCharacterLiteral(SourceLocation startLocation)
    {
        var sb = new StringBuilder();
        Advance(); // Skip opening quote

        if (Current() == '\\')
        {
            Advance();
            sb.Append(ScanEscapeSequence());
        }
        else if (Current() != '\'')
        {
            sb.Append(Current());
            Advance();
        }

        if (Current() == '\'')
        {
            Advance(); // Skip closing quote
        }

        startLocation.EndLine = _line;
        startLocation.EndColumn = _column - 1;
        startLocation.EndPosition = _position - 1;

        // Character literals are treated as integers in Neo
        return new Token(TokenType.IntegerLiteral, sb.ToString(), startLocation);
    }

    private string ScanEscapeSequence()
    {
        return Current() switch
        {
            'n' => "\n",
            'r' => "\r",
            't' => "\t",
            'b' => "\b",
            'f' => "\f",
            'v' => "\v",
            '0' => "\0",
            '\\' => "\\",
            '"' => "\"",
            '\'' => "'",
            'x' => ScanHexEscape(2),
            'u' => ScanHexEscape(4),
            'U' => ScanHexEscape(8),
            _ => Current().ToString()
        };
    }

    private string ScanHexEscape(int digits)
    {
        Advance(); // Skip x, u, or U
        var hex = new StringBuilder();

        for (int i = 0; i < digits && IsHexDigit(Current()); i++)
        {
            hex.Append(Current());
            Advance();
        }

        if (hex.Length > 0)
        {
            var value = Convert.ToInt32(hex.ToString(), 16);
            return char.ConvertFromUtf32(value);
        }

        return "";
    }

    private Token ScanOperatorOrDelimiter(SourceLocation startLocation)
    {
        var ch = Current();
        Advance();

        var type = ch switch
        {
            '+' => Current() == '+' ? (Advance(), TokenType.Increment) :
                   Current() == '=' ? (Advance(), TokenType.PlusAssign) : TokenType.Plus,
            '-' => Current() == '-' ? (Advance(), TokenType.Decrement) :
                   Current() == '=' ? (Advance(), TokenType.MinusAssign) :
                   Current() == '>' ? (Advance(), TokenType.Arrow) : TokenType.Minus,
            '*' => Current() == '=' ? (Advance(), TokenType.MultiplyAssign) : TokenType.Multiply,
            '/' => Current() == '=' ? (Advance(), TokenType.DivideAssign) : TokenType.Divide,
            '%' => Current() == '=' ? (Advance(), TokenType.ModuloAssign) : TokenType.Modulo,
            '=' => Current() == '=' ? (Advance(), TokenType.Equal) :
                   Current() == '>' ? (Advance(), TokenType.Lambda) : TokenType.Assign,
            '!' => Current() == '=' ? (Advance(), TokenType.NotEqual) : TokenType.Not,
            '<' => Current() == '=' ? (Advance(), TokenType.LessThanOrEqual) :
                   Current() == '<' ? (Advance(), Current() == '=' ? (Advance(), TokenType.LeftShiftAssign) : TokenType.LeftShift) : TokenType.LessThan,
            '>' => Current() == '=' ? (Advance(), TokenType.GreaterThanOrEqual) :
                   Current() == '>' ? (Advance(), Current() == '=' ? (Advance(), TokenType.RightShiftAssign) : TokenType.RightShift) : TokenType.GreaterThan,
            '&' => Current() == '&' ? (Advance(), TokenType.And) :
                   Current() == '=' ? (Advance(), TokenType.AndAssign) : TokenType.BitwiseAnd,
            '|' => Current() == '|' ? (Advance(), TokenType.Or) :
                   Current() == '=' ? (Advance(), TokenType.OrAssign) : TokenType.BitwiseOr,
            '^' => Current() == '=' ? (Advance(), TokenType.XorAssign) : TokenType.BitwiseXor,
            '~' => TokenType.BitwiseNot,
            '?' => Current() == '?' ? (Advance(), TokenType.NullCoalescing) : TokenType.Question,
            ':' => TokenType.Colon,
            '(' => TokenType.LeftParen,
            ')' => TokenType.RightParen,
            '{' => TokenType.LeftBrace,
            '}' => TokenType.RightBrace,
            '[' => TokenType.LeftBracket,
            ']' => TokenType.RightBracket,
            ';' => TokenType.Semicolon,
            ',' => TokenType.Comma,
            '.' => TokenType.Dot,
            _ => TokenType.Unknown
        };

        startLocation.EndLine = _line;
        startLocation.EndColumn = _column - 1;
        startLocation.EndPosition = _position - 1;

        return new Token(type, ch.ToString(), startLocation);
    }

    private string ScanTrivia()
    {
        var sb = new StringBuilder();

        while (!IsAtEnd())
        {
            if (char.IsWhiteSpace(Current()))
            {
                if (Current() == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else
                {
                    _column++;
                }
                sb.Append(Current());
                _position++;
            }
            else if (Current() == '/' && Peek() == '/')
            {
                // Single-line comment
                sb.Append(ScanSingleLineComment());
            }
            else if (Current() == '/' && Peek() == '*')
            {
                // Multi-line comment
                sb.Append(ScanMultiLineComment());
            }
            else
            {
                break;
            }
        }

        return sb.ToString();
    }

    private string ScanSingleLineComment()
    {
        var sb = new StringBuilder();

        while (Current() != '\n' && !IsAtEnd())
        {
            sb.Append(Current());
            Advance();
        }

        return sb.ToString();
    }

    private string ScanMultiLineComment()
    {
        var sb = new StringBuilder();
        sb.Append(Current()); // /
        Advance();
        sb.Append(Current()); // *
        Advance();

        while (!IsAtEnd())
        {
            if (Current() == '*' && Peek() == '/')
            {
                sb.Append(Current());
                Advance();
                sb.Append(Current());
                Advance();
                break;
            }

            if (Current() == '\n')
            {
                _line++;
                _column = 1;
            }

            sb.Append(Current());
            Advance();
        }

        return sb.ToString();
    }

    private SourceLocation GetCurrentLocation()
    {
        return new SourceLocation
        {
            StartLine = _line,
            StartColumn = _column,
            StartPosition = _position,
            EndLine = _line,
            EndColumn = _column,
            EndPosition = _position
        };
    }

    private char Current()
    {
        return _position < _source.Length ? _source[_position] : '\0';
    }

    private char Peek(int offset = 1)
    {
        var pos = _position + offset;
        return pos < _source.Length ? _source[pos] : '\0';
    }

    private void Advance()
    {
        if (_position < _source.Length)
        {
            _position++;
            _column++;
        }
    }

    private bool IsAtEnd()
    {
        return _position >= _source.Length;
    }

    private bool IsHexDigit(char ch)
    {
        return char.IsDigit(ch) || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
    }

    private Dictionary<string, TokenType> InitializeKeywords()
    {
        return new Dictionary<string, TokenType>
        {
            ["abstract"] = TokenType.Abstract,
            ["as"] = TokenType.As,
            ["base"] = TokenType.Base,
            ["bool"] = TokenType.Bool,
            ["break"] = TokenType.Break,
            ["byte"] = TokenType.Byte,
            ["ByteArray"] = TokenType.ByteArray,
            ["case"] = TokenType.Case,
            ["catch"] = TokenType.Catch,
            ["class"] = TokenType.Class,
            ["const"] = TokenType.Const,
            ["continue"] = TokenType.Continue,
            ["contract"] = TokenType.Contract,
            ["default"] = TokenType.Default,
            ["do"] = TokenType.Do,
            ["else"] = TokenType.Else,
            ["enum"] = TokenType.Enum,
            ["event"] = TokenType.Event,
            ["false"] = TokenType.False,
            ["finally"] = TokenType.Finally,
            ["for"] = TokenType.For,
            ["foreach"] = TokenType.Foreach,
            ["get"] = TokenType.Get,
            ["if"] = TokenType.If,
            ["in"] = TokenType.In,
            ["int"] = TokenType.Int,
            ["interface"] = TokenType.Interface,
            ["internal"] = TokenType.Internal,
            ["is"] = TokenType.Is,
            ["namespace"] = TokenType.Namespace,
            ["new"] = TokenType.New,
            ["null"] = TokenType.Null,
            ["object"] = TokenType.Object,
            ["override"] = TokenType.Override,
            ["private"] = TokenType.Private,
            ["protected"] = TokenType.Protected,
            ["public"] = TokenType.Public,
            ["readonly"] = TokenType.ReadOnly,
            ["return"] = TokenType.Return,
            ["safe"] = TokenType.Safe,
            ["set"] = TokenType.Set,
            ["static"] = TokenType.Static,
            ["string"] = TokenType.String,
            ["struct"] = TokenType.Struct,
            ["switch"] = TokenType.Switch,
            ["this"] = TokenType.This,
            ["throw"] = TokenType.Throw,
            ["true"] = TokenType.True,
            ["try"] = TokenType.Try,
            ["uint"] = TokenType.Uint,
            ["ulong"] = TokenType.Ulong,
            ["using"] = TokenType.Using,
            ["var"] = TokenType.Var,
            ["virtual"] = TokenType.Virtual,
            ["void"] = TokenType.Void,
            ["while"] = TokenType.While,

            // Neo-specific
            ["Deploy"] = TokenType.Deploy,
            ["Runtime"] = TokenType.Runtime,
            ["Storage"] = TokenType.Storage,
            ["Trigger"] = TokenType.Trigger,
            ["Oracle"] = TokenType.Oracle,
            ["Crypto"] = TokenType.Crypto,
            ["StdLib"] = TokenType.StdLib,
            ["Native"] = TokenType.Native,
            ["Ledger"] = TokenType.Ledger,
            ["Policy"] = TokenType.Policy,
            ["RoleManagement"] = TokenType.RoleManagement,
            ["GAS"] = TokenType.GAS,
            ["NEO"] = TokenType.NEO,
            ["ContractManagement"] = TokenType.ContractManagement
        };
    }
}
