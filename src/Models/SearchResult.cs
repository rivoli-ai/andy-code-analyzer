using System.Collections.Generic;

namespace Andy.CodeAnalyzer.Models;

/// <summary>
/// Represents a search result.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the snippet with highlighted matches.
    /// </summary>
    public string Snippet { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relevance score.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets the programming language.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the matched locations in the file.
    /// </summary>
    public List<Location> MatchLocations { get; set; } = new();
}