# Andy.CodeAnalyzer Examples

This directory contains example applications demonstrating various features of the Andy.CodeAnalyzer library.

## Running the Examples

To run any of the examples, use the `dotnet run` command from the examples directory:

```bash
# From the examples directory
cd examples

# Run the C# analysis example
dotnet run -p Examples.csproj --property:StartupObject=Examples.CSharpAnalysisExample

# Run the Python analysis example  
dotnet run -p Examples.csproj --property:StartupObject=Examples.PythonAnalysisExample

# Run the events example
dotnet run -p Examples.csproj --property:StartupObject=Examples.EventsExample
```

Or from the project root:

```bash
# Run examples from the root directory
dotnet run --project examples/Examples.csproj --property:StartupObject=Examples.CSharpAnalysisExample
dotnet run --project examples/Examples.csproj --property:StartupObject=Examples.PythonAnalysisExample
dotnet run --project examples/Examples.csproj --property:StartupObject=Examples.EventsExample
```

## Examples Overview

### 1. CSharpAnalysisExample.cs
Demonstrates analyzing C# source files, including:
- Setting up the analyzer with dependency injection
- Analyzing individual C# files
- Searching for specific symbol types (classes, methods)
- Finding public async methods
- Retrieving file statistics and metadata

**Key Features Demonstrated:**
- Symbol extraction (classes, methods, properties)
- Documentation extraction
- Modifier detection
- Import/using statement analysis

### 2. PythonAnalysisExample.cs
Shows how to analyze Python source files, including:
- Creating and analyzing Python modules
- Searching for functions and methods
- Identifying class inheritance patterns
- Analyzing code complexity metrics
- Detecting code quality indicators (type hints, async functions)

**Key Features Demonstrated:**
- Python-specific constructs (decorators, docstrings)
- Complexity analysis
- Import statement parsing
- Metadata extraction

### 3. EventsExample.cs
Comprehensive example of event handling and real-time monitoring:
- Tracking indexing progress with visual feedback
- Monitoring file changes in real-time
- Building a live dashboard for code analysis
- Handling concurrent events safely

**Key Features Demonstrated:**
- `IndexingProgress` event handling
- `FileChanged` event handling
- Progress bar implementation
- Real-time statistics updates
- File system monitoring

## Running the Examples

### Prerequisites
- .NET 8.0 SDK or later
- Write access to create example files and databases

### Basic Setup
Each example is a standalone console application. To run an example:

1. Navigate to the examples directory:
   ```bash
   cd examples
   ```

2. Create a new console project for the example:
   ```bash
   dotnet new console -n ExampleRunner
   cd ExampleRunner
   ```

3. Add reference to Andy.CodeAnalyzer:
   ```bash
   dotnet add reference ../../src/Andy.CodeAnalyzer.csproj
   ```

4. Copy one of the example files as Program.cs:
   ```bash
   cp ../CSharpAnalysisExample.cs Program.cs
   ```

5. Run the example:
   ```bash
   dotnet run
   ```

### Alternative: Direct Compilation
You can also compile and run examples directly:

```bash
# For C# analysis example
dotnet build ../src/Andy.CodeAnalyzer.csproj
csc CSharpAnalysisExample.cs -r:../src/bin/Debug/net8.0/Andy.CodeAnalyzer.dll -r:System.Runtime.dll
./CSharpAnalysisExample.exe
```

## Example Output

### CSharpAnalysisExample Output
```
=== Analyzing a Single C# File ===
File: CSharpAnalysisExample.cs
Language: csharp
Symbols found: 8
  Namespace: Examples (line 10)
  Class: CSharpAnalysisExample (line 15)
    Doc: Example demonstrating how to analyze C# source files
  Method: static async Main (line 17)
  Method: static async AnalyzeSingleFile (line 56)
  ...

Imports: 7
  using System;
  using System.Linq;
  ...
```

### PythonAnalysisExample Output
```
=== Analyzing Python File ===
File: sample_python_module.py
Language: python
Total symbols: 15

Classes (2):
  BaseProcessor
    Doc: Base class for all processors
  DataProcessor
    Doc: Main data processor implementation

Functions (3):
  [async] async_fetch_data()
  calculate_metrics()
  main()
...
```

### EventsExample Output
```
=== Indexing Progress Example ===
[##########----------] 50% - StringUtils.cs
Indexed 3 files successfully!

Indexing Statistics:
  Total files: 3
  Total symbols: 12
  Time taken: 0.45 seconds

=== File Change Monitoring Example ===
[14:32:15] File Modified: test-project/Calculator.cs
  Updated symbols: 5
[14:32:16] File Added: test-project/MathHelpers.cs
...
```

## Customizing Examples

Feel free to modify these examples to:
- Analyze your own source files
- Add custom event handlers
- Implement different UI patterns
- Test specific language features
- Benchmark performance

## Tips

1. **Database Location**: Each example creates its own SQLite database. You can inspect these with any SQLite viewer.

2. **Logging**: Adjust the logging level in the examples to see more detailed information:
   ```csharp
   builder.SetMinimumLevel(LogLevel.Debug);
   ```

3. **File Watching**: The file watcher has a debounce delay to prevent multiple events for rapid changes. Adjust this in the options:
   ```csharp
   options.DebounceDelay = TimeSpan.FromMilliseconds(200);
   ```

4. **Memory Usage**: For large codebases, consider using the indexing in batches and monitoring memory usage through the statistics API.

## Troubleshooting

- **Permission Errors**: Ensure you have write access to create databases and example files
- **File Not Found**: Examples expect to be run from the examples directory
- **Build Errors**: Make sure to build the main project first: `dotnet build ../src/Andy.CodeAnalyzer.csproj`