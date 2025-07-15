using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Andy.CodeAnalyzer.Analyzers;
using Andy.CodeAnalyzer.Models;

namespace Andy.CodeAnalyzer.Services;

/// <summary>
/// Main interface for the code analyzer service.
/// </summary>
public interface ICodeAnalyzerService
{
    /// <summary>
    /// Initializes the analyzer for a workspace.
    /// </summary>
    /// <param name="workspacePath">The workspace path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task InitializeAsync(string workspacePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuts down the analyzer.
    /// </summary>
    Task ShutdownAsync();

    /// <summary>
    /// Gets the structure of a specific file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file structure.</returns>
    Task<CodeStructure> GetFileStructureAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all indexed files matching a pattern.
    /// </summary>
    /// <param name="pattern">The file pattern (default is "*").</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching files.</returns>
    Task<IEnumerable<FileInfo>> GetFilesAsync(string pattern = "*", CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for text in the codebase.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="options">The search options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The search results.</returns>
    Task<IEnumerable<SearchResult>> SearchTextAsync(string query, SearchOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for symbols in the codebase.
    /// </summary>
    /// <param name="query">The symbol query.</param>
    /// <param name="filter">The filter options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching symbols.</returns>
    Task<IEnumerable<Symbol>> SearchSymbolsAsync(string query, SymbolFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds references to a symbol.
    /// </summary>
    /// <param name="symbolName">The symbol name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The references.</returns>
    Task<IEnumerable<Reference>> FindReferencesAsync(string symbolName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the context for a specific location.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="line">The line number.</param>
    /// <param name="column">The column number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The code context.</returns>
    Task<CodeContext> GetContextForLocationAsync(string filePath, int line, int column, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets files related to a given file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The related file paths.</returns>
    Task<IEnumerable<string>> GetRelatedFilesAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about the analyzer.
    /// </summary>
    /// <returns>The analyzer statistics.</returns>
    Task<AnalyzerStatistics> GetStatisticsAsync();

    /// <summary>
    /// Event raised when a file changes.
    /// </summary>
    event EventHandler<FileChangedEventArgs>? FileChanged;

    /// <summary>
    /// Event raised to report indexing progress.
    /// </summary>
    event EventHandler<IndexingProgressEventArgs>? IndexingProgress;
}

/// <summary>
/// Represents file information.
/// </summary>
public class FileInfo
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
    /// Gets or sets the last modified time.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets or sets the file size.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets when the file was last indexed.
    /// </summary>
    public DateTime? IndexedAt { get; set; }
}

/// <summary>
/// Represents code context around a location.
/// </summary>
public class CodeContext
{
    /// <summary>
    /// Gets or sets the current symbol at the location.
    /// </summary>
    public Symbol? CurrentSymbol { get; set; }

    /// <summary>
    /// Gets or sets the parent symbols hierarchy.
    /// </summary>
    public List<Symbol> ParentSymbols { get; set; } = new();

    /// <summary>
    /// Gets or sets nearby symbols.
    /// </summary>
    public List<Symbol> NearbySymbols { get; set; } = new();

    /// <summary>
    /// Gets or sets the imports in scope.
    /// </summary>
    public List<Import> ImportsInScope { get; set; } = new();

    /// <summary>
    /// Gets or sets the code snippet around the location.
    /// </summary>
    public string CodeSnippet { get; set; } = string.Empty;
}

/// <summary>
/// Event args for file changes.
/// </summary>
public class FileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the file change event.
    /// </summary>
    public FileChangeEvent Change { get; set; } = new();
}

/// <summary>
/// Event args for indexing progress.
/// </summary>
public class IndexingProgressEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the total number of files.
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of processed files.
    /// </summary>
    public int ProcessedFiles { get; set; }

    /// <summary>
    /// Gets or sets the current file being processed.
    /// </summary>
    public string? CurrentFile { get; set; }

    /// <summary>
    /// Gets or sets whether indexing is complete.
    /// </summary>
    public bool IsComplete { get; set; }
}

/// <summary>
/// Statistics about the analyzer.
/// </summary>
public class AnalyzerStatistics
{
    /// <summary>
    /// Gets or sets the total number of indexed files.
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Gets or sets the total number of symbols.
    /// </summary>
    public int TotalSymbols { get; set; }

    /// <summary>
    /// Gets or sets the memory usage in bytes.
    /// </summary>
    public long MemoryUsage { get; set; }

    /// <summary>
    /// Gets or sets the indexing time.
    /// </summary>
    public TimeSpan IndexingTime { get; set; }

    /// <summary>
    /// Gets or sets the database size in bytes.
    /// </summary>
    public long DatabaseSize { get; set; }

    /// <summary>
    /// Gets or sets the language distribution.
    /// </summary>
    public Dictionary<string, int> LanguageDistribution { get; set; } = new();
}