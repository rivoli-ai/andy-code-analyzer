# Andy.CodeAnalyzer

A code analysis library for the Andy AI assistant that provides real-time code indexing, searching, and context understanding capabilities.

## Features

- **Multi-Language Support**: Built-in analyzers for C# and Python with extensible architecture for additional languages
- **Real-Time Indexing**: Automatic file watching and incremental indexing of code changes
- **Advanced Search**: Full-text and symbol-based search with support for regex patterns
- **Code Structure Analysis**: Extracts classes, methods, properties, and other symbols from source files
- **Context Awareness**: Provides code context including parent symbols, nearby symbols, and imports
- **SQLite Storage**: Efficient local storage using Entity Framework Core with SQLite
- **Async Operations**: Fully asynchronous API for non-blocking operations
- **Event-Driven**: File change notifications and indexing progress events

## Installation

Add the Andy.CodeAnalyzer package to your .NET project:

```bash
dotnet add package Andy.CodeAnalyzer
```

## Quick Start

```csharp
using Andy.CodeAnalyzer.Extensions;
using Microsoft.Extensions.DependencyInjection;

// Configure services
var services = new ServiceCollection();
services.AddCodeAnalyzer(options =>
{
    options.DatabaseConnectionString = "Data Source=codeanalyzer.db";
    options.EnableFileWatching = true;
    options.MaxConcurrentIndexing = 4;
});

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Get the analyzer service
var analyzer = serviceProvider.GetRequiredService<ICodeAnalyzerService>();

// Subscribe to progress events
analyzer.IndexingProgress += (sender, e) =>
{
    Console.WriteLine($"Indexing progress: {e.ProcessedFiles}/{e.TotalFiles} files");
    if (e.CurrentFile != null)
    {
        Console.WriteLine($"Processing: {e.CurrentFile}");
    }
    if (e.IsComplete)
    {
        Console.WriteLine("Indexing complete!");
    }
};

// Initialize for a workspace
await analyzer.InitializeAsync("/path/to/your/project");

// Search for symbols
var symbols = await analyzer.SearchSymbolsAsync("MyClass", new SymbolFilter 
{ 
    Kind = SymbolKind.Class 
});

// Get file structure
var structure = await analyzer.GetFileStructureAsync("/path/to/file.cs");

// Search text
var results = await analyzer.SearchTextAsync("TODO", new SearchOptions 
{ 
    UseRegex = false,
    CaseSensitive = false 
});
```

## Architecture

### Core Components

- **Language Analyzers**: Pluggable analyzers for different programming languages
  - `CSharpAnalyzer`: Uses Roslyn for C# code analysis
  - `PythonAnalyzer`: Python code analysis support
  - Extensible via `ILanguageAnalyzer` interface

- **Services**:
  - `CodeAnalyzerService`: Main orchestrator service
  - `IndexingService`: Handles file indexing and database operations with progress reporting
  - `SearchService`: Provides search functionality
  - `ContextProviderService`: Generates code context information
  - `FileWatcherService`: Monitors file system changes

- **Storage**:
  - Entity Framework Core with SQLite
  - Stores file metadata, symbols, and dependencies
  - Optimized for fast retrieval and search operations

### Data Models

- **CodeStructure**: Represents analyzed file structure with symbols, imports, and exports
- **Symbol**: Code elements (classes, methods, properties, etc.) with location and metadata
- **SearchResult**: Search match results with context
- **FileChangeEvent**: File system change notifications

## Configuration

```csharp
services.AddCodeAnalyzer(options =>
{
    // Database connection string
    options.DatabaseConnectionString = "Data Source=codeanalyzer.db";
    
    // Enable/disable file watching
    options.EnableFileWatching = true;
    
    // Enable/disable initial indexing on startup
    options.IndexOnStartup = true;
    
    // File patterns to include/exclude
    options.IncludePatterns = new[] { "**/*.cs", "**/*.py" };
    options.ExcludePatterns = new[] { "**/bin/**", "**/obj/**", "**/node_modules/**" };
    
    // Performance tuning
    options.MaxConcurrentIndexing = 4;
    options.IndexingBatchSize = 100;
    options.CacheSize = 1000;
});
```

## API Reference

For detailed API documentation, see [API Reference](docs/api-reference.md).

### ICodeAnalyzerService

The main service interface providing:

- `InitializeAsync(workspacePath)`: Initialize analyzer for a workspace
- `GetFileStructureAsync(filePath)`: Get analyzed structure of a file
- `SearchTextAsync(query, options)`: Search for text in codebase
- `SearchSymbolsAsync(query, filter)`: Search for symbols
- `FindReferencesAsync(symbolName)`: Find all references to a symbol
- `GetContextForLocationAsync(file, line, column)`: Get code context at location
- `GetRelatedFilesAsync(filePath)`: Find related files
- `GetStatisticsAsync()`: Get analyzer statistics

### Events

#### FileChanged
Raised when a file is added, modified, or deleted in the watched workspace.

```csharp
analyzer.FileChanged += (sender, e) =>
{
    Console.WriteLine($"File {e.Change.ChangeType}: {e.Change.FilePath}");
};
```

#### IndexingProgress
Reports detailed progress during indexing operations, including:
- Total number of files to be indexed
- Number of files processed so far
- Current file being processed
- Completion status

```csharp
analyzer.IndexingProgress += (sender, e) =>
{
    var percentage = (e.ProcessedFiles * 100.0) / e.TotalFiles;
    Console.WriteLine($"Progress: {percentage:F1}% ({e.ProcessedFiles}/{e.TotalFiles})");
    
    if (e.CurrentFile != null)
    {
        Console.WriteLine($"Indexing: {Path.GetFileName(e.CurrentFile)}");
    }
    
    if (e.IsComplete)
    {
        Console.WriteLine("Indexing completed successfully!");
    }
};
```

## Requirements

- .NET 8.0 or later
- SQLite support
- Write access for database file

## Dependencies

- Microsoft.CodeAnalysis.CSharp (4.12.0) - For C# code analysis
- Microsoft.EntityFrameworkCore.Sqlite (8.0.x) - For data storage
- Microsoft.Extensions.DependencyInjection (8.0.x) - For dependency injection
- System.Threading.Channels (8.0.x) - For async operations

## Documentation

- [API Reference](docs/api-reference.md) - Detailed API documentation
- [C# Analyzer](docs/csharp-analyzer.md) - How the C# analyzer works
- [Python Analyzer](docs/python-analyzer.md) - How the Python analyzer works
- [Examples](examples/README.md) - Sample applications demonstrating features

## Contributing

Contributions are welcome! To add support for a new language:

1. Implement the `ILanguageAnalyzer` interface
2. Register your analyzer in `ServiceCollectionExtensions`
3. Add appropriate tests

## License

This project is part of the Andy AI assistant and follows the same license terms.