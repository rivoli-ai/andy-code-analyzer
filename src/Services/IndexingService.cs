using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Andy.CodeAnalyzer.Analyzers;
using Andy.CodeAnalyzer.Models;
using Andy.CodeAnalyzer.Storage;
using Andy.CodeAnalyzer.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Andy.CodeAnalyzer.Services;

/// <summary>
/// Implementation of the indexing service.
/// </summary>
public class IndexingService : IIndexingService
{
    private readonly ILogger<IndexingService> _logger;
    private readonly IEnumerable<ILanguageAnalyzer> _analyzers;
    private readonly CodeAnalyzerDbContext _dbContext;
    private readonly CodeAnalyzerOptions _options;
    private readonly Dictionary<string, ILanguageAnalyzer> _analyzerMap;

    /// <inheritdoc/>
    public event EventHandler<IndexingProgressEventArgs>? IndexingProgress;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexingService"/> class.
    /// </summary>
    public IndexingService(
        ILogger<IndexingService> logger,
        IEnumerable<ILanguageAnalyzer> analyzers,
        CodeAnalyzerDbContext dbContext,
        IOptions<CodeAnalyzerOptions> options)
    {
        _logger = logger;
        _analyzers = analyzers;
        _dbContext = dbContext;
        _options = options.Value;
        
        // Build analyzer map for quick lookup
        _analyzerMap = new Dictionary<string, ILanguageAnalyzer>(StringComparer.OrdinalIgnoreCase);
        foreach (var analyzer in analyzers)
        {
            foreach (var ext in analyzer.SupportedExtensions)
            {
                _analyzerMap[ext] = analyzer;
            }
        }
    }

    /// <inheritdoc/>
    public async Task IndexWorkspaceAsync(string workspacePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting workspace indexing for: {WorkspacePath}", workspacePath);

        // Get all supported files
        var allFiles = Directory.GetFiles(workspacePath, "*.*", SearchOption.AllDirectories);
        _logger.LogDebug("Found {Count} total files in {Path}", allFiles.Length, workspacePath);
        
        var files = allFiles
            .Where(f => IsSupportedFile(f) && !IsIgnored(f))
            .ToList();

        _logger.LogInformation("Found {FileCount} files to index", files.Count);

        // Raise initial progress event
        IndexingProgress?.Invoke(this, new IndexingProgressEventArgs
        {
            TotalFiles = files.Count,
            ProcessedFiles = 0,
            CurrentFile = null,
            IsComplete = false
        });

        // Index files sequentially to avoid SQLite concurrency issues
        var indexed = 0;
        foreach (var file in files)
        {
            // Report current file being processed
            IndexingProgress?.Invoke(this, new IndexingProgressEventArgs
            {
                TotalFiles = files.Count,
                ProcessedFiles = indexed,
                CurrentFile = file,
                IsComplete = false
            });

            await IndexFileAsync(file, cancellationToken);
            indexed++;
            
            if (indexed % 10 == 0)
            {
                _logger.LogInformation("Indexed {Count}/{Total} files", indexed, files.Count);
            }
        }
        
        if (indexed % 10 != 0)
        {
            _logger.LogInformation("Indexed {Count}/{Total} files", indexed, files.Count);
        }

        // Report completion
        IndexingProgress?.Invoke(this, new IndexingProgressEventArgs
        {
            TotalFiles = files.Count,
            ProcessedFiles = indexed,
            CurrentFile = null,
            IsComplete = true
        });
    }

    /// <inheritdoc/>
    public async Task IndexFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Indexing file: {FilePath}", filePath);
        
        try
        {
            var extension = Path.GetExtension(filePath);
            if (!_analyzerMap.TryGetValue(extension, out var analyzer))
            {
                _logger.LogDebug("No analyzer found for file: {FilePath}", filePath);
                return;
            }

            // Get file info
            var fileInfo = new System.IO.FileInfo(filePath);
            var contentHash = await ComputeFileHashAsync(filePath, cancellationToken);

            // Check if file needs reindexing
            var existingFile = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Path == filePath, cancellationToken);

            if (existingFile != null && existingFile.ContentHash == contentHash)
            {
                _logger.LogDebug("File unchanged, skipping: {FilePath}", filePath);
                return;
            }

            // Analyze the file
            var structure = await analyzer.AnalyzeFileAsync(filePath, cancellationToken);

            // Update database
            await UpdateDatabaseAsync(filePath, fileInfo, contentHash, structure, cancellationToken);
            
            _logger.LogDebug("Successfully indexed: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index file: {FilePath}", filePath);
            throw; // Re-throw to make test failures visible
        }
    }

    /// <inheritdoc/>
    public Task HandleFileChangeAsync(FileChangeEvent change, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Handling file change: {ChangeType} for {Path}", change.ChangeType, change.Path);
        
        return change.ChangeType switch
        {
            FileChangeType.Created or FileChangeType.Modified => IndexFileAsync(change.Path, cancellationToken),
            FileChangeType.Deleted => RemoveFileFromIndexAsync(change.Path, cancellationToken),
            FileChangeType.Renamed => HandleRenameAsync(change, cancellationToken),
            _ => Task.CompletedTask
        };
    }

    /// <inheritdoc/>
    public async Task<IndexingStatistics> GetStatisticsAsync()
    {
        var stats = new IndexingStatistics();
        
        // Get file count
        stats.TotalFilesIndexed = await _dbContext.Files.CountAsync();
        
        // Get symbol count
        stats.TotalSymbolsExtracted = await _dbContext.Symbols.CountAsync();
        
        // Get last index time
        var lastFile = await _dbContext.Files
            .OrderByDescending(f => f.IndexedAt)
            .FirstOrDefaultAsync();
            
        if (lastFile != null)
        {
            stats.LastIndexTime = lastFile.IndexedAt;
        }
        
        // TODO: Track actual indexing duration
        stats.IndexingDuration = TimeSpan.Zero;
        
        return stats;
    }

    private Task RemoveFileFromIndexAsync(string filePath, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Removing file from index: {FilePath}", filePath);
        // TODO: Implement file removal
        return Task.CompletedTask;
    }

    private async Task HandleRenameAsync(FileChangeEvent change, CancellationToken cancellationToken)
    {
        if (change.OldPath != null)
        {
            await RemoveFileFromIndexAsync(change.OldPath, cancellationToken);
        }
        await IndexFileAsync(change.Path, cancellationToken);
    }

    private async Task UpdateDatabaseAsync(
        string filePath,
        System.IO.FileInfo fileInfo,
        string contentHash,
        CodeStructure structure,
        CancellationToken cancellationToken)
    {
        // Don't use transactions for now - SQLite has issues with parallel transactions
        try
        {
            // Get or create file entity
            var fileEntity = await _dbContext.Files
                .Include(f => f.Symbols)
                .FirstOrDefaultAsync(f => f.Path == filePath, cancellationToken);

            if (fileEntity == null)
            {
                fileEntity = new FileEntity { Path = filePath };
                _dbContext.Files.Add(fileEntity);
            }
            else
            {
                // Remove old symbols
                _dbContext.Symbols.RemoveRange(fileEntity.Symbols);
            }

            // Update file info
            fileEntity.Language = structure.Language;
            fileEntity.ContentHash = contentHash;
            fileEntity.LastModified = fileInfo.LastWriteTimeUtc;
            fileEntity.Size = fileInfo.Length;
            fileEntity.IndexedAt = DateTime.UtcNow;

            // Add symbols
            foreach (var symbol in structure.Symbols)
            {
                var symbolEntity = new SymbolEntity
                {
                    Name = symbol.Name,
                    Kind = symbol.Kind.ToString(),
                    StartLine = symbol.Location.StartLine,
                    StartColumn = symbol.Location.StartColumn,
                    EndLine = symbol.Location.EndLine,
                    EndColumn = symbol.Location.EndColumn,
                    Documentation = symbol.Documentation
                };

                // Find parent symbol if any
                if (!string.IsNullOrEmpty(symbol.ParentSymbol))
                {
                    var parentEntity = fileEntity.Symbols
                        .FirstOrDefault(s => s.Name == symbol.ParentSymbol);
                    if (parentEntity != null)
                    {
                        symbolEntity.ParentSymbol = parentEntity;
                    }
                }

                fileEntity.Symbols.Add(symbolEntity);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            
            // Update FTS index
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            await _dbContext.Database.ExecuteSqlRawAsync(
                "INSERT OR REPLACE INTO file_content (file_id, content) VALUES ({0}, {1})",
                fileEntity.Id, content);
        }
        catch
        {
            throw;
        }
    }

    private async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToBase64String(hash);
    }

    private bool IsSupportedFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return _analyzerMap.ContainsKey(extension);
    }

    private bool IsIgnored(string filePath)
    {
        var normalizedPath = filePath.Replace('\\', '/');
        var isIgnored = _options.IgnorePatterns.Any(pattern => 
            MatchesPattern(normalizedPath, pattern));
        
        if (isIgnored)
        {
            _logger.LogDebug("File {FilePath} is ignored", filePath);
        }
        
        return isIgnored;
    }

    private static bool MatchesPattern(string path, string pattern)
    {
        // Normalize paths
        path = path.Replace('\\', '/');
        pattern = pattern.Replace('\\', '/');
        
        // Check if the pattern contains a directory path (e.g., "**/bin/**")
        if (pattern.Contains("**/"))
        {
            // Extract the directory name from pattern like "**/bin/**"
            var parts = pattern.Split(new[] { "**/" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var dirPart = part.TrimEnd('/', '*');
                if (!string.IsNullOrEmpty(dirPart) && path.Contains($"/{dirPart}/"))
                {
                    return true;
                }
            }
        }
        
        // Simple glob pattern matching for other cases
        var regexPattern = pattern
            .Replace(".", "\\.")
            .Replace("**/", ".*")
            .Replace("*", "[^/]*")
            .Replace("?", ".");

        return System.Text.RegularExpressions.Regex.IsMatch(
            path,
            regexPattern,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}