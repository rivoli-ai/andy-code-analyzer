using System;

namespace Andy.CodeAnalyzer.Models;

/// <summary>
/// Configuration options for the code analyzer.
/// </summary>
public class CodeAnalyzerOptions
{
    /// <summary>
    /// Gets or sets the workspace path to analyze.
    /// </summary>
    public string WorkspacePath { get; set; } = Environment.CurrentDirectory;

    /// <summary>
    /// Gets or sets the maximum memory usage in bytes.
    /// </summary>
    public long MaxMemoryUsage { get; set; } = 512 * 1024 * 1024; // 512MB

    /// <summary>
    /// Gets or sets the maximum number of cached files.
    /// </summary>
    public int MaxCachedFiles { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the cache expiration time.
    /// </summary>
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets the file patterns to ignore.
    /// </summary>
    public string[] IgnorePatterns { get; set; } = new[]
    {
        "**/node_modules/**",
        "**/bin/**",
        "**/obj/**",
        "**/.git/**",
        "**/dist/**",
        "**/.vs/**",
        "**/.vscode/**",
        "**/.idea/**"
    };

    /// <summary>
    /// Gets or sets the enabled languages.
    /// </summary>
    public string[] EnabledLanguages { get; set; } = new[]
    {
        "csharp",
        "javascript",
        "typescript",
        "python",
        "java",
        "go"
    };

    /// <summary>
    /// Gets or sets whether to index on startup.
    /// </summary>
    public bool IndexOnStartup { get; set; } = true;

    /// <summary>
    /// Gets or sets the debounce delay for file changes.
    /// </summary>
    public TimeSpan DebounceDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets the database connection string.
    /// </summary>
    public string DatabaseConnectionString { get; set; } = "Data Source=codeanalyzer.db";

    /// <summary>
    /// Gets or sets whether file watching is enabled.
    /// </summary>
    public bool EnableFileWatcher { get; set; } = true;
}

/// <summary>
/// Options for file watching.
/// </summary>
public class FileWatcherOptions
{
    /// <summary>
    /// Gets or sets the patterns to ignore.
    /// </summary>
    public string[] IgnorePatterns { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the debounce delay.
    /// </summary>
    public TimeSpan DebounceDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets whether to include subdirectories.
    /// </summary>
    public bool IncludeSubdirectories { get; set; } = true;
}

/// <summary>
/// Options for search operations.
/// </summary>
public class SearchOptions
{
    /// <summary>
    /// Gets or sets the maximum number of results.
    /// </summary>
    public int MaxResults { get; set; } = 100;

    /// <summary>
    /// Gets or sets the offset for pagination.
    /// </summary>
    public int Offset { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether to use regex search.
    /// </summary>
    public bool UseRegex { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include documentation in search.
    /// </summary>
    public bool IncludeDocumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets file patterns to include.
    /// </summary>
    public string[]? IncludePatterns { get; set; }

    /// <summary>
    /// Gets or sets file patterns to exclude.
    /// </summary>
    public string[]? ExcludePatterns { get; set; }
}

/// <summary>
/// Filter options for symbol search.
/// </summary>
public class SymbolFilter
{
    /// <summary>
    /// Gets or sets the symbol kinds to include.
    /// </summary>
    public SymbolKind[]? Kinds { get; set; }

    /// <summary>
    /// Gets or sets the language to filter by.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the file path prefix to filter by.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of results.
    /// </summary>
    public int MaxResults { get; set; } = 50;
}