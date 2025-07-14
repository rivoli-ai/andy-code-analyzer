# C# Analyzer Documentation

## Overview

The C# Analyzer is a sophisticated code analysis component that leverages Microsoft's Roslyn compiler platform to provide deep semantic analysis of C# source code. It extracts symbols, analyzes code structure, and provides rich metadata about C# files.

## How It Works

### 1. Architecture

The `CSharpAnalyzer` implements the `ILanguageAnalyzer` interface and uses the following key components:

- **Roslyn Compiler Platform**: The analyzer uses Roslyn's syntax trees and semantic models for accurate code analysis
- **AdhocWorkspace**: Creates a lightweight workspace for analyzing individual files without requiring a full project context
- **CSharpCompilationOptions**: Configures compilation settings for analysis

### 2. Analysis Process

#### Step 1: File Reading and Parsing
```csharp
var text = await File.ReadAllTextAsync(filePath);
var sourceText = SourceText.From(text);
var syntaxTree = CSharpSyntaxTree.ParseText(sourceText);
```

The analyzer reads the source file and parses it into a syntax tree using Roslyn's `CSharpSyntaxTree`.

#### Step 2: Compilation Creation
```csharp
var compilation = CSharpCompilation.Create("Analysis")
    .AddSyntaxTrees(syntaxTree)
    .AddReferences(GetBasicReferences())
    .WithOptions(_compilationOptions);
```

A minimal compilation unit is created with basic .NET references to enable semantic analysis.

#### Step 3: Semantic Model Generation
```csharp
var semanticModel = compilation.GetSemanticModel(syntaxTree);
```

The semantic model provides type information, symbol resolution, and other semantic insights.

#### Step 4: Symbol Extraction

The analyzer uses a custom `SymbolExtractorVisitor` that walks the syntax tree and extracts various symbol types:

- **Namespaces**: Both traditional and file-scoped
- **Types**: Classes, interfaces, structs, enums
- **Members**: Methods, properties, fields, constructors
- **Modifiers**: Access levels, static, async, etc.

### 3. Symbol Extraction Details

The `SymbolExtractorVisitor` extends `CSharpSyntaxWalker` and overrides visit methods for each symbol type:

```csharp
public override void VisitClassDeclaration(ClassDeclarationSyntax node)
{
    var symbol = CreateSymbol(node, SymbolKind.Class, node.Identifier.Text);
    AddModifiers(symbol, node.Modifiers);
    symbol.Documentation = GetDocumentation(node);
    _symbols.Add(symbol);
    _symbolStack.Push(symbol);
    base.VisitClassDeclaration(node);
    _symbolStack.Pop();
}
```

Key features:
- **Hierarchical Structure**: Uses a stack to track parent-child relationships
- **Location Tracking**: Records precise line and column positions
- **Documentation Extraction**: Parses XML documentation comments
- **Modifier Detection**: Captures all access modifiers and keywords

### 4. Features Extracted

#### Symbols
- Classes, interfaces, structs, enums
- Methods (including constructors)
- Properties and fields
- Nested type support

#### Metadata
- **HasAsync**: Detects if file contains async methods
- **TargetFramework**: Currently returns default (would read from project file)
- **LangVersion**: Language version used

#### Documentation
- Extracts XML documentation comments
- Focuses on `<summary>` tags
- Preserves documentation for IntelliSense

#### Imports
- Captures all `using` directives
- Supports aliased imports

### 5. Advanced Features

#### XML Documentation Parsing
```csharp
private static string ExtractDocumentationText(DocumentationCommentTriviaSyntax docComment)
{
    var summaryElement = docComment.Content
        .OfType<XmlElementSyntax>()
        .FirstOrDefault(e => e.StartTag.Name.ToString() == "summary");
    // Extract and clean text...
}
```

#### Reference Resolution
The analyzer includes basic .NET references to ensure proper type resolution:
- System.Runtime.dll
- System.Collections.dll
- System.Linq.dll
- System.Threading.Tasks.dll

### 6. Performance Considerations

- **Lazy Loading**: Syntax trees are parsed on-demand
- **Minimal Compilation**: Only creates compilation units when needed
- **Cancellation Support**: All async operations support cancellation tokens
- **Memory Efficiency**: Uses `AdhocWorkspace` for lightweight analysis

## Usage Example

```csharp
var analyzer = new CSharpAnalyzer(logger);

// Analyze a single file
var structure = await analyzer.AnalyzeFileAsync("/path/to/MyClass.cs");

// Access extracted symbols
foreach (var symbol in structure.Symbols)
{
    Console.WriteLine($"{symbol.Kind}: {symbol.Name} at line {symbol.Location.StartLine}");
}

// Check metadata
if (structure.Metadata["HasAsync"] is true)
{
    Console.WriteLine("File contains async methods");
}
```

## Supported C# Features

### Currently Supported
- ✅ Classes, interfaces, structs, enums
- ✅ Methods, properties, fields
- ✅ Constructors
- ✅ Namespaces (traditional and file-scoped)
- ✅ Access modifiers
- ✅ Async methods
- ✅ XML documentation comments
- ✅ Using directives and aliases

### Limitations
- ❌ Generic type parameters
- ❌ Attributes
- ❌ Events and delegates
- ❌ Local functions
- ❌ Expression-bodied members (partial support)
- ❌ Pattern matching constructs
- ❌ Record types

## Integration with Code Analyzer

The C# Analyzer integrates seamlessly with the broader code analysis framework:

1. **Registration**: Automatically registered in DI container as `ILanguageAnalyzer`
2. **File Detection**: Handles `.cs` and `.csx` files
3. **Storage**: Symbols are persisted to SQLite database
4. **Indexing**: Supports incremental updates through file watching
5. **Search**: Extracted symbols are searchable through the search service

## Future Enhancements

1. **Full Project Analysis**: Support for analyzing entire projects with cross-file references
2. **Type Resolution**: Complete type information including generic parameters
3. **Reference Finding**: Implementation of `FindReferencesAsync` using Roslyn's Find References API
4. **Refactoring Support**: Code fixes and refactoring suggestions
5. **Diagnostics**: Integration with Roslyn analyzers for code quality checks
6. **Performance Metrics**: Method complexity, code metrics
7. **Semantic Highlighting**: Token classification for syntax highlighting