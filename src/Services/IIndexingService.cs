using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.CodeAnalyzer.Models;

namespace Andy.CodeAnalyzer.Services;

/// <summary>
/// Service for indexing code files.
/// </summary>
public interface IIndexingService
{
    /// <summary>
    /// Event raised to report indexing progress.
    /// </summary>
    event EventHandler<IndexingProgressEventArgs>? IndexingProgress;

    /// <summary>
    /// Indexes a workspace.
    /// </summary>
    /// <param name="workspacePath">The workspace path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task IndexWorkspaceAsync(string workspacePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes a single file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task IndexFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a file change event.
    /// </summary>
    /// <param name="change">The file change event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task HandleFileChangeAsync(FileChangeEvent change, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets indexing statistics.
    /// </summary>
    /// <returns>The indexing statistics.</returns>
    Task<IndexingStatistics> GetStatisticsAsync();
}

/// <summary>
/// Statistics about the indexing process.
/// </summary>
public class IndexingStatistics
{
    /// <summary>
    /// Gets or sets the total files indexed.
    /// </summary>
    public int TotalFilesIndexed { get; set; }

    /// <summary>
    /// Gets or sets the total symbols extracted.
    /// </summary>
    public int TotalSymbolsExtracted { get; set; }

    /// <summary>
    /// Gets or sets the indexing duration.
    /// </summary>
    public TimeSpan IndexingDuration { get; set; }

    /// <summary>
    /// Gets or sets the last index time.
    /// </summary>
    public DateTime? LastIndexTime { get; set; }
}