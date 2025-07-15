using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Andy.CodeAnalyzer.Models;

namespace Andy.CodeAnalyzer.Analyzers;

/// <summary>
/// Interface for language-specific code analyzers.
/// </summary>
public interface ILanguageAnalyzer
{
    /// <summary>
    /// Gets the file extensions supported by this analyzer.
    /// </summary>
    string[] SupportedExtensions { get; }

    /// <summary>
    /// Gets the language identifier.
    /// </summary>
    string Language { get; }

    /// <summary>
    /// Analyzes a file and extracts its code structure.
    /// </summary>
    /// <param name="filePath">The file path to analyze.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The analyzed code structure.</returns>
    Task<CodeStructure> AnalyzeFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts symbols from the given content.
    /// </summary>
    /// <param name="content">The code content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The extracted symbols.</returns>
    Task<IEnumerable<Symbol>> ExtractSymbolsAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds references to a given symbol.
    /// </summary>
    /// <param name="symbol">The symbol to find references for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The found references.</returns>
    Task<IEnumerable<Reference>> FindReferencesAsync(Symbol symbol, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a reference to a symbol.
/// </summary>
public class Reference
{
    /// <summary>
    /// Gets or sets the file path containing the reference.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the location of the reference.
    /// </summary>
    public Location Location { get; set; } = new();

    /// <summary>
    /// Gets or sets the kind of reference.
    /// </summary>
    public ReferenceKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the context snippet.
    /// </summary>
    public string ContextSnippet { get; set; } = string.Empty;
}

/// <summary>
/// Types of references.
/// </summary>
public enum ReferenceKind
{
    /// <summary>
    /// A usage of the symbol.
    /// </summary>
    Usage,

    /// <summary>
    /// A definition of the symbol.
    /// </summary>
    Definition,

    /// <summary>
    /// An implementation of an interface or abstract member.
    /// </summary>
    Implementation,

    /// <summary>
    /// An override of a virtual member.
    /// </summary>
    Override,

    /// <summary>
    /// An inheritance relationship.
    /// </summary>
    Inheritance
}