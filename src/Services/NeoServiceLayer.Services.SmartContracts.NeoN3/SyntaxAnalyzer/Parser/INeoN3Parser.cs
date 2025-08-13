using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeoServiceLayer.Services.SmartContracts.NeoN3.SyntaxAnalyzer.Lexer;

namespace NeoServiceLayer.Services.SmartContracts.NeoN3.SyntaxAnalyzer.Parser;

/// <summary>
/// Interface for Neo N3 parser.
/// </summary>
public interface INeoN3Parser
{
    /// <summary>
    /// Parses tokens into an abstract syntax tree.
    /// </summary>
    /// <param name="tokens">Token stream from lexer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parse result containing AST or errors.</returns>
    Task<ParseResult> ParseAsync(List<Token> tokens, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of parsing operation.
/// </summary>
public class ParseResult
{
    /// <summary>
    /// Whether parsing was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Abstract syntax tree if successful.
    /// </summary>
    public IAstNode? Ast { get; set; }

    /// <summary>
    /// Parse errors.
    /// </summary>
    public List<SyntaxError> Errors { get; set; } = new();

    /// <summary>
    /// Parse warnings.
    /// </summary>
    public List<SyntaxWarning> Warnings { get; set; } = new();
}