# Andy.CodeAnalyzer API Reference

## Table of Contents
- [Services](#services)
  - [ICodeAnalyzerService](#icodeanalyzerservice)
  - [IIndexingService](#iindexingservice)
  - [ISearchService](#isearchservice)
- [Events](#events)
  - [IndexingProgressEventArgs](#indexingprogresseventargs)
  - [FileChangedEventArgs](#filechangedeventargs)
- [Models](#models)
  - [CodeStructure](#codestructure)
  - [Symbol](#symbol)
  - [SearchResult](#searchresult)

## Services

### ICodeAnalyzerService

The main service interface for code analysis operations.

#### Methods

##### InitializeAsync
```csharp
Task InitializeAsync(string workspacePath, CancellationToken cancellationToken = default)
```
Initializes the analyzer for a workspace. This method:
- Creates the database if it doesn't exist
- Sets up file watching (if enabled)
- Subscribes to indexing progress events
- Performs initial indexing (if configured)

##### GetFileStructureAsync
```csharp
Task<CodeStructure> GetFileStructureAsync(string filePath, CancellationToken cancellationToken = default)
```
Analyzes and returns the structure of a specific file.

##### SearchTextAsync
```csharp
Task<IEnumerable<SearchResult>> SearchTextAsync(string query, SearchOptions options, CancellationToken cancellationToken = default)
```
Searches for text within the indexed codebase.

##### SearchSymbolsAsync
```csharp
Task<IEnumerable<Symbol>> SearchSymbolsAsync(string query, SymbolFilter filter, CancellationToken cancellationToken = default)
```
Searches for symbols (classes, methods, etc.) matching the query.

##### GetStatisticsAsync
```csharp
Task<AnalyzerStatistics> GetStatisticsAsync()
```
Returns statistics about the analyzer including file counts, symbol counts, and performance metrics.

#### Events

##### FileChanged
```csharp
event EventHandler<FileChangedEventArgs>? FileChanged
```
Raised when a file is added, modified, or deleted in the watched workspace.

##### IndexingProgress
```csharp
event EventHandler<IndexingProgressEventArgs>? IndexingProgress
```
Raised to report progress during indexing operations. This event provides detailed information about the indexing process.

### IIndexingService

Handles file indexing and database operations.

#### Methods

##### IndexWorkspaceAsync
```csharp
Task IndexWorkspaceAsync(string workspacePath, CancellationToken cancellationToken = default)
```
Indexes all supported files in a workspace. Raises `IndexingProgress` events during the operation.

##### IndexFileAsync
```csharp
Task IndexFileAsync(string filePath, CancellationToken cancellationToken = default)
```
Indexes a single file.

##### HandleFileChangeAsync
```csharp
Task HandleFileChangeAsync(FileChangeEvent change, CancellationToken cancellationToken = default)
```
Handles file system change events (add, modify, delete).

#### Events

##### IndexingProgress
```csharp
event EventHandler<IndexingProgressEventArgs>? IndexingProgress
```
Reports progress during indexing operations. This event is raised:
- At the start of indexing (with TotalFiles set and ProcessedFiles = 0)
- Before processing each file (with CurrentFile set)
- After indexing completion (with IsComplete = true)

### ISearchService

Provides search functionality for text and symbols.

#### Methods

##### SearchTextAsync
```csharp
Task<IEnumerable<SearchResult>> SearchTextAsync(string query, SearchOptions options, CancellationToken cancellationToken = default)
```
Performs text search with support for regular expressions and case sensitivity options.

##### SearchSymbolsAsync
```csharp
Task<IEnumerable<Symbol>> SearchSymbolsAsync(string query, SymbolFilter filter, CancellationToken cancellationToken = default)
```
Searches for symbols with filtering by kind (class, method, property, etc.).

## Events

### IndexingProgressEventArgs

Provides detailed information about indexing progress.

```csharp
public class IndexingProgressEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the total number of files to be indexed.
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of files that have been processed.
    /// </summary>
    public int ProcessedFiles { get; set; }

    /// <summary>
    /// Gets or sets the path of the file currently being processed.
    /// Null when indexing is starting or has completed.
    /// </summary>
    public string? CurrentFile { get; set; }

    /// <summary>
    /// Gets or sets whether the indexing operation has completed.
    /// </summary>
    public bool IsComplete { get; set; }
}
```

#### Usage Example
```csharp
analyzer.IndexingProgress += (sender, e) =>
{
    if (!e.IsComplete)
    {
        var progress = (double)e.ProcessedFiles / e.TotalFiles * 100;
        progressBar.Value = progress;
        
        if (e.CurrentFile != null)
        {
            statusLabel.Text = $"Processing: {Path.GetFileName(e.CurrentFile)}";
        }
    }
    else
    {
        statusLabel.Text = "Indexing complete!";
        progressBar.Value = 100;
    }
};
```

### FileChangedEventArgs

Contains information about file system changes.

```csharp
public class FileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the file change event details.
    /// </summary>
    public FileChangeEvent Change { get; set; }
}
```

## Models

### CodeStructure

Represents the analyzed structure of a code file.

```csharp
public class CodeStructure
{
    public string FilePath { get; set; }
    public string Language { get; set; }
    public List<Symbol> Symbols { get; set; }
    public List<Import> Imports { get; set; }
    public List<Export> Exports { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public DateTime AnalyzedAt { get; set; }
}
```

### Symbol

Represents a code symbol (class, method, property, etc.).

```csharp
public class Symbol
{
    public string Name { get; set; }
    public SymbolKind Kind { get; set; }
    public Location Location { get; set; }
    public string? ParentSymbol { get; set; }
    public List<string> Modifiers { get; set; }
    public string? Documentation { get; set; }
}
```

### SearchResult

Represents a search match with context.

```csharp
public class SearchResult
{
    public string FilePath { get; set; }
    public int LineNumber { get; set; }
    public int Column { get; set; }
    public string MatchedText { get; set; }
    public string LineContent { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

## Progress Reporting Best Practices

1. **Subscribe Early**: Subscribe to progress events before calling `InitializeAsync` to ensure you don't miss any events.

2. **Handle UI Updates**: If updating UI from progress events, ensure you marshal calls to the UI thread:
   ```csharp
   analyzer.IndexingProgress += (sender, e) =>
   {
       Application.Current.Dispatcher.Invoke(() =>
       {
           UpdateProgressUI(e);
       });
   };
   ```

3. **Progress Calculation**: Calculate percentage as `(ProcessedFiles / TotalFiles) * 100` for accurate progress.

4. **Cancellation Support**: Pass a CancellationToken to indexing operations for graceful cancellation:
   ```csharp
   var cts = new CancellationTokenSource();
   await analyzer.InitializeAsync(workspacePath, cts.Token);
   ```

5. **Error Handling**: Wrap event handlers in try-catch to prevent exceptions from breaking the indexing process:
   ```csharp
   analyzer.IndexingProgress += (sender, e) =>
   {
       try
       {
           UpdateProgress(e);
       }
       catch (Exception ex)
       {
           logger.LogError(ex, "Error updating progress");
       }
   };
   ```