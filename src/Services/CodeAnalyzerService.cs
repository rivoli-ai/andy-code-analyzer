using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Andy.CodeAnalyzer.Analyzers;
using Andy.CodeAnalyzer.Models;
using Andy.CodeAnalyzer.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Andy.CodeAnalyzer.Services;

/// <summary>
/// Main implementation of the code analyzer service.
/// </summary>
public class CodeAnalyzerService : ICodeAnalyzerService
{
    private readonly ILogger<CodeAnalyzerService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly CodeAnalyzerOptions _options;
    private readonly IIndexingService _indexingService;
    private readonly ISearchService _searchService;
    private readonly CodeAnalyzerDbContext _dbContext;
    private readonly IEnumerable<ILanguageAnalyzer> _analyzers;
    private FileWatcherService? _fileWatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeAnalyzerService"/> class.
    /// </summary>
    public CodeAnalyzerService(
        ILogger<CodeAnalyzerService> logger,
        ILoggerFactory loggerFactory,
        IOptions<CodeAnalyzerOptions> options,
        IIndexingService indexingService,
        ISearchService searchService,
        CodeAnalyzerDbContext dbContext,
        IEnumerable<ILanguageAnalyzer> analyzers)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _options = options.Value;
        _indexingService = indexingService;
        _searchService = searchService;
        _dbContext = dbContext;
        _analyzers = analyzers;
    }

    /// <inheritdoc/>
    public event EventHandler<FileChangedEventArgs>? FileChanged;

    /// <inheritdoc/>
    public event EventHandler<IndexingProgressEventArgs>? IndexingProgress;

    /// <inheritdoc/>
    public async Task InitializeAsync(string workspacePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing code analyzer for workspace: {WorkspacePath}", workspacePath);

        // Subscribe to indexing progress events
        _indexingService.IndexingProgress += OnIndexingProgress;

        // Ensure database is created and FTS tables exist
        await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await _dbContext.CreateFtsTablesAsync();

        // Set up file watcher
        var watcherOptions = new FileWatcherOptions
        {
            IgnorePatterns = _options.IgnorePatterns,
            DebounceDelay = _options.DebounceDelay,
            IncludeSubdirectories = true
        };

        _fileWatcher = new FileWatcherService(
            workspacePath,
            watcherOptions,
            _loggerFactory.CreateLogger<FileWatcherService>());

        // Start watching for changes (but not during tests where we control indexing manually)
        if (_options.EnableFileWatcher)
        {
            _ = Task.Run(async () =>
            {
                await foreach (var change in _fileWatcher.GetChangesAsync(cancellationToken))
                {
                    await _indexingService.HandleFileChangeAsync(change, cancellationToken);
                    FileChanged?.Invoke(this, new FileChangedEventArgs { Change = change });
                }
            }, cancellationToken);
        }

        // Initial indexing if configured
        if (_options.IndexOnStartup)
        {
            await _indexingService.IndexWorkspaceAsync(workspacePath, cancellationToken);
        }

        _logger.LogInformation("Code analyzer initialized successfully");
    }

    /// <inheritdoc/>
    public Task ShutdownAsync()
    {
        _logger.LogInformation("Shutting down code analyzer");
        
        // Unsubscribe from events
        _indexingService.IndexingProgress -= OnIndexingProgress;
        
        _fileWatcher?.Dispose();
        return Task.CompletedTask;
    }

    private void OnIndexingProgress(object? sender, IndexingProgressEventArgs e)
    {
        // Forward the progress event
        IndexingProgress?.Invoke(this, e);
    }

    /// <inheritdoc/>
    public async Task<CodeStructure> GetFileStructureAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // Try to get from cache/database first
        var fileEntity = await _dbContext.Files
            .Include(f => f.Symbols)
            .FirstOrDefaultAsync(f => f.Path == filePath, cancellationToken);

        if (fileEntity != null)
        {
            // Convert from entity to model
            var structure = new CodeStructure
            {
                FilePath = filePath,
                Language = fileEntity.Language,
                AnalyzedAt = fileEntity.IndexedAt
            };

            // TODO: Populate symbols and imports from database
            // For now, re-analyze the file
        }

        // Re-analyze the file
        await _indexingService.IndexFileAsync(filePath, cancellationToken);
        
        // Get the analyzer
        var extension = Path.GetExtension(filePath);
        var analyzer = _analyzers.FirstOrDefault(a => a.SupportedExtensions.Contains(extension));
        
        if (analyzer == null)
        {
            throw new NotSupportedException($"No analyzer found for file extension: {extension}");
        }

        return await analyzer.AnalyzeFileAsync(filePath, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FileInfo>> GetFilesAsync(string pattern = "*", CancellationToken cancellationToken = default)
    {
        // Get all files with their info in a single query to avoid concurrent access
        var query = _dbContext.Files.AsQueryable();
        
        if (pattern != "*")
        {
            // Simple pattern matching - this could be improved
            var searchPattern = pattern.Replace("*", "%");
            query = query.Where(f => EF.Functions.Like(f.Path, searchPattern));
        }
        
        var fileEntities = await query.ToListAsync(cancellationToken);
        
        return fileEntities.Select(f => new FileInfo 
        { 
            Path = f.Path,
            Language = f.Language,
            LastModified = f.LastModified,
            Size = f.Size,
            IndexedAt = f.IndexedAt
        });
    }

    /// <inheritdoc/>
    public Task<IEnumerable<SearchResult>> SearchTextAsync(string query, SearchOptions options, CancellationToken cancellationToken = default)
    {
        return _searchService.SearchTextAsync(query, options, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Symbol>> SearchSymbolsAsync(string query, SymbolFilter filter, CancellationToken cancellationToken = default)
    {
        return _searchService.SearchSymbolsAsync(query, filter, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Reference>> FindReferencesAsync(string symbolName, CancellationToken cancellationToken = default)
    {
        return _searchService.FindReferencesAsync(symbolName, null, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<CodeContext> GetContextForLocationAsync(string filePath, int line, int column, CancellationToken cancellationToken = default)
    {
        // TODO: Implement context retrieval
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<string>> GetRelatedFilesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // TODO: Implement related files retrieval
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<AnalyzerStatistics> GetStatisticsAsync()
    {
        var indexingStats = await _indexingService.GetStatisticsAsync();
        
        return new AnalyzerStatistics
        {
            TotalFiles = indexingStats.TotalFilesIndexed,
            TotalSymbols = indexingStats.TotalSymbolsExtracted,
            IndexingTime = indexingStats.IndexingDuration,
            // TODO: Get other statistics
        };
    }
}