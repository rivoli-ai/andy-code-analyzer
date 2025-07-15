# Python Analyzer Documentation

## Overview

The Python Analyzer provides code analysis capabilities for Python source files using regex-based pattern matching and heuristic analysis. It extracts symbols, analyzes code structure, and provides insights into Python codebases without requiring a Python runtime.

## How It Works

### 1. Architecture

The `PythonAnalyzer` implements the `ILanguageAnalyzer` interface and uses:

- **Regex Pattern Matching**: Carefully crafted regular expressions to identify Python constructs
- **Heuristic Analysis**: Smart detection of code patterns and conventions
- **Line-by-Line Processing**: Efficient parsing without external dependencies

### 2. Core Components

#### Regular Expression Patterns

```csharp
// Class detection - handles inheritance
private static readonly Regex ClassRegex = 
    new(@"^\s*class\s+(\w+)\s*(?:\((.*?)\))?\s*:", RegexOptions.Multiline);

// Function detection - supports async and type hints
private static readonly Regex FunctionRegex = 
    new(@"^\s*(?:async\s+)?def\s+(\w+)\s*\((.*?)\)\s*(?:->\s*(.*?))?\s*:", RegexOptions.Multiline);

// Import detection - handles 'from' imports and regular imports
private static readonly Regex ImportRegex = 
    new(@"^\s*(?:from\s+([\w.]+)\s+)?import\s+(.+)", RegexOptions.Multiline);
```

### 3. Analysis Process

#### Step 1: File Reading
```csharp
var content = await File.ReadAllTextAsync(filePath);
var lines = content.Split('\n');
```

#### Step 2: Import Extraction
The analyzer identifies and categorizes imports:
- Standard imports: `import os`
- From imports: `from datetime import datetime`
- Aliased imports: `import numpy as np`
- Multi-line imports with proper alias tracking

#### Step 3: Symbol Extraction
Symbols are extracted in order of appearance with proper categorization:

##### Classes
- Detects class definitions with inheritance
- Identifies base classes
- Extracts class-level docstrings
- Marks private classes (starting with `_`)

##### Functions and Methods
- Distinguishes between functions and methods (presence of `self`)
- Detects async functions
- Identifies special methods:
  - Instance methods (with `self`)
  - Class methods (with `cls` or `@classmethod`)
  - Static methods (with `@staticmethod`)
  - Magic methods (`__init__`, `__str__`, etc.)
- Extracts function signatures and return type hints

##### Variables and Constants
- Module-level variables
- Constants (UPPER_CASE convention)
- Type-annotated variables

### 4. Advanced Features

#### Docstring Extraction
```csharp
private string? ExtractDocstring(int startPosition, string content)
{
    var substring = content.Substring(startPosition);
    var match = DocstringRegex.Match(substring);
    
    if (match.Success && match.Index < 50) // Must be close to definition
    {
        return match.Groups[1].Value.Trim();
    }
    return null;
}
```

#### Decorator Detection
The analyzer looks backwards from function definitions to find decorators:
- `@property`
- `@staticmethod`
- `@classmethod`
- Custom decorators

#### Naming Convention Analysis
- Private members: `_private_method`
- Magic methods: `__init__`
- Constants: `CONSTANT_VALUE`
- Protected members: `_protected`

#### Metadata Collection
- **HasAsyncFunctions**: Detects async/await usage
- **HasTypeHints**: Identifies type annotations
- **LinesOfCode**: Total line count
- **HasTests**: Detects test files/functions
- **Complexity Metrics**: Basic cyclomatic complexity

### 5. Cyclomatic Complexity Calculation

The analyzer provides a simplified complexity metric:

```csharp
private int CalculateCyclomaticComplexity(string content, Symbol function)
{
    var complexity = 1; // Base complexity
    var decisionKeywords = new[] { 
        "if ", "elif ", "else:", "for ", "while ", 
        "except", "with ", " and ", " or " 
    };
    
    foreach (var keyword in decisionKeywords)
    {
        complexity += Regex.Matches(content, $@"\b{keyword}").Count;
    }
    
    return Math.Min(complexity, 20); // Cap for reasonable values
}
```

### 6. Symbol Location Tracking

Precise location tracking for all symbols:
```csharp
private Location GetLocationFromMatch(Match match, string[] lines)
{
    // Calculate line and column from character position
    var position = match.Index;
    var line = 1;
    var column = 1;
    // ... calculation logic
}
```

## Supported Python Features

### Currently Supported
- ✅ Classes with inheritance
- ✅ Functions and methods
- ✅ Async functions
- ✅ Import statements (all forms)
- ✅ Module-level variables and constants
- ✅ Docstrings (triple-quoted strings)
- ✅ Basic decorators
- ✅ Type hints (detection only)
- ✅ Private member detection

### Limitations
- ❌ Nested functions
- ❌ Lambda expressions
- ❌ Complex decorator chains
- ❌ Dynamic class creation
- ❌ Metaclasses
- ❌ Context managers (partial support)
- ❌ Generator expressions
- ❌ Proper scope resolution

## Usage Example

```csharp
var analyzer = new PythonAnalyzer(logger);

// Analyze a Python file
var structure = await analyzer.AnalyzeFileAsync("/path/to/module.py");

// Check for async code
if (structure.Metadata["HasAsyncFunctions"] is true)
{
    Console.WriteLine("File contains async functions");
}

// List all classes
var classes = structure.Symbols.Where(s => s.Kind == SymbolKind.Class);
foreach (var cls in classes)
{
    Console.WriteLine($"Class: {cls.Name}");
    if (cls.Documentation != null)
    {
        Console.WriteLine($"  Docstring: {cls.Documentation}");
    }
}

// Find test functions
var tests = structure.Symbols
    .Where(s => s.Kind == SymbolKind.Function && s.Name.StartsWith("test_"));
```

## Pattern Matching Details

### Class Pattern
```regex
^\s*class\s+(\w+)\s*(?:\((.*?)\))?\s*:
```
- Captures class name
- Optional inheritance list
- Handles any indentation

### Function Pattern
```regex
^\s*(?:async\s+)?def\s+(\w+)\s*\((.*?)\)\s*(?:->\s*(.*?))?\s*:
```
- Optional async keyword
- Function name
- Parameter list
- Optional return type annotation

### Import Pattern
```regex
^\s*(?:from\s+([\w.]+)\s+)?import\s+(.+)
```
- Optional 'from' module
- Import items (supports multiple)

## Integration with Code Analyzer

The Python Analyzer integrates with the framework:

1. **Auto-Registration**: Registered as `ILanguageAnalyzer` for `.py`, `.pyw`, `.pyi` files
2. **Unified Storage**: Symbols stored in same SQLite database
3. **Cross-Language Search**: Python symbols searchable alongside C# symbols
4. **Consistent API**: Same interface as other language analyzers

## Performance Characteristics

- **Fast Processing**: Regex-based approach is very fast
- **Low Memory**: No AST construction, minimal memory usage
- **Scalable**: Can handle large Python files efficiently
- **No Dependencies**: Doesn't require Python runtime or external libraries

## Future Enhancements

1. **AST-Based Analysis**: Integration with Python AST parser for accurate analysis
2. **Language Server Protocol**: Connection to Python LSP for semantic information
3. **Better Scope Resolution**: Proper indentation-based scope detection
4. **Import Resolution**: Track imported symbols and their usage
5. **Type Inference**: Basic type inference from assignments
6. **Docstring Parsing**: Structured parsing of docstring formats (Google, NumPy, etc.)
7. **Code Quality Metrics**: More sophisticated complexity metrics
8. **Virtual Environment Support**: Understanding of pip packages and dependencies

## Known Issues and Workarounds

### Issue: Nested Function Detection
Currently, nested functions are not properly associated with their parent scope.

**Workaround**: Functions are listed at module level, check indentation manually.

### Issue: Multi-line Statements
Multi-line function definitions or class definitions might not be fully captured.

**Workaround**: Ensure definitions are on a single line or use type stubs.

### Issue: Dynamic Code
Dynamically generated classes or functions are not detected.

**Workaround**: Use static analysis tools in conjunction with this analyzer.

## Best Practices for Analyzable Python Code

To get the best results from the analyzer:

1. **Use Type Hints**: Add type annotations to functions and variables
2. **Follow PEP 8**: Standard naming conventions help detection
3. **Add Docstrings**: Triple-quoted strings immediately after definitions
4. **Avoid Dynamic Patterns**: Stick to static class and function definitions
5. **Use Clear Imports**: Prefer explicit imports over wildcards