using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Andy.CodeAnalyzer.Analyzers;
using Andy.CodeAnalyzer.Models;
using Andy.CodeAnalyzer.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Andy.CodeAnalyzer.Tests.Services;

public class ContextProviderServiceTests
{
    private readonly Mock<ILogger<ContextProviderService>> _loggerMock;
    private readonly Mock<ICodeAnalyzerService> _analyzerServiceMock;
    private readonly Mock<ISearchService> _searchServiceMock;
    private readonly ContextProviderService _contextProvider;

    public ContextProviderServiceTests()
    {
        _loggerMock = new Mock<ILogger<ContextProviderService>>();
        _analyzerServiceMock = new Mock<ICodeAnalyzerService>();
        _searchServiceMock = new Mock<ISearchService>();
        _contextProvider = new ContextProviderService(
            _loggerMock.Object,
            _analyzerServiceMock.Object,
            _searchServiceMock.Object);
    }

    [Fact]
    public async Task GetRelevantContextAsync_WithNoSearchResults_ShouldReturnBasicContext()
    {
        // Arrange
        var query = "test query";
        var maxTokens = 1000;
        var searchResults = new List<SearchResult>();
        
        _searchServiceMock.Setup(x => x.SearchTextAsync(
                query,
                It.IsAny<SearchOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        // Act
        var result = await _contextProvider.GetRelevantContextAsync(query, maxTokens);

        // Assert
        Assert.Contains("## Code Context", result);
        Assert.DoesNotContain("### File:", result);
        _searchServiceMock.Verify(x => x.SearchTextAsync(
            query,
            It.Is<SearchOptions>(o => o.MaxResults == 10),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRelevantContextAsync_WithSearchResults_ShouldIncludeSnippets()
    {
        // Arrange
        var query = "calculate total";
        var maxTokens = 2000;
        var searchResults = new List<SearchResult>
        {
            new SearchResult
            {
                FilePath = "/src/OrderService.cs",
                Language = "csharp",
                Snippet = "public decimal CalculateTotal(Order order)\n{\n    return order.Items.Sum(i => i.Price);\n}",
                Score = 0.95f
            },
            new SearchResult
            {
                FilePath = "/src/InvoiceService.cs",
                Language = "csharp",
                Snippet = "private decimal CalculateTotalWithTax(decimal subtotal)\n{\n    return subtotal * 1.1m;\n}",
                Score = 0.85f
            }
        };
        
        _searchServiceMock.Setup(x => x.SearchTextAsync(
                query,
                It.IsAny<SearchOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        // Act
        var result = await _contextProvider.GetRelevantContextAsync(query, maxTokens);

        // Assert
        Assert.Contains("## Code Context", result);
        Assert.Contains("### File: /src/OrderService.cs", result);
        Assert.Contains("```csharp", result);
        Assert.Contains("CalculateTotal", result);
        Assert.Contains("### File: /src/InvoiceService.cs", result);
        Assert.Contains("CalculateTotalWithTax", result);
    }

    [Fact]
    public async Task GetRelevantContextAsync_WithCancellation_ShouldPropagateToken()
    {
        // Arrange
        var query = "test";
        var maxTokens = 1000;
        var cts = new CancellationTokenSource();
        
        _searchServiceMock.Setup(x => x.SearchTextAsync(
                It.IsAny<string>(),
                It.IsAny<SearchOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, SearchOptions, CancellationToken>((q, o, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult<IEnumerable<SearchResult>>(new List<SearchResult>());
            });

        // Act & Assert
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _contextProvider.GetRelevantContextAsync(query, maxTokens, cts.Token));
    }

    [Fact]
    public async Task GenerateCodeMapAsync_WithEmptyFilePaths_ShouldReturnEmptyMap()
    {
        // Arrange
        var filePaths = Array.Empty<string>();

        // Act
        var result = await _contextProvider.GenerateCodeMapAsync(filePaths);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Files);
        Assert.Empty(result.Dependencies);
        Assert.Empty(result.SymbolReferences);
    }

    [Fact]
    public async Task GenerateCodeMapAsync_WithValidFiles_ShouldGenerateMap()
    {
        // Arrange
        var filePaths = new[] { "/src/File1.cs", "/src/File2.cs" };
        
        var structure1 = new CodeStructure
        {
            Language = "csharp",
            Symbols = new List<Symbol>
            {
                new Symbol { Name = "Class1", Kind = SymbolKind.Class },
                new Symbol { Name = "Method1", Kind = SymbolKind.Function },
                new Symbol { Name = "Field1", Kind = SymbolKind.Field }
            },
            Imports = new List<Import> { new Import { Name = "System" } },
            Exports = new List<Export> { new Export { Name = "Class1" } }
        };
        
        var structure2 = new CodeStructure
        {
            Language = "csharp",
            Symbols = new List<Symbol>
            {
                new Symbol { Name = "Interface1", Kind = SymbolKind.Interface },
                new Symbol { Name = "Method2", Kind = SymbolKind.Function }
            },
            Imports = new List<Import> { new Import { Name = "System.Linq" } },
            Exports = new List<Export> { new Export { Name = "Interface1" } }
        };
        
        _analyzerServiceMock.Setup(x => x.GetFileStructureAsync("/src/File1.cs", It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure1);
        _analyzerServiceMock.Setup(x => x.GetFileStructureAsync("/src/File2.cs", It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure2);

        // Act
        var result = await _contextProvider.GenerateCodeMapAsync(filePaths);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Files.Count);
        
        var file1Overview = result.Files["/src/File1.cs"];
        Assert.Equal("/src/File1.cs", file1Overview.Path);
        Assert.Equal("csharp", file1Overview.Language);
        Assert.Equal(2, file1Overview.MainSymbols.Count); // Class1 and Method1 (not Field1)
        Assert.Contains("Class1", file1Overview.MainSymbols);
        Assert.Contains("Method1", file1Overview.MainSymbols);
        Assert.Single(file1Overview.Imports);
        Assert.Single(file1Overview.Exports);
        
        var file2Overview = result.Files["/src/File2.cs"];
        Assert.Equal(2, file2Overview.MainSymbols.Count);
        Assert.Contains("Interface1", file2Overview.MainSymbols);
    }

    [Fact]
    public async Task GenerateCodeMapAsync_WithFileAnalysisFailure_ShouldContinueWithOtherFiles()
    {
        // Arrange
        var filePaths = new[] { "/src/File1.cs", "/src/File2.cs", "/src/File3.cs" };
        
        var structure1 = new CodeStructure
        {
            Language = "csharp",
            Symbols = new List<Symbol> { new Symbol { Name = "Class1", Kind = SymbolKind.Class } },
            Imports = new List<Import>(),
            Exports = new List<Export>()
        };
        
        var structure3 = new CodeStructure
        {
            Language = "csharp",
            Symbols = new List<Symbol> { new Symbol { Name = "Class3", Kind = SymbolKind.Class } },
            Imports = new List<Import>(),
            Exports = new List<Export>()
        };
        
        _analyzerServiceMock.Setup(x => x.GetFileStructureAsync("/src/File1.cs", It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure1);
        _analyzerServiceMock.Setup(x => x.GetFileStructureAsync("/src/File2.cs", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("File parsing error"));
        _analyzerServiceMock.Setup(x => x.GetFileStructureAsync("/src/File3.cs", It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure3);

        // Act
        var result = await _contextProvider.GenerateCodeMapAsync(filePaths);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Files.Count); // Only File1 and File3
        Assert.True(result.Files.ContainsKey("/src/File1.cs"));
        Assert.False(result.Files.ContainsKey("/src/File2.cs"));
        Assert.True(result.Files.ContainsKey("/src/File3.cs"));
        
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to generate map for file")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateCodeMapAsync_FilterSymbolsByKind_ShouldOnlyIncludeMainSymbols()
    {
        // Arrange
        var filePaths = new[] { "/src/TestFile.cs" };
        
        var structure = new CodeStructure
        {
            Language = "csharp",
            Symbols = new List<Symbol>
            {
                new Symbol { Name = "TestClass", Kind = SymbolKind.Class },
                new Symbol { Name = "TestInterface", Kind = SymbolKind.Interface },
                new Symbol { Name = "TestMethod", Kind = SymbolKind.Function },
                new Symbol { Name = "TestProperty", Kind = SymbolKind.Property },
                new Symbol { Name = "TestField", Kind = SymbolKind.Field },
                new Symbol { Name = "TestEnum", Kind = SymbolKind.Enum }
            },
            Imports = new List<Import>(),
            Exports = new List<Export>()
        };
        
        _analyzerServiceMock.Setup(x => x.GetFileStructureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure);

        // Act
        var result = await _contextProvider.GenerateCodeMapAsync(filePaths);

        // Assert
        var fileOverview = result.Files[filePaths[0]];
        Assert.Equal(3, fileOverview.MainSymbols.Count); // Only Class, Interface, and Function
        Assert.Contains("TestClass", fileOverview.MainSymbols);
        Assert.Contains("TestInterface", fileOverview.MainSymbols);
        Assert.Contains("TestMethod", fileOverview.MainSymbols);
        Assert.DoesNotContain("TestProperty", fileOverview.MainSymbols);
        Assert.DoesNotContain("TestField", fileOverview.MainSymbols);
        Assert.DoesNotContain("TestEnum", fileOverview.MainSymbols);
    }

    [Fact]
    public async Task AnswerStructuralQueryAsync_ShouldReturnNotImplementedMessage()
    {
        // Arrange
        var question = "What classes implement IOrderService?";

        // Act
        var result = await _contextProvider.AnswerStructuralQueryAsync(question);

        // Assert
        Assert.Equal("Structural query answering not yet implemented.", result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Answering structural query")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnswerStructuralQueryAsync_WithCancellation_ShouldReturnImmediately()
    {
        // Arrange
        var question = "test question";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _contextProvider.AnswerStructuralQueryAsync(question, cts.Token);

        // Assert
        Assert.Equal("Structural query answering not yet implemented.", result);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ContextProviderService(
            null!,
            _analyzerServiceMock.Object,
            _searchServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullAnalyzerService_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ContextProviderService(
            _loggerMock.Object,
            null!,
            _searchServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullSearchService_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ContextProviderService(
            _loggerMock.Object,
            _analyzerServiceMock.Object,
            null!));
    }
}