using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Andy.CodeAnalyzer.Analyzers;
using Andy.CodeAnalyzer.Models;

namespace Andy.CodeAnalyzer.Services;

/// <summary>
/// Service for searching code.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Searches for text in the codebase.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="options">The search options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The search results.</returns>
    Task<IEnumerable<SearchResult>> SearchTextAsync(
        string query,
        SearchOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for symbols.
    /// </summary>
    /// <param name="query">The symbol query.</param>
    /// <param name="filter">The symbol filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching symbols.</returns>
    Task<IEnumerable<Symbol>> SearchSymbolsAsync(
        string query,
        SymbolFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds references to a symbol.
    /// </summary>
    /// <param name="symbolName">The symbol name.</param>
    /// <param name="filePath">Optional file path to search within.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The references.</returns>
    Task<IEnumerable<Reference>> FindReferencesAsync(
        string symbolName,
        string? filePath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets files that match a pattern.
    /// </summary>
    /// <param name="pattern">The file pattern.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching file paths.</returns>
    Task<IEnumerable<string>> GetFilesAsync(
        string pattern,
        CancellationToken cancellationToken = default);
}