using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Andy.CodeAnalyzer.Services;

/// <summary>
/// Implementation of the code context provider.
/// </summary>
public class ContextProviderService : ICodeContextProvider
{
    private readonly ILogger<ContextProviderService> _logger;
    private readonly ICodeAnalyzerService _analyzerService;
    private readonly ISearchService _searchService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextProviderService"/> class.
    /// </summary>
    public ContextProviderService(
        ILogger<ContextProviderService> logger,
        ICodeAnalyzerService analyzerService,
        ISearchService searchService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _analyzerService = analyzerService ?? throw new ArgumentNullException(nameof(analyzerService));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
    }

    /// <inheritdoc/>
    public async Task<string> GetRelevantContextAsync(string query, int maxTokens, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting relevant context for query: {Query}", query);

        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("## Code Context");
        contextBuilder.AppendLine();

        // Search for relevant files
        var searchResults = await _searchService.SearchTextAsync(
            query,
            new Models.SearchOptions { MaxResults = 10 },
            cancellationToken);

        foreach (var result in searchResults)
        {
            contextBuilder.AppendLine($"### File: {result.FilePath}");
            contextBuilder.AppendLine($"```{result.Language}");
            contextBuilder.AppendLine(result.Snippet);
            contextBuilder.AppendLine("```");
            contextBuilder.AppendLine();

            // TODO: Check token count and stop if exceeded
        }

        return contextBuilder.ToString();
    }

    /// <inheritdoc/>
    public async Task<CodeMap> GenerateCodeMapAsync(string[] filePaths, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating code map for {Count} files", filePaths.Length);

        var codeMap = new CodeMap();

        foreach (var filePath in filePaths)
        {
            try
            {
                var structure = await _analyzerService.GetFileStructureAsync(filePath, cancellationToken);
                
                codeMap.Files[filePath] = new FileOverview
                {
                    Path = filePath,
                    Language = structure.Language,
                    MainSymbols = structure.Symbols
                        .Where(s => s.Kind == Models.SymbolKind.Class || 
                               s.Kind == Models.SymbolKind.Interface ||
                               s.Kind == Models.SymbolKind.Function)
                        .Select(s => s.Name)
                        .ToList(),
                    Imports = structure.Imports.Select(i => i.Name).ToList(),
                    Exports = structure.Exports.Select(e => e.Name).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate map for file: {FilePath}", filePath);
            }
        }

        // TODO: Generate dependencies and symbol references

        return codeMap;
    }

    /// <inheritdoc/>
    public Task<string> AnswerStructuralQueryAsync(string question, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Answering structural query: {Question}", question);

        // TODO: Implement structural query answering
        return Task.FromResult("Structural query answering not yet implemented.");
    }
}