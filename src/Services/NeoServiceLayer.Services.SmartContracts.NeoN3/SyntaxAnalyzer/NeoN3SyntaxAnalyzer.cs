using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.SmartContracts.NeoN3.SyntaxAnalyzer.Lexer;
using NeoServiceLayer.Services.SmartContracts.NeoN3.SyntaxAnalyzer.Optimization;
using NeoServiceLayer.Services.SmartContracts.NeoN3.SyntaxAnalyzer.Parser;
using NeoServiceLayer.Services.SmartContracts.NeoN3.SyntaxAnalyzer.Security;
using NeoServiceLayer.Services.SmartContracts.NeoN3.SyntaxAnalyzer.Semantic;
using NeoServiceLayer.ServiceFramework;
using System.ComponentModel.DataAnnotations;
using System;


namespace NeoServiceLayer.Services.SmartContracts.NeoN3.SyntaxAnalyzer;

/// <summary>
/// Professional Neo N3 smart contract syntax analyzer implementation.
/// </summary>
public class NeoN3SyntaxAnalyzer : INeoN3SyntaxAnalyzer
{
    private readonly ILogger<NeoN3SyntaxAnalyzer> Logger;
    private readonly INeoN3Parser _parser;
    private readonly ISemanticAnalyzer _semanticAnalyzer;
    private readonly ISecurityAnalyzer _securityAnalyzer;
    private readonly IOptimizationAnalyzer _optimizationAnalyzer;
    private readonly ICodeFormatter _codeFormatter;
    private readonly List<IValidationRule> _validationRules;

    public NeoN3SyntaxAnalyzer(
        ILogger<NeoN3SyntaxAnalyzer> logger,
        INeoN3Parser? parser = null,
        ISemanticAnalyzer? semanticAnalyzer = null,
        ISecurityAnalyzer? securityAnalyzer = null,
        IOptimizationAnalyzer? optimizationAnalyzer = null,
        ICodeFormatter? codeFormatter = null)
    {
        Logger = logger;
        _parser = parser ?? new NeoN3Parser();
        _semanticAnalyzer = semanticAnalyzer ?? new SemanticAnalyzer();
        _securityAnalyzer = securityAnalyzer ?? new SecurityAnalyzer();
        _optimizationAnalyzer = optimizationAnalyzer ?? new OptimizationAnalyzer();
        _codeFormatter = codeFormatter ?? new NeoN3CodeFormatter();
        _validationRules = InitializeDefaultValidationRules();
    }

    /// <inheritdoc/>
    public async Task<SyntaxAnalysisResult> AnalyzeAsync(
        string sourceCode,
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        options ??= new AnalysisOptions();

        Logger.LogInformation("Starting Neo N3 syntax analysis");

        var result = new SyntaxAnalysisResult();

        try
        {
            // Lexical analysis
            Logger.LogDebug("Performing lexical analysis");
            var lexer = new NeoN3Lexer(sourceCode);
            var tokens = lexer.Tokenize();

            cancellationToken.ThrowIfCancellationRequested();

            // Syntax analysis (parsing)
            Logger.LogDebug("Parsing tokens into AST");
            var parseResult = await _parser.ParseAsync(tokens, cancellationToken);

            if (!parseResult.Success)
            {
                result.Success = false;
                result.Errors.AddRange(parseResult.Errors);
                return result;
            }

            result.Ast = parseResult.Ast;
            cancellationToken.ThrowIfCancellationRequested();

            // Semantic analysis
            if (options.EnableSemanticAnalysis && parseResult.Ast != null)
            {
                Logger.LogDebug("Performing semantic analysis");
                var semanticResult = await _semanticAnalyzer.AnalyzeAsync(
                    parseResult.Ast,
                    cancellationToken);

                result.SymbolTable = semanticResult.SymbolTable;
                result.ControlFlowGraph = semanticResult.ControlFlowGraph;
                result.Errors.AddRange(semanticResult.Errors);
                result.Warnings.AddRange(semanticResult.Warnings);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Custom validation rules
            if (options.CustomRules.Any() && result.Ast != null)
            {
                Logger.LogDebug("Applying custom validation rules");
                var validationContext = CreateValidationContext(result);

                foreach (var rule in options.CustomRules.Concat(_validationRules))
                {
                    ApplyValidationRule(rule, result.Ast, validationContext, result);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Security analysis
            if (options.EnableSecurityAnalysis)
            {
                Logger.LogDebug("Performing security analysis");
                var securityResult = await AnalyzeSecurityAsync(sourceCode, cancellationToken);

                // Add security issues as errors/warnings
                foreach (var vulnerability in securityResult.Vulnerabilities)
                {
                    if (vulnerability.Severity >= SecuritySeverity.High)
                    {
                        result.Errors.Add(new SyntaxError
                        {
                            Code = $"SEC{vulnerability.Id}",
                            Message = vulnerability.Description,
                            Location = vulnerability.Location,
                            Severity = Severity.Error
                        });
                    }
                    else
                    {
                        result.Warnings.Add(new SyntaxWarning
                        {
                            Code = $"SEC{vulnerability.Id}",
                            Message = vulnerability.Description,
                            Location = vulnerability.Location
                        });
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Optimization analysis
            if (options.EnableOptimizationAnalysis)
            {
                Logger.LogDebug("Performing optimization analysis");
                var optimizationResult = await SuggestOptimizationsAsync(sourceCode, cancellationToken);

                // Add optimization suggestions as info messages
                foreach (var suggestion in optimizationResult.Suggestions)
                {
                    result.Info.Add(new SyntaxInfo
                    {
                        Code = $"OPT{suggestion.Type}",
                        Message = suggestion.Description,
                        Location = suggestion.Location
                    });
                }
            }

            // Calculate metrics
            if (result.Ast != null)
            {
                Logger.LogDebug("Calculating code metrics");
                result.Metrics = CalculateMetrics(result.Ast, sourceCode);
            }

            // Limit errors and warnings
            if (result.Errors.Count > options.MaxErrors)
            {
                result.Errors = result.Errors.Take(options.MaxErrors).ToList();
                result.Info.Add(new SyntaxInfo
                {
                    Code = "LIMIT",
                    Message = $"Error limit reached. Showing first {options.MaxErrors} errors."
                });
            }

            if (result.Warnings.Count > options.MaxWarnings)
            {
                result.Warnings = result.Warnings.Take(options.MaxWarnings).ToList();
                result.Info.Add(new SyntaxInfo
                {
                    Code = "LIMIT",
                    Message = $"Warning limit reached. Showing first {options.MaxWarnings} warnings."
                });
            }

            result.Success = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during syntax analysis");
            result.Success = false;
            result.Errors.Add(new SyntaxError
            {
                Code = "INTERNAL",
                Message = $"Internal analysis error: {ex.Message}",
                Severity = Severity.Error
            });
        }
        finally
        {
            stopwatch.Stop();
            result.AnalysisDurationMs = stopwatch.ElapsedMilliseconds;
            Logger.LogInformation("Syntax analysis completed in {Duration}ms", result.AnalysisDurationMs);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<BytecodeValidationResult> ValidateBytecodeAsync(
        byte[] bytecode,
        string manifest,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Validating Neo N3 bytecode");

        var result = new BytecodeValidationResult
        {
            ContractSize = bytecode.Length
        };

        try
        {
            // Validate bytecode structure
            var bytecodeErrors = ValidateBytecodeStructure(bytecode);
            result.Errors.AddRange(bytecodeErrors);

            // Validate manifest
            var manifestErrors = ValidateManifest(manifest);
            result.Errors.AddRange(manifestErrors);

            // Analyze stack usage
            result.StackUsage = AnalyzeStackUsage(bytecode);

            // Estimate gas consumption
            result.EstimatedGasConsumption = EstimateGasConsumption(bytecode);

            // Check size limits
            if (bytecode.Length > 64 * 1024) // 64KB limit
            {
                result.Errors.Add(new ValidationError
                {
                    Message = $"Contract size ({bytecode.Length} bytes) exceeds maximum allowed size (65536 bytes)",
                    ErrorType = ValidationErrorType.ContractTooLarge
                });
            }

            result.IsValid = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating bytecode");
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                Message = $"Validation error: {ex.Message}",
                ErrorType = ValidationErrorType.Other
            });
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<SecurityAnalysisResult> AnalyzeSecurityAsync(
        string sourceCode,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Performing security analysis");

        try
        {
            // Parse the code first
            var lexer = new NeoN3Lexer(sourceCode);
            var tokens = lexer.Tokenize();
            var parseResult = await _parser.ParseAsync(tokens, cancellationToken);

            if (!parseResult.Success || parseResult.Ast == null)
            {
                return new SecurityAnalysisResult
                {
                    SecurityScore = 0,
                    Vulnerabilities = new List<SecurityVulnerability>
                    {
                        new SecurityVulnerability
                        {
                            Id = "PARSE_ERROR",
                            Name = "Parse Error",
                            Description = "Unable to parse contract code for security analysis",
                            Severity = SecuritySeverity.Critical
                        }
                    }
                };
            }

            return await _securityAnalyzer.AnalyzeAsync(parseResult.Ast, sourceCode, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during security analysis");
            return new SecurityAnalysisResult
            {
                SecurityScore = 0,
                Vulnerabilities = new List<SecurityVulnerability>
                {
                    new SecurityVulnerability
                    {
                        Id = "ANALYSIS_ERROR",
                        Name = "Analysis Error",
                        Description = $"Security analysis error: {ex.Message}",
                        Severity = SecuritySeverity.Critical
                    }
                }
            };
        }
    }

    /// <inheritdoc/>
    public async Task<OptimizationResult> SuggestOptimizationsAsync(
        string sourceCode,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Analyzing code for optimization opportunities");

        try
        {
            // Parse the code first
            var lexer = new NeoN3Lexer(sourceCode);
            var tokens = lexer.Tokenize();
            var parseResult = await _parser.ParseAsync(tokens, cancellationToken);

            if (!parseResult.Success || parseResult.Ast == null)
            {
                return new OptimizationResult();
            }

            return await _optimizationAnalyzer.AnalyzeAsync(parseResult.Ast, sourceCode, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during optimization analysis");
            return new OptimizationResult();
        }
    }

    /// <inheritdoc/>
    public string FormatCode(string sourceCode, FormattingOptions? options = null)
    {
        Logger.LogDebug("Formatting Neo N3 code");

        try
        {
            return _codeFormatter.Format(sourceCode, options ?? new FormattingOptions());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error formatting code");
            return sourceCode; // Return original code if formatting fails
        }
    }

    /// <inheritdoc/>
    public IEnumerable<LanguageFeature> GetSupportedFeatures()
    {
        return new List<LanguageFeature>
        {
            new LanguageFeature
            {
                Name = "Smart Contracts",
                Description = "Full support for Neo N3 smart contract development",
                MinVersion = "N3"
            },
            new LanguageFeature
            {
                Name = "Events",
                Description = "Contract event declaration and emission",
                MinVersion = "N3"
            },
            new LanguageFeature
            {
                Name = "Storage",
                Description = "Persistent storage operations",
                MinVersion = "N3"
            },
            new LanguageFeature
            {
                Name = "Oracle",
                Description = "Oracle service integration",
                MinVersion = "N3"
            },
            new LanguageFeature
            {
                Name = "Native Contracts",
                Description = "Integration with Neo native contracts",
                MinVersion = "N3"
            },
            new LanguageFeature
            {
                Name = "Safe Methods",
                Description = "Safe method declarations for read-only operations",
                MinVersion = "N3"
            },
            new LanguageFeature
            {
                Name = "Contract Interfaces",
                Description = "Contract interface definitions and implementations",
                MinVersion = "N3"
            },
            new LanguageFeature
            {
                Name = "Manifest Attributes",
                Description = "Contract manifest configuration via attributes",
                MinVersion = "N3"
            }
        };
    }

    #region Private Methods

    private List<IValidationRule> InitializeDefaultValidationRules()
    {
        return new List<IValidationRule>
        {
            new NamingConventionRule(),
            new UnusedVariableRule(),
            new DeadCodeRule(),
            new ComplexityRule(),
            new SecurityPatternRule(),
            new GasOptimizationRule(),
            new StorageOptimizationRule(),
            new EventNamingRule(),
            new AccessControlRule(),
            new ReentrancyGuardRule()
        };
    }

    private IValidationContext CreateValidationContext(SyntaxAnalysisResult result)
    {
        return new ValidationContext(
            result.SymbolTable ?? new SymbolTable(),
            result.ControlFlowGraph ?? new ControlFlowGraph(),
            new Dictionary<string, object>());
    }

    private void ApplyValidationRule(
        IValidationRule rule,
        IAstNode ast,
        IValidationContext context,
        SyntaxAnalysisResult result)
    {
        try
        {
            var validationResult = rule.Validate(ast, context);

            foreach (var message in validationResult.Messages)
            {
                switch (message.Severity)
                {
                    case Severity.Error:
                        result.Errors.Add(new SyntaxError
                        {
                            Code = rule.Id,
                            Message = message.Message,
                            Location = message.Location,
                            Severity = message.Severity
                        });
                        break;
                    case Severity.Warning:
                        result.Warnings.Add(new SyntaxWarning
                        {
                            Code = rule.Id,
                            Message = message.Message,
                            Location = message.Location
                        });
                        break;
                    case Severity.Info:
                    case Severity.Hint:
                        result.Info.Add(new SyntaxInfo
                        {
                            Code = rule.Id,
                            Message = message.Message,
                            Location = message.Location
                        });
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error applying validation rule {RuleId}", rule.Id);
        }
    }

    private CodeMetrics CalculateMetrics(IAstNode ast, string sourceCode)
    {
        var metricsCalculator = new MetricsCalculator();
        return metricsCalculator.Calculate(ast, sourceCode);
    }

    private List<ValidationError> ValidateBytecodeStructure(byte[] bytecode)
    {
        var errors = new List<ValidationError>();
        var offset = 0;

        try
        {
            while (offset < bytecode.Length)
            {
                var opcode = bytecode[offset];

                // Validate opcode
                if (!IsValidOpCode(opcode))
                {
                    errors.Add(new ValidationError
                    {
                        Message = $"Invalid opcode: 0x{opcode:X2}",
                        Offset = offset,
                        OpCode = $"0x{opcode:X2}",
                        ErrorType = ValidationErrorType.InvalidOpCode
                    });
                }

                // Move to next instruction
                offset += GetInstructionSize(opcode, bytecode, offset);
            }
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError
            {
                Message = $"Error parsing bytecode: {ex.Message}",
                Offset = offset,
                ErrorType = ValidationErrorType.Other
            });
        }

        return errors;
    }

    private List<ValidationError> ValidateManifest(string manifest)
    {
        var errors = new List<ValidationError>();

        try
        {
            // Parse and validate manifest JSON
            var manifestObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(manifest);

            if (manifestObj == null)
            {
                errors.Add(new ValidationError
                {
                    Message = "Invalid manifest: null or empty",
                    ErrorType = ValidationErrorType.InvalidManifest
                });
                return errors;
            }

            // Validate required fields
            var requiredFields = new[] { "name", "abi", "permissions", "trusts", "supportedstandards" };
            foreach (var field in requiredFields)
            {
                if (!manifestObj.ContainsKey(field))
                {
                    errors.Add(new ValidationError
                    {
                        Message = $"Missing required manifest field: {field}",
                        ErrorType = ValidationErrorType.InvalidManifest
                    });
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError
            {
                Message = $"Error parsing manifest: {ex.Message}",
                ErrorType = ValidationErrorType.InvalidManifest
            });
        }

        return errors;
    }

    private StackUsageAnalysis AnalyzeStackUsage(byte[] bytecode)
    {
        // Simplified stack analysis
        return new StackUsageAnalysis
        {
            MaxStackDepth = EstimateMaxStackDepth(bytecode),
            AverageStackDepth = 5, // Placeholder
            StackDepthMap = new Dictionary<int, int>(),
            PotentialOverflowPoints = new List<int>(),
            PotentialUnderflowPoints = new List<int>()
        };
    }

    private long EstimateGasConsumption(byte[] bytecode)
    {
        // Simplified gas estimation
        long gasEstimate = 0;

        for (int i = 0; i < bytecode.Length; i++)
        {
            gasEstimate += GetOpCodeGasCost(bytecode[i]);
        }

        return gasEstimate;
    }

    private int EstimateMaxStackDepth(byte[] bytecode)
    {
        // Simplified estimation
        return Math.Min(16, bytecode.Length / 10);
    }

    private bool IsValidOpCode(byte opcode)
    {
        // Simplified validation - would check against Neo VM opcodes
        return opcode <= 0xFF;
    }

    private int GetInstructionSize(byte opcode, byte[] bytecode, int offset)
    {
        // Simplified - would need to handle variable-length instructions
        return 1;
    }

    private long GetOpCodeGasCost(byte opcode)
    {
        // Simplified - would map opcodes to their gas costs
        return 100;
    }

    #endregion
}
