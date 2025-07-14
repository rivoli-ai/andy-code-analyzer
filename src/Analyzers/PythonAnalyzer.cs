using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Andy.CodeAnalyzer.Models;

namespace Andy.CodeAnalyzer.Analyzers;

/// <summary>
/// Python language analyzer using AST parsing.
/// </summary>
public class PythonAnalyzer : ILanguageAnalyzer
{
    private readonly ILogger<PythonAnalyzer> _logger;
    
    // Regex patterns for Python constructs
    private static readonly Regex ClassRegex = new(@"^\s*class\s+(\w+)\s*(?:\((.*?)\))?\s*:", RegexOptions.Multiline);
    private static readonly Regex FunctionRegex = new(@"^\s*(?:async\s+)?def\s+(\w+)\s*\((.*?)\)\s*(?:->\s*(.*?))?\s*:", RegexOptions.Multiline);
    private static readonly Regex ImportRegex = new(@"^\s*(?:from\s+([\w.]+)\s+)?import\s+(.+)", RegexOptions.Multiline);
    private static readonly Regex VariableRegex = new(@"^\s*(\w+)\s*[:=]\s*(.+)", RegexOptions.Multiline);
    private static readonly Regex DecoratorRegex = new(@"^\s*@(\w+)(?:\((.*?)\))?\s*$", RegexOptions.Multiline);
    private static readonly Regex DocstringRegex = new(@"^\s*['""\""]{3}([\s\S]*?)['""\""]{3}", RegexOptions.Multiline);

    /// <summary>
    /// Initializes a new instance of the <see cref="PythonAnalyzer"/> class.
    /// </summary>
    public PythonAnalyzer(ILogger<PythonAnalyzer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public string[] SupportedExtensions => new[] { ".py", ".pyw", ".pyi" };

    /// <inheritdoc/>
    public string Language => "python";

    /// <inheritdoc/>
    public async Task<CodeStructure> AnalyzeFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Analyzing Python file: {FilePath}", filePath);

        try
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var lines = content.Split('\n');
            
            var structure = new CodeStructure
            {
                FilePath = filePath,
                Language = Language,
                AnalyzedAt = DateTime.UtcNow
            };

            // Extract imports
            ExtractImports(content, structure);
            
            // Extract symbols
            await ExtractSymbolsFromContent(content, lines, structure, cancellationToken);
            
            // Add metadata
            structure.Metadata["HasAsyncFunctions"] = content.Contains("async def");
            structure.Metadata["HasTypeHints"] = content.Contains("->") || content.Contains(": ");
            structure.Metadata["LinesOfCode"] = lines.Length;
            structure.Metadata["HasTests"] = content.Contains("def test_") || content.Contains("class Test");
            
            // Calculate cyclomatic complexity for functions
            CalculateComplexityMetrics(content, structure);

            _logger.LogDebug("Extracted {SymbolCount} symbols from {FilePath}", structure.Symbols.Count, filePath);
            return structure;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing Python file: {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Symbol>> ExtractSymbolsAsync(string content, CancellationToken cancellationToken = default)
    {
        var structure = new CodeStructure();
        var lines = content.Split('\n');
        await ExtractSymbolsFromContent(content, lines, structure, cancellationToken);
        return structure.Symbols;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Reference>> FindReferencesAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        // TODO: Implement reference finding using regex or integrate with Python Language Server
        _logger.LogWarning("FindReferencesAsync not yet implemented for Python");
        return Task.FromResult<IEnumerable<Reference>>(new List<Reference>());
    }

    private void ExtractImports(string content, CodeStructure structure)
    {
        var importMatches = ImportRegex.Matches(content);
        foreach (Match match in importMatches)
        {
            var fromModule = match.Groups[1].Value;
            var importItems = match.Groups[2].Value.Split(',').Select(s => s.Trim());

            foreach (var item in importItems)
            {
                var parts = item.Split(" as ");
                var name = parts[0].Trim();
                var alias = parts.Length > 1 ? parts[1].Trim() : null;

                structure.Imports.Add(new Import
                {
                    Name = string.IsNullOrEmpty(fromModule) ? name : $"{fromModule}.{name}",
                    Alias = alias
                });
            }
        }
    }

    private async Task ExtractSymbolsFromContent(string content, string[] lines, CodeStructure structure, CancellationToken cancellationToken)
    {
        var currentIndentLevel = new Stack<(int level, string? parent)>();
        currentIndentLevel.Push((0, null));

        // Extract classes
        var classMatches = ClassRegex.Matches(content);
        foreach (Match match in classMatches)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var className = match.Groups[1].Value;
            var baseClasses = match.Groups[2].Value;
            var location = GetLocationFromMatch(match, lines);
            
            var classSymbol = new Symbol
            {
                Name = className,
                Kind = SymbolKind.Class,
                Location = location,
                ParentSymbol = GetParentSymbol(match.Index, content, structure.Symbols),
                Documentation = ExtractDocstring(match.Index + match.Length, content)
            };

            // Add modifiers based on naming convention
            if (className.StartsWith("_"))
            {
                classSymbol.Modifiers.Add("private");
            }

            structure.Symbols.Add(classSymbol);
        }

        // Extract functions/methods
        var functionMatches = FunctionRegex.Matches(content);
        foreach (Match match in functionMatches)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var functionName = match.Groups[1].Value;
            var parameters = match.Groups[2].Value;
            var returnType = match.Groups[3].Value;
            var location = GetLocationFromMatch(match, lines);
            var isAsync = match.Value.Contains("async");

            var functionSymbol = new Symbol
            {
                Name = functionName,
                Kind = SymbolKind.Function,
                Location = location,
                ParentSymbol = GetParentSymbol(match.Index, content, structure.Symbols),
                Documentation = ExtractDocstring(match.Index + match.Length, content)
            };

            // Add modifiers
            if (isAsync)
            {
                functionSymbol.Modifiers.Add("async");
            }
            if (functionName.StartsWith("_"))
            {
                functionSymbol.Modifiers.Add("private");
            }
            if (functionName.StartsWith("__") && functionName.EndsWith("__"))
            {
                functionSymbol.Modifiers.Add("magic");
            }
            if (parameters.Contains("self"))
            {
                functionSymbol.Kind = SymbolKind.Method;
            }
            else if (parameters.Contains("cls"))
            {
                functionSymbol.Modifiers.Add("classmethod");
            }
            else if (GetDecorators(match.Index, content).Any(d => d == "staticmethod"))
            {
                functionSymbol.Modifiers.Add("staticmethod");
            }

            structure.Symbols.Add(functionSymbol);
        }

        // Extract module-level variables/constants
        var variableMatches = VariableRegex.Matches(content);
        foreach (Match match in variableMatches)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var varName = match.Groups[1].Value;
            var location = GetLocationFromMatch(match, lines);
            
            // Skip if inside a function or class
            if (GetParentSymbol(match.Index, content, structure.Symbols) != null)
                continue;

            var varSymbol = new Symbol
            {
                Name = varName,
                Kind = varName.ToUpper() == varName ? SymbolKind.Constant : SymbolKind.Field,
                Location = location
            };

            if (varName.StartsWith("_"))
            {
                varSymbol.Modifiers.Add("private");
            }

            structure.Symbols.Add(varSymbol);
        }

        await Task.CompletedTask;
    }

    private Location GetLocationFromMatch(Match match, string[] lines)
    {
        var position = match.Index;
        var line = 1;
        var column = 1;
        var currentPos = 0;

        foreach (var lineContent in lines)
        {
            if (currentPos + lineContent.Length >= position)
            {
                column = position - currentPos + 1;
                break;
            }
            currentPos += lineContent.Length + 1; // +1 for newline
            line++;
        }

        return new Location
        {
            StartLine = line,
            StartColumn = column,
            EndLine = line,
            EndColumn = column + match.Length
        };
    }

    private string? GetParentSymbol(int position, string content, List<Symbol> symbols)
    {
        // Find the nearest class or function that contains this position
        var candidateParents = symbols
            .Where(s => s.Kind == SymbolKind.Class || s.Kind == SymbolKind.Function)
            .OrderBy(s => s.Location.StartLine)
            .ToList();

        // Simple heuristic: check indentation level
        var lines = content.Split('\n');
        var currentLine = 0;
        var currentPos = 0;
        
        foreach (var line in lines)
        {
            if (currentPos >= position) break;
            currentLine++;
            currentPos += line.Length + 1;
        }

        // TODO: Implement proper scope detection based on indentation
        return null;
    }

    private string? ExtractDocstring(int startPosition, string content)
    {
        if (startPosition >= content.Length) return null;
        
        var substring = content.Substring(startPosition);
        var match = DocstringRegex.Match(substring);
        
        if (match.Success && match.Index < 50) // Docstring should be close to the definition
        {
            return match.Groups[1].Value.Trim();
        }

        return null;
    }

    private List<string> GetDecorators(int position, string content)
    {
        var decorators = new List<string>();
        var lines = content.Substring(0, position).Split('\n');
        
        // Look backwards for decorators
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            var decoratorMatch = DecoratorRegex.Match(line);
            if (decoratorMatch.Success)
            {
                decorators.Add(decoratorMatch.Groups[1].Value);
            }
            else
            {
                break; // Stop when we hit a non-decorator line
            }
        }

        return decorators;
    }

    private void CalculateComplexityMetrics(string content, CodeStructure structure)
    {
        foreach (var function in structure.Symbols.Where(s => s.Kind == SymbolKind.Function || s.Kind == SymbolKind.Method))
        {
            var complexity = CalculateCyclomaticComplexity(content, function);
            structure.Metadata[$"{function.Name}_complexity"] = complexity;
        }
    }

    private int CalculateCyclomaticComplexity(string content, Symbol function)
    {
        // Simple cyclomatic complexity calculation
        // Count decision points: if, elif, else, for, while, except, with, and, or
        var complexity = 1; // Base complexity
        
        var decisionKeywords = new[] { "if ", "elif ", "else:", "for ", "while ", "except", "with ", " and ", " or " };
        
        // TODO: Extract just the function body based on indentation
        // For now, use a simple approximation
        foreach (var keyword in decisionKeywords)
        {
            complexity += Regex.Matches(content, $@"\b{keyword}", RegexOptions.IgnoreCase).Count;
        }

        return Math.Min(complexity, 20); // Cap at 20 for reasonable values
    }
}