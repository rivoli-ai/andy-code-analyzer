using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Andy.CodeAnalyzer.Services;

/// <summary>
/// Interface for providing code context to the AI assistant.
/// </summary>
public interface ICodeContextProvider
{
    /// <summary>
    /// Gets relevant code context for a query.
    /// </summary>
    /// <param name="query">The query to get context for.</param>
    /// <param name="maxTokens">The maximum number of tokens to include.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The relevant code context as a string.</returns>
    Task<string> GetRelevantContextAsync(string query, int maxTokens, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a code map for the specified files.
    /// </summary>
    /// <param name="filePaths">The file paths to include in the map.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated code map.</returns>
    Task<CodeMap> GenerateCodeMapAsync(string[] filePaths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers a structural query about the codebase.
    /// </summary>
    /// <param name="question">The question to answer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The answer to the question.</returns>
    Task<string> AnswerStructuralQueryAsync(string question, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a map of code structure.
/// </summary>
public class CodeMap
{
    /// <summary>
    /// Gets or sets the file overviews.
    /// </summary>
    public Dictionary<string, FileOverview> Files { get; set; } = new();

    /// <summary>
    /// Gets or sets the dependencies between files.
    /// </summary>
    public List<Dependency> Dependencies { get; set; } = new();

    /// <summary>
    /// Gets or sets the symbol references.
    /// </summary>
    public Dictionary<string, List<string>> SymbolReferences { get; set; } = new();
}

/// <summary>
/// Overview of a file.
/// </summary>
public class FileOverview
{
    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the language.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the main symbols.
    /// </summary>
    public List<string> MainSymbols { get; set; } = new();

    /// <summary>
    /// Gets or sets the imports.
    /// </summary>
    public List<string> Imports { get; set; } = new();

    /// <summary>
    /// Gets or sets the exports.
    /// </summary>
    public List<string> Exports { get; set; } = new();
}

/// <summary>
/// Represents a dependency between files.
/// </summary>
public class Dependency
{
    /// <summary>
    /// Gets or sets the source file.
    /// </summary>
    public string FromFile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target file.
    /// </summary>
    public string ToFile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the dependency type.
    /// </summary>
    public string Type { get; set; } = string.Empty;
}