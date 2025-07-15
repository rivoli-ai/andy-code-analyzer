using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Andy.CodeAnalyzer.Analyzers;
using Andy.CodeAnalyzer.Models;
using Andy.CodeAnalyzer.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Andy.CodeAnalyzer.Services;

/// <summary>
/// Implementation of the search service.
/// </summary>
public class SearchService : ISearchService
{
    private readonly ILogger<SearchService> _logger;
    private readonly CodeAnalyzerDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchService"/> class.
    /// </summary>
    public SearchService(ILogger<SearchService> logger, CodeAnalyzerDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SearchResult>> SearchTextAsync(
        string query,
        SearchOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching for text: {Query}", query);

        // Use FTS5 to search content
        var sql = @"
            SELECT f.Id, f.Path, snippet(file_content, 1, '<mark>', '</mark>', '...', 30) as Snippet
            FROM file_content fc
            JOIN Files f ON fc.file_id = f.Id
            WHERE file_content MATCH {0}
            LIMIT {1}";

        var results = await _dbContext.Database
            .SqlQueryRaw<SearchResultDto>(sql, query, options.MaxResults)
            .ToListAsync(cancellationToken);

        return results.Select(r => new SearchResult
        {
            FilePath = r.Path,
            Snippet = r.Snippet,
            Score = 1.0f // FTS5 doesn't expose scores by default
        });
    }

    private class SearchResultDto
    {
        public int Id { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Symbol>> SearchSymbolsAsync(
        string query,
        SymbolFilter filter,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching for symbols: {Query}", query);

        var queryBuilder = _dbContext.Symbols
            .Include(s => s.File)
            .AsQueryable();

        // Apply name filter
        if (!string.IsNullOrEmpty(query) && query != "*")
        {
            queryBuilder = queryBuilder.Where(s => EF.Functions.Like(s.Name, $"%{query}%"));
        }

        // Apply kind filter
        if (filter.Kinds?.Any() == true)
        {
            var kindStrings = filter.Kinds.Select(k => k.ToString());
            queryBuilder = queryBuilder.Where(s => kindStrings.Contains(s.Kind));
        }

        // Apply language filter
        if (!string.IsNullOrEmpty(filter.Language))
        {
            queryBuilder = queryBuilder.Where(s => s.File.Language == filter.Language);
        }

        // Apply file path filter
        if (!string.IsNullOrEmpty(filter.FilePath))
        {
            queryBuilder = queryBuilder.Where(s => s.File.Path.StartsWith(filter.FilePath));
        }

        var entities = await queryBuilder
            .Take(filter.MaxResults)
            .ToListAsync(cancellationToken);

        // Convert entities to models
        return entities.Select(e => new Symbol
        {
            Name = e.Name,
            Kind = Enum.Parse<SymbolKind>(e.Kind),
            Location = new Location
            {
                StartLine = e.StartLine,
                StartColumn = e.StartColumn,
                EndLine = e.EndLine,
                EndColumn = e.EndColumn
            },
            Documentation = e.Documentation
        });
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Reference>> FindReferencesAsync(
        string symbolName,
        string? filePath = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Finding references for: {SymbolName}", symbolName);

        // TODO: Implement reference finding
        return Task.FromResult(Enumerable.Empty<Reference>());
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetFilesAsync(
        string pattern,
        CancellationToken cancellationToken = default)
    {
        var files = await _dbContext.Files
            .Select(f => f.Path)
            .ToListAsync(cancellationToken);

        if (pattern != "*")
        {
            // TODO: Implement pattern matching
            files = files.Where(f => MatchesPattern(f, pattern)).ToList();
        }

        return files;
    }

    private static bool MatchesPattern(string path, string pattern)
    {
        // Simple pattern matching implementation
        // TODO: Implement proper glob pattern matching
        return path.Contains(pattern.Replace("*", ""));
    }
}