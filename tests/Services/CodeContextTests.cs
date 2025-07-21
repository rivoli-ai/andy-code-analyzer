using System;
using System.Collections.Generic;
using Andy.CodeAnalyzer.Analyzers;
using Andy.CodeAnalyzer.Models;
using Andy.CodeAnalyzer.Services;
using Xunit;

namespace Andy.CodeAnalyzer.Tests.Services;

public class CodeContextTests
{
    [Fact]
    public void CodeContext_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var context = new CodeContext();

        // Assert
        Assert.Null(context.CurrentSymbol);
        Assert.NotNull(context.ParentSymbols);
        Assert.Empty(context.ParentSymbols);
        Assert.NotNull(context.NearbySymbols);
        Assert.Empty(context.NearbySymbols);
        Assert.NotNull(context.ImportsInScope);
        Assert.Empty(context.ImportsInScope);
        Assert.Equal(string.Empty, context.CodeSnippet);
    }

    [Fact]
    public void CodeContext_SetCurrentSymbol_ShouldSetCorrectly()
    {
        // Arrange
        var context = new CodeContext();
        var symbol = new Symbol
        {
            Name = "TestMethod",
            Kind = SymbolKind.Method,
            Location = new Location { StartLine = 10, StartColumn = 5 }
        };

        // Act
        context.CurrentSymbol = symbol;

        // Assert
        Assert.NotNull(context.CurrentSymbol);
        Assert.Equal("TestMethod", context.CurrentSymbol.Name);
        Assert.Equal(SymbolKind.Method, context.CurrentSymbol.Kind);
        Assert.Equal(10, context.CurrentSymbol.Location.StartLine);
    }

    [Fact]
    public void CodeContext_AddParentSymbols_ShouldAddToList()
    {
        // Arrange
        var context = new CodeContext();
        var parentClass = new Symbol { Name = "TestClass", Kind = SymbolKind.Class };
        var parentNamespace = new Symbol { Name = "TestNamespace", Kind = SymbolKind.Namespace };

        // Act
        context.ParentSymbols.Add(parentNamespace);
        context.ParentSymbols.Add(parentClass);

        // Assert
        Assert.Equal(2, context.ParentSymbols.Count);
        Assert.Equal("TestNamespace", context.ParentSymbols[0].Name);
        Assert.Equal("TestClass", context.ParentSymbols[1].Name);
    }

    [Fact]
    public void CodeContext_AddNearbySymbols_ShouldAddToList()
    {
        // Arrange
        var context = new CodeContext();
        var symbols = new List<Symbol>
        {
            new Symbol { Name = "Method1", Kind = SymbolKind.Method },
            new Symbol { Name = "Method2", Kind = SymbolKind.Method },
            new Symbol { Name = "Property1", Kind = SymbolKind.Property }
        };

        // Act
        context.NearbySymbols.AddRange(symbols);

        // Assert
        Assert.Equal(3, context.NearbySymbols.Count);
        Assert.Contains(context.NearbySymbols, s => s.Name == "Method1");
        Assert.Contains(context.NearbySymbols, s => s.Name == "Method2");
        Assert.Contains(context.NearbySymbols, s => s.Name == "Property1");
    }

    [Fact]
    public void CodeContext_AddImports_ShouldAddToList()
    {
        // Arrange
        var context = new CodeContext();
        var imports = new List<Import>
        {
            new Import { Name = "System", Alias = null },
            new Import { Name = "System.Collections.Generic", Alias = null },
            new Import { Name = "MyNamespace.MyClass", Alias = "MyAlias" }
        };

        // Act
        context.ImportsInScope.AddRange(imports);

        // Assert
        Assert.Equal(3, context.ImportsInScope.Count);
        Assert.Contains(context.ImportsInScope, i => i.Name == "System");
        Assert.Contains(context.ImportsInScope, i => i.Name == "System.Collections.Generic");
        Assert.Contains(context.ImportsInScope, i => i.Name == "MyNamespace.MyClass" && i.Alias == "MyAlias");
    }

    [Fact]
    public void CodeContext_SetCodeSnippet_ShouldSetCorrectly()
    {
        // Arrange
        var context = new CodeContext();
        var snippet = @"public void TestMethod()
{
    Console.WriteLine(""Hello, World!"");
}";

        // Act
        context.CodeSnippet = snippet;

        // Assert
        Assert.Equal(snippet, context.CodeSnippet);
    }

    [Fact]
    public void CodeContext_ComplexScenario_ShouldHandleAllProperties()
    {
        // Arrange
        var context = new CodeContext();
        
        // Set current symbol
        context.CurrentSymbol = new Symbol 
        { 
            Name = "CalculateTotal", 
            Kind = SymbolKind.Method,
            Location = new Location { StartLine = 25, StartColumn = 10 }
        };

        // Add parent symbols hierarchy
        context.ParentSymbols.AddRange(new[]
        {
            new Symbol { Name = "MyApp", Kind = SymbolKind.Namespace },
            new Symbol { Name = "Services", Kind = SymbolKind.Namespace },
            new Symbol { Name = "OrderService", Kind = SymbolKind.Class }
        });

        // Add nearby symbols
        context.NearbySymbols.AddRange(new[]
        {
            new Symbol { Name = "ValidateOrder", Kind = SymbolKind.Method, Location = new Location { StartLine = 20 } },
            new Symbol { Name = "ProcessPayment", Kind = SymbolKind.Method, Location = new Location { StartLine = 30 } },
            new Symbol { Name = "_orderRepository", Kind = SymbolKind.Field, Location = new Location { StartLine = 10 } }
        });

        // Add imports
        context.ImportsInScope.AddRange(new[]
        {
            new Import { Name = "System" },
            new Import { Name = "System.Linq" },
            new Import { Name = "MyApp.Models" }
        });

        // Set code snippet
        context.CodeSnippet = @"public decimal CalculateTotal(Order order)
{
    return order.Items.Sum(item => item.Price * item.Quantity);
}";

        // Assert all properties are set correctly
        Assert.NotNull(context.CurrentSymbol);
        Assert.Equal("CalculateTotal", context.CurrentSymbol.Name);
        Assert.Equal(3, context.ParentSymbols.Count);
        Assert.Equal("OrderService", context.ParentSymbols[2].Name);
        Assert.Equal(3, context.NearbySymbols.Count);
        Assert.Equal(3, context.ImportsInScope.Count);
        Assert.Contains("CalculateTotal", context.CodeSnippet);
    }
}