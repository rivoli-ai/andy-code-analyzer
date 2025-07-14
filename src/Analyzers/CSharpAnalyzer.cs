using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Extensions.Logging;
using Andy.CodeAnalyzer.Models;
using SymbolKind = Andy.CodeAnalyzer.Models.SymbolKind;
using Location = Andy.CodeAnalyzer.Models.Location;

namespace Andy.CodeAnalyzer.Analyzers;

/// <summary>
/// C# language analyzer using Roslyn.
/// </summary>
public class CSharpAnalyzer : ILanguageAnalyzer
{
    private readonly ILogger<CSharpAnalyzer> _logger;
    private readonly AdhocWorkspace _workspace;
    private readonly CSharpCompilationOptions _compilationOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpAnalyzer"/> class.
    /// </summary>
    public CSharpAnalyzer(ILogger<CSharpAnalyzer> logger)
    {
        _logger = logger;
        _workspace = new AdhocWorkspace();
        _compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            allowUnsafe: true,
            concurrentBuild: true);
    }

    /// <inheritdoc/>
    public string[] SupportedExtensions => new[] { ".cs", ".csx" };

    /// <inheritdoc/>
    public string Language => "csharp";

    /// <inheritdoc/>
    public async Task<CodeStructure> AnalyzeFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Analyzing C# file: {FilePath}", filePath);

        try
        {
            var text = await File.ReadAllTextAsync(filePath, cancellationToken);
            var sourceText = SourceText.From(text);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, cancellationToken: cancellationToken);

            // Create a simple compilation for semantic analysis
            var compilation = CSharpCompilation.Create("Analysis")
                .AddSyntaxTrees(syntaxTree)
                .AddReferences(GetBasicReferences())
                .WithOptions(_compilationOptions);

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = await syntaxTree.GetRootAsync(cancellationToken);

            var structure = new CodeStructure
            {
                FilePath = filePath,
                Language = Language,
                AnalyzedAt = DateTime.UtcNow
            };

            // Extract using directives
            var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
            foreach (var usingDirective in usingDirectives)
            {
                structure.Imports.Add(new Import
                {
                    Name = usingDirective.Name?.ToString() ?? string.Empty,
                    Alias = usingDirective.Alias?.Name.ToString()
                });
            }

            // Extract symbols
            var symbolVisitor = new SymbolExtractorVisitor(semanticModel, _logger);
            symbolVisitor.Visit(root);
            structure.Symbols = symbolVisitor.ExtractedSymbols;

            // Add metadata
            structure.Metadata["HasAsync"] = HasAsyncMethods(root);
            structure.Metadata["TargetFramework"] = GetTargetFramework(root);
            structure.Metadata["LangVersion"] = GetLangVersion(root);

            _logger.LogDebug("Extracted {SymbolCount} symbols from {FilePath}", structure.Symbols.Count, filePath);
            return structure;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze file: {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Symbol>> ExtractSymbolsAsync(string content, CancellationToken cancellationToken = default)
    {
        var sourceText = SourceText.From(content);
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, cancellationToken: cancellationToken);
        
        var compilation = CSharpCompilation.Create("SymbolExtraction")
            .AddSyntaxTrees(syntaxTree)
            .AddReferences(GetBasicReferences())
            .WithOptions(_compilationOptions);

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = await syntaxTree.GetRootAsync(cancellationToken);

        var symbolVisitor = new SymbolExtractorVisitor(semanticModel, _logger);
        symbolVisitor.Visit(root);

        return symbolVisitor.ExtractedSymbols;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Reference>> FindReferencesAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        // TODO: Implement reference finding using Roslyn's FindReferences API
        _logger.LogWarning("FindReferencesAsync not yet implemented for C#");
        return Task.FromResult(Enumerable.Empty<Reference>());
    }

    private static IEnumerable<MetadataReference> GetBasicReferences()
    {
        var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        
        yield return MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        yield return MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll"));
        yield return MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Collections.dll"));
        yield return MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Linq.dll"));
        yield return MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Threading.Tasks.dll"));
        yield return MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Console.dll"));
        yield return MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location);
    }

    private static bool HasAsyncMethods(SyntaxNode root)
    {
        return root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Any(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.AsyncKeyword)));
    }

    private static string GetTargetFramework(SyntaxNode root)
    {
        // This would need to read from the project file in a real implementation
        return "net9.0";
    }

    private static string GetLangVersion(SyntaxNode root)
    {
        // This would need to read from the project file in a real implementation
        return "latest";
    }

    /// <summary>
    /// Visitor to extract symbols from the syntax tree.
    /// </summary>
    private class SymbolExtractorVisitor : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly ILogger _logger;
        private readonly Stack<Symbol> _symbolStack;
        private readonly List<Symbol> _symbols;

        public List<Symbol> ExtractedSymbols => _symbols;

        public SymbolExtractorVisitor(SemanticModel semanticModel, ILogger logger)
        {
            _semanticModel = semanticModel;
            _logger = logger;
            _symbolStack = new Stack<Symbol>();
            _symbols = new List<Symbol>();
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            var symbol = CreateSymbol(node, SymbolKind.Namespace, node.Name.ToString());
            _symbols.Add(symbol);
            _symbolStack.Push(symbol);
            base.VisitNamespaceDeclaration(node);
            _symbolStack.Pop();
        }

        public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
        {
            var symbol = CreateSymbol(node, SymbolKind.Namespace, node.Name.ToString());
            _symbols.Add(symbol);
            _symbolStack.Push(symbol);
            base.VisitFileScopedNamespaceDeclaration(node);
            _symbolStack.Pop();
        }

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

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            var symbol = CreateSymbol(node, SymbolKind.Interface, node.Identifier.Text);
            AddModifiers(symbol, node.Modifiers);
            symbol.Documentation = GetDocumentation(node);
            _symbols.Add(symbol);
            _symbolStack.Push(symbol);
            base.VisitInterfaceDeclaration(node);
            _symbolStack.Pop();
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            var symbol = CreateSymbol(node, SymbolKind.Struct, node.Identifier.Text);
            AddModifiers(symbol, node.Modifiers);
            symbol.Documentation = GetDocumentation(node);
            _symbols.Add(symbol);
            _symbolStack.Push(symbol);
            base.VisitStructDeclaration(node);
            _symbolStack.Pop();
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var symbol = CreateSymbol(node, SymbolKind.Enum, node.Identifier.Text);
            AddModifiers(symbol, node.Modifiers);
            symbol.Documentation = GetDocumentation(node);
            _symbols.Add(symbol);
            base.VisitEnumDeclaration(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var symbol = CreateSymbol(node, SymbolKind.Method, node.Identifier.Text);
            AddModifiers(symbol, node.Modifiers);
            symbol.Documentation = GetDocumentation(node);
            _symbols.Add(symbol);
            base.VisitMethodDeclaration(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var symbol = CreateSymbol(node, SymbolKind.Method, node.Identifier.Text);
            AddModifiers(symbol, node.Modifiers);
            symbol.Documentation = GetDocumentation(node);
            _symbols.Add(symbol);
            base.VisitConstructorDeclaration(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var symbol = CreateSymbol(node, SymbolKind.Property, node.Identifier.Text);
            AddModifiers(symbol, node.Modifiers);
            symbol.Documentation = GetDocumentation(node);
            _symbols.Add(symbol);
            base.VisitPropertyDeclaration(node);
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            foreach (var variable in node.Declaration.Variables)
            {
                var symbol = CreateSymbol(node, SymbolKind.Field, variable.Identifier.Text);
                AddModifiers(symbol, node.Modifiers);
                symbol.Documentation = GetDocumentation(node);
                _symbols.Add(symbol);
            }
            base.VisitFieldDeclaration(node);
        }

        private Symbol CreateSymbol(SyntaxNode node, Models.SymbolKind kind, string name)
        {
            var span = node.GetLocation().GetLineSpan();
            var symbol = new Symbol
            {
                Name = name,
                Kind = kind,
                Location = new Location
                {
                    StartLine = span.StartLinePosition.Line + 1,
                    StartColumn = span.StartLinePosition.Character + 1,
                    EndLine = span.EndLinePosition.Line + 1,
                    EndColumn = span.EndLinePosition.Character + 1
                },
                ParentSymbol = _symbolStack.Count > 0 ? _symbolStack.Peek().Name : null
            };

            return symbol;
        }

        private static void AddModifiers(Symbol symbol, SyntaxTokenList modifiers)
        {
            foreach (var modifier in modifiers)
            {
                symbol.Modifiers.Add(modifier.Text);
            }
        }

        private string? GetDocumentation(SyntaxNode node)
        {
            var trivia = node.GetLeadingTrivia()
                .Where(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || 
                           t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                .FirstOrDefault();

            if (trivia == default)
                return null;

            var structure = trivia.GetStructure();
            if (structure is DocumentationCommentTriviaSyntax docComment)
            {
                return ExtractDocumentationText(docComment);
            }

            return null;
        }

        private static string ExtractDocumentationText(DocumentationCommentTriviaSyntax docComment)
        {
            var summaryElement = docComment.Content
                .OfType<XmlElementSyntax>()
                .FirstOrDefault(e => e.StartTag.Name.ToString() == "summary");

            if (summaryElement == null)
                return string.Empty;

            var text = string.Join(" ", summaryElement.Content
                .OfType<XmlTextSyntax>()
                .SelectMany(t => t.TextTokens)
                .Select(token => token.ToString().Trim()));

            return text.Trim();
        }
    }
}